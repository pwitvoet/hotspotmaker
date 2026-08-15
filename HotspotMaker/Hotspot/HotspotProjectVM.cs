using Avalonia;
using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using HotspotMaker.Configuration;
using HotspotMaker.Controls;
using HotspotMaker.Editor;
using HotspotMaker.History;
using HotspotMaker.Util;
using MLib.Texturing;
using MLib.Texturing.Hotspotting;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace HotspotMaker.Hotspot
{
    public class HotspotProjectVM : ChangeTrackingVM
    {
        // TODO: Improve error reporting!
        public static HotspotProjectVM Load(string wadFilePath, string hotspotFilePath, Settings settings, IClipboard? clipboard)
        {
            WadFile wadFile;
            try
            {
                wadFile = WadFile.Load(wadFilePath);
            }
            catch (Exception ex)
            {
                // TODO: Wrap this in an exception that explains that the wad loading part failed!
                throw;
            }

            HotspotFileData hotspotFileData;
            try
            {
                if (File.Exists(hotspotFilePath))
                {
                    hotspotFileData = HotspotFileParser.Load(hotspotFilePath);
                }
                else
                {
                    hotspotFileData = new HotspotFileData(Array.Empty<HotspotRectangleSet>(), Array.Empty<HotspotBinding>());
                }
            }
            catch (Exception ex)
            {
                // TODO: Wrap this in an exception that explains that the hotspot loading part failed!
                throw;
            }

            return new HotspotProjectVM(wadFile, hotspotFileData, hotspotFilePath, settings, clipboard);
        }


        // Events:
        public event Action? HotspotRectangleLabelsFocusRequested;
        protected void RaiseHotspotRectangleLabelsFocusRequested()
            => HotspotRectangleLabelsFocusRequested?.Invoke();


        // Bindable properties:
        private Bitmap? _selectedTextureImage;
        public Bitmap? SelectedTextureImage
        {
            get => _selectedTextureImage;
            set
            {
                _selectedTextureImage = value;
                HotspotEditor.TextureImage = value;

                RaisePropertyChanged();
            }
        }

        private HotspotRectangleSetVM? _selectedHotspotRectangleSet;
        public HotspotRectangleSetVM? SelectedHotspotRectangleSet
        {
            get => _selectedHotspotRectangleSet;
            set
            {
                _selectedHotspotRectangleSet = value;
                HotspotEditor.RectangleSet = value;

                RaisePropertyChanged();
                RaisePropertyChanged(nameof(HasSelectedHotspotRectangleSet));
            }
        }

        public ObservableCollection<HotspotRectangleSetVM> HotspotRectangleSets { get; } = new();

        private bool _isTexturePanelVisible = true;
        public bool IsTexturePanelVisible
        {
            get => _isTexturePanelVisible;
            set { _isTexturePanelVisible = value; RaisePropertyChanged(); }
        }

        private bool _isHotspotRectanglePanelVisible = true;
        public bool IsHotspotRectanglePanelVisible
        {
            get => _isHotspotRectanglePanelVisible;
            set { _isHotspotRectanglePanelVisible = value; RaisePropertyChanged(); }
        }


        // Derived properties:
        public string WadFilePath => WadFile.FilePath;

        public bool HasSelectedHotspotRectangleSet => SelectedHotspotRectangleSet != null;

        public bool IsUndoAvailable => UndoSystem.IsUndoAvailable;

        public bool IsRedoAvailable => UndoSystem.IsRedoAvailable;

        public override bool IsModified
        {
            get
            {
                return base.IsModified ||
                    Textures.Any(textureVM => textureVM.IsModified) ||
                    TextureSelection.IsModified ||
                    HotspotRectangleSets.Skip(1).Any(rectangleSetVM => rectangleSetVM.IsModified) ||
                    RectangleSelection.IsModified ||
                    HotspotEditor.IsModified;
            }
        }


        // Read-only:
        public string HotspotFilePath { get; }

        public TextureInfoVM[] Textures { get; }

        public TextureSelectionVM TextureSelection { get; }

        public HotspotRectangleSelectionVM RectangleSelection { get; }

        public HotspotEditorVM HotspotEditor { get; }

        public HotspotRectangleSetVM NoHotspotRectangleSet { get; }


        // Internal state:
        private WadFile WadFile { get; }


        public HotspotProjectVM(WadFile wadFile, HotspotFileData hotspotFileData, string hotspotFilePath, Settings settings, IClipboard? clipboard)
            : base(new UndoSystem())
        {
            HotspotRectangleSets.CollectionChanged += HotspotRectangleSets_CollectionChanged;


            WadFile = wadFile;
            HotspotFilePath = hotspotFilePath;

            TextureSelection = new TextureSelectionVM(UndoSystem);
            TextureSelection.SelectionChanged += TextureSelection_SelectionChanged;
            TextureSelection.PropertyChanged += TextureSelection_PropertyChanged;

            RectangleSelection = new HotspotRectangleSelectionVM(UndoSystem);
            RectangleSelection.PropertyChanged += RectangleSelection_PropertyChanged;

            HotspotEditor = new HotspotEditorVM(UndoSystem, settings, RectangleSelection, clipboard);
            HotspotEditor.PropertyChanged += HotspotEditor_PropertyChanged;


            NoHotspotRectangleSet = new HotspotRectangleSetVM("", UndoSystem);
            HotspotRectangleSets.Add(NoHotspotRectangleSet);
            foreach (var rectangleSet in hotspotFileData.RectangleSets)
                HotspotRectangleSets.Add(new HotspotRectangleSetVM(rectangleSet, UndoSystem));

            var bindingLookup = hotspotFileData.Bindings.ToDictionary(binding => binding.TextureNamePattern, binding => binding);
            var hotspotRectangleSetLookup = HotspotRectangleSets
                .Skip(1)
                .ToDictionary(rectangleSetVM => rectangleSetVM.Name, rectangleSetVM => rectangleSetVM);

            Textures = wadFile.TextureInfos
                .Select(textureInfo =>
                {
                    var textureInfoVM = new TextureInfoVM(textureInfo, UndoSystem);
                    textureInfoVM.HotspotRectangleSet = NoHotspotRectangleSet;

                    if (bindingLookup.TryGetValue(textureInfo.Name, out var hotspotBinding))
                    {
                        textureInfoVM.WithoutUndo(() =>
                        {
                            if (hotspotRectangleSetLookup.TryGetValue(hotspotBinding.HotspotName, out var rectangleSetVM))
                                textureInfoVM.HotspotRectangleSet = rectangleSetVM;

                            textureInfoVM.FallbackTextureNamePattern = hotspotBinding.FallbackTextureNamePattern;
                            textureInfoVM.FallbackScoreThreshold = hotspotBinding.FallbackScoreThreshold;
                            textureInfoVM.Labels = hotspotBinding.Labels.ToArray();
                        });
                    }

                    textureInfoVM.PropertyChanged += TextureInfoVM_PropertyChanged;
                    return textureInfoVM;
                })
                .ToArray();

            UndoSystem.OnActionDone += UndoSystem_OnActionDone;
            UndoSystem.OnActionUndone += UndoSystem_OnActionUndone;
            UndoSystem.OnActionRedone += UndoSystem_OnActionRedone;
        }

        public HotspotFileData CreateHotspotFileData()
        {
            var rectangleSets = HotspotRectangleSets
                .Skip(1)
                .Select(rectangleSetVM => rectangleSetVM.CreateHotspotRectangleSet())
                .ToArray();

            var bindings = Textures
                .Select(textureInfoVM => textureInfoVM.CreateHotspotBinding())
                .WhereNotNull()
                .ToArray();

            return new HotspotFileData(rectangleSets, bindings);
        }

        public override void MarkAsUnmodified()
        {
            base.MarkAsUnmodified();

            foreach (var textureInfoVM in Textures)
                textureInfoVM.MarkAsUnmodified();

            foreach (var rectangleSetVM in HotspotRectangleSets.Skip(1))
                rectangleSetVM.MarkAsUnmodified();

            RectangleSelection.MarkAsUnmodified();
            HotspotEditor.MarkAsUnmodified();
        }


        // Commands:
        public async Task CreateNewHotspotRectangleSet()
        {
            var initialName = $"rectangle_set_#{HotspotRectangleSets.Count}";
            var newName = await MessageBox.ShowTextBox(
                "Create new rectangle set",
                "Enter a name for the new rectangle set:",
                initialName,
                name =>
                {
                    if (string.IsNullOrEmpty(name))
                        return "Name must not be empty.";

                    if (HotspotRectangleSets.Any(rectangleSet => string.Equals(rectangleSet.Name, name, StringComparison.InvariantCultureIgnoreCase)))
                        return "A rectangle set with that name already exists.";

                    return null;
                });
            if (string.IsNullOrEmpty(newName))
                return;


            var newRectangleSet = new HotspotRectangleSetVM(newName, UndoSystem);
            var selectedTextures = TextureSelection.Textures.ToArray();
            var previousRectangleSets = selectedTextures.Select(textureVM => textureVM.HotspotRectangleSet).ToArray();

            PerformUndoableAction(
                () =>
                {
                    HotspotRectangleSets.Add(newRectangleSet);

                    foreach (var textureVM in selectedTextures)
                        textureVM.HotspotRectangleSet = newRectangleSet;
                },
                () =>
                {
                    for (int i = 0; i < selectedTextures.Length; i++)
                        selectedTextures[i].HotspotRectangleSet = previousRectangleSets[i];

                    HotspotRectangleSets.Remove(newRectangleSet);
                });
        }

        public async Task RenameHotspotRectangleSet()
        {
            var selectedRectangleSet = SelectedHotspotRectangleSet;
            if (selectedRectangleSet == null)
                return;


            var oldName = selectedRectangleSet.Name;
            var newName = await MessageBox.ShowTextBox(
                "Rename rectangle set",
                "Enter a new name for the rectangle set:",
                selectedRectangleSet.Name,
                name =>
                {
                    if (string.IsNullOrEmpty(name))
                        return "Name must not be empty.";

                    if (HotspotRectangleSets.Any(rectangleSet => rectangleSet != selectedRectangleSet && string.Equals(rectangleSet.Name, name, StringComparison.InvariantCultureIgnoreCase)))
                        return "A rectangle set with this name already exists.";

                    return null;
                });
            if (string.IsNullOrEmpty(newName))
                return;


            PerformUndoableAction(
                () => selectedRectangleSet.WithoutUndo(() => selectedRectangleSet.Name = newName),
                () => selectedRectangleSet.WithoutUndo(() => selectedRectangleSet.Name = oldName));
        }

        public async Task DeleteHotspotRectangleSet()
        {
            var selectedRectangleSet = SelectedHotspotRectangleSet;
            if (selectedRectangleSet == null)
                return;


            var affectedTextures = Textures.Where(texture => texture.HotspotRectangleSet == selectedRectangleSet).ToArray();
            var result = await MessageBox.Show(
                "Delete rectangle set",
                $"Rectangle set '{selectedRectangleSet.Name}' is used by {affectedTextures.Length} textures. Are you sure you want to delete it?",
                MessageBoxButtons.YesNo);
            if (result != true)
                return;


            var rectangleSetIndex = HotspotRectangleSets.IndexOf(selectedRectangleSet);

            PerformUndoableAction(
                () =>
                {
                    foreach (var textureVM in affectedTextures)
                        textureVM.HotspotRectangleSet = NoHotspotRectangleSet;

                    HotspotRectangleSets.Remove(selectedRectangleSet);
                },
                () =>
                {
                    HotspotRectangleSets.Insert(rectangleSetIndex, selectedRectangleSet);

                    foreach (var textureVM in affectedTextures)
                        textureVM.HotspotRectangleSet = selectedRectangleSet;
                });
        }

        public void UndoLastAction()
            => UndoSystem.UndoLastAction();

        public void RedoLastAction()
            => UndoSystem.RedoLastAction();

        public void ToggleTexturePanel()
        {
            IsTexturePanelVisible = !IsTexturePanelVisible;
        }

        public void ToggleHotspotRectanglePanel()
        {
            IsHotspotRectanglePanelVisible = !IsHotspotRectanglePanelVisible;
        }

        public void FocusHotspotRectangleLabels()
            => RaiseHotspotRectangleLabelsFocusRequested();

        public async Task AddLabelToHotspotRectangles()
        {
            if (RectangleSelection.IsEmpty)
                return;


            var newLabel = await MessageBox.ShowTextBox(
                "Add label to rectangle(s)",
                "Enter the new label:",
                "label");
            if (string.IsNullOrEmpty(newLabel))
                return;

            var affectedRectangles = RectangleSelection.Rectangles
                .Where(rectangleVM => !rectangleVM.Labels.Contains(newLabel, StringComparer.InvariantCultureIgnoreCase))
                .ToArray();
            if (!affectedRectangles.Any())
                return;

            var newLabels = affectedRectangles
                .Select(rectangleVM => rectangleVM.Labels.Append(newLabel).ToArray())
                .ToArray();
            var oldLabels = affectedRectangles
                .Select(rectangleVM => rectangleVM.Labels)
                .ToArray();

            PerformUndoableAction(
                () =>
                {
                    for (int i = 0; i < affectedRectangles.Length; i++)
                        affectedRectangles[i].Labels = newLabels[i];
                },
                () =>
                {
                    for (int i = 0; i < affectedRectangles.Length; i++)
                        affectedRectangles[i].Labels = oldLabels[i];
                });
        }

        public async Task RenameLabelInHotspotRectangles()
        {
            if (RectangleSelection.IsEmpty || !RectangleSelection.HasLabels)
                return;


            var availableLabels = RectangleSelection.Rectangles
                .SelectMany(rectangleVM => rectangleVM.Labels)
                .Distinct(StringComparer.InvariantCultureIgnoreCase)
                .OrderBy(label => label)
                .ToArray();
            if (!availableLabels.Any())
                return;

            (var confirmed, var index, var newLabel) = await RenameLabelWindow.Show("Rename label in rectangle(s)", availableLabels);
            if (confirmed != true)
                return;

            var oldLabel = availableLabels[index];
            if (string.Equals(oldLabel, newLabel, StringComparison.InvariantCultureIgnoreCase))
                return;

            var affectedRectangles = RectangleSelection.Rectangles
                .Where(rectangleVM => rectangleVM.Labels.Contains(oldLabel, StringComparer.InvariantCultureIgnoreCase))
                .ToArray();

            var newLabels = affectedRectangles
                .Select(rectangleVM =>
                {
                    if (rectangleVM.Labels.Contains(newLabel))
                        return rectangleVM.Labels.Except([oldLabel], StringComparer.InvariantCultureIgnoreCase).ToArray();
                    else
                        return rectangleVM.Labels.Select(label => string.Equals(label, oldLabel, StringComparison.InvariantCultureIgnoreCase) ? newLabel : label).ToArray();
                })
                .ToArray();
            var oldLabels = affectedRectangles
                .Select(rectangleVM => rectangleVM.Labels)
                .ToArray();

            PerformUndoableAction(
                () =>
                {
                    for (int i = 0; i < affectedRectangles.Length; i++)
                        affectedRectangles[i].Labels = newLabels[i];
                },
                () =>
                {
                    for (int i = 0; i < affectedRectangles.Length; i++)
                        affectedRectangles[i].Labels = oldLabels[i];
                });
        }

        public async Task RemoveLabelFromHotspotRectangles()
        {
            if (RectangleSelection.IsEmpty || !RectangleSelection.HasLabels)
                return;


            var availableLabels = RectangleSelection.Rectangles
                .SelectMany(rectangleVM => rectangleVM.Labels)
                .Distinct(StringComparer.InvariantCultureIgnoreCase)
                .OrderBy(label => label)
                .ToArray();
            if (!availableLabels.Any())
                return;

            var removeLabelIndex = await MessageBox.ShowComboBox(
                "Remove label from rectangle(s)",
                "Select the label that will be removed:",
                availableLabels);
            if (removeLabelIndex == null)
                return;

            var removeLabel = availableLabels[removeLabelIndex.Value];
            var affectedRectangles = RectangleSelection.Rectangles
                .Where(rectangleVM => rectangleVM.Labels.Contains(removeLabel, StringComparer.InvariantCultureIgnoreCase))
                .ToArray();
            if (!affectedRectangles.Any())
                return;

            var newLabels = affectedRectangles
                .Select(rectangleVM => rectangleVM.Labels.Except([removeLabel], StringComparer.InvariantCultureIgnoreCase).ToArray())
                .ToArray();
            var oldLabels = affectedRectangles
                .Select(rectangleVM => rectangleVM.Labels)
                .ToArray();

            PerformUndoableAction(
                () =>
                {
                    for (int i = 0; i < affectedRectangles.Length; i++)
                        affectedRectangles[i].Labels = newLabels[i];
                },
                () =>
                {
                    for (int i = 0; i < affectedRectangles.Length; i++)
                        affectedRectangles[i].Labels = oldLabels[i];
                });
        }

        public async Task AddLabelToTextures()
        {
            if (TextureSelection.IsEmpty)
                return;


            var newLabel = await MessageBox.ShowTextBox(
                "Add label to texture(s)",
                "Enter the new label:",
                "label");
            if (string.IsNullOrEmpty(newLabel))
                return;

            var affectedTextures = TextureSelection.Textures
                .Where(textureVM => !textureVM.Labels.Contains(newLabel, StringComparer.InvariantCultureIgnoreCase))
                .ToArray();
            if (!affectedTextures.Any())
                return;

            var newLabels = affectedTextures
                .Select(textureVM => textureVM.Labels.Append(newLabel).ToArray())
                .ToArray();
            var oldLabels = affectedTextures
                .Select(textureVM => textureVM.Labels)
                .ToArray();

            PerformUndoableAction(
                () =>
                {
                    for (int i = 0; i < affectedTextures.Length; i++)
                        affectedTextures[i].Labels = newLabels[i];
                },
                () =>
                {
                    for (int i = 0; i < affectedTextures.Length; i++)
                        affectedTextures[i].Labels = oldLabels[i];
                });
        }

        public async Task RenameLabelInTextures()
        {
            if (TextureSelection.IsEmpty || !TextureSelection.HasLabels)
                return;


            var availableLabels = TextureSelection.Textures
                .SelectMany(textureVM => textureVM.Labels)
                .Distinct(StringComparer.InvariantCultureIgnoreCase)
                .OrderBy(label => label)
                .ToArray();
            if (!availableLabels.Any())
                return;

            (var confirmed, var index, var newLabel) = await RenameLabelWindow.Show("Rename label in texture(s)", availableLabels);
            if (confirmed != true)
                return;

            var oldLabel = availableLabels[index];
            if (string.Equals(oldLabel, newLabel, StringComparison.InvariantCultureIgnoreCase))
                return;

            var affectedTextures = TextureSelection.Textures
                .Where(textureVM => textureVM.Labels.Contains(oldLabel, StringComparer.InvariantCultureIgnoreCase))
                .ToArray();

            var newLabels = affectedTextures
                .Select(textureVM =>
                {
                    if (textureVM.Labels.Contains(newLabel))
                        return textureVM.Labels.Except([oldLabel], StringComparer.InvariantCultureIgnoreCase).ToArray();
                    else
                        return textureVM.Labels.Select(label => string.Equals(label, oldLabel, StringComparison.InvariantCultureIgnoreCase) ? newLabel : label).ToArray();
                })
                .ToArray();
            var oldLabels = affectedTextures
                .Select(textureVM => textureVM.Labels)
                .ToArray();

            PerformUndoableAction(
                () =>
                {
                    for (int i = 0; i < affectedTextures.Length; i++)
                        affectedTextures[i].Labels = newLabels[i];
                },
                () =>
                {
                    for (int i = 0; i < affectedTextures.Length; i++)
                        affectedTextures[i].Labels = oldLabels[i];
                });
        }

        public async Task RemoveLabelFromTextures()
        {
            if (TextureSelection.IsEmpty || !TextureSelection.HasLabels)
                return;


            var availableLabels = TextureSelection.Textures
                .SelectMany(textureVM => textureVM.Labels)
                .Distinct(StringComparer.InvariantCultureIgnoreCase)
                .OrderBy(label => label)
                .ToArray();
            if (!availableLabels.Any())
                return;

            var removeLabelIndex = await MessageBox.ShowComboBox(
                "Remove label from texture(s)",
                "Select the label that will be removed:",
                availableLabels);
            if (removeLabelIndex == null)
                return;

            var removeLabel = availableLabels[removeLabelIndex.Value];
            var affectedTextures = TextureSelection.Textures
                .Where(textureVM => textureVM.Labels.Contains(removeLabel, StringComparer.InvariantCultureIgnoreCase))
                .ToArray();
            if (!affectedTextures.Any())
                return;

            var newLabels = affectedTextures
                .Select(textureVM => textureVM.Labels.Except([removeLabel], StringComparer.InvariantCultureIgnoreCase).ToArray())
                .ToArray();
            var oldLabels = affectedTextures
                .Select(textureVM => textureVM.Labels)
                .ToArray();

            PerformUndoableAction(
                () =>
                {
                    for (int i = 0; i < affectedTextures.Length; i++)
                        affectedTextures[i].Labels = newLabels[i];
                },
                () =>
                {
                    for (int i = 0; i < affectedTextures.Length; i++)
                        affectedTextures[i].Labels = oldLabels[i];
                });
        }


        private void UndoSystem_OnActionDone()
        {
            RaisePropertyChanged(nameof(IsUndoAvailable));
            RaisePropertyChanged(nameof(IsRedoAvailable));
        }

        private void UndoSystem_OnActionUndone()
        {
            RaisePropertyChanged(nameof(IsUndoAvailable));
            RaisePropertyChanged(nameof(IsRedoAvailable));
        }

        private void UndoSystem_OnActionRedone()
        {
            RaisePropertyChanged(nameof(IsUndoAvailable));
            RaisePropertyChanged(nameof(IsRedoAvailable));
        }

        private void HotspotEditor_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(HotspotEditorVM.IsModified))
                RaisePropertyChanged(nameof(IsModified));
        }

        private void TextureSelection_SelectionChanged(TextureInfoVM[] deselected, TextureInfoVM[] selected)
            => UpdateSelectedHotspotRectangleSet();

        private void TextureSelection_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(TextureSelectionVM.SingleTexture): UpdateTextureDisplay(); break;
                case nameof(TextureSelectionVM.IsModified): RaisePropertyChanged(nameof(IsModified)); break;
            }
        }

        private void TextureInfoVM_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TextureInfoVM.HotspotRectangleSet))
            {
                if (sender is TextureInfoVM textureInfoVM && textureInfoVM == TextureSelection.SingleTexture)
                    UpdateSelectedHotspotRectangleSet();
            }
        }

        private void RectangleSelection_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(HotspotRectangleSelectionVM.IsModified))
                RaisePropertyChanged(nameof(IsModified));
        }

        private void HotspotRectangleSets_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (var rectangleSetVM in e.NewItems.OfType<HotspotRectangleSetVM>())
                    rectangleSetVM.PropertyChanged += RectangleSetVM_PropertyChanged;
            }

            if (e.OldItems != null)
            {
                foreach (var rectangleSetVM in e.OldItems.OfType<HotspotRectangleSetVM>())
                    rectangleSetVM.PropertyChanged -= RectangleSetVM_PropertyChanged;
            }

            RaisePropertyChanged(nameof(IsModified));
        }

        private void RectangleSetVM_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(HotspotRectangleSetVM.IsModified))
                RaisePropertyChanged(nameof(IsModified));
        }


        private void UpdateSelectedHotspotRectangleSet()
        {
            var hotspotRectangleSet = TextureSelection.SingleTexture?.HotspotRectangleSet;
            SelectedHotspotRectangleSet = (hotspotRectangleSet == NoHotspotRectangleSet) ? null : hotspotRectangleSet;
        }

        private void UpdateTextureDisplay()
        {
            var singleTexture = TextureSelection.SingleTexture;
            if (singleTexture != null)
            {
                // TODO: Error handling -- show a 'failed to load texture' message if loading fails!
                var texture = WadFile.LoadTexture(singleTexture.TextureInfo);
                SelectedTextureImage = CreateBitmapFromTexture(texture);
            }
            else
            {
                SelectedTextureImage = null;
            }
        }

        private Bitmap CreateBitmapFromTexture(Texture texture)
        {
            var bitmap = new WriteableBitmap(new PixelSize(texture.Width, texture.Height), new Vector(96, 96), PixelFormat.Rgba8888, AlphaFormat.Unpremul);
            using (var buffer = bitmap.Lock())
            {
                var isTransparent = texture.Name.StartsWith('{');

                for (int y = 0; y < texture.Height; y++)
                {
                    var row = new byte[buffer.RowBytes];
                    for (int x = 0; x < texture.Width; x++)
                    {
                        var index = texture.ImageData[y * texture.Width + x];
                        var color = texture.Palette[index];
                        if (!(index == 255 && isTransparent))
                        {
                            row[x * 4] = color.R;
                            row[x * 4 + 1] = color.G;
                            row[x * 4 + 2] = color.B;
                            row[x * 4 + 3] = 255;
                        }
                    }
                    Marshal.Copy(row, 0, buffer.Address + y * buffer.RowBytes, buffer.RowBytes);
                }
            }
            return bitmap;
        }
    }
}
