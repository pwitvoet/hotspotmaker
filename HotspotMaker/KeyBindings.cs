using Avalonia.Input;
using HotspotMaker.Configuration;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HotspotMaker
{
    public class KeyBindings : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));


        public KeyGesture? OpenWadFile => Settings.GetKeyGesture(EditorAction.OpenWadFile);
        public KeyGesture? SaveProject => Settings.GetKeyGesture(EditorAction.SaveProject);
        public KeyGesture? CloseProject => Settings.GetKeyGesture(EditorAction.CloseProject);
        public KeyGesture? ExitProgram => Settings.GetKeyGesture(EditorAction.ExitProgram);
        public KeyGesture? Undo => Settings.GetKeyGesture(EditorAction.Undo);
        public KeyGesture? Redo => Settings.GetKeyGesture(EditorAction.Redo);
        public KeyGesture? ToggleGrid => Settings.GetKeyGesture(EditorAction.ToggleGrid);
        public KeyGesture? IncreaseGridSize => Settings.GetKeyGesture(EditorAction.IncreaseGridSize);
        public KeyGesture? DecreaseGridSize => Settings.GetKeyGesture(EditorAction.DecreaseGridSize);
        public KeyGesture? ToggleCoordinatesDisplay => Settings.GetKeyGesture(EditorAction.ToggleCoordinatesDisplay);
        public KeyGesture? ToggleIconsDisplay => Settings.GetKeyGesture(EditorAction.ToggleIconsDisplay);
        public KeyGesture? ToggleRectanglesDisplay => Settings.GetKeyGesture(EditorAction.ToggleRectanglesDisplay);
        public KeyGesture? ToggleTexturePanel => Settings.GetKeyGesture(EditorAction.ToggleTexturePanel);
        public KeyGesture? ToggleRectanglePanel => Settings.GetKeyGesture(EditorAction.ToggleRectanglePanel);
        public KeyGesture? CreateNewRectangleSet => Settings.GetKeyGesture(EditorAction.CreateNewRectangleSet);
        public KeyGesture? EditRectangleLabels => Settings.GetKeyGesture(EditorAction.EditRectangleLabels);
        public KeyGesture? AddRectangleLabel => Settings.GetKeyGesture(EditorAction.AddRectangleLabel);
        public KeyGesture? RenameRectangleLabel => Settings.GetKeyGesture(EditorAction.RenameRectangleLabel);
        public KeyGesture? RemoveRectangleLabel => Settings.GetKeyGesture(EditorAction.RemoveRectangleLabel);
        public KeyGesture? AddTextureLabel => Settings.GetKeyGesture(EditorAction.AddTextureLabel);
        public KeyGesture? RenameTextureLabel => Settings.GetKeyGesture(EditorAction.RenameTextureLabel);
        public KeyGesture? RemoveTextureLabel => Settings.GetKeyGesture(EditorAction.RemoveTextureLabel);
        public KeyGesture? Cut => Settings.GetKeyGesture(EditorAction.Cut);
        public KeyGesture? Copy => Settings.GetKeyGesture(EditorAction.Copy);
        public KeyGesture? Paste => Settings.GetKeyGesture(EditorAction.Paste);
        public KeyGesture? SelectAll => Settings.GetKeyGesture(EditorAction.SelectAll);
        public KeyGesture? Delete => Settings.GetKeyGesture(EditorAction.Delete);
        public KeyGesture? MoveUp => Settings.GetKeyGesture(EditorAction.MoveUp);
        public KeyGesture? MoveRight => Settings.GetKeyGesture(EditorAction.MoveRight);
        public KeyGesture? MoveDown => Settings.GetKeyGesture(EditorAction.MoveDown);
        public KeyGesture? MoveLeft => Settings.GetKeyGesture(EditorAction.MoveLeft);


        private Settings Settings { get; }


        public KeyBindings(Settings settings)
        {
            Settings = settings;
        }

        public void Update()
        {
            RaisePropertyChanged("");
        }
    }
}
