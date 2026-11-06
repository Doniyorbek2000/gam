using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace BilliardGame.Editor
{
    /// <summary>Android player settings required by CLAUDE.md: IL2CPP, ARM64, API 26+, landscape.</summary>
    internal static class ProjectSetup
    {
        public const string AndroidIdentifier = "com.billiardgame.mobile";

        [MenuItem("BilliardGame/Configure Android Player")]
        public static void ConfigureAndroid()
        {
            PlayerSettings.productName = "BilliardGame";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, AndroidIdentifier);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;

            // Keeps Play mode ticking while the editor is unfocused (MCP-driven runs); no effect on Android.
            PlayerSettings.runInBackground = true;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            AssetDatabase.SaveAssets();
            Debug.Log("[BilliardGame] Android player configured: IL2CPP, ARM64, min API 26, landscape.");
        }

        public const string DevApkPath = "Builds/BilliardGame-dev.apk";

        [MenuItem("BilliardGame/Build Android Dev APK")]
        public static void BuildAndroidDevApk()
        {
            ConfigureAndroid();
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            {
                throw new System.InvalidOperationException("Android Build Support module is not installed (Unity Hub > Installs > Add modules).");
            }

            EditorUserBuildSettings.buildAppBundle = false;
            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),   // Main + venue scenes
                locationPathName = DevApkPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.Development,
            };

            UnityEditor.Build.Reporting.BuildReport report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            if (summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                throw new System.InvalidOperationException($"Android build {summary.result}: {summary.totalErrors} errors, see Console.");
            }

            // summary.totalSize is the uncompressed content size; the APK file is what gets installed.
            long apkBytes = new System.IO.FileInfo(DevApkPath).Length;
            Debug.Log($"[BilliardGame] Built {DevApkPath}: {apkBytes / (1024f * 1024f):F1} MB in {summary.totalTime.TotalMinutes:F1} min.");
        }

        public const string WindowsBotPath = "Builds/Win/BilliardGame.exe";

        /// <summary>
        /// Windows dev player for E2E (run with <c>-autopilot [-profile name]</c>). Addressables (localization tables)
        /// are rebuilt for Windows first: a player only reads the catalog of the active target, and the Android one
        /// made the bot log "SelectedLocale is null". The editor is switched back to Android afterwards.
        /// </summary>
        [MenuItem("BilliardGame/Build Windows Dev Bot")]
        public static void BuildWindowsDevBot()
        {
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
            {
                throw new System.InvalidOperationException("Could not switch to Windows (Windows Build Support module missing?).");
            }

            try
            {
                UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.BuildPlayerContent(out var content);
                if (!string.IsNullOrEmpty(content.Error)) throw new System.InvalidOperationException("Addressables build failed: " + content.Error);

                var options = new BuildPlayerOptions
                {
                    scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                    locationPathName = WindowsBotPath,
                    target = BuildTarget.StandaloneWindows64,
                    targetGroup = BuildTargetGroup.Standalone,
                    options = BuildOptions.Development,
                };
                var summary = BuildPipeline.BuildPlayer(options).summary;
                if (summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                {
                    throw new System.InvalidOperationException($"Windows build {summary.result}: {summary.totalErrors} errors, see Console.");
                }

                Debug.Log($"[BilliardGame] Built {WindowsBotPath} in {summary.totalTime.TotalMinutes:F1} min.");
            }
            finally
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            }
        }

        /// <summary>
        /// The whole scene fits in a 3 m box, so a short shadow distance gives crisp ball shadows;
        /// MSAA keeps ball silhouettes and aim lines clean, full render scale keeps the numbers legible.
        /// </summary>
        [MenuItem("BilliardGame/Configure Rendering")]
        public static void ConfigureRendering()
        {
            ConfigurePipeline("Assets/Settings/Mobile_RPAsset.asset", shadowResolution: 2048, cascades: 1);
            ConfigurePipeline("Assets/Settings/PC_RPAsset.asset", shadowResolution: 2048, cascades: 1);
            AssetDatabase.SaveAssets();
            Debug.Log("[BilliardGame] URP assets configured: shadows 6 m soft, MSAA 4x, render scale 1, GPU Resident Drawer off.");
        }

        private static void ConfigurePipeline(string path, int shadowResolution, int cascades)
        {
            Object asset = AssetDatabase.LoadMainAssetAtPath(path);
            if (asset == null) throw new System.IO.FileNotFoundException("URP asset missing", path);

            var serialized = new SerializedObject(asset);
            SetFloat(serialized, "m_ShadowDistance", 6f);
            SetInt(serialized, "m_ShadowCascadeCount", cascades);
            SetInt(serialized, "m_MainLightShadowmapResolution", shadowResolution);
            SetBool(serialized, "m_SoftShadowsSupported", true);
            SetBool(serialized, "m_SupportsHDR", true);
            SetInt(serialized, "m_MSAA", 4);
            SetFloat(serialized, "m_RenderScale", 1f);
            // Off: ~40 renderers gain nothing from it, and in 6000.5 recreating it after Play crashed the editor.
            SetInt(serialized, "m_GPUResidentDrawerMode", 0);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static SerializedProperty Find(SerializedObject serialized, string name) =>
            serialized.FindProperty(name) ?? throw new System.MissingFieldException(serialized.targetObject.name, name);

        private static void SetFloat(SerializedObject s, string name, float value) => Find(s, name).floatValue = value;

        private static void SetInt(SerializedObject s, string name, int value) => Find(s, name).intValue = value;

        private static void SetBool(SerializedObject s, string name, bool value) => Find(s, name).boolValue = value;
    }
}
