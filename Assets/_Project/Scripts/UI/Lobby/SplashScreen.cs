using System.Collections;
using TMPro;
using UnityEngine;

namespace BilliardGame.UI
{
    /// <summary>
    /// Splash / loading (docs/UI_UX.md §1): logo, progress bar and a rotating tip while localization tables and saved
    /// settings load. Fades out into the lobby.
    /// </summary>
    public sealed class SplashScreen : ScreenView
    {
        private const int TipCount = 6;
        private const float MinSeconds = 1.4f;

        [SerializeField] private ProgressBar progress;
        [SerializeField] private TMP_Text tip;
        [SerializeField] private TMP_Text status;
        [SerializeField] private RectTransform logo;

        private void Start()
        {
            ScreenRouter.Instance.Open(this);
            StartCoroutine(Load());
        }

        private IEnumerator Load()
        {
            progress.Set(0f, false);
            yield return null;
            Loc.ApplySavedLanguage();
            GameSettings.Apply();
            tip.text = Loc.Get("tip." + Random.Range(1, TipCount + 1));
            status.text = Loc.Get("splash.loading");
            Tween.Scale(logo, Vector3.one, 0.6f, Ease.OutBack);

            for (float t = 0f; t < MinSeconds; t += Time.unscaledDeltaTime)
            {
                progress.Set(Mathf.SmoothStep(0f, 1f, t / MinSeconds), false);
                yield return null;
            }

            progress.Set(1f, false);
            ScreenRouter.Instance.Close(this);
        }

        // The splash cannot be dismissed with Back.
        public override bool OnBack() => false;
    }
}
