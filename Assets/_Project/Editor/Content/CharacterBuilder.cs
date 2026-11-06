using System.Collections.Generic;
using System.Linq;
using BilliardGame.Avatar;
using BilliardGame.Content;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations.Rigging;

namespace BilliardGame.Editor.Content
{
    /// <summary>
    /// Builds the M1.6 player characters (docs/TECH_ARCHITECTURE.md §2.3.1) from the imported Humanoid models:
    /// one shared Animator controller (UAL CC0 clips + procedural address/sad/celebrate poses written as muscle curves),
    /// a Two Bone IK rig per arm, a NavMeshAgent, the chalk prop, a portrait, and a CharacterDefinition per character.
    /// </summary>
    internal static class CharacterBuilder
    {
        private const string Folder = "Characters";
        private const string DefinitionFolder = ContentBuilder.ContentFolder + "/Characters";
        private const string ClipLibrary = "Assets/_Project/Art/Avatars/avatar-ual/Source/AnimationLibrary_Unity_Standard.fbx";

        private struct Spec
        {
            public string Slug, Id, Name;
            public bool Female;
            public float Scale;
        }

        private static readonly Spec[] Specs =
        {
            new Spec { Slug = "char-timur", Id = "timur", Name = "Timur", Female = false, Scale = 0.95f },
            new Spec { Slug = "char-aziz", Id = "aziz", Name = "Aziz", Female = false, Scale = 0.95f },
            new Spec { Slug = "char-malika", Id = "malika", Name = "Malika", Female = true, Scale = 0.92f },
            new Spec { Slug = "char-dilnoza", Id = "dilnoza", Name = "Dilnoza", Female = true, Scale = 0.92f },
        };

        [MenuItem("BilliardGame/Build Characters")]
        public static void Build()
        {
            string folder = GeneratedArt.Folder(Folder);
            BallMaterialFactory.EnsureFolder(DefinitionFolder);
            Dictionary<string, AnimationClip> library = AssetDatabase.LoadAllAssetsAtPath(ClipLibrary).OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__")).ToDictionary(c => c.name.Replace("Rig|", string.Empty));

            var definitions = new List<CharacterDefinition>();
            AnimatorController controller = null;
            foreach (Spec spec in Specs)
            {
                var source = AssetDatabase.LoadAssetAtPath<GameObject>($"{Art.ArtImporter.ArtRoot}/Avatars/{spec.Slug}/{spec.Slug}.prefab");
                if (source == null)
                {
                    Debug.LogWarning($"[Characters] {spec.Slug} is not imported; run Art ▸ Import Incoming first.");
                    continue;
                }

                if (controller == null) controller = Controller(folder, source, library, out walkSpeed);
                GameObject prefab = BuildPrefab(spec, source, controller, folder);
                Sprite portrait = Portrait(source, $"{folder}/{spec.Slug}_portrait.png");
                definitions.Add(Definition(spec, prefab, portrait));
            }

            SkinCatalog catalog = ContentBuilder.LoadCatalog();
            catalog.ConfigureCharacters(definitions.ToArray());
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Characters] Built {definitions.Count} characters (walk {walkSpeed:F2} m/s) → {DefinitionFolder}");
        }

        /// <summary>Character definitions already built (Build Content keeps them in the catalog).</summary>
        public static CharacterDefinition[] Existing() =>
            Specs.Select(s => AssetDatabase.LoadAssetAtPath<CharacterDefinition>($"{DefinitionFolder}/{s.Id}.asset")).Where(d => d != null).ToArray();

        private static float walkSpeed = 1f;

        private static GameObject BuildPrefab(Spec spec, GameObject source, AnimatorController controller, string folder)
        {
            var root = new GameObject(spec.Slug);
            try
            {
                var model = (GameObject)PrefabUtility.InstantiatePrefab(source, root.transform);
                model.name = "Model";
                // The Animator faces the humanoid body along its own +Z, so the model needs no turn.
                model.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                model.transform.localScale = Vector3.one * spec.Scale;

                Animator animator = model.GetComponent<Animator>() ?? model.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

                TwoBoneIKConstraint left = Arm(model.transform, animator, "Left", HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, out Transform rigRoot);
                TwoBoneIKConstraint right = Arm(rigRoot, animator, "Right", HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, out _);
                var rigBuilder = model.AddComponent<RigBuilder>();
                rigBuilder.layers.Add(new RigLayer(rigRoot.GetComponent<Rig>(), true));

                Transform chalk = Chalk(animator.GetBoneTransform(HumanBodyBones.RightHand), folder);
                var rig = model.AddComponent<CharacterRig>();
                rig.Configure(animator, rigBuilder, left, right, chalk, walkSpeed * spec.Scale);

                var agent = root.AddComponent<NavMeshAgent>();
                agent.radius = 0.28f;
                agent.height = 1.75f;
                agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
                agent.enabled = false;
                var character = root.AddComponent<MatchCharacter>();
                character.Configure(rig, agent);

                string path = $"{folder}/{spec.Slug}.prefab";
                return PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static TwoBoneIKConstraint Arm(Transform parent, Animator animator, string side, HumanBodyBones upper, HumanBodyBones lower, HumanBodyBones hand, out Transform rigRoot)
        {
            rigRoot = parent.Find("IK Rig");
            if (rigRoot == null)
            {
                rigRoot = new GameObject("IK Rig").transform;
                rigRoot.SetParent(parent, false);
                rigRoot.gameObject.AddComponent<Rig>();
            }

            var constraintObject = new GameObject($"{side} Arm IK");
            constraintObject.transform.SetParent(rigRoot, false);
            var target = new GameObject($"{side} Hand Target").transform;
            target.SetParent(rigRoot, false);
            var hint = new GameObject($"{side} Elbow Hint").transform;
            hint.SetParent(rigRoot, false);
            Transform handBone = animator.GetBoneTransform(hand);
            target.SetPositionAndRotation(handBone.position, handBone.rotation);
            hint.position = animator.GetBoneTransform(lower).position + Vector3.up * 0.1f;

            var constraint = constraintObject.AddComponent<TwoBoneIKConstraint>();
            TwoBoneIKConstraintData data = constraint.data;
            data.root = animator.GetBoneTransform(upper);
            data.mid = animator.GetBoneTransform(lower);
            data.tip = handBone;
            data.target = target;
            data.hint = hint;
            data.targetPositionWeight = 1f;
            // Hand orientation stays the animation's; only the position is solved (bone axes differ per rig).
            data.targetRotationWeight = 0f;
            data.hintWeight = 1f;
            constraint.data = data;
            constraint.weight = 0f;
            return constraint;
        }

        private static Transform Chalk(Transform hand, string folder)
        {
            Material blue = GeneratedArt.Lit($"{folder}/chalk.mat", new Color(0.12f, 0.35f, 0.75f), 0.2f, 0f);
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(cube.GetComponent<Collider>());
            cube.name = "Chalk";
            cube.transform.SetParent(hand, false);
            cube.transform.localPosition = new Vector3(0f, 0.08f, 0.02f);
            cube.transform.localScale = Vector3.one * 0.022f / Mathf.Max(hand.lossyScale.x, 1e-4f);
            cube.GetComponent<Renderer>().sharedMaterial = blue;
            cube.SetActive(false);
            return cube.transform;
        }

        private static CharacterDefinition Definition(Spec spec, GameObject prefab, Sprite portrait)
        {
            string path = $"{DefinitionFolder}/{spec.Id}.asset";
            var definition = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(path);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<CharacterDefinition>();
                AssetDatabase.CreateAsset(definition, path);
            }

            definition.Configure(spec.Id, spec.Name, spec.Female, prefab, portrait);
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static Sprite Portrait(GameObject source, string path)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            Animator animator = instance.GetComponent<Animator>();
            AnimationClip idle = AssetDatabase.LoadAllAssetsAtPath(ClipLibrary).OfType<AnimationClip>().First(c => c.name.EndsWith("|Idle_Loop"));
            Sample(instance, idle, 0.5f);
            Vector3 head = animator.GetBoneTransform(HumanBodyBones.Head).position + Vector3.up * 0.06f;
            // The model faces -Z: the camera stands in front of the face, slightly to the side.
            try
            {
                return IconStudio.Render(instance, path, head + new Vector3(0.25f, 0.02f, -0.85f), head - Vector3.up * 0.08f, 30f);
            }
            finally
            {
                AnimationMode.StopAnimationMode();
            }
        }

        /// <summary>Poses a Humanoid in edit mode (AnimationClip.SampleAnimation ignores the avatar's retargeting).</summary>
        private static void Sample(GameObject instance, AnimationClip clip, float time)
        {
            bool started = !AnimationMode.InAnimationMode();
            if (started) AnimationMode.StartAnimationMode();
            AnimationMode.BeginSampling();
            AnimationMode.SampleAnimationClip(instance, clip, time);
            AnimationMode.EndSampling();
        }

        // ---------------- Animator controller ----------------

        private static AnimatorController Controller(string folder, GameObject sample, Dictionary<string, AnimationClip> library, out float walk)
        {
            AnimationClip idle = library["Idle_Loop"];
            AnimationClip walkClip = library["Walk_Loop"];
            walk = MeasureWalkSpeed(sample, walkClip);

            AnimationClip address = PoseClip(folder, "Address", idle, sample, true, (0f, (Pose)AddressPose));
            AnimationClip sad = PoseClip(folder, "Disappointed", idle, sample, false, (0f, (Pose)Neutral), (0.35f, (Pose)SadPose), (1.7f, (Pose)SadPose), (2.1f, (Pose)Neutral));
            AnimationClip celebrate = PoseClip(folder, "Celebrate", idle, sample, false, (0f, (Pose)Neutral), (0.3f, (Pose)CheerPose), (0.6f, (Pose)CheerLowPose), (0.9f, (Pose)CheerPose), (1.5f, (Pose)Neutral));

            string path = $"{folder}/PlayerCharacter.controller";
            AssetDatabase.DeleteAsset(path);
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Address", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Seated", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Sad", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Celebrate", AnimatorControllerParameterType.Trigger);

            AnimatorControllerLayer[] layers = controller.layers;
            layers[0].iKPass = true;   // head look-at (CharacterRig.OnAnimatorIK)
            controller.layers = layers;
            AnimatorStateMachine machine = controller.layers[0].stateMachine;

            AnimatorState locomotion = machine.AddState("Locomotion");
            var tree = new BlendTree { name = "Locomotion", blendType = BlendTreeType.Simple1D, blendParameter = "Speed", useAutomaticThresholds = false };
            AssetDatabase.AddObjectToAsset(tree, controller);
            tree.AddChild(idle, 0f);
            tree.AddChild(walkClip, walk);
            locomotion.motion = tree;
            machine.defaultState = locomotion;

            AnimatorState addressState = machine.AddState("Address");
            addressState.motion = address;
            Transition(locomotion, addressState, 0.35f, ("Address", true));
            Transition(addressState, locomotion, 0.3f, ("Address", false));

            AnimatorState sitEnter = machine.AddState("Sit Enter");
            sitEnter.motion = library["Sitting_Enter"];
            AnimatorState sitIdle = machine.AddState("Sit Idle");
            sitIdle.motion = library["Sitting_Idle_Loop"];
            AnimatorState sitExit = machine.AddState("Sit Exit");
            sitExit.motion = library["Sitting_Exit"];
            Transition(locomotion, sitEnter, 0.25f, ("Seated", true));
            ExitTransition(sitEnter, sitIdle, 0.25f);
            Transition(sitIdle, sitExit, 0.2f, ("Seated", false));
            ExitTransition(sitExit, locomotion, 0.25f);

            AnimatorState sadState = machine.AddState("Disappointed");
            sadState.motion = sad;
            AnimatorState cheer = machine.AddState("Celebrate");
            cheer.motion = celebrate;
            Trigger(locomotion, sadState, "Sad");
            Trigger(locomotion, cheer, "Celebrate");
            ExitTransition(sadState, locomotion, 0.2f);
            ExitTransition(cheer, locomotion, 0.2f);
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void Transition(AnimatorState from, AnimatorState to, float duration, (string name, bool value) condition)
        {
            AnimatorStateTransition t = from.AddTransition(to);
            t.hasExitTime = false;
            t.duration = duration;
            t.AddCondition(condition.value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, condition.name);
        }

        private static void Trigger(AnimatorState from, AnimatorState to, string trigger)
        {
            AnimatorStateTransition t = from.AddTransition(to);
            t.hasExitTime = false;
            t.duration = 0.2f;
            t.AddCondition(AnimatorConditionMode.If, 0f, trigger);
        }

        private static void ExitTransition(AnimatorState from, AnimatorState to, float duration)
        {
            AnimatorStateTransition t = from.AddTransition(to);
            t.hasExitTime = true;
            t.exitTime = 0.92f;
            t.duration = duration;
        }

        /// <summary>The in-place walk's speed: how fast a planted foot slides back under the body.</summary>
        private static float MeasureWalkSpeed(GameObject source, AnimationClip walk)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            try
            {
                Transform foot = instance.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.LeftFoot);
                const int samples = 80;
                float dt = walk.length / samples, lowest = float.MaxValue;
                var positions = new Vector3[samples + 1];
                for (int i = 0; i <= samples; i++)
                {
                    Sample(instance, walk, dt * i);
                    positions[i] = foot.position;
                    lowest = Mathf.Min(lowest, foot.position.y);
                }

                float sum = 0f;
                int planted = 0;
                for (int i = 0; i < samples; i++)
                {
                    if (positions[i].y > lowest + 0.02f || positions[i + 1].y > lowest + 0.02f) continue;
                    sum += Vector3.Distance(positions[i], positions[i + 1]) / dt;
                    planted++;
                }

                return planted > 0 ? sum / planted : 1f;
            }
            finally
            {
                if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
                Object.DestroyImmediate(instance);
            }
        }

        // ---------------- Procedural poses (muscle offsets on the idle pose, normalised -1..1) ----------------

        private delegate void Pose(Dictionary<string, float> m, ref Quaternion body);

        private static void Neutral(Dictionary<string, float> m, ref Quaternion body) { }

        /// <summary>
        /// Bent over the cue: body tipped 65° with the legs flexed back to upright (left foot forward), neck and head
        /// raised to look along the cue. Values found by a pose search on the UAL idle frame (feet level, hips over the
        /// feet, head ~1.08 m up and ~0.5 m forward). Upper Leg Front-Back is negative towards the front.
        /// </summary>
        private static void AddressPose(Dictionary<string, float> m, ref Quaternion body)
        {
            body *= Quaternion.Euler(65f, 0f, 0f);
            Add(m, "Left Upper Leg Front-Back", -0.75f);
            Add(m, "Right Upper Leg Front-Back", -1.15f);
            Add(m, "Neck Nod Down-Up", 0.75f);
            Add(m, "Head Nod Down-Up", 0.75f);
        }

        private static void SadPose(Dictionary<string, float> m, ref Quaternion body)
        {
            Add(m, "Head Nod Down-Up", -0.45f);
            Add(m, "Neck Nod Down-Up", -0.15f);
            Add(m, "Head Turn Left-Right", 0.15f);
            Add(m, "Left Shoulder Down-Up", -0.5f);
            Add(m, "Right Shoulder Down-Up", -0.5f);
        }

        private static void CheerPose(Dictionary<string, float> m, ref Quaternion body)
        {
            Add(m, "Right Arm Down-Up", 1.3f);
            Add(m, "Right Forearm Stretch", -0.3f);
            Add(m, "Left Arm Down-Up", 0.5f);
            Add(m, "Head Nod Down-Up", 0.3f);
        }

        private static void CheerLowPose(Dictionary<string, float> m, ref Quaternion body)
        {
            Add(m, "Right Arm Down-Up", 0.9f);
            Add(m, "Right Forearm Stretch", -0.6f);
            Add(m, "Left Arm Down-Up", 0.3f);
            Add(m, "Head Nod Down-Up", 0.2f);
        }

        private static void Add(Dictionary<string, float> m, string muscle, float delta)
        {
            if (!m.ContainsKey(muscle)) throw new System.ArgumentException($"Unknown muscle '{muscle}'");
            m[muscle] = Mathf.Clamp(m[muscle] + delta, -1f, 1f);
        }

        /// <summary>
        /// Writes a humanoid clip of muscle/body keys built from the idle frame, then lowers or raises the body so the
        /// lower foot rests where it does in the idle pose (the feet stay on the floor).
        /// </summary>
        private static AnimationClip PoseClip(string folder, string name, AnimationClip idle, GameObject sample, bool loop, params (float time, Pose pose)[] keys)
        {
            string path = $"{folder}/{name}.anim";
            var clip = new AnimationClip { name = name };
            var baseMuscles = new Dictionary<string, float>();
            foreach (string muscle in HumanTrait.MuscleName) baseMuscles[muscle] = Value(idle, muscle);
            Vector3 rootT = new Vector3(Value(idle, "RootT.x"), Value(idle, "RootT.y"), Value(idle, "RootT.z"));
            var rootQ = new Quaternion(Value(idle, "RootQ.x"), Value(idle, "RootQ.y"), Value(idle, "RootQ.z"), Value(idle, "RootQ.w"));

            var curves = new Dictionary<string, AnimationCurve>();
            foreach ((float time, Pose pose) in keys)
            {
                var muscles = new Dictionary<string, float>(baseMuscles);
                Quaternion body = rootQ;
                pose(muscles, ref body);
                foreach (KeyValuePair<string, float> kv in muscles) Key(curves, kv.Key, time, kv.Value);
                Key(curves, "RootQ.x", time, body.x);
                Key(curves, "RootQ.y", time, body.y);
                Key(curves, "RootQ.z", time, body.z);
                Key(curves, "RootQ.w", time, body.w);
                Key(curves, "RootT.x", time, rootT.x);
                Key(curves, "RootT.y", time, rootT.y);
                Key(curves, "RootT.z", time, rootT.z);
            }

            if (keys.Length == 1)
            {
                // A held pose: a second key one second later so the clip has length.
                foreach (AnimationCurve curve in curves.Values) curve.AddKey(1f, curve.keys[0].value);
            }

            Write(clip, curves);
            float drop = FootDrop(sample, idle, clip, keys.Length == 1 ? 0.5f : keys[1].time);
            if (Mathf.Abs(drop) > 0.002f)
            {
                AnimationCurve y = curves["RootT.y"];
                Keyframe[] k = y.keys;
                for (int i = 0; i < k.Length; i++) k[i].value -= drop * (keys.Length == 1 || (i > 0 && i < k.Length - 1) ? 1f : 0f);
                y.keys = k;
                Write(clip, curves);
            }

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(clip, path);
            Debug.Log($"[Characters] {name}: body moved {-drop:F3} to keep the feet down");
            return clip;
        }

        private static void Write(AnimationClip clip, Dictionary<string, AnimationCurve> curves)
        {
            foreach (KeyValuePair<string, AnimationCurve> kv in curves)
            {
                AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(string.Empty, typeof(Animator), kv.Key), kv.Value);
            }
        }

        private static void Key(Dictionary<string, AnimationCurve> curves, string name, float time, float value)
        {
            if (!curves.TryGetValue(name, out AnimationCurve curve)) curves[name] = curve = new AnimationCurve();
            curve.AddKey(new Keyframe(time, value));
        }

        private static float Value(AnimationClip clip, string property)
        {
            AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve(string.Empty, typeof(Animator), property));
            return curve != null ? curve.Evaluate(0f) : 0f;
        }

        /// <summary>How much lower (positive) the lowest foot sits in <paramref name="pose"/> than in the idle frame.</summary>
        private static float FootDrop(GameObject source, AnimationClip idle, AnimationClip pose, float time)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            try
            {
                Animator animator = instance.GetComponent<Animator>();
                Transform left = animator.GetBoneTransform(HumanBodyBones.LeftFoot), right = animator.GetBoneTransform(HumanBodyBones.RightFoot);
                Sample(instance, idle, 0f);
                float idleFoot = Mathf.Min(left.position.y, right.position.y);
                Vector3 idleHead = animator.GetBoneTransform(HumanBodyBones.Head).position;
                Sample(instance, pose, time);
                Vector3 head = animator.GetBoneTransform(HumanBodyBones.Head).position;
                Debug.Log($"[Characters] {pose.name}: head {idleHead} → {head}");
                return Mathf.Min(left.position.y, right.position.y) - idleFoot;
            }
            finally
            {
                if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
                Object.DestroyImmediate(instance);
            }
        }
    }
}
