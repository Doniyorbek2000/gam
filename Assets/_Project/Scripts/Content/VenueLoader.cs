using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BilliardGame.Content
{
    /// <summary>
    /// Loads a venue scene additively around the table (its baked lightmaps come with it) and hides the built-in garage.
    /// shortcut: scenes are in the build; M4 moves venues to remote Addressables bundles.
    /// </summary>
    public sealed class VenueLoader : MonoBehaviour
    {
        [SerializeField] private GameObject garageRoot;

        private string loadedScene;

        public string LoadedScene => loadedScene;

        public async Task LoadAsync(VenueDefinition venue)
        {
            string target = venue != null ? venue.SceneName : null;
            if (target == loadedScene) return;
            await UnloadAsync();
            if (string.IsNullOrEmpty(target)) return;

            AsyncOperation load = SceneManager.LoadSceneAsync(target, LoadSceneMode.Additive);
            if (load == null)
            {
                Debug.LogError($"[Venue] Scene '{target}' is not in the build; staying in the garage.");
                return;
            }

            while (!load.isDone) await Task.Yield();
            loadedScene = target;
            garageRoot.SetActive(false);
            RefreshRealtimeProbes();
        }

        public async Task UnloadAsync()
        {
            if (loadedScene != null)
            {
                AsyncOperation unload = SceneManager.UnloadSceneAsync(loadedScene);
                loadedScene = null;
                while (unload != null && !unload.isDone) await Task.Yield();
            }

            garageRoot.SetActive(true);
            RefreshRealtimeProbes();
        }

        // The table's realtime probe renders once on awake; re-render it so the balls reflect the new room.
        private static void RefreshRealtimeProbes()
        {
            foreach (ReflectionProbe probe in FindObjectsByType<ReflectionProbe>())
            {
                if (probe.mode == UnityEngine.Rendering.ReflectionProbeMode.Realtime) probe.RenderProbe();
            }
        }
    }
}
