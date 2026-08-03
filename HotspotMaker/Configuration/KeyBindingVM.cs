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
                case EditorAction.ToggleGrid: return "Toggle grid";
                case EditorAction.IncreaseGridSize: return "Increase grid size";
                case EditorAction.DecreaseGridSize: return "Decrease grid size";
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
