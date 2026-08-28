using System;

namespace HotspotMaker.History
{
    public class UndoableAction
    {
        public int ActionID { get; }

        private Action<UndoContext> DoAction { get; }
        private Action<UndoContext> UndoAction { get; }


        public UndoableAction(int id, Action<UndoContext> doAction, Action<UndoContext> undoAction)
        {
            ActionID = id;
            DoAction = doAction;
            UndoAction = undoAction;
        }

        public void Do()
            => DoAction(new UndoContext(ActionID, ActionType.Do));

        public void Undo()
            => UndoAction(new UndoContext(ActionID, ActionType.Undo));

        public void Redo()
            => DoAction(new UndoContext(ActionID, ActionType.Redo));
    }
}
