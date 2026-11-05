using System.IO;
using UnityEditor;
using UnityEngine;

namespace BilliardGame.Editor.Content
{
    /// <summary>Asset helpers for art generated in code (original work, outside the license registry).</summary>
    internal static class GeneratedArt
    {
        public const string Root = "Assets/_Project/Art/Generated";

        public static string Folder(string sub)
        {
            string path = $"{Root}/{sub}";
            BallMaterialFactory.EnsureFolder(path);
            return path;
        }

        public static Texture2D SavePng(string path, int width, int height, Color32[] pixels, bool linear = false, FilterMode filter = FilterMode.Bilinear, TextureWrapMode wrap = TextureWrapMode.Repeat)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false, linear);
            texture.SetPixels32(pixels);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.sRGBTexture = !linear;
            importer.filterMode = filter;
            importer.wrapMode = wrap;
            importer.mipmapEnabled = filter != FilterMode.Point;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings { name = "Android", overridden = true, maxTextureSize = 1024, format = TextureImporterFormat.ASTC_6x6 });
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>Loads or creates a URP Lit material at <paramref name="path"/>, reset to the given surface.</summary>
        public static Material Lit(string path, Color color, float smoothness, float metallic, Texture baseMap = null, Vector2? tiling = null)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", metallic);
            material.SetTexture("_BaseMap", baseMap);
            material.SetTextureScale("_BaseMap", tiling ?? Vector2.one);
            material.SetTexture("_BumpMap", null);
            material.DisableKeyword("_NORMALMAP");
            material.SetTexture("_MetallicGlossMap", null);
            material.DisableKeyword("_METALLICSPECGLOSSMAP");
            material.SetTexture("_OcclusionMap", null);
            material.DisableKeyword("_OCCLUSIONMAP");
            material.SetTexture("_EmissionMap", null);
            material.SetColor("_EmissionColor", Color.black);
            material.DisableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>Copies albedo/normal/mask from an imported texture-set material (Import Incoming), if it exists.</summary>
        public static bool UseImportedMaps(Material target, string slug, Vector2 tiling, float normalScale = 1f)
        {
            var source = AssetDatabase.LoadAssetAtPath<Material>($"{Art.ArtImporter.ArtRoot}/Textures/{slug}/{slug}.mat");
            if (source == null)
            {
                Debug.LogWarning($"[Content] {slug} is not imported yet; using a flat material (run BilliardGame ▸ Art ▸ Import Incoming).");
                return false;
            }

            target.SetTexture("_BaseMap", source.GetTexture("_BaseMap"));
            target.SetTextureScale("_BaseMap", tiling);
            Texture normal = source.GetTexture("_BumpMap");
            if (normal != null)
            {
                target.SetTexture("_BumpMap", normal);
                target.SetFloat("_BumpScale", normalScale);
                target.EnableKeyword("_NORMALMAP");
            }

            Texture mask = source.GetTexture("_MetallicGlossMap");
            if (mask != null)
            {
                target.SetTexture("_MetallicGlossMap", mask);
                target.SetTexture("_OcclusionMap", mask);
                target.EnableKeyword("_METALLICSPECGLOSSMAP");
                target.EnableKeyword("_OCCLUSIONMAP");
            }

            EditorUtility.SetDirty(target);
            return true;
        }

        public static void Emission(Material material, Texture map, Color hdr)
        {
            material.SetTexture("_EmissionMap", map);
            material.SetColor("_EmissionColor", hdr);
            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            EditorUtility.SetDirty(material);
        }

        public static Mesh SaveMesh(Mesh mesh, string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(mesh, path);
                return mesh;
            }

            EditorUtility.CopySerialized(mesh, existing);
            Object.DestroyImmediate(mesh);
            EditorUtility.SetDirty(existing);
            return existing;
        }
    }
}
