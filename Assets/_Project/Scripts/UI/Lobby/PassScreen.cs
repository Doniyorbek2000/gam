using System.Collections.Generic;
using BilliardGame.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BilliardGame.UI
{
    /// <summary>
    /// Season Pass (docs/UI_UX.md §10, ECONOMY §6): level + XP, the 50-level free / premium reward track, tap a reached
    /// reward to claim it, "Claim all", premium for gems. Levels, rewards and prices come from the server config.
    /// </summary>
    public sealed class PassScreen : LiveOpsScreen
    {
        [SerializeField] private TMP_Text seasonLabel;
        [SerializeField] private TMP_Text timerLabel;
        [SerializeField] private TMP_Text levelLabel;
        [SerializeField] private TMP_Text xpLabel;
        [SerializeField] private ProgressBar xpBar;
        [SerializeField] private Button premiumButton;
        [SerializeField] private TMP_Text premiumLabel;
        [SerializeField] private GameObject premiumActive;
        [SerializeField] private Button claimAllButton;
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private RectTransform columnsRoot;
        [SerializeField] private PassColumnView columnTemplate;
        [SerializeField] private Sprite gemSprite;

        private readonly List<PassColumnView> columns = new List<PassColumnView>();
        private bool scrolled;

        protected override void Awake()
        {
            base.Awake();
            columnTemplate.gameObject.SetActive(false);
            premiumButton.onClick.AddListener(BuyPremium);
            claimAllButton.onClick.AddListener(() => Run(LiveOps.ClaimPassAsync(0, false), Loc.Get("pass.claimed_all")));
        }

        protected override void OnShown()
        {
            scrolled = false;
            base.OnShown();
        }

        protected override void Render()
        {
            PassInfo pass = LiveOps.State.pass;
            seasonLabel.text = Loc.Get("pass.season_" + pass.season, Loc.Format("pass.season", pass.season)).ToUpper(Loc.Culture);
            levelLabel.text = pass.level.ToString(Loc.Culture);
            LiveOpsRules.PassProgress(pass, out int into, out int needed);
            xpBar.Set((float)into / needed);
            xpLabel.text = pass.level >= pass.levels.Length ? Loc.Get("pass.max") : Loc.Number(into) + " / " + Loc.Number(needed) + " XP";
            premiumButton.gameObject.SetActive(!pass.premium && pass.active);
            premiumLabel.text = Loc.Format("pass.buy_premium", Loc.Number(pass.premiumPrice));
            premiumActive.SetActive(pass.premium);
            int claimable = LiveOpsRules.PassClaimable(pass);
            claimAllButton.interactable = claimable > 0;
            claimAllButton.GetComponent<CanvasGroup>().alpha = claimable > 0 ? 1f : 0.45f;

            while (columns.Count < pass.levels.Length) columns.Add(Instantiate(columnTemplate, columnsRoot));
            for (int i = 0; i < columns.Count; i++)
            {
                bool used = i < pass.levels.Length;
                columns[i].gameObject.SetActive(used);
                if (used) columns[i].Bind(pass.levels[i], pass, services.Catalog, Tap);
            }

            if (!scrolled && pass.levels.Length > 1)
            {
                scrolled = true;
                Canvas.ForceUpdateCanvases();
                scroll.horizontalNormalizedPosition = Mathf.Clamp01((pass.level - 2f) / (pass.levels.Length - 1f));
            }
        }

        protected override void Tick()
        {
            PassInfo pass = LiveOps.State.pass;
            if (pass.active) timerLabel.text = Loc.Format("pass.ends", LiveOpsText.Countdown(LiveOps.Remaining(pass.endsInMs)));
            else if (pass.startsInMs > 0) timerLabel.text = Loc.Format("pass.starts", LiveOpsText.Countdown(LiveOps.Remaining(pass.startsInMs)));
            else timerLabel.text = Loc.Get("pass.ended");
        }

        private void Tap(int level, bool premium)
        {
            PassInfo pass = LiveOps.State.pass;
            PassLevelInfo info = pass.levels[level - 1];
            if (premium && !pass.premium)
            {
                BuyPremium();
                return;
            }

            if (level > pass.level)
            {
                Dialogs.Instance.Toast(Loc.Format("pass.reach_level", level));
                return;
            }

            if (premium ? info.premiumClaimed : info.freeClaimed) return;
            string rewards = LiveOpsText.Rewards(premium ? info.premium : info.free, services.Catalog);
            Run(LiveOps.ClaimPassAsync(level, premium), Loc.Format("daily.got", rewards));
        }

        private void BuyPremium()
        {
            PassInfo pass = LiveOps.State.pass;
            if (pass.premium || !pass.active) return;
            Dialogs.Instance.Confirm("pass.premium_title", Loc.Get("pass.premium_body"), "common.confirm",
                () => Run(LiveOps.BuyPassAsync(), Loc.Get("pass.premium_done")), gemSprite, pass.premiumPrice);
        }
    }
}
