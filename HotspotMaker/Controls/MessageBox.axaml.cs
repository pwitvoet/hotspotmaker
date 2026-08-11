using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using HotspotMaker.Util.UI;
using System;
using System.Threading.Tasks;

namespace HotspotMaker.Controls
{
    [Flags]
    public enum MessageBoxButtons
    {
        None =      0x00,

        Ok =        0x01,
        Cancel =    0x02,
        Yes =       0x04,
        No =        0x08,

        OkCancel = Ok | Cancel,
        YesNo = Yes | No,
    }


    public partial class MessageBox : Window
    {
        public static async Task<bool?> Show(string title, string message, MessageBoxButtons buttons = MessageBoxButtons.OkCancel)
        {
            var messageBox = new MessageBox(title, message, buttons);
            if (!await messageBox.ShowAsDialog())
                return null;

            return messageBox.Result;
        }

        public static async Task<int?> Show(string title, string message, string[] customButtonLabels)
        {
            var messageBox = new MessageBox(title, message, customButtonLabels);
            if (!await messageBox.ShowAsDialog())
                return null;

            return messageBox.ButtonIndex;
        }

        public static async Task<int?> ShowComboBox(string title, string message, string[] options, MessageBoxButtons buttons = MessageBoxButtons.OkCancel)
        {
            var messageBox = new MessageBox(title, message, buttons);
            messageBox.SetOptions(options);

            if (!await messageBox.ShowAsDialog())
                return null;

            return messageBox.Result == true ? messageBox.OptionIndex : null;
        }

        public static async Task<string?> ShowTextBox(string title, string message, string? initialText = null, Func<string?, string?>? getErrorMessage = null, MessageBoxButtons buttons = MessageBoxButtons.OkCancel)
        {
            var messageBox = new MessageBox(title, message, buttons);
            messageBox.SetTextBoxSettings(initialText, getErrorMessage);

            if (!await messageBox.ShowAsDialog())
                return null;

            return messageBox.Result == true ? messageBox.InputText : null;
        }


        private bool? Result { get; set; }
        private int? ButtonIndex { get; set; }
        private int? OptionIndex => OptionsComboBox.SelectedIndex;
        private string? InputText => InputTextBox.Text;

        private Func<string?, string?>? GetErrorMessage { get; set; }


        public MessageBox(string title, string message, MessageBoxButtons buttons)
        {
            InitializeComponent();

            Title = title;
            MessageTextBlock.Text = message;

            OkButton.IsEnabled = buttons.HasFlag(MessageBoxButtons.Ok) || buttons.HasFlag(MessageBoxButtons.Yes);
            OkButton.IsVisible = OkButton.IsEnabled;

            CancelButton.IsEnabled = buttons.HasFlag(MessageBoxButtons.Cancel) || buttons.HasFlag(MessageBoxButtons.No);
            CancelButton.IsVisible = CancelButton.IsEnabled;

            if (buttons.HasFlag(MessageBoxButtons.Yes))
                OkButton.Content = "Yes";

            if (buttons.HasFlag(MessageBoxButtons.No))
                CancelButton.Content = "No";
        }

        public MessageBox(string title, string message, string[] customButtonLabels)
            : this(title, message, MessageBoxButtons.None)
        {
            for (int i = 0; i < customButtonLabels.Length; i++)
            {
                var button = new Button {
                    Content = customButtonLabels[i],
                    Padding = OkButton.Padding,
                    Margin = OkButton.Margin,
                };

                var index = i;
                button.Click += (sender, e) =>
                {
                    ButtonIndex = index;
                    Close();
                };

                ButtonsBar.Children.Add(button);
            }
        }


        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);

            if (InputTextBox.IsVisible)
            {
                InputTextBox.Focus();
                InputTextBox.SelectAll();
            }
        }


        private void SetOptions(string[] options)
        {
            OptionsComboBox.IsEnabled = true;
            OptionsComboBox.IsVisible = true;

            OptionsComboBox.ItemsSource = options;
            OptionsComboBox.SelectedIndex = 0;
        }

        private void SetTextBoxSettings(string? initialText, Func<string?, string?>? getErrorMessage)
        {
            InputTextBox.IsEnabled = true;
            InputTextBox.IsVisible = true;

            GetErrorMessage = getErrorMessage;
            InputTextBox.Text = initialText;
        }

        private void InputTextBox_TextChanged(object? sender, TextChangedEventArgs e)
        {
            if (GetErrorMessage != null)
            {
                var errorMessage = GetErrorMessage(InputTextBox.Text);
                var isValid = string.IsNullOrEmpty(errorMessage);
                InputTextErrorMessage.Text = errorMessage;

                if (!isValid)
                    InputTextBox.Classes.Add("Invalid");
                else
                    InputTextBox.Classes.Remove("Invalid");

                OkButton.IsEnabled = isValid;
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == Key.Enter && OkButton.IsVisible && OkButton.IsEnabled)
            {
                Result = true;

                // Only close if we have an Ok/Yes button:
                Close();
            }
            else if (e.Key == Key.Escape)
            {
                if (CancelButton.IsVisible)
                    Result = false;

                // Always close on escape:
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