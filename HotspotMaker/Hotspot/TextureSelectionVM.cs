using HotspotMaker.History;
using HotspotMaker.Presets;
using HotspotMaker.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace HotspotMaker.Hotspot
{
    public class TextureSelectionVM : ChangeTrackingVM
    {
        public event Action<TextureInfoVM[], TextureInfoVM[]>? SelectionChanged;
        protected void RaiseSelectionChanged(TextureInfoVM[] deselected, TextureInfoVM[] selected)
            => SelectionChanged?.Invoke(deselected, selected);


        // Bindable properties:
        private ObservableCollection<TextureInfoVM> _textures = new();
        public IEnumerable<TextureInfoVM> Textures => _textures;


        // Multi-selection property editing:
        public NullableMultiValue<HotspotRectangleSetVM?> HotspotRectangleSet { get; }
        public NullableMultiValue<string?> FallbackTextureNamePattern { get; }
        public NullableMultiValue<double?> FallbackScoreThreshold { get; }
        public NullableMultiValue<string[]> Labels { get; }


        // Derived properties:
        public bool IsEmpty => _textures.Count == 0;

        public bool IsSingleSelection => _textures.Count == 1;

        public bool IsMultiSelection => _textures.Count > 1;

        public TextureInfoVM? SingleTexture => _textures.Count == 1 ? _textures[0] : null;

        public int SelectionCount => _textures.Count;

        public bool HasLabels => Labels.HasMultipleValues || Labels.Value?.Any() == true;


        public TextureSelectionVM(UndoSystem undoSystem)
            : base(undoSystem)
        {
            _textures.CollectionChanged += Textures_CollectionChanged;

            HotspotRectangleSet = new NullableMultiValue<HotspotRectangleSetVM?>(value => SetMultiProperty(value, r => r.HotspotRectangleSet, (r, v) => r.HotspotRectangleSet = v));
            FallbackTextureNamePattern = new NullableMultiValue<string?>(value => SetMultiPropertyOngoing(value, r => r.FallbackTextureNamePattern, (r, v) => r.FallbackTextureNamePattern = v, nameof(FallbackTextureNamePattern)));
            FallbackScoreThreshold = new NullableMultiValue<double?>(value => SetMultiPropertyOngoing(value, r => r.FallbackScoreThreshold, (r, v) => r.FallbackScoreThreshold = v, nameof(FallbackScoreThreshold)));
            Labels = new NullableMultiValue<string[]>(value => SetMultiPropertyOngoing(value, r => r.Labels, (r, v) => r.Labels = v, nameof(Labels)), HotspotRectangleProperties.GetLabelsEqualityComparer());
        }

        private void Textures_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            StopOngoingAction();

            UpdateMultiProperties();

            var deselectedTextures = e.OldItems?.OfType<TextureInfoVM>().ToArray() ?? Array.Empty<TextureInfoVM>();
            foreach (var textureVM in deselectedTextures)
                textureVM.PropertyChanged -= TextureVM_PropertyChanged;

            var selectedTextures = e.NewItems?.OfType<TextureInfoVM>().ToArray() ?? Array.Empty<TextureInfoVM>();
            foreach (var textureVM in selectedTextures)
                textureVM.PropertyChanged += TextureVM_PropertyChanged;

            RaiseSelectionChanged(deselectedTextures, selectedTextures);

            RaisePropertyChanged(nameof(IsEmpty));
            RaisePropertyChanged(nameof(IsSingleSelection));
            RaisePropertyChanged(nameof(IsMultiSelection));
            RaisePropertyChanged(nameof(SingleTexture));
            RaisePropertyChanged(nameof(SelectionCount));
        }

        private void TextureVM_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(TextureInfoVM.HotspotRectangleSet): UpdateHotspotRectangleSet(); break;
                case nameof(TextureInfoVM.FallbackTextureNamePattern): UpdateFallbackTextureNamePattern(); break;
                case nameof(TextureInfoVM.FallbackScoreThreshold): UpdateFallbackScoreThreshold(); break;
                case nameof(TextureInfoVM.Labels): UpdateLabels(); break;
            }
        }


        // TODO: This should mark all affected textures as modified! Maybe give undoable actions a unique ID, and give each object a last-modified-by-id property?
        private void SetMultiProperty<TValue>(TValue newValue, Func<TextureInfoVM, TValue> getValue, Action<TextureInfoVM, TValue> setValue)
        {
            var selectedTextures = Textures.ToArray();
            var originalValues = selectedTextures.Select(getValue).ToArray();

            PerformUndoableAction(
                () =>
                {
                    for (int i = 0; i < selectedTextures.Length; i++)
                        setValue(selectedTextures[i], newValue);
                },
                () =>
                {
                    for (int i = 0; i < selectedTextures.Length; i++)
                        setValue(selectedTextures[i], originalValues[i]);
                });
        }

        // TODO: Same as above - mark all affected textures as modified!
        private void SetMultiPropertyOngoing<TValue>(TValue newValue, Func<TextureInfoVM, TValue> getValue, Action<TextureInfoVM, TValue> setValue, string propertyName)
        {
            var selectedTextures = Textures.ToArray();
            var originalValues = selectedTextures.Select(getValue).ToArray();

            PerformUndoableActionOngoing(
                propertyName,
                () =>
                {
                    for (int i = 0; i < selectedTextures.Length; i++)
                        setValue(selectedTextures[i], newValue);
                },
                () =>
                {
                    for (int i = 0; i < selectedTextures.Length; i++)
                        setValue(selectedTextures[i], originalValues[i]);
                });
        }

        // TODO: Same as above - mark all affected textures as modified!
        private void SetMultiPropertyOngoing<TValue>(TValue? newValue, Func<TextureInfoVM, TValue?> getValue, Action<TextureInfoVM, TValue?> setValue, string propertyName)
            where TValue : struct
        {
            var selectedTextures = Textures.ToArray();
            var originalValues = selectedTextures.Select(getValue).ToArray();

            PerformUndoableActionOngoing(
                propertyName,
                () =>
                {
                    for (int i = 0; i < selectedTextures.Length; i++)
                        setValue(selectedTextures[i], newValue);
                },
                () =>
                {
                    for (int i = 0; i < selectedTextures.Length; i++)
                        setValue(selectedTextures[i], originalValues[i]);
                });
        }

        private void UpdateMultiProperties()
        {
            UpdateHotspotRectangleSet();
            UpdateFallbackTextureNamePattern();
            UpdateFallbackScoreThreshold();
            UpdateLabels();
        }

        private void UpdateMultiProperty<TValue>(NullableMultiValue<TValue> multiValue, Func<TextureInfoVM, TValue> getValue)
        {
            if (_textures.Count == 0)
            {
                // NOTE: No value actually, but the UI shouldn't display anything, so this should be OK.
                multiValue.SetMultiValue();
            }
            else if (_textures.Count == 1)
            {
                multiValue.SetSingleValue(getValue(_textures[0]));
            }
            else
            {
                var firstValue = getValue(_textures[0]);
                var comparer = EqualityComparer<TValue>.Default;
                var hasMultipleValues = _textures.Any(rectangleVM => !multiValue.Comparer.Equals(getValue(rectangleVM), firstValue));

                if (hasMultipleValues)
                    multiValue.SetMultiValue();
                else
                    multiValue.SetSingleValue(firstValue);
            }
        }

        private void UpdateHotspotRectangleSet() => UpdateMultiProperty(HotspotRectangleSet, t => t.HotspotRectangleSet);
        private void UpdateFallbackTextureNamePattern() => UpdateMultiProperty(FallbackTextureNamePattern, t => t.FallbackTextureNamePattern);
        private void UpdateFallbackScoreThreshold() => UpdateMultiProperty(FallbackScoreThreshold, t => t.FallbackScoreThreshold);

        private void UpdateLabels()
        {
            UpdateMultiProperty(Labels, t => t.Labels);
            RaisePropertyChanged(nameof(HasLabels));
        }
    }
}
