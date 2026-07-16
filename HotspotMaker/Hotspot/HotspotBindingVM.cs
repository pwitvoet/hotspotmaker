using HotspotMaker.History;
using MLib.Texturing.Hotspotting;
using System;
using System.Linq;

namespace HotspotMaker.Hotspot
{
    public class HotspotBindingVM : ChangeTrackingVM
    {
        private string _textureNamePattern = "";
        public string TextureNamePattern
        {
            get => _textureNamePattern;
            set => SetPropertyOngoing(v => _textureNamePattern = v, _textureNamePattern, value);
        }

        private HotspotRectangleSetVM? _hotspotRectangleSet;
        public HotspotRectangleSetVM? HotspotRectangleSet
        {
            get => _hotspotRectangleSet;
            set => SetProperty(v => _hotspotRectangleSet = v, _hotspotRectangleSet, value);
        }

        private string? _fallbackTextureNamePattern;
        public string? FallbackTextureNamePattern
        {
            get => _fallbackTextureNamePattern;
            set => SetPropertyOngoing(v => _fallbackTextureNamePattern = v, _fallbackTextureNamePattern, value);
        }

        private double? _fallbackScoreThreshold;
        public double? FallbackScoreThreshold
        {
            get => _fallbackScoreThreshold;
            set => SetPropertyOngoing(v => _fallbackScoreThreshold = v, _fallbackScoreThreshold, value);
        }

        private string[] _labels = Array.Empty<string>();
        public string[] Labels
        {
            get => _labels;
            set => SetPropertyOngoing(v => _labels = v, _labels, value);
        }


        public HotspotBindingVM(string textureNamePattern, HotspotRectangleSetVM? hotspotRectangleSet, UndoSystem undoSystem)
            : base(undoSystem)
        {
            WithoutChangeTracking(() =>
            {
                TextureNamePattern = textureNamePattern;
                HotspotRectangleSet = hotspotRectangleSet;
            });
        }

        public HotspotBindingVM(HotspotBinding binding, HotspotRectangleSetVM? hotspotRectangleSet, UndoSystem undoSystem)
            : base(undoSystem)
        {
            WithoutChangeTracking(() =>
            {
                TextureNamePattern = binding.TextureNamePattern;
                HotspotRectangleSet = hotspotRectangleSet;

                FallbackTextureNamePattern = binding.FallbackTextureNamePattern;
                FallbackScoreThreshold = binding.FallbackScoreThreshold;

                Labels = binding.Labels.ToArray();
            });
        }

        public HotspotBinding CreateHotspotBinding()
        {
            return new HotspotBinding(
                TextureNamePattern,
                HotspotRectangleSet?.Name ?? "",
                FallbackTextureNamePattern,
                FallbackScoreThreshold ?? 0,
                Labels);
        }
    }
}
