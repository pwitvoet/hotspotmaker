using HotspotMaker.Hotspot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HotspotMaker.Presets
{
    public class Preset
    {
        public string Description { get; }

        private IPropertyPreset[] _propertyPresets;
        public IReadOnlyList<IPropertyPreset> PropertyPresets => _propertyPresets;


        public Preset(string description, IEnumerable<IPropertyPreset> propertyPresets)
        {
            Description = description;
            _propertyPresets = propertyPresets.ToArray();
        }


        public Action CreateDoAction(IReadOnlyList<HotspotRectangleVM> hotspotRectangles)
        {
            var doActions = PropertyPresets
                .Select(propertyPreset => propertyPreset.CreateDoAction(hotspotRectangles))
                .ToArray();

            return () =>
            {
                foreach (var doAction in doActions)
                    doAction();
            };
        }

        public Action CreateUndoAction(IReadOnlyList<HotspotRectangleVM> hotspotRectangles)
        {
            var undoActions = PropertyPresets
                .Select(propertyPreset => propertyPreset.CreateUndoAction(hotspotRectangles))
                .ToArray();

            return () =>
            {
                foreach (var undoAction in undoActions)
                    undoAction();
            };
        }
    }
}
