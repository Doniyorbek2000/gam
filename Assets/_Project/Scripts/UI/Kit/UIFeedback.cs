using UnityEngine;

namespace BilliardGame.UI
{
    /// <summary>Tap sound and haptic tick for every button; both follow Settings (effects volume, vibration).</summary>
    public static class UIFeedback
    {
        private const long TapMillis = 12;
        private const long StrongMillis = 35;

        private static AudioSource source;
        private static AudioClip tap;
        private static AudioClip confirm;
#if UNITY_ANDROID && !UNITY_EDITOR
        private static AndroidJavaObject vibrator;
#endif

        public static float Volume { get; set; } = 1f;
        public static bool Vibration { get; set; } = true;

        public static void Configure(AudioClip tapClip, AudioClip confirmClip)
        {
            tap = tapClip;
            confirm = confirmClip;
        }

        public static void Tap()
        {
            Play(tap, 0.6f);
            Haptic(TapMillis);
        }

        public static void Confirm()
        {
            Play(confirm != null ? confirm : tap, 0.8f);
            Haptic(StrongMillis);
        }

        private static void Play(AudioClip clip, float gain)
        {
            if (clip == null || Volume <= 0f || !Application.isPlaying) return;
            if (source == null)
            {
                var go = new GameObject("[UI Audio]") { hideFlags = HideFlags.HideInHierarchy };
                Object.DontDestroyOnLoad(go);
                source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
            }

            source.PlayOneShot(clip, gain * Volume);
        }

        private static void Haptic(long millis)
        {
            if (!Vibration) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                if (vibrator == null)
                {
                    using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                    using AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                    vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                }

                vibrator?.Call("vibrate", millis);
            }
            catch (System.Exception e)
            {
                // Some devices block the short vibrate call; turn haptics off rather than failing every tap.
                Debug.LogWarning("[UI] Haptics unavailable: " + e.Message);
                Vibration = false;
            }
#endif
        }
    }
}
