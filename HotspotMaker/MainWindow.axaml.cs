using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using System;
using System.ComponentModel;
using System.Linq;

namespace HotspotMaker
{
    public partial class MainWindow : Window
    {
        private MainWindowVM ViewModel { get; }


        public MainWindow()
        {
            InitializeComponent();

            ViewModel = new MainWindowVM(StorageProvider, Clipboard);
            DataContext = ViewModel;

            ViewModel.PropertyChanged += ViewModel_PropertyChanged;

            UpdateRecentFilePathMenuItems();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            // To improve the user experience, the editor view needs to handle certain keys regardless of whether it has focus.

            // NOTE: This hack ensures that our key-down handling won't gobble up certain keys when writing in a TextBox:
            if (e.Source is not TextBox)
                ProjectView.HandleKeyDown(e);
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            base.OnClosing(e);

            if (DataContext is MainWindowVM mainWindowVM && mainWindowVM.HotspotProject?.IsModified == true)
            {
                // Let the VM warn the user about unsaved changes:
                e.Cancel = true;
                mainWindowVM.ExitProgram();
            }
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainWindowVM.RecentFilePaths))
                UpdateRecentFilePathMenuItems();
        }


        private void UpdateRecentFilePathMenuItems()
        {
            var startSeparatorIndex = FileMenuItem.Items.IndexOf(RecentFilesStartSeparator);
            var endSeparatorIndex = FileMenuItem.Items.IndexOf(RecentFilesEndSeparator);
            var recentItemsCount = endSeparatorIndex - startSeparatorIndex - 1;

            // Remove old items:
            for (int i = 0; i < recentItemsCount; i++)
                FileMenuItem.Items.RemoveAt(startSeparatorIndex + 1);


            RecentFilesStartSeparator.IsVisible = ViewModel.RecentFilePaths.Any();

            // Add new items:
            for (int i = 0; i < ViewModel.RecentFilePaths.Count; i++)
            {
                var recentFilePath = ViewModel.RecentFilePaths[i];

                var menuItem = new MenuItem {
                    Header = $"_{i + 1}: {recentFilePath.Replace("_", "__")}",
                };

                menuItem.Click += (sender, e) =>
                {
                    e.Handled = true;
                    ViewModel.OpenRecentFilePath(recentFilePath);

                    Menu.Close();
                };

                FileMenuItem.Items.Insert(startSeparatorIndex + 1 + i, menuItem);
            }
        }
    }
}