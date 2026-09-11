using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace GromCore.Laser.Server.Utils
{
    public static class LocalizationHelper
    {
        private static Dictionary<string, Dictionary<string, Dictionary<string, string>>> _translations = new();
        public static void Load()
        {
            var files = Directory.GetFiles("Assets/Localization", "*.json");

            foreach (var file in files)
            {
                var langCode = Path.GetFileNameWithoutExtension(file);
                var json = File.ReadAllText(file);

                try
                {
                    var dict = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(json);

                    if (dict != null)
                        _translations[langCode] = dict;
                }
                catch (Exception ex)
                {
                    // Console.WriteLine($"[LocalizationHelper] ERROR {langCode}: {ex.Message}");
                }
            }

            // Console.WriteLine($"[LocalizationHelper] Loaded {_translations.Count} lang. packets.");
        }
        public static string Get(string langCode, string group, string key, params object[] args)
        {
            if (string.IsNullOrEmpty(langCode) || !_translations.TryGetValue(langCode, out var langDict))
            {
                if (!_translations.TryGetValue("en", out langDict))
                    return $"[{langCode}:{group}.{key}]";
            }

            if (!langDict.TryGetValue(group, out var valueDict))
                return $"[{langCode}:{group}.{key}]";

            if (!valueDict.TryGetValue(key, out string value))
                return $"[{langCode}:{group}.{key}]";

            try
            {
                return string.Format(value, args);
            }
            catch (FormatException)
            {
                return value;
            }
        }
        public static bool HasKey(string langCode, string group, string key)
        {
            if (_translations.TryGetValue(langCode, out var langDict) &&
                langDict.TryGetValue(group, out var valueDict) &&
                valueDict.ContainsKey(key))
            {
                return true;
            }

            return false;
        }
    }
}