using System;
using System.Collections.Generic;
using System.IO;

namespace BilliardGame.Editor.Art
{
    /// <summary>One <c>## slug</c> block of ArtSource/LICENSES.md (docs/ART_PIPELINE.md §4).</summary>
    internal sealed class LicenseEntry
    {
        public LicenseEntry(string slug) => Slug = slug;

        public string Slug { get; }
        public Dictionary<string, string> Fields { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public string this[string key] => Fields.TryGetValue(key, out string value) ? value : string.Empty;
        public string Type => this["type"];
        public string License => this["license"];
    }

    /// <summary>Parses and checks the license registry; the import tool and the build refuse anything it rejects.</summary>
    internal static class LicenseRegistry
    {
        public const string RegistryPath = "ArtSource/LICENSES.md";

        private static readonly HashSet<string> AllowedLicenses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "CC0", "CC-BY-4.0", "Sketchfab-Standard", "CGTrader-RF", "OFL",
        };

        /// <summary>Registry type → folder under Assets/_Project/Art.</summary>
        public static readonly Dictionary<string, string> TypeFolders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "table", "Tables" }, { "cue", "Cues" }, { "balls", "BallSets" }, { "venue", "Venues" }, { "avatar", "Avatars" },
            { "outfit", "Outfits" }, { "prop", "Props" }, { "texture", "Textures" }, { "hdri", "Hdri" }, { "font", "Fonts" },
        };

        public static Dictionary<string, LicenseEntry> Load() => Parse(File.Exists(RegistryPath) ? File.ReadAllText(RegistryPath) : string.Empty);

        public static Dictionary<string, LicenseEntry> Parse(string markdown)
        {
            var entries = new Dictionary<string, LicenseEntry>(StringComparer.OrdinalIgnoreCase);
            LicenseEntry current = null;
            foreach (string raw in markdown.Split('\n'))
            {
                string line = raw.Trim();
                if (line.StartsWith("## ", StringComparison.Ordinal))
                {
                    current = new LicenseEntry(line.Substring(3).Trim());
                    entries[current.Slug] = current;
                    continue;
                }

                if (current == null || !line.StartsWith("- ", StringComparison.Ordinal)) continue;
                int colon = line.IndexOf(':');
                if (colon < 0) continue;
                string key = line.Substring(2, colon - 2).Trim();
                string value = line.Substring(colon + 1);
                int comment = value.IndexOf(" #", StringComparison.Ordinal);
                if (comment >= 0) value = value.Substring(0, comment);
                current.Fields[key] = value.Trim();
            }

            return entries;
        }

        /// <summary>Null when the entry may be imported, otherwise the reason it may not.</summary>
        public static string Reject(LicenseEntry entry)
        {
            if (entry == null) return "no entry in " + RegistryPath;
            if (!AllowedLicenses.Contains(entry.License)) return $"license '{entry.License}' is not allowed (NC/ND/SA/Editorial/unknown are banned)";
            if (!TypeFolders.ContainsKey(entry.Type)) return $"unknown type '{entry.Type}'";
            foreach (string required in new[] { "name", "source_url", "author", "attribution" })
            {
                if (string.IsNullOrEmpty(entry[required])) return $"missing '{required}'";
            }

            return null;
        }
    }
}
