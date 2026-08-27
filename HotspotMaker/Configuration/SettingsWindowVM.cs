using HotspotMaker.Presets;
using HotspotMaker.Util.UI;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace HotspotMaker.Configuration
{
    public class SettingsWindowVM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));


        // Bindable properties:
        public ObservableCollection<object> KeyBindingsAndHeaders { get; } = new();


        // Commands:
        public void RevertToDefaultSettings()
        {
            SetKeyBindings(new Settings());
        }

        public void AddPreset()
        {
            KeyBindingsAndHeaders.Add(new KeyBindingVM(new KeyBinding(null, new Preset("Apply preset", []))));
            // TODO: Scroll into view!
        }

        public async void EditKeyBindingPreset(KeyBindingVM keyBindingVM)
        {
            if (keyBindingVM.Preset == null)
                return;

            var presetVM = new PresetVM(keyBindingVM.Preset.GetPreset());
            var result = await PresetWindow.Show(presetVM);
            if (result != true)
                return;


            keyBindingVM.Preset = presetVM;
        }

        public void RemoveKeyBinding(KeyBindingVM keyBindingVM)
        {
            if (keyBindingVM.EditorAction != EditorAction.ApplyPreset)
                return;

            KeyBindingsAndHeaders.Remove(keyBindingVM);
        }


        public SettingsWindowVM(Settings settings)
        {
            KeyBindingsAndHeaders.CollectionChanged += KeyBindingsAndHeaders_CollectionChanged;

            SetKeyBindings(settings);
        }

        public KeyBinding[] GetKeyBindings()
        {
            return KeyBindingsAndHeaders
                .OfType<KeyBindingVM>()
                .Select(keyBindingVM => keyBindingVM.GetKeyBinding())
                .ToArray();
        }


        private void SetKeyBindings(Settings settings)
        {
            KeyBindingsAndHeaders.Clear();

            KeyBindingsAndHeaders.Add(new GroupHeader("Project"));
            AddKeyBindings(settings,
                EditorAction.OpenWadFile,
                EditorAction.SaveProject,
                EditorAction.CloseProject,
                EditorAction.ExitProgram);

            KeyBindingsAndHeaders.Add(new GroupHeader("Undo/redo"));
            AddKeyBindings(settings,
                EditorAction.Undo,
                EditorAction.Redo);
            
            KeyBindingsAndHeaders.Add(new GroupHeader("Editor view"));
            AddKeyBindings(settings,
                EditorAction.ToggleGrid,
                EditorAction.IncreaseGridSize,
                EditorAction.DecreaseGridSize,
                EditorAction.ToggleCoordinatesDisplay,
                EditorAction.ToggleIconsDisplay,
                EditorAction.ToggleRectanglesDisplay);

            KeyBindingsAndHeaders.Add(new GroupHeader("Panels"));
            AddKeyBindings(settings,
                EditorAction.ToggleTexturePanel,
                EditorAction.ToggleRectanglePanel);
            
            KeyBindingsAndHeaders.Add(new GroupHeader("Rectangle sets"));
            AddKeyBindings(settings,
                EditorAction.CreateNewRectangleSet);
            
            KeyBindingsAndHeaders.Add(new GroupHeader("Labels"));
            AddKeyBindings(settings,
                EditorAction.EditRectangleLabels,
                EditorAction.AddRectangleLabel,
                EditorAction.RenameRectangleLabel,
                EditorAction.RemoveRectangleLabel,
                EditorAction.AddTextureLabel,
                EditorAction.RenameTextureLabel,
                EditorAction.RemoveTextureLabel);

            KeyBindingsAndHeaders.Add(new GroupHeader("Selection"));
            AddKeyBindings(settings,
                EditorAction.Cut,
                EditorAction.Copy,
                EditorAction.Paste,
                EditorAction.SelectAll,
                EditorAction.Delete,
                EditorAction.MoveUp,
                EditorAction.MoveRight,
                EditorAction.MoveDown,
                EditorAction.MoveLeft);

            KeyBindingsAndHeaders.Add(new GroupHeader("Presets"));
            foreach (var keyBinding in settings.KeyBindings.Where(keyBinding => keyBinding.EditorAction == EditorAction.ApplyPreset))
                KeyBindingsAndHeaders.Add(new KeyBindingVM(keyBinding));
        }

        private void AddKeyBindings(Settings settings, params EditorAction[] editorActions)
        {
            foreach (var editorAction in editorActions)
            {
                var keyBinding = settings.KeyBindings.FirstOrDefault(keyBinding => keyBinding.EditorAction == editorAction);
                if (keyBinding == null)
                    keyBinding = new KeyBinding(editorAction, null);

                KeyBindingsAndHeaders.Add(new KeyBindingVM(keyBinding));
            }
        }

        private void UpdateDuplicateKeyGestureState()
        {
            var keyBindings = KeyBindingsAndHeaders.OfType<KeyBindingVM>();

            foreach (var keyBindingVM in keyBindings)
                keyBindingVM.HasDuplicateKeyGesture = keyBindings.Count(keyBinding => object.Equals(keyBinding.KeyGesture, keyBindingVM.KeyGesture)) > 1;
        }


        private void KeyBindingsAndHeaders_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems)
                {
                    if (item is KeyBindingVM keyBindingVM)
                        keyBindingVM.PropertyChanged += KeyBindingVM_PropertyChanged;
                }
            }

            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems)
                {
                    if (item is KeyBindingVM keyBindingVM)
                        keyBindingVM.PropertyChanged -= KeyBindingVM_PropertyChanged;
                }
            }
        }

        private void KeyBindingVM_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(KeyBindingVM.KeyGesture))
                UpdateDuplicateKeyGestureState();
        }
    }
}
