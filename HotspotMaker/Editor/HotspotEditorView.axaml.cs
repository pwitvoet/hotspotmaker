using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using HotspotMaker.Hotspot;
using MLib.Texturing.Hotspotting;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace HotspotMaker.Editor;

public partial class HotspotEditorView : UserControl
{
    // This editor supports the following mouse actions:
    // - Click: select rectangle at point, or the rectangle underneath the currently selected rectangle.
    // - Ctrl + click: add or remove a rectangle from the selection.
    // - Shift + drag: select rectangles within an area.
    // - Drag selection: move the selected rectangles.
    // - Ctrl + drag selection: duplicat the selected rectangles and move them.
    // - Drag: create a new rectangle.
    // - Drag resize handle: resize the selected rectangle.

    enum Operation
    {
        None,

        PointSelection,                 // Click
        PointSelectionUpdate,           // Ctrl + click
        AreaSelection,                  // Shift + drag
        CreateRectangle,                // Drag
        MoveSelectedRectangles,         // Drag selected
        DuplicateSelectedRectangles,    // Ctrl + drag selected
        ResizeSelectedRectangles,       // Drag resize handle
    }

    private const double PointSelectionMoveThreshold = 2;


    public static readonly DirectProperty<HotspotEditorView, bool> IsGridEnabledProperty = AvaloniaProperty.RegisterDirect<HotspotEditorView, bool>(
        nameof(IsGridEnabled),
        o => o.IsGridEnabled,
        (o, v) => o.IsGridEnabled = v,
        defaultBindingMode: BindingMode.TwoWay);

    public static readonly DirectProperty<HotspotEditorView, double> GridSizeProperty = AvaloniaProperty.RegisterDirect<HotspotEditorView, double>(
        nameof(GridSize),
        o => o.GridSize,
        (o, v) => o.GridSize = v,
        defaultBindingMode: BindingMode.TwoWay);

    public static readonly DirectProperty<HotspotEditorView, bool> IsCoordinatesVisibleProperty = AvaloniaProperty.RegisterDirect<HotspotEditorView, bool>(
        nameof(IsCoordinatesVisible),
        o => o.IsCoordinatesVisible,
        (o, v) => o.IsCoordinatesVisible = v,
        defaultBindingMode: BindingMode.TwoWay);

    public static readonly DirectProperty<HotspotEditorView, bool> IsIconsVisibleProperty = AvaloniaProperty.RegisterDirect<HotspotEditorView, bool>(
        nameof(IsIconsVisible),
        o => o.IsIconsVisible,
        (o, v) => o.IsIconsVisible = v,
        defaultBindingMode: BindingMode.TwoWay);


    public event Action<HotspotRectangleVM>? RectangleClicked;


    // Editor state:
    private HotspotEditorVM? Editor { get; set; }

    private Point _cameraOffset;
    private Point CameraOffset
    {
        get => _cameraOffset;
        set { _cameraOffset = value; InvalidateVisual(); }
    }

    private double _cameraScale = 1.5;
    private double CameraScale
    {
        get => _cameraScale;
        set { _cameraScale = value; InvalidateVisual(); }
    }

    private bool _isGridEnabled = true;
    private bool IsGridEnabled
    {
        get => _isGridEnabled;
        set { SetAndRaise(IsGridEnabledProperty, ref _isGridEnabled, value); InvalidateVisual(); }
    }

    private double _gridSize = 16;
    private double GridSize
    {
        get => _gridSize;
        set { SetAndRaise(GridSizeProperty, ref _gridSize, value); InvalidateVisual(); }
    }

    private bool _isCoordinatesVisible = true;
    private bool IsCoordinatesVisible
    {
        get => _isCoordinatesVisible;
        set { SetAndRaise(IsCoordinatesVisibleProperty, ref _isCoordinatesVisible, value); InvalidateVisual(); }
    }

    private bool _isIconsVisible = true;
    private bool IsIconsVisible
    {
        get => _isIconsVisible;
        set { SetAndRaise(IsIconsVisibleProperty, ref _isIconsVisible, value); InvalidateVisual(); }
    }

    private PointerButtons PointerState { get; set; }
    private Point PreviousPointerPosition { get; set; }

    private Size PreviousTextureSize { get; set; }


    // Mouse/keyboard related state:
    private KeyModifiers KeyModifiers { get; set; }
    private Point LastKnownPointerPosition { get; set; }

    private Operation PointerOperation { get; set; }
    private Point PointerOperationStartPosition { get; set; }
    private bool PointerOperationStartedAtSelectedRectangle { get; set; }
    private KeyModifiers PointerOperationStartKeyModifiers { get; set; }


    // Brushes and pens:
    private Brush BackgroundBrush { get; } = new SolidColorBrush(0xFF404040);
    private Pen GridPen { get; } = new Pen(0x20FFFFFF);

    private Brush RectangleBrush { get; } = new SolidColorBrush(0x40F0F0FF);
    private Pen RectangleBorderPen { get; } = new Pen(0xFFFFFFFF, 2);
    private Pen RectangleDashedBorderPen { get; } = new Pen(0xFFFFFFFF, 2, DashStyle.Dash);
    private Pen RectangleSnapLinePen { get; } = new Pen(0xC0FFE000, 2, DashStyle.Dash);
    private Pen RectangleConcaveEdgePen { get; } = new Pen(0xFF0040C0, 2);

    private Brush TilingRectangleTopBrush { get; } = MakeLinearGradientBrush(new Point(0, 1), new Point(0, 0), 0x40F0F0FF, 0x00F0F0FF);
    private Brush TilingRectangleBottomBrush { get; } = MakeLinearGradientBrush(new Point(0, 0), new Point(0, 1), 0x40F0F0FF, 0x00F0F0FF);
    private Brush TilingRectangleLeftBrush { get; } = MakeLinearGradientBrush(new Point(1, 0), new Point(0, 0), 0x40F0F0FF, 0x00F0F0FF);
    private Brush TilingRectangleRightBrush { get; } = MakeLinearGradientBrush(new Point(0, 0), new Point(1, 0), 0x40F0F0FF, 0x00F0F0FF);

    private Brush SelectedRectangleBrush { get; } = new SolidColorBrush(0x40FFF0F0);
    private Pen SelectedRectangleBorderPen { get; } = new Pen(0xFFFF0000, 2);
    private Pen SelectedRectangleDashedBorderPen { get; } = new Pen(0xFFFF0000, 2, DashStyle.Dash);

    private Brush SelectedTilingRectangleTopBrush { get; } = MakeLinearGradientBrush(new Point(0, 1), new Point(0, 0), 0x40FFF0F0, 0x00FFF0F0);
    private Brush SelectedTilingRectangleBottomBrush { get; } = MakeLinearGradientBrush(new Point(0, 0), new Point(0, 1), 0x40FFF0F0, 0x00FFF0F0);
    private Brush SelectedTilingRectangleLeftBrush { get; } = MakeLinearGradientBrush(new Point(1, 0), new Point(0, 0), 0x40FFF0F0, 0x00FFF0F0);
    private Brush SelectedTilingRectangleRightBrush { get; } = MakeLinearGradientBrush(new Point(0, 0), new Point(1, 0), 0x40FFF0F0, 0x00FFF0F0);

    private Brush SelectionAreaBrush { get; } = new SolidColorBrush(0x40FFFFFF);
    private Pen SelectionAreaBorderPen { get; } = new Pen(0x808080FF);

    private Bitmap RotateIconBitmap { get; }
    private Bitmap MirrorHorizontalIconBitmap { get; }
    private Bitmap MirrorVerticalIconBitmap { get; }
    private Bitmap MirrorHorizontalVerticalIconBitmap { get; }


    public HotspotEditorView()
    {
        InitializeComponent();

        // We want nice crispy pixels when zooming in:
        RenderOptions.SetBitmapInterpolationMode(this, Avalonia.Media.Imaging.BitmapInterpolationMode.None);

        RotateIconBitmap = new Bitmap(AssetLoader.Open(new Uri("avares://HotspotMaker/Assets/Images/icon_rotate.png")));
        MirrorHorizontalIconBitmap = new Bitmap(AssetLoader.Open(new Uri("avares://HotspotMaker/Assets/Images/icon_mirror_h.png")));
        MirrorVerticalIconBitmap = new Bitmap(AssetLoader.Open(new Uri("avares://HotspotMaker/Assets/Images/icon_mirror_v.png")));
        MirrorHorizontalVerticalIconBitmap = new Bitmap(AssetLoader.Open(new Uri("avares://HotspotMaker/Assets/Images/icon_mirror_hv.png")));
    }


    public void HandleKeyDown(KeyEventArgs e)
    {
        if (e.Handled)
            return;

        var editor = Editor;
        if (editor == null)
            return;

        var handled = true;
        if (KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            switch (e.Key)
            {
                // Select all:
                case Key.A:
                    if (editor.RectangleSet != null)
                        editor.SetSelection(editor.RectangleSet.Rectangles);
                    break;

                // TODO: These should also show status messages on failure, like the menu commands!
                //Cut:
                case Key.X:
                    _ = editor.CopySelectionToClipboard(deleteSelection: true);
                    break;

                // Copy:
                case Key.C:
                    _ = editor.CopySelectionToClipboard();
                    break;

                // Paste:
                case Key.V:
                    _ = editor.PasteFromClipboard();
                    break;

                // Hotkeys for quickly toggling concave edges:
                case Key.Up: editor.Selection.IsTopConcave.Value = !(editor.Selection.IsTopConcave.Value ?? true); break;
                case Key.Down: editor.Selection.IsBottomConcave.Value = !(editor.Selection.IsBottomConcave.Value ?? true); break;
                case Key.Left: editor.Selection.IsLeftConcave.Value = !(editor.Selection.IsLeftConcave.Value ?? true); break;
                case Key.Right: editor.Selection.IsRightConcave.Value = !(editor.Selection.IsRightConcave.Value ?? true); break;

                default:
                    handled = false;
                    break;
            }
        }
        else
        {
            switch (e.Key)
            {
                case Key.Delete:
                    editor.DeleteSelectedRectangles();
                    break;

                // Hotkeys for quickly adjusting certain rectangle properties:
                case Key.R: editor.Selection.AllowRotation.Value = !(editor.Selection.AllowRotation.Value ?? true); break;
                case Key.H: editor.Selection.AllowHorizontalMirroring.Value = !(editor.Selection.AllowHorizontalMirroring.Value ?? true); break;
                case Key.V: editor.Selection.AllowVerticalMirroring.Value = !(editor.Selection.AllowVerticalMirroring.Value ?? true); break;
                case Key.K: editor.Selection.CycleHorizontalLayout(); break;
                case Key.L: editor.Selection.CycleVerticalLayout(); break;

                // Hotkeys for quickly adding or removing common labels:
                case Key.D0:
                case Key.D1:
                case Key.D2:
                case Key.D3:
                case Key.D4:
                case Key.D5:
                case Key.D6:
                case Key.D7:
                case Key.D8:
                case Key.D9:
                    editor.Selection.ToggleCommonLabel(e.Key - Key.D0);
                    break;

                // Toggle grid:
                case Key.G:
                    editor.ToggleGrid();
                    break;

                // Decrease grid size with '['
                case Key.OemOpenBrackets:
                    editor.DecreaseGridSize();
                    break;

                // Increase grid size with ']'
                case Key.OemCloseBrackets:
                    editor.IncreaseGridSize();
                    break;

                default:
                    handled = false;
                    break;
            }
        }

        e.Handled = handled;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        context.FillRectangle(BackgroundBrush, new Rect(0, 0, Bounds.Width, Bounds.Height));

        var editor = Editor;
        if (editor != null)
        {
            DrawTexture(context, editor);
            DrawHotspotRectangles(context, editor);

            if (IsCoordinatesVisible)
                DrawSelectionInformation(context, editor);

            if (PointerOperation == Operation.AreaSelection)
                DrawSelectionArea(context, editor);
        }

        DrawGrid(context);

        if (IsCoordinatesVisible)
            DrawPointerCoordinates(context);
    }

    private void DrawTexture(DrawingContext context, HotspotEditorVM editor)
    {
        var textureImage = editor.TextureImage;
        if (textureImage == null)
            return;


        context.DrawImage(textureImage, new Rect(CameraOffset.X, CameraOffset.Y, textureImage.Size.Width * CameraScale, textureImage.Size.Height * CameraScale));
    }

    // TODO: Draw the various hotspot rectangle settings! (snap distances as sub-rects, horizontal/vertical tiling as open-ended sides (no edge), tiling/rotating as little icons maybe? etc!)
    private void DrawHotspotRectangles(DrawingContext context, HotspotEditorVM editor)
    {
        var rectangleSet = editor.RectangleSet;
        if (rectangleSet == null)
            return;


        foreach (var rectangle in rectangleSet.Rectangles)
            DrawHotspotRectangle(context, editor, rectangle);

        var selectedRectangles = editor.Selection.Rectangles;
        if (selectedRectangles.Any())
        {
            foreach (var selectedRectangle in selectedRectangles)
            {
                context.DrawRectangle(
                    SelectedRectangleBorderPen,
                    new Rect(
                        CameraOffset.X + (selectedRectangle.X * CameraScale),
                        CameraOffset.Y + (selectedRectangle.Y * CameraScale),
                        selectedRectangle.Width * CameraScale,
                        selectedRectangle.Height * CameraScale));
            }
        }
    }

    private void DrawHotspotRectangle(DrawingContext context, HotspotEditorVM editor, HotspotRectangleVM rectangle)
    {
        var topLeft = TextureToScreenCoordinate(new Point(rectangle.X, rectangle.Y));
        var bottomRight = TextureToScreenCoordinate(new Point(rectangle.X + rectangle.Width, rectangle.Y + rectangle.Height));
        var tileExtent = 32 * CameraScale;


        // Rectangle background:
        var isSelected = editor.Selection.IsSelected(rectangle);
        context.FillRectangle(
            isSelected ? SelectedRectangleBrush : RectangleBrush,
            new Rect(topLeft, bottomRight));


        // Snap lines:
        if (IsIconsVisible)
        {
            if (rectangle.SnapWidth != null && rectangle.SnapWidth >= 1)
            {
                for (var x = rectangle.X + rectangle.SnapWidth.Value; x < rectangle.X + rectangle.Width; x += rectangle.SnapWidth.Value)
                    context.DrawLine(RectangleSnapLinePen, TextureToScreenCoordinate(new Point(x, rectangle.Y)), TextureToScreenCoordinate(new Point(x, rectangle.Y + rectangle.Height)));
            }

            if (rectangle.SnapHeight != null && rectangle.SnapHeight >= 1)
            {
                for (var y = rectangle.Y + rectangle.SnapHeight.Value; y < rectangle.Y + rectangle.Height; y += rectangle.SnapHeight.Value)
                    context.DrawLine(RectangleSnapLinePen, TextureToScreenCoordinate(new Point(rectangle.X, y)), TextureToScreenCoordinate(new Point(rectangle.X + rectangle.Width, y)));
            }
        }


        // Concave edges:
        if (IsIconsVisible)
        {
            var left = topLeft.X + 2;
            var right = bottomRight.X - 2;
            var top = topLeft.Y + 2;
            var bottom = bottomRight.Y - 2;

            if (rectangle.IsTopConcave)
                context.DrawLine(RectangleConcaveEdgePen, new Point(left, top), new Point(right, top));

            if (rectangle.IsRightConcave)
                context.DrawLine(RectangleConcaveEdgePen, new Point(right, top), new Point(right, bottom));

            if (rectangle.IsBottomConcave)
                context.DrawLine(RectangleConcaveEdgePen, new Point(left, bottom), new Point(right, bottom));

            if (rectangle.IsLeftConcave)
                context.DrawLine(RectangleConcaveEdgePen, new Point(left, top), new Point(left, bottom));
        }


        // Rectangle edges:
        var borderPen = isSelected ? SelectedRectangleBorderPen : RectangleBorderPen;
        if (rectangle.HorizontalLayout == HotspotLayout.Tile)
        {
            context.FillRectangle(isSelected ? SelectedTilingRectangleLeftBrush : TilingRectangleLeftBrush, new Rect(topLeft.WithX(topLeft.X - tileExtent), topLeft.WithY(bottomRight.Y)));
            context.FillRectangle(isSelected ? SelectedTilingRectangleRightBrush : TilingRectangleRightBrush, new Rect(topLeft.WithX(bottomRight.X), bottomRight.WithX(bottomRight.X + tileExtent)));

            context.DrawLine(isSelected ? SelectedRectangleDashedBorderPen : RectangleDashedBorderPen, topLeft, topLeft.WithY(bottomRight.Y));
            context.DrawLine(isSelected ? SelectedRectangleDashedBorderPen : RectangleDashedBorderPen, topLeft.WithX(bottomRight.X), bottomRight);
        }
        else
        {
            context.DrawLine(borderPen, topLeft, topLeft.WithY(bottomRight.Y));
            context.DrawLine(borderPen, topLeft.WithX(bottomRight.X), bottomRight);
        }

        if (rectangle.VerticalLayout == HotspotLayout.Tile)
        {
            context.FillRectangle(isSelected ? SelectedTilingRectangleTopBrush : TilingRectangleTopBrush, new Rect(topLeft.WithY(topLeft.Y - tileExtent), topLeft.WithX(bottomRight.X)));
            context.FillRectangle(isSelected ? SelectedTilingRectangleBottomBrush : TilingRectangleBottomBrush, new Rect(topLeft.WithY(bottomRight.Y), bottomRight.WithY(bottomRight.Y + tileExtent)));

            context.DrawLine(isSelected ? SelectedRectangleDashedBorderPen : RectangleDashedBorderPen, topLeft, topLeft.WithX(bottomRight.X));
            context.DrawLine(isSelected ? SelectedRectangleDashedBorderPen : RectangleDashedBorderPen, topLeft.WithY(bottomRight.Y), bottomRight);
        }
        else
        {
            context.DrawLine(borderPen, topLeft, topLeft.WithX(bottomRight.X));
            context.DrawLine(borderPen, topLeft.WithY(bottomRight.Y), bottomRight);
        }


        // Property icons:
        if (IsIconsVisible && CameraScale >= 0.5)
        {
            var drawPosition = new Point(topLeft.X + 1, topLeft.Y + 1);

            if (rectangle.AllowRotation)
            {
                context.DrawImage(RotateIconBitmap, new Rect(drawPosition, RotateIconBitmap.Size));
                drawPosition = new Point(drawPosition.X + RotateIconBitmap.Size.Width + 1, drawPosition.Y);
            }

            var mirrorIcon = (rectangle.AllowHorizontalMirroring && rectangle.AllowVerticalMirroring) ? MirrorHorizontalVerticalIconBitmap :
                                                                   rectangle.AllowHorizontalMirroring ? MirrorHorizontalIconBitmap :
                                                                     rectangle.AllowVerticalMirroring ? MirrorVerticalIconBitmap :
                                                                                                        null;
            if (mirrorIcon != null)
            {
                context.DrawImage(mirrorIcon, new Rect(drawPosition, mirrorIcon.Size));
                drawPosition = new Point(drawPosition.X + mirrorIcon.Size.Width + 1, drawPosition.Y);
            }
        }
    }

    private void DrawSelectionInformation(DrawingContext context, HotspotEditorVM editor)
    {
        if (editor.Selection.IsEmpty)
            return;

        var bounds = editor.Selection.GetBounds();
        var topLeft = TextureToScreenCoordinate(bounds.TopLeft);
        var topCenter = TextureToScreenCoordinate(new Point(bounds.X + bounds.Width / 2, bounds.Y));
        var leftCenter = TextureToScreenCoordinate(new Point(bounds.X, bounds.Y + bounds.Height / 2));

        DrawText(context, $"{bounds.X}, {bounds.Y}", new Point(topLeft.X - 20, topLeft.Y - 20));
        DrawText(context, $"{bounds.Width}", new Point(topCenter.X, topCenter.Y - 20));
        DrawText(context, $"{bounds.Height}", new Point(leftCenter.X - 20, leftCenter.Y));
    }

    private void DrawSelectionArea(DrawingContext context, HotspotEditorVM editor)
    {
        var area = GetBoundingRect(PointerOperationStartPosition, LastKnownPointerPosition);
        context.DrawRectangle(SelectionAreaBrush, SelectionAreaBorderPen, area);

        // TODO: Also highlight all rectangles that would be selected if the LMB was released at this moment?
    }

    private void DrawGrid(DrawingContext context)
    {
        if (!IsGridEnabled || GridSize < 1)
            return;


        var cameraSpaceBounds = new Rect(
            -CameraOffset.X / CameraScale,
            -CameraOffset.Y / CameraScale,
            Bounds.Width / CameraScale,
            Bounds.Height / CameraScale);

        var minX = Math.Round(cameraSpaceBounds.X / GridSize) * GridSize;
        var maxX = Math.Round((cameraSpaceBounds.X + cameraSpaceBounds.Width) / GridSize) * GridSize;
        var minY = Math.Round(cameraSpaceBounds.Y / GridSize) * GridSize;
        var maxY = Math.Round((cameraSpaceBounds.Y + cameraSpaceBounds.Height) / GridSize) * GridSize;

        for (var x = minX; x <= maxX; x += GridSize)
        {
            var xPos = Math.Round(CameraOffset.X + x * CameraScale) + 0.5;
            context.DrawLine(GridPen, new Point(xPos, 0), new Point(xPos + 0.5, Bounds.Height));
        }

        for (var y = minY; y <= maxY; y += GridSize)
        {
            var yPos = Math.Round(CameraOffset.Y + y * CameraScale) + 0.5;
            context.DrawLine(GridPen, new Point(0, yPos), new Point(Bounds.Width, yPos));
        }
    }

    private void DrawPointerCoordinates(DrawingContext context)
    {
        var textureCoordinates = ScreenToTextureCoordinate(LastKnownPointerPosition);
        DrawText(context, $"{Math.Round(textureCoordinates.X)}, {Math.Round(textureCoordinates.Y)}", new Point(2, 2));
    }

    private void DrawText(DrawingContext context, string text, Point position)
    {
        context.DrawText(new FormattedText(text, CultureInfo.CurrentUICulture, FlowDirection, Typeface.Default, 12, Foreground), position);
    }


    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (Editor != null)
        {
            Editor.PropertyChanged -= Editor_PropertyChanged;
            Editor.RectanglesChanged -= Editor_RectanglesChanged;
            Editor.RectanglePropertyChanged -= Editor_RectanglePropertyChanged;
            Editor.Selection.SelectionChanged -= Selection_SelectionChanged;
        }

        Editor = DataContext as HotspotEditorVM;

        if (Editor != null)
        {
            Editor.PropertyChanged += Editor_PropertyChanged;
            Editor.RectanglesChanged += Editor_RectanglesChanged;
            Editor.RectanglePropertyChanged += Editor_RectanglePropertyChanged;
            Editor.Selection.SelectionChanged += Selection_SelectionChanged;
        }
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);

        // Keep the view centered:
        var sizeChange = e.NewSize - e.PreviousSize;
        CameraOffset += new Point(sizeChange.Width / 2, sizeChange.Height / 2);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        KeyModifiers = e.KeyModifiers;

        var pointerPosition = e.GetPosition(this);
        LastKnownPointerPosition = pointerPosition;
        InvalidateVisual();     // Update the pointer coordinates display

        UpdatePointerState(pointerPosition, e.Properties);

        var delta = pointerPosition - PreviousPointerPosition;
        PreviousPointerPosition = pointerPosition;

        if (Editor != null)
            HandlePointerMovement(Editor, pointerPosition, delta);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        KeyModifiers = e.KeyModifiers;
        UpdatePointerState(e.GetPosition(this), e.Properties);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        KeyModifiers = e.KeyModifiers;
        UpdatePointerState(e.GetPosition(this), e.Properties);
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);

        KeyModifiers = e.KeyModifiers;

        if (Editor != null)
            HandlePointerWheelChange(Editor, e.GetPosition(this), e.Delta);
    }

    // NOTE: This method gets called only when the editor view has focus.
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Handled)
            return;

        KeyModifiers = e.KeyModifiers;

        var editor = Editor;
        if (editor == null)
            return;

        var selectedRectangles = editor.Selection.Rectangles;
        if (selectedRectangles.Any())
        {
            if (!KeyModifiers.HasFlag(KeyModifiers.Control) && (e.Key == Key.Up || e.Key == Key.Down || e.Key == Key.Left || e.Key == Key.Right))
            {
                var distance = IsGridEnabled ? GridSize : 1;
                var movement = new Vector(0, 0);
                if (e.Key == Key.Up) movement -= new Vector(0, distance);
                if (e.Key == Key.Down) movement += new Vector(0, distance);
                if (e.Key == Key.Left) movement -= new Vector(distance, 0);
                if (e.Key == Key.Right) movement += new Vector(distance, 0);

                if (movement.X != 0 || movement.Y != 0)
                    editor.MoveSelectedRectangles(movement);

                e.Handled = true;
            }
        }
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);

        KeyModifiers = e.KeyModifiers;
    }


    private void Editor_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(HotspotEditorVM.TextureImage))
        {
            // Adjust camera position somewhat if the new texture has a different size:
            var newTextureSize = Editor?.TextureImage?.Size ?? new Size();
            var oldTextureSize = PreviousTextureSize;
            PreviousTextureSize = newTextureSize;

            var cameraMovement = (oldTextureSize - newTextureSize) / 2 * CameraScale;
            CameraOffset += new Point(cameraMovement.Width, cameraMovement.Height);

            InvalidateVisual();
        }
    }

    private void Editor_RectanglesChanged() => InvalidateVisual();

    private void Editor_RectanglePropertyChanged(HotspotRectangleVM sender, string? propertyName) => InvalidateVisual();

    private void Selection_SelectionChanged(HotspotRectangleVM[] deselected, HotspotRectangleVM[] selected) => InvalidateVisual();


    private void UpdatePointerState(Point position, PointerPointProperties pointer)
    {
        CheckButtonState(PointerButtons.Left, pointer.IsLeftButtonPressed);
        CheckButtonState(PointerButtons.Middle, pointer.IsMiddleButtonPressed);
        CheckButtonState(PointerButtons.Right, pointer.IsRightButtonPressed);
        CheckButtonState(PointerButtons.X1, pointer.IsXButton1Pressed);
        CheckButtonState(PointerButtons.X2, pointer.IsXButton2Pressed);


        void CheckButtonState(PointerButtons button, bool isPressed)
        {
            if (PointerState.HasFlag(button) != isPressed)
            {
                if (isPressed)
                    PointerState |= button;
                else
                    PointerState &= ~button;

                if (Editor != null)
                {
                    if (isPressed)
                        HandlePointerButtonPress(Editor, position, button);
                    else
                        HandlePointerButtonRelease(Editor, position, button);
                }
            }
        }
    }


    private void HandlePointerMovement(HotspotEditorVM editor, Point position, Vector delta)
    {
        if (PointerState.HasFlag(PointerButtons.Left))
        {
            var startTextureCoordinate = ScreenToTextureCoordinate(PointerOperationStartPosition);
            var currentTextureCoordinate = ScreenToTextureCoordinate(position);

            if ((PointerOperation == Operation.PointSelection || PointerOperation == Operation.PointSelectionUpdate) &&
                DistanceBetween(position, PointerOperationStartPosition) > PointSelectionMoveThreshold)
            {
                // TODO: Check whether the pointer started at a resize handle -- if so, switch to resize mode! To be implemented later!

                if (PointerOperationStartedAtSelectedRectangle)
                {
                    if (PointerOperationStartKeyModifiers.HasFlag(KeyModifiers.Control))
                    {
                        PointerOperation = Operation.DuplicateSelectedRectangles;
                        editor.StartDuplicateRectanglesOperation(startTextureCoordinate, GridSize, IsGridEnabled);
                        editor.UpdateDuplicateRectanglesOperation(currentTextureCoordinate, GridSize, IsGridEnabled);
                    }
                    else
                    {
                        PointerOperation = Operation.MoveSelectedRectangles;
                        editor.StartMoveRectanglesOperation(startTextureCoordinate, GridSize, IsGridEnabled);
                        editor.UpdateMoveRectanglesOperation(currentTextureCoordinate, GridSize, IsGridEnabled);
                    }
                }
                else
                {
                    PointerOperation = Operation.CreateRectangle;
                    editor.StartCreateRectangleOperation(startTextureCoordinate, GridSize, IsGridEnabled);
                    editor.UpdateCreateRectangleOperation(currentTextureCoordinate, GridSize, IsGridEnabled);
                }
            }
            else if (PointerOperation == Operation.DuplicateSelectedRectangles)
            {
                editor.UpdateDuplicateRectanglesOperation(currentTextureCoordinate, GridSize, IsGridEnabled);
            }
            else if (PointerOperation == Operation.MoveSelectedRectangles)
            {
                editor.UpdateMoveRectanglesOperation(currentTextureCoordinate, GridSize, IsGridEnabled);
            }
            else if (PointerOperation == Operation.CreateRectangle)
            {
                editor.UpdateCreateRectangleOperation(currentTextureCoordinate, GridSize, IsGridEnabled);
            }
            // TODO: Update resize operation!
        }

        if (PointerState.HasFlag(PointerButtons.Right) || PointerState.HasFlag(PointerButtons.Middle))
        {
            CameraOffset += delta;
        }


        if (PointerOperation == Operation.AreaSelection)
            InvalidateVisual();
    }

    private void HandlePointerButtonPress(HotspotEditorVM editor, Point position, PointerButtons button)
    {
        if (button == PointerButtons.Left)
        {
            PointerOperationStartPosition = position;

            // NOTE: Pointer movement may change the current pointer operation into a different kind of operation.
            if (KeyModifiers.HasFlag(KeyModifiers.Shift))
                PointerOperation = Operation.AreaSelection;
            else if (KeyModifiers.HasFlag(KeyModifiers.Control))
                PointerOperation = Operation.PointSelectionUpdate;
            else
                PointerOperation = Operation.PointSelection;

            var rectanglesAtPosition = editor.GetRectanglesAtPoint(ScreenToTextureCoordinate(position));
            PointerOperationStartedAtSelectedRectangle = editor.Selection.Rectangles.Any(rectangleVM => rectanglesAtPosition.Contains(rectangleVM));
            PointerOperationStartKeyModifiers = KeyModifiers;
        }
    }

    private void HandlePointerButtonRelease(HotspotEditorVM editor, Point position, PointerButtons button)
    {
        if (button == PointerButtons.Left)
        {
            var startTextureCoordinate = ScreenToTextureCoordinate(PointerOperationStartPosition);
            var currentTextureCoordinate = ScreenToTextureCoordinate(position);

            switch (PointerOperation)
            {
                case Operation.PointSelection:
                    SelectRectangleAtPoint(editor, currentTextureCoordinate);
                    break;

                case Operation.PointSelectionUpdate:
                    ToggleRectangleSelectionAtPoint(editor, currentTextureCoordinate);
                    break;

                case Operation.AreaSelection:
                    SelectRectanglesInArea(editor, GetBoundingRect(startTextureCoordinate, currentTextureCoordinate));
                    InvalidateVisual();
                    break;

                case Operation.CreateRectangle:
                    editor.FinalizeCurrentOperation();
                    break;

                case Operation.MoveSelectedRectangles:
                    editor.FinalizeCurrentOperation();
                    break;

                case Operation.DuplicateSelectedRectangles:
                    editor.FinalizeCurrentOperation();
                    break;

                case Operation.ResizeSelectedRectangles:
                    // TODO: Finalize resize action!
                    break;
            }

            PointerOperation = Operation.None;
            PointerOperationStartPosition = new Point();
            PointerOperationStartedAtSelectedRectangle = false;
            PointerOperationStartKeyModifiers = KeyModifiers.None;
        }
    }

    private void HandlePointerWheelChange(HotspotEditorVM editor, Point position, Vector wheelDelta)
    {
        // Adjust zoom level:
        if (wheelDelta.Y > 0)
        {
            if (CameraScale < 10)
                ChangeCameraScale(position, CameraScale * 1.1);
        }
        else if (wheelDelta.Y < 0)
        {
            if (CameraScale > 0.1)
                ChangeCameraScale(position, CameraScale / 1.1);
        }
    }


    private void ChangeCameraScale(Point position, double newScale)
    {
        var relativePosition = position - CameraOffset;
        var newRelativePosition = (relativePosition / CameraScale) * newScale;

        CameraOffset = position - newRelativePosition;
        CameraScale = newScale;
    }

    private void SelectRectangleAtPoint(HotspotEditorVM editor, Point textureCoordinate)
    {
        var rectanglesAtPoint = editor.GetRectanglesAtPoint(textureCoordinate);
        if (!rectanglesAtPoint.Any())
        {
            // Clicking on an empty spot will deselect everything:
            editor.ClearSelection();
            return;
        }

        // Did we click on the currently selected rectangle?
        var singleSelectedRectangle = editor.Selection.Rectangles.Count() == 1 ? editor.Selection.Rectangles.First() : null;
        if (singleSelectedRectangle != null && rectanglesAtPoint.Contains(singleSelectedRectangle))
        {
            if (rectanglesAtPoint.Length > 1)
            {
                // Clicking on the currently selected rectangle will select the rectangle underneath it, if there is any.
                // If the bottom-most rectangle was selected, the top-most rectangle will be selected again:
                var selectedIndex = rectanglesAtPoint.TakeWhile(rectangle => rectangle != singleSelectedRectangle).Count();
                var newSelectedIndex = (selectedIndex + 1) % rectanglesAtPoint.Length;
                editor.SetSelection(rectanglesAtPoint[newSelectedIndex]);
            }
            // Else, do nothing - the currently selected rectangle will remain selected.
        }
        else
        {
            // Select the top-most rectangle at this point:
            editor.SetSelection(rectanglesAtPoint[0]);
        }
    }

    private void ToggleRectangleSelectionAtPoint(HotspotEditorVM editor, Point textureCoordinate)
    {
        var rectanglesAtPoint = editor.GetRectanglesAtPoint(textureCoordinate);
        if (!rectanglesAtPoint.Any())
            return;

        if (editor.Selection.Rectangles.Any(rectanglesAtPoint.Contains))
        {
            // Deselect all rectangles at this point, if at least one of them is selected:
            editor.SetSelection(editor.Selection.Rectangles.Where(rectangleVM => !rectanglesAtPoint.Contains(rectangleVM)));
        }
        else
        {
            // Else, add the top-most rectangle to the selection (support for cycling to underlying rectangles would get a bit complicated here):
            editor.SetSelection(editor.Selection.Rectangles.Append(rectanglesAtPoint[0]));
        }
    }

    private void SelectRectanglesInArea(HotspotEditorVM editor, Rect textureArea)
        => editor.SetSelection(editor.GetRectanglesInArea(textureArea));


    private Point ScreenToTextureCoordinate(Point screenPoint)
        => (screenPoint - CameraOffset) / CameraScale;

    private Point TextureToScreenCoordinate(Point texturePoint)
        => (texturePoint * CameraScale) + CameraOffset;


    // TODO: Move these to a util or extensions class!
    private static double DistanceBetween(Point p1, Point p2)
    {
        var dx = p1.X - p2.X;
        var dy = p1.Y - p2.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static Rect GetBoundingRect(Point p1, Point p2)
    {
        var x = Math.Min(p1.X, p2.X);
        var y = Math.Min(p1.Y, p2.Y);
        var width = Math.Max(p1.X, p2.X) - x;
        var height = Math.Max(p1.Y, p2.Y) - y;

        return new Rect(x, y, width, height);
    }

    private static Brush MakeLinearGradientBrush(Point startPoint, Point endPoint, uint startColor, uint endColor)
    {
        return new LinearGradientBrush {
            StartPoint = new RelativePoint(startPoint, RelativeUnit.Relative),
            EndPoint = new RelativePoint(endPoint, RelativeUnit.Relative),
            GradientStops = [
                new GradientStop(Color.FromUInt32(startColor), 0),
                new GradientStop(Color.FromUInt32(endColor), 1),
            ],
        };
    }
}
