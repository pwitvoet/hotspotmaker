using HotspotMaker.Hotspot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HotspotMaker.Presets
{
    public class PropertyInfo<TValue> : IPropertyInfo
    {
        public string Name { get; }
        public Func<HotspotRectangleVM, TValue> GetValue { get; }
        public Action<HotspotRectangleVM, TValue> SetValue { get; }
        public Action<HotspotRectangleVM, TValue> InsertValue { get; }

        public TValue[] PossibleValues { get; }


        public PropertyInfo(
            string name,
            Func<HotspotRectangleVM, TValue> getValue,
            Action<HotspotRectangleVM, TValue> setValue,
            Action<HotspotRectangleVM, TValue>? insertValue = null,
            IEnumerable<TValue>? possibleValues = null)
        {
            Name = name;
            GetValue = getValue;
            SetValue = setValue;
            InsertValue = insertValue ?? ((r, v) => { });

            PossibleValues = possibleValues?.ToArray() ?? Array.Empty<TValue>();
        }
    }
}
