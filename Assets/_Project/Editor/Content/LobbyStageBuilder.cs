using System.Linq;
using BilliardGame.Content;
using BilliardGame.UI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BilliardGame.Editor.Content
{
    /// <summary>
    /// The lobby showcase, built into the Main scene 40 m from the table so the two cameras never see each other:
    /// turntable with the CC0 mannequin and the equipped cue, item preview slots, the reveal crate, lights and camera.
    /// </summary>
    internal static class LobbyStageBuilder
    {
        public static readonly Vector3 Origin = new Vector3(40f, 0f, 0f);
        private const string AvatarPrefab = "Assets/_Project/Art/Avatars/avatar-ual/avatar-ual.prefab";
        private const string AvatarModel = "Assets/_Project/Art/Avatars/avatar-ual/Source/AnimationLibrary_Unity_Standard.fbx";
        private const float CharacterHeight = 1.8f;

        public static LobbyStage Build(out SkinTarget equippedCue)
        {
            string folder = GeneratedArt.Folder("Lobby");
            var root = new GameObject("Lobby Stage");
            root.transform.position = Origin;
            var stage = root.AddComponent<LobbyStage>();

            Material floor = GeneratedArt.Lit($"{folder}/stage_floor.mat", new Color(0.09f, 0.1f, 0.14f), 0.85f, 0.2f);
            Material backdrop = GeneratedArt.Lit($"{folder}/stage_backdrop.mat", new Color(0.05f, 0.06f, 0.09f), 0.3f, 0f);
            Material gold = GeneratedArt.Lit($"{folder}/stage_gold.mat", new Color(1f, 0.76f, 0.3f), 0.8f, 1f);
            Material wood = AssetDatabase.LoadAssetAtPath<Material>($"{GeneratedArt.Root}/Venues/silk-road-lounge/wood.mat") ?? backdrop;
            Material niche = AssetDatabase.LoadAssetAtPath<Material>($"{GeneratedArt.Root}/Venues/silk-road-lounge/niche.mat") ?? backdrop;

            Local(PrimitiveType.Cylinder, "Platform", root.transform, new Vector3(0f, -0.03f, 0f), new Vector3(2.4f, 0.03f, 2.4f), floor);
            Local(PrimitiveType.Cylinder, "Platform Rim", root.transform, new Vector3(0f, -0.035f, 0f), new Vector3(2.5f, 0.025f, 2.5f), gold);
            Local(PrimitiveType.Cube, "Backdrop", root.transform, new Vector3(0f, 2f, 2.6f), new Vector3(9f, 5f, 0.1f), backdrop);
            Local(PrimitiveType.Cube, "Ground", root.transform, new Vector3(0f, -0.06f, 0f), new Vector3(12f, 0.02f, 10f), backdrop);
            BuildArches(root.transform, wood, niche);

            Transform turntable = new GameObject("Turntable").transform;
            turntable.SetParent(root.transform, false);

            var showcase = new GameObject("Showcase");
            showcase.transform.SetParent(turntable, false);
            GameObject mannequin = BuildCharacter(showcase.transform, folder);
            equippedCue = CueSlot("Equipped Cue", showcase.transform, new Vector3(0.48f, 1.48f, 0.05f), Quaternion.LookRotation(Vector3.down, Vector3.forward));
            StickerDecals.AddCueDecal(equippedCue);

            // Inventory preview: a cue lying across the turntable, or the mini table.
            var previewCue = new GameObject("Preview Cue");
            previewCue.transform.SetParent(turntable, false);
            SkinTarget previewCueSlot = CueSlot("Cue Slot", previewCue.transform, new Vector3(-0.71f, 0.85f, 0f), Quaternion.Euler(0f, 0f, 12f) * Quaternion.LookRotation(Vector3.right));
            SkinDefinition defaultTable = ContentBuilder.LoadCatalog().Skins.First(s => s.Type == BilliardGame.Meta.ItemType.Table);
            GameObject mini = ContentBuilder.MiniTable(defaultTable.Cloth, defaultTable.Rail, out Renderer[] cloth, out Renderer[] rails);
            mini.name = "Preview Table";
            mini.transform.SetParent(turntable, false);
            var preview = turntable.gameObject.AddComponent<SkinTarget>();
            SceneWiring.Set(preview, "cueHolder", previewCueSlot.CueHolder);
            SceneWiring.SetArray(preview, "clothRenderers", cloth);
            SceneWiring.SetArray(preview, "railRenderers", rails);
            WireTableModel(preview, mini.transform);
            Object.DestroyImmediate(previewCueSlot);

            Crate(root.transform, folder, wood, gold, out Transform crate, out Transform lid, out Light crateLight, out Renderer seam);
            Camera camera = BuildLights(root.transform);

            SceneWiring.Set(stage, "lobbyCamera", camera);
            SceneWiring.Set(stage, "turntable", turntable);
            SceneWiring.Set(stage, "showcase", showcase);
            SceneWiring.Set(stage, "defaultAvatar", mannequin);
            SceneWiring.Set(stage, "previewCue", previewCue);
            SceneWiring.Set(stage, "previewTable", mini);
            SceneWiring.Set(stage, "previewTarget", preview);
            SceneWiring.Set(stage, "crate", crate);
            SceneWiring.Set(stage, "crateLid", lid);
            SceneWiring.Set(stage, "crateLight", crateLight);
            SceneWiring.Set(stage, "crateSeam", seam);
            return stage;
        }

        /// <summary>Imported table skins replace the mini table's boxes with the model at mini scale.</summary>
        internal static void WireTableModel(SkinTarget target, Transform mini)
        {
            GameObject[] builtIn = Enumerable.Range(0, mini.childCount).Select(i => mini.GetChild(i).gameObject).ToArray();
            var holder = new GameObject("Table Model").transform;
            holder.SetParent(mini, false);
            holder.localPosition = new Vector3(0f, ContentBuilder.ClothHeight * ContentBuilder.MiniScale, 0f);
            holder.localScale = Vector3.one * ContentBuilder.MiniScale;
            SceneWiring.Set(target, "tableHolder", holder);
            SceneWiring.SetArray(target, "builtInTable", builtIn);
        }

        private static GameObject BuildCharacter(Transform parent, string folder)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AvatarPrefab);
            if (prefab == null)
            {
                // Placeholder until avatar-ual (or avatar-base) is imported.
                Material skin = GeneratedArt.Lit($"{folder}/placeholder_avatar.mat", new Color(0.6f, 0.62f, 0.68f), 0.4f, 0f);
                return Local(PrimitiveType.Capsule, "Placeholder Avatar", parent, new Vector3(0f, 0.9f, 0f), new Vector3(0.5f, 0.9f, 0.35f), skin);
            }

            var character = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            character.name = "Avatar";
            character.transform.localPosition = Vector3.zero;
            character.transform.localRotation = Quaternion.Euler(0f, 180f, 0f) * character.transform.localRotation;
            Bounds bounds = character.GetComponentsInChildren<Renderer>().Select(r => r.bounds).Aggregate((a, b) => { a.Encapsulate(b); return a; });
            character.transform.localScale *= CharacterHeight / bounds.size.y;

            var animator = character.GetComponent<Animator>() ?? character.AddComponent<Animator>();
            animator.runtimeAnimatorController = Controller(folder);
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            return character;
        }

        private static AnimatorController Controller(string folder)
        {
            string path = $"{folder}/LobbyAvatar.controller";
            AssetDatabase.DeleteAsset(path);
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(AvatarModel).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__")).ToArray();
            AnimationClip idle = clips.FirstOrDefault(c => c.name.EndsWith("Idle_Loop") && !c.name.Contains("Crouch") && !c.name.Contains("Pistol") && !c.name.Contains("Spell") && !c.name.Contains("Swim") && !c.name.Contains("Sitting"));
            AnimationClip emote = clips.FirstOrDefault(c => c.name.EndsWith("Idle_Talking_Loop"));
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorState idleState = machine.AddState("Idle");
            idleState.motion = idle;
            machine.defaultState = idleState;
            if (emote != null)
            {
                // Lobby emote every ~10 s (docs/UI_UX.md §2): idle loops a few times, then one talking cycle.
                AnimatorState emoteState = machine.AddState("Emote");
                emoteState.motion = emote;
                AnimatorStateTransition toEmote = idleState.AddTransition(emoteState);
                toEmote.hasExitTime = true;
                toEmote.exitTime = 4f;
                toEmote.duration = 0.4f;
                AnimatorStateTransition back = emoteState.AddTransition(idleState);
                back.hasExitTime = true;
                back.exitTime = 0.95f;
                back.duration = 0.4f;
            }

            return controller;
        }

        private static SkinTarget CueSlot(string name, Transform parent, Vector3 position, Quaternion rotation)
        {
            var slot = new GameObject(name);
            slot.transform.SetParent(parent, false);
            slot.transform.SetLocalPositionAndRotation(position, rotation);
            var target = slot.AddComponent<SkinTarget>();
            SceneWiring.Set(target, "cueHolder", slot.transform);
            return target;
        }

        private static void Crate(Transform root, string folder, Material wood, Material gold, out Transform crate, out Transform lid, out Light light, out Renderer seam)
        {
            Material glow = GeneratedArt.Lit($"{folder}/crate_seam.mat", Color.black, 0.5f, 0f);
            GeneratedArt.Emission(glow, null, Color.black);
            glow.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

            // Floats above the turntable so the whole crate stays in the lobby camera frame.
            var anchor = new GameObject("Crate Anchor").transform;
            anchor.SetParent(root, false);
            anchor.localPosition = new Vector3(0f, 0.35f, 0.2f);
            crate = new GameObject("Crate").transform;
            crate.SetParent(anchor, false);
            Local(PrimitiveType.Cube, "Body", crate, new Vector3(0f, 0.22f, 0f), new Vector3(0.8f, 0.44f, 0.55f), wood);
            Local(PrimitiveType.Cube, "Band L", crate, new Vector3(-0.3f, 0.22f, 0f), new Vector3(0.05f, 0.45f, 0.56f), gold);
            Local(PrimitiveType.Cube, "Band R", crate, new Vector3(0.3f, 0.22f, 0f), new Vector3(0.05f, 0.45f, 0.56f), gold);
            seam = Local(PrimitiveType.Cube, "Seam", crate, new Vector3(0f, 0.445f, 0f), new Vector3(0.78f, 0.012f, 0.53f), glow).GetComponent<Renderer>();

            lid = new GameObject("Lid").transform;
            lid.SetParent(crate, false);
            lid.localPosition = new Vector3(0f, 0.44f, 0.275f);                 // hinge on the back top edge
            Local(PrimitiveType.Cube, "Lid Board", lid, new Vector3(0f, 0.05f, -0.275f), new Vector3(0.82f, 0.1f, 0.57f), wood);
            Local(PrimitiveType.Cube, "Lid Band", lid, new Vector3(0f, 0.05f, -0.275f), new Vector3(0.06f, 0.11f, 0.58f), gold);

            light = new GameObject("Crate Light").AddComponent<Light>();
            light.transform.SetParent(crate, false);
            light.transform.localPosition = new Vector3(0f, 0.75f, -0.1f);
            light.type = LightType.Point;
            light.range = 3f;
            light.intensity = 0f;
            light.shadows = LightShadows.None;
            crate.gameObject.SetActive(false);
        }

        private static Camera BuildLights(Transform root)
        {
            Spot("Key", root, new Vector3(1.8f, 3.2f, -2.4f), new Color(1f, 0.93f, 0.82f), 13f, 40f, true);
            Spot("Rim", root, new Vector3(-1.6f, 2.8f, 2.0f), new Color(0.45f, 0.6f, 1f), 30f, 45f, false);
            Spot("Fill", root, new Vector3(-2.4f, 1.6f, -2.2f), new Color(1f, 0.8f, 0.6f), 6f, 60f, false);

            var cameraObject = new GameObject("Lobby Camera");
            cameraObject.transform.SetParent(root, false);
            cameraObject.transform.localPosition = new Vector3(0f, 1.3f, -3.6f);
            cameraObject.transform.LookAt(root.position + new Vector3(0f, 0.95f, 0f));
            var camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 34f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 15f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.03f, 0.04f, 0.06f);
            camera.depth = 1;
            camera.allowHDR = true;
            UniversalAdditionalCameraData data = camera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.renderShadows = true;
            return camera;
        }

        private static void Spot(string name, Transform root, Vector3 position, Color color, float intensity, float angle, bool shadows)
        {
            var light = new GameObject(name + " Light").AddComponent<Light>();
            light.transform.SetParent(root, false);
            light.transform.localPosition = position;
            light.transform.LookAt(root.position + Vector3.up);
            light.type = LightType.Spot;
            light.color = color;
            light.intensity = intensity;
            light.range = 8f;
            light.spotAngle = angle;
            light.innerSpotAngle = angle * 0.6f;
            light.shadows = shadows ? LightShadows.Soft : LightShadows.None;
        }

        private static void BuildArches(Transform root, Material frame, Material niche)
        {
            var inner = AssetDatabase.LoadAssetAtPath<Mesh>($"{GeneratedArt.Root}/Venues/silk-road-lounge/arch_inner.asset");
            var outer = AssetDatabase.LoadAssetAtPath<Mesh>($"{GeneratedArt.Root}/Venues/silk-road-lounge/arch_frame.asset");
            if (inner == null || outer == null) return;
            foreach (float x in new[] { -2.4f, 0f, 2.4f })
            {
                var arch = new GameObject("Arch").transform;
                arch.SetParent(root, false);
                arch.localPosition = new Vector3(x, 0.1f, 2.54f);
                arch.localScale = Vector3.one * (x == 0f ? 1.25f : 1f);
                MeshChild("Frame", arch, outer, frame, new Vector3(0f, -0.1f, -0.005f));
                MeshChild("Niche", arch, inner, niche, new Vector3(0f, 0f, -0.012f));
            }
        }

        private static void MeshChild(string name, Transform parent, Mesh mesh, Material material, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        private static GameObject Local(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject go = BilliardSceneBuilder.Primitive(type, name, parent, Vector3.zero, Quaternion.identity, scale, material);
            go.transform.SetLocalPositionAndRotation(position, Quaternion.identity);
            return go;
        }
    }
}
