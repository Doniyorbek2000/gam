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

        private const int TextureWidth = 512;
        private const int TextureHeight = 256;

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
            material.SetFloat("_Smoothness", 0.9f);
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
        private static Texture2D PaintBall(int id)
        {
            Color white = new Color(0.96f, 0.95f, 0.90f);
            Color body = id == 0 ? white : id == 8 ? new Color(0.05f, 0.05f, 0.05f) : GroupColors[(id - 1) % 8];
            bool stripe = id >= 9;
            var texture = new Texture2D(TextureWidth, TextureHeight, TextureFormat.RGBA32, false);
            var pixels = new Color[TextureWidth * TextureHeight];

            for (int y = 0; y < TextureHeight; y++)
            {
                float v = (y + 0.5f) / TextureHeight;
                for (int x = 0; x < TextureWidth; x++)
                {
                    float u = (x + 0.5f) / TextureWidth;
                    Color c = stripe ? (Mathf.Abs(v - 0.5f) < 0.2f ? body : white) : body;
                    if (id == 0)
                    {
                        if (InDisc(u, v, 0.25f, 0.5f, 0.05f) || InDisc(u, v, 0.75f, 0.5f, 0.05f)) c = new Color(0.8f, 0.1f, 0.1f);
                    }
                    else if (InDisc(u, v, 0.25f, 0.5f, 0.12f) || InDisc(u, v, 0.75f, 0.5f, 0.12f))
                    {
                        c = white;
                    }

                    pixels[y * TextureWidth + x] = c;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        // Round on the sphere near the equator: one unit of u spans twice the angle of one unit of v.
        private static bool InDisc(float u, float v, float cu, float cv, float radius)
        {
            float du = Mathf.Abs(u - cu);
            du = Mathf.Min(du, 1f - du) * 2f;
            float dv = v - cv;
            return du * du + dv * dv < radius * radius;
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
