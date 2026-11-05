using System.Collections;
using System.Collections.Generic;
using System.Text;
using BilliardGame.Content;
using BilliardGame.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BilliardGame.UI
{
    /// <summary>
    /// Crates (docs/UI_UX.md §5–6): one card per crate with price, odds [i], pity counters and 1×/10×. Spending gems
    /// always asks first. The reveal plays the 3D crate on the lobby stage, then the result tiles pop in one by one.
    /// Odds text is generated from the crate table (CLAUDE.md rule 6).
    /// </summary>
    public sealed class CrateScreen : ScreenView
    {
        private const int DevCoins = 10_000;
        private const int DevGems = 1_000;

        [SerializeField] private GameServices services;
        [SerializeField] private LobbyStage stage;
        [SerializeField] private Sprite coinSprite;
        [SerializeField] private Sprite gemSprite;
        [SerializeField] private CurrencyPill coins;
        [SerializeField] private CurrencyPill gems;
        [SerializeField] private GameObject cardsRoot;
        [SerializeField] private CrateCardView[] cards = new CrateCardView[0];

        [SerializeField] private ScreenView oddsView;
        [SerializeField] private TMP_Text oddsTitle;
        [SerializeField] private TMP_Text oddsText;

        [SerializeField] private GameObject revealPanel;
        [SerializeField] private Button skipButton;
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TMP_Text resultTitle;
        [SerializeField] private ItemTile[] resultTiles = new ItemTile[0];
        [SerializeField] private Button equipButton;
        [SerializeField] private TMP_Text equipLabel;
        [SerializeField] private Button againButton;
        [SerializeField] private Button resultClose;

        private bool skipRequested;
        private bool busy;
        private string lastCrate;
        private int lastCount;
        private SkinDefinition equipCandidate;

        public bool IsRevealing => busy;

        protected override void Awake()
        {
            base.Awake();
            skipButton.onClick.AddListener(() => skipRequested = true);
            equipButton.onClick.AddListener(EquipCandidate);
            againButton.onClick.AddListener(() => RequestOpen(lastCrate, lastCount));
            resultClose.onClick.AddListener(CloseResult);
            // Mock/dev top-ups (STORE_MOCK and editor); M2 replaces them with the real shop.
            coins.Plus.onClick.AddListener(() => Grant(DevCoins, 0, "lobby.dev_coins", DevCoins));
            gems.Plus.onClick.AddListener(() => Grant(0, DevGems, "lobby.dev_gems", DevGems));
        }

        protected override void OnShown()
        {
            services.Economy.Changed += Refresh;
            Loc.Changed += Refresh;
            revealPanel.SetActive(false);
            resultPanel.SetActive(false);
            cardsRoot.SetActive(true);
            for (int i = 0; i < cards.Length; i++)
            {
                CrateDefinition crate = i < services.Economy.Crates.Count ? services.Economy.Crates[i] : null;
                cards[i].gameObject.SetActive(crate != null);
                if (crate != null) cards[i].Bind(crate, this);
            }

            coins.Set(services.Economy.Coins, false);
            gems.Set(services.Economy.Gems, false);
            Refresh();
        }

        protected override void OnHidden()
        {
            services.Economy.Changed -= Refresh;
            Loc.Changed -= Refresh;
            stage.ShowCharacter();
        }

        public override bool OnBack()
        {
            if (busy) return false;
            if (resultPanel.activeSelf)
            {
                CloseResult();
                return true;
            }

            return base.OnBack();
        }

        private void Refresh()
        {
            coins.Set(services.Economy.Coins);
            gems.Set(services.Economy.Gems);
            foreach (CrateCardView card in cards)
            {
                if (card.gameObject.activeSelf) card.Refresh(services.Economy);
            }
        }

        private void Grant(int coinAmount, int gemAmount, string key, int amount)
        {
            services.Mock.DevGrant(coinAmount, gemAmount);
            Dialogs.Instance.Toast(Loc.Format(key, Loc.Number(amount)));
        }

        public void ShowOdds(CrateDefinition crate)
        {
            oddsTitle.text = Loc.Format("crates.odds", CrateName(crate));
            oddsText.text = OddsText(crate, services.Catalog);
            ScreenRouter.Instance.Open(oddsView);
        }

        public static string CrateName(CrateDefinition crate) => Loc.Get("crate." + crate.id, crate.displayName);

        /// <summary>Odds window text, generated from the crate table and the catalog (CLAUDE.md rule 6).</summary>
        public static string OddsText(CrateDefinition crate, SkinCatalog catalog)
        {
            var text = new StringBuilder();
            for (int r = (int)Rarity.Mythic; r >= 0; r--)
            {
                var rarity = (Rarity)r;
                float weight = crate.weights[r];
                if (weight <= 0f) continue;
                var items = new List<SkinDefinition>();
                foreach (SkinDefinition skin in catalog.Skins)
                {
                    if (skin.Rarity == rarity) items.Add(skin);
                }

                text.AppendLine(RarityStyle.Tagged(rarity, $"<b>{RarityStyle.Label(rarity)}  {weight:0.##}%</b>"));
                foreach (SkinDefinition skin in items) text.AppendLine($"    {RarityStyle.ItemName(skin)} — {weight / items.Count:0.###}%");
            }

            text.AppendLine();
            foreach (PityRule rule in crate.pity ?? new PityRule[0])
            {
                double p = CrateRoller.ChanceAtLeast(crate.weights, rule.MinRarity) / 100.0;
                double effective = CrateRoller.EffectiveChance(p, rule.count) * 100.0;
                text.AppendLine(Loc.Format("crates.guarantee", rule.count, RarityStyle.Name(rule.MinRarity), (p * 100).ToString("0.##", Loc.Culture), effective.ToString("0.##", Loc.Culture)));
            }

            if (crate.HasTenGuarantee(out Rarity ten)) text.AppendLine(Loc.Format("crates.ten_guarantee", RarityStyle.Name(ten)));
            text.AppendLine(Loc.Get("crates.equal"));
            return text.ToString();
        }

        /// <summary>1× / 10× button: gem crates confirm the spend first (docs/UI_UX.md §5).</summary>
        public void RequestOpen(string crateId, int count)
        {
            if (busy || crateId == null) return;
            CrateDefinition crate = Find(crateId);
            if (crate == null) return;
            bool gemCrate = crate.Currency == Currency.Gem;
            int balance = gemCrate ? services.Economy.Gems : services.Economy.Coins;
            if (balance < crate.Price(count))
            {
                Dialogs.Instance.Toast(Loc.Get(gemCrate ? "crates.not_enough_gems" : "crates.not_enough_coins"));
                return;
            }

            if (!gemCrate)
            {
                Open(crateId, count);
                return;
            }

            Dialogs.Instance.Confirm("crates.open_confirm", Loc.Format("crates.open_body", CrateName(crate), count), "common.confirm",
                () => Open(crateId, count), gemSprite, crate.Price(count));
        }

        private CrateDefinition Find(string crateId)
        {
            foreach (CrateDefinition crate in services.Economy.Crates)
            {
                if (crate.id == crateId) return crate;
            }

            return null;
        }

        private async void Open(string crateId, int count)
        {
            if (busy) return;
            busy = true;
            lastCrate = crateId;
            lastCount = count;
            CrateOpenResult result = await services.Economy.OpenCrateAsync(crateId, count);
            if (!result.Success)
            {
                busy = false;
                Dialogs.Instance.Toast(result.Error);
                return;
            }

            StartCoroutine(Reveal(result));
        }

        private IEnumerator Reveal(CrateOpenResult result)
        {
            // The animation starts only after the result exists (server result in M2).
            resultPanel.SetActive(false);
            cardsRoot.SetActive(false);
            revealPanel.SetActive(true);
            skipRequested = false;
            yield return stage.OpenCrate(Best(result), () => skipRequested);
            revealPanel.SetActive(false);
            ShowResult(result);
            busy = false;
        }

        private static Rarity Best(CrateOpenResult result)
        {
            Rarity top = Rarity.Common;
            foreach (CrateDrop drop in result.Drops)
            {
                if (drop.Item.Rarity > top) top = drop.Item.Rarity;
            }

            return top;
        }

        private void ShowResult(CrateOpenResult result)
        {
            resultPanel.SetActive(true);
            equipCandidate = null;
            int shown = Mathf.Min(result.Drops.Count, resultTiles.Length);
            for (int i = 0; i < resultTiles.Length; i++) resultTiles[i].gameObject.SetActive(i < shown);
            for (int i = 0; i < shown; i++)
            {
                CrateDrop drop = result.Drops[i];
                SkinDefinition skin = services.Catalog.Find(drop.Item.Id);
                string note = drop.IsNew ? null : Loc.Format("crates.duplicate", drop.Shards);
                resultTiles[i].Bind(skin, true, false, drop.IsNew, false, null, note);
                resultTiles[i].PlayReveal(i * 0.08f);
                if (drop.IsNew && (equipCandidate == null || skin.Rarity > equipCandidate.Rarity)) equipCandidate = skin;
            }

            Rarity best = Best(result);
            string label = RarityStyle.Tagged(best, RarityStyle.Label(best));
            resultTitle.text = shown == 1 ? label : Loc.Format("crates.result_best", label);
            equipButton.gameObject.SetActive(equipCandidate != null);
            if (equipCandidate != null) equipLabel.text = Loc.Format("crates.equip", RarityStyle.ItemName(equipCandidate)).ToUpper(Loc.Culture);
            UIFeedback.Confirm();
        }

        private async void EquipCandidate()
        {
            if (equipCandidate == null) return;
            if (await services.Economy.EquipAsync(equipCandidate.Id)) Dialogs.Instance.Toast(Loc.Format("inventory.equipped_toast", RarityStyle.ItemName(equipCandidate)));
            equipButton.gameObject.SetActive(false);
        }

        private void CloseResult()
        {
            resultPanel.SetActive(false);
            cardsRoot.SetActive(true);
            stage.ShowCharacter();
            Refresh();
        }
    }
}
