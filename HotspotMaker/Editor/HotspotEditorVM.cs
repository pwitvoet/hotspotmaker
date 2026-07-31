using Avalonia;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Media;
using HotspotMaker.Configuration;
using HotspotMaker.History;
using HotspotMaker.Hotspot;
using HotspotMaker.Presets;
using MLib.Mathematics.Spatial;
using MLib.Texturing.Hotspotting;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace HotspotMaker.Editor
{
    public class HotspotEditorVM : ChangeTrackingVM
    {
        public event Action? RectanglesChanged;
        protected void RaiseRectanglesChanged()
            => RectanglesChanged?.Invoke();

        public event Action<HotspotRectangleVM, string?>? RectanglePropertyChanged;
        protected void RaiseRectanglePropertyChanged(HotspotRectangleVM sender, string? propertyName)
            => RectanglePropertyChanged?.Invoke(sender, propertyName);


        // Bindable properties:
        private IImage? _textureImage;
        public IImage? TextureImage
        {
            get => _textureImage;
            set { _textureImage = value; RaisePropertyChanged(); }
        }

        private HotspotRectangleSetVM? _rectangleSet;
        public HotspotRectangleSetVM? RectangleSet
        {
            get => _rectangleSet;
            set
            {
                if (_rectangleSet != null)
                {
                    foreach (var rectangleVM in _rectangleSet.Rectangles)
                        rectangleVM.PropertyChanged -= Rectangle_PropertyChanged;

                    _rectangleSet.Rectangles.CollectionChanged -= Rectangles_CollectionChanged;
                }

                if (_rectangleSet != null)
                    Selection.Clear();

                _rectangleSet = value;

                if (_rectangleSet != null)
                {
                    foreach (var rectangleVM in _rectangleSet.Rectangles)
                        rectangleVM.PropertyChanged += Rectangle_PropertyChanged;

                    _rectangleSet.Rectangles.CollectionChanged += Rectangles_CollectionChanged;
                }

                RaisePropertyChanged();
                RaiseRectanglesChanged();
            }
        }

        public HotspotRectangleSelectionVM Selection { get; }

        private bool _isGridEnabled = true;
        public bool IsGridEnabled
        {
            get => _isGridEnabled;
            set { _isGridEnabled = value; RaisePropertyChanged(); }
        }

        private double _gridSize = 16;
        public double GridSize
        {
            get => _gridSize;
            set { _gridSize = value; RaisePropertyChanged(); }
        }

        private bool _isCoordinatesVisible = true;
        public bool IsCoordinatesVisible
        {
            get => _isCoordinatesVisible;
            set { _isCoordinatesVisible = value; RaisePropertyChanged(); }
        }

        private bool _isIconsVisible = true;
        public bool IsIconsVisible
        {
            get => _isIconsVisible;
            set { _isIconsVisible = value; RaisePropertyChanged(); }
        }


        // Internal state:
        private Point CurrentOperationStartCoordinate { get; set; }
        private HotspotRectangleVM[] CurrentOperationRectangles { get; set; } = Array.Empty<HotspotRectangleVM>();
        private Point[] CurrentOperationOriginalPositions { get; set; } = Array.Empty<Point>();
        private ResizeDirection CurrentOperationResizeDirection { get; set; } = ResizeDirection.None;
        private Rect[] CurrentOperationOriginalSizes { get; set; } = Array.Empty<Rect>();
        private Rect CurrentOperationOriginalSelectionBounds { get; set; }

        private Settings Settings { get; }
        private IClipboard? Clipboard { get; }


        public HotspotEditorVM(UndoSystem undoSystem, Settings settings, HotspotRectangleSelectionVM selection, IClipboard? clipboard)
            : base(undoSystem)
        {
            Selection = selection;
            Selection.SelectionChanged += Selection_SelectionChanged;

            Settings = settings;
            Clipboard = clipboard;
        }

        public HotspotRectangleVM[] GetRectanglesAtPoint(Point point)
            => RectangleSet?.Rectangles.Where(rectangleVM => IsTouching(rectangleVM, point)).ToArray() ?? [];

        public HotspotRectangleVM[] GetRectanglesInArea(Rect rect)
            => RectangleSet?.Rectangles.Where(rectangleVM => IsTouching(rectangleVM, rect)).ToArray() ?? [];


        public void SetSelection(HotspotRectangleVM rectangle)
        {
            Selection.Clear();
            Selection.Add(rectangle);
        }

        public void SetSelection(IEnumerable<HotspotRectangleVM> rectangles)
        {
            // Materialize immediately - the caller is likely to use the current selection as basis, and clearing it before enumerating would cause trouble:
            var rectanglesArray = rectangles.ToArray();

            Selection.Clear();
            Selection.Add(rectanglesArray);
        }

        public void ClearSelection()
        {
            Selection.Clear();
        }


        public async Task CopySelectionToClipboard(bool deleteSelection = false)
        {
            if (Clipboard == null)
                return;


            var rectangles = Selection.Rectangles
                .Select(rectangleVM => rectangleVM.CreateHotspotRectangle())
                .ToArray();
            var json = HotspotFileWriter.Serialize(rectangles);
            await Clipboard.SetTextAsync(json);

            if (deleteSelection)
                DeleteSelectedRectangles();
        }

        public async Task<PasteResult> PasteFromClipboard()
        {
            var rectangleSet = RectangleSet;
            if (rectangleSet == null)
                return PasteResult.NoTargetRectangleSet;

            if (Clipboard == null)
                return PasteResult.ClipboardNotAvailable;


            var json = await Clipboard.TryGetTextAsync();
            if (json == null)
                return PasteResult.ClipboardEmpty;

            HotspotRectangle[]? rectangles = null;
            try
            {
                rectangles = HotspotFileParser.DeserializeHotspotRectangles(json);
            }
            catch
            {
                return PasteResult.ClipboardInvalidData;
            }

            // Create VM instances, and apply an offset to make the pasted rectangles more 'visible' when pasting immediately after copying:
            var rectangleVMs = rectangles
                .Select(rectangle => new HotspotRectangleVM(rectangle, UndoSystem))
                .ToArray();
            foreach (var rectangleVM in rectangleVMs)
            {
                rectangleVM.X += 32;
                rectangleVM.Y += 32;
            }

            PerformUndoableAction(
                () =>
                {
                    foreach (var rectangleVM in rectangleVMs)
                        rectangleSet.Rectangles.Add(rectangleVM);
                },
                () =>
                {
                    foreach (var rectangleVM in rectangleVMs)
                        rectangleSet.Rectangles.Remove(rectangleVM);
                });

            // Select the newly pasted rectangles:
            Selection.Clear();
            Selection.Add(rectangleVMs);

            return PasteResult.Success;
        }


        public bool HandleKeyBinding(KeyGesture keyGesture)
        {
            var keyBinding = Settings.GetKeyBinding(keyGesture);
            if (keyBinding == null)
                return false;


            switch (keyBinding.EditorAction)
            {
                case EditorAction.ToggleGrid: ToggleGrid(); break;
                case EditorAction.IncreaseGridSize: IncreaseGridSize(); break;
                case EditorAction.DecreaseGridSize: DecreaseGridSize(); break;
                case EditorAction.Cut: _ = CopySelectionToClipboard(deleteSelection: true); break;
                case EditorAction.Copy: _ = CopySelectionToClipboard(); break;
                case EditorAction.Paste: _ = PasteFromClipboard(); break;
                case EditorAction.SelectAll: SelectAllRectangles(); break;
                case EditorAction.Delete: DeleteSelectedRectangles(); break;
                case EditorAction.MoveUp: MoveSelectedRectangles(new Vector(0, -(IsGridEnabled ? GridSize : 1))); break;
                case EditorAction.MoveRight: MoveSelectedRectangles(new Vector((IsGridEnabled ? GridSize : 1), 0)); break;
                case EditorAction.MoveDown: MoveSelectedRectangles(new Vector(0, (IsGridEnabled ? GridSize : 1))); break;
                case EditorAction.MoveLeft: MoveSelectedRectangles(new Vector(-(IsGridEnabled ? GridSize : 1), 0)); break;
                case EditorAction.ApplyPreset: ApplyPresetToSelectedRectangles(keyBinding.Preset); break;
                default: return false;
            }
            return true;
        }

        public void ToggleGrid() => IsGridEnabled = !IsGridEnabled;

        public void IncreaseGridSize()
        {
            if (GridSize < 1024)
                GridSize *= 2;
        }

        public void DecreaseGridSize()
        {
            if (GridSize > 1)
                GridSize /= 2;
        }

        public void ToggleCoordinatesDisplay() => IsCoordinatesVisible = !IsCoordinatesVisible;

        public void ToggleIconsDisplay() => IsIconsVisible = !IsIconsVisible;


        public void StartDuplicateRectanglesOperation(Point startTextureCoordinate, double gridSize, bool snapToGrid)
        {
            var rectangleSet = RectangleSet;
            if (rectangleSet == null)
                return;

            var duplicatedRectangles = Selection.Rectangles
                .Select(rectangleVM => new HotspotRectangleVM(rectangleVM.CreateHotspotRectangle(), UndoSystem))
                .ToArray();
            if (!duplicatedRectangles.Any())
                return;

            PerformUndoableActionOngoing(
                "DuplicateRectangles",
                () =>
                {
                    foreach (var rectangleVM in duplicatedRectangles)
                        rectangleSet.Rectangles.Add(rectangleVM);
                },
                () =>
                {
                    foreach (var rectangleVM in duplicatedRectangles)
                        rectangleSet.Rectangles.Remove(rectangleVM);
                });

            CurrentOperationStartCoordinate = startTextureCoordinate;
            CurrentOperationRectangles = duplicatedRectangles;
            CurrentOperationOriginalPositions = duplicatedRectangles
                .Select(rectangleVM => new Point(rectangleVM.X, rectangleVM.Y))
                .ToArray();


            // Also select the new rectangles:
            SetSelection(duplicatedRectangles);
        }

        public void UpdateDuplicateRectanglesOperation(Point currentTextureCoordinate, double gridSize, bool snapToGrid)
        {
            var offset = currentTextureCoordinate - CurrentOperationStartCoordinate;
            if (snapToGrid)
                offset = GetSnappedCoordinate(offset, gridSize, snapToGrid);

            // NOTE: We're updating the selected rectangles in-place, so undoing and redoing this action will restore them with their latest positions.
            for (int i = 0; i < CurrentOperationRectangles.Length; i++)
            {
                var rectangleVM = CurrentOperationRectangles[i];
                var originalPosition = CurrentOperationOriginalPositions[i];

                rectangleVM.SetDimensionsWithoutUndo(originalPosition.X + offset.X, originalPosition.Y + offset.Y, rectangleVM.Width, rectangleVM.Height);
            }
        }

        public void StartMoveRectanglesOperation(Point startTextureCoordinate, double gridSize, bool snapToGrid)
        {
            if (RectangleSet == null)
                return;

            // NOTE: No undoable action yet, because there has been no actual movement yet.

            CurrentOperationStartCoordinate = startTextureCoordinate;
            CurrentOperationRectangles = Selection.Rectangles.ToArray();
            CurrentOperationOriginalPositions = Selection.Rectangles
                .Select(rectangleVM => new Point(rectangleVM.X, rectangleVM.Y))
                .ToArray();
        }

        public void UpdateMoveRectanglesOperation(Point currentTextureCoordinate, double gridSize, bool snapToGrid)
        {
            var offset = GetSnappedCoordinate(currentTextureCoordinate - CurrentOperationStartCoordinate, gridSize, snapToGrid);

            var selectedRectangles = CurrentOperationRectangles;
            var originalPositions = CurrentOperationOriginalPositions;

            PerformUndoableActionOngoing(
                "MoveRectangles",
                () =>
                {
                    for (int i = 0; i < selectedRectangles.Length; i++)
                    {
                        var rectangleVM = selectedRectangles[i];
                        var originalPosition = originalPositions[i];

                        rectangleVM.SetDimensionsWithoutUndo(originalPosition.X + offset.X, originalPosition.Y + offset.Y, rectangleVM.Width, rectangleVM.Height);
                    }
                },
                () =>
                {
                    for (int i = 0; i < selectedRectangles.Length; i++)
                    {
                        var rectangleVM = selectedRectangles[i];
                        var originalPosition = originalPositions[i];

                        rectangleVM.SetDimensionsWithoutUndo(originalPosition.X, originalPosition.Y, rectangleVM.Width, rectangleVM.Height);
                    }
                });
        }

        public void StartResizeRectanglesOperation(Point startTextureCoordinate, ResizeDirection resizeDirection, double gridSize, bool snapToGrid)
        {
            if (RectangleSet == null)
                return;

            // NOTE: No undoable action yet, because there has been no actual resizing yet.

            CurrentOperationStartCoordinate = startTextureCoordinate;
            CurrentOperationRectangles = Selection.Rectangles.ToArray();
            CurrentOperationResizeDirection = resizeDirection;
            CurrentOperationOriginalSizes = Selection.Rectangles
                .Select(rectangleVM => new Rect(rectangleVM.X, rectangleVM.Y, rectangleVM.Width, rectangleVM.Height))
                .ToArray();
            CurrentOperationOriginalSelectionBounds = Selection.GetBounds();
        }

        public void UpdateResizeRectanglesOperation(Point currentTextureCoordinate, double gridSize, bool snapToGrid)
        {
            // Determine the effective offset:
            var offset = GetSnappedCoordinate(currentTextureCoordinate - CurrentOperationStartCoordinate, gridSize, snapToGrid);
            var resizeDirection = CurrentOperationResizeDirection;
            if (!resizeDirection.HasFlag(ResizeDirection.Left) && !resizeDirection.HasFlag(ResizeDirection.Right))
                offset = offset.WithX(0);
            else if (!resizeDirection.HasFlag(ResizeDirection.Top) && !resizeDirection.HasFlag(ResizeDirection.Bottom))
                offset = offset.WithY(0);


            // Calculate the new selection boundary:
            var originalBounds = CurrentOperationOriginalSelectionBounds;
            var top = originalBounds.Top;
            var right = originalBounds.Right;
            var bottom = originalBounds.Bottom;
            var left = originalBounds.Left;

            if (resizeDirection.HasFlag(ResizeDirection.Top))
                top += offset.Y;
            if (resizeDirection.HasFlag(ResizeDirection.Right))
                right += offset.X;
            if (resizeDirection.HasFlag(ResizeDirection.Bottom))
                bottom += offset.Y;
            if (resizeDirection.HasFlag(ResizeDirection.Left))
                left += offset.X;

            if (left > right)
                (left, right) = (right, left);
            if (top > bottom)
                (top, bottom) = (bottom, top);

            // Special case: do not allow a width or height of 0:
            if (right - left < 1)
                right += (snapToGrid ? gridSize : 1);
            if (bottom - top < 1)
                bottom += (snapToGrid ? gridSize : 1);

            var newBounds = new Rect(new Point(left, top), new Point(right, bottom));


            // Calculate scale:
            var scaleX = newBounds.Width / originalBounds.Width;
            var scaleY = newBounds.Height / originalBounds.Height;


            var selectedRectangles = CurrentOperationRectangles;
            var originalSizes = CurrentOperationOriginalSizes;

            var maxDigits = 2;

            PerformUndoableActionOngoing(
                "ResizeRectangles",
                () =>
                {
                    for (int i = 0; i < selectedRectangles.Length; i++)
                    {
                        var rectangleVM = selectedRectangles[i];
                        var originalSize = originalSizes[i];

                        rectangleVM.SetDimensionsWithoutUndo(
                            Math.Round(newBounds.Left + (originalSize.X - originalBounds.Left) * scaleX, maxDigits),
                            Math.Round(newBounds.Top + (originalSize.Y - originalBounds.Top) * scaleY, maxDigits),
                            Math.Round(originalSize.Width * scaleX, maxDigits),
                            Math.Round(originalSize.Height * scaleY, maxDigits));
                    }
                },
                () =>
                {
                    for (int i = 0; i < selectedRectangles.Length; i++)
                    {
                        var rectangleVM = selectedRectangles[i];
                        var originalSize = originalSizes[i];

                        rectangleVM.SetDimensionsWithoutUndo(originalSize.X, originalSize.Y, originalSize.Width, originalSize.Height);
                    }
                });
        }

        public void MoveSelectedRectangles(Vector offset)
        {
            if (RectangleSet == null)
                return;

            var selectedRectangles = Selection.Rectangles.ToArray();
            var originalPositions = selectedRectangles
                .Select(rectangleVM => new Point(rectangleVM.X, rectangleVM.Y))
                .ToArray();

            PerformUndoableActionOngoing(
                "MoveRectanglesByOffset",
                () =>
                {
                    for (int i = 0; i < selectedRectangles.Length; i++)
                    {
                        var rectangleVM = selectedRectangles[i];
                        var originalPosition = originalPositions[i];

                        rectangleVM.SetDimensionsWithoutUndo(originalPosition.X + offset.X, originalPosition.Y + offset.Y, rectangleVM.Width, rectangleVM.Height);
                    }
                },
                () =>
                {
                    for (int i = 0; i < selectedRectangles.Length; i++)
                    {
                        var rectangleVM = selectedRectangles[i];
                        var originalPosition = originalPositions[i];

                        rectangleVM.SetDimensionsWithoutUndo(originalPosition.X, originalPosition.Y, rectangleVM.Width, rectangleVM.Height);
                    }
                });
        }

        public void StartCreateRectangleOperation(Point startTextureCoordinate, double gridSize, bool snapToGrid)
        {
            var rectangleSet = RectangleSet;
            if (rectangleSet == null)
                return;

            var snappedCoordinate = GetSnappedCoordinate(startTextureCoordinate, gridSize, snapToGrid);
            var newRectangle = new HotspotRectangle(
                new Rectangle(snappedCoordinate.X, snappedCoordinate.Y, gridSize, gridSize),
                false,
                Mirrorings.None,
                HotspotLayout.Fit,
                HotspotLayout.Fit,
                null,
                null,
                1,
                ConcaveEdges.None,
                Array.Empty<string>());
            var newRectangleVM = new HotspotRectangleVM(newRectangle, UndoSystem);

            var applyDefaultPreset = Settings.DefaultPreset.CreateDoAction([newRectangleVM]);
            applyDefaultPreset();


            PerformUndoableActionOngoing(
                "CreateRectangle",
                () => rectangleSet.Rectangles.Add(newRectangleVM),
                () => rectangleSet.Rectangles.Remove(newRectangleVM));

            CurrentOperationStartCoordinate = startTextureCoordinate;
            CurrentOperationRectangles = [newRectangleVM];


            // Also select the new rectangle:
            SetSelection(newRectangleVM);
        }

        public void UpdateCreateRectangleOperation(Point currentTextureCoordinate, double gridSize, bool snapToGrid)
        {
            var rectangleVM = CurrentOperationRectangles.FirstOrDefault();
            if (rectangleVM == null)
                return;

            var minX = Math.Min(CurrentOperationStartCoordinate.X, currentTextureCoordinate.X);
            var minY = Math.Min(CurrentOperationStartCoordinate.Y, currentTextureCoordinate.Y);
            var maxX = Math.Max(CurrentOperationStartCoordinate.X, currentTextureCoordinate.X);
            var maxY = Math.Max(CurrentOperationStartCoordinate.Y, currentTextureCoordinate.Y);

            if (snapToGrid)
            {
                // Always making the grid cell that the cursor is over (and the cell that the cursor started at)
                // part of the new rectangle results in a more intuitive editing experience:
                var halfGridSize = gridSize / 2;
                minX -= halfGridSize;
                maxX += halfGridSize;
                minY -= halfGridSize;
                maxY += halfGridSize;
            }

            var snappedTopLeft = GetSnappedCoordinate(new Point(minX, minY), gridSize, snapToGrid);
            var snappedBottomRight = GetSnappedCoordinate(new Point(maxX, maxY), gridSize, snapToGrid);

            // We don't need to update the undoable action here because we're modifying the newly created element:
            var minSize = snapToGrid ? gridSize : 1;
            rectangleVM.SetDimensionsWithoutUndo(
                snappedTopLeft.X,
                snappedTopLeft.Y,
                Math.Max(minSize, snappedBottomRight.X - snappedTopLeft.X),
                Math.Max(minSize, snappedBottomRight.Y - snappedTopLeft.Y));
        }

        public void FinalizeCurrentOperation()
        {
            StopOngoingAction();

            CurrentOperationStartCoordinate = new Point();
            CurrentOperationRectangles = Array.Empty<HotspotRectangleVM>();
            CurrentOperationOriginalPositions = Array.Empty<Point>();
            CurrentOperationResizeDirection = ResizeDirection.None;
            CurrentOperationOriginalSizes = Array.Empty<Rect>();
            CurrentOperationOriginalSelectionBounds = new Rect();
        }


        public void SelectAllRectangles()
        {
            if (RectangleSet != null)
                SetSelection(RectangleSet.Rectangles);
        }

        public void ApplyPresetToSelectedRectangles(Preset? preset)
        {
            if (preset == null)
                return;

            var selectedRectangles = Selection.Rectangles.ToArray();
            if (!selectedRectangles.Any())
                return;


            PerformUndoableAction(
                preset.CreateDoAction(selectedRectangles),
                preset.CreateUndoAction(selectedRectangles));
        }

        public void DeleteSelectedRectangles()
        {
            StopOngoingAction();

            var rectangleSet = RectangleSet;
            if (rectangleSet == null)
                return;

            var selectedRectangles = Selection.Rectangles
                .OrderBy(rectangleSet.Rectangles.IndexOf)
                .ToArray();
            var originalIndices = selectedRectangles
                .Select(rectangleSet.Rectangles.IndexOf)
                .ToArray();

            PerformUndoableAction(
                () =>
                {
                    foreach (var rectangleVM in selectedRectangles)
                        rectangleSet.Rectangles.Remove(rectangleVM);
                },
                () =>
                {
                    for (int i = 0; i < selectedRectangles.Length; i++)
                        rectangleSet.Rectangles.Insert(originalIndices[i], selectedRectangles[i]);
                });
        }


        private Point GetSnappedCoordinate(Point startTextureCoordinate, double gridSize, bool snapToGrid)
        {
            if (snapToGrid)
                return new Point(Math.Round(startTextureCoordinate.X / gridSize) * gridSize, Math.Round(startTextureCoordinate.Y / gridSize) * gridSize);
            else
                return new Point(Math.Round(startTextureCoordinate.X), Math.Round(startTextureCoordinate.Y));
        }


        private void Rectangles_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (HotspotRectangleVM rectangleVM in e.NewItems)
                    rectangleVM.PropertyChanged += Rectangle_PropertyChanged;
            }

            if (e.OldItems != null)
            {
                foreach (HotspotRectangleVM rectangleVM in e.OldItems)
                    rectangleVM.PropertyChanged -= Rectangle_PropertyChanged;
            }

            RaiseRectanglesChanged();
        }

        private void Rectangle_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is HotspotRectangleVM rectangleVM)
                RaiseRectanglePropertyChanged(rectangleVM, e.PropertyName);
        }

        private void Selection_SelectionChanged(HotspotRectangleVM[] deselected, HotspotRectangleVM[] selected)
        {
            StopOngoingAction();
        }


        private static bool IsTouching(HotspotRectangleVM rectangleVM, Point point)
            => point.X >= rectangleVM.X && point.X <= rectangleVM.X + rectangleVM.Width &&
            point.Y >= rectangleVM.Y && point.Y <= rectangleVM.Y + rectangleVM.Height;

        private static bool IsTouching(HotspotRectangleVM rectangleVM, Rect rect)
            => rect.Right >= rectangleVM.X && rect.Left <= rectangleVM.X + rectangleVM.Width &&
            rect.Bottom >= rectangleVM.Y && rect.Top <= rectangleVM.Y + rectangleVM.Height;
    }
}
