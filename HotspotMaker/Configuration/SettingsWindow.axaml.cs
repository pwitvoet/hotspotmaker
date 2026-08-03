using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using HotspotMaker.Util.UI;
using System.Linq;
using System.Threading.Tasks;

namespace HotspotMaker.Configuration
{
    public partial class SettingsWindow : Window
    {
        public static async Task<bool?> Show(SettingsWindowVM viewModel)
        {
            var window = new SettingsWindow(viewModel);
            if (!await window.ShowAsDialog())
                return null;

            return window.Result;
        }


        private SettingsWindowVM ViewModel { get; }
        private bool? Result { get; set; }

        private KeyBindingVM? KeyGestureCapturingKeyBinding { get; set; }


        public SettingsWindow(SettingsWindowVM viewModel)
        {
            InitializeComponent();

            ViewModel = viewModel;
            DataContext = viewModel;

            KeyDownEvent.AddClassHandler<SettingsWindow>(KeyDownHandler, RoutingStrategies.Tunnel, handledEventsToo: true);
        }


        private void KeyDownHandler(object? sender, KeyEventArgs e)
        {
            if (e.Handled)
                return;

            if (KeyGestureCapturingKeyBinding != null && !e.Key.IsModifierKey())
            {
                KeyGestureCapturingKeyBinding.KeyGesture = new KeyGesture(e.Key, e.KeyModifiers);
                KeyGestureCapturingKeyBinding.IsEditingKey = false;
                KeyGestureCapturingKeyBinding = null;
                e.Handled = true;
            }
        }


        private void OkButton_Click(object? sender, RoutedEventArgs e)
        {
            Result = true;
            Close();
        }

        private void CancelButton_Click(object? sender, RoutedEventArgs e)
        {
            Result = false;
            Close();
        }

        private void KeyBinding_Tapped(object? sender, TappedEventArgs e)
        {
            if (KeyGestureCapturingKeyBinding != null)
            {
                KeyGestureCapturingKeyBinding.IsEditingKey = false;
                KeyGestureCapturingKeyBinding = null;
            }
        }

        private void KeyBinding_DoubleTapped(object? sender, TappedEventArgs e)
        {
            var listBoxItem = (sender as Control)?.GetLogicalAncestors()
                .OfType<ListBoxItem>()
                .FirstOrDefault();
            if (listBoxItem == null)
                return;

            if (listBoxItem.DataContext is not KeyBindingVM keyBindingVM)
                return;


            if (KeyGestureCapturingKeyBinding != null)
                KeyGestureCapturingKeyBinding.IsEditingKey = false;

            KeyGestureCapturingKeyBinding = keyBindingVM;
            KeyGestureCapturingKeyBinding.IsEditingKey = true;
        }
    }
}
