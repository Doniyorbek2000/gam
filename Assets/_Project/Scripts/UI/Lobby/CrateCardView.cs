using BilliardGame.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BilliardGame.UI
{
    /// <summary>One crate on the crate screen: name, prices, pity counters, [i], 1× and 10×.</summary>
    public sealed class CrateCardView : MonoBehaviour
    {
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text pity;
        [SerializeField] private Button infoButton;
        [SerializeField] private Button openOne;
        [SerializeField] private Button openTen;

        private CrateDefinition crate;

        public void Bind(CrateDefinition definition, CrateScreen screen)
        {
            crate = definition;
            title.text = $"{definition.displayName} yashik";
            Caption(openOne, $"1×  {Price(1)}");
            Caption(openTen, $"10×  {Price(10)}");
            infoButton.onClick.RemoveAllListeners();
            infoButton.onClick.AddListener(() => screen.ShowOdds(crate));
            openOne.onClick.RemoveAllListeners();
            openOne.onClick.AddListener(() => screen.Open(crate.id, 1));
            openTen.onClick.RemoveAllListeners();
            openTen.onClick.AddListener(() => screen.Open(crate.id, 10));
        }

        public void Refresh(IEconomyService economy)
        {
            if (crate == null) return;
            var lines = new System.Text.StringBuilder();
            foreach (PityRule rule in crate.pity ?? new PityRule[0])
            {
                lines.AppendLine($"{RarityStyle.Tagged(rule.MinRarity, RarityStyle.Name(rule.MinRarity))}'gacha: {economy.PityRemaining(crate.id, rule)} ta qoldi");
            }

            pity.text = lines.ToString();
            int balance = crate.Currency == Currency.Gem ? economy.Gems : economy.Coins;
            openOne.interactable = balance >= crate.Price(1);
            openTen.interactable = balance >= crate.Price(10);
        }

        private string Price(int count) => $"{crate.Price(count):N0} {(crate.Currency == Currency.Gem ? "Gem" : "Coin")}";

        private static void Caption(Button button, string text) => button.GetComponentInChildren<TMP_Text>().text = text;
    }
}
