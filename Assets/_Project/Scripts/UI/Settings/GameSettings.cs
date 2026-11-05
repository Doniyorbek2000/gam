using System;
using BilliardGame.Presentation;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace BilliardGame.UI
{
    public enum GraphicsQuality
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Auto = 3,
    }

    /// <summary>Player options (docs/UI_UX.md §11 Settings). Saved as JSON in PlayerPrefs; applied immediately.</summary>
    [Serializable]
    public sealed class GameSettingsData
    {
        public int version = 1;
        public GraphicsQuality quality = GraphicsQuality.Auto;
        public int fps = 60;
        public bool shadows = true;
        public bool postProcessing = true;
        public bool batterySaver;
        public float master = 1f;
        public float music = 0.7f;
        public float effects = 1f;
        public float voice = 1f;
        public bool vibration = true;
        public float aimSensitivity = 1f;
        public float fineSensitivity = 1f;
        public bool leftHanded;
        public bool aimGuide = true;
        public bool confirmShot;
        public bool notifications = true;
    }

    public static class GameSettings
    {
        public static readonly int[] FpsOptions = { 30, 60, 90, 120 };
        private const string PrefsKey = "settings";
        private const int BatteryFps = 30;

        private static GameSettingsData data;

        public static event Action Changed;

        public static GameSettingsData Data => data ?? (data = Load());

        /// <summary>Frame rates the screen can show; 90/120 only on high-refresh panels.</summary>
        public static int MaxFps => Mathf.Max(60, Mathf.RoundToInt((float)Screen.currentResolution.refreshRateRatio.value));

        public static void Save()
        {
            PlayerPrefs.SetString(PrefsKey, JsonUtility.ToJson(Data));
            PlayerPrefs.Save();
            Apply();
            Changed?.Invoke();
        }

        /// <summary>Applies everything that does not need a scene reference; cameras and the HUD read <see cref="Data"/>.</summary>
        public static void Apply()
        {
            GameSettingsData s = Data;
            int level = s.quality == GraphicsQuality.Auto ? AutoQuality() : (int)s.quality;
            if (s.batterySaver) level = 0;
            ApplyQuality(level);
            Application.targetFrameRate = s.batterySaver ? BatteryFps : Mathf.Min(s.fps, MaxFps);
            QualitySettings.vSyncCount = 0;
            AudioListener.volume = s.master;
            TableAudio.EffectsVolume = s.effects;
            UIFeedback.Volume = s.effects;
            UIFeedback.Vibration = s.vibration;
            foreach (Camera camera in Camera.allCameras) ApplyCamera(camera);
        }

        public static void ApplyCamera(Camera camera)
        {
            var urp = camera != null ? camera.GetComponent<UniversalAdditionalCameraData>() : null;
            if (urp == null) return;
            urp.renderShadows = Data.shadows && !Data.batterySaver;
            urp.renderPostProcessing = Data.postProcessing && !Data.batterySaver;
        }

        public static void ApplyInput(CueController cue, FineTuneWheel wheel)
        {
            if (cue != null)
            {
                cue.AimSensitivity = Data.aimSensitivity;
                cue.ShowGuide = Data.aimGuide;
            }

            if (wheel != null) wheel.Sensitivity = Data.fineSensitivity;
        }

        /// <summary>Wipes saved settings (account deletion).</summary>
        public static void Reset()
        {
            PlayerPrefs.DeleteKey(PrefsKey);
            data = new GameSettingsData();
            Apply();
            Changed?.Invoke();
        }

        private static GameSettingsData Load()
        {
            string json = PlayerPrefs.GetString(PrefsKey, string.Empty);
            if (string.IsNullOrEmpty(json)) return new GameSettingsData();
            try
            {
                return JsonUtility.FromJson<GameSettingsData>(json) ?? new GameSettingsData();
            }
            catch (ArgumentException e)
            {
                Debug.LogWarning("[Settings] Saved settings unreadable, using defaults: " + e.Message);
                return new GameSettingsData();
            }
        }

        private static UniversalRenderPipelineAsset runtimeAsset;
        private static readonly float[] RenderScale = { 0.7f, 0.85f, 1f };
        private static readonly int[] Msaa = { 1, 2, 4 };
        private static readonly float[] ShadowDistance = { 8f, 15f, 25f };

        /// <summary>Tiers change a runtime copy of the URP asset, so the project asset on disk is never modified.</summary>
        private static void ApplyQuality(int level)
        {
            // In the editor the swap would be written into ProjectSettings; tiers are verified on device builds.
            if (Application.isEditor) return;
            if (runtimeAsset == null)
            {
                var source = QualitySettings.renderPipeline as UniversalRenderPipelineAsset ?? UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
                if (source == null) return;
                runtimeAsset = UnityEngine.Object.Instantiate(source);
                runtimeAsset.name = source.name + " (Runtime)";
                QualitySettings.renderPipeline = runtimeAsset;
            }

            level = Mathf.Clamp(level, 0, 2);
            runtimeAsset.renderScale = RenderScale[level];
            runtimeAsset.msaaSampleCount = Msaa[level];
            runtimeAsset.shadowDistance = ShadowDistance[level];
        }

        // Rough device tiering until M11 adds a benchmark: RAM and GPU memory.
        private static int AutoQuality()
        {
            int ram = SystemInfo.systemMemorySize;
            if (ram >= 6000 && SystemInfo.graphicsMemorySize >= 2000) return (int)GraphicsQuality.High;
            return ram >= 3500 ? (int)GraphicsQuality.Medium : (int)GraphicsQuality.Low;
        }
    }
}
