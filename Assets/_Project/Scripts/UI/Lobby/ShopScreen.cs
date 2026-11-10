using BilliardGame.Content;
using BilliardGame.Meta;
using TMPro;
using UnityEngine;

namespace BilliardGame.UI
{
    /// <summary>
    /// Daily rotating shop (docs/ECONOMY.md §4.3, UI_UX §7): six offers drawn by the server for this player each day at
    /// 00:00 Tashkent, items not owned yet, one purchase per slot. Prices and items come from the server.
    /// </summary>
    public sealed class ShopScreen : LiveOpsScreen
    {
        [SerializeField] private OfferView[] offers = new OfferView[0];
        [SerializeField] private TMP_Text resetLabel;
        [SerializeField] private CurrencyPill coins;
        [SerializeField] private CurrencyPill gems;
        [SerializeField] private GameObject noneLabel;

        protected override void Render()
        {
            coins.Set(services.Economy.Coins, false);
            gems.Set(services.Economy.Gems, false);
            ShopOfferInfo[] list = LiveOps.State.shop.offers;
            int shown = 0;
            for (int i = 0; i < offers.Length; i++)
            {
                ShopOfferInfo offer = i < list.Length ? list[i] : null;
                SkinDefinition item = offer != null ? services.Catalog.Find(offer.id) : null;
                offers[i].gameObject.SetActive(item != null);   // an item this build does not know yet stays hidden
                if (item == null) continue;
                shown++;
                OfferView view = offers[i];
                offers[i].Bind(offer, item, () => Buy(offer, item, view));
            }

            noneLabel.SetActive(shown == 0);
        }

        protected override void Tick()
        {
            resetLabel.text = Loc.Format("shop.daily_reset", LiveOpsText.Countdown(LiveOps.Remaining(LiveOps.State.shop.resetInMs)));
        }

        private void Buy(ShopOfferInfo offer, SkinDefinition item, OfferView view)
        {
            string name = RarityStyle.ItemName(item);
            Dialogs.Instance.Confirm("shop.buy_title", Loc.Format("shop.buy_body", name), "common.confirm",
                () => Run(LiveOps.BuyOfferAsync(offer.slot), Loc.Format("shop.bought", name)), view.CurrencySprite(offer), offer.price);
        }
    }
}
