using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace BilliardGame.Editor.Art
{
    /// <summary>Copies OFL fonts and makes a dynamic TextMeshPro SDF asset per weight (Latin + Cyrillic fill in on demand).</summary>
    internal static class FontImport
    {
        public static void Import(string incomingDir, string folder)
        {
            BallMaterialFactory.EnsureFolder(folder);
            foreach (string file in Directory.GetFiles(incomingDir, "*.ttf"))
            {
                string fontPath = $"{folder}/{Path.GetFileName(file)}";
                File.Copy(file, fontPath, true);
                AssetDatabase.ImportAsset(fontPath, ImportAssetOptions.ForceSynchronousImport);
                CreateTmpAsset(fontPath);
            }

            string license = Path.Combine(incomingDir, "OFL.txt");
            if (File.Exists(license)) File.Copy(license, $"{folder}/OFL.txt", true);
        }

        public static TMP_FontAsset CreateTmpAsset(string fontPath)
        {
            string assetPath = Path.ChangeExtension(fontPath, null) + " SDF.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (existing != null) return existing;

            var font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
            TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(font, 72, 8, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            asset.name = Path.GetFileNameWithoutExtension(assetPath);
            AssetDatabase.CreateAsset(asset, assetPath);
            foreach (Texture2D atlas in asset.atlasTextures)
            {
                atlas.name = asset.name + " Atlas";
                AssetDatabase.AddObjectToAsset(atlas, asset);
            }

            asset.material.name = asset.name + " Material";
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            // Pre-bake the glyphs every screen uses so the first frames don't rasterise them.
            asset.TryAddCharacters(" 0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz.,:;!?%+-×/()[]'\"ʼʻ’‘•·…▶◀");
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return asset;
        }
    }
}
