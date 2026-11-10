using System;
using BilliardGame.Content;
using BilliardGame.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BilliardGame.UI
{
    /// <summary>One daily shop slot: item tile, featured tag, price button with currency icon, "sold" overlay.</summary>
    public sealed class OfferView : MonoBehaviour
    {
        [SerializeField] private ItemTile tile;
        [SerializeField] private Button buy;
        [SerializeField] private TMP_Text price;
        [SerializeField] private Image currency;
        [SerializeField] private GameObject featured;
        [SerializeField] private GameObject sold;
        [SerializeField] private Sprite coinSprite;
        [SerializeField] private Sprite gemSprite;

        public void Bind(ShopOfferInfo offer, SkinDefinition item, Action onBuy)
        {
            Action tap = offer.bought ? null : onBuy;
            tile.Bind(item, true, false, false, false, tap == null ? (Action<ItemTile>)null : _ => tap());
            currency.sprite = offer.currency == "gem" ? gemSprite : coinSprite;
            price.text = Loc.Number(offer.price);
            featured.SetActive(offer.featured);
            sold.SetActive(offer.bought);
            buy.gameObject.SetActive(!offer.bought);
            buy.onClick.RemoveAllListeners();
            if (tap != null) buy.onClick.AddListener(() => tap());
        }

        public Sprite CurrencySprite(ShopOfferInfo offer) => offer.currency == "gem" ? gemSprite : coinSprite;
    }
}
