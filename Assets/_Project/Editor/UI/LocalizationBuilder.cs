using System.Collections.Generic;
using System.IO;
using System.Linq;
using BilliardGame.UI;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace BilliardGame.Editor.UI
{
    /// <summary>
    /// strings.psv (key, uz, ru, en) → Unity Localization string table collection "UI". The file is the reviewable source;
    /// table assets are regenerated from it. Builders also read it to put the Uzbek text into labels at edit time.
    /// </summary>
    internal static class LocalizationBuilder
    {
        public const string SourcePath = "Assets/_Project/Localization/strings.psv";
        private const string TableDir = "Assets/_Project/Localization/Tables";
        private const string SettingsPath = "Assets/_Project/Localization/LocalizationSettings.asset";
        private const string LocaleDir = "Assets/_Project/Localization/Locales";

        private static Dictionary<string, string[]> rows;

        /// <summary>Uzbek text for a key (edit-time preview of labels; runtime uses <see cref="Loc"/>).</summary>
        public static string Uz(string key)
        {
            Dictionary<string, string[]> table = Rows();
            if (table.TryGetValue(key, out string[] values)) return values[0];
            Debug.LogWarning($"[Loc] Missing key '{key}' in strings.psv");
            return key;
        }

        public static bool Has(string key) => Rows().ContainsKey(key);

        public static void Reload() => rows = null;

        public static Dictionary<string, string[]> Rows()
        {
            if (rows != null) return rows;
            rows = new Dictionary<string, string[]>();
            int line = 0;
            foreach (string raw in File.ReadAllLines(SourcePath))
            {
                line++;
                if (string.IsNullOrWhiteSpace(raw) || raw.StartsWith("#")) continue;
                string[] cells = raw.Split('|');
                if (cells.Length < 4)
                {
                    Debug.LogError($"[Loc] strings.psv:{line} needs 4 columns (key, uz, ru, en): {raw}");
                    continue;
                }

                if (rows.ContainsKey(cells[0])) Debug.LogError($"[Loc] strings.psv:{line} duplicate key {cells[0]}");
                rows[cells[0]] = new[] { Unescape(cells[1]), Unescape(cells[2]), Unescape(cells[3]) };
            }

            return rows;
        }

        private static string Unescape(string cell) => cell.Replace("\\n", "\n").Trim();

        [MenuItem("BilliardGame/UI/Build Localization")]
        public static void Build()
        {
            Reload();
            LocalizationSettings settings = EnsureSettings();
            var locales = new List<Locale>();
            foreach (string code in Loc.Languages) locales.Add(EnsureLocale(code));

            StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection(Loc.Table)
                ?? LocalizationEditorSettings.CreateStringTableCollection(Loc.Table, TableDir, locales);
            foreach (Locale locale in locales)
            {
                if (collection.GetTable(locale.Identifier) == null) collection.AddNewTable(locale.Identifier);
            }

            Dictionary<string, string[]> source = Rows();
            // Drop keys that left the file so the table never serves stale text.
            foreach (SharedTableData.SharedTableEntry entry in collection.SharedData.Entries.ToList())
            {
                if (!source.ContainsKey(entry.Key)) collection.RemoveEntry(entry.Key);
            }

            for (int i = 0; i < locales.Count; i++)
            {
                var table = (StringTable)collection.GetTable(locales[i].Identifier);
                foreach (KeyValuePair<string, string[]> row in source)
                {
                    string value = string.IsNullOrEmpty(row.Value[i]) ? row.Value[2] : row.Value[i];
                    table.AddEntry(row.Key, value);
                }

                EditorUtility.SetDirty(table);
            }

            EditorUtility.SetDirty(collection.SharedData);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log($"[BilliardGame] Localization: {source.Count} keys × {locales.Count} languages");
        }

        private static LocalizationSettings EnsureSettings()
        {
            LocalizationSettings settings = LocalizationEditorSettings.ActiveLocalizationSettings;
            if (settings == null)
            {
                BallMaterialFactory.EnsureFolder(Path.GetDirectoryName(SettingsPath).Replace('\\', '/'));
                settings = ScriptableObject.CreateInstance<LocalizationSettings>();
                settings.name = "LocalizationSettings";
                AssetDatabase.CreateAsset(settings, SettingsPath);
                LocalizationEditorSettings.ActiveLocalizationSettings = settings;
            }

            // Saved Settings choice first, else Uzbek whatever the device language is (no system-locale selector).
            // "-language=ru" on the command line stays for dev/E2E runs.
            List<IStartupLocaleSelector> selectors = settings.GetStartupLocaleSelectors();
            selectors.Clear();
            selectors.Add(new CommandLineLocaleSelector());
            selectors.Add(new PlayerPrefLocaleSelector { PlayerPreferenceKey = Loc.LanguageKey });
            selectors.Add(new SpecificLocaleSelector { LocaleId = new LocaleIdentifier(Loc.DefaultLanguage) });
            return settings;
        }

        private static Locale EnsureLocale(string code)
        {
            Locale existing = LocalizationEditorSettings.GetLocale(code);
            if (existing != null) return existing;
            BallMaterialFactory.EnsureFolder(LocaleDir);
            Locale locale = Locale.CreateLocale(new LocaleIdentifier(code));
            AssetDatabase.CreateAsset(locale, $"{LocaleDir}/{code}.asset");
            LocalizationEditorSettings.AddLocale(locale, false);
            return locale;
        }
    }
}
