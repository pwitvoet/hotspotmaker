using System.Collections.Generic;
using System.IO;
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
                    foreach (var item in recentFilePathsArray)
                    {
                        var filePath = (string?)item?.AsValue();
                        if (filePath != null)
                            settings._recentFilePaths.Add(filePath);
                    }
                }

                return settings;
            }
        }


        public const int MaxRecentFiles = 4;


        private List<string> _recentFilePaths = new();
        public IReadOnlyList<string> RecentFilePaths => _recentFilePaths;


        public Settings()
        {
        }

        public void Save(string path)
        {
            using (var file = File.Create(path))
            {
                var recentFilePathsArray = new JsonArray();
                foreach (var filePath in RecentFilePaths)
                    recentFilePathsArray.Add(filePath);

                var root = new JsonObject();
                root["recent_file_paths"] = recentFilePathsArray;

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
    }
}
