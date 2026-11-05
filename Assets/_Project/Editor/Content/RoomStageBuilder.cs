using System.Collections.Generic;
using System.Linq;
using BilliardGame.Content;
using BilliardGame.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BilliardGame.Editor.Content
{
    /// <summary>
    /// "My Room" (docs/UI_UX.md §4.1), built 80 m from the match table: floor, three walls, a small table, a six-cue
    /// rack, an eight-trophy shelf and three poster frames. Starts hidden; <see cref="RoomStage.Show"/> turns it on.
    /// </summary>
    internal static class RoomStageBuilder
    {
        public static readonly Vector3 Origin = new Vector3(80f, 0f, 0f);
        private const float HalfWidth = 2.6f;
        private const float BackZ = 1.9f;
        private const float WallHeight = 3f;

        public static RoomStage Build()
        {
            string folder = GeneratedArt.Folder("Room");
            var root = new GameObject("Room Stage");
            root.transform.position = Origin;
            var stage = root.AddComponent<RoomStage>();

            SkinCatalog catalog = ContentBuilder.LoadCatalog();
            Material wallMat = catalog.Find("wall_plain")?.Cloth ?? GeneratedArt.Lit($"{folder}/wall.mat", new Color(0.2f, 0.22f, 0.28f), 0.2f, 0f);
            Material floorMat = catalog.Find("floor_wood")?.Cloth ?? GeneratedArt.Lit($"{folder}/floor.mat", new Color(0.3f, 0.2f, 0.12f), 0.5f, 0f);
            Material shelf = GeneratedArt.Lit($"{folder}/shelf.mat", new Color(0.16f, 0.1f, 0.06f), 0.45f, 0f);
            Material gold = GeneratedArt.Lit($"{folder}/trophy_gold.mat", new Color(1f, 0.76f, 0.3f), 0.85f, 1f);
            Material frameMat = GeneratedArt.Lit($"{folder}/poster_frame.mat", new Color(0.08f, 0.08f, 0.1f), 0.6f, 0.4f);
            MatteRoomMaterials(catalog, wallMat, floorMat, shelf);

            GameObject floor = Box("Floor", root.transform, new Vector3(0f, -0.01f, -1.5f), new Vector3(HalfWidth * 2f, 0.02f, 8f), floorMat);
            var walls = new List<Renderer>
            {
                Box("Back Wall", root.transform, new Vector3(0f, WallHeight * 0.5f, BackZ + 0.05f), new Vector3(HalfWidth * 2f, WallHeight, 0.1f), wallMat).GetComponent<Renderer>(),
                Box("Left Wall", root.transform, new Vector3(-HalfWidth - 0.05f, WallHeight * 0.5f, -1.5f), new Vector3(0.1f, WallHeight, 7f), wallMat).GetComponent<Renderer>(),
                Box("Right Wall", root.transform, new Vector3(HalfWidth + 0.05f, WallHeight * 0.5f, -1.5f), new Vector3(0.1f, WallHeight, 7f), wallMat).GetComponent<Renderer>(),
            };
            // Ceiling: shadows the scene's directional key light, which otherwise floods the left wall through the open top.
            Box("Ceiling", root.transform, new Vector3(0f, WallHeight + 0.05f, -1.5f), new Vector3(HalfWidth * 2f + 0.2f, 0.1f, 8f), shelf);

            // Table in the middle (same mini table + model holder as the lobby preview).
            SkinDefinition defaultTable = catalog.Skins.First(s => s.Type == BilliardGame.Meta.ItemType.Table);
            GameObject mini = ContentBuilder.MiniTable(defaultTable.Cloth, defaultTable.Rail, out Renderer[] cloth, out Renderer[] rails);
            mini.name = "Room Table";
            mini.transform.SetParent(root.transform, false);
            mini.transform.localPosition = new Vector3(0f, 0f, 0.2f);
            mini.transform.localScale = Vector3.one * 1.15f;
            var table = mini.AddComponent<SkinTarget>();
            SceneWiring.SetArray(table, "clothRenderers", cloth);
            SceneWiring.SetArray(table, "railRenderers", rails);
            LobbyStageBuilder.WireTableModel(table, mini.transform);

            // Cue rack on the left of the back wall: cues stand tip up, butt on the floor, against a board.
            Box("Rack Board", root.transform, new Vector3(-1.75f, 0.9f, BackZ - 0.02f), new Vector3(1.2f, 1.6f, 0.04f), shelf);
            Box("Rack Rail", root.transform, new Vector3(-1.75f, 1.3f, BackZ - 0.08f), new Vector3(1.2f, 0.05f, 0.08f), shelf);
            var rack = new List<SkinTarget>();
            for (int i = 0; i < 6; i++)
            {
                var slot = new GameObject("Rack Cue " + i);
                slot.transform.SetParent(root.transform, false);
                // CueModel: pivot at the tip, butt along +Z (pointing down here).
                slot.transform.SetLocalPositionAndRotation(new Vector3(-2.25f + i * 0.2f, 1.5f, BackZ - 0.1f), Quaternion.LookRotation(Vector3.down, Vector3.back));
                var target = slot.AddComponent<SkinTarget>();
                SceneWiring.Set(target, "cueHolder", slot.transform);
                rack.Add(target);
            }

            // Trophy shelf on the right.
            Box("Trophy Shelf", root.transform, new Vector3(1.6f, 1.05f, BackZ - 0.12f), new Vector3(1.8f, 0.05f, 0.24f), shelf);
            var trophies = new List<GameObject>();
            for (int i = 0; i < 8; i++)
            {
                var trophy = new GameObject("Trophy " + i);
                trophy.transform.SetParent(root.transform, false);
                trophy.transform.localPosition = new Vector3(0.85f + i * 0.21f, 1.08f, BackZ - 0.12f);
                float size = i % 3 == 0 ? 1.2f : 1f;
                Local(PrimitiveType.Cube, "Base", trophy.transform, new Vector3(0f, 0.02f, 0f), new Vector3(0.09f, 0.04f, 0.09f) * size, shelf);
                Local(PrimitiveType.Cylinder, "Stem", trophy.transform, new Vector3(0f, 0.08f, 0f), new Vector3(0.025f, 0.04f, 0.025f) * size, gold);
                Local(PrimitiveType.Sphere, "Cup", trophy.transform, new Vector3(0f, 0.16f, 0f) * size, new Vector3(0.11f, 0.09f, 0.11f) * size, gold);
                trophies.Add(trophy);
            }

            // Three poster frames above the table.
            Material poster = StickerDecals.Material();
            var posters = new List<Renderer>();
            for (int i = 0; i < 3; i++)
            {
                float x = (i - 1) * 0.75f;
                Box("Poster Frame " + i, root.transform, new Vector3(x, 2.1f, BackZ - 0.01f), new Vector3(0.62f, 0.62f, 0.03f), frameMat);
                GameObject quad = Local(PrimitiveType.Quad, "Poster " + i, root.transform, new Vector3(x, 2.1f, BackZ - 0.03f), new Vector3(0.54f, 0.54f, 1f), poster);
                quad.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                posters.Add(quad.GetComponent<Renderer>());
            }

            Transform orbit = new GameObject("Orbit").transform;
            orbit.SetParent(root.transform, false);
            orbit.localPosition = new Vector3(0f, 0.8f, 0.4f);
            Camera camera = BuildCamera(orbit);
            Lights(root.transform);

            SceneWiring.Set(stage, "roomCamera", camera);
            SceneWiring.Set(stage, "orbit", orbit);
            SceneWiring.Set(stage, "table", table);
            SceneWiring.SetArray(stage, "walls", walls.ToArray());
            SceneWiring.Set(stage, "floor", floor.GetComponent<Renderer>());
            SceneWiring.SetArray(stage, "rack", rack.ToArray());
            SceneWiring.SetArray(stage, "trophies", trophies.ToArray());
            SceneWiring.SetArray(stage, "posters", posters.ToArray());
            root.SetActive(false);
            return stage;
        }

        private static Camera BuildCamera(Transform orbit)
        {
            var go = new GameObject("Room Camera");
            go.transform.SetParent(orbit, false);
            go.transform.localPosition = new Vector3(0f, 0.95f, -4.3f);
            go.transform.LookAt(orbit.position + new Vector3(0f, 0.35f, 0f));
            var camera = go.AddComponent<Camera>();
            camera.fieldOfView = 48f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 12f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.03f, 0.04f, 0.06f);
            camera.depth = 1;
            camera.enabled = false;
            UniversalAdditionalCameraData data = camera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.renderShadows = true;
            return camera;
        }

        private static void Lights(Transform root)
        {
            Spot("Room Key", root, new Vector3(0f, 2.9f, -0.6f), new Vector3(0f, 0.7f, 0.3f), new Color(1f, 0.92f, 0.8f), 14f, 70f, true);
            Spot("Room Wall", root, new Vector3(0f, 2.8f, 0.6f), new Vector3(0f, 1.6f, BackZ), new Color(1f, 0.85f, 0.65f), 4f, 100f, false);
            Spot("Room Fill", root, new Vector3(-0.5f, 2.2f, -2.4f), new Vector3(0f, 0.8f, 0.5f), new Color(0.55f, 0.65f, 1f), 2.5f, 70f, false);
        }

        // The closed room has no probe of its own, so its surfaces would reflect the bright lobby skybox at grazing
        // angles (the left wall turned white). Walls/floors are matte anyway: turn environment reflections off.
        private static void MatteRoomMaterials(SkinCatalog catalog, params Material[] extra)
        {
            IEnumerable<Material> materials = catalog.Skins
                .Where(s => s.Type == BilliardGame.Meta.ItemType.RoomWall || s.Type == BilliardGame.Meta.ItemType.RoomFloor)
                .Select(s => s.Cloth)
                .Concat(extra)
                .Where(m => m != null && m.HasProperty("_EnvironmentReflections"));
            foreach (Material material in materials.Distinct())
            {
                material.SetFloat("_EnvironmentReflections", 0f);
                material.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
                EditorUtility.SetDirty(material);
            }
        }

        private static void Spot(string name, Transform root, Vector3 position, Vector3 target, Color color, float intensity, float angle, bool shadows)
        {
            var light = new GameObject(name).AddComponent<Light>();
            light.transform.SetParent(root, false);
            light.transform.localPosition = position;
            light.transform.LookAt(root.TransformPoint(target));
            light.type = LightType.Spot;
            light.color = color;
            light.intensity = intensity;
            light.range = 7f;
            light.spotAngle = angle;
            light.innerSpotAngle = angle * 0.6f;
            light.shadows = shadows ? LightShadows.Soft : LightShadows.None;
        }

        private static GameObject Box(string name, Transform parent, Vector3 position, Vector3 scale, Material material) =>
            Local(PrimitiveType.Cube, name, parent, position, scale, material);

        private static GameObject Local(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject go = BilliardSceneBuilder.Primitive(type, name, parent, Vector3.zero, Quaternion.identity, scale, material);
            go.transform.SetLocalPositionAndRotation(position, Quaternion.identity);
            return go;
        }
    }

    /// <summary>Sticker decals (docs/ECONOMY.md stickers): cut-out unlit quads whose texture SkinTarget sets per sticker.</summary>
    internal static class StickerDecals
    {
        private const string MaterialPath = "Assets/_Project/Art/Generated/Room/sticker_decal.mat";

        public static Material Material()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                GeneratedArt.Folder("Room");
                material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(material, MaterialPath);
            }

            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_AlphaClip", 1f);
            material.SetFloat("_Cutoff", 0.4f);
            material.EnableKeyword("_ALPHATEST_ON");
            material.renderQueue = (int)RenderQueue.AlphaTest;
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>Decal on the cue butt (CueModel: pivot at the tip, butt along +Z, butt radius ≈ 0.0145 m).</summary>
        public static void AddCueDecal(SkinTarget target)
        {
            Transform holder = target.CueHolder;
            GameObject quad = Quad("Cue Sticker", holder);
            quad.transform.SetLocalPositionAndRotation(new Vector3(0f, 0.0152f, 1.28f), Quaternion.Euler(90f, 0f, 0f));
            quad.transform.localScale = new Vector3(0.022f, 0.05f, 1f);
            SceneWiring.Set(target, "cueSticker", quad.GetComponent<Renderer>());
        }

        /// <summary>One decal on top of each long rail of the built-in match table.</summary>
        public static void AddRailDecals(SkinTarget target, Transform tableRoot, IEnumerable<Renderer> longRails)
        {
            var decals = new List<Renderer>();
            foreach (Renderer rail in longRails)
            {
                Bounds bounds = rail.bounds;
                GameObject quad = Quad("Rail Sticker", tableRoot);
                // A quarter along the rail, clear of the side pocket in the middle.
                quad.transform.SetPositionAndRotation(new Vector3(bounds.center.x - bounds.size.x * 0.25f, bounds.max.y + 0.001f, bounds.center.z), Quaternion.Euler(90f, 0f, 0f));
                quad.transform.localScale = Vector3.one * 0.09f;
                decals.Add(quad.GetComponent<Renderer>());
            }

            SceneWiring.SetArray(target, "tableStickers", decals.ToArray());
        }

        private static GameObject Quad(string name, Transform parent)
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = name;
            Object.DestroyImmediate(quad.GetComponent<Collider>());
            quad.transform.SetParent(parent, false);
            var renderer = quad.GetComponent<Renderer>();
            renderer.sharedMaterial = Material();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            quad.SetActive(false);
            return quad;
        }
    }
}
