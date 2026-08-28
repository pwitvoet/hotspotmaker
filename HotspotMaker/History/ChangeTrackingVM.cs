using HotspotMaker.Util.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace HotspotMaker.History
{
    /// <summary>
    /// Base class for view models that need undo/redo support.
    /// </summary>
    public abstract class ChangeTrackingVM : INotifyPropertyChanged, IFocusTrackingVM
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        void IFocusTrackingVM.FocustLost(string propertyName)
        {
            if (_ongoingActionPropertyName == propertyName)
                StopOngoingAction();
        }


        public virtual bool IsModified => CurrentStateID != UnmodifiedStateID;


        // Internal state:
        protected UndoSystem UndoSystem { get; }
        private bool SuppressChangeTracking { get; set; }

        private int UnmodifiedStateID { get; set; } = 0;
        private int CurrentStateID => ModificationIDs.TryPeek(out var id) ? id : 0;

        private string? _ongoingActionPropertyName;
        private Action<UndoContext>? _ongoingActionUndo;

        private Stack<int> ModificationIDs { get; } = new();


        public ChangeTrackingVM(UndoSystem undoSystem)
        {
            UndoSystem = undoSystem;

            undoSystem.OnActionUndone += UndoSystem_OnActionUndone;
            undoSystem.OnActionRedone += UndoSystem_OnActionRedone;
        }

        /// <summary>
        /// Sets the current state of this view model as the unmodified state.
        /// </summary>
        public virtual void MarkAsUnmodified()
        {
            UnmodifiedStateID = CurrentStateID;
            RaisePropertyChanged(nameof(IsModified));
        }

        public void RegisterModification(UndoContext context)
        {
            if (context.Type != ActionType.Do && context.Type != ActionType.Redo)
                return;

            if (!ModificationIDs.Any() || ModificationIDs.Peek() != context.ActionID)
            {
                ModificationIDs.Push(context.ActionID);
                RaisePropertyChanged(nameof(IsModified));
            }
        }

        public void UnregisterModification(UndoContext context)
        {
            if (context.Type != ActionType.Undo)
                return;

            if (ModificationIDs.Any() && ModificationIDs.Peek() == context.ActionID)
            {
                ModificationIDs.Pop();
                RaisePropertyChanged(nameof(IsModified));
            }
        }


        /// <summary>
        /// Performs an undoable action. This will stop any ongoing action.
        /// </summary>
        protected void PerformUndoableAction(Action<UndoContext> doAction, Action<UndoContext> undoAction)
        {
            StopOngoingAction();
            UndoSystem.PerformUndoableAction(doAction, undoAction);
        }

        /// <summary>
        /// Performs an undoable action.
        /// Subsequent calls to this method will update the registered undoable action instead of creating multiple undoable actions,
        /// so that all changes can be undone in one go. Also see <see cref="StopOngoingAction"/>.
        /// </summary>
        protected void PerformUndoableActionOngoing(string actionName, Action<UndoContext> doAction, Action<UndoContext> undoAction)
        {
            if (SuppressChangeTracking)
            {
                doAction(UndoContext.None);
                return;
            }

            if (_ongoingActionPropertyName != null && _ongoingActionUndo != null && _ongoingActionPropertyName == actionName)
            {
                UndoSystem.ReplaceCurrentUndoableAction(doAction, _ongoingActionUndo);
            }
            else
            {
                _ongoingActionPropertyName = actionName;
                _ongoingActionUndo = undoAction;

                UndoSystem.PerformUndoableAction(doAction, _ongoingActionUndo);
            }
        }

        /// <summary>
        /// Updates a property and registers the change with the undo system so that it can be undone.
        /// This will stop any ongoing action.
        /// </summary>
        protected void SetProperty<TValue>(Action<UndoContext, TValue> setter, TValue oldValue, TValue newValue, [CallerMemberName] string? propertyName = null)
        {
            if (SuppressChangeTracking)
            {
                setter(UndoContext.None, newValue);
                RaisePropertyChanged(propertyName);
                return;
            }

            StopOngoingAction();
            UndoSystem.PerformUndoableAction(
                context =>
                {
                    setter(context, newValue);
                    RaisePropertyChanged(propertyName);
                },
                context =>
                {
                    setter(context, oldValue);
                    RaisePropertyChanged(propertyName);
                });
        }

        /// <summary>
        /// Updates a property and registers the change with the undo system so that it can be undone.
        /// Subsequent calls to this method will update the registered undoable action instead of creating multiple undoable actions,
        /// so that all changes can be undone in one go. Also see <see cref="StopOngoingAction"/>.
        /// </summary>
        protected void SetPropertyOngoing<TValue>(Action<TValue> setter, TValue oldValue, TValue newValue, [CallerMemberName] string? propertyName = null)
        {
            if (SuppressChangeTracking)
            {
                setter(newValue);
                RaisePropertyChanged(propertyName);
                return;
            }

            if (_ongoingActionPropertyName != null && _ongoingActionUndo != null && _ongoingActionPropertyName == propertyName)
            {
                UndoSystem.ReplaceCurrentUndoableAction(
                    context =>
                    {
                        setter(newValue);
                        RegisterModification(context);
                        RaisePropertyChanged(propertyName);
                    },
                    _ongoingActionUndo);
            }
            else
            {
                _ongoingActionPropertyName = propertyName;
                _ongoingActionUndo = context =>
                {
                    setter(oldValue);
                    UnregisterModification(context);
                    RaisePropertyChanged(propertyName);
                };

                UndoSystem.PerformUndoableAction(
                    context =>
                    {
                        setter(newValue);
                        RegisterModification(context);
                        RaisePropertyChanged(propertyName);
                    },
                    _ongoingActionUndo);
            }
        }

        /// <summary>
        /// Stops the currently ongoing action, if there is any.
        /// Subsequent calls to <see cref="SetPropertyOngoing{TValue}(Action{TValue}, TValue, TValue, string?)"/> will create a new undoable action.
        /// </summary>
        protected void StopOngoingAction()
        {
            _ongoingActionPropertyName = null;
            _ongoingActionUndo = null;
        }

        /// <summary>
        /// Executes the given action without change tracking. Useful for initializing properties.
        /// </summary>
        protected void WithoutChangeTracking(Action action)
        {
            try
            {
                SuppressChangeTracking = true;
                action();
            }
            finally
            {
                SuppressChangeTracking = false;
            }
        }


        private void UndoSystem_OnActionUndone()
            => StopOngoingAction();

        private void UndoSystem_OnActionRedone()
            => StopOngoingAction();
    }
}
