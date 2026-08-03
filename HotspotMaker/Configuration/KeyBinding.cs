using Avalonia.Input;
using HotspotMaker.Presets;

namespace HotspotMaker.Configuration
{
    public class KeyBinding
    {
        public EditorAction EditorAction { get; }
        public KeyGesture? KeyGesture { get; }

        public Preset? Preset { get; }


        public KeyBinding(EditorAction editorAction, KeyGesture? keyGesture)
        {
            EditorAction = editorAction;
            KeyGesture = keyGesture;
        }

        public KeyBinding(KeyGesture? keyGesture, Preset preset)
        {
            EditorAction = EditorAction.ApplyPreset;
            KeyGesture = keyGesture;
            Preset = preset;
        }
    }
}
