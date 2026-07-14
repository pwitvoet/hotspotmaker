using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using HotspotMaker.Controls;
using HotspotMaker.Editor;
using HotspotMaker.Hotspot;
using MLib.Texturing.Hotspotting;
using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace HotspotMaker
{
    public class MainWindowVM : INotifyPropertyChanged
    {
        private const string DefaultWindowTitle = "HotspotMaker";


        public event PropertyChangedEventHandler? PropertyChanged;
        protected void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));


        // Bindable properties:
        private string _windowTitle = DefaultWindowTitle;
        public string WindowTitle
        {
            get => _windowTitle;
            set { _windowTitle = value; RaisePropertyChanged(); }
        }

        private string? _statusMessage;
        public string? StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; RaisePropertyChanged(); }
        }

        private HotspotProjectVM? _hotspotProject;
        public HotspotProjectVM? HotspotProject
        {
            get => _hotspotProject;
            set
            {
                if (_hotspotProject != null)
                {
                    _hotspotProject.PropertyChanged -= HotspotProject_PropertyChanged;
                    _hotspotProject.HotspotEditor.PropertyChanged -= HotspotEditor_PropertyChanged;
                    _hotspotProject.Selection.SelectionChanged -= Selection_SelectionChanged;
                }

                _hotspotProject = value;

                if (_hotspotProject != null)
                {
                    _hotspotProject.PropertyChanged += HotspotProject_PropertyChanged;
                    _hotspotProject.HotspotEditor.PropertyChanged += HotspotEditor_PropertyChanged;
                    _hotspotProject.Selection.SelectionChanged += Selection_SelectionChanged;
                }

                UpdateWindowTitle(value);
                RaisePropertyChanged(nameof(HasOpenProject));
                RaisePropertyChanged(nameof(IsCutAvailable));
                RaisePropertyChanged(nameof(IsCopyAvailable));
                RaisePropertyChanged(nameof(IsPasteAvailable));
                RaisePropertyChanged(nameof(IsGridEnabled));
                RaisePropertyChanged(nameof(IsCoordinatesVisible));
                RaisePropertyChanged();
            }
        }


        // Derived properties:
        public bool HasOpenProject => HotspotProject != null;

        public bool IsCutAvailable => IsCopyAvailable;

        public bool IsCopyAvailable => Clipboard != null && HotspotProject != null && !HotspotProject.Selection.IsEmpty;

        public bool IsPasteAvailable => Clipboard != null && HotspotProject != null;

        public bool IsUndoAvailable => HotspotProject?.IsUndoAvailable == true;

        public bool IsRedoAvailable => HotspotProject?.IsRedoAvailable == true;

        public bool IsGridEnabled => HotspotProject?.HotspotEditor.IsGridEnabled == true;

        public bool IsCoordinatesVisible => HotspotProject?.HotspotEditor.IsCoordinatesVisible == true;


        private IStorageProvider StorageProvider { get; }
        private IClipboard? Clipboard { get; }


        public MainWindowVM(IStorageProvider storageProvider, IClipboard? clipboard)
        {
            StorageProvider = storageProvider;
            Clipboard = clipboard;

            UpdateWindowTitle(null);
        }


        // Commands:
        public async Task OpenWadFile()
        {
            if (HotspotProject != null)
            {
                await CloseCurrentProject();
                if (HotspotProject != null)
                    return;
            }

            try
            {
                // TODO: Remember the previously opened file(s), and open the most recent folder (SuggestedStartLocation)!
                var selectedFiles = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Open .wad file",
                    FileTypeFilter = [new FilePickerFileType("Wad file") { Patterns = ["*.wad"] }],
                    AllowMultiple = false,
                });

                if (selectedFiles.Any())
                {
                    var wadFilePath = selectedFiles.First().Path.LocalPath;
                    var hotspotFilePath = wadFilePath + ".hotspot";
                    HotspotProject = HotspotProjectVM.Load(wadFilePath, hotspotFilePath, Clipboard);

                    StatusMessage = $"Opened '{wadFilePath}'.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to open wad file: {ex.GetType().Name}: {ex.Message}.";

                // TODO: Improve error message!
                await MessageBox.Show("Error", $"Failed to open project: {ex.GetType().Name}: {ex.Message}.", MessageBoxButtons.Ok);
            }
        }

        public async Task SaveCurrentProject()
        {
            try
            {
                if (HotspotProject == null)
                    return;

                var hotspotFileData = HotspotProject.CreateHotspotFileData();
                HotspotFileWriter.Save(HotspotProject.HotspotFilePath, hotspotFileData);

                StatusMessage = $"Hotspot file saved.";

                HotspotProject.MarkAsUnmodified();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to save hotspot file: {ex.GetType().Name}: {ex.Message}.";

                // TODO: Improve error message!
                await MessageBox.Show("Error", $"Failed to save project: {ex.GetType().Name}: {ex.Message}.", MessageBoxButtons.Ok);
            }
        }

        public async Task CloseCurrentProject()
        {
            if (HotspotProject == null)
                return;

            if (HotspotProject.IsModified)
            {
                var confirmation = await MessageBox.Show("Unsaved changes", "You have unsaved changes. Are you sure you want to close the project without saving?", MessageBoxButtons.OkCancel);
                if (confirmation != true)
                    return;
            }

            // TODO: This does not erase/reset the editor VM state or the view!
            HotspotProject = null;

            StatusMessage = $"Project closed.";
        }

        public async Task ExitProgram()
        {
            if (HotspotProject != null)
            {
                await CloseCurrentProject();
                if (HotspotProject != null)
                    return;
            }

            Environment.Exit(0);
        }

        public async Task CutSelection()
        {
            if (HotspotProject == null || Clipboard == null)
                return;

            try
            {
                await HotspotProject.HotspotEditor.CopySelectionToClipboard(deleteSelection: true);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Cut failed: {ex.GetType().Name}: {ex.Message}.";
            }
        }

        public async Task CopySelection()
        {
            if (HotspotProject == null || Clipboard == null)
                return;

            try
            {
                await HotspotProject.HotspotEditor.CopySelectionToClipboard();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Copy failed: {ex.GetType().Name}: {ex.Message}.";
            }
        }

        public async Task PasteSelection()
        {
            if (HotspotProject == null || Clipboard == null)
                return;

            try
            {
                var pasteResult = await HotspotProject.HotspotEditor.PasteFromClipboard();
                switch (pasteResult)
                {
                    case Editor.PasteResult.NoTargetRectangleSet:
                        StatusMessage = $"Cannot paste, no hotspot rectangle set selected.";
                        break;

                    case Editor.PasteResult.ClipboardEmpty:
                        StatusMessage = $"Paste failed: clipboard is empty.";
                        break;

                    case Editor.PasteResult.ClipboardInvalidData:
                        StatusMessage = $"Paste failed: clipboard does not contain valid hotspot rectangle data.";
                        break;

                    case Editor.PasteResult.ClipboardNotAvailable:
                        StatusMessage = $"Paste failed: clipboard not available.";
                        break;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Paste failed: {ex.GetType().Name}: {ex.Message}.";
            }
        }

        public void UndoLastAction()
            => HotspotProject?.UndoLastAction();

        public void RedoLastAction()
            => HotspotProject?.RedoLastAction();

        public void ToggleGrid()
        {
            if (HotspotProject == null)
                return;

            HotspotProject.HotspotEditor.ToggleGrid();
        }

        public void IncreaseGridSize()
        {
            if (HotspotProject == null)
                return;

            HotspotProject.HotspotEditor.IncreaseGridSize();
        }

        public void DecreaseGridSize()
        {
            if (HotspotProject == null)
                return;

            HotspotProject.HotspotEditor.DecreaseGridSize();
        }

        public void ToggleCoordinatesDisplay()
        {
            if (HotspotProject == null)
                return;

            HotspotProject.HotspotEditor.ToggleCoordinatesDisplay();
        }


        private void HotspotProject_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(HotspotProjectVM.IsUndoAvailable))
                RaisePropertyChanged(nameof(IsUndoAvailable));
            else if (e.PropertyName == nameof(HotspotProjectVM.IsRedoAvailable))
                RaisePropertyChanged(nameof(IsRedoAvailable));
            else if (e.PropertyName == nameof(HotspotProjectVM.IsModified))
                UpdateWindowTitle(HotspotProject);
        }

        private void HotspotEditor_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(HotspotEditorVM.IsGridEnabled))
                RaisePropertyChanged(nameof(IsGridEnabled));
            else if (e.PropertyName == nameof(HotspotEditorVM.IsCoordinatesVisible))
                RaisePropertyChanged(nameof(IsCoordinatesVisible));
        }

        private void Selection_SelectionChanged(HotspotRectangleVM[] deselected, HotspotRectangleVM[] selected)
        {
            RaisePropertyChanged(nameof(IsCutAvailable));
            RaisePropertyChanged(nameof(IsCopyAvailable));
        }

        private void UpdateWindowTitle(HotspotProjectVM? hotspotProject)
        {
            if (hotspotProject == null)
            {
                WindowTitle = DefaultWindowTitle;
            }
            else
            {
                WindowTitle = $"{DefaultWindowTitle} - {hotspotProject.HotspotFilePath}{(hotspotProject.IsModified ? " *" : "")}";
            }
        }
    }
}
