using BilliardGame.Physics;
using BilliardGame.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BilliardGame.Editor
{
    /// <summary>Non-gameplay dressing: table body, chrome castings, diamonds, floor, lights, reflections and post-processing.</summary>
    internal static class TableDressingBuilder
    {
        public const string ReflectionOnlyLayer = "ReflectionOnly";
        private const string PostFxPath = "Assets/_Project/Art/PostFX.asset";

        public static void Build(TableGeometry table, Transform root, Transform garage)
        {
            Material wood = AssetDatabase.LoadAssetAtPath<Material>($"{BallMaterialFactory.MaterialFolder}/Wood.mat");
            // Satin rather than mirror: a mirror only shows the dark room and reads as black plates.
            Material chrome = BallMaterialFactory.Lit("Chrome", new Color(0.8f, 0.8f, 0.82f), 0.4f, 1f);
            Material pearl = BallMaterialFactory.Lit("Pearl", new Color(0.95f, 0.93f, 0.85f), 0.9f);
            Material floor = BallMaterialFactory.Lit("Floor", new Color(0.09f, 0.07f, 0.06f), 0.2f);

            float hl = (float)table.HalfLength;
            float hw = (float)table.HalfWidth;
            float frame = BilliardSceneBuilder.CushionDepth + BilliardSceneBuilder.RailWidth;
            float railMid = BilliardSceneBuilder.CushionDepth + BilliardSceneBuilder.RailWidth * 0.5f;
            float top = BilliardSceneBuilder.RailHeight;

            BilliardSceneBuilder.Box("Body", root, new Vector3(0f, -0.15f, 0f), Quaternion.identity,
                new Vector3(2f * (hl + frame), 0.22f, 2f * (hw + frame)), wood);
            BilliardSceneBuilder.Box("Floor", garage, new Vector3(0f, -0.8f, 0f), Quaternion.identity, new Vector3(12f, 0.02f, 8f), floor);

            // Chrome castings around the pocket openings.
            const float plateHeight = 0.006f;
            float plateY = top + plateHeight * 0.5f;
            for (int sx = -1; sx <= 1; sx += 2)
            {
                for (int sz = -1; sz <= 1; sz += 2)
                {
                    var centre = new Vector3(sx * (hl + 0.035f), plateY, sz * (hw + 0.035f));
                    BilliardSceneBuilder.Box($"Corner Casting {sx},{sz}", root, centre, Quaternion.identity, new Vector3(0.21f, plateHeight, 0.21f), chrome);
                }

                var side = new Vector3(0f, plateY, sx * (hw + 0.055f));
                BilliardSceneBuilder.Box($"Side Casting {sx}", root, side, Quaternion.identity, new Vector3(0.17f, plateHeight, 0.13f), chrome);
            }

            // Sights: three diamonds between pockets on every rail.
            var diamond = new Vector3(0.014f, 0.0008f, 0.014f);
            float length = (float)table.Length;
            float width = (float)table.Width;
            foreach (int k in new[] { 1, 2, 3, 5, 6, 7 })
            {
                float x = -hl + length * k / 8f;
                BilliardSceneBuilder.Primitive(PrimitiveType.Cylinder, "Diamond", root, new Vector3(x, top + 0.0008f, hw + railMid), Quaternion.identity, diamond, pearl);
                BilliardSceneBuilder.Primitive(PrimitiveType.Cylinder, "Diamond", root, new Vector3(x, top + 0.0008f, -hw - railMid), Quaternion.identity, diamond, pearl);
            }

            for (int k = 1; k <= 3; k++)
            {
                float z = -hw + width * k / 4f;
                BilliardSceneBuilder.Primitive(PrimitiveType.Cylinder, "Diamond", root, new Vector3(hl + railMid, top + 0.0008f, z), Quaternion.identity, diamond, pearl);
                BilliardSceneBuilder.Primitive(PrimitiveType.Cylinder, "Diamond", root, new Vector3(-hl - railMid, top + 0.0008f, z), Quaternion.identity, diamond, pearl);
            }
        }

        public static void BuildLightingAndPostFx(Camera camera)
        {
            int reflectionLayer = EnsureLayer(ReflectionOnlyLayer);

            var sun = new GameObject("Key Light").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.96f, 0.9f);
            sun.intensity = 1.25f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.8f;
            sun.transform.rotation = Quaternion.Euler(72f, -25f, 0f);

            // Pool-hall lamp: warm pool of light on the cloth, no shadows (the key light owns those).
            var lamp = new GameObject("Table Lamp").AddComponent<Light>();
            lamp.type = LightType.Spot;
            lamp.color = new Color(1f, 0.92f, 0.8f);
            lamp.intensity = 2.2f;
            lamp.range = 3.5f;
            lamp.spotAngle = 120f;
            lamp.innerSpotAngle = 60f;
            lamp.shadows = LightShadows.None;
            lamp.transform.SetPositionAndRotation(new Vector3(0f, 1.6f, 0f), Quaternion.Euler(90f, 0f, 0f));

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.5f, 0.5f, 0.55f);
            RenderSettings.ambientEquatorColor = new Color(0.32f, 0.3f, 0.28f);
            RenderSettings.ambientGroundColor = new Color(0.08f, 0.07f, 0.06f);

            // Lamp panels exist only for reflections: the camera culls their layer, the probe renders it,
            // so every ball carries the familiar window highlight without a visible fixture.
            Material panel = BallMaterialFactory.Emissive("LampPanel", new Color(1f, 0.95f, 0.85f) * 6f);
            for (int i = -1; i <= 1; i += 2)
            {
                GameObject shade = BilliardSceneBuilder.Box($"Lamp Panel {i}", null, new Vector3(i * 0.65f, 1.2f, 0f), Quaternion.identity, new Vector3(0.6f, 0.02f, 0.35f), panel);
                shade.layer = reflectionLayer;
                shade.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            }

            camera.cullingMask &= ~(1 << reflectionLayer);

            var probe = new GameObject("Reflection Probe").AddComponent<ReflectionProbe>();
            probe.transform.position = new Vector3(0f, 0.25f, 0f);
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
            probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.NoTimeSlicing;
            probe.size = new Vector3(4f, 3f, 3f);
            probe.resolution = 128;
            probe.hdr = true;
            probe.cullingMask = ~0;

            UniversalAdditionalCameraData cameraData = camera.GetUniversalAdditionalCameraData();
            cameraData.renderPostProcessing = true;
            cameraData.renderShadows = true;
            camera.allowHDR = true;

            var volume = new GameObject("Post FX").AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = PostFxProfile();
        }

        private static VolumeProfile PostFxProfile()
        {
            AssetDatabase.DeleteAsset(PostFxPath);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, PostFxPath);

            var tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.Override(TonemappingMode.Neutral);

            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(1.0f);
            bloom.intensity.Override(0.35f);
            bloom.scatter.Override(0.6f);

            var colour = profile.Add<ColorAdjustments>(true);
            colour.postExposure.Override(0.15f);
            colour.contrast.Override(10f);
            colour.saturation.Override(8f);

            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.22f);
            vignette.smoothness.Override(0.45f);

            foreach (VolumeComponent component in profile.components) AssetDatabase.AddObjectToAsset(component, profile);
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return profile;
        }

        /// <summary>Returns the layer index, adding it to the first free user slot if needed.</summary>
        private static int EnsureLayer(string name)
        {
            int existing = LayerMask.NameToLayer(name);
            if (existing >= 0) return existing;

            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");
            for (int i = 8; i < layers.arraySize; i++)
            {
                SerializedProperty slot = layers.GetArrayElementAtIndex(i);
                if (!string.IsNullOrEmpty(slot.stringValue)) continue;
                slot.stringValue = name;
                tagManager.ApplyModifiedPropertiesWithoutUndo();
                return i;
            }

            throw new System.InvalidOperationException("No free user layer for " + name);
        }
    }
}
