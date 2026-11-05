using System.Collections.Generic;
using BilliardGame.Content;
using BilliardGame.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BilliardGame.UI
{
    /// <summary>
    /// Inventory (docs/UI_UX.md §9): nine category tabs, ItemTile grid with sort and "owned only" filter, and a detail
    /// panel with a live 3D preview (the lobby stage camera renders into a texture), rarity, description, source and
    /// Equip / sticker slots / Mythic upgrade.
    /// </summary>
    public sealed class InventoryScreen : ScreenView
    {
        public static readonly ItemType[] Categories =
        {
            ItemType.Cue, ItemType.Table, ItemType.Balls, ItemType.VenueTheme, ItemType.Outfit, ItemType.Frame, ItemType.Emote, ItemType.Sticker, ItemType.Effect,
        };

        private const string SeenKey = "seen_items";
        private static HashSet<string> seen;

        [SerializeField] private GameServices services;
        [SerializeField] private LobbyStage stage;
        [SerializeField] private ScreenView crates;
        [SerializeField] private TabBar categories;
        [SerializeField] private TMP_Dropdown sort;
        [SerializeField] private ToggleSwitch ownedOnly;
        [SerializeField] private TMP_Text shards;
        [SerializeField] private ItemTile tileTemplate;
        [SerializeField] private EmptyStateView empty;

        [Header("Detail")]
        [SerializeField] private GameObject detail;
        [SerializeField] private RawImage preview;
        [SerializeField] private Image previewIcon;
        [SerializeField] private TMP_Text previewHint;
        [SerializeField] private TMP_Text itemName;
        [SerializeField] private TMP_Text rarityLabel;
        [SerializeField] private Image rarityBar;
        [SerializeField] private TMP_Text description;
        [SerializeField] private TMP_Text source;
        [SerializeField] private Button equipButton;
        [SerializeField] private TMP_Text equipLabel;
        [SerializeField] private GameObject stickerRow;
        [SerializeField] private Button stickerCue;
        [SerializeField] private Button stickerTable;
        [SerializeField] private Button stickerRemove;
        [SerializeField] private Button upgradeButton;

        private readonly List<ItemTile> tiles = new List<ItemTile>();
        private RenderTexture previewTexture;
        private SkinDefinition selected;

        protected override void Awake()
        {
            base.Awake();
            categories.Changed += _ => SelectCategory();
            sort.onValueChanged.AddListener(_ => Rebuild());
            ownedOnly.Changed += _ => Rebuild();
            equipButton.onClick.AddListener(Equip);
            stickerCue.onClick.AddListener(() => EquipSticker(StickerSlot.Cue));
            stickerTable.onClick.AddListener(() => EquipSticker(StickerSlot.Table));
            stickerRemove.onClick.AddListener(RemoveSticker);
            upgradeButton.onClick.AddListener(() => Dialogs.Instance.Toast(Loc.Get("common.soon_toast")));
        }

        /// <summary>Owned items the player has not opened in the inventory yet (lobby badge).</summary>
        public static int NewCount(GameServices services)
        {
            int count = 0;
            foreach (SkinDefinition skin in services.Catalog.Skins)
            {
                if (IsNew(services, skin)) count++;
            }

            return count;
        }

        private static bool IsNew(GameServices services, SkinDefinition skin)
        {
            if (!services.Economy.Owns(skin.Id)) return false;
            if (seen == null || !PlayerPrefs.HasKey(SeenKey)) LoadSeen(services);
            return !seen.Contains(skin.Id);
        }

        // First run: everything already owned (starter kit) counts as seen, so only later drops show "NEW".
        private static void LoadSeen(GameServices services)
        {
            seen = new HashSet<string>();
            if (!PlayerPrefs.HasKey(SeenKey))
            {
                foreach (SkinDefinition skin in services.Catalog.Skins)
                {
                    if (services.Economy.Owns(skin.Id)) seen.Add(skin.Id);
                }

                SaveSeen();
                return;
            }

            foreach (string id in PlayerPrefs.GetString(SeenKey).Split('|'))
            {
                if (id.Length > 0) seen.Add(id);
            }
        }

        private static void MarkSeen(string id)
        {
            if (seen.Add(id)) SaveSeen();
        }

        private void LocalizeSort()
        {
            int value = sort.value;
            sort.ClearOptions();
            sort.AddOptions(new List<string> { Loc.Get("inventory.sort_rarity"), Loc.Get("inventory.sort_new"), Loc.Get("inventory.sort_name") });
            sort.SetValueWithoutNotify(value);
            sort.RefreshShownValue();
        }

        private static void SaveSeen() => PlayerPrefs.SetString(SeenKey, string.Join("|", seen));

        protected override void OnShown()
        {
            LocalizeSort();
            services.Economy.Changed += Rebuild;
            Loc.Changed += Rebuild;
            previewTexture = new RenderTexture(1024, 720, 24) { name = "Inventory Preview", antiAliasing = 2 };
            preview.texture = previewTexture;
            stage.Camera.targetTexture = previewTexture;
            stage.DragFilter = InPreview;
            if (categories.Selected < 0) categories.Select(0, false);
            SelectCategory();
        }

        protected override void OnHidden()
        {
            services.Economy.Changed -= Rebuild;
            Loc.Changed -= Rebuild;
            stage.Camera.targetTexture = null;
            stage.DragFilter = null;
            stage.ShowCharacter();
            if (previewTexture != null)
            {
                previewTexture.Release();
                Destroy(previewTexture);
                previewTexture = null;
            }
        }

        private bool InPreview(Vector2 screenPoint) => RectTransformUtility.RectangleContainsScreenPoint(preview.rectTransform, screenPoint, null);

        private ItemType Category => Categories[Mathf.Clamp(categories.Selected, 0, Categories.Length - 1)];

        private void SelectCategory()
        {
            selected = services.Equipped(Category);
            Rebuild();
        }

        private void Rebuild()
        {
            if (!IsOpen) return;
            IEconomyService economy = services.Economy;
            shards.text = Loc.Format("inventory.shards", Loc.Number(economy.Shards), Loc.Number(economy.Crystals));

            var items = new List<SkinDefinition>();
            foreach (SkinDefinition skin in services.Catalog.Skins)
            {
                if (skin.Type == Category && (!ownedOnly.IsOn || economy.Owns(skin.Id))) items.Add(skin);
            }

            items.Sort(Compare);
            if (selected != null && !items.Contains(selected)) selected = null;
            if (selected == null && items.Count > 0) selected = items[0];

            tileTemplate.gameObject.SetActive(false);
            while (tiles.Count < items.Count) tiles.Add(Instantiate(tileTemplate, tileTemplate.transform.parent));
            string equipped = Category == ItemType.Sticker ? null : economy.EquippedId(Category);
            for (int i = 0; i < tiles.Count; i++)
            {
                bool used = i < items.Count;
                tiles[i].gameObject.SetActive(used);
                if (!used) continue;
                SkinDefinition skin = items[i];
                bool isEquipped = skin.Id == equipped || IsStickerUsed(skin.Id);
                tiles[i].Bind(skin, economy.Owns(skin.Id), isEquipped, IsNew(services, skin), skin == selected, Select);
            }

            if (items.Count == 0)
            {
                empty.Show(null, "inventory.empty", "inventory.empty_hint", "inventory.go_crates", GoToCrates);
            }
            else
            {
                empty.Hide();
            }

            ShowDetail();
        }

        /// <summary>Sort: 0 = rarity (high first), 1 = new first, 2 = name. Owned items always lead.</summary>
        private int Compare(SkinDefinition a, SkinDefinition b)
        {
            IEconomyService economy = services.Economy;
            int owned = economy.Owns(b.Id).CompareTo(economy.Owns(a.Id));
            if (owned != 0) return owned;
            switch (sort.value)
            {
                case 1:
                    int fresh = IsNew(services, b).CompareTo(IsNew(services, a));
                    return fresh != 0 ? fresh : b.Rarity.CompareTo(a.Rarity);
                case 2:
                    return string.Compare(RarityStyle.ItemName(a), RarityStyle.ItemName(b), System.StringComparison.CurrentCultureIgnoreCase);
                default:
                    int rarity = b.Rarity.CompareTo(a.Rarity);
                    return rarity != 0 ? rarity : string.CompareOrdinal(a.Id, b.Id);
            }
        }

        private bool IsStickerUsed(string id) =>
            id == services.Economy.EquippedSticker(StickerSlot.Cue) || id == services.Economy.EquippedSticker(StickerSlot.Table);

        private void Select(ItemTile tile)
        {
            selected = tile.Item;
            if (services.Economy.Owns(selected.Id)) MarkSeen(selected.Id);
            Rebuild();
        }

        private void ShowDetail()
        {
            detail.SetActive(selected != null);
            if (selected == null)
            {
                stage.ShowCharacter();
                return;
            }

            IEconomyService economy = services.Economy;
            bool owned = economy.Owns(selected.Id);
            Color color = RarityStyle.Color(selected.Rarity);
            itemName.text = RarityStyle.ItemName(selected);
            rarityLabel.text = RarityStyle.Label(selected.Rarity).ToUpper(Loc.Culture);
            rarityLabel.color = color;
            rarityBar.color = color;
            description.text = Loc.Get(DescriptionKey(selected.Type)) + "\n<size=80%><color=#A7B0C4>" + Loc.Get("inventory.fair") + "</color></size>";
            source.text = owned ? Loc.Get("inventory.owned") : Loc.Format("inventory.from", Loc.Get(selected.UnlockSource, selected.UnlockSource));

            bool model = selected.Type == ItemType.Cue || selected.Type == ItemType.Table;
            preview.gameObject.SetActive(model);
            previewHint.gameObject.SetActive(model);
            previewIcon.gameObject.SetActive(!model);
            previewIcon.sprite = selected.Icon;
            if (model) stage.Preview(selected);

            bool sticker = selected.Type == ItemType.Sticker;
            stickerRow.SetActive(sticker && owned);
            equipButton.gameObject.SetActive(!sticker);
            stickerRemove.gameObject.SetActive(sticker && owned && IsStickerUsed(selected.Id));
            upgradeButton.gameObject.SetActive(owned && selected.Rarity == Rarity.Mythic);
            bool isEquipped = !sticker && economy.EquippedId(selected.Type) == selected.Id;
            equipButton.interactable = owned && !isEquipped;
            equipLabel.text = Loc.Get(isEquipped ? "inventory.equipped" : owned ? "inventory.equip" : "common.locked").ToUpper(Loc.Culture);
        }

        private static string DescriptionKey(ItemType type)
        {
            switch (type)
            {
                case ItemType.Cue: return "type.cue.desc";
                case ItemType.Table: return "type.table.desc";
                case ItemType.Sticker: return "type.sticker.desc";
                case ItemType.Frame: return "type.frame.desc";
                case ItemType.RoomWall: return "type.roomwall.desc";
                case ItemType.RoomFloor: return "type.roomfloor.desc";
                default: return "type.other.desc";
            }
        }

        private async void Equip()
        {
            if (selected == null) return;
            if (await services.Economy.EquipAsync(selected.Id))
            {
                UIFeedback.Confirm();
                Dialogs.Instance.Toast(Loc.Format("inventory.equipped_toast", RarityStyle.ItemName(selected)));
            }
        }

        private async void EquipSticker(StickerSlot slot)
        {
            if (selected == null) return;
            if (await services.Economy.EquipStickerAsync(selected.Id, slot))
            {
                UIFeedback.Confirm();
                Dialogs.Instance.Toast(Loc.Format("inventory.sticker_toast", RarityStyle.ItemName(selected)));
            }
        }

        private async void RemoveSticker()
        {
            if (selected == null) return;
            if (services.Economy.EquippedSticker(StickerSlot.Cue) == selected.Id) await services.Economy.EquipStickerAsync(null, StickerSlot.Cue);
            if (services.Economy.EquippedSticker(StickerSlot.Table) == selected.Id) await services.Economy.EquipStickerAsync(null, StickerSlot.Table);
        }

        private void GoToCrates()
        {
            ScreenRouter.Instance.Close(this);
            ScreenRouter.Instance.Open(crates);
        }
    }
}
