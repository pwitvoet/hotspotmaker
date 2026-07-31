using HotspotMaker.Hotspot;
using System;
using System.Collections.Generic;

namespace HotspotMaker.Presets
{
    public enum PresetAction
    {
        SetValue,
        CycleValue,
        InsertValue,
    }


    public interface IPropertyPreset
    {
        IPropertyInfo Property { get; }
        PresetAction Action { get; }
        object? Value { get; }


        Action CreateDoAction(IReadOnlyList<HotspotRectangleVM> hotspotRectangles);

        Action CreateUndoAction(IReadOnlyList<HotspotRectangleVM> hotspotRectangles);
    }
}
