using BilliardGame.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BilliardGame.UI
{
    /// <summary>One crate on the crate screen: name, pity counters, odds [i], 1× and 10× with price and currency icon.</summary>
    public sealed class CrateCardView : MonoBehaviour
    {
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text pity;
        [SerializeField] private Button infoButton;
        [SerializeField] private Button openOne;
        [SerializeField] private TMP_Text priceOne;
        [SerializeField] private Image currencyOne;
        [SerializeField] private Button openTen;
        [SerializeField] private TMP_Text priceTen;
        [SerializeField] private Image currencyTen;
        [SerializeField] private Sprite coinSprite;
        [SerializeField] private Sprite gemSprite;

        private CrateDefinition crate;

        public void Bind(CrateDefinition definition, CrateScreen screen)
        {
            crate = definition;
            infoButton.onClick.RemoveAllListeners();
            infoButton.onClick.AddListener(() => screen.ShowOdds(crate));
            openOne.onClick.RemoveAllListeners();
            openOne.onClick.AddListener(() => screen.RequestOpen(crate.id, 1));
            openTen.onClick.RemoveAllListeners();
            openTen.onClick.AddListener(() => screen.RequestOpen(crate.id, 10));
        }

        public void Refresh(IEconomyService economy)
        {
            if (crate == null) return;
            title.text = Loc.Format("crates.name", CrateScreen.CrateName(crate)).ToUpper(Loc.Culture);
            var lines = new System.Text.StringBuilder();
            foreach (PityRule rule in crate.pity ?? new PityRule[0])
            {
                lines.AppendLine(Loc.Format("crates.pity", RarityStyle.Tagged(rule.MinRarity, RarityStyle.Name(rule.MinRarity)), economy.PityRemaining(crate.id, rule)));
            }

            pity.text = lines.ToString();
            Sprite currency = crate.Currency == Currency.Gem ? gemSprite : coinSprite;
            currencyOne.sprite = currencyTen.sprite = currency;
            priceOne.text = "1×  " + Loc.Number(crate.Price(1));
            priceTen.text = "10×  " + Loc.Number(crate.Price(10));
            // Buttons stay tappable when short of currency: the tap explains why instead of a silent grey button.
            int balance = crate.Currency == Currency.Gem ? economy.Gems : economy.Coins;
            openOne.GetComponent<CanvasGroup>().alpha = balance >= crate.Price(1) ? 1f : 0.6f;
            openTen.GetComponent<CanvasGroup>().alpha = balance >= crate.Price(10) ? 1f : 0.6f;
        }
    }
}
