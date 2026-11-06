using BilliardGame.Content;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace BilliardGame.Editor.Content
{
    /// <summary>
    /// Hall furniture the M1.6 characters use (docs/GDD.md §8.1): a seat per player with a side table and a tea glass,
    /// marked by <see cref="CharacterSpot"/>, plus the NavMesh they walk on (the table is carved out).
    /// </summary>
    internal static class PlayerSeats
    {
        public const string RootName = "Player Seats";
        private const float AgentRadius = 0.28f;
        private const float AgentHeight = 1.75f;
        /// <summary>Table body half size (rails included), world X/Z; MatchCharacterDirector uses the same.</summary>
        public static readonly Vector2 TableHalf = new Vector2(1.47f, 0.83f);

        /// <summary>Seats at <paramref name="seats"/> (floor position, yaw facing out of the seat); chairs only where the hall has none.</summary>
        public static Transform Build(Transform parent, float floorY, (Vector3 position, float yaw)[] seats, bool buildChairs)
        {
            string folder = GeneratedArt.Folder("Characters");
            Material wood = GeneratedArt.Lit($"{folder}/seat_wood.mat", new Color(0.36f, 0.22f, 0.13f), 0.45f, 0f);
            Material cushion = GeneratedArt.Lit($"{folder}/seat_cushion.mat", new Color(0.42f, 0.08f, 0.1f), 0.2f, 0f);
            Material glass = GeneratedArt.Lit($"{folder}/tea_glass.mat", new Color(0.85f, 0.88f, 0.9f), 0.92f, 0f);
            Material tea = GeneratedArt.Lit($"{folder}/tea.mat", new Color(0.55f, 0.22f, 0.05f), 0.8f, 0f);

            var root = new GameObject(RootName).transform;
            root.SetParent(parent, false);
            for (int i = 0; i < seats.Length; i++)
            {
                (Vector3 position, float yaw) = seats[i];
                position.y = floorY;
                Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
                var seat = new GameObject($"Seat {i}").transform;
                seat.SetParent(root, false);
                seat.SetPositionAndRotation(position, rotation);
                if (buildChairs) Chair(seat, wood, cushion);

                // Side table on the seat's right, glass on it within arm's reach.
                Vector3 right = rotation * Vector3.right;
                Vector3 tablePos = position + right * 0.55f + rotation * Vector3.forward * 0.1f;
                Box(PrimitiveType.Cylinder, "Side Table", root, tablePos + Vector3.up * 0.29f, new Vector3(0.36f, 0.29f, 0.36f), wood);
                var glassRoot = new GameObject($"Tea Glass {i}").transform;
                glassRoot.SetParent(root, false);
                glassRoot.position = tablePos + Vector3.up * 0.58f - right * 0.08f;
                Box(PrimitiveType.Cylinder, "Glass", glassRoot, glassRoot.position + Vector3.up * 0.05f, new Vector3(0.06f, 0.05f, 0.06f), glass);
                Box(PrimitiveType.Cylinder, "Tea", glassRoot, glassRoot.position + Vector3.up * 0.045f, new Vector3(0.062f, 0.04f, 0.062f), tea);
                seat.gameObject.AddComponent<CharacterSpot>().Configure(CharacterSpotKind.Seat, i, glassRoot);
            }

            return root;
        }

        /// <summary>Bakes a NavMesh over <paramref name="hall"/>'s render meshes with the table footprint carved out.</summary>
        public static void BakeNavMesh(GameObject hall, float floorY, string assetPath)
        {
            ConfigureAgent();
            Transform old = hall.transform.Find("Table Footprint");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var footprint = new GameObject("Table Footprint");
            footprint.transform.SetParent(hall.transform, false);
            footprint.transform.position = new Vector3(0f, floorY + 0.5f, 0f);
            var volume = footprint.AddComponent<NavMeshModifierVolume>();
            volume.size = new Vector3(TableHalf.x * 2f, 1.2f, TableHalf.y * 2f);
            volume.center = Vector3.zero;
            volume.area = NavMesh.GetAreaFromName("Not Walkable");

            NavMeshSurface surface = hall.GetComponent<NavMeshSurface>() ?? hall.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
            surface.agentTypeID = 0;
            surface.BuildNavMesh();
            AssetDatabase.DeleteAsset(assetPath);
            AssetDatabase.CreateAsset(surface.navMeshData, assetPath);
            EditorUtility.SetDirty(surface);
        }

        /// <summary>Adds seats and the NavMesh to the already-baked Silk Road scene without re-baking its lightmaps.</summary>
        [MenuItem("BilliardGame/Venue/Add Player Seats + NavMesh")]
        public static void PatchSilkRoad()
        {
            Scene scene = EditorSceneManager.OpenScene(VenueBuilder.ScenePath, OpenSceneMode.Single);
            GameObject hall = scene.GetRootGameObjects()[0];
            Transform old = hall.transform.Find(RootName);
            if (old != null) Object.DestroyImmediate(old.gameObject);
            VenueBuilder.BuildSeats(hall.transform);
            BakeNavMesh(hall, VenueBuilder.Floor, VenueBuilder.NavMeshPath);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Characters] Silk Road: player seats and NavMesh added");
        }

        private static void ConfigureAgent()
        {
            // Default agent type = the player characters (thin so they fit between chairs and the table).
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/NavMeshAreas.asset")[0]);
            SerializedProperty agent = settings.FindProperty("m_Settings").GetArrayElementAtIndex(0);
            agent.FindPropertyRelative("agentRadius").floatValue = AgentRadius;
            agent.FindPropertyRelative("agentHeight").floatValue = AgentHeight;
            agent.FindPropertyRelative("agentClimb").floatValue = 0.2f;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Chair(Transform seat, Material wood, Material cushion)
        {
            Box(PrimitiveType.Cube, "Chair Seat", seat, seat.TransformPoint(new Vector3(0f, 0.42f, -0.05f)), new Vector3(0.48f, 0.06f, 0.46f), cushion);
            Box(PrimitiveType.Cube, "Chair Back", seat, seat.TransformPoint(new Vector3(0f, 0.72f, -0.27f)), new Vector3(0.48f, 0.56f, 0.05f), wood);
            foreach (float x in new[] { -0.21f, 0.21f })
            {
                foreach (float z in new[] { -0.25f, 0.15f })
                {
                    Box(PrimitiveType.Cube, "Chair Leg", seat, seat.TransformPoint(new Vector3(x, 0.2f, z)), new Vector3(0.04f, 0.4f, 0.04f), wood);
                }
            }
        }

        private static void Box(PrimitiveType type, string name, Transform parent, Vector3 world, Vector3 scale, Material material)
        {
            GameObject go = BilliardSceneBuilder.Primitive(type, name, parent, Vector3.zero, Quaternion.identity, scale, material);
            go.transform.position = world;
            go.transform.rotation = parent.rotation;
        }
    }
}
