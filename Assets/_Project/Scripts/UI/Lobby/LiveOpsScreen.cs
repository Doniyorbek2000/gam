using System.Threading.Tasks;
using BilliardGame.Online;
using UnityEngine;

namespace BilliardGame.UI
{
    /// <summary>
    /// Shared base of the missions, Season Pass, events and daily shop screens: re-reads the server state when opened,
    /// redraws on every change, ticks countdowns once a second, and shows an offline / server-only state instead.
    /// </summary>
    public abstract class LiveOpsScreen : ScreenView
    {
        [SerializeField] protected GameServices services;
        [SerializeField] private GameObject body;
        [SerializeField] private EmptyStateView empty;

        private float nextTick;
        private bool busy;

        protected NakamaLiveOpsService LiveOps => services.LiveOps;

        protected override void OnShown()
        {
            base.OnShown();
            if (LiveOps == null)
            {
                // Mock economy (editor toggle / STORE_MOCK): LiveOps exists only on the server.
                ShowEmpty("liveops.server_only", null);
                return;
            }

            LiveOps.Changed += Redraw;
            Loc.Changed += Redraw;
            Redraw();
            _ = Reload();
        }

        protected override void OnHidden()
        {
            base.OnHidden();
            if (LiveOps == null) return;
            LiveOps.Changed -= Redraw;
            Loc.Changed -= Redraw;
        }

        private async Task Reload()
        {
            if (!await LiveOps.RefreshAsync() && LiveOps.State == null) ShowEmpty("liveops.offline", () => _ = Reload());
        }

        private void Redraw()
        {
            if (LiveOps?.State == null) return;
            empty.Hide();
            body.SetActive(true);
            Render();
            Tick();
        }

        private void ShowEmpty(string hintKey, UnityEngine.Events.UnityAction retry)
        {
            body.SetActive(false);
            empty.gameObject.SetActive(true);
            empty.Show(null, "liveops.unavailable", hintKey, retry != null ? "common.retry" : null, retry);
        }

        private void Update()
        {
            if (!IsOpen || LiveOps?.State == null || Time.unscaledTime < nextTick) return;
            nextTick = Time.unscaledTime + 1f;
            Tick();
        }

        /// <summary>Runs one server request at a time, then shows its error (if any) or the success toast.</summary>
        protected async void Run(Task<string> request, string successText)
        {
            if (busy) return;
            busy = true;
            string error = await request;
            busy = false;
            if (error != null)
            {
                Dialogs.Instance.Error("common.error", LiveOpsText.Error(error));
                return;
            }

            UIFeedback.Confirm();
            if (successText != null) Dialogs.Instance.Toast(successText);
        }

        /// <summary>Draws the whole state (called on open and after every server answer).</summary>
        protected abstract void Render();

        /// <summary>Updates countdown labels (once a second).</summary>
        protected abstract void Tick();
    }
}
