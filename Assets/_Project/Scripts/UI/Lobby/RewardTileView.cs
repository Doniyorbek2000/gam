using BilliardGame.Content;
using BilliardGame.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BilliardGame.UI
{
    /// <summary>One Season Pass reward: icon (currency or item), amount, rarity frame for items, lock / claimed / ready marks.</summary>
    public sealed class RewardTileView : MonoBehaviour
    {
        [SerializeField] private Image frame;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text amount;
        [SerializeField] private GameObject lockMark;
        [SerializeField] private GameObject claimedMark;
        [SerializeField] private Image glow;
        [SerializeField] private Button button;
        [SerializeField] private Sprite coinSprite;
        [SerializeField] private Sprite gemSprite;
        [SerializeField] private Sprite shardSprite;
        [SerializeField] private Sprite crystalSprite;
        [SerializeField] private Sprite keySprite;
        [SerializeField] private Color plainFrame = new Color(1f, 1f, 1f, 0.12f);

        public Button Button => button;

        /// <summary>Shows the first reward large and the rest in the amount line; empty rewards hide the tile.</summary>
        public void Bind(RewardInfo[] rewards, SkinCatalog catalog, bool locked, bool claimed, bool claimable)
        {
            bool any = rewards != null && rewards.Length > 0;
            gameObject.SetActive(any);
            if (!any) return;

            RewardInfo first = rewards[0];
            SkinDefinition item = first.IsItem ? catalog.Find(first.id) : null;
            icon.sprite = item != null ? item.Icon : Sprite(first.kind);
            icon.color = claimed ? new Color(1f, 1f, 1f, 0.45f) : Color.white;
            frame.color = item != null ? RarityStyle.Color(item.Rarity) : plainFrame;
            var text = new System.Text.StringBuilder(first.IsItem ? string.Empty : Loc.Number(first.amount));
            for (int i = 1; i < rewards.Length; i++) text.Append(text.Length > 0 ? " +" : "+").Append(rewards[i].IsItem ? "1" : Loc.Number(rewards[i].amount));
            amount.text = text.ToString();
            lockMark.SetActive(locked && !claimed);
            claimedMark.SetActive(claimed);
            glow.gameObject.SetActive(claimable);
        }

        private Sprite Sprite(string kind)
        {
            switch (kind)
            {
                case "coins": return coinSprite;
                case "gems": return gemSprite;
                case "shards": return shardSprite;
                case "crystals": return crystalSprite;
                default: return keySprite;
            }
        }
    }
}
