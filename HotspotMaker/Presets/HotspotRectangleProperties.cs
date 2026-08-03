using HotspotMaker.Hotspot;
using MLib.Texturing.Hotspotting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HotspotMaker.Presets
{
    public class HotspotRectangleProperties
    {
        public static PropertyInfo<double> XProperty { get; } = new PropertyInfo<double>(nameof(HotspotRectangleVM.X), r => r.X, (r, v) => r.X = v);
        public static PropertyInfo<double> YProperty { get; } = new PropertyInfo<double>(nameof(HotspotRectangleVM.Y), r => r.Y, (r, v) => r.Y = v);
        public static PropertyInfo<double> WidthProperty { get; } = new PropertyInfo<double>(nameof(HotspotRectangleVM.Width), r => r.Width, (r, v) => r.Width = v);
        public static PropertyInfo<double> HeightProperty { get; } = new PropertyInfo<double>(nameof(HotspotRectangleVM.Height), r => r.Height, (r, v) => r.Height = v);

        public static PropertyInfo<bool> AllowRotationProperty { get; } = new PropertyInfo<bool>(nameof(HotspotRectangleVM.AllowRotation), r => r.AllowRotation, (r, v) => r.AllowRotation = v, possibleValues: [false, true]);
        public static PropertyInfo<bool> AllowHorizontalMirroringProperty { get; } = new PropertyInfo<bool>(nameof(HotspotRectangleVM.AllowHorizontalMirroring), r => r.AllowHorizontalMirroring, (r, v) => r.AllowHorizontalMirroring = v, possibleValues: [false, true]);
        public static PropertyInfo<bool> AllowVerticalMirroringProperty { get; } = new PropertyInfo<bool>(nameof(HotspotRectangleVM.AllowVerticalMirroring), r => r.AllowVerticalMirroring, (r, v) => r.AllowVerticalMirroring = v, possibleValues: [false, true]);

        public static PropertyInfo<HotspotLayout> HorizontalLayoutProperty { get; } = new PropertyInfo<HotspotLayout>(nameof(HotspotRectangleVM.HorizontalLayout), r => r.HorizontalLayout, (r, v) => r.HorizontalLayout = v, possibleValues: [HotspotLayout.Fit, HotspotLayout.Clip, HotspotLayout.Tile]);
        public static PropertyInfo<HotspotLayout> VerticalLayoutProperty { get; } = new PropertyInfo<HotspotLayout>(nameof(HotspotRectangleVM.VerticalLayout), r => r.VerticalLayout, (r, v) => r.VerticalLayout = v, possibleValues: [HotspotLayout.Fit, HotspotLayout.Clip, HotspotLayout.Tile]);

        public static PropertyInfo<double?> SnapWidthProperty { get; } = new PropertyInfo<double?>(nameof(HotspotRectangleVM.SnapWidth), r => r.SnapWidth, (r, v) => r.SnapWidth = v);
        public static PropertyInfo<double?> SnapHeightProperty { get; } = new PropertyInfo<double?>(nameof(HotspotRectangleVM.SnapHeight), r => r.SnapHeight, (r, v) => r.SnapHeight = v);

        public static PropertyInfo<double> SelectionWeightProperty { get; } = new PropertyInfo<double>(nameof(HotspotRectangleVM.SelectionWeight), r => r.SelectionWeight, (r, v) => r.SelectionWeight = v);

        public static PropertyInfo<bool> IsTopConcaveProperty { get; } = new PropertyInfo<bool>(nameof(HotspotRectangleVM.IsTopConcave), r => r.IsTopConcave, (r, v) => r.IsTopConcave = v, possibleValues: [false, true]);
        public static PropertyInfo<bool> IsRightConcaveProperty { get; } = new PropertyInfo<bool>(nameof(HotspotRectangleVM.IsRightConcave), r => r.IsRightConcave, (r, v) => r.IsRightConcave = v, possibleValues: [false, true]);
        public static PropertyInfo<bool> IsBottomConcaveProperty { get; } = new PropertyInfo<bool>(nameof(HotspotRectangleVM.IsBottomConcave), r => r.IsBottomConcave, (r, v) => r.IsBottomConcave = v, possibleValues: [false, true]);
        public static PropertyInfo<bool> IsLeftConcaveProperty { get; } = new PropertyInfo<bool>(nameof(HotspotRectangleVM.IsLeftConcave), r => r.IsLeftConcave, (r, v) => r.IsLeftConcave = v, possibleValues: [false, true]);

        public static PropertyInfo<string[]> LabelsProperty { get; } = new PropertyInfo<string[]>(nameof(HotspotRectangleVM.Labels), r => r.Labels, (r, v) => r.Labels = v, InsertLabels);


        public static IReadOnlyList<IPropertyInfo> AllProperties { get; } = [
            XProperty,
            YProperty,
            WidthProperty,
            HeightProperty,
            AllowRotationProperty,
            AllowHorizontalMirroringProperty,
            AllowVerticalMirroringProperty,
            HorizontalLayoutProperty,
            VerticalLayoutProperty,
            SnapWidthProperty,
            SnapHeightProperty,
            SelectionWeightProperty,
            IsTopConcaveProperty,
            IsRightConcaveProperty,
            IsBottomConcaveProperty,
            IsLeftConcaveProperty,
            LabelsProperty,
        ];

        public static IPropertyInfo? GetProperty(string name)
            => AllProperties.FirstOrDefault(property => property.Name == name);


        private static void InsertLabels(HotspotRectangleVM hotspotRectangle, string[] labels)
        {
            var newLabels = hotspotRectangle.Labels.ToList();
            foreach (var label in labels)
            {
                if (!newLabels.Contains(label, StringComparer.InvariantCultureIgnoreCase))
                    newLabels.Add(label);
            }
            hotspotRectangle.Labels = newLabels.ToArray();
        }
    }
}
