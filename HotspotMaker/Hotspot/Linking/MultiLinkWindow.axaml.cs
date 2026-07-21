using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using HotspotMaker.Util.UI;
using System.Threading.Tasks;

namespace HotspotMaker.Hotspot.Linking
{
    public partial class MultiLinkWindow : Window
    {
        public static async Task<bool?> Show(MultiLinkWindowVM viewModel)
        {
            var window = new MultiLinkWindow(viewModel);
            if (!await window.ShowAsDialog())
                return null;

            return window.Result;
        }


        private MultiLinkWindowVM ViewModel { get; }
        private bool? Result { get; set; }


        public MultiLinkWindow(MultiLinkWindowVM viewModel)
        {
            InitializeComponent();

            ViewModel = viewModel;
            DataContext = viewModel;
        }


        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);

            TextureNamePatternTextBox.Focus();
            TextureNamePatternTextBox.SelectAll();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == Key.Enter && OkButton.IsEnabled)
            {
                Result = true;
                Close();
            }
            else if (e.Key == Key.Escape)
            {
                Result = false;
                Close();
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
    }
}