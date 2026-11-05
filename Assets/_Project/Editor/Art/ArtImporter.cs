using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace BilliardGame.Editor.Art
{
    /// <summary>
    /// BilliardGame ▸ Art ▸ Import Incoming (docs/ART_PIPELINE.md §5): license check, copy, model/texture import settings,
    /// tri budget, Mesh LOD, URP Lit material with packed mask map, prefab, report. Fonts and HDRIs get their own paths.
    /// </summary>
    internal static class ArtImporter
    {
        public const string IncomingRoot = "ArtSource/Incoming";
        public const string ReportRoot = "ArtSource/Reports";
        public const string ArtRoot = "Assets/_Project/Art";

        private static readonly string[] ModelExtensions = { ".fbx", ".obj" };
        private static readonly string[] TextureExtensions = { ".jpg", ".jpeg", ".png", ".tga" };

        /// <summary>LOD0 triangle budgets (docs/ART_PIPELINE.md §3.1).</summary>
        private static readonly Dictionary<string, int> TriBudget = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "table", 40_000 }, { "cue", 5_000 }, { "balls", 19_200 }, { "venue", 150_000 }, { "avatar", 20_000 },
            { "outfit", 5_000 }, { "prop", 3_000 },
        };

        private enum MapKind { Albedo, Normal, Roughness, Metallic, Occlusion, Arm, Other }

        public const string SidecarFile = "materials.json";

        /// <summary>materials.json written by the Blender glTF converter: maps are already ≤ 2K and the mask is URP-packed.</summary>
        [Serializable]
        private sealed class Sidecar
        {
            public SidecarMaterial[] materials = new SidecarMaterial[0];
        }

        [Serializable]
        private sealed class SidecarMaterial
        {
            public string name, alpha, @base, normal, mask, emission;
            public bool doubleSided;
            public float metallic, roughness = 0.5f;
            public float[] color;
        }

        [MenuItem("BilliardGame/Art/Import Incoming")]
        public static void ImportAll()
        {
            Dictionary<string, LicenseEntry> registry = LicenseRegistry.Load();
            if (!Directory.Exists(IncomingRoot))
            {
                Debug.LogWarning($"[Art] {IncomingRoot} does not exist; nothing to import.");
                return;
            }

            int ok = 0, failed = 0;
            foreach (string dir in Directory.GetDirectories(IncomingRoot).OrderBy(d => d))
            {
                string slug = Path.GetFileName(dir);
                registry.TryGetValue(slug, out LicenseEntry entry);
                try
                {
                    if (Import(dir, entry)) ok++;
                    else failed++;
                }
                catch (Exception e)
                {
                    failed++;
                    Debug.LogError($"[Art] {slug}: import crashed: {e}");
                }
            }

            AssetDatabase.SaveAssets();
            CreditsGenerator.Generate();
            Debug.Log($"[Art] Import Incoming finished: {ok} imported, {failed} failed.");
        }

        public static string FolderFor(LicenseEntry entry) => $"{ArtRoot}/{LicenseRegistry.TypeFolders[entry.Type]}/{entry.Slug}";

        private static bool Import(string incomingDir, LicenseEntry entry)
        {
            string slug = Path.GetFileName(incomingDir);
            string rejected = LicenseRegistry.Reject(entry);
            if (rejected != null)
            {
                Debug.LogError($"[Art] {slug}: refused — {rejected}. Add or fix its block in {LicenseRegistry.RegistryPath}.");
                return false;
            }

            var report = new Report(entry);
            string folder = FolderFor(entry);
            string type = entry.Type.ToLowerInvariant();
            switch (type)
            {
                case "font":
                    FontImport.Import(incomingDir, folder);
                    report.Line($"Fonts + dynamic TMP SDF assets: `{folder}`");
                    break;
                case "hdri":
                    ImportHdri(incomingDir, folder, report);
                    break;
                case "texture":
                    CopyAll(incomingDir, folder + "/Source", TextureExtensions);
                    BuildMaterial(incomingDir, folder, slug, report);
                    break;
                default:
                    if (!ImportModel(incomingDir, folder, entry, report)) return Finish(report, false);
                    break;
            }

            return Finish(report, true);
        }

        private static bool ImportModel(string incomingDir, string folder, LicenseEntry entry, Report report)
        {
            string slug = entry.Slug;
            string type = entry.Type.ToLowerInvariant();
            string[] glb = Directory.GetFiles(incomingDir).Where(f => f.EndsWith(".glb", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".gltf", StringComparison.OrdinalIgnoreCase)).ToArray();
            List<string> models = CopyAll(incomingDir, folder + "/Source", ModelExtensions);
            CopyAll(incomingDir, folder + "/Source", TextureExtensions);
            if (models.Count == 0)
            {
                report.Error(glb.Length > 0 ? "GLB/glTF needs com.unity.cloud.gltfast (not installed); export FBX instead" : "no .fbx/.obj model found");
                return false;
            }

            string modelPath = models[0];
            var importer = (ModelImporter)AssetImporter.GetAtPath(modelPath);
            bool isAvatar = type == "avatar" || type == "outfit";
            // glTF sources converted in Blender ship per-material maps + materials.json (one URP Lit per FBX material).
            string sidecarPath = Path.Combine(incomingDir, SidecarFile);
            bool hasSidecar = File.Exists(sidecarPath);
            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.isReadable = false;
            importer.meshCompression = ModelImporterMeshCompression.Medium;
            importer.importTangents = ModelImporterTangents.CalculateMikk;
            importer.importAnimation = isAvatar;
            importer.animationType = isAvatar ? ModelImporterAnimationType.Human : ModelImporterAnimationType.None;
            importer.generateSecondaryUV = type == "prop" || type == "venue" || type == "table";
            importer.materialImportMode = isAvatar || hasSidecar ? ModelImporterMaterialImportMode.ImportViaMaterialDescription : ModelImporterMaterialImportMode.None;
            importer.generateMeshLods = false;
            importer.SaveAndReimport();
            if (isAvatar && importer.defaultClipAnimations.Length > 0)
            {
                // Clips named *_Loop (Quaternius/Mixamo convention) loop; one-shots don't.
                importer.clipAnimations = importer.defaultClipAnimations.Select(c =>
                {
                    c.loopTime = c.name.EndsWith("_Loop", StringComparison.Ordinal);
                    return c;
                }).ToArray();
                importer.SaveAndReimport();
                report.Line($"Animation clips: {importer.clipAnimations.Length} (looping: {importer.clipAnimations.Count(c => c.loopTime)})");
            }

            // Measure LOD0 before Mesh LOD generation appends lower levels to the index buffer.
            Mesh[] meshes = AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<Mesh>().ToArray();
            int tris = meshes.Sum(m => Enumerable.Range(0, m.subMeshCount).Sum(s => (int)m.GetIndexCount(s)) / 3);
            int submeshes = meshes.Sum(m => m.subMeshCount);
            Bounds bounds = WorldBounds(modelPath);
            report.Line($"Model: `{Path.GetFileName(modelPath)}`, meshes {meshes.Length}, submeshes {submeshes}, LOD0 triangles **{tris:N0}**");
            report.Line($"Bounds size (m): {bounds.size.x:F2} × {bounds.size.y:F2} × {bounds.size.z:F2}");

            if (TriBudget.TryGetValue(type, out int budget))
            {
                if (tris > budget * 1.5f)
                {
                    report.Error($"LOD0 {tris:N0} tris is over 1.5× the {type} budget ({budget:N0}); optimise by hand first");
                    return false;
                }

                if (tris > budget) report.Warn($"LOD0 {tris:N0} tris is over the {type} budget ({budget:N0})");
            }

            if (type == "cue" && Mathf.Abs(bounds.size.z - 1.47f) > 0.2f) report.Warn("cue length is not ~1.47 m along Z");
            if (type == "table" && Mathf.Abs(Mathf.Max(bounds.size.x, bounds.size.z) - 2.8f) > 0.4f) report.Warn("table is not ~2.8 m long");

            if (!isAvatar)
            {
                importer.generateMeshLods = true;
                importer.maximumMeshLod = 2;
                importer.SaveAndReimport();
                report.Line("Mesh LOD: generated (Unity 6 Mesh LOD, max 2 levels)");
            }

            if (hasSidecar)
            {
                foreach (KeyValuePair<string, Material> pair in BuildSidecarMaterials(sidecarPath, folder, SmoothnessScale(entry), report))
                {
                    importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), pair.Key), pair.Value);
                }

                importer.SaveAndReimport();
            }

            // Avatars keep the materials embedded in the FBX; sidecar models use the remapped ones.
            Material material = isAvatar || hasSidecar ? null : BuildMaterial(incomingDir, folder, entry.Slug, report);
            GameObject prefab = BuildPrefab(modelPath, folder, slug, material, report);
            if (type == "cue") CreateCueSkin(entry, folder, prefab, report);
            return true;
        }

        /// <summary>Imported cues become catalog items on the next Build Content; `rarity:` in LICENSES.md (default Rare).</summary>
        private static void CreateCueSkin(LicenseEntry entry, string folder, GameObject prefab, Report report)
        {
            if (!Enum.TryParse(entry["rarity"], true, out Meta.Rarity rarity)) rarity = Meta.Rarity.Rare;
            string path = $"{folder}/{entry.Slug}_skin.asset";
            var skin = AssetDatabase.LoadAssetAtPath<BilliardGame.Content.SkinDefinition>(path);
            if (skin == null)
            {
                skin = ScriptableObject.CreateInstance<BilliardGame.Content.SkinDefinition>();
                AssetDatabase.CreateAsset(skin, path);
            }

            skin.Configure(entry.Slug.Replace('-', '_'), Meta.ItemType.Cue, rarity, entry["name"], false, prefab, null, null, null, "Yashik");
            EditorUtility.SetDirty(skin);
            report.Line($"SkinDefinition: `{path}` ({rarity}); run BilliardGame ▸ Build Content to add it to the catalog");
        }

        // Node transforms often carry the unit scale (cm FBX), so measure renderers, not raw mesh bounds.
        private static Bounds WorldBounds(string modelPath)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(modelPath));
            try
            {
                Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0) return default;
                Bounds bounds = renderers[0].bounds;
                foreach (Renderer r in renderers.Skip(1)) bounds.Encapsulate(r.bounds);
                return bounds;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static GameObject BuildPrefab(string modelPath, string folder, string slug, Material material, Report report)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            try
            {
                foreach (Collider c in instance.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(c);
                if (material != null)
                {
                    foreach (Renderer r in instance.GetComponentsInChildren<Renderer>(true))
                    {
                        r.sharedMaterials = Enumerable.Repeat(material, Math.Max(1, r.sharedMaterials.Length)).ToArray();
                    }
                }

                string prefabPath = $"{folder}/{slug}.prefab";
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
                report.Line($"Prefab: `{prefabPath}`");
                return prefab;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        /// <summary>URP Lit from the slug's maps; roughness/metal/AO (or Poly Haven ARM) are packed into one mask map.</summary>
        private static Material BuildMaterial(string incomingDir, string folder, string slug, Report report)
        {
            string[] sources = Directory.GetFiles(incomingDir).Where(f => TextureExtensions.Contains(Path.GetExtension(f).ToLowerInvariant())).ToArray();
            var maps = sources.GroupBy(Classify).ToDictionary(g => g.Key, g => g.First());
            if (!maps.ContainsKey(MapKind.Albedo))
            {
                report.Warn("no albedo map; material left untextured");
                return null;
            }

            int maxSize = MaxTextureSize(slug);
            TextureImporterFormat colourFormat = slug.StartsWith("tex-venue", StringComparison.Ordinal) ? TextureImporterFormat.ASTC_8x8 : TextureImporterFormat.ASTC_6x6;
            string textureFolder = folder + "/Source";

            Texture2D albedo = ConfigureTexture($"{textureFolder}/{Path.GetFileName(maps[MapKind.Albedo])}", maxSize, colourFormat, srgb: true, normal: false);
            Texture2D normal = maps.TryGetValue(MapKind.Normal, out string normalFile)
                ? ConfigureTexture($"{textureFolder}/{Path.GetFileName(normalFile)}", maxSize, TextureImporterFormat.ASTC_5x5, srgb: false, normal: true)
                : null;
            Texture2D mask = PackMask(maps, $"{folder}/{slug}_mask.png", maxSize, report);

            string materialPath = $"{folder}/{slug}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, materialPath);
            }

            material.SetTexture("_BaseMap", albedo);
            material.SetColor("_BaseColor", Color.white);
            if (normal != null)
            {
                material.SetTexture("_BumpMap", normal);
                material.EnableKeyword("_NORMALMAP");
            }

            if (mask != null)
            {
                // URP reads metallic from R, smoothness from A and occlusion from G: one texture serves both slots.
                material.SetTexture("_MetallicGlossMap", mask);
                material.SetTexture("_OcclusionMap", mask);
                material.SetFloat("_Smoothness", 1f);
                material.EnableKeyword("_METALLICSPECGLOSSMAP");
                material.EnableKeyword("_OCCLUSIONMAP");
            }
            else
            {
                material.SetFloat("_Smoothness", 0.3f);
            }

            EditorUtility.SetDirty(material);
            report.Line($"Material: `{materialPath}` (URP Lit; albedo{(normal != null ? " + normal" : string.Empty)}{(mask != null ? " + mask" : string.Empty)}, max {maxSize}px, Android {colourFormat})");
            return material;
        }

        /// <summary>One URP Lit per sidecar entry, keyed by the FBX material name for <see cref="ModelImporter.AddRemap"/>.</summary>
        /// <summary>Optional `smoothness:` in LICENSES.md scales the mask smoothness (tames over-glossy sources under HDRI reflections).</summary>
        private static float SmoothnessScale(LicenseEntry entry) =>
            float.TryParse(entry["smoothness"], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float scale) ? Mathf.Clamp01(scale) : 1f;

        private static Dictionary<string, Material> BuildSidecarMaterials(string sidecarPath, string folder, float smoothnessScale, Report report)
        {
            var sidecar = JsonUtility.FromJson<Sidecar>(File.ReadAllText(sidecarPath));
            var result = new Dictionary<string, Material>();
            string source = folder + "/Source";
            foreach (SidecarMaterial m in sidecar.materials)
            {
                Texture2D Map(string file, TextureImporterFormat format, bool srgb, bool normal) =>
                    string.IsNullOrEmpty(file) ? null : ConfigureTexture($"{source}/{file}", 2048, format, srgb, normal);

                string materialPath = $"{folder}/{m.name}.mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null)
                {
                    material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    AssetDatabase.CreateAsset(material, materialPath);
                }

                Texture2D baseMap = Map(m.@base, TextureImporterFormat.ASTC_6x6, true, false);
                material.SetTexture("_BaseMap", baseMap);
                material.SetColor("_BaseColor", baseMap != null || m.color == null || m.color.Length < 4 ? Color.white : new Color(m.color[0], m.color[1], m.color[2], m.color[3]));

                Texture2D normalMap = Map(m.normal, TextureImporterFormat.ASTC_5x5, false, true);
                material.SetTexture("_BumpMap", normalMap);
                SetKeyword(material, "_NORMALMAP", normalMap != null);

                Texture2D mask = Map(m.mask, TextureImporterFormat.ASTC_6x6, false, false);
                material.SetTexture("_MetallicGlossMap", mask);
                material.SetFloat("_Smoothness", (mask != null ? 1f : 1f - m.roughness) * smoothnessScale);
                material.SetFloat("_Metallic", m.metallic);
                SetKeyword(material, "_METALLICSPECGLOSSMAP", mask != null);

                Texture2D emission = Map(m.emission, TextureImporterFormat.ASTC_6x6, true, false);
                material.SetTexture("_EmissionMap", emission);
                material.SetColor("_EmissionColor", emission != null ? Color.white : Color.black);
                SetKeyword(material, "_EMISSION", emission != null);

                // Cutout only (nets, fringes): no sorted transparency on mobile. BLEND sources are clipped too.
                bool clip = m.alpha == "MASK" || m.alpha == "BLEND";
                material.SetFloat("_AlphaClip", clip ? 1f : 0f);
                material.SetFloat("_Cutoff", 0.5f);
                SetKeyword(material, "_ALPHATEST_ON", clip);
                material.SetFloat("_Cull", clip && m.doubleSided ? 0f : 2f);
                material.renderQueue = clip ? (int)UnityEngine.Rendering.RenderQueue.AlphaTest : -1;
                if (m.alpha == "BLEND") report.Warn($"{m.name}: alpha BLEND source rendered as cutout");

                EditorUtility.SetDirty(material);
                result[m.name] = material;
                report.Line($"Material: `{materialPath}` (URP Lit{(baseMap != null ? ", base" : string.Empty)}{(normalMap != null ? " + normal" : string.Empty)}{(mask != null ? " + mask" : string.Empty)}{(emission != null ? " + emission" : string.Empty)}{(clip ? ", alpha clip" : string.Empty)}, max 2048px, Android ASTC 6×6/5×5, smoothness ×{smoothnessScale:0.##})");
            }

            return result;
        }

        private static void SetKeyword(Material material, string keyword, bool on)
        {
            if (on) material.EnableKeyword(keyword);
            else material.DisableKeyword(keyword);
        }

        private static Texture2D PackMask(Dictionary<MapKind, string> maps, string outputPath, int maxSize, Report report)
        {
            Color32[] arm = Load(maps, MapKind.Arm, maxSize, out int width, out int height);
            Color32[] rough = arm == null ? Load(maps, MapKind.Roughness, maxSize, out width, out height) : null;
            if (arm == null && rough == null) return null;
            Color32[] metal = arm == null ? Load(maps, MapKind.Metallic, maxSize, out _, out _) : null;
            Color32[] ao = arm == null ? Load(maps, MapKind.Occlusion, maxSize, out _, out _) : null;

            var packed = new Color32[width * height];
            for (int i = 0; i < packed.Length; i++)
            {
                // Poly Haven ARM: R = AO, G = roughness, B = metal. Output: R metal, G AO, A smoothness.
                byte m = arm != null ? arm[i].b : metal != null && metal.Length == packed.Length ? metal[i].r : (byte)0;
                byte o = arm != null ? arm[i].r : ao != null && ao.Length == packed.Length ? ao[i].r : (byte)255;
                byte r = arm != null ? arm[i].g : rough[i].r;
                packed[i] = new Color32(m, o, 0, (byte)(255 - r));
            }

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
            texture.SetPixels32(packed);
            File.WriteAllBytes(outputPath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(outputPath);
            report.Line($"Mask map packed: `{outputPath}` ({width}×{height})");
            return ConfigureTexture(outputPath, maxSize, TextureImporterFormat.ASTC_6x6, srgb: false, normal: false);
        }

        /// <summary>Reads a raw source map from Incoming (never the imported, compressed copy) scaled down to <paramref name="maxSize"/>.</summary>
        private static Color32[] Load(Dictionary<MapKind, string> maps, MapKind kind, int maxSize, out int width, out int height)
        {
            width = height = 0;
            if (!maps.TryGetValue(kind, out string file)) return null;
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            try
            {
                if (!texture.LoadImage(File.ReadAllBytes(file))) return null;
                width = Math.Min(texture.width, maxSize);
                height = Math.Min(texture.height, maxSize);
                if (width == texture.width && height == texture.height) return texture.GetPixels32();

                RenderTexture rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                Graphics.Blit(texture, rt);
                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = rt;
                var scaled = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
                scaled.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(rt);
                Color32[] pixels = scaled.GetPixels32();
                UnityEngine.Object.DestroyImmediate(scaled);
                return pixels;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static Texture2D ConfigureTexture(string path, int maxSize, TextureImporterFormat androidFormat, bool srgb, bool normal)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = srgb;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = maxSize;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                name = "Android",
                overridden = true,
                maxTextureSize = maxSize,
                format = androidFormat,
            });
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static void ImportHdri(string incomingDir, string folder, Report report)
        {
            List<string> copied = CopyAll(incomingDir, folder, new[] { ".hdr", ".exr" });
            foreach (string path in copied)
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureShape = TextureImporterShape.TextureCube;
                importer.maxTextureSize = 512;
                importer.mipmapEnabled = true;
                importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
                {
                    name = "Android",
                    overridden = true,
                    maxTextureSize = 512,
                    format = TextureImporterFormat.ASTC_HDR_6x6,
                });
                importer.SaveAndReimport();
                report.Line($"HDRI cubemap: `{path}` (512, Android ASTC HDR 6×6)");
            }
        }

        private static List<string> CopyAll(string from, string to, string[] extensions)
        {
            BallMaterialFactory.EnsureFolder(to);
            var copied = new List<string>();
            foreach (string file in Directory.GetFiles(from))
            {
                if (!extensions.Contains(Path.GetExtension(file).ToLowerInvariant())) continue;
                string target = $"{to}/{Path.GetFileName(file)}";
                File.Copy(file, target, true);
                AssetDatabase.ImportAsset(target, ImportAssetOptions.ForceSynchronousImport);
                copied.Add(target);
            }

            return copied;
        }

        private static MapKind Classify(string file)
        {
            string name = Path.GetFileNameWithoutExtension(file).ToLowerInvariant();
            if (name.EndsWith("_arm", StringComparison.Ordinal) || name.Contains("_arm_")) return MapKind.Arm;
            if (name.Contains("normalgl") || name.Contains("nor_gl") || name.Contains("_normal")) return MapKind.Normal;
            if (name.Contains("rough")) return MapKind.Roughness;
            if (name.Contains("metal")) return MapKind.Metallic;
            if (name.Contains("ambientocclusion") || name.EndsWith("_ao", StringComparison.Ordinal)) return MapKind.Occlusion;
            if (name.Contains("color") || name.Contains("diff") || name.Contains("albedo")) return MapKind.Albedo;
            return MapKind.Other;
        }

        private static int MaxTextureSize(string slug) =>
            slug.StartsWith("tex-cloth", StringComparison.Ordinal) || slug.StartsWith("tex-wood", StringComparison.Ordinal) ? 2048 : 1024;

        private static bool Finish(Report report, bool success)
        {
            report.Write(success);
            if (success) Debug.Log($"[Art] {report.Slug}: imported{(report.Warnings > 0 ? $" with {report.Warnings} warning(s)" : string.Empty)} → {ReportRoot}/{report.Slug}.md");
            return success;
        }

        /// <summary>ArtSource/Reports/&lt;slug&gt;.md, also echoed to the Console for warnings and errors.</summary>
        private sealed class Report
        {
            private readonly LicenseEntry entry;
            private readonly StringBuilder body = new StringBuilder();

            public Report(LicenseEntry entry) => this.entry = entry;

            public string Slug => entry.Slug;
            public int Warnings { get; private set; }

            public void Line(string text) => body.AppendLine("- " + text);

            public void Warn(string text)
            {
                Warnings++;
                body.AppendLine("- ⚠ " + text);
                Debug.LogWarning($"[Art] {Slug}: {text}");
            }

            public void Error(string text)
            {
                body.AppendLine("- ❌ " + text);
                Debug.LogError($"[Art] {Slug}: {text}");
            }

            public void Write(bool success)
            {
                Directory.CreateDirectory(ReportRoot);
                string header = $"# {Slug}\n\n- Status: {(success ? "imported" : "FAILED")}\n- Name: {entry["name"]}\n- Type: {entry.Type}\n- License: {entry.License}\n- Source: {entry["source_url"]}\n";
                File.WriteAllText($"{ReportRoot}/{Slug}.md", header + body);
            }
        }
    }
}
