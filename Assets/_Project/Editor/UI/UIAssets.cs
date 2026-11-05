using System.Collections.Generic;
using System.IO;
using BilliardGame.UI;
using TMPro;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace BilliardGame.Editor.UI
{
    /// <summary>
    /// Design-system assets made in code: rounded 9-slice sprites (anti-aliased SDF), circle/ring/glow/gradient sprites,
    /// the <see cref="UITheme"/> asset, icon/sticker import settings and the icon SpriteAtlas.
    /// </summary>
    internal static class UIAssets
    {
        public const string Root = "Assets/_Project/UI";
        public const string ThemePath = Root + "/Theme/UITheme.asset";
        public const string SpriteDir = Root + "/Sprites";
        public const string IconDir = Root + "/Icons";
        public const string StickerDir = Root + "/Stickers";
        public const string AtlasPath = Root + "/Icons.spriteatlasv2";
        private const string FontDir = "Assets/_Project/Art/Fonts/font-montserrat";

        private static UITheme theme;
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static UITheme Theme => theme != null ? theme : theme = EnsureTheme();

        [MenuItem("BilliardGame/UI/Build UI Assets")]
        public static void BuildAll()
        {
            Cache.Clear();
            BuildSprites();
            ImportFolder(IconDir, 128);
            ImportFolder(StickerDir, 256);
            BuildAtlas();
            theme = EnsureTheme();
            AssetDatabase.SaveAssets();
            Debug.Log("[BilliardGame] UI assets built");
        }

        public static UITheme EnsureTheme()
        {
            var asset = AssetDatabase.LoadAssetAtPath<UITheme>(ThemePath);
            if (asset == null)
            {
                BallMaterialFactory.EnsureFolder(Path.GetDirectoryName(ThemePath).Replace('\\', '/'));
                asset = ScriptableObject.CreateInstance<UITheme>();
                AssetDatabase.CreateAsset(asset, ThemePath);
            }

            asset.bold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>($"{FontDir}/Montserrat-Bold SDF.asset");
            asset.semiBold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>($"{FontDir}/Montserrat-SemiBold SDF.asset");
            asset.medium = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>($"{FontDir}/Montserrat-Medium SDF.asset");
            EditorUtility.SetDirty(asset);
            return asset;
        }

        // ---- Sprites -------------------------------------------------------------------------------------------------

        public static Sprite Rounded(int radius) => Load($"{SpriteDir}/rounded_{radius}.png");
        public static Sprite Pill => Load($"{SpriteDir}/pill.png");
        public static Sprite Circle => Load($"{SpriteDir}/circle.png");
        public static Sprite Ring => Load($"{SpriteDir}/ring.png");
        public static Sprite Glow => Load($"{SpriteDir}/glow.png");
        public static Sprite Shadow => Load($"{SpriteDir}/shadow.png");
        public static Sprite GradientUp => Load($"{SpriteDir}/gradient_up.png");
        public static Sprite GradientRight => Load($"{SpriteDir}/gradient_right.png");
        public static Sprite Stroke(int radius) => Load($"{SpriteDir}/stroke_{radius}.png");
        public static Sprite Vignette => Load($"{SpriteDir}/vignette.png");
        public static Sprite Rays => Load($"{SpriteDir}/rays.png");

        public static Sprite Icon(string name)
        {
            Sprite sprite = Load($"{IconDir}/{name}.png");
            if (sprite == null) Debug.LogWarning($"[UI] Missing icon '{name}'");
            return sprite;
        }

        public static Sprite Sticker(string id) => Load($"{StickerDir}/{id}.png");

        private static Sprite Load(string path)
        {
            if (Cache.TryGetValue(path, out Sprite sprite) && sprite != null) return sprite;
            sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            Cache[path] = sprite;
            return sprite;
        }

        private static void BuildSprites()
        {
            BallMaterialFactory.EnsureFolder(SpriteDir);
            foreach (int radius in new[] { 12, 20, 32 })
            {
                int size = radius * 2 + 8;
                Save($"rounded_{radius}.png", size, size, (x, y) => RoundedAlpha(x, y, size, size, radius), radius + 2);
                Save($"stroke_{radius}.png", size, size, (x, y) => StrokeAlpha(x, y, size, size, radius, 3f), radius + 2);
            }

            Save("pill.png", 132, 132, (x, y) => RoundedAlpha(x, y, 132, 132, 64), 64);
            Save("circle.png", 128, 128, (x, y) => Mathf.Clamp01(64f - Distance(x, y, 64f, 64f)), 0);
            Save("ring.png", 128, 128, (x, y) => Mathf.Clamp01(6f - Mathf.Abs(Distance(x, y, 64f, 64f) - 56f)), 0);
            Save("glow.png", 128, 128, (x, y) => Mathf.Pow(Mathf.Clamp01(1f - Distance(x, y, 64f, 64f) / 64f), 2.2f), 0);
            // Soft drop shadow: a blurred rounded rect, sliced so it stretches under any card.
            Save("shadow.png", 96, 96, (x, y) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((28f - RoundedDistance(x, y, 96, 96, 20, 28f)) / 28f)) * 0.9f, 46);
            Save("gradient_up.png", 4, 64, (x, y) => y / 63f, 0, linearAlpha: true);
            Save("gradient_right.png", 64, 4, (x, y) => x / 63f, 0, linearAlpha: true);
            // Victory sunburst: 16 soft wedges fading out from the centre.
            Save("rays.png", 256, 256, (x, y) =>
            {
                float dx = x + 0.5f - 128f, dy = y + 0.5f - 128f;
                float wedge = Mathf.Pow(Mathf.Abs(Mathf.Cos(Mathf.Atan2(dy, dx) * 8f)), 6f);
                return wedge * Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy) / 128f);
            }, 0);
            Save("vignette.png", 128, 128, (x, y) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((Distance(x, y, 64f, 64f) - 30f) / 60f)), 0);
        }

        private static float Distance(int x, int y, float cx, float cy) => Mathf.Sqrt((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - cy) * (y + 0.5f - cy));

        /// <summary>Signed distance from a pixel centre to a rounded rect inset by <paramref name="inset"/> (negative inside).</summary>
        private static float RoundedDistance(int x, int y, int w, int h, float radius, float inset = 0f)
        {
            float px = x + 0.5f - w * 0.5f, py = y + 0.5f - h * 0.5f;
            float hx = w * 0.5f - inset - radius, hy = h * 0.5f - inset - radius;
            float qx = Mathf.Abs(px) - hx, qy = Mathf.Abs(py) - hy;
            float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
            return outside + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
        }

        private static float RoundedAlpha(int x, int y, int w, int h, float radius) => Mathf.Clamp01(0.5f - RoundedDistance(x, y, w, h, radius, 1f));

        private static float StrokeAlpha(int x, int y, int w, int h, float radius, float width) =>
            Mathf.Clamp01(0.5f - RoundedDistance(x, y, w, h, radius, 1f)) * Mathf.Clamp01(RoundedDistance(x, y, w, h, radius, 1f) + width + 0.5f);

        private static void Save(string file, int w, int h, System.Func<int, int, float> alpha, int border, bool linearAlpha = false)
        {
            var pixels = new Color32[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                pixels[y * w + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(alpha(x, y)) * 255f));

            string path = $"{SpriteDir}/{file}";
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spriteBorder = new Vector4(border, border, border, border);
            importer.SaveAndReimport();
        }

        private static void ImportFolder(string folder, int maxSize)
        {
            foreach (string path in Directory.GetFiles(folder, "*.png"))
            {
                string asset = path.Replace('\\', '/');
                var importer = (TextureImporter)AssetImporter.GetAtPath(asset);
                if (importer == null) continue;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.maxTextureSize = maxSize;
                // Atlas sources stay uncompressed; the atlas itself is compressed per platform.
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.ClearPlatformTextureSettings("Android");
                importer.SaveAndReimport();
            }
        }

        /// <summary>One atlas for all icons and design-system sprites: a single draw call for most of the chrome.</summary>
        private static void BuildAtlas()
        {
            EditorSettings.spritePackerMode = SpritePackerMode.SpriteAtlasV2;
            if (!File.Exists(AtlasPath))
            {
                var atlas = new SpriteAtlasAsset();
                atlas.Add(new Object[] { AssetDatabase.LoadAssetAtPath<DefaultAsset>(IconDir), AssetDatabase.LoadAssetAtPath<DefaultAsset>(SpriteDir) });
                SpriteAtlasAsset.Save(atlas, AtlasPath);
                AssetDatabase.ImportAsset(AtlasPath);
            }

            if (AssetImporter.GetAtPath(AtlasPath) is SpriteAtlasImporter importer)
            {
                importer.includeInBuild = true;
                importer.packingSettings = new SpriteAtlasPackingSettings { enableRotation = false, enableTightPacking = false, padding = 4, enableAlphaDilation = true };
                importer.textureSettings = new SpriteAtlasTextureSettings { filterMode = FilterMode.Bilinear, generateMipMaps = false, sRGB = true };
                importer.SaveAndReimport();
            }
        }
    }
}
