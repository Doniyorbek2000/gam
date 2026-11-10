using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace BilliardGame.Editor.Art
{
    /// <summary>
    /// Bakes the Montserrat TMP fonts as Static atlases: every uz / ru / en character (and common nickname letters) is
    /// rasterised once here, so Play mode and builds never add glyphs or font features and never dirty the assets.
    /// Re-run after strings.psv gains new characters (Build Localization calls it and skips when nothing changed).
    /// </summary>
    internal static class FontBake
    {
        private const string FontDir = "Assets/_Project/Art/Fonts/font-montserrat";
        private const string LocalizationFile = "Assets/_Project/Localization/strings.psv";
        private const int AtlasSize = 2048;
        private static readonly string[] Weights = { "Montserrat-Bold SDF", "Montserrat-SemiBold SDF", "Montserrat-Medium SDF" };

        // Fixed ranges: ASCII, Latin-1 + Latin Extended-A (é ü ş ğ in nicknames), Uzbek ʻ ʼ, Cyrillic + Cyrillic
        // supplement used by Uzbek / Kazakh / Tajik (Ўў Ққ Ғғ Ҳҳ ...), punctuation and symbols the UI uses.
        private static readonly (int from, int to)[] Ranges =
        {
            (0x20, 0x7E), (0xA0, 0x17F), (0x2BB, 0x2BC), (0x400, 0x45F), (0x490, 0x4FF), (0x2010, 0x2027), (0x2030, 0x203A),
            (0x2116, 0x2116), (0x2190, 0x2193), (0x2212, 0x2212), (0x2248, 0x2248), (0x25B2, 0x25C0),
        };

        [MenuItem("BilliardGame/UI/Bake Static Fonts")]
        public static void BakeMenu() => BakeAll(true);

        /// <summary>Bakes each weight whose baked character set differs from the current one (or all with force).</summary>
        public static void BakeAll(bool force)
        {
            string charset = Charset();
            foreach (string weight in Weights)
            {
                string path = $"{FontDir}/{weight}.asset";
                var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                if (font == null) throw new FileNotFoundException("TMP font asset missing", path);
                if (!force && font.atlasPopulationMode == AtlasPopulationMode.Static && font.creationSettings.characterSequence == charset) continue;
                Bake(font, charset);
            }

            AssetDatabase.SaveAssets();
        }

        private static void Bake(TMP_FontAsset font, string charset)
        {
            var serialized = new SerializedObject(font);
            serialized.FindProperty("m_AtlasWidth").intValue = AtlasSize;
            serialized.FindProperty("m_AtlasHeight").intValue = AtlasSize;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // Static assets refuse new glyphs, so fill in Dynamic mode, then lock.
            font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            font.ClearFontAssetData(false);
            uint[] unicodes = charset.Select(c => (uint)c).ToArray();
            font.TryAddCharacters(unicodes, out uint[] missing, true);
            PruneFeatures(font);
            font.atlasPopulationMode = AtlasPopulationMode.Static;
            FontAssetCreationSettings settings = font.creationSettings;
            settings.characterSequence = charset;
            font.creationSettings = settings;
            // The property is internal in this TMP version; the serialized flag is not.
            var locked = new SerializedObject(font);
            locked.FindProperty("m_ClearDynamicDataOnBuild").boolValue = false;
            locked.ApplyModifiedPropertiesWithoutUndo();

            for (int i = 0; i < font.atlasTextures.Length; i++)
            {
                Texture2D atlas = font.atlasTextures[i];
                atlas.name = i == 0 ? font.name + " Atlas" : $"{font.name} Atlas {i}";
                if (!AssetDatabase.Contains(atlas)) AssetDatabase.AddObjectToAsset(atlas, font);
                // No CPU copy at runtime: the static atlas is only sampled by the GPU.
                var texture = new SerializedObject(atlas);
                texture.FindProperty("m_IsReadable").boolValue = false;
                texture.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorUtility.SetDirty(font);
            long used = font.glyphTable.Sum(g => (long)(g.glyphRect.width + 2 * font.atlasPadding) * (g.glyphRect.height + 2 * font.atlasPadding));
            string absent = missing == null || missing.Length == 0 ? "none" : string.Join(" ", missing.Select(u => "U+" + u.ToString("X4")));
            Debug.Log($"[Fonts] {font.name}: {font.characterTable.Count} chars, {font.fontFeatureTable.glyphPairAdjustmentRecords.Count} kerning pairs, {font.atlasTextures.Length} atlas {AtlasSize}², ~{100 * used / ((long)AtlasSize * AtlasSize * font.atlasTextures.Length)}% filled; not in the font: {absent}");
        }

        /// <summary>
        /// Montserrat kerns nearly every pair of the 566 baked glyphs (~140 MB of YAML per weight, over GitHub's file
        /// limit). Keeps kerning between ASCII and UI-string letters only; mark attachment is dropped because the UI
        /// uses precomposed letters (oʻ uses the spacing ʻ, not a combining mark).
        /// </summary>
        private static void PruneFeatures(TMP_FontAsset font)
        {
            var letters = new HashSet<char>(File.ReadAllText(LocalizationFile, Encoding.UTF8));
            for (char c = (char)0x20; c <= 0x7E; c++) letters.Add(c);
            var kerned = new HashSet<uint>(font.characterTable.Where(c => letters.Contains((char)c.unicode)).Select(c => c.glyphIndex));
            TMP_FontFeatureTable table = font.fontFeatureTable;
            table.glyphPairAdjustmentRecords.RemoveAll(r =>
                !kerned.Contains(r.firstAdjustmentRecord.glyphIndex) || !kerned.Contains(r.secondAdjustmentRecord.glyphIndex));
            table.MarkToBaseAdjustmentRecords.Clear();
            table.MarkToMarkAdjustmentRecords.Clear();
            font.ReadFontAssetDefinition();
        }

        /// <summary>Fixed ranges plus every character in the UI strings and in code literals, sorted.</summary>
        private static string Charset()
        {
            var set = new SortedSet<char>();
            foreach ((int from, int to) in Ranges)
            {
                for (int c = from; c <= to; c++) set.Add((char)c);
            }

            var sources = new List<string> { LocalizationFile };
            sources.AddRange(Directory.GetFiles("Assets/_Project/Scripts", "*.cs", SearchOption.AllDirectories));
            foreach (string file in sources)
            {
                foreach (char c in File.ReadAllText(file, Encoding.UTF8))
                {
                    if (c >= 0x20 && !char.IsSurrogate(c) && c != 0xFEFF) set.Add(c);
                }
            }

            return new string(set.ToArray());
        }
    }
}
