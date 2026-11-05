using System.IO;
using UnityEditor;
using UnityEngine;

namespace BilliardGame.Editor
{
    /// <summary>Tileable cloth and wood textures generated in the editor and saved as PNG assets.</summary>
    internal static class ProceduralTextures
    {
        public const string Folder = "Assets/_Project/Art/Textures";

        /// <summary>Near-white worsted-cloth grain; the material colour tints it green.</summary>
        public static Texture2D Felt()
        {
            const int size = 512;
            var pixels = new Color[size * size];
            var random = new System.Random(11);
            var fibres = new float[size * size];
            for (int i = 0; i < fibres.Length; i++) fibres[i] = (float)random.NextDouble();

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Fine fibre noise plus two soft octaves; every term wraps so the texture tiles.
                    float fine = fibres[y * size + x];
                    float soft = TileNoise(x, y, size, 32, 5) * 0.6f + TileNoise(x, y, size, 8, 9) * 0.4f;
                    float value = 0.88f + (fine - 0.5f) * 0.10f + (soft - 0.5f) * 0.12f;
                    pixels[y * size + x] = new Color(value, value, value, 1f);
                }
            }

            return Save("Felt", size, size, pixels, TextureWrapMode.Repeat);
        }

        /// <summary>Lacquered mahogany grain running along U.</summary>
        public static Texture2D Wood()
        {
            const int width = 1024;
            const int height = 256;
            var dark = new Color(0.20f, 0.08f, 0.03f);
            var light = new Color(0.42f, 0.19f, 0.07f);
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float warp = TileNoise(x, y, width, 16, 3) * 6f + TileNoise(x, y, width, 64, 7) * 1.5f;
                    float rings = Mathf.Sin((y / (float)height * 18f + warp) * Mathf.PI);
                    float grain = Mathf.Pow(Mathf.Abs(rings), 3f);
                    float fleck = TileNoise(x, y, width, 256, 13) * 0.15f;
                    pixels[y * width + x] = Color.Lerp(light, dark, Mathf.Clamp01(grain * 0.8f + fleck));
                }
            }

            return Save("Wood", width, height, pixels, TextureWrapMode.Repeat);
        }

        /// <summary>Knurled-wheel ridges along V, shaded across U like a cylinder seen side-on; tiles vertically.</summary>
        public static Texture2D Ridges()
        {
            const int width = 64;
            const int height = 64;
            const int ridgeCount = 8;
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                float ridge = 0.5f + 0.5f * Mathf.Cos(y * ridgeCount * 2f * Mathf.PI / height);
                for (int x = 0; x < width; x++)
                {
                    float across = Mathf.Sin((x + 0.5f) / width * Mathf.PI);   // bright middle, dark edges
                    float value = across * (0.35f + 0.65f * ridge);
                    pixels[y * width + x] = new Color(value, value, value, 1f);
                }
            }

            return Save("Ridges", width, height, pixels, TextureWrapMode.Repeat);
        }

        // Smooth value noise on a periodic lattice: `cells` cells across `size` pixels, so it tiles.
        private static float TileNoise(int x, int y, int size, int cells, int seed)
        {
            float fx = x * cells / (float)size;
            float fy = y * cells / (float)size;
            int x0 = Mathf.FloorToInt(fx);
            int y0 = Mathf.FloorToInt(fy);
            float tx = Smooth(fx - x0);
            float ty = Smooth(fy - y0);
            float a = Lattice(x0, y0, cells, seed), b = Lattice(x0 + 1, y0, cells, seed);
            float c = Lattice(x0, y0 + 1, cells, seed), d = Lattice(x0 + 1, y0 + 1, cells, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), ty);
        }

        private static float Smooth(float t) => t * t * (3f - 2f * t);

        private static float Lattice(int x, int y, int cells, int seed)
        {
            x = ((x % cells) + cells) % cells;
            y = ((y % cells) + cells) % cells;
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + seed * 1442695041);
                h = (h ^ (h >> 13)) * 1274126177u;
                return (h ^ (h >> 16)) / (float)uint.MaxValue;
            }
        }

        private static Texture2D Save(string name, int width, int height, Color[] pixels, TextureWrapMode wrap)
        {
            BallMaterialFactory.EnsureFolder(Folder);
            string path = $"{Folder}/{name}.png";
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.wrapMode = wrap;
            importer.anisoLevel = 4;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
