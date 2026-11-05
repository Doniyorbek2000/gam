using System.Collections.Generic;
using BilliardGame.Content;
using BilliardGame.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BilliardGame.UI
{
    /// <summary>Inventory (docs/UI_UX.md §9, minimal): Cues / Table tabs, owned and locked items, 3D preview, Equip.</summary>
    public sealed class InventoryScreen : MonoBehaviour
    {
        [SerializeField] private GameServices services;
        [SerializeField] private LobbyStage stage;
        [SerializeField] private GameObject root;
        [SerializeField] private GameObject lobbyChrome;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button cuesTab;
        [SerializeField] private Button tablesTab;
        [SerializeField] private TMP_Text shardsLabel;
        [SerializeField] private RectTransform grid;
        [SerializeField] private ItemCardView cardTemplate;
        [SerializeField] private TMP_Text detailName;
        [SerializeField] private TMP_Text detailInfo;
        [SerializeField] private Button equipButton;

        private readonly List<ItemCardView> cards = new List<ItemCardView>();
        private ItemType tab = ItemType.Cue;
        private SkinDefinition selected;

        private void OnEnable()
        {
            closeButton.onClick.AddListener(Close);
            cuesTab.onClick.AddListener(() => ShowTab(ItemType.Cue));
            tablesTab.onClick.AddListener(() => ShowTab(ItemType.Table));
            equipButton.onClick.AddListener(Equip);
            services.Economy.Changed += Rebuild;
        }

        private void OnDisable()
        {
            foreach (Button b in new[] { closeButton, cuesTab, tablesTab, equipButton }) b.onClick.RemoveAllListeners();
            services.Economy.Changed -= Rebuild;
        }

        public void Open()
        {
            root.SetActive(true);
            lobbyChrome.SetActive(false);
            cardTemplate.gameObject.SetActive(false);
            ShowTab(tab);
        }

        public void Close()
        {
            root.SetActive(false);
            lobbyChrome.SetActive(true);
            stage.ShowCharacter();
        }

        private void ShowTab(ItemType type)
        {
            tab = type;
            selected = services.Equipped(type);
            cuesTab.GetComponent<Image>().color = type == ItemType.Cue ? new Color(1f, 0.69f, 0.13f) : new Color(0.16f, 0.2f, 0.3f);
            tablesTab.GetComponent<Image>().color = type == ItemType.Table ? new Color(1f, 0.69f, 0.13f) : new Color(0.16f, 0.2f, 0.3f);
            Rebuild();
        }

        private void Rebuild()
        {
            if (!root.activeSelf) return;
            IEconomyService economy = services.Economy;
            shardsLabel.text = $"Shard: {economy.Shards:N0}   Kristall: {economy.Crystals:N0}";

            var items = new List<SkinDefinition>();
            foreach (SkinDefinition skin in services.Catalog.Skins)
            {
                if (skin.Type == tab) items.Add(skin);
            }

            // Owned first, then by rarity (highest first).
            items.Sort((a, b) =>
            {
                int owned = economy.Owns(b.Id).CompareTo(economy.Owns(a.Id));
                return owned != 0 ? owned : b.Rarity.CompareTo(a.Rarity);
            });

            while (cards.Count < items.Count)
            {
                ItemCardView card = Instantiate(cardTemplate, grid);
                cards.Add(card);
            }

            string equipped = economy.EquippedId(tab);
            for (int i = 0; i < cards.Count; i++)
            {
                bool used = i < items.Count;
                cards[i].gameObject.SetActive(used);
                if (!used) continue;
                SkinDefinition skin = items[i];
                cards[i].Bind(skin, skin.Id == equipped ? "Jihozlangan" : null, !economy.Owns(skin.Id), skin == selected, Select);
            }

            ShowDetail();
        }

        private void Select(ItemCardView card)
        {
            selected = card.Skin;
            foreach (ItemCardView c in cards) c.SetSelected(c.Skin == selected);
            ShowDetail();
        }

        private void ShowDetail()
        {
            if (selected == null) return;
            IEconomyService economy = services.Economy;
            bool owned = economy.Owns(selected.Id);
            bool isEquipped = economy.EquippedId(selected.Type) == selected.Id;
            detailName.text = RarityStyle.Tagged(selected.Rarity, selected.DisplayName);
            detailInfo.text = $"{RarityStyle.Label(selected.Rarity)}\n{(owned ? "Sizda bor" : "Qayerdan: " + selected.UnlockSource)}\n<size=75%>Faqat ko'rinish: zarba kuchi va aniqlikka ta'sir qilmaydi.</size>";
            equipButton.interactable = owned && !isEquipped;
            equipButton.GetComponentInChildren<TMP_Text>().text = isEquipped ? "JIHOZLANGAN" : owned ? "JIHOZLASH" : "QULFLANGAN";
            stage.Preview(selected);
        }

        private async void Equip()
        {
            if (selected != null) await services.Economy.EquipAsync(selected.Id);
        }
    }
}
