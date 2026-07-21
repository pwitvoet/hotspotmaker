using HotspotMaker.History;
using MLib.Texturing.Hotspotting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace HotspotMaker.Hotspot.Linking
{
    public class MultiLinkWindowVM : INotifyPropertyChanged, INotifyDataErrorInfo
    {
        // INotifyPropertyChanged:
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));


        // INotifyDataErrorInfo:
        public bool HasErrors => GetErrors(null).Any();

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;
        protected void RaiseErrorsChanged([CallerMemberName] string? propertyName = null)
            => ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));

        IEnumerable INotifyDataErrorInfo.GetErrors(string? propertyName) => GetErrors(propertyName);

        public IEnumerable<string> GetErrors(string? propertyName)
        {
            var getAllErrors = string.IsNullOrEmpty(propertyName);

            if (getAllErrors || propertyName == nameof(TextureNamePattern))
            {
                if (string.IsNullOrEmpty(TextureNamePattern))
                    yield return "Texture name pattern must not be empty.";
            }

            if (getAllErrors || propertyName == nameof(NewHotspotRectangleSetName))
            {
                if (IsNewHotspotRectangleSet && HotspotRectangleSets.Any(rectangleSet => rectangleSet.Name == NewHotspotRectangleSetName))
                    yield return "A rectangle set with this name already exists.";
            }
        }


        // Bindable properties:
        private string _textureNamePattern = "";
        public string TextureNamePattern
        {
            get => _textureNamePattern;
            set
            {
                _textureNamePattern = value;

                RaisePropertyChanged();

                UpdateAffectedTextureNames();
                RaiseErrorsChanged();
            }
        }

        private string _fallbackTextureNamePattern = "";
        public string FallbackTextureNamePattern
        {
            get => _fallbackTextureNamePattern;
            set { _fallbackTextureNamePattern = value; RaisePropertyChanged(); }
        }

        private HotspotRectangleSetVM _selectedHotspotRectangleSet = NewHotspotRectangleSetPlaceholder;
        public HotspotRectangleSetVM SelectedHotspotRectangleSet
        {
            get => _selectedHotspotRectangleSet;
            set
            {
                _selectedHotspotRectangleSet = value;

                RaisePropertyChanged(nameof(IsNewHotspotRectangleSet));
                RaisePropertyChanged();

                RaiseErrorsChanged();
            }
        }

        private string _newHotspotRectangleSetName = "new_rectangle_set";
        public string NewHotspotRectangleSetName
        {
            get => _newHotspotRectangleSetName;
            set
            {
                _newHotspotRectangleSetName = value;

                RaisePropertyChanged();

                RaiseErrorsChanged();
            }
        }

        private string[] _affectedTextureNames = Array.Empty<string>();
        public string[] AffectedTextureNames
        {
            get => _affectedTextureNames;
            set { _affectedTextureNames = value; RaisePropertyChanged(); }
        }


        // Derived properties:
        public bool IsNewHotspotRectangleSet => SelectedHotspotRectangleSet == NewHotspotRectangleSetPlaceholder;


        // Read-only:
        public IReadOnlyList<HotspotRectangleSetVM> HotspotRectangleSets { get; }

        public static HotspotRectangleSetVM NewHotspotRectangleSetPlaceholder { get; } = new HotspotRectangleSetVM("Create new rectangle set", new UndoSystem());


        private IReadOnlyList<TextureInfoVM> TextureInfos { get; }
        private IReadOnlyList<HotspotBindingVM> HotspotBindings { get; }


        public MultiLinkWindowVM(string textureNamePattern, IReadOnlyList<TextureInfoVM> textureInfos, IReadOnlyList<HotspotBindingVM> hotspotBindings, IReadOnlyList<HotspotRectangleSetVM> hotspotRectangleSets)
        {
            _textureNamePattern = textureNamePattern;
            TextureInfos = textureInfos;
            HotspotBindings = hotspotBindings;
            HotspotRectangleSets = hotspotRectangleSets.Prepend(NewHotspotRectangleSetPlaceholder).ToArray();

            UpdateAffectedTextureNames();
        }


        private void UpdateAffectedTextureNames()
        {
            Func<TextureInfoVM, bool> predicate;
            var hasWildcards = HotspotNameMatching.HasWildcards(TextureNamePattern);
            if (hasWildcards)
            {
                var regex = HotspotNameMatching.MakeNamePatternRegex(TextureNamePattern);
                predicate = textureInfo => regex.IsMatch(textureInfo.Name);
            }
            else
            {
                predicate = textureInfo => string.Equals(textureInfo.Name, TextureNamePattern, StringComparison.InvariantCultureIgnoreCase);
            }

            AffectedTextureNames = TextureInfos
                .Where(textureInfo => textureInfo.Binding == null)
                .Where(predicate)
                .Select(textureInfo => textureInfo.Name)
                .ToArray();
        }
    }
}
