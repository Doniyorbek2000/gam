using System.Collections.Generic;
using System.Linq;
using BilliardGame.Content;
using BilliardGame.Meta;
using UnityEditor;
using UnityEngine;

namespace BilliardGame.Editor.Content
{
    /// <summary>
    /// BilliardGame ▸ Build Content: procedural cue models, table skins, SkinDefinitions, venues, icons and the catalog.
    /// Imported cue SkinDefinitions (Art/Cues) are picked up too. Placeholder art until the models in ART_PIPELINE §6 arrive.
    /// </summary>
    internal static class ContentBuilder
    {
        public const string ContentFolder = "Assets/_Project/Content";
        public const string CatalogPath = ContentFolder + "/SkinCatalog.asset";
        public const string SilkRoadScene = "SilkRoadLounge";

        private enum Body { WoodDark, WoodLight, Lacquer, Carbon }

        private sealed class CueSpec
        {
            public string Id, Name;
            public Rarity Rarity;
            public bool Default;
            public Body Body;
            public Color BodyTint = Color.white;
            public float BodySmooth = 0.75f, BodyMetal;
            public Color Ferrule = new Color(0.95f, 0.94f, 0.9f), Tip = new Color(0.15f, 0.35f, 0.75f);
            public Color AccentA = new Color(0.9f, 0.9f, 0.88f), AccentB = new Color(0.1f, 0.1f, 0.1f);
            public Color Shaft = new Color(0.93f, 0.82f, 0.62f), Wrap = new Color(0.08f, 0.08f, 0.09f), Cap = new Color(0.05f, 0.05f, 0.05f);
            public float TrimMetal = 0.2f, TrimSmooth = 0.7f;
            public Color Glow = Color.black;
            public int Rings;
            public bool HasWrap = true;
        }

        private sealed class TableSpec
        {
            public string Id, Name;
            public Rarity Rarity;
            public bool Default;
            public Color Cloth;
            public string RailSlug;
            public Color RailTint = Color.white;
            public float RailSmooth = 0.6f, RailMetal;
        }

        private static readonly Color Gold = new Color(1f, 0.76f, 0.3f);

        private static readonly CueSpec[] Cues =
        {
            new CueSpec { Id = "cue_house", Name = "Garaj kiyi", Rarity = Rarity.Common, Default = true, Body = Body.WoodLight, BodyTint = new Color(0.75f, 0.55f, 0.38f) },
            new CueSpec { Id = "cue_maple", Name = "Zarang", Rarity = Rarity.Common, Body = Body.WoodLight, AccentA = new Color(0.2f, 0.45f, 0.85f), Wrap = new Color(0.12f, 0.2f, 0.4f) },
            new CueSpec { Id = "cue_classic", Name = "Klassik yong'oq", Rarity = Rarity.Rare, Body = Body.WoodDark, Rings = 2, AccentA = new Color(0.92f, 0.88f, 0.76f), AccentB = new Color(0.85f, 0.65f, 0.3f), TrimMetal = 0.5f },
            new CueSpec { Id = "cue_crimson", Name = "Qirmizi", Rarity = Rarity.Rare, Body = Body.Lacquer, BodyTint = new Color(0.55f, 0.04f, 0.06f), BodySmooth = 0.88f, AccentA = new Color(0.8f, 0.82f, 0.85f), AccentB = new Color(0.8f, 0.82f, 0.85f), TrimMetal = 0.8f },
            new CueSpec { Id = "cue_carbon", Name = "Karbon Ilon", Rarity = Rarity.Epic, Body = Body.Carbon, BodySmooth = 0.85f, BodyMetal = 0.4f, Shaft = new Color(0.12f, 0.12f, 0.13f), AccentA = new Color(0.65f, 0.3f, 1f), AccentB = new Color(0.3f, 0.3f, 0.32f), Wrap = new Color(0.05f, 0.05f, 0.06f), Glow = new Color(0.65f, 0.3f, 1f) * 2.5f, Rings = 1, TrimMetal = 0.7f },
            new CueSpec { Id = "cue_ornate", Name = "Samarqand oltini", Rarity = Rarity.Legendary, Body = Body.WoodDark, BodyTint = new Color(0.8f, 0.7f, 0.6f), Rings = 5, AccentA = Gold, AccentB = new Color(0.1f, 0.35f, 0.6f), Wrap = new Color(0.25f, 0.12f, 0.05f), TrimMetal = 0.9f, TrimSmooth = 0.8f, Glow = Gold * 1.2f },
            new CueSpec { Id = "cue_dragon", Name = "Ipak ajdari", Rarity = Rarity.Mythic, Body = Body.Lacquer, BodyTint = new Color(0.7f, 0.05f, 0.2f), BodySmooth = 0.9f, Rings = 6, AccentA = Gold, AccentB = new Color(1f, 0.3f, 0.45f), Wrap = new Color(0.3f, 0.02f, 0.08f), TrimMetal = 0.9f, TrimSmooth = 0.85f, Glow = new Color(1f, 0.25f, 0.4f) * 3f },
        };

        private static readonly TableSpec[] Tables =
        {
            new TableSpec { Id = "table_classic", Name = "Klassik", Rarity = Rarity.Common, Default = true, Cloth = new Color(0.07f, 0.42f, 0.22f), RailSlug = "tex-wood-dark" },
            new TableSpec { Id = "table_royal", Name = "Qirollik ko'k", Rarity = Rarity.Rare, Cloth = new Color(0.07f, 0.22f, 0.62f), RailSlug = "tex-wood-light", RailTint = new Color(1f, 0.85f, 0.7f) },
            new TableSpec { Id = "table_modern", Name = "Modern", Rarity = Rarity.Epic, Cloth = new Color(0.2f, 0.22f, 0.26f), RailTint = new Color(0.05f, 0.05f, 0.06f), RailSmooth = 0.75f, RailMetal = 0.85f },
            new TableSpec { Id = "table_silk", Name = "Ipak yo'li", Rarity = Rarity.Legendary, Cloth = new Color(0.02f, 0.36f, 0.4f), RailSlug = "tex-wood-dark", RailTint = new Color(1.1f, 0.85f, 0.55f), RailMetal = 0.25f, RailSmooth = 0.7f },
        };

        [MenuItem("BilliardGame/Build Content")]
        public static void Build()
        {
            BallMaterialFactory.EnsureFolder(ContentFolder + "/Skins");
            BallMaterialFactory.EnsureFolder(ContentFolder + "/Venues");
            var skins = new List<SkinDefinition>();
            foreach (CueSpec spec in Cues) skins.Add(BuildCue(spec));
            foreach (TableSpec spec in Tables) skins.Add(BuildTable(spec));
            skins.AddRange(ImportedCueSkins(skins));

            VenueDefinition[] venues =
            {
                Venue("garage", "Garaj", string.Empty),
                Venue("silk_road_lounge", "Silk Road Lounge", SilkRoadScene),
            };

            var catalog = AssetDatabase.LoadAssetAtPath<SkinCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<SkinCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            catalog.Configure(skins.ToArray(), venues);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Content] Built {skins.Count} skins and {venues.Length} venues → {CatalogPath}");
        }

        public static SkinCatalog LoadCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<SkinCatalog>(CatalogPath);
            if (catalog == null)
            {
                Build();
                catalog = AssetDatabase.LoadAssetAtPath<SkinCatalog>(CatalogPath);
            }

            return catalog;
        }

        private static SkinDefinition BuildCue(CueSpec spec)
        {
            string folder = GeneratedArt.Folder("Cues/" + spec.Id);
            Material body = BodyMaterial(spec, $"{folder}/{spec.Id}_body.mat");

            Color[] cells = { spec.Ferrule, spec.Tip, spec.AccentA, spec.AccentB, spec.Shaft, spec.Wrap, spec.Cap, Color.black };
            Texture2D atlas = GeneratedArt.SavePng($"{folder}/{spec.Id}_trim.png", CueModelBuilder.AtlasCells * 8, 8, CueModelBuilder.Atlas(cells, out _, out _), wrap: TextureWrapMode.Clamp);
            Material trim = GeneratedArt.Lit($"{folder}/{spec.Id}_trim.mat", Color.white, 1f, 0f, atlas);
            // Per-cell metal/smoothness (R/A) so accents can be metal while the shaft, ferrule and wrap stay dielectric.
            float[] metal = { 0f, 0f, spec.TrimMetal, spec.TrimMetal * 0.7f, 0f, 0f, 0.2f, 0f };
            float[] smooth = { 0.8f, 0.2f, spec.TrimSmooth, spec.TrimSmooth, 0.72f, 0.25f, 0.6f, 0f };
            Color[] maskCells = metal.Select((m, i) => new Color(m, 1f, 0f, smooth[i])).ToArray();
            Texture2D trimMask = GeneratedArt.SavePng($"{folder}/{spec.Id}_trim_mask.png", CueModelBuilder.AtlasCells * 8, 8, CueModelBuilder.Atlas(maskCells, out _, out _), linear: true, wrap: TextureWrapMode.Clamp);
            trim.SetTexture("_MetallicGlossMap", trimMask);
            trim.EnableKeyword("_METALLICSPECGLOSSMAP");
            if (spec.Glow.maxColorComponent > 0f)
            {
                Color[] glowCells = { Color.black, Color.black, Color.white, Color.black, Color.black, Color.black, Color.black, Color.black };
                Texture2D glow = GeneratedArt.SavePng($"{folder}/{spec.Id}_glow.png", CueModelBuilder.AtlasCells * 8, 8, CueModelBuilder.Atlas(glowCells, out _, out _), wrap: TextureWrapMode.Clamp);
                GeneratedArt.Emission(trim, glow, spec.Glow);
            }

            Mesh mesh = GeneratedArt.SaveMesh(CueModelBuilder.Build(spec.Id, CueModelBuilder.Profile(spec.Rings, spec.HasWrap)), $"{folder}/{spec.Id}.asset");
            var root = new GameObject(spec.Id);
            root.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = root.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = new[] { body, trim };
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{folder}/{spec.Id}.prefab");
            Object.DestroyImmediate(root);

            Sprite icon = CueIcon(prefab, $"{folder}/{spec.Id}_icon.png");
            SkinDefinition skin = Skin(spec.Id);
            skin.Configure(spec.Id, ItemType.Cue, spec.Rarity, spec.Name, spec.Default, prefab, null, null, icon, spec.Default ? "Boshlang'ich" : "Yashik");
            EditorUtility.SetDirty(skin);
            return skin;
        }

        private static Material BodyMaterial(CueSpec spec, string path)
        {
            switch (spec.Body)
            {
                case Body.Carbon:
                    return GeneratedArt.Lit(path, Color.white, spec.BodySmooth, spec.BodyMetal, CarbonWeave(), new Vector2(40f, 3f));
                case Body.Lacquer:
                    Material lacquer = GeneratedArt.Lit(path, spec.BodyTint, spec.BodySmooth, spec.BodyMetal);
                    GeneratedArt.UseImportedMaps(lacquer, "tex-wood-light", new Vector2(2f, 0.5f), 0.3f);
                    lacquer.SetColor("_BaseColor", spec.BodyTint);
                    return lacquer;
                default:
                    Material wood = GeneratedArt.Lit(path, spec.BodyTint, spec.BodySmooth, spec.BodyMetal);
                    GeneratedArt.UseImportedMaps(wood, spec.Body == Body.WoodDark ? "tex-wood-dark" : "tex-wood-light", new Vector2(2f, 0.5f), 0.5f);
                    return wood;
            }
        }

        private static Texture2D CarbonWeave()
        {
            string path = GeneratedArt.Folder("Cues") + "/carbon_weave.png";
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;

            const int size = 64;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // 2/2 twill: tows alternate direction every 16 px, shifted one tow per row band.
                    int band = ((x / 16) + (y / 16)) % 2;
                    float along = band == 0 ? (x % 16) / 16f : (y % 16) / 16f;
                    float sheen = 0.5f + 0.5f * Mathf.Sin(along * Mathf.PI);
                    byte v = (byte)(18 + 40 * sheen * (band == 0 ? 1f : 0.7f));
                    pixels[y * size + x] = new Color32(v, v, (byte)(v + 3), 255);
                }
            }

            return GeneratedArt.SavePng(path, size, size, pixels);
        }

        private static SkinDefinition BuildTable(TableSpec spec)
        {
            string folder = GeneratedArt.Folder("Tables/" + spec.Id);
            Material cloth = GeneratedArt.Lit($"{folder}/{spec.Id}_cloth.mat", spec.Cloth, 0.1f, 0f);
            GeneratedArt.UseImportedMaps(cloth, "tex-cloth-felt", new Vector2(6f, 3f), 0.35f);
            cloth.SetFloat("_Smoothness", 0.35f);

            Material rail = GeneratedArt.Lit($"{folder}/{spec.Id}_rail.mat", spec.RailTint, spec.RailSmooth, spec.RailMetal);
            if (spec.RailSlug != null) GeneratedArt.UseImportedMaps(rail, spec.RailSlug, new Vector2(3f, 1f), 0.6f);

            GameObject mini = MiniTable(cloth, rail, out _, out _);
            Sprite icon = IconStudio.Render(mini, $"{folder}/{spec.Id}_icon.png", new Vector3(1.25f, 1.55f, -1.55f), new Vector3(0f, 0.42f, 0f), 34f);
            SkinDefinition skin = Skin(spec.Id);
            skin.Configure(spec.Id, ItemType.Table, spec.Rarity, spec.Name, spec.Default, null, cloth, rail, icon, spec.Default ? "Boshlang'ich" : "Yashik");
            EditorUtility.SetDirty(skin);
            return skin;
        }

        /// <summary>Lobby/inventory display table, 1.2 m long, cloth and rail renderers returned for skinning.</summary>
        public static GameObject MiniTable(Material cloth, Material rail, out Renderer[] clothRenderers, out Renderer[] railRenderers)
        {
            var root = new GameObject("Mini Table");
            Material dark = BallMaterialFactory.Lit("Pocket", new Color(0.01f, 0.01f, 0.01f), 0f);
            const float length = 1.2f, width = 0.62f, top = 0.62f, railWidth = 0.07f;
            GameObject bed = BilliardSceneBuilder.Box("Cloth", root.transform, new Vector3(0f, top, 0f), Quaternion.identity, new Vector3(length, 0.03f, width), cloth);
            var rails = new List<Renderer>
            {
                BilliardSceneBuilder.Box("Rail N", root.transform, new Vector3(0f, top + 0.02f, width * 0.5f + railWidth * 0.5f), Quaternion.identity, new Vector3(length + 2f * railWidth, 0.06f, railWidth), rail).GetComponent<Renderer>(),
                BilliardSceneBuilder.Box("Rail S", root.transform, new Vector3(0f, top + 0.02f, -width * 0.5f - railWidth * 0.5f), Quaternion.identity, new Vector3(length + 2f * railWidth, 0.06f, railWidth), rail).GetComponent<Renderer>(),
                BilliardSceneBuilder.Box("Rail E", root.transform, new Vector3(length * 0.5f + railWidth * 0.5f, top + 0.02f, 0f), Quaternion.identity, new Vector3(railWidth, 0.06f, width), rail).GetComponent<Renderer>(),
                BilliardSceneBuilder.Box("Rail W", root.transform, new Vector3(-length * 0.5f - railWidth * 0.5f, top + 0.02f, 0f), Quaternion.identity, new Vector3(railWidth, 0.06f, width), rail).GetComponent<Renderer>(),
                BilliardSceneBuilder.Box("Apron", root.transform, new Vector3(0f, top - 0.08f, 0f), Quaternion.identity, new Vector3(length + 0.1f, 0.13f, width + 0.1f), rail).GetComponent<Renderer>(),
            };
            for (int sx = -1; sx <= 1; sx += 2)
            {
                for (int sz = -1; sz <= 1; sz += 2)
                {
                    rails.Add(BilliardSceneBuilder.Box("Leg", root.transform, new Vector3(sx * (length * 0.5f - 0.05f), (top - 0.14f) * 0.5f, sz * (width * 0.5f - 0.04f)), Quaternion.identity, new Vector3(0.08f, top - 0.14f, 0.08f), rail).GetComponent<Renderer>());
                    BilliardSceneBuilder.Primitive(PrimitiveType.Cylinder, "Pocket", root.transform, new Vector3(sx * length * 0.5f, top + 0.052f, sz * width * 0.5f), Quaternion.identity, new Vector3(0.07f, 0.001f, 0.07f), dark);
                }

                BilliardSceneBuilder.Primitive(PrimitiveType.Cylinder, "Pocket", root.transform, new Vector3(0f, top + 0.052f, sx * width * 0.5f), Quaternion.identity, new Vector3(0.065f, 0.001f, 0.065f), dark);
            }

            clothRenderers = new[] { bed.GetComponent<Renderer>() };
            railRenderers = rails.ToArray();
            return root;
        }

        private static Sprite CueIcon(GameObject prefab, string path)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            // Diagonal, framed on the decorated butt half (z 0.74..1.47), which is where cues differ.
            Vector3 axis = new Vector3(1f, 1f, 0f).normalized;
            instance.transform.rotation = Quaternion.LookRotation(axis, Vector3.back);
            instance.transform.position = -axis * 1.02f;
            return IconStudio.Render(instance, path, new Vector3(0f, 0f, -1.25f), Vector3.zero, 30f);
        }

        private static IEnumerable<SkinDefinition> ImportedCueSkins(List<SkinDefinition> generated)
        {
            string folder = $"{Art.ArtImporter.ArtRoot}/Cues";
            if (!AssetDatabase.IsValidFolder(folder)) return Enumerable.Empty<SkinDefinition>();
            return AssetDatabase.FindAssets("t:SkinDefinition", new[] { folder })
                .Select(guid => AssetDatabase.LoadAssetAtPath<SkinDefinition>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(s => s != null && generated.All(g => g.Id != s.Id));
        }

        private static SkinDefinition Skin(string id)
        {
            string path = $"{ContentFolder}/Skins/{id}.asset";
            var skin = AssetDatabase.LoadAssetAtPath<SkinDefinition>(path);
            if (skin != null) return skin;
            skin = ScriptableObject.CreateInstance<SkinDefinition>();
            AssetDatabase.CreateAsset(skin, path);
            return skin;
        }

        private static VenueDefinition Venue(string id, string name, string scene)
        {
            string path = $"{ContentFolder}/Venues/{id}.asset";
            var venue = AssetDatabase.LoadAssetAtPath<VenueDefinition>(path);
            if (venue == null)
            {
                venue = ScriptableObject.CreateInstance<VenueDefinition>();
                AssetDatabase.CreateAsset(venue, path);
            }

            venue.Configure(id, name, scene, null);
            EditorUtility.SetDirty(venue);
            return venue;
        }
    }
}
