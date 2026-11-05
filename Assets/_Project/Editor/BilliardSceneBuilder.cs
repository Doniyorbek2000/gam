using System.Linq;
using BilliardGame.Content;
using BilliardGame.Editor.Content;
using BilliardGame.Editor.UI;
using BilliardGame.Gameplay;
using BilliardGame.Online;
using BilliardGame.Physics;
using BilliardGame.Presentation;
using BilliardGame.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace BilliardGame.Editor
{
    /// <summary>Builds Assets/_Project/Scenes/Main.unity from code so the scene is reproducible and reviewable.</summary>
    internal static class BilliardSceneBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/Main.unity";

        internal const float CushionDepth = 0.05f;
        internal const float CushionHeight = 0.04f;
        internal const float RailWidth = 0.12f;
        internal const float RailHeight = 0.05f;

        /// <summary>Corner castings sit on the rails; pocket openings are drawn just above them.</summary>
        internal const float PocketTop = RailHeight + 0.007f;
        private const float LineWidth = 0.004f;

        [MenuItem("BilliardGame/Build Main Scene")]
        public static void Build()
        {
            TableGeometry table = TableGeometry.CreateStandard();
            PhysicsConfig config = PhysicsConfig.Default;
            float r = (float)config.BallRadius;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Camera camera = BuildCamera();
            Transform tableRoot = BuildTable(table);
            var garage = new GameObject("Venue Garage");
            TableDressingBuilder.Build(table, tableRoot, garage.transform);
            TableDressingBuilder.BuildLightingAndPostFx(camera);
            BallView[] balls = BuildBalls(table, config);

            var game = new GameObject("Game");
            var playback = game.AddComponent<TablePlayback>();
            playback.Bind(balls);
            var cue = game.AddComponent<CueController>();
            BuildAimVisuals(cue, camera, balls[EightBallSet.CueBall], r);
            var audio = game.AddComponent<TableAudio>();
            SceneWiring.Set(audio, "playback", playback);
            SceneWiring.Set(audio, "ballHit", ProceduralAudio.BallHit());
            SceneWiring.Set(audio, "cushionHit", ProceduralAudio.CushionHit());
            SceneWiring.Set(audio, "pocketDrop", ProceduralAudio.PocketDrop());
            SceneWiring.Set(audio, "cueStrike", ProceduralAudio.CueStrike());
            var controller = game.AddComponent<GameController>();
            SceneWiring.Set(controller, "playback", playback);
            SceneWiring.Set(controller, "cue", cue);
            SceneWiring.Set(controller, "tableAudio", audio);

            LobbyStage stage = LobbyStageBuilder.Build(out SkinTarget lobbyCue);
            RoomStage room = RoomStageBuilder.Build();
            GameServices services = BuildServices(TableTarget(tableRoot, cue), lobbyCue);
            HudController hud = HudBuilder.Build(controller, cue, services, out GameObject hudRoot);
            var venues = game.AddComponent<VenueLoader>();
            SceneWiring.Set(venues, "garageRoot", garage);
            UseHdriReflections();

            var online = new GameObject("Online");
            var client = online.AddComponent<OnlineClient>();
            var match = online.AddComponent<OnlineMatchController>();
            SceneWiring.Set(match, "client", client);
            SceneWiring.Set(match, "game", controller);
            LobbyUIBuilder.Build(controller, match, hud, hudRoot, services, venues, stage, room, camera);

            BallMaterialFactory.EnsureFolder("Assets/_Project/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true), new EditorBuildSettingsScene(VenueBuilder.ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log($"[BilliardGame] Built {ScenePath}");
        }

        private const string CatalogPath = "Assets/_Project/Content/SkinCatalog.asset";
        private const string CrateTablesPath = "Assets/_Project/Resources/Economy/crates.json";

        /// <summary>Equipped skins show on the match table: cloth + cushions, rails + body, and the cue holder.</summary>
        private static SkinTarget TableTarget(Transform tableRoot, CueController cue)
        {
            Renderer[] parts = tableRoot.GetComponentsInChildren<Renderer>();
            var target = tableRoot.gameObject.AddComponent<SkinTarget>();
            SceneWiring.Set(target, "cueHolder", new SerializedObject(cue).FindProperty("cueStick").objectReferenceValue);
            SceneWiring.SetArray(target, "clothRenderers", parts.Where(r => r.name == "Cloth" || r.name.StartsWith("Cushion")).ToArray());
            SceneWiring.SetArray(target, "railRenderers", parts.Where(r => r.name.StartsWith("Rail") || r.name == "Body").ToArray());
            // Imported table skins (cloth at y = 0, fitted to the physics play area) replace every built-in table mesh.
            SceneWiring.SetArray(target, "builtInTable", parts.Select(r => r.gameObject).Distinct().ToArray());
            var holder = new GameObject("Table Model").transform;
            holder.SetParent(tableRoot, false);
            SceneWiring.Set(target, "tableHolder", holder);
            StickerDecals.AddCueDecal(target);
            // Long rails: the two "Rail" boxes stretched along x.
            StickerDecals.AddRailDecals(target, tableRoot, parts.Where(r => r.name.StartsWith("Rail") && r.bounds.size.x > r.bounds.size.z).Take(2));
            return target;
        }

        private static GameServices BuildServices(params SkinTarget[] targets)
        {
            var services = new GameObject("Game Services").AddComponent<GameServices>();
            SceneWiring.Set(services, "catalog", AssetDatabase.LoadAssetAtPath<SkinCatalog>(CatalogPath));
            SceneWiring.Set(services, "crateTables", AssetDatabase.LoadAssetAtPath<TextAsset>(CrateTablesPath));
            SceneWiring.SetArray(services, "equipTargets", targets);
            return services;
        }

        private static void UseHdriReflections()
        {
            var hdri = AssetDatabase.LoadAssetAtPath<Cubemap>(IconStudio.HdriPath);
            if (hdri == null) return;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = hdri;
        }

        private static Camera BuildCamera()
        {
            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.07f, 0.08f, 0.1f);
            camera.fieldOfView = 38f;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 20f;
            cameraObject.transform.SetPositionAndRotation(new Vector3(0f, 3.1f, -1.25f), Quaternion.Euler(68f, 0f, 0f));
            return camera;
        }

        private static Transform BuildTable(TableGeometry table)
        {
            Texture2D felt = ProceduralTextures.Felt();
            Material cloth = BallMaterialFactory.Textured("Cloth", felt, new Color(0.06f, 0.45f, 0.24f), new Vector2(10f, 5f), 0.08f);
            Material cushion = BallMaterialFactory.Textured("Cushion", felt, new Color(0.05f, 0.38f, 0.2f), new Vector2(20f, 1f), 0.1f);
            Material wood = BallMaterialFactory.Textured("Wood", ProceduralTextures.Wood(), Color.white, new Vector2(2f, 1f), 0.7f);
            Material pocket = BallMaterialFactory.Lit("Pocket", new Color(0.01f, 0.01f, 0.01f), 0f);
            Material marking = BallMaterialFactory.Unlit("Marking", new Color(0.8f, 0.85f, 0.8f));

            var root = new GameObject("Table").transform;
            float length = (float)table.Length;
            float width = (float)table.Width;
            float outer = CushionDepth + RailWidth;

            Box("Cloth", root, new Vector3(0f, -0.02f, 0f), Quaternion.identity, new Vector3(length + 2f * CushionDepth, 0.04f, width + 2f * CushionDepth), cloth);

            for (int i = 0; i < table.Cushions.Count; i++)
            {
                CushionSegment segment = table.Cushions[i];
                Vector3 start = TableSpace.ToWorld(segment.Start, 0f);
                Vector3 end = TableSpace.ToWorld(segment.End, 0f);
                Vector3 normal = TableSpace.ToWorld(segment.Normal, 0f);
                Vector3 centre = (start + end) * 0.5f - normal * (CushionDepth * 0.5f) + Vector3.up * (CushionHeight * 0.5f);
                Box($"Cushion {i}", root, centre, Quaternion.LookRotation(end - start), new Vector3(CushionDepth, CushionHeight, (float)segment.Length), cushion);
            }

            float railCentreZ = width * 0.5f + CushionDepth + RailWidth * 0.5f;
            float railCentreX = length * 0.5f + CushionDepth + RailWidth * 0.5f;
            var longRail = new Vector3(length + 2f * outer, RailHeight, RailWidth);
            var shortRail = new Vector3(RailWidth, RailHeight, width + 2f * CushionDepth);
            float railY = RailHeight * 0.5f;
            Box("Rail Top", root, new Vector3(0f, railY, railCentreZ), Quaternion.identity, longRail, wood);
            Box("Rail Bottom", root, new Vector3(0f, railY, -railCentreZ), Quaternion.identity, longRail, wood);
            Box("Rail Right", root, new Vector3(railCentreX, railY, 0f), Quaternion.identity, shortRail, wood);
            Box("Rail Left", root, new Vector3(-railCentreX, railY, 0f), Quaternion.identity, shortRail, wood);

            for (int i = 0; i < table.Pockets.Count; i++)
            {
                Pocket hole = table.Pockets[i];
                float d = (float)hole.Radius * 2.2f;
                Primitive(PrimitiveType.Cylinder, $"Pocket {i}", root, TableSpace.ToWorld(hole.Center, PocketTop), Quaternion.identity, new Vector3(d, 0.001f, d), pocket);
            }

            Vector3 foot = TableSpace.ToWorld(EightBallRack.FootSpot(table), 0.0005f);
            Primitive(PrimitiveType.Cylinder, "Foot Spot", root, foot, Quaternion.identity, new Vector3(0.012f, 0.0005f, 0.012f), marking);
            float headX = (float)EightBallRack.HeadStringX(table);
            Box("Head String", root, new Vector3(headX, 0.0005f, 0f), Quaternion.identity, new Vector3(0.006f, 0.001f, width), marking);
            return root;
        }

        private static BallView[] BuildBalls(TableGeometry table, PhysicsConfig config)
        {
            var root = new GameObject("Balls").transform;
            float r = (float)config.BallRadius;
            BallState[] rack = EightBallRack.Create(table, config);
            var views = new BallView[rack.Length];
            var random = new System.Random(8);
            // Sphere UV u = 0.25 (a number disc) faces local -Z: turn it up, text upright for the camera, then vary it a little.
            Quaternion numberUp = Quaternion.LookRotation(Vector3.down, Vector3.forward);
            for (int id = 0; id < rack.Length; id++)
            {
                float yaw = (float)(random.NextDouble() * 70.0 - 35.0);
                Quaternion rotation = Quaternion.AngleAxis(-20f, Vector3.right) * Quaternion.AngleAxis(yaw, Vector3.up) * numberUp;
                GameObject ball = Primitive(PrimitiveType.Sphere, id == 0 ? "Cue Ball" : $"Ball {id}", root,
                    TableSpace.ToWorld(rack[id].Position, r), rotation, Vector3.one * (2f * r), BallMaterialFactory.Ball(id));
                var view = ball.AddComponent<BallView>();
                view.Configure(id, r);
                views[id] = view;
            }

            return views;
        }

        private static void BuildAimVisuals(CueController cue, Camera camera, BallView cueBall, float r)
        {
            Material line = BallMaterialFactory.Unlit("AimLine", new Color(1f, 1f, 1f));
            Material objectPath = BallMaterialFactory.Unlit("ObjectLine", new Color(1f, 0.85f, 0.3f));
            var root = new GameObject("Aim").transform;

            LineRenderer aim = Line("Aim Line", root, line, 2, true);
            LineRenderer objectLine = Line("Object Line", root, objectPath, 2, true);
            LineRenderer cueLine = Line("Cue Line", root, line, 2, true);

            const int ringPoints = 40;
            LineRenderer ring = Line("Ghost Ball", root, line, ringPoints, false);
            ring.loop = true;
            for (int i = 0; i < ringPoints; i++)
            {
                float a = i * Mathf.PI * 2f / ringPoints;
                ring.SetPosition(i, new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r));
            }

            // Empty holder: the equipped cue skin prefab is placed under it at runtime (SkinTarget).
            var cueStick = new GameObject("Cue Stick");
            cueStick.transform.SetParent(root, false);

            SceneWiring.Set(cue, "viewCamera", camera);
            SceneWiring.Set(cue, "cueBall", cueBall);
            SceneWiring.Set(cue, "aimLine", aim);
            SceneWiring.Set(cue, "objectLine", objectLine);
            SceneWiring.Set(cue, "cueLine", cueLine);
            SceneWiring.Set(cue, "ghostBall", ring.transform);
            SceneWiring.Set(cue, "cueStick", cueStick.transform);
        }

        private static LineRenderer Line(string name, Transform parent, Material material, int points, bool worldSpace)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.positionCount = points;
            line.useWorldSpace = worldSpace;
            line.widthMultiplier = LineWidth;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.alignment = LineAlignment.View;
            return line;
        }

        internal static GameObject Box(string name, Transform parent, Vector3 position, Quaternion rotation, Vector3 scale, Material material) =>
            Primitive(PrimitiveType.Cube, name, parent, position, rotation, scale, material);

        internal static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 position, Quaternion rotation, Vector3 scale, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(position, rotation);
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }
    }
}
