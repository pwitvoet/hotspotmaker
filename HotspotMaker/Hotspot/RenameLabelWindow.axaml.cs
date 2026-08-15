using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using HotspotMaker.Util.UI;
using System.Linq;
using System.Threading.Tasks;

namespace HotspotMaker.Hotspot
{
    public partial class RenameLabelWindow : Window
    {
        public static async Task<(bool?, int, string)> Show(string[] labels)
        {
            var window = new RenameLabelWindow(labels);
            if (!await window.ShowAsDialog() || window.NewLabelTextBox.Text == null)
                return (null, 0, "");

            return (window.Result, window.LabelSelectionComboBox.SelectedIndex, window.NewLabelTextBox.Text);
        }


        private string[] Labels { get; }
        private bool? Result { get; set; }


        public RenameLabelWindow(string[] labels)
        {
            InitializeComponent();

            Labels = labels;

            LabelSelectionComboBox.ItemsSource = Labels;
            LabelSelectionComboBox.SelectedIndex = 0;

            NewLabelTextBox.Text = labels.FirstOrDefault();
        }

        private void LabelSelectionComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            var index = LabelSelectionComboBox.SelectedIndex;
            if (index >= 0 && index < Labels.Length)
                NewLabelTextBox.Text = Labels[index];
        }

        private void NewLabelTextBox_TextChanged(object? sender, TextChangedEventArgs e)
        {
            OkButton.IsEnabled = !string.IsNullOrEmpty(NewLabelTextBox.Text);
        }


        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);

            LabelSelectionComboBox.Focus(NavigationMethod.Tab);
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