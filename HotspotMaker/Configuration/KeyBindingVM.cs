using Avalonia.Input;
using HotspotMaker.Presets;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HotspotMaker.Configuration
{
    public class KeyBindingVM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));


        // Bindable properties:
        private KeyGesture? _keyGesture;
        public KeyGesture? KeyGesture
        {
            get => _keyGesture;
            set
            {
                _keyGesture = value;
                RaisePropertyChanged(nameof(KeyGestureRepresentation));
                RaisePropertyChanged();
            }
        }

        private bool _isEditingKey;
        public bool IsEditingKey
        {
            get => _isEditingKey;
            set { _isEditingKey = value; RaisePropertyChanged(); }
        }

        private PresetVM? _preset;
        public PresetVM? Preset
        {
            get => _preset;
            set
            {
                if (_preset != null)
                    _preset.PropertyChanged -= Preset_PropertyChanged;

                _preset = value;

                if (_preset != null)
                    _preset.PropertyChanged += Preset_PropertyChanged;

                RaisePropertyChanged();
                RaisePropertyChanged(nameof(Description));
            }
        }

        private bool _hasDuplicateKeyGesture;
        public bool HasDuplicateKeyGesture
        {
            get => _hasDuplicateKeyGesture;
            set { _hasDuplicateKeyGesture = value; RaisePropertyChanged(); }
        }


        // Derived properties:
        public bool IsPresetBinding => EditorAction == EditorAction.ApplyPreset;

        public string Description => GetDescription(EditorAction, Preset);

        public string KeyGestureRepresentation => KeyGesture?.ToString("p", null) ?? "";


        // Read-only:
        public EditorAction EditorAction { get; }


        public KeyBindingVM(KeyBinding keyBinding)
        {
            _keyGesture = keyBinding.KeyGesture;

            if (keyBinding.Preset != null)
                _preset = new PresetVM(keyBinding.Preset);

            EditorAction = keyBinding.EditorAction;
        }

        public KeyBinding GetKeyBinding()
        {
            if (EditorAction == EditorAction.ApplyPreset)
                return new KeyBinding(KeyGesture, Preset?.GetPreset() ?? new Preset("", []));
            else
                return new KeyBinding(EditorAction, KeyGesture);
        }


        private void Preset_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PresetVM.Description))
                RaisePropertyChanged(nameof(Description));
        }


        private static string GetDescription(EditorAction editorAction, PresetVM? presetVM)
        {
            switch (editorAction)
            {
                case EditorAction.OpenWadFile: return "Open wad file";
                case EditorAction.SaveProject: return "Save project";
                case EditorAction.CloseProject: return "Close project";
                case EditorAction.ExitProgram: return "Exit program";
                case EditorAction.Undo: return "Undo";
                case EditorAction.Redo: return "Redo";
                case EditorAction.ToggleGrid: return "Toggle grid";
                case EditorAction.IncreaseGridSize: return "Increase grid size";
                case EditorAction.DecreaseGridSize: return "Decrease grid size";
                case EditorAction.ToggleCoordinatesDisplay: return "Toggle coordinate display";
                case EditorAction.ToggleIconsDisplay: return "Toggle icons display";
                case EditorAction.ToggleRectanglesDisplay: return "Toggle rectangle display";
                case EditorAction.ToggleTexturePanel: return "Toggle texture panel";
                case EditorAction.ToggleRectanglePanel: return "Toggle rectangle panel";
                case EditorAction.CreateNewRectangleSet: return "Create new rectangle set";
                case EditorAction.EditRectangleLabels: return "Edit rectangle labels";
                case EditorAction.AddRectangleLabel: return "Add rectangle label";
                case EditorAction.RenameRectangleLabel: return "Rename rectangle label";
                case EditorAction.RemoveRectangleLabel: return "Remove rectangle label";
                case EditorAction.AddTextureLabel: return "Add texture label";
                case EditorAction.RenameTextureLabel: return "Rename texture label";
                case EditorAction.RemoveTextureLabel: return "Remove texture label";
                case EditorAction.Cut: return "Cut";
                case EditorAction.Copy: return "Copy";
                case EditorAction.Paste: return "Paste";
                case EditorAction.SelectAll: return "Select all";
                case EditorAction.Delete: return "Delete";
                case EditorAction.MoveUp: return "Move up";
                case EditorAction.MoveRight: return "Move right";
                case EditorAction.MoveDown: return "Move down";
                case EditorAction.MoveLeft: return "Move left";
                case EditorAction.ApplyPreset: return presetVM?.Description ?? "";
                default: return "";
            }
        }
    }
}
