using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace BilliardGame.Editor.Content
{
    /// <summary>Renders 256×256 transparent item icons in an isolated preview scene (docs/ART_PIPELINE.md §5 "Icon Studio").</summary>
    internal static class IconStudio
    {
        public const string HdriPath = "Assets/_Project/Art/Hdri/hdri-interior/billiard_hall_1k.hdr";
        private const int Size = 256;

        /// <summary>Renders <paramref name="subject"/> (moved into the preview scene and destroyed) and saves a sprite.</summary>
        public static Sprite Render(GameObject subject, string path, Vector3 cameraPosition, Vector3 lookAt, float fov)
        {
            Scene scene = EditorSceneManager.NewPreviewScene();
            RenderTexture target = RenderTexture.GetTemporary(Size, Size, 24, RenderTextureFormat.ARGB32);
            try
            {
                SceneManager.MoveGameObjectToScene(subject, scene);

                var lightObject = new GameObject("Icon Key");
                SceneManager.MoveGameObjectToScene(lightObject, scene);
                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 2.4f;
                light.color = new Color(1f, 0.96f, 0.9f);
                light.transform.rotation = Quaternion.Euler(35f, -25f, 0f);

                var fillObject = new GameObject("Icon Fill");
                SceneManager.MoveGameObjectToScene(fillObject, scene);
                var fill = fillObject.AddComponent<Light>();
                fill.type = LightType.Directional;
                fill.intensity = 1.1f;
                fill.transform.rotation = Quaternion.Euler(10f, 160f, 0f);

                // Neutral ambient, and the CC0 billiard-hall HDRI for metals and lacquer to reflect (not the blue default sky).
                Unsupported.SetOverrideLightingSettings(scene);
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.42f, 0.4f, 0.38f);
                var hdri = AssetDatabase.LoadAssetAtPath<Cubemap>(HdriPath);
                if (hdri != null)
                {
                    RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Custom;
                    RenderSettings.customReflectionTexture = hdri;
                    RenderSettings.reflectionIntensity = 1f;
                }

                var cameraObject = new GameObject("Icon Camera");
                SceneManager.MoveGameObjectToScene(cameraObject, scene);
                var camera = cameraObject.AddComponent<Camera>();
                camera.scene = scene;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
                camera.fieldOfView = fov;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 20f;
                camera.transform.position = cameraPosition;
                camera.transform.LookAt(lookAt);
                camera.targetTexture = target;
                camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
                camera.Render();

                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = target;
                var pixels = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
                pixels.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
                RenderTexture.active = previous;
                Color32[] data = pixels.GetPixels32();
                Object.DestroyImmediate(pixels);

                GeneratedArt.SavePng(path, Size, Size, data, wrap: TextureWrapMode.Clamp);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;   // the project default is Multiple, which yields no main sprite
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings { name = "Android", overridden = true, maxTextureSize = Size, format = TextureImporterFormat.ASTC_4x4 });
                importer.SaveAndReimport();
                return AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            finally
            {
                Unsupported.RestoreOverrideLightingSettings();
                RenderTexture.ReleaseTemporary(target);
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
}
