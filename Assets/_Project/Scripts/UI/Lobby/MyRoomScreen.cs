using System.Collections.Generic;
using BilliardGame.Content;
using BilliardGame.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BilliardGame.UI
{
    /// <summary>
    /// My Room (docs/UI_UX.md §4.1): shows the 3D room; edit mode picks table, wall, floor and the three posters from
    /// owned items only, previews live and saves the layout to the profile.
    /// </summary>
    public sealed class MyRoomScreen : ScreenView
    {
        private static readonly ItemType[] TabTypes = { ItemType.Table, ItemType.RoomWall, ItemType.RoomFloor, ItemType.Sticker };
        private const string DefaultWall = "wall_plain";
        private const string DefaultFloor = "floor_wood";

        [SerializeField] private GameServices services;
        [SerializeField] private RoomStage room;
        [SerializeField] private LobbyStage lobbyStage;
        [SerializeField] private TMP_Text trophiesLabel;
        [SerializeField] private Button editButton;
        [SerializeField] private GameObject editPanel;
        [SerializeField] private TabBar editTabs;
        [SerializeField] private ItemTile tileTemplate;
        [SerializeField] private GameObject slotRow;
        [SerializeField] private Button[] posterSlots = new Button[0];
        [SerializeField] private Button saveButton;
        [SerializeField] private Button cancelButton;

        private readonly List<ItemTile> tiles = new List<ItemTile>();
        private RoomLayout working;
        private int posterSlot;

        protected override void Awake()
        {
            base.Awake();
            editButton.onClick.AddListener(BeginEdit);
            saveButton.onClick.AddListener(Save);
            cancelButton.onClick.AddListener(CancelEdit);
            editTabs.Changed += _ => RebuildTiles();
            for (int i = 0; i < posterSlots.Length; i++)
            {
                int slot = i;
                posterSlots[i].onClick.AddListener(() => SelectSlot(slot));
            }
        }

        protected override void OnShown()
        {
            lobbyStage.Camera.enabled = false;
            lobbyStage.gameObject.SetActive(false);
            room.Show(true);
            editPanel.SetActive(false);
            editButton.gameObject.SetActive(true);
            working = Saved();
            Draw();
        }

        protected override void OnHidden()
        {
            room.Show(false);
            lobbyStage.gameObject.SetActive(true);
            lobbyStage.Camera.enabled = true;
        }

        public override bool OnBack()
        {
            if (!editPanel.activeSelf) return base.OnBack();
            CancelEdit();
            return true;
        }

        private RoomLayout Saved()
        {
            string table = services.Economy.EquippedId(ItemType.Table);
            return services.Profile.State.room.Sanitize(services.Economy.Owns, table, DefaultWall, DefaultFloor);
        }

        private void Draw()
        {
            int trophies = 0;
            foreach (AchievementDef def in Achievements.All)
            {
                if (def.IsDone(services.Profile.State)) trophies++;
            }

            room.Apply(working, services.Catalog, services.Economy.Owns, Mathf.Min(trophies, room.TrophySlots));
            trophiesLabel.text = Loc.Format("room.trophies", trophies);
        }

        private void BeginEdit()
        {
            editPanel.SetActive(true);
            editButton.gameObject.SetActive(false);
            editTabs.Select(0, false);
            posterSlot = 0;
            RebuildTiles();
        }

        private void CancelEdit()
        {
            working = Saved();
            Draw();
            editPanel.SetActive(false);
            editButton.gameObject.SetActive(true);
        }

        private void Save()
        {
            services.Profile.SetRoom(working);
            editPanel.SetActive(false);
            editButton.gameObject.SetActive(true);
            Dialogs.Instance.Toast(Loc.Get("room.saved"));
            UIFeedback.Confirm();
        }

        private void SelectSlot(int slot)
        {
            posterSlot = slot;
            RebuildTiles();
        }

        private void RebuildTiles()
        {
            ItemType type = TabTypes[Mathf.Clamp(editTabs.Selected, 0, TabTypes.Length - 1)];
            bool decor = type == ItemType.Sticker;
            slotRow.SetActive(decor);
            for (int i = 0; i < posterSlots.Length; i++) posterSlots[i].GetComponent<UIButtonFx>().SetSelected(i == posterSlot);

            var items = new List<SkinDefinition>();
            foreach (SkinDefinition skin in services.Catalog.Skins)
            {
                if (skin.Type == type && services.Economy.Owns(skin.Id)) items.Add(skin);
            }

            tileTemplate.gameObject.SetActive(false);
            while (tiles.Count < items.Count) tiles.Add(Instantiate(tileTemplate, tileTemplate.transform.parent));
            for (int i = 0; i < tiles.Count; i++)
            {
                bool used = i < items.Count;
                tiles[i].gameObject.SetActive(used);
                if (used) tiles[i].Bind(items[i], true, IsUsed(items[i]), false, false, Pick);
            }
        }

        private bool IsUsed(SkinDefinition item)
        {
            switch (item.Type)
            {
                case ItemType.Table: return working.tableId == item.Id;
                case ItemType.RoomWall: return working.wallId == item.Id;
                case ItemType.RoomFloor: return working.floorId == item.Id;
                default: return working.posters.Count > posterSlot && working.posters[posterSlot] == item.Id;
            }
        }

        private void Pick(ItemTile tile)
        {
            SkinDefinition item = tile.Item;
            switch (item.Type)
            {
                case ItemType.Table:
                    working.tableId = item.Id;
                    break;
                case ItemType.RoomWall:
                    working.wallId = item.Id;
                    break;
                case ItemType.RoomFloor:
                    working.floorId = item.Id;
                    break;
                default:
                    while (working.posters.Count < RoomLayout.PosterSlots) working.posters.Add(null);
                    working.posters[posterSlot] = working.posters[posterSlot] == item.Id ? null : item.Id;
                    break;
            }

            Draw();
            RebuildTiles();
        }
    }
}
