using Avalonia.Input;

namespace HotspotMaker.Util.UI
{
    public static class KeyExtensions
    {
        public static bool IsModifierKey(this Key key)
            => key == Key.LeftCtrl || key == Key.RightCtrl || key == Key.LeftShift || key == Key.RightShift || key == Key.LeftAlt || key == Key.RightAlt;
    }
}
