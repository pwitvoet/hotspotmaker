using Avalonia.Controls;
using Avalonia.Interactivity;
using HotspotMaker.Util.UI;
using System.Threading.Tasks;

namespace HotspotMaker.Configuration
{
    public partial class PresetWindow : Window
    {
        public static async Task<bool?> Show(PresetVM viewModel)
        {
            var window = new PresetWindow(viewModel);
            if (!await window.ShowAsDialog())
                return null;

            return window.Result;
        }


        private PresetVM ViewModel { get; }
        private bool? Result { get; set; }

        private KeyBindingVM? KeyGestureCapturingKeyBinding { get; set; }


        public PresetWindow(PresetVM viewModel)
        {
            InitializeComponent();

            ViewModel = viewModel;
            DataContext = viewModel;
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
    }
}
