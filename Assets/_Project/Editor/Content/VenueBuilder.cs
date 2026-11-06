using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace BilliardGame.Editor.Content
{
    /// <summary>
    /// BilliardGame ▸ Build Silk Road Lounge: the premium hall as an additive scene around the Main scene's table
    /// (table centre at the origin, floor at y = −0.8). Tiled dado, mosaic band, pointed arches, wooden ceiling, pool lamp,
    /// CC0 furniture, baked lightmaps and a baked reflection probe. Placeholder for the `venue-silk-road-lounge` model.
    /// </summary>
    internal static class VenueBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/" + ContentBuilder.SilkRoadScene + ".unity";

        private const float FloorY = -0.8f;
        public const float Floor = FloorY;
        public const string NavMeshPath = GeneratedArt.Root + "/Venues/silk-road-lounge/navmesh.asset";
        private const float HalfX = 5.5f, HalfZ = 4.5f, Height = 4.6f;
        private const float DadoHeight = 1.3f, BandHeight = 0.22f;

        private static readonly Color Warm = new Color(1f, 0.78f, 0.52f);

        [MenuItem("BilliardGame/Build Silk Road Lounge")]
        public static void Build() => Build(bake: true);

        public static void Build(bool bake)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            string folder = GeneratedArt.Folder("Venues/silk-road-lounge");
            var root = new GameObject("Silk Road Lounge").transform;

            Material floor = VenueMaterial(folder, "floor", "tex-venue-floor", Color.white, new Vector2(5f, 4f), 0.5f);
            Material carpet = VenueMaterial(folder, "carpet", "tex-venue-carpet", new Color(0.9f, 0.75f, 0.7f), new Vector2(3f, 2f), 0.1f);
            Material tiles = VenueMaterial(folder, "tiles", "tex-venue-tiles", Color.white, new Vector2(1f, 1f), 0.8f);
            Material mosaic = VenueMaterial(folder, "mosaic", "tex-venue-mosaic", Color.white, new Vector2(1f, 1f), 0.5f);
            Material plaster = VenueMaterial(folder, "plaster", "tex-venue-plaster", new Color(1f, 0.9f, 0.76f), new Vector2(3f, 1.5f), 0.15f);
            Material wood = VenueMaterial(folder, "wood", "tex-wood-dark", new Color(0.8f, 0.7f, 0.62f), new Vector2(2f, 1f), 0.55f);
            Material brass = GeneratedArt.Lit($"{folder}/brass.mat", new Color(0.85f, 0.62f, 0.3f), 0.75f, 1f);
            Material glow = GeneratedArt.Lit($"{folder}/lamp_glow.mat", Color.black, 0.2f, 0f);
            GeneratedArt.Emission(glow, null, Warm * 4f);

            BuildShell(root, floor, carpet, tiles, mosaic, plaster, wood);
            BuildArches(root, folder, tiles, wood, glow);
            BuildPoolLamp(root, wood, brass, glow);
            BuildCueRack(root, wood, brass);
            BuildProps(root);
            BuildLighting(root);

            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                GameObjectUtility.SetStaticEditorFlags(t.gameObject,
                    StaticEditorFlags.ContributeGI | StaticEditorFlags.ReflectionProbeStatic | StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
            }

            // Seats come after the static flags: the tea glass moves (characters drink from it).
            BuildSeats(root);
            PlayerSeats.BakeNavMesh(root.gameObject, FloorY, NavMeshPath);
            ConfigureLightingSettings(folder);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuild();
            if (!bake) return;

            Lightmapping.bakeCompleted -= OnBaked;
            Lightmapping.bakeCompleted += OnBaked;
            Lightmapping.BakeAsync();
            Debug.Log("[Venue] Silk Road Lounge built; lightmap bake started.");
        }

        private static void OnBaked()
        {
            Lightmapping.bakeCompleted -= OnBaked;
            DropRealtimeShadowCasters(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            Debug.Log($"[Venue] Lightmaps baked and saved: {ScenePath}");
        }

        /// <summary>
        /// The room's shadows are in its lightmaps; as realtime casters it would only add ~80 shadow draws for the
        /// match key light (and the ceiling would shade the table). Run after the bake so the bake still sees them.
        /// </summary>
        [MenuItem("BilliardGame/Venue/Drop Realtime Shadow Casters")]
        private static void DropRealtimeShadowCasters() => DropRealtimeShadowCasters(SceneManager.GetActiveScene());

        private static void DropRealtimeShadowCasters(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true)) renderer.shadowCastingMode = ShadowCastingMode.Off;
            }

            EditorSceneManager.MarkSceneDirty(scene);
        }

        private static void BuildShell(Transform root, Material floor, Material carpet, Material tiles, Material mosaic, Material plaster, Material wood)
        {
            BilliardSceneBuilder.Box("Floor", root, new Vector3(0f, FloorY - 0.02f, 0f), Quaternion.identity, new Vector3(2f * HalfX, 0.04f, 2f * HalfZ), floor);
            Tiled(BilliardSceneBuilder.Box("Carpet Border", root, Vector3.zero, Quaternion.identity, Vector3.one, mosaic), new Vector3(0f, FloorY + 0.003f, 0f), new Vector3(6.2f, 0.006f, 4.4f), 0.5f);
            BilliardSceneBuilder.Box("Carpet", root, new Vector3(0f, FloorY + 0.006f, 0f), Quaternion.identity, new Vector3(5.8f, 0.006f, 4.0f), carpet);

            float ceilingY = FloorY + Height;
            BilliardSceneBuilder.Box("Ceiling", root, new Vector3(0f, ceilingY + 0.05f, 0f), Quaternion.identity, new Vector3(2f * HalfX, 0.1f, 2f * HalfZ), wood);
            for (int i = -4; i <= 4; i++)
            {
                BilliardSceneBuilder.Box("Beam", root, new Vector3(i * 1.2f, ceilingY - 0.09f, 0f), Quaternion.identity, new Vector3(0.18f, 0.18f, 2f * HalfZ), wood);
            }

            var walls = new (string name, Vector3 centre, float length, Quaternion rotation)[]
            {
                ("North", new Vector3(0f, 0f, HalfZ), 2f * HalfX, Quaternion.identity),
                ("South", new Vector3(0f, 0f, -HalfZ), 2f * HalfX, Quaternion.Euler(0f, 180f, 0f)),
                ("East", new Vector3(HalfX, 0f, 0f), 2f * HalfZ, Quaternion.Euler(0f, 90f, 0f)),
                ("West", new Vector3(-HalfX, 0f, 0f), 2f * HalfZ, Quaternion.Euler(0f, -90f, 0f)),
            };

            foreach (var wall in walls)
            {
                // Each wall is built facing -Z locally (towards the room) and rotated into place.
                var pivot = new GameObject("Wall " + wall.name).transform;
                pivot.SetParent(root, false);
                pivot.SetPositionAndRotation(wall.centre, wall.rotation);
                float upper = Height - DadoHeight - BandHeight;
                Tiled(BilliardSceneBuilder.Box("Dado", pivot, Vector3.zero, Quaternion.identity, Vector3.one, tiles), new Vector3(0f, FloorY + DadoHeight * 0.5f, 0.05f), new Vector3(wall.length, DadoHeight, 0.1f), 1.0f);
                Tiled(BilliardSceneBuilder.Box("Band", pivot, Vector3.zero, Quaternion.identity, Vector3.one, mosaic), new Vector3(0f, FloorY + DadoHeight + BandHeight * 0.5f, 0.04f), new Vector3(wall.length, BandHeight, 0.12f), BandHeight);
                BilliardSceneBuilder.Box("Upper", pivot, Vector3.zero, Quaternion.identity, Vector3.one, plaster).transform.SetLocalPositionAndRotation(new Vector3(0f, FloorY + DadoHeight + BandHeight + upper * 0.5f, 0.06f), Quaternion.identity);
                pivot.Find("Upper").localScale = new Vector3(wall.length, upper, 0.1f);
                BilliardSceneBuilder.Box("Skirting", pivot, Vector3.zero, Quaternion.identity, Vector3.one, wood).transform.SetLocalPositionAndRotation(new Vector3(0f, FloorY + 0.06f, -0.01f), Quaternion.identity);
                pivot.Find("Skirting").localScale = new Vector3(wall.length, 0.12f, 0.04f);
            }
        }

        // Positions a unit box in its parent and keeps the tile pattern square by scaling UVs to the face size.
        private static void Tiled(GameObject box, Vector3 localPosition, Vector3 size, float tileSize)
        {
            box.transform.SetLocalPositionAndRotation(localPosition, Quaternion.identity);
            box.transform.localScale = size;
            var renderer = box.GetComponent<MeshRenderer>();
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            // shortcut: per-renderer UV scale breaks SRP batching for ~12 static wall pieces; fine for M1, bake into UVs later.
            block.SetVector("_BaseMap_ST", new Vector4(size.x / tileSize, size.y / tileSize, 0f, 0f));
            renderer.SetPropertyBlock(block);
        }

        private static void BuildArches(Transform root, string folder, Material tiles, Material wood, Material glow)
        {
            Mesh inner = GeneratedArt.SaveMesh(ArchMesh("Arch Inner", 1.3f, 1.7f), $"{folder}/arch_inner.asset");
            Mesh outer = GeneratedArt.SaveMesh(ArchMesh("Arch Frame", 1.5f, 1.8f), $"{folder}/arch_frame.asset");
            Material niche = GeneratedArt.Lit($"{folder}/niche.mat", new Color(0.3f, 0.5f, 0.85f), 0.7f, 0f);
            GeneratedArt.UseImportedMaps(niche, "tex-venue-tiles", new Vector2(2.5f, 3.5f), 1f);

            float baseY = FloorY + 0.35f;
            var placements = new List<(Vector3 position, Quaternion rotation)>();
            foreach (float x in new[] { -3.3f, 0f, 3.3f })
            {
                placements.Add((new Vector3(x, baseY, HalfZ - 0.01f), Quaternion.identity));
                placements.Add((new Vector3(x, baseY, -HalfZ + 0.01f), Quaternion.Euler(0f, 180f, 0f)));
            }

            foreach (float z in new[] { -2f, 2f })
            {
                placements.Add((new Vector3(HalfX - 0.01f, baseY, z), Quaternion.Euler(0f, 90f, 0f)));
                placements.Add((new Vector3(-HalfX + 0.01f, baseY, z), Quaternion.Euler(0f, -90f, 0f)));
            }

            int index = 0;
            foreach (var (position, rotation) in placements)
            {
                var arch = new GameObject($"Arch {index++}").transform;
                arch.SetParent(root, false);
                arch.SetPositionAndRotation(position, rotation);
                MeshObject("Frame", arch, outer, wood, new Vector3(0f, -0.1f, -0.005f));
                MeshObject("Niche", arch, inner, niche, new Vector3(0f, 0f, -0.012f));
                // Brass lantern glow at the crown of each arch.
                BilliardSceneBuilder.Box("Lantern", arch, Vector3.zero, Quaternion.identity, Vector3.one, glow).transform.SetLocalPositionAndRotation(new Vector3(0f, 2.15f, -0.08f), Quaternion.Euler(0f, 0f, 45f));
                arch.Find("Lantern").localScale = new Vector3(0.12f, 0.12f, 0.06f);
            }
        }

        private static void MeshObject(string name, Transform parent, Mesh mesh, Material material, Vector3 localPosition)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        /// <summary>Flat pointed (Islamic) arch facing -Z: rectangle up to <paramref name="springHeight"/>, two arcs to the apex.</summary>
        private static Mesh ArchMesh(string name, float width, float springHeight)
        {
            float half = width * 0.5f;
            float radius = width * 0.85f;
            var outline = new List<Vector2> { new Vector2(-half, 0f), new Vector2(half, 0f), new Vector2(half, springHeight) };
            const int steps = 12;
            float centre = radius - half;                                 // right arc centred left of the right edge
            float startAngle = 0f, endAngle = Mathf.Acos(centre / radius);
            for (int i = 1; i <= steps; i++)
            {
                float a = Mathf.Lerp(startAngle, endAngle, i / (float)steps);
                outline.Add(new Vector2(-centre + radius * Mathf.Cos(a), springHeight + radius * Mathf.Sin(a)));
            }

            for (int i = steps - 1; i >= 0; i--)
            {
                float a = Mathf.Lerp(startAngle, endAngle, i / (float)steps);
                outline.Add(new Vector2(centre - radius * Mathf.Cos(a), springHeight + radius * Mathf.Sin(a)));
            }

            var vertices = new List<Vector3> { new Vector3(0f, springHeight * 0.6f, 0f) };
            var uvs = new List<Vector2> { new Vector2(0.5f, springHeight * 0.6f) };
            foreach (Vector2 p in outline)
            {
                vertices.Add(new Vector3(p.x, p.y, 0f));
                uvs.Add(new Vector2(p.x / width + 0.5f, p.y / width));
            }

            var triangles = new List<int>();
            for (int i = 1; i < vertices.Count; i++)
            {
                int next = i + 1 < vertices.Count ? i + 1 : 1;
                triangles.Add(0);
                triangles.Add(next);
                triangles.Add(i);
            }

            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            Unwrapping.GenerateSecondaryUVSet(mesh);
            return mesh;
        }

        private static void BuildPoolLamp(Transform root, Material wood, Material brass, Material glow)
        {
            // Bottom at y = 1.9: every camera ray to the cloth passes in front of it (see TableSpace camera at y 3.1).
            var lamp = new GameObject("Pool Lamp").transform;
            lamp.SetParent(root, false);
            lamp.localPosition = new Vector3(0f, 1.9f, 0f);
            LocalBox("Shade", lamp, new Vector3(0f, 0.09f, 0f), new Vector3(1.9f, 0.14f, 0.42f), wood);
            LocalBox("Trim", lamp, new Vector3(0f, 0.17f, 0f), new Vector3(1.94f, 0.025f, 0.46f), brass);
            for (int i = -1; i <= 1; i++)
            {
                LocalBox("Diffuser", lamp, new Vector3(i * 0.6f, 0.012f, 0f), new Vector3(0.5f, 0.01f, 0.34f), glow);
            }

            float rodLength = FloorY + Height - 2.07f;
            for (int i = -1; i <= 1; i += 2)
            {
                LocalPrimitive(PrimitiveType.Cylinder, "Rod", lamp, new Vector3(i * 0.75f, 0.17f + rodLength * 0.5f, 0f), new Vector3(0.015f, rodLength * 0.5f, 0.015f), brass);
            }
        }

        private static GameObject LocalBox(string name, Transform parent, Vector3 localPosition, Vector3 scale, Material material) =>
            LocalPrimitive(PrimitiveType.Cube, name, parent, localPosition, scale, material);

        private static GameObject LocalPrimitive(PrimitiveType type, string name, Transform parent, Vector3 localPosition, Vector3 scale, Material material)
        {
            GameObject go = BilliardSceneBuilder.Primitive(type, name, parent, Vector3.zero, Quaternion.identity, scale, material);
            go.transform.SetLocalPositionAndRotation(localPosition, Quaternion.identity);
            return go;
        }

        private static void BuildCueRack(Transform root, Material wood, Material brass)
        {
            var rack = new GameObject("Cue Rack").transform;
            rack.SetParent(root, false);
            rack.SetPositionAndRotation(new Vector3(HalfX - 0.06f, FloorY, -0.3f), Quaternion.Euler(0f, 90f, 0f));
            LocalBox("Board", rack, new Vector3(0f, 1.05f, 0f), new Vector3(1.3f, 1.5f, 0.04f), wood);
            LocalBox("Shelf", rack, new Vector3(0f, 0.32f, -0.06f), new Vector3(1.3f, 0.04f, 0.12f), wood);
            LocalBox("Holder", rack, new Vector3(0f, 1.6f, -0.05f), new Vector3(1.3f, 0.05f, 0.08f), brass);

            var cue = AssetDatabase.LoadAssetAtPath<GameObject>($"{GeneratedArt.Root}/Cues/cue_house/cue_house.prefab");
            if (cue == null) return;
            for (int i = 0; i < 6; i++)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(cue, rack);
                // Tip up, butt resting on the shelf.
                instance.transform.SetLocalPositionAndRotation(new Vector3(-0.5f + i * 0.2f, 1.82f, -0.07f), Quaternion.Euler(90f, 0f, 0f));
            }
        }

        /// <summary>Player seats on the two armchairs beside the table (docs/GDD.md §8.1).</summary>
        public static void BuildSeats(Transform root) => PlayerSeats.Build(root, FloorY,
            new[] { (new Vector3(-2.75f, 0f, 1.35f), 145f), (new Vector3(2.75f, 0f, 1.35f), -145f) }, buildChairs: false);

        private static void BuildProps(Transform root)
        {
            Prop(root, "prop-chinese-armchair", new Vector3(-2.75f, FloorY, 1.35f), 145f);
            Prop(root, "prop-chinese-armchair", new Vector3(2.75f, FloorY, 1.35f), -145f);
            Prop(root, "prop-chinese-tea-table", new Vector3(-3.1f, FloorY, -0.15f), 90f);
            Prop(root, "prop-brass-vase", new Vector3(-3.1f, FloorY + 0.5f, -0.15f), 0f, tabletop: true);
            Prop(root, "prop-chinese-stool", new Vector3(3.05f, FloorY, -0.5f), 10f);
            Prop(root, "prop-chinese-stool", new Vector3(3.05f, FloorY, 0.3f), -15f);
            Prop(root, "prop-chinese-sofa", new Vector3(0f, FloorY, HalfZ - 0.55f), 180f);
            Prop(root, "prop-chinese-tea-table", new Vector3(0f, FloorY, HalfZ - 1.5f), 0f);
            Prop(root, "prop-brass-vase", new Vector3(0.15f, FloorY + 0.5f, HalfZ - 1.5f), 0f, tabletop: true);
            Prop(root, "prop-chinese-armchair", new Vector3(-1.9f, FloorY, HalfZ - 0.6f), 180f);
            Prop(root, "prop-chinese-armchair", new Vector3(1.9f, FloorY, HalfZ - 0.6f), 180f);
            Prop(root, "prop-chinese-stool", new Vector3(-HalfX + 0.6f, FloorY, -2.8f), 40f);
        }

        private static void Prop(Transform root, string slug, Vector3 position, float yaw, bool tabletop = false)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Art.ArtImporter.ArtRoot}/Props/{slug}/{slug}.prefab");
            if (prefab == null)
            {
                Debug.LogWarning($"[Venue] {slug} not imported; skipped (run BilliardGame ▸ Art ▸ Import Incoming).");
                return;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root);
            // Keep the FBX root's own axis-conversion rotation; only add the yaw.
            instance.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f) * instance.transform.rotation);
            if (tabletop)
            {
                // Sit the vase on whatever surface height was given, using its own bounds.
                Bounds b = new Bounds(position, Vector3.zero);
                foreach (Renderer r in instance.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
                instance.transform.position += Vector3.up * (position.y - b.min.y);
            }
        }

        private static void BuildLighting(Transform root)
        {
            var lights = new GameObject("Baked Lights").transform;
            lights.SetParent(root, false);
            for (int i = -1; i <= 1; i++) BakedPoint(lights, new Vector3(i * 0.6f, 1.8f, 0f), 2.6f, 4.5f);
            for (int i = 0; i < 16; i++)
            {
                Transform arch = root.Find($"Arch {i}");
                if (arch == null) continue;
                BakedPoint(lights, arch.position + arch.rotation * new Vector3(0f, 2.1f, -0.35f), 1.4f, 3.2f);
            }

            var probe = new GameObject("Hall Reflection Probe").AddComponent<ReflectionProbe>();
            probe.transform.SetParent(root, false);
            probe.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            probe.mode = ReflectionProbeMode.Baked;
            probe.size = new Vector3(2f * HalfX, Height, 2f * HalfZ);
            probe.center = new Vector3(0f, FloorY + Height * 0.5f - 0.6f, 0f);
            probe.boxProjection = true;
            probe.resolution = 128;
            probe.importance = 0;
        }

        private static void BakedPoint(Transform parent, Vector3 position, float intensity, float range)
        {
            var light = new GameObject("Point").AddComponent<Light>();
            light.transform.SetParent(parent, false);
            light.transform.position = position;
            light.type = LightType.Point;
            light.color = Warm;
            light.intensity = intensity;
            light.range = range;
            light.lightmapBakeType = LightmapBakeType.Baked;
            light.shadows = LightShadows.Soft;
        }

        private static void ConfigureLightingSettings(string folder)
        {
            string path = $"{folder}/SilkRoadLighting.lighting";
            var settings = AssetDatabase.LoadAssetAtPath<LightingSettings>(path);
            if (settings == null)
            {
                settings = new LightingSettings { name = "SilkRoadLighting" };
                AssetDatabase.CreateAsset(settings, path);
            }

            settings.bakedGI = true;
            settings.realtimeGI = false;
            settings.lightmapper = LightingSettings.Lightmapper.ProgressiveGPU;
            settings.lightmapResolution = 10f;
            settings.lightmapMaxSize = 1024;
            settings.directSampleCount = 32;
            settings.indirectSampleCount = 128;
            settings.environmentSampleCount = 64;
            settings.maxBounces = 2;
            settings.lightmapCompression = LightmapCompression.NormalQuality;
            settings.mixedBakeMode = MixedLightingMode.IndirectOnly;
            settings.directionalityMode = LightmapsMode.NonDirectional;
            EditorUtility.SetDirty(settings);
            Lightmapping.lightingSettings = settings;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.35f, 0.3f, 0.28f);
            RenderSettings.ambientEquatorColor = new Color(0.25f, 0.2f, 0.16f);
            RenderSettings.ambientGroundColor = new Color(0.08f, 0.06f, 0.05f);
        }

        private static Material VenueMaterial(string folder, string name, string slug, Color tint, Vector2 tiling, float smoothness)
        {
            Material material = GeneratedArt.Lit($"{folder}/{name}.mat", tint, smoothness, 0f);
            GeneratedArt.UseImportedMaps(material, slug, tiling);
            material.SetColor("_BaseColor", tint);
            return material;
        }

        private static void AddToBuild()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (scenes.Exists(s => s.path == ScenePath)) return;
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
