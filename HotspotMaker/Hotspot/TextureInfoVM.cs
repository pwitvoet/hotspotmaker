using HotspotMaker.History;
using MLib.Texturing;
using MLib.Texturing.Hotspotting;
using System;
using System.Linq;

namespace HotspotMaker.Hotspot
{
    public class TextureInfoVM : ChangeTrackingVM
    {
        // Bindable properties:
        private HotspotRectangleSetVM? _hotspotRectangleSet;
        public HotspotRectangleSetVM? HotspotRectangleSet
        {
            get => _hotspotRectangleSet;
            set
            {
                _hotspotRectangleSet = value;

                RaisePropertyChanged(nameof(HasHotspotRectangleSet));
                RaisePropertyChanged();
            }
        }

        private string? _fallbackTextureNamePattern;
        public string? FallbackTextureNamePattern
        {
            get => _fallbackTextureNamePattern;
            set { _fallbackTextureNamePattern = value; RaisePropertyChanged(); }
        }

        private double? _fallbackScoreThreshold;
        public double? FallbackScoreThreshold
        {
            get => _fallbackScoreThreshold;
            set { _fallbackScoreThreshold = value; RaisePropertyChanged(); }
        }

        private string[] _labels = [];
        public string[] Labels
        {
            get => _labels;
            set { _labels = value; RaisePropertyChanged(); }
        }


        // Derived properties:
        public bool HasHotspotRectangleSet => !string.IsNullOrEmpty(HotspotRectangleSet?.Name);


        // Read-only:
        public string Name => TextureInfo.Name;

        public int Width => TextureInfo.Width;

        public int Height => TextureInfo.Height;

        public TextureInfo TextureInfo { get; }


        public TextureInfoVM(TextureInfo textureInfo, UndoSystem undoSystem)
            : base(undoSystem)
        {
            TextureInfo = textureInfo;
        }

        public void WithoutUndo(Action action)
            => WithoutChangeTracking(action);

        public HotspotBinding? CreateHotspotBinding()
        {
            if ((HotspotRectangleSet == null || string.IsNullOrEmpty(HotspotRectangleSet.Name)) && string.IsNullOrEmpty(FallbackTextureNamePattern) && FallbackScoreThreshold == null && !Labels.Any())
                return null;

            return new HotspotBinding(Name, HotspotRectangleSet?.Name ?? "", FallbackTextureNamePattern, FallbackScoreThreshold ?? 0, Labels);
        }
    }
}
