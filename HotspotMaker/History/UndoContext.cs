namespace HotspotMaker.History
{
    public enum ActionType
    {
        None,

        Do,
        Undo,
        Redo,
    }


    public class UndoContext
    {
        public static UndoContext None { get; } = new UndoContext(0, ActionType.None);


        public int ActionID { get; }
        public ActionType Type { get; }


        public UndoContext(int actionID, ActionType type)
        {
            ActionID = actionID;
            Type = type;
        }
    }
}
