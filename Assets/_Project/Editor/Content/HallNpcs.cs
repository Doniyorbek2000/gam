using BilliardGame.Content;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BilliardGame.Editor.Content
{
    /// <summary>
    /// Where the hall's background people go (ROADMAP M1.6 item 5): <see cref="CharacterSpot"/> markers (Npc stands,
    /// NpcSeat sits; Index = fill order, so Medium quality keeps the first four) plus the Silk Road tea counter the
    /// bartender stands behind. HallLife fills them at match start.
    /// </summary>
    internal static class HallNpcs
    {
        public const string RootName = "NPC Spots";

        /// <summary>Silk Road: bartender, sofa, armchairs, stool and three standers round the walls.</summary>
        public static void BuildSilkRoad(Transform hall, float floorY)
        {
            Transform root = Root(hall);
            TeaCounter(root, floorY);
            Spot(root, 0, CharacterSpotKind.Npc, new Vector3(-4.1f, floorY, -3.9f), 0f);
            Spot(root, 1, CharacterSpotKind.NpcSeat, new Vector3(-0.45f, floorY, 3.95f), 180f);
            Spot(root, 2, CharacterSpotKind.Npc, new Vector3(4.6f, floorY, -0.9f), FaceTable(4.6f, -0.9f));
            Spot(root, 3, CharacterSpotKind.NpcSeat, new Vector3(-1.9f, floorY, 3.9f), 180f);
            Spot(root, 4, CharacterSpotKind.Npc, new Vector3(-1.2f, floorY, -3.3f), FaceTable(-1.2f, -3.3f));
            Spot(root, 5, CharacterSpotKind.NpcSeat, new Vector3(3.05f, floorY, 0.3f), -90f);
            Spot(root, 6, CharacterSpotKind.Npc, new Vector3(2.2f, floorY, -3.1f), FaceTable(2.2f, -3.1f));
            Spot(root, 7, CharacterSpotKind.NpcSeat, new Vector3(1.9f, floorY, 3.9f), 180f);
        }

        /// <summary>Garage (no venue loaded): two onlookers.</summary>
        public static void BuildGarage(Transform garage, float floorY)
        {
            Transform root = Root(garage);
            Spot(root, 0, CharacterSpotKind.Npc, new Vector3(-3.5f, floorY, -2f), FaceTable(-3.5f, -2f));
            Spot(root, 1, CharacterSpotKind.Npc, new Vector3(3.8f, floorY, -1.2f), FaceTable(3.8f, -1.2f));
        }

        /// <summary>Adds the NPC spots and tea counter to the baked Silk Road scene without re-baking its lightmaps.</summary>
        [MenuItem("BilliardGame/Venue/Add Hall NPC Spots + NavMesh")]
        public static void PatchSilkRoad()
        {
            Scene scene = EditorSceneManager.OpenScene(VenueBuilder.ScenePath, OpenSceneMode.Single);
            GameObject hall = scene.GetRootGameObjects()[0];
            BuildSilkRoad(hall.transform, VenueBuilder.Floor);
            PlayerSeats.BakeNavMesh(hall, VenueBuilder.Floor, VenueBuilder.NavMeshPath);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Hall] Silk Road: NPC spots, tea counter and NavMesh updated");
        }

        private static Transform Root(Transform parent)
        {
            Transform old = parent.Find(RootName);
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var root = new GameObject(RootName).transform;
            root.SetParent(parent, false);
            return root;
        }

        private static float FaceTable(float x, float z) => Mathf.Atan2(-x, -z) * Mathf.Rad2Deg;

        private static void Spot(Transform root, int index, CharacterSpotKind kind, Vector3 position, float yaw)
        {
            var spot = new GameObject($"{kind} {index}");
            spot.transform.SetParent(root, false);
            spot.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            spot.AddComponent<CharacterSpot>().Configure(kind, index, null);
        }

        // Low wooden counter with a brass samovar and tea glasses, in the corner by the stool.
        private static void TeaCounter(Transform root, float floorY)
        {
            string folder = GeneratedArt.Folder("Characters");
            Material wood = GeneratedArt.Lit($"{folder}/seat_wood.mat", new Color(0.36f, 0.22f, 0.13f), 0.45f, 0f);
            Material top = GeneratedArt.Lit($"{folder}/counter_top.mat", new Color(0.2f, 0.12f, 0.07f), 0.6f, 0f);
            Material brass = GeneratedArt.Lit($"{folder}/samovar_brass.mat", new Color(0.85f, 0.62f, 0.3f), 0.75f, 1f);
            Material glass = GeneratedArt.Lit($"{folder}/tea_glass.mat", new Color(0.85f, 0.88f, 0.9f), 0.92f, 0f);
            const float height = 1.0f;
            var counter = new GameObject("Tea Counter").transform;
            counter.SetParent(root, false);
            Part(PrimitiveType.Cube, "Body", counter, new Vector3(-4.1f, floorY + height * 0.5f - 0.02f, -3.3f), new Vector3(1.8f, height - 0.04f, 0.5f), wood);
            Part(PrimitiveType.Cube, "Top", counter, new Vector3(-4.1f, floorY + height, -3.3f), new Vector3(1.9f, 0.04f, 0.6f), top);
            Part(PrimitiveType.Cylinder, "Samovar", counter, new Vector3(-4.6f, floorY + height + 0.2f, -3.35f), new Vector3(0.24f, 0.18f, 0.24f), brass);
            Part(PrimitiveType.Sphere, "Samovar Lid", counter, new Vector3(-4.6f, floorY + height + 0.42f, -3.35f), new Vector3(0.16f, 0.1f, 0.16f), brass);
            for (int i = 0; i < 3; i++)
            {
                Part(PrimitiveType.Cylinder, "Glass", counter, new Vector3(-4.05f + i * 0.14f, floorY + height + 0.07f, -3.2f), new Vector3(0.06f, 0.05f, 0.06f), glass);
            }
        }

        private static void Part(PrimitiveType type, string name, Transform parent, Vector3 world, Vector3 scale, Material material)
        {
            GameObject go = BilliardSceneBuilder.Primitive(type, name, parent, Vector3.zero, Quaternion.identity, scale, material);
            go.transform.position = world;
        }
    }
}
