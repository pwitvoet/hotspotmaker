using HotspotMaker.History;
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


        public Action<UndoContext> CreateDoAction(IReadOnlyList<HotspotRectangleVM> hotspotRectangles)
        {
            var doActions = PropertyPresets
                .Select(propertyPreset => propertyPreset.CreateDoAction(hotspotRectangles))
                .ToArray();

            return context =>
            {
                foreach (var doAction in doActions)
                    doAction(context);
            };
        }

        public Action<UndoContext> CreateUndoAction(IReadOnlyList<HotspotRectangleVM> hotspotRectangles)
        {
            var undoActions = PropertyPresets
                .Select(propertyPreset => propertyPreset.CreateUndoAction(hotspotRectangles))
                .ToArray();

            return context =>
            {
                foreach (var undoAction in undoActions)
                    undoAction(context);
            };
        }
    }
}
