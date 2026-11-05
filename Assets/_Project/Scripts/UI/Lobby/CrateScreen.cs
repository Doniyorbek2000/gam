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
    /// Crates (docs/UI_UX.md §8): one card per crate with price, [i] odds, pity counters and 1×/10×; the reveal plays the
    /// 3D crate on the lobby stage, then shows the result card(s). Odds text is generated from the crate table.
    /// </summary>
    public sealed class CrateScreen : MonoBehaviour
    {
        [SerializeField] private GameServices services;
        [SerializeField] private LobbyStage stage;
        [SerializeField] private LobbyController lobby;
        [SerializeField] private GameObject root;
        [SerializeField] private GameObject lobbyChrome;
        [SerializeField] private Button closeButton;
        [SerializeField] private CrateCardView[] cards = new CrateCardView[0];

        [SerializeField] private GameObject oddsPanel;
        [SerializeField] private TMP_Text oddsTitle;
        [SerializeField] private TMP_Text oddsText;
        [SerializeField] private Button oddsClose;

        [SerializeField] private GameObject revealPanel;
        [SerializeField] private Button skipButton;
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TMP_Text resultTitle;
        [SerializeField] private ItemCardView[] resultCards = new ItemCardView[0];
        [SerializeField] private Button equipButton;
        [SerializeField] private Button againButton;
        [SerializeField] private Button resultClose;

        private bool skipRequested;
        private bool busy;
        private string lastCrate;
        private int lastCount;
        private SkinDefinition equipCandidate;

        public bool IsRevealing => busy;

        private void OnEnable()
        {
            closeButton.onClick.AddListener(Close);
            oddsClose.onClick.AddListener(() => oddsPanel.SetActive(false));
            skipButton.onClick.AddListener(() => skipRequested = true);
            equipButton.onClick.AddListener(EquipCandidate);
            againButton.onClick.AddListener(() => Open(lastCrate, lastCount));
            resultClose.onClick.AddListener(CloseResult);
            services.Economy.Changed += Refresh;
        }

        private void OnDisable()
        {
            foreach (Button b in new[] { closeButton, oddsClose, skipButton, equipButton, againButton, resultClose }) b.onClick.RemoveAllListeners();
            services.Economy.Changed -= Refresh;
        }

        public void Open()
        {
            root.SetActive(true);
            lobbyChrome.SetActive(false);
            oddsPanel.SetActive(false);
            revealPanel.SetActive(false);
            resultPanel.SetActive(false);
            for (int i = 0; i < cards.Length; i++)
            {
                CrateDefinition crate = i < services.Economy.Crates.Count ? services.Economy.Crates[i] : null;
                cards[i].gameObject.SetActive(crate != null);
                if (crate != null) cards[i].Bind(crate, this);
            }

            Refresh();
        }

        public void Close()
        {
            if (busy) return;
            root.SetActive(false);
            lobbyChrome.SetActive(true);
            stage.ShowCharacter();
        }

        private void Refresh()
        {
            foreach (CrateCardView card in cards)
            {
                if (card.gameObject.activeSelf) card.Refresh(services.Economy);
            }
        }

        public void ShowOdds(CrateDefinition crate)
        {
            oddsTitle.text = $"{crate.displayName} yashik — ehtimollar";
            oddsText.text = OddsText(crate, services.Catalog);
            oddsPanel.SetActive(true);
        }

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
                foreach (SkinDefinition skin in items) text.AppendLine($"    {skin.DisplayName} — {weight / items.Count:0.###}%");
            }

            text.AppendLine();
            foreach (PityRule rule in crate.pity ?? new PityRule[0])
            {
                double p = CrateRoller.ChanceAtLeast(crate.weights, rule.MinRarity) / 100.0;
                double effective = CrateRoller.EffectiveChance(p, rule.count) * 100.0;
                text.AppendLine($"Kafolat: {rule.count}-ochishda {RarityStyle.Name(rule.MinRarity)} yoki yuqori. Asosiy {p * 100:0.##}%, pity bilan umumiy {effective:0.##}%.");
            }

            if (crate.HasTenGuarantee(out Rarity ten)) text.AppendLine($"10× ochishda kamida bitta {RarityStyle.Name(ten)} yoki yuqori.");
            text.AppendLine("Raritet ichida har bir item teng ehtimolli. Dublikat → Shard.");
            return text.ToString();
        }

        public async void Open(string crateId, int count)
        {
            if (busy) return;
            busy = true;
            lastCrate = crateId;
            lastCount = count;
            CrateOpenResult result = await services.Economy.OpenCrateAsync(crateId, count);
            if (!result.Success)
            {
                busy = false;
                lobby.ShowToast(result.Error);
                return;
            }

            StartCoroutine(Reveal(result));
        }

        private IEnumerator Reveal(CrateOpenResult result)
        {
            // The animation starts only after the result exists (server result in M2).
            resultPanel.SetActive(false);
            oddsPanel.SetActive(false);
            revealPanel.SetActive(true);
            foreach (CrateCardView card in cards) card.gameObject.SetActive(false);
            skipRequested = false;

            Rarity top = Rarity.Common;
            foreach (CrateDrop drop in result.Drops)
            {
                if (drop.Item.Rarity > top) top = drop.Item.Rarity;
            }

            yield return stage.OpenCrate(top, () => skipRequested);
            revealPanel.SetActive(false);
            ShowResult(result);
            busy = false;
        }

        private void ShowResult(CrateOpenResult result)
        {
            resultPanel.SetActive(true);
            equipCandidate = null;
            int shown = Mathf.Min(result.Drops.Count, resultCards.Length);
            // Single result in the centre slot; 10× fills the 2×5 grid.
            for (int i = 0; i < resultCards.Length; i++) resultCards[i].gameObject.SetActive(false);
            for (int i = 0; i < shown; i++)
            {
                CrateDrop drop = result.Drops[i];
                SkinDefinition skin = services.Catalog.Find(drop.Item.Id);
                string badge = drop.IsNew ? "YANGI" : $"Dublikat → +{drop.Shards} Shard" + (drop.Crystals > 0 ? $" +{drop.Crystals} Kristall" : string.Empty);
                ItemCardView card = resultCards[shown == 1 ? 0 : i];
                card.gameObject.SetActive(true);
                card.Bind(skin, badge, false, false, null);
                if (drop.IsNew && (equipCandidate == null || skin.Rarity > equipCandidate.Rarity)) equipCandidate = skin;
            }

            Rarity best = Rarity.Common;
            foreach (CrateDrop drop in result.Drops)
            {
                if (drop.Item.Rarity > best) best = drop.Item.Rarity;
            }

            resultTitle.text = shown == 1 ? RarityStyle.Tagged(best, RarityStyle.Label(best)) : $"10× natija — eng yaxshisi: {RarityStyle.Tagged(best, RarityStyle.Label(best))}";
            equipButton.gameObject.SetActive(equipCandidate != null);
            if (equipCandidate != null) equipButton.GetComponentInChildren<TMP_Text>().text = $"JIHOZLASH: {equipCandidate.DisplayName}";
        }

        private async void EquipCandidate()
        {
            if (equipCandidate == null) return;
            if (await services.Economy.EquipAsync(equipCandidate.Id)) lobby.ShowToast($"Jihozlandi: {equipCandidate.DisplayName}");
            equipButton.gameObject.SetActive(false);
        }

        private void CloseResult()
        {
            resultPanel.SetActive(false);
            stage.ShowCharacter();
            Open();
        }
    }
}
