using Avalonia.Input;
using HotspotMaker.Presets;
using MLib.Texturing.Hotspotting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace HotspotMaker.Configuration
{
    public class Settings
    {
        public static Settings Load(string path)
        {
            using (var file = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var root = JsonSerializer.Deserialize<JsonObject>(file);
                if (root == null)
                    throw new InvalidDataException("Missing root object.");

                var settings = new Settings();

                var recentFilePathsArray = root["recent_file_paths"]?.AsArray();
                if (recentFilePathsArray != null)
                {
                    settings._recentFilePaths.Clear();
                    foreach (var item in recentFilePathsArray)
                    {
                        var filePath = (string?)item?.AsValue();
                        if (filePath != null)
                            settings._recentFilePaths.Add(filePath);
                    }
                }

                var keyBindingsArray = root["key_bindings"]?.AsArray();
                if (keyBindingsArray != null)
                {
                    settings._keyBindings.Clear();
                    foreach (var item in keyBindingsArray)
                    {
                        if (item == null)
                            continue;

                        var keyBinding = ParseKeyBinding(item.AsObject());
                        settings._keyBindings.Add(keyBinding);
                    }
                }

                var defaultPresetNode = root["default_preset"]?.AsObject();
                if (defaultPresetNode != null)
                {
                    settings._defaultPreset = ParsePreset(defaultPresetNode);
                }

                return settings;
            }
        }


        public const int MaxRecentFiles = 4;


        private List<KeyBinding> _keyBindings = new();
        public IReadOnlyList<KeyBinding> KeyBindings => _keyBindings;

        private Preset _defaultPreset;
        public Preset DefaultPreset => _defaultPreset;

        private List<string> _recentFilePaths = new();
        public IReadOnlyList<string> RecentFilePaths => _recentFilePaths;


        public Settings()
        {
            _keyBindings = GetDefaultKeyBindings().ToList();

            _defaultPreset = new Preset("Default", [
                CreatePropertyPreset(HotspotRectangleProperties.AllowRotationProperty, PresetAction.SetValue, true),
                CreatePropertyPreset(HotspotRectangleProperties.AllowHorizontalMirroringProperty, PresetAction.SetValue, true),
                CreatePropertyPreset(HotspotRectangleProperties.AllowVerticalMirroringProperty, PresetAction.SetValue, true)]);
        }

        public void Save(string path)
        {
            using (var file = File.Create(path))
            {
                var recentFilePathsArray = new JsonArray();
                foreach (var filePath in RecentFilePaths)
                    recentFilePathsArray.Add(filePath);

                var keyBindingsArray = new JsonArray();
                foreach (var keyBinding in KeyBindings)
                    keyBindingsArray.Add(ToJson(keyBinding));

                var root = new JsonObject();
                root["recent_file_paths"] = recentFilePathsArray;
                root["key_bindings"] = keyBindingsArray;
                root["default_preset"] = ToJson(DefaultPreset);

                JsonSerializer.Serialize(file, root);
            }
        }


        public void AddRecentFilePath(string filePath)
        {
            if (_recentFilePaths.Contains(filePath))
                _recentFilePaths.Remove(filePath);

            _recentFilePaths.Insert(0, filePath);

            while (_recentFilePaths.Count > MaxRecentFiles)
                _recentFilePaths.RemoveAt(_recentFilePaths.Count - 1);
        }

        public void UpdateKeyBindings(IEnumerable<KeyBinding> keyBindings)
        {
            _keyBindings.Clear();
            _keyBindings.AddRange(keyBindings);
        }

        public KeyBinding? GetKeyBinding(KeyGesture keyGesture)
            => KeyBindings.FirstOrDefault(keyBinding => keyBinding.KeyGesture == keyGesture);

        public KeyGesture? GetKeyGesture(EditorAction editorAction)
            => KeyBindings.FirstOrDefault(keyBinding => keyBinding.EditorAction == editorAction)?.KeyGesture;


        private static KeyBinding[] GetDefaultKeyBindings()
        {
            return [
                // Project:
                new KeyBinding(EditorAction.OpenWadFile, new KeyGesture(Key.O, KeyModifiers.Control)),
                new KeyBinding(EditorAction.SaveProject, new KeyGesture(Key.S, KeyModifiers.Control)),
                new KeyBinding(EditorAction.CloseProject, new KeyGesture(Key.W, KeyModifiers.Control)),
                new KeyBinding(EditorAction.ExitProgram, new KeyGesture(Key.F4, KeyModifiers.Alt)),

                // Undo/redo:
                new KeyBinding(EditorAction.Undo, new KeyGesture(Key.Z, KeyModifiers.Control)),
                new KeyBinding(EditorAction.Redo, new KeyGesture(Key.Y, KeyModifiers.Control)),

                // Editor view:
                new KeyBinding(EditorAction.ToggleGrid, new KeyGesture(Key.G)),
                new KeyBinding(EditorAction.IncreaseGridSize, new KeyGesture(Key.OemCloseBrackets)),
                new KeyBinding(EditorAction.DecreaseGridSize, new KeyGesture(Key.OemOpenBrackets)),
                new KeyBinding(EditorAction.ToggleCoordinatesDisplay, null),
                new KeyBinding(EditorAction.ToggleIconsDisplay, new KeyGesture(Key.I, KeyModifiers.Control)),
                new KeyBinding(EditorAction.ToggleRectanglesDisplay, new KeyGesture(Key.E, KeyModifiers.Control)),

                // Panels:
                new KeyBinding(EditorAction.ToggleTexturePanel, new KeyGesture(Key.T, KeyModifiers.Control)),
                new KeyBinding(EditorAction.ToggleRectanglePanel, new KeyGesture(Key.R, KeyModifiers.Control)),

                // Rectangle set:
                new KeyBinding(EditorAction.CreateNewRectangleSet, new KeyGesture(Key.N, KeyModifiers.Control | KeyModifiers.Shift)),

                // Labels:
                new KeyBinding(EditorAction.EditRectangleLabels, new KeyGesture(Key.F2)),
                new KeyBinding(EditorAction.AddRectangleLabel, new KeyGesture(Key.A, KeyModifiers.Shift)),
                new KeyBinding(EditorAction.RenameRectangleLabel, new KeyGesture(Key.E, KeyModifiers.Shift)),
                new KeyBinding(EditorAction.RemoveRectangleLabel, new KeyGesture(Key.R, KeyModifiers.Shift)),
                new KeyBinding(EditorAction.AddTextureLabel, new KeyGesture(Key.A, KeyModifiers.Control | KeyModifiers.Shift)),
                new KeyBinding(EditorAction.RenameTextureLabel, new KeyGesture(Key.E, KeyModifiers.Control | KeyModifiers.Shift)),
                new KeyBinding(EditorAction.RemoveTextureLabel, new KeyGesture(Key.R, KeyModifiers.Control | KeyModifiers.Shift)),

                // Selection and copy/paste/delete:
                new KeyBinding(EditorAction.Cut, new KeyGesture(Key.X, KeyModifiers.Control)),
                new KeyBinding(EditorAction.Copy, new KeyGesture(Key.C, KeyModifiers.Control)),
                new KeyBinding(EditorAction.Paste, new KeyGesture(Key.V, KeyModifiers.Control)),
                new KeyBinding(EditorAction.SelectAll, new KeyGesture(Key.A, KeyModifiers.Control)),
                new KeyBinding(EditorAction.Delete, new KeyGesture(Key.Delete)),

                // Move rectangles with arrow keys:
                new KeyBinding(EditorAction.MoveUp, new KeyGesture(Key.Up)),
                new KeyBinding(EditorAction.MoveRight, new KeyGesture(Key.Right)),
                new KeyBinding(EditorAction.MoveDown, new KeyGesture(Key.Down)),
                new KeyBinding(EditorAction.MoveLeft, new KeyGesture(Key.Left)),


                // Presets for toggling/cycling single properties:
                new KeyBinding(new KeyGesture(Key.R), new Preset("Toggle rotation", [
                    CreatePropertyPreset(HotspotRectangleProperties.AllowRotationProperty, PresetAction.CycleValue, false)])),

                new KeyBinding(new KeyGesture(Key.H), new Preset("Toggle horizontal mirroring", [
                    CreatePropertyPreset(HotspotRectangleProperties.AllowHorizontalMirroringProperty, PresetAction.CycleValue, false)])),

                new KeyBinding(new KeyGesture(Key.V), new Preset("Toggle vertical mirroring", [
                    CreatePropertyPreset(HotspotRectangleProperties.AllowVerticalMirroringProperty, PresetAction.CycleValue, false)])),

                new KeyBinding(new KeyGesture(Key.K), new Preset("Toggle horizontal layout", [
                    CreatePropertyPreset(HotspotRectangleProperties.HorizontalLayoutProperty, PresetAction.CycleValue, HotspotLayout.Fit)])),

                new KeyBinding(new KeyGesture(Key.L), new Preset("Toggle vertical layout", [
                    CreatePropertyPreset(HotspotRectangleProperties.VerticalLayoutProperty, PresetAction.CycleValue, HotspotLayout.Fit)])),

                new KeyBinding(new KeyGesture(Key.Up, KeyModifiers.Control), new Preset("Toggle top concave edge", [
                    CreatePropertyPreset(HotspotRectangleProperties.IsTopConcaveProperty, PresetAction.CycleValue, false)])),

                new KeyBinding(new KeyGesture(Key.Right, KeyModifiers.Control), new Preset("Toggle right concave edge", [
                    CreatePropertyPreset(HotspotRectangleProperties.IsRightConcaveProperty, PresetAction.CycleValue, false)])),

                new KeyBinding(new KeyGesture(Key.Down, KeyModifiers.Control), new Preset("Toggle bottom concave edge", [
                    CreatePropertyPreset(HotspotRectangleProperties.IsBottomConcaveProperty, PresetAction.CycleValue, false)])),

                new KeyBinding(new KeyGesture(Key.Left, KeyModifiers.Control), new Preset("Toggle left concave edge", [
                    CreatePropertyPreset(HotspotRectangleProperties.IsLeftConcaveProperty, PresetAction.CycleValue, false)])),


                // Presets that affect multiple properties, including labels:
                new KeyBinding(new KeyGesture(Key.D1), new Preset("Apply 'alt' label", [
                    CreatePropertyPreset(HotspotRectangleProperties.LabelsProperty, PresetAction.InsertValue, ["alt"])])),

                new KeyBinding(new KeyGesture(Key.D2), new Preset("Apply 'wall' preset", [
                    CreatePropertyPreset(HotspotRectangleProperties.LabelsProperty, PresetAction.InsertValue, ["wall"]),
                    CreatePropertyPreset(HotspotRectangleProperties.AllowRotationProperty, PresetAction.SetValue, false),
                    CreatePropertyPreset(HotspotRectangleProperties.AllowHorizontalMirroringProperty, PresetAction.SetValue, true),
                    CreatePropertyPreset(HotspotRectangleProperties.AllowVerticalMirroringProperty, PresetAction.SetValue, false)])),

                new KeyBinding(new KeyGesture(Key.D3), new Preset("Apply 'floor' preset", [
                    CreatePropertyPreset(HotspotRectangleProperties.LabelsProperty, PresetAction.InsertValue, ["floor"]),
                    CreatePropertyPreset(HotspotRectangleProperties.AllowRotationProperty, PresetAction.SetValue, true),
                    CreatePropertyPreset(HotspotRectangleProperties.AllowHorizontalMirroringProperty, PresetAction.SetValue, true),
                    CreatePropertyPreset(HotspotRectangleProperties.AllowVerticalMirroringProperty, PresetAction.SetValue, true)])),

                new KeyBinding(new KeyGesture(Key.D4), new Preset("Apply 'ceiling' preset", [
                    CreatePropertyPreset(HotspotRectangleProperties.LabelsProperty, PresetAction.InsertValue, ["ceiling"]),
                    CreatePropertyPreset(HotspotRectangleProperties.AllowRotationProperty, PresetAction.SetValue, true),
                    CreatePropertyPreset(HotspotRectangleProperties.AllowHorizontalMirroringProperty, PresetAction.SetValue, true),
                    CreatePropertyPreset(HotspotRectangleProperties.AllowVerticalMirroringProperty, PresetAction.SetValue, true)])),


                // Reset all properties:
                new KeyBinding(new KeyGesture(Key.X), new Preset("Reset properties", [
                    CreatePropertyPreset(HotspotRectangleProperties.AllowRotationProperty, PresetAction.SetValue, false),
                    CreatePropertyPreset(HotspotRectangleProperties.AllowHorizontalMirroringProperty, PresetAction.SetValue, false),
                    CreatePropertyPreset(HotspotRectangleProperties.AllowVerticalMirroringProperty, PresetAction.SetValue, false),
                    CreatePropertyPreset(HotspotRectangleProperties.HorizontalLayoutProperty, PresetAction.SetValue, HotspotLayout.Fit),
                    CreatePropertyPreset(HotspotRectangleProperties.VerticalLayoutProperty, PresetAction.SetValue, HotspotLayout.Fit),
                    CreatePropertyPreset(HotspotRectangleProperties.SnapWidthProperty, PresetAction.SetValue, null),
                    CreatePropertyPreset(HotspotRectangleProperties.SnapHeightProperty, PresetAction.SetValue, null),
                    CreatePropertyPreset(HotspotRectangleProperties.SelectionWeightProperty, PresetAction.SetValue, 1),
                    CreatePropertyPreset(HotspotRectangleProperties.IsTopConcaveProperty, PresetAction.SetValue, false),
                    CreatePropertyPreset(HotspotRectangleProperties.IsRightConcaveProperty, PresetAction.SetValue, false),
                    CreatePropertyPreset(HotspotRectangleProperties.IsBottomConcaveProperty, PresetAction.SetValue, false),
                    CreatePropertyPreset(HotspotRectangleProperties.IsLeftConcaveProperty, PresetAction.SetValue, false),
                    CreatePropertyPreset(HotspotRectangleProperties.LabelsProperty, PresetAction.SetValue, Array.Empty<string>())])),
            ];
        }

        private static PropertyPreset<TValue> CreatePropertyPreset<TValue>(PropertyInfo<TValue> property, PresetAction action, TValue value)
            => new PropertyPreset<TValue>(property, action, value);


        private static KeyBinding ParseKeyBinding(JsonObject json)
        {
            var editorAction = Enum.Parse<EditorAction>((string?)json["action"]?.AsValue() ?? "");
            var keyGestureString = (string?)json["key_gesture"]?.AsValue();
            var keyGesture = keyGestureString != null ? KeyGesture.Parse(keyGestureString) : null;

            var presetNode = json["preset"]?.AsObject();
            var preset = presetNode == null ? null : ParsePreset(presetNode);

            if (editorAction == EditorAction.ApplyPreset && preset != null)
                return new KeyBinding(keyGesture, preset);
            else
                return new KeyBinding(editorAction, keyGesture);
        }

        private static Preset ParsePreset(JsonObject json)
        {
            var description = (string?)json["description"]?.AsValue() ?? "";

            var propertyPresets = new List<IPropertyPreset>();
            var propertiesArray = json["properties"]?.AsArray();
            if (propertiesArray != null)
            {
                foreach (var item in propertiesArray)
                {
                    if (item != null)
                        propertyPresets.Add(ParsePropertyPreset(item.AsObject()));
                }
            }

            return new Preset(description, propertyPresets);
        }

        private static IPropertyPreset ParsePropertyPreset(JsonObject json)
        {
            var propertyName = (string?)json["property"]?.AsValue();
            var presetAction = Enum.Parse<PresetAction>((string?)json["action"]?.AsValue() ?? "");
            var valueNode = json["value"];

            var property = HotspotRectangleProperties.GetProperty(propertyName ?? "");
            switch (property)
            {
                case PropertyInfo<bool> boolProperty:
                    var boolValue = (bool?)valueNode?.AsValue() ?? false;
                    return new PropertyPreset<bool>(boolProperty, presetAction, boolValue);

                case PropertyInfo<double> doubleProperty:
                    var doubleValue = (double?)valueNode?.AsValue() ?? 0;
                    return new PropertyPreset<double>(doubleProperty, presetAction, doubleValue);

                case PropertyInfo<double?> nullableDoubleProperty:
                    var nullableDoubleValue = valueNode != null ? (double?)valueNode.AsValue() : null;
                    return new PropertyPreset<double?>(nullableDoubleProperty, presetAction, nullableDoubleValue);

                case PropertyInfo<HotspotLayout> hotspotLayoutProperty:
                    var hotspotLayoutValue = Enum.Parse<HotspotLayout>((string?)valueNode?.AsValue() ?? "");
                    return new PropertyPreset<HotspotLayout>(hotspotLayoutProperty, presetAction, hotspotLayoutValue);

                case PropertyInfo<string[]> stringArrayProperty:
                    var valuesArray = json["value"]?.AsArray();
                    var stringArrayValue = valuesArray
                        ?.Select(item => (string?)item?.AsValue() ?? "")
                        .Where(str => !string.IsNullOrEmpty(str))
                        .ToArray();
                    return new PropertyPreset<string[]>(stringArrayProperty, presetAction, stringArrayValue ?? Array.Empty<string>());

                default:
                    throw new InvalidDataException($"Unknown property '{propertyName}'.");
            }
        }


        private static JsonObject ToJson(KeyBinding keyBinding)
        {
            var json = new JsonObject();
            json["action"] = keyBinding.EditorAction.ToString();

            if (keyBinding.KeyGesture != null)
                json["key_gesture"] = keyBinding.KeyGesture.ToString();

            if (keyBinding.Preset != null)
                json["preset"] = ToJson(keyBinding.Preset);

            return json;
        }

        private static JsonObject ToJson(Preset preset)
        {
            var propertiesArray = new JsonArray();
            foreach (var property in preset.PropertyPresets)
                propertiesArray.Add(ToJson(property));

            var json = new JsonObject();
            json["description"] = preset.Description;
            json["properties"] = propertiesArray;
            return json;
        }

        private static JsonObject ToJson(IPropertyPreset propertyPreset)
        {
            var json = new JsonObject();
            json["property"] = propertyPreset.Property.Name;
            json["action"] = propertyPreset.Action.ToString();
            json["value"] = ToJson(propertyPreset.Value);
            return json;
        }

        private static JsonNode? ToJson(object? value)
        {
            switch (value)
            {
                case null: return null;
                case double number: return number;
                case bool boolean: return boolean;
                default: return value.ToString();

                case string[] array:
                    var jsonArray = new JsonArray();
                    foreach (var item in array)
                        jsonArray.Add(item);
                    return jsonArray;
            }
        }
    }
}
