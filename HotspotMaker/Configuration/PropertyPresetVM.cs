using HotspotMaker.Presets;
using MLib.Texturing.Hotspotting;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HotspotMaker.Configuration
{
    public class PropertyPresetVM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));


        // Bindable properties:
        private IPropertyInfo _property;
        public IPropertyInfo Property
        {
            get => _property;
            set { _property = value; RaisePropertyChanged(); }
        }

        private PresetAction _action;
        public PresetAction Action
        {
            get => _action;
            set
            {
                _action = value;

                RaisePropertyChanged();
                RaisePropertyChanged(nameof(IsSetAction));
                RaisePropertyChanged(nameof(IsCycleAction));
                RaisePropertyChanged(nameof(IsInsertAction));
            }
        }

        private bool _boolValue;
        public bool BoolValue
        {
            get => _boolValue;
            set { _boolValue = value; RaisePropertyChanged(); }
        }

        private double _doubleValue;
        public double DoubleValue
        {
            get => _doubleValue;
            set { _doubleValue = value; RaisePropertyChanged(); }
        }

        private double? _nullableDoubleValue;
        public double? NullableDoubleValue
        {
            get => _nullableDoubleValue;
            set { _nullableDoubleValue = value; RaisePropertyChanged(); }
        }

        private HotspotLayout _hotspotLayoutValue;
        public HotspotLayout HotspotLayoutValue
        {
            get => _hotspotLayoutValue;
            set { _hotspotLayoutValue = value; RaisePropertyChanged(); }
        }

        private string[] _stringArrayValue;
        public string[] StringArrayValue
        {
            get => _stringArrayValue;
            set { _stringArrayValue = value; RaisePropertyChanged(); }
        }


        // Derived properties:
        public PresetAction[] AvailableActions => IsStringArrayProperty ? [PresetAction.SetValue, PresetAction.InsertValue] :
                            (IsBoolProperty || IsHotspotLayoutProperty) ? [PresetAction.SetValue, PresetAction.CycleValue] :
                                                                          [PresetAction.SetValue];

        public bool IsSetAction => Action == PresetAction.SetValue;

        public bool IsCycleAction => Action == PresetAction.CycleValue;

        public bool IsInsertAction => Action == PresetAction.InsertValue;


        // Read-only:
        public bool IsBoolProperty { get; }
        public bool IsDoubleProperty { get; }
        public bool IsNullableDoubleProperty { get; }
        public bool IsHotspotLayoutProperty { get; }
        public bool IsStringArrayProperty { get; }

        public static HotspotLayout[] AvailableHotspotLayoutValues { get; } = [HotspotLayout.Fit, HotspotLayout.Clip, HotspotLayout.Tile];


        public PropertyPresetVM(IPropertyInfo propertyInfo)
        {
            _action = PresetAction.SetValue;
            _property = propertyInfo;

            _boolValue = false;
            _doubleValue = 0;
            _nullableDoubleValue = null;
            _hotspotLayoutValue = HotspotLayout.Fit;
            _stringArrayValue = Array.Empty<string>();

            IsBoolProperty = propertyInfo is PropertyInfo<bool>;
            IsDoubleProperty = propertyInfo is PropertyInfo<double>;
            IsNullableDoubleProperty = propertyInfo is PropertyInfo<double?>;
            IsHotspotLayoutProperty = propertyInfo is PropertyInfo<HotspotLayout>;
            IsStringArrayProperty = propertyInfo is PropertyInfo<string[]>;
        }

        public PropertyPresetVM(IPropertyPreset propertyPreset)
            : this(propertyPreset.Property)
        {
            _action = propertyPreset.Action;

            _boolValue = (propertyPreset as PropertyPreset<bool>)?.Value ?? false;
            _doubleValue = (propertyPreset as PropertyPreset<double>)?.Value ?? 0;
            _nullableDoubleValue = (propertyPreset as PropertyPreset<double?>)?.Value;
            _hotspotLayoutValue = (propertyPreset as PropertyPreset<HotspotLayout>)?.Value ?? HotspotLayout.Fit;
            _stringArrayValue = (propertyPreset as PropertyPreset<string[]>)?.Value ?? Array.Empty<string>();
        }

        public IPropertyPreset GetPropertyPreset()
        {
            switch (Property)
            {
                case PropertyInfo<bool> boolProperty:
                    return new PropertyPreset<bool>(boolProperty, Action, BoolValue);

                case PropertyInfo<double> doubleProperty:
                    return new PropertyPreset<double>(doubleProperty, Action, DoubleValue);

                case PropertyInfo<double?> nullableDoubleProperty:
                    return new PropertyPreset<double?>(nullableDoubleProperty, Action, NullableDoubleValue);

                case PropertyInfo<HotspotLayout> hotspotLayoutProperty:
                    return new PropertyPreset<HotspotLayout>(hotspotLayoutProperty, Action, HotspotLayoutValue);

                case PropertyInfo<string[]> stringArrayProperty:
                    return new PropertyPreset<string[]>(stringArrayProperty, Action, StringArrayValue);

                default:
                    throw new NotImplementedException($"Support for property '{Property.Name}' has not been implemented yet.");
            }
        }
    }
}
