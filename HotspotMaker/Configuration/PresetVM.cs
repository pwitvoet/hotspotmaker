using HotspotMaker.Presets;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace HotspotMaker.Configuration
{
    public class PresetVM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));


        // Bindable properties:
        private string _description;
        public string Description
        {
            get => _description;
            set { _description = value; RaisePropertyChanged(); }
        }

        public ObservableCollection<PropertyPresetVM> PropertyPresets { get; } = new ObservableCollection<PropertyPresetVM>();

        private IPropertyInfo? _selectedNewProperty;
        public IPropertyInfo? SelectedNewProperty
        {
            get => _selectedNewProperty;
            set { _selectedNewProperty = value; RaisePropertyChanged(); }
        }

        private IReadOnlyList<IPropertyInfo> _availableProperties;
        public IReadOnlyList<IPropertyInfo> AvailableProperties
        {
            get => _availableProperties;
            set { _availableProperties = value; RaisePropertyChanged(); }
        }


        // Commands:
        public void AddPropertyPreset()
        {
            if (SelectedNewProperty == null)
                return;

            PropertyPresets.Add(new PropertyPresetVM(SelectedNewProperty));

            AvailableProperties = GetAvailableProperties();
            SelectedNewProperty = AvailableProperties.FirstOrDefault();
        }

        public void RemovePropertyPreset(PropertyPresetVM propertyPresetVM)
        {
            PropertyPresets.Remove(propertyPresetVM);

            AvailableProperties = GetAvailableProperties();
            SelectedNewProperty = AvailableProperties.FirstOrDefault();
        }


        public PresetVM(Preset preset)
        {
            _description = preset.Description;

            foreach (var propertyPreset in preset.PropertyPresets)
                PropertyPresets.Add(new PropertyPresetVM(propertyPreset));

            _availableProperties = GetAvailableProperties();
            _selectedNewProperty = _availableProperties.FirstOrDefault();
        }

        public Preset GetPreset()
        {
            return new Preset(Description, PropertyPresets.Select(propertyPresetVM => propertyPresetVM.GetPropertyPreset()));
        }


        private IPropertyInfo[] GetAvailableProperties()
        {
            return HotspotRectangleProperties.AllProperties
                .Where(propertyInfo => !PropertyPresets.Any(propertyPresetVM => propertyPresetVM.Property == propertyInfo))
                .ToArray();
        }
    }
}
