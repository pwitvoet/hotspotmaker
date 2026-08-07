using HotspotMaker.History;
using MLib.Mathematics.Spatial;
using MLib.Texturing.Hotspotting;
using System;
using System.Linq;

namespace HotspotMaker.Hotspot
{
    public class HotspotRectangleVM : ChangeTrackingVM
    {
        public static HotspotLayout[] AvailableHotspotLayouts { get; } = [HotspotLayout.Fit, HotspotLayout.Clip, HotspotLayout.Tile];


        private double _x;
        public double X
        {
            get => _x;
            set
            {
                _x = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(DisplayName));
            }
        }

        private double _y;
        public double Y
        {
            get => _y;
            set
            {
                _y = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(DisplayName));
            }
        }

        private double _width;
        public double Width
        {
            get => _width;
            set
            {
                _width = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(DisplayName));
            }
        }

        private double _height;
        public double Height
        {
            get => _height;
            set
            {
                _height = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(DisplayName));
            }
        }

        private bool _allowRotation;
        public bool AllowRotation
        {
            get => _allowRotation;
            set { _allowRotation = value; RaisePropertyChanged(); }
        }

        // TODO: Internally this is a None|Horizontal|Vertical enum, so it doesn't technically support horizontal + vertical (which is a 180 degree rotation)!
        private bool _allowHorizontalMirroring;
        public bool AllowHorizontalMirroring
        {
            get => _allowHorizontalMirroring;
            set { _allowHorizontalMirroring = value; RaisePropertyChanged(); }
        }

        private bool _allowVerticalMirroring;
        public bool AllowVerticalMirroring
        {
            get => _allowVerticalMirroring;
            set { _allowVerticalMirroring = value; RaisePropertyChanged(); }
        }

        private HotspotLayout _horizontalLayout;
        public HotspotLayout HorizontalLayout
        {
            get => _horizontalLayout;
            set { _horizontalLayout = value; RaisePropertyChanged(); }
        }

        private HotspotLayout _verticalLayout;
        public HotspotLayout VerticalLayout
        {
            get => _verticalLayout;
            set { _verticalLayout = value; RaisePropertyChanged(); }
        }

        private double? _snapWidth;
        public double? SnapWidth
        {
            get => _snapWidth;
            set { _snapWidth = value; RaisePropertyChanged(); }
        }

        private double? _snapHeight;
        public double? SnapHeight
        {
            get => _snapHeight;
            set { _snapHeight = value; RaisePropertyChanged(); }
        }

        private double _selectionWeight;
        public double SelectionWeight
        {
            get => _selectionWeight;
            set { _selectionWeight = value; RaisePropertyChanged(); }
        }

        private bool _isTopConcave;
        public bool IsTopConcave
        {
            get => _isTopConcave;
            set { _isTopConcave = value; RaisePropertyChanged(); }
        }

        private bool _isRightConcave;
        public bool IsRightConcave
        {
            get => _isRightConcave;
            set { _isRightConcave = value; RaisePropertyChanged(); }
        }

        private bool _isBottomConcave;
        public bool IsBottomConcave
        {
            get => _isBottomConcave;
            set { _isBottomConcave = value; RaisePropertyChanged(); }
        }

        private bool _isLeftConcave;
        public bool IsLeftConcave
        {
            get => _isLeftConcave;
            set { _isLeftConcave = value; RaisePropertyChanged(); }
        }

        private string[] _labels = Array.Empty<string>();
        public string[] Labels
        {
            get => _labels;
            set { _labels = value; RaisePropertyChanged(); }
        }


        public string DisplayName => $"Rectangle ({X}, {Y}), {Width} x {Height}";


        public HotspotRectangleVM(UndoSystem undoSystem)
            : base(undoSystem)
        {
        }

        public HotspotRectangleVM(HotspotRectangle rectangle, UndoSystem undoSystem)
            : base(undoSystem)
        {
            X = rectangle.Rectangle.X;
            Y = rectangle.Rectangle.Y;
            Width = rectangle.Rectangle.Width;
            Height = rectangle.Rectangle.Height;

            AllowRotation = rectangle.AllowRotation;
            AllowHorizontalMirroring = rectangle.AllowedMirroring.HasFlag(Mirrorings.Horizontal);
            AllowVerticalMirroring = rectangle.AllowedMirroring.HasFlag(Mirrorings.Vertical);

            HorizontalLayout = rectangle.HorizontalLayout;
            VerticalLayout = rectangle.VerticalLayout;
            SnapWidth = rectangle.SnapWidth;
            SnapHeight = rectangle.SnapHeight;

            SelectionWeight = rectangle.SelectionWeight;
            IsTopConcave = rectangle.ConcaveEdges.HasFlag(ConcaveEdges.Top);
            IsRightConcave = rectangle.ConcaveEdges.HasFlag(ConcaveEdges.Right);
            IsBottomConcave = rectangle.ConcaveEdges.HasFlag(ConcaveEdges.Bottom);
            IsLeftConcave = rectangle.ConcaveEdges.HasFlag(ConcaveEdges.Left);

            Labels = rectangle.Labels.ToArray();
        }

        public HotspotRectangle CreateHotspotRectangle()
        {
            var mirrorings = Mirrorings.None;
            if (AllowHorizontalMirroring) mirrorings |= Mirrorings.Horizontal;
            if (AllowVerticalMirroring) mirrorings |= Mirrorings.Vertical;

            var concaveEdges = ConcaveEdges.None;
            if (IsTopConcave) concaveEdges |= ConcaveEdges.Top;
            if (IsRightConcave) concaveEdges |= ConcaveEdges.Right;
            if (IsBottomConcave) concaveEdges |= ConcaveEdges.Bottom;
            if (IsLeftConcave) concaveEdges |= ConcaveEdges.Left;

            return new HotspotRectangle(
                new Rectangle(X, Y, Width, Height),
                AllowRotation,
                mirrorings,
                HorizontalLayout,
                VerticalLayout,
                SnapWidth,
                SnapHeight,
                SelectionWeight,
                concaveEdges,
                Labels);
        }


        public void SetDimensions(double x, double y, double width, double height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
    }
}
