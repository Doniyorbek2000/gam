using BilliardGame.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BilliardGame.Editor.Content
{
    /// <summary>Crate screen (UI_UX §5–6), inventory (§9, minimal) and credits, all children of the lobby panel.</summary>
    internal static class LobbyScreensBuilder
    {
        private const int ResultSlots = 10;
        private static readonly Color Dark = new Color(0.1f, 0.07f, 0.02f);

        public static CrateScreen Crates(GameObject host, RectTransform panel, GameObject chrome, GameServices services, LobbyStage stage)
        {
            RectTransform root = LobbyBuilder.Stretch("Crate Screen", panel, new Color(0f, 0f, 0f, 0f));
            Vector2 topLeft = new Vector2(0f, 1f), right = new Vector2(1f, 0.5f), centre = new Vector2(0.5f, 0.5f);
            HudBuilder.Label("Crate Title", root, topLeft, new Vector2(60f, -140f), new Vector2(800f, 80f), TextAlignmentOptions.TopLeft, 56).text = "<b>YASHIKLAR</b>";
            Button close = HudBuilder.Button("Crate Close", "YOPISH", root, LobbyBuilder.Rounded, Vector2.one, new Vector2(-150f, -175f), new Vector2(240f, 80f), LobbyBuilder.Slate);
            LobbyBuilder.Caption(close, 30);

            var cards = new CrateCardView[2];
            for (int i = 0; i < cards.Length; i++) cards[i] = CrateCard(root, i, right, new Vector2(-370f, 120f - i * 410f));

            RectTransform odds = LobbyBuilder.Modal("Odds Panel", root, new Vector2(1320f, 960f));
            TMP_Text oddsTitle = HudBuilder.Label("Odds Title", odds, new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(1200f, 60f), TextAlignmentOptions.Top, 40);
            oddsTitle.fontStyle = FontStyles.Bold;
            TMP_Text oddsText = HudBuilder.Label("Odds Text", odds, new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(1220f, 720f), TextAlignmentOptions.TopLeft, 28);
            oddsText.textWrappingMode = TextWrappingModes.Normal;
            Button oddsClose = HudBuilder.Button("Odds Close", "TUSHUNARLI", odds, LobbyBuilder.Rounded, new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(420f, 90f), LobbyBuilder.Gold);
            LobbyBuilder.Caption(oddsClose, 36).color = Dark;

            RectTransform reveal = LobbyBuilder.Stretch("Reveal Panel", root, null);
            Button skip = HudBuilder.Button("Skip", "O'TKAZISH »", reveal, LobbyBuilder.Rounded, new Vector2(1f, 0f), new Vector2(-200f, 80f), new Vector2(320f, 90f), LobbyBuilder.Slate);
            LobbyBuilder.Caption(skip, 32);

            RectTransform result = LobbyBuilder.Stretch("Result Panel", root, new Color(0.01f, 0.02f, 0.04f, 0.82f));
            TMP_Text resultTitle = HudBuilder.Label("Result Title", result, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(1600f, 80f), TextAlignmentOptions.Top, 50);
            resultTitle.fontStyle = FontStyles.Bold;
            var grid = new GameObject("Result Grid", typeof(RectTransform), typeof(GridLayoutGroup));
            HudBuilder.Place(grid, result, centre, new Vector2(0f, 20f), new Vector2(1640f, 780f));
            Grid(grid.GetComponent<GridLayoutGroup>(), new Vector2(300f, 370f), 5, 20f);
            var resultCards = new ItemCardView[ResultSlots];
            for (int i = 0; i < ResultSlots; i++) resultCards[i] = Card(grid.transform, "Result " + i);

            Button equip = HudBuilder.Button("Result Equip", "JIHOZLASH", result, LobbyBuilder.Rounded, new Vector2(0.5f, 0f), new Vector2(-460f, 80f), new Vector2(560f, 100f), LobbyBuilder.Gold);
            LobbyBuilder.Caption(equip, 32).color = Dark;
            Button again = HudBuilder.Button("Result Again", "YANA OCHISH", result, LobbyBuilder.Rounded, new Vector2(0.5f, 0f), new Vector2(150f, 80f), new Vector2(360f, 100f), LobbyBuilder.Blue);
            LobbyBuilder.Caption(again, 32);
            Button resultClose = HudBuilder.Button("Result Close", "YOPISH", result, LobbyBuilder.Rounded, new Vector2(0.5f, 0f), new Vector2(560f, 80f), new Vector2(300f, 100f), LobbyBuilder.Slate);
            LobbyBuilder.Caption(resultClose, 32);

            odds.parent.gameObject.SetActive(false);
            reveal.gameObject.SetActive(false);
            result.gameObject.SetActive(false);
            root.gameObject.SetActive(false);

            var screen = host.AddComponent<CrateScreen>();
            SceneWiring.Set(screen, "services", services);
            SceneWiring.Set(screen, "stage", stage);
            SceneWiring.Set(screen, "root", root.gameObject);
            SceneWiring.Set(screen, "lobbyChrome", chrome);
            SceneWiring.Set(screen, "closeButton", close);
            SceneWiring.SetArray(screen, "cards", cards);
            SceneWiring.Set(screen, "oddsPanel", odds.parent.gameObject);
            SceneWiring.Set(screen, "oddsTitle", oddsTitle);
            SceneWiring.Set(screen, "oddsText", oddsText);
            SceneWiring.Set(screen, "oddsClose", oddsClose);
            SceneWiring.Set(screen, "revealPanel", reveal.gameObject);
            SceneWiring.Set(screen, "skipButton", skip);
            SceneWiring.Set(screen, "resultPanel", result.gameObject);
            SceneWiring.Set(screen, "resultTitle", resultTitle);
            SceneWiring.SetArray(screen, "resultCards", resultCards);
            SceneWiring.Set(screen, "equipButton", equip);
            SceneWiring.Set(screen, "againButton", again);
            SceneWiring.Set(screen, "resultClose", resultClose);
            return screen;
        }

        public static InventoryScreen Inventory(GameObject host, RectTransform panel, GameObject chrome, GameServices services, LobbyStage stage)
        {
            RectTransform root = LobbyBuilder.Stretch("Inventory Screen", panel, new Color(0f, 0f, 0f, 0f));
            Image list = LobbyBuilder.Image(root, "Item List", Vector2.zero, new Vector2(0f, 1f), new Vector2(30f, 30f), new Vector2(740f, -130f), LobbyBuilder.Glass);
            list.raycastTarget = true;
            RectTransform listRect = list.rectTransform;
            Vector2 top = new Vector2(0.5f, 1f);
            Button cues = HudBuilder.Button("Cues Tab", "KIYLAR", listRect, LobbyBuilder.Rounded, top, new Vector2(-170f, -55f), new Vector2(320f, 80f), LobbyBuilder.Gold);
            Button tables = HudBuilder.Button("Tables Tab", "STOLLAR", listRect, LobbyBuilder.Rounded, top, new Vector2(170f, -55f), new Vector2(320f, 80f), LobbyBuilder.Slate);
            LobbyBuilder.Caption(cues, 32);
            LobbyBuilder.Caption(tables, 32);
            TMP_Text shards = HudBuilder.Label("Shards", listRect, top, new Vector2(0f, -110f), new Vector2(660f, 44f), TextAlignmentOptions.Top, 26);
            shards.color = new Color(0.75f, 0.8f, 0.9f);

            var gridObject = new GameObject("Item Grid", typeof(RectTransform), typeof(GridLayoutGroup));
            RectTransform grid = HudBuilder.Place(gridObject, listRect, top, new Vector2(0f, -160f), new Vector2(680f, 780f));
            grid.pivot = top;
            Grid(gridObject.GetComponent<GridLayoutGroup>(), new Vector2(206f, 236f), 3, 16f);
            gridObject.GetComponent<GridLayoutGroup>().childAlignment = TextAnchor.UpperCenter;
            ItemCardView template = Card(grid, "Card Template");

            Image detail = LobbyBuilder.Image(root, "Detail", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-620f, 30f), new Vector2(-30f, 470f), LobbyBuilder.Glass);
            detail.raycastTarget = true;
            RectTransform detailRect = detail.rectTransform;
            TMP_Text detailName = HudBuilder.Label("Detail Name", detailRect, top, new Vector2(0f, -26f), new Vector2(540f, 60f), TextAlignmentOptions.Top, 42);
            detailName.fontStyle = FontStyles.Bold;
            TMP_Text detailInfo = HudBuilder.Label("Detail Info", detailRect, top, new Vector2(0f, -96f), new Vector2(540f, 200f), TextAlignmentOptions.Top, 28);
            detailInfo.textWrappingMode = TextWrappingModes.Normal;
            Button equip = HudBuilder.Button("Detail Equip", "JIHOZLASH", detailRect, LobbyBuilder.Rounded, new Vector2(0.5f, 0f), new Vector2(0f, 75f), new Vector2(520f, 100f), LobbyBuilder.Gold);
            LobbyBuilder.Caption(equip, 36).color = Dark;
            Button close = HudBuilder.Button("Inventory Close", "YOPISH", root, LobbyBuilder.Rounded, Vector2.one, new Vector2(-150f, -175f), new Vector2(240f, 80f), LobbyBuilder.Slate);
            LobbyBuilder.Caption(close, 30);
            root.gameObject.SetActive(false);

            var screen = host.AddComponent<InventoryScreen>();
            SceneWiring.Set(screen, "services", services);
            SceneWiring.Set(screen, "stage", stage);
            SceneWiring.Set(screen, "root", root.gameObject);
            SceneWiring.Set(screen, "lobbyChrome", chrome);
            SceneWiring.Set(screen, "closeButton", close);
            SceneWiring.Set(screen, "cuesTab", cues);
            SceneWiring.Set(screen, "tablesTab", tables);
            SceneWiring.Set(screen, "shardsLabel", shards);
            SceneWiring.Set(screen, "grid", grid);
            SceneWiring.Set(screen, "cardTemplate", template);
            SceneWiring.Set(screen, "detailName", detailName);
            SceneWiring.Set(screen, "detailInfo", detailInfo);
            SceneWiring.Set(screen, "equipButton", equip);
            return screen;
        }

        public static CreditsScreen Credits(GameObject host, RectTransform panel)
        {
            RectTransform card = LobbyBuilder.Modal("Credits", panel, new Vector2(1400f, 940f));
            var scrollObject = new GameObject("Scroll", typeof(RectTransform), typeof(RectMask2D), typeof(ScrollRect));
            RectTransform viewport = HudBuilder.Place(scrollObject, card, new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(1300f, 760f));
            viewport.pivot = new Vector2(0.5f, 1f);
            TMP_Text body = HudBuilder.Label("Credits Body", viewport, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(1280f, 0f), TextAlignmentOptions.TopLeft, 26);
            body.textWrappingMode = TextWrappingModes.Normal;
            body.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = scrollObject.GetComponent<ScrollRect>();
            scroll.content = body.rectTransform;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scrollObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);   // catches drags across the whole viewport
            Button close = HudBuilder.Button("Credits Close", "YOPISH", card, LobbyBuilder.Rounded, new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(380f, 90f), LobbyBuilder.Slate);
            LobbyBuilder.Caption(close, 32);
            card.parent.gameObject.SetActive(false);

            var screen = host.AddComponent<CreditsScreen>();
            SceneWiring.Set(screen, "root", card.parent.gameObject);
            SceneWiring.Set(screen, "body", body);
            SceneWiring.Set(screen, "closeButton", close);
            return screen;
        }

        private static CrateCardView CrateCard(RectTransform parent, int index, Vector2 anchor, Vector2 position)
        {
            var go = new GameObject("Crate Card " + index, typeof(RectTransform), typeof(Image));
            RectTransform rect = HudBuilder.Place(go, parent, anchor, position, new Vector2(660f, 380f));
            Image background = go.GetComponent<Image>();
            background.sprite = LobbyBuilder.Rounded;
            background.type = Image.Type.Sliced;
            background.color = LobbyBuilder.Glass;
            Vector2 topLeft = new Vector2(0f, 1f);
            TMP_Text title = HudBuilder.Label("Title", rect, topLeft, new Vector2(30f, -24f), new Vector2(520f, 60f), TextAlignmentOptions.TopLeft, 42);
            title.fontStyle = FontStyles.Bold;
            title.color = index == 0 ? new Color(1f, 0.85f, 0.45f) : new Color(1f, 0.55f, 0.95f);
            TMP_Text pity = HudBuilder.Label("Pity", rect, topLeft, new Vector2(30f, -100f), new Vector2(600f, 130f), TextAlignmentOptions.TopLeft, 28);
            pity.textWrappingMode = TextWrappingModes.Normal;
            Button info = HudBuilder.Button("Info", "i", rect, LobbyBuilder.Rounded, Vector2.one, new Vector2(-55f, -55f), new Vector2(70f, 70f), LobbyBuilder.Slate);
            LobbyBuilder.Caption(info, 40);
            Vector2 bottom = new Vector2(0.5f, 0f);
            Button one = HudBuilder.Button("Open 1", "1×", rect, LobbyBuilder.Rounded, bottom, new Vector2(-160f, 70f), new Vector2(300f, 100f), LobbyBuilder.Blue);
            Button ten = HudBuilder.Button("Open 10", "10×", rect, LobbyBuilder.Rounded, bottom, new Vector2(160f, 70f), new Vector2(300f, 100f), LobbyBuilder.Gold);
            LobbyBuilder.Caption(one, 30);
            LobbyBuilder.Caption(ten, 30).color = Dark;

            var view = go.AddComponent<CrateCardView>();
            SceneWiring.Set(view, "title", title);
            SceneWiring.Set(view, "pity", pity);
            SceneWiring.Set(view, "infoButton", info);
            SceneWiring.Set(view, "openOne", one);
            SceneWiring.Set(view, "openTen", ten);
            return view;
        }

        /// <summary>Rarity-framed item card: icon, name, rarity dots, badge, lock overlay and a selection bar.</summary>
        private static ItemCardView Card(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            Image frame = go.GetComponent<Image>();
            frame.sprite = LobbyBuilder.Rounded;
            frame.type = Image.Type.Sliced;
            LobbyBuilder.Image(rect, "Inner", Vector2.zero, Vector2.one, new Vector2(5f, 5f), new Vector2(-5f, -5f), new Color(0.06f, 0.08f, 0.12f));
            Image icon = LobbyBuilder.Image(rect, "Icon", new Vector2(0f, 0.32f), Vector2.one, new Vector2(10f, 0f), new Vector2(-10f, -10f), Color.white);
            icon.preserveAspect = true;
            TMP_Text title = HudBuilder.Label("Name", rect, new Vector2(0.5f, 0f), new Vector2(0f, 48f), new Vector2(280f, 40f), TextAlignmentOptions.Bottom, 24);
            title.rectTransform.anchorMin = new Vector2(0f, 0f);
            title.rectTransform.anchorMax = new Vector2(1f, 0f);
            title.rectTransform.sizeDelta = new Vector2(-16f, 40f);
            title.fontStyle = FontStyles.Bold;
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.overflowMode = TextOverflowModes.Ellipsis;
            TMP_Text rarity = HudBuilder.Label("Rarity", rect, new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(280f, 32f), TextAlignmentOptions.Bottom, 20);
            TMP_Text badge = HudBuilder.Label("Badge", rect, new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(280f, 32f), TextAlignmentOptions.Top, 20);
            badge.color = new Color(1f, 0.9f, 0.5f);
            badge.outlineWidth = 0.25f;
            badge.outlineColor = Color.black;
            Image lockOverlay = LobbyBuilder.Image(rect, "Lock", Vector2.zero, Vector2.one, new Vector2(5f, 5f), new Vector2(-5f, -5f), new Color(0f, 0f, 0f, 0.5f));
            HudBuilder.Label("Lock Text", lockOverlay.rectTransform, new Vector2(0.5f, 0.6f), Vector2.zero, new Vector2(260f, 40f), TextAlignmentOptions.Center, 22).text = "QULFLANGAN";
            Image selected = LobbyBuilder.Image(rect, "Selected", Vector2.zero, new Vector2(1f, 0f), new Vector2(5f, 0f), new Vector2(-5f, 8f), Color.white);
            go.GetComponent<Button>().targetGraphic = frame;

            var view = go.AddComponent<ItemCardView>();
            SceneWiring.Set(view, "frame", frame);
            SceneWiring.Set(view, "icon", icon);
            SceneWiring.Set(view, "title", title);
            SceneWiring.Set(view, "rarity", rarity);
            SceneWiring.Set(view, "badge", badge);
            SceneWiring.Set(view, "lockOverlay", lockOverlay.gameObject);
            SceneWiring.Set(view, "selectedMark", selected.gameObject);
            SceneWiring.Set(view, "button", go.GetComponent<Button>());
            return view;
        }

        private static void Grid(GridLayoutGroup grid, Vector2 cell, int columns, float spacing)
        {
            grid.cellSize = cell;
            grid.spacing = new Vector2(spacing, spacing);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
            grid.childAlignment = TextAnchor.MiddleCenter;
        }
    }
}
