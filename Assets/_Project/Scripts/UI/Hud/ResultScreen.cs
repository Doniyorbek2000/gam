using System;
using BilliardGame.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BilliardGame.UI
{
    /// <summary>
    /// Match result (docs/UI_UX.md §10): victory/defeat banner with rays, XP bar and coin reward counting up, level-up
    /// line, "Play again" and "Lobby".
    /// </summary>
    public sealed class ResultScreen : ScreenView
    {
        private const float RaySpeed = 18f;

        [SerializeField] private UITheme theme;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text subtitle;
        [SerializeField] private RectTransform banner;
        [SerializeField] private RectTransform rays;
        [SerializeField] private Image glow;
        [SerializeField] private CountUpText xpCount;
        [SerializeField] private ProgressBar xpBar;
        [SerializeField] private TMP_Text levelLabel;
        [SerializeField] private GameObject coinRow;
        [SerializeField] private CountUpText coinCount;
        [SerializeField] private TMP_Text practiceNote;
        [SerializeField] private TMP_Text potted;
        [SerializeField] private Button againButton;
        [SerializeField] private Button lobbyButton;

        public event Action AgainPressed;
        public event Action LobbyPressed;

        protected override void Awake()
        {
            base.Awake();
            againButton.onClick.AddListener(() =>
            {
                ScreenRouter.Instance.Close(this);
                AgainPressed?.Invoke();
            });
            lobbyButton.onClick.AddListener(() =>
            {
                ScreenRouter.Instance.Close(this);
                LobbyPressed?.Invoke();
            });
        }

        public void Present(MatchSummary summary)
        {
            Color tint = summary.Won ? theme.accent : theme.secondary;
            title.text = Loc.Get(summary.Won ? "result.victory" : "result.defeat").ToUpper(Loc.Culture);
            title.color = tint;
            glow.color = new Color(tint.r, tint.g, tint.b, 0.55f);
            rays.gameObject.SetActive(summary.Won);
            subtitle.text = Loc.Format("result.winner", summary.Winner);
            potted.text = Loc.Format("result.potted", summary.BallsPotted);

            int levelBefore = ProfileRules.Level(summary.XpBefore, out _, out _);
            int levelAfter = ProfileRules.Level(summary.XpBefore + summary.XpGained, out int into, out int needed);
            levelLabel.text = levelAfter > levelBefore ? Loc.Format("result.level_up", levelAfter) : Loc.Format("common.level", levelAfter);
            levelLabel.color = levelAfter > levelBefore ? theme.accent : theme.textSecondary;

            ScreenRouter.Instance.Open(this);
            xpCount.CountFrom(0, summary.XpGained);
            xpBar.Set(0f, false);
            xpBar.Set(needed > 0 ? (float)into / needed : 1f);
            coinRow.SetActive(!summary.Practice);
            practiceNote.gameObject.SetActive(summary.Practice);
            if (!summary.Practice) coinCount.CountFrom(0, summary.CoinsGained);

            banner.localScale = Vector3.one * 0.4f;
            Tween.Scale(banner, Vector3.one, 0.45f, Ease.OutBack);
            UIFeedback.Confirm();
        }

        // Back = Lobby (the match is over).
        public override bool OnBack()
        {
            lobbyButton.onClick.Invoke();
            return true;
        }

        private void Update()
        {
            if (rays.gameObject.activeSelf) rays.Rotate(0f, 0f, -RaySpeed * Time.unscaledDeltaTime);
        }
    }
}
