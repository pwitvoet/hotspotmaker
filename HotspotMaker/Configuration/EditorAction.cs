namespace HotspotMaker.Configuration
{
    public enum EditorAction
    {
        OpenWadFile,
        SaveProject,
        CloseProject,
        ExitProgram,

        Undo,
        Redo,

        ToggleGrid,
        IncreaseGridSize,
        DecreaseGridSize,
        ToggleCoordinatesDisplay,
        ToggleIconsDisplay,
        ToggleRectanglesDisplay,

        ToggleTexturePanel,
        ToggleRectanglePanel,

        CreateNewRectangleSet,

        EditRectangleLabels,
        AddRectangleLabel,
        RenameRectangleLabel,
        RemoveRectangleLabel,
        AddTextureLabel,
        RenameTextureLabel,
        RemoveTextureLabel,

        Cut,
        Copy,
        Paste,
        SelectAll,
        Delete,

        MoveUp,
        MoveRight,
        MoveDown,
        MoveLeft,

        ApplyPreset,
    }
}
