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
    }
}
