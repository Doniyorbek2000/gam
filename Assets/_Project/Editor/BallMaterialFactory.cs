using System.IO;
using UnityEditor;
using UnityEngine;

namespace BilliardGame.Editor
{
    /// <summary>Creates (or reuses) the URP materials and generated ball textures under Assets/_Project/Art.</summary>
    internal static class BallMaterialFactory
    {
        public const string MaterialFolder = "Assets/_Project/Art/Materials";
        public const string TextureFolder = "Assets/_Project/Art/Balls";

        private const int TextureWidth = 1024;
        private const int TextureHeight = 512;

        // Standard pool colours for 1-7 (stripes 9-15 reuse them).
        private static readonly Color[] GroupColors =
        {
            new Color(0.98f, 0.80f, 0.10f), // 1 yellow
            new Color(0.10f, 0.25f, 0.75f), // 2 blue
            new Color(0.85f, 0.12f, 0.10f), // 3 red
            new Color(0.40f, 0.15f, 0.55f), // 4 purple
            new Color(0.98f, 0.45f, 0.08f), // 5 orange
            new Color(0.08f, 0.50f, 0.22f), // 6 green
            new Color(0.50f, 0.10f, 0.12f), // 7 maroon
        };

        public static Material Lit(string name, Color color, float smoothness, float metallic = 0f)
        {
            Material material = LoadOrCreate(name, "Universal Render Pipeline/Lit");
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", metallic);
            EditorUtility.SetDirty(material);
            return material;
        }

        public static Material Textured(string name, Texture2D texture, Color tint, Vector2 tiling, float smoothness, float metallic = 0f)
        {
            Material material = Lit(name, tint, smoothness, metallic);
            material.SetTexture("_BaseMap", texture);
            material.SetTextureScale("_BaseMap", tiling);
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>HDR emissive surface, used for lamp panels that only the reflection probe sees.</summary>
        public static Material Emissive(string name, Color hdrColor)
        {
            Material material = Lit(name, Color.black, 0f);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", hdrColor);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(material);
            return material;
        }

        public static Material Unlit(string name, Color color)
        {
            Material material = LoadOrCreate(name, "Universal Render Pipeline/Unlit");
            material.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(material);
            return material;
        }

        public static Material Ball(int id)
        {
            Material material = LoadOrCreate($"Ball_{id:00}", "Universal Render Pipeline/Lit");
            material.SetTexture("_BaseMap", BallTexture(id));
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Smoothness", 0.93f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material LoadOrCreate(string name, string shaderName)
        {
            EnsureFolder(MaterialFolder);
            string path = $"{MaterialFolder}/{name}.mat";
            Shader shader = Shader.Find(shaderName);
            if (shader == null) throw new FileNotFoundException($"Shader '{shaderName}' not found; is URP active?");

            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                material.shader = shader;
                return material;
            }

            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Texture2D BallTexture(int id)
        {
            EnsureFolder(TextureFolder);
            string path = $"{TextureFolder}/Ball_{id:00}.png";
            File.WriteAllBytes(path, PaintBall(id).EncodeToPNG());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // Equirectangular paint for the Unity sphere UVs: u wraps around, v runs pole to pole.
        // Equirectangular paint for the Unity sphere UVs: u is longitude, v is latitude (0 = bottom pole).
        // Discs and digits are laid out on the sphere surface, so they stay round and undistorted.
        private static Texture2D PaintBall(int id)
        {
            Color white = new Color(0.96f, 0.95f, 0.90f);
            Color ink = new Color(0.06f, 0.06f, 0.06f);
            Color body = id == 0 ? white : id == 8 ? new Color(0.04f, 0.04f, 0.04f) : GroupColors[(id - 1) % 8];
            bool stripe = id >= 9;
            float pixelAngle = Mathf.PI / TextureHeight;
            float discRadius = (id == 0 ? 7f : 25f) * Mathf.Deg2Rad;
            float stripeHalf = 38f * Mathf.Deg2Rad;
            float[] label = id == 0 ? null : (numberLabel ??= new BallNumberLabel()).Render(id.ToString(), LabelSize, id >= 10 ? 0.46f : 0.56f);
            // Two numbered discs on opposite sides (cue ball: four red spots) make the roll readable.
            float[] centres = id == 0 ? new[] { 0f, 0.5f * Mathf.PI, Mathf.PI, 1.5f * Mathf.PI } : new[] { 0.5f * Mathf.PI, 1.5f * Mathf.PI };
            Color spot = id == 0 ? new Color(0.8f, 0.1f, 0.1f) : white;

            var texture = new Texture2D(TextureWidth, TextureHeight, TextureFormat.RGBA32, true);
            var pixels = new Color[TextureWidth * TextureHeight];
            for (int y = 0; y < TextureHeight; y++)
            {
                float lat = ((y + 0.5f) / TextureHeight - 0.5f) * Mathf.PI;
                for (int x = 0; x < TextureWidth; x++)
                {
                    float lon = (x + 0.5f) / TextureWidth * 2f * Mathf.PI;
                    Color c = body;
                    if (stripe) c = Color.Lerp(white, body, Edge(stripeHalf - Mathf.Abs(lat), pixelAngle));

                    foreach (float centre in centres)
                    {
                        float dLon = lon - centre;
                        float cosDistance = Mathf.Cos(lat) * Mathf.Cos(dLon);
                        float distance = Mathf.Acos(Mathf.Clamp(cosDistance, -1f, 1f));
                        float inside = Edge(discRadius - distance, pixelAngle);
                        if (inside <= 0f) continue;

                        Color disc = spot;
                        if (label != null)
                        {
                            // Orthographic projection onto the plane touching the disc centre.
                            float tx = Mathf.Cos(lat) * Mathf.Sin(dLon) * LongitudeToScreen / Mathf.Sin(discRadius);
                            float ty = Mathf.Sin(lat) / Mathf.Sin(discRadius);
                            disc = Color.Lerp(white, ink, SampleLabel(label, tx, ty));
                        }

                        c = Color.Lerp(c, disc, inside);
                    }

                    pixels[y * TextureWidth + x] = c;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(true);
            return texture;
        }

        /// <summary>On Unity's sphere u grows to the viewer's right (u 0.25 faces local -Z), so digits need no flip.</summary>
        private const float LongitudeToScreen = 1f;

        private const int LabelSize = 256;
        private static BallNumberLabel numberLabel;

        private static float Edge(float signedDistance, float pixel) => Mathf.Clamp01(signedDistance / pixel + 0.5f);

        private static float SampleLabel(float[] label, float x, float y)
        {
            float px = (x * 0.5f + 0.5f) * LabelSize - 0.5f;
            float py = (y * 0.5f + 0.5f) * LabelSize - 0.5f;
            int ix = Mathf.FloorToInt(px);
            int iy = Mathf.FloorToInt(py);
            float fx = px - ix;
            float fy = py - iy;
            float a = LabelTexel(label, ix, iy), b = LabelTexel(label, ix + 1, iy);
            float c = LabelTexel(label, ix, iy + 1), d = LabelTexel(label, ix + 1, iy + 1);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        private static float LabelTexel(float[] label, int x, int y) =>
            x < 0 || y < 0 || x >= LabelSize || y >= LabelSize ? 0f : label[y * LabelSize + x];

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
