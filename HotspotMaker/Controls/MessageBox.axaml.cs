using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
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
            if (!await ShowMessageBox(messageBox))
                return null;

            return messageBox.Result;
        }

        public static async Task<int?> Show(string title, string message, string[] customButtonLabels)
        {
            var messagBox = new MessageBox(title, message, customButtonLabels);
            if (!await ShowMessageBox(messagBox))
                return null;

            return messagBox.ButtonIndex;
        }


        private static async Task<bool> ShowMessageBox(MessageBox messageBox)
        {
            var mainWindow = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
            if (mainWindow == null)
                return false;

            messageBox.Position = new PixelPoint(
                mainWindow.Position.X + (int)((mainWindow.Width - messageBox.Width) / 2),
                mainWindow.Position.Y + (int)((mainWindow.Height - messageBox.Height) / 2));

            await messageBox.ShowDialog(mainWindow);
            return true;
        }


        private bool? Result { get; set; }
        private int? ButtonIndex { get; set; }

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

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == Key.Enter && OkButton.IsVisible)
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