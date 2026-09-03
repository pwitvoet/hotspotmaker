using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace HotspotMaker.Controls
{
    /// <summary>
    /// A workaround for TreeView's broken SelectionChanged behavior (which doesn't report deselected items, and doesn't batch operations like select-all).
    /// Do not bind to SelectedItems, but instead add a listener to the CustomSelectionChanged event to handle selection changes.
    /// </summary>
    public class CustomTreeView : TreeView
    {
        public static readonly RoutedEvent<SelectionChangedEventArgs> CustomSelectionChangedEvent =
            RoutedEvent.Register<CustomTreeView, SelectionChangedEventArgs>(
                nameof(CustomSelectionChanged),
                RoutingStrategies.Bubble);


        public event EventHandler<SelectionChangedEventArgs>? CustomSelectionChanged
        {
            add => AddHandler(CustomSelectionChangedEvent, value);
            remove => RemoveHandler(CustomSelectionChangedEvent, value);
        }


        protected override Type StyleKeyOverride => typeof(TreeView);


        private List<object?> CurrentSelectedItems { get; } = new();
        private List<object?> SelectionAddedItems { get; } = new();
        private List<object?> SelectionRemovedItems { get; } = new();


        public CustomTreeView()
        {
            if (SelectedItems is INotifyCollectionChanged observableCollection)
                observableCollection.CollectionChanged += SelectedItems_CollectionChanged;
        }

        public override bool UpdateSelectionFromEvent(Control container, RoutedEventArgs eventArgs)
        {
            var result = base.UpdateSelectionFromEvent(container, eventArgs);
            HandleSelectionChanges();
            return result;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            HandleSelectionChanges();
        }


        private void SelectedItems_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                case NotifyCollectionChangedAction.Remove:
                case NotifyCollectionChangedAction.Replace:
                    if (e.NewItems != null)
                    {
                        foreach (var item in e.NewItems)
                        {
                            if (SelectionRemovedItems.Contains(item))
                                SelectionRemovedItems.Remove(item);
                            else
                                SelectionAddedItems.Add(item);
                        }

                        CurrentSelectedItems.AddRange(e.NewItems.OfType<object?>());
                    }

                    if (e.OldItems != null)
                    {
                        SelectionRemovedItems.AddRange(e.OldItems.OfType<object?>());

                        foreach (var item in e.OldItems)
                            CurrentSelectedItems.Remove(item);
                    }
                    break;

                case NotifyCollectionChangedAction.Reset:
                    SelectionRemovedItems.AddRange(CurrentSelectedItems);
                    CurrentSelectedItems.Clear();
                    break;
            }
        }

        private void HandleSelectionChanges()
        {
            if (SelectionAddedItems.Count == 0 && SelectionRemovedItems.Count == 0)
                return;

            RaiseEvent(new SelectionChangedEventArgs(CustomSelectionChangedEvent, SelectionRemovedItems, SelectionAddedItems));
            SelectionAddedItems.Clear();
            SelectionRemovedItems.Clear();
        }
    }
}
