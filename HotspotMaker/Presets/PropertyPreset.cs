using HotspotMaker.Hotspot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HotspotMaker.Presets
{
    public class PropertyPreset<TValue> : IPropertyPreset
    {
        // IPropertyPreset:
        IPropertyInfo IPropertyPreset.Property => Property;
        object? IPropertyPreset.Value => Value;


        public PropertyInfo<TValue> Property { get; }
        public PresetAction Action { get; }
        public TValue Value { get; }

        private EqualityComparer<TValue> Comparer { get; } = EqualityComparer<TValue>.Default;


        public PropertyPreset(PropertyInfo<TValue> property, PresetAction action, TValue value)
        {
            Property = property;
            Action = action;
            Value = value;
        }


        /// <summary>
        /// Returns an action that will apply this preset to the given hotspot rectangles.
        /// The returned action does not generate undoable actions when called.
        /// </summary>
        public Action CreateDoAction(IReadOnlyList<HotspotRectangleVM> hotspotRectangles)
        {
            var rectangles = hotspotRectangles.ToArray();
            switch (Action)
            {
                default:
                case PresetAction.SetValue: return CreateSetValueAction(rectangles);
                case PresetAction.CycleValue: return CreateCycleValueAction(rectangles);
                case PresetAction.InsertValue: return CreateInsertValueAction(rectangles);
            }
        }

        /// <summary>
        /// Captures the current property values of the given hotspot rectangles, and returns an action that will set their properties back to those values.
        /// The returned action does not generate undoable actions when called.
        /// </summary>
        public Action CreateUndoAction(IReadOnlyList<HotspotRectangleVM> hotspotRectangles)
        {
            var rectangles = hotspotRectangles.ToArray();
            var currentValues = rectangles.Select(Property.GetValue).ToArray();
            return () =>
            {
                for (int i = 0; i < rectangles.Length; i++)
                    Property.SetValue(rectangles[i], currentValues[i]);
            };
        }


        private Action CreateSetValueAction(IReadOnlyList<HotspotRectangleVM> hotspotRectangles)
        {
            var value = Value;
            return () =>
            {
                foreach (var hotspotRectangle in hotspotRectangles)
                    Property.SetValue(hotspotRectangle, value);
            };
        }

        private Action CreateCycleValueAction(IReadOnlyList<HotspotRectangleVM> hotspotRectangles)
        {
            if (!hotspotRectangles.Any() || !Property.PossibleValues.Any())
                return () => { };


            var nextValue = Value;
            if (HasSameValue(hotspotRectangles, out var currentValue))
                nextValue = GetNextValue(currentValue);

            return () =>
            {
                foreach (var hotspotRectangle in hotspotRectangles)
                    Property.SetValue(hotspotRectangle, nextValue);
            };
        }

        private Action CreateInsertValueAction(IReadOnlyList<HotspotRectangleVM> hotspotRectangles)
        {
            return () =>
            {
                foreach (var hotspotRectangle in hotspotRectangles)
                    Property.InsertValue(hotspotRectangle, Value);
            };
        }


        private bool HasSameValue(IReadOnlyList<HotspotRectangleVM> hotspotRectangles, out TValue value)
        {
            if (!hotspotRectangles.Any())
            {
                value = default!;
                return false;
            }

            var firstValue = Property.GetValue(hotspotRectangles[0]);
            for (int i = 1; i < hotspotRectangles.Count; i++)
            {
                var itemValue = Property.GetValue(hotspotRectangles[i]);
                if (!Comparer.Equals(itemValue, firstValue))
                {
                    value = default!;
                    return false;
                }
            }

            value = firstValue;
            return true;
        }

        private TValue GetNextValue(TValue value)
        {
            for (int i = 0; i < Property.PossibleValues.Length; i++)
            {
                if (Comparer.Equals(value, Property.PossibleValues[i]))
                    return Property.PossibleValues[(i + 1) % Property.PossibleValues.Length];
            }
            return Property.PossibleValues.FirstOrDefault() ?? value;
        }
    }
}
