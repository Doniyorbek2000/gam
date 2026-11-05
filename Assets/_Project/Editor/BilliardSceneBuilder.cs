using BilliardGame.Gameplay;
using BilliardGame.Physics;
using BilliardGame.Presentation;
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

        private const float CushionDepth = 0.05f;
        private const float CushionHeight = 0.04f;
        private const float RailWidth = 0.12f;
        private const float RailHeight = 0.05f;
        private const float LineWidth = 0.004f;

        [MenuItem("BilliardGame/Build Main Scene")]
        public static void Build()
        {
            TableGeometry table = TableGeometry.CreateStandard();
            PhysicsConfig config = PhysicsConfig.Default;
            float r = (float)config.BallRadius;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Camera camera = BuildCameraAndLight();
            BuildTable(table);
            BallView[] balls = BuildBalls(table, config);

            var game = new GameObject("Game");
            var playback = game.AddComponent<TablePlayback>();
            playback.Bind(balls);
            var cue = game.AddComponent<CueController>();
            BuildAimVisuals(cue, camera, balls[EightBallSet.CueBall], r);
            var controller = game.AddComponent<GameController>();
            SceneWiring.Set(controller, "playback", playback);
            SceneWiring.Set(controller, "cue", cue);

            HudBuilder.Build(controller, cue);

            BallMaterialFactory.EnsureFolder("Assets/_Project/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log($"[BilliardGame] Built {ScenePath}");
        }

        private static Camera BuildCameraAndLight()
        {
            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.07f, 0.08f, 0.1f);
            camera.fieldOfView = 38f;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 20f;
            cameraObject.transform.SetPositionAndRotation(new Vector3(0f, 3.1f, -1.25f), Quaternion.Euler(68f, 0f, 0f));

            var lightObject = new GameObject("Directional Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.shadows = LightShadows.Soft;
            lightObject.transform.rotation = Quaternion.Euler(70f, 20f, 0f);

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.45f, 0.45f, 0.5f);
            return camera;
        }

        private static void BuildTable(TableGeometry table)
        {
            Material cloth = BallMaterialFactory.Lit("Cloth", new Color(0.05f, 0.42f, 0.22f), 0.05f);
            Material cushion = BallMaterialFactory.Lit("Cushion", new Color(0.04f, 0.35f, 0.18f), 0.08f);
            Material wood = BallMaterialFactory.Lit("Wood", new Color(0.33f, 0.17f, 0.07f), 0.55f);
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
                Primitive(PrimitiveType.Cylinder, $"Pocket {i}", root, TableSpace.ToWorld(hole.Center, RailHeight + 0.001f), Quaternion.identity, new Vector3(d, 0.001f, d), pocket);
            }

            Vector3 foot = TableSpace.ToWorld(EightBallRack.FootSpot(table), 0.0005f);
            Primitive(PrimitiveType.Cylinder, "Foot Spot", root, foot, Quaternion.identity, new Vector3(0.012f, 0.0005f, 0.012f), marking);
            float headX = (float)EightBallRack.HeadStringX(table);
            Box("Head String", root, new Vector3(headX, 0.0005f, 0f), Quaternion.identity, new Vector3(0.006f, 0.001f, width), marking);
        }

        private static BallView[] BuildBalls(TableGeometry table, PhysicsConfig config)
        {
            var root = new GameObject("Balls").transform;
            float r = (float)config.BallRadius;
            BallState[] rack = EightBallRack.Create(table, config);
            var views = new BallView[rack.Length];
            for (int id = 0; id < rack.Length; id++)
            {
                GameObject ball = Primitive(PrimitiveType.Sphere, id == 0 ? "Cue Ball" : $"Ball {id}", root,
                    TableSpace.ToWorld(rack[id].Position, r), Quaternion.Euler(0f, 90f, 0f), Vector3.one * (2f * r), BallMaterialFactory.Ball(id));
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
            Material stick = BallMaterialFactory.Lit("CueStick", new Color(0.85f, 0.68f, 0.45f), 0.6f);
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

            GameObject cueStick = Primitive(PrimitiveType.Cylinder, "Cue Stick", root, Vector3.zero, Quaternion.identity, new Vector3(0.018f, 0.7f, 0.018f), stick);
            cueStick.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.On;

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

        private static GameObject Box(string name, Transform parent, Vector3 position, Quaternion rotation, Vector3 scale, Material material) =>
            Primitive(PrimitiveType.Cube, name, parent, position, rotation, scale, material);

        private static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 position, Quaternion rotation, Vector3 scale, Material material)
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
