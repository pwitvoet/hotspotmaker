using Avalonia;
using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using HotspotMaker.Controls;
using HotspotMaker.Editor;
using HotspotMaker.History;
using MLib.Texturing;
using MLib.Texturing.Hotspotting;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HotspotMaker.Hotspot
{
    public class HotspotProjectVM : ChangeTrackingVM
    {
        // TODO: Improve error reporting!
        public static HotspotProjectVM Load(string wadFilePath, string hotspotFilePath, IClipboard? clipboard)
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

            return new HotspotProjectVM(wadFile, hotspotFileData, hotspotFilePath, clipboard);
        }


        // Bindable properties:
        private TextureInfoVM? _selectedTextureInfo;
        public TextureInfoVM? SelectedTextureInfo
        {
            get => _selectedTextureInfo;
            set
            {
                _selectedTextureInfo = value;

                RaisePropertyChanged();
                RaisePropertyChanged(nameof(HasSelectedTextureInfo));
                RaisePropertyChanged(nameof(HasSelectedTextureWithoutHotspot));

                OnSelectedTextureUpdate(value);
            }
        }

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

        private HotspotBindingVM? _selectedHotspotBinding;
        public HotspotBindingVM? SelectedHotspotBinding
        {
            get => _selectedHotspotBinding;
            set
            {
                _selectedHotspotBinding = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(HasSelectedTextureWithoutHotspot));
                RaisePropertyChanged(nameof(HasSelectedHotspotBinding));
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

        public ObservableCollection<HotspotBindingVM> HotspotBindings { get; } = new();

        public ObservableCollection<HotspotRectangleSetVM> HotspotRectangleSets { get; } = new();


        // Derived properties:
        public string WadFilePath => WadFile.FilePath;

        public TextureInfoVM[] TextureInfos { get; }

        public bool HasSelectedTextureInfo => SelectedTextureInfo != null;

        public bool HasSelectedTextureWithoutHotspot => SelectedTextureInfo != null && SelectedHotspotBinding == null;

        public bool HasSelectedHotspotBinding => SelectedHotspotBinding != null;

        public bool HasSelectedHotspotRectangleSet => SelectedHotspotRectangleSet != null;

        public bool IsUndoAvailable => UndoSystem.IsUndoAvailable;

        public bool IsRedoAvailable => UndoSystem.IsRedoAvailable;

        public override bool IsModified
            => base.IsModified || HotspotBindings.Any(bindingVM => bindingVM.IsModified) || HotspotRectangleSets.Any(rectangleSetVM => rectangleSetVM.IsModified) || Selection.IsModified || HotspotEditor.IsModified;


        // Read-only:
        public string HotspotFilePath { get; }

        public HotspotRectangleSelectionVM Selection { get; }
        public HotspotEditorVM HotspotEditor { get; }


        // Internal state:
        private WadFile WadFile { get; }
        private Dictionary<string, HotspotBindingVM> ExactBindings { get; } = new Dictionary<string, HotspotBindingVM>(StringComparer.InvariantCultureIgnoreCase);
        private List<(Regex, HotspotBindingVM)> WildcardHotspotBindings { get; } = new();


        public HotspotProjectVM(WadFile wadFile, HotspotFileData hotspotFileData, string hotspotFilePath, IClipboard? clipboard)
            : base(new UndoSystem())
        {
            HotspotBindings.CollectionChanged += HotspotBindings_CollectionChanged;
            HotspotRectangleSets.CollectionChanged += HotspotRectangleSets_CollectionChanged;


            WadFile = wadFile;
            HotspotFilePath = hotspotFilePath;

            Selection = new HotspotRectangleSelectionVM(UndoSystem);
            Selection.PropertyChanged += Selection_PropertyChanged;

            HotspotEditor = new HotspotEditorVM(UndoSystem, Selection, clipboard);
            HotspotEditor.PropertyChanged += HotspotEditor_PropertyChanged;

            foreach (var rectangleSet in hotspotFileData.RectangleSets)
                HotspotRectangleSets.Add(new HotspotRectangleSetVM(rectangleSet, UndoSystem));

            foreach (var binding in hotspotFileData.Bindings)
            {
                var hotspotRectangleSetVM = GetHotspotRectangleSet(binding.HotspotName);
                var bindingVM = new HotspotBindingVM(binding, hotspotRectangleSetVM, UndoSystem);
                HotspotBindings.Add(bindingVM);

                // Register binding lookup:
                var regex = HotspotDataCollection.GetTextureNamePatternRegex(bindingVM.TextureNamePattern);
                if (regex == null)
                {
                    // TODO: Handle duplicate names!
                    ExactBindings[bindingVM.TextureNamePattern] = bindingVM;
                }
                else
                {
                    WildcardHotspotBindings.Add((regex, bindingVM));
                }
            }

            // Initialize texture infos:
            TextureInfos = wadFile.TextureInfos
                .Select(textureInfo => new TextureInfoVM(textureInfo, GetBindingForTexture(textureInfo.Name)))
                .OrderBy(entry => entry.Name)
                .ToArray();

            foreach (var textureInfo in TextureInfos)
                textureInfo.PropertyChanged += TextureInfo_PropertyChanged;

            UndoSystem.OnActionDone += UndoSystem_OnActionDone;
            UndoSystem.OnActionUndone += UndoSystem_OnActionUndone;
            UndoSystem.OnActionRedone += UndoSystem_OnActionRedone;
        }

        public HotspotFileData CreateHotspotFileData()
        {
            var rectangleSets = HotspotRectangleSets
                .Select(rectangleSetVM => rectangleSetVM.CreateHotspotRectangleSet())
                .ToArray();

            var bindings = HotspotBindings
                .Select(bindingVM => bindingVM.CreateHotspotBinding())
                .ToArray();

            return new HotspotFileData(rectangleSets, bindings);
        }

        public override void MarkAsUnmodified()
        {
            base.MarkAsUnmodified();

            foreach (var bindingVM in HotspotBindings)
                bindingVM.MarkAsUnmodified();

            foreach (var rectangleSetVM in HotspotRectangleSets)
                rectangleSetVM.MarkAsUnmodified();

            Selection.MarkAsUnmodified();
            HotspotEditor.MarkAsUnmodified();
        }


        // Commands:
        public void LinkToNewHotspotSet()
        {
            var textureInfo = SelectedTextureInfo;
            if (textureInfo == null)
                return;


            var hotspotSetName = $"{textureInfo.Name}_hotspots";
            var counter = 1;
            while (HotspotRectangleSets.Any(rectangleSet => string.Equals(hotspotSetName, rectangleSet.Name, StringComparison.InvariantCultureIgnoreCase)))
                hotspotSetName = $"{textureInfo.Name}_hotspots_{counter++}";

            var newHotspotRectangleSet = new HotspotRectangleSetVM(hotspotSetName, UndoSystem);
            var newHotspotBinding = new HotspotBindingVM(textureInfo.Name, newHotspotRectangleSet, UndoSystem);

            var oldHotspotBinding = textureInfo.Binding;
            var oldHotspotRectangleSet = oldHotspotBinding?.HotspotRectangleSet;

            PerformUndoableAction(
                () =>
                {
                    HotspotRectangleSets.Add(newHotspotRectangleSet);
                    HotspotBindings.Add(newHotspotBinding);
                    textureInfo.Binding = newHotspotBinding;
                },
                () =>
                {
                    textureInfo.Binding = oldHotspotBinding;
                    HotspotBindings.Remove(newHotspotBinding);
                    HotspotRectangleSets.Remove(newHotspotRectangleSet);
                });
        }

        public async Task LinkToExistingHotspotSet()
        {
            var textureInfo = SelectedTextureInfo;
            if (textureInfo == null)
                return;


            var result = await MessageBox.ShowComboBox(
                "Link to existing rectangle set",
                "Link this texture to the following rectangle set:",
                HotspotRectangleSets.Select(rectangleSet => rectangleSet.Name).ToArray());
            if (result == null)
                return;

            var hotspotRectangleSet = HotspotRectangleSets[result.Value];
            var newHotspotBinding = new HotspotBindingVM(textureInfo.Name, hotspotRectangleSet, UndoSystem);

            var oldHotspotBinding = textureInfo.Binding;
            var oldHotspotRectangleSet = oldHotspotBinding?.HotspotRectangleSet;

            PerformUndoableAction(
                () =>
                {
                    HotspotBindings.Add(newHotspotBinding);
                    textureInfo.Binding = newHotspotBinding;
                },
                () =>
                {
                    textureInfo.Binding = oldHotspotBinding;
                    HotspotBindings.Remove(newHotspotBinding);
                });
        }

        public async Task UnlinkTextureFromHotspotSet()
        {
            var textureInfo = SelectedTextureInfo;
            var oldHotspotBinding = textureInfo?.Binding;
            if (textureInfo == null || oldHotspotBinding == null)
                return;


            var oldHotspotRectangleSet = oldHotspotBinding.HotspotRectangleSet;
            var useCount = HotspotBindings.Where(binding => binding.HotspotRectangleSet == oldHotspotRectangleSet).Count();
            var removeHotspotRectangleSet = false;
            if (useCount == 1)
            {
                var result = await MessageBox.Show(
                    "Unlink texture from rectangle set",
                    "After unlinking, there are no other textures that use this rectangle set. Do you want to remove the rectangle set?",
                    [
                        "Unlink only",
                        "Unlink and remove rectangle set",
                        "Cancel",
                    ]);

                if (result != 0 && result != 1)
                    return;

                removeHotspotRectangleSet = result == 1;
            }

            var oldBindingIndex = HotspotBindings.IndexOf(oldHotspotBinding);
            var oldRectangleSetIndex = oldHotspotRectangleSet == null ? 0 : HotspotRectangleSets.IndexOf(oldHotspotRectangleSet);

            PerformUndoableAction(
                () =>
                {
                    textureInfo.Binding = null;
                    HotspotBindings.Remove(oldHotspotBinding);

                    if (removeHotspotRectangleSet && oldHotspotRectangleSet != null)
                        HotspotRectangleSets.Remove(oldHotspotRectangleSet);
                },
                () =>
                {
                    if (removeHotspotRectangleSet && oldHotspotRectangleSet != null)
                        HotspotRectangleSets.Insert(oldRectangleSetIndex, oldHotspotRectangleSet);

                    HotspotBindings.Insert(oldBindingIndex, oldHotspotBinding);
                    textureInfo.Binding = oldHotspotBinding;
                });
        }

        public void UndoLastAction()
            => UndoSystem.UndoLastAction();

        public void RedoLastAction()
            => UndoSystem.RedoLastAction();


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

        private void Selection_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(HotspotRectangleSelectionVM.IsModified))
                RaisePropertyChanged(nameof(IsModified));
        }

        private void HotspotBindings_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (var bindingVM in e.NewItems.OfType<HotspotBindingVM>())
                    bindingVM.PropertyChanged += BindingVM_PropertyChanged;
            }

            if (e.OldItems != null)
            {
                foreach (var bindingVM in e.OldItems.OfType<HotspotBindingVM>())
                    bindingVM.PropertyChanged -= BindingVM_PropertyChanged;
            }

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

        private void TextureInfo_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TextureInfoVM.Binding))
            {
                if (sender == SelectedTextureInfo)
                {
                    SelectedHotspotBinding = SelectedTextureInfo?.Binding;
                    SelectedHotspotRectangleSet = SelectedHotspotBinding?.HotspotRectangleSet;
                }
            }
        }

        private void BindingVM_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(HotspotBindingVM.IsModified))
            {
                RaisePropertyChanged(nameof(IsModified));
            }
            else if (e.PropertyName == nameof(HotspotBindingVM.HotspotRectangleSet))
            {
                if (sender == SelectedHotspotBinding)
                    SelectedHotspotRectangleSet = SelectedHotspotBinding?.HotspotRectangleSet;
            }
        }

        private void RectangleSetVM_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(HotspotRectangleSetVM.IsModified))
                RaisePropertyChanged(nameof(IsModified));
        }


        private void OnSelectedTextureUpdate(TextureInfoVM? textureItem)
        {
            if (textureItem == null)
            {
                SelectedTextureImage = null;
                SelectedHotspotBinding = null;
                SelectedHotspotRectangleSet = null;
            }
            else
            {
                var texture = WadFile.LoadTexture(textureItem.TextureInfo);
                SelectedTextureImage = CreateBitmapFromTexture(texture);
                SelectedHotspotBinding = textureItem.Binding;
                SelectedHotspotRectangleSet = textureItem.Binding?.HotspotRectangleSet;
            }
        }

        private HotspotRectangleSetVM? GetHotspotRectangleSet(string? hotspotName)
            => hotspotName != null ? HotspotRectangleSets.FirstOrDefault(rectangleSet => string.Equals(rectangleSet.Name, hotspotName, StringComparison.InvariantCultureIgnoreCase)) : null;

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

        private HotspotBindingVM? GetBindingForTexture(string textureName)
        {
            if (ExactBindings.TryGetValue(textureName, out var exactBinding))
                return exactBinding;

            foreach ((var regex, var binding) in WildcardHotspotBindings)
            {
                if (regex.IsMatch(textureName))
                    return binding;
            }

            return null;
        }
    }
}
