using BilliardGame.Content;
using BilliardGame.Online;
using BilliardGame.Presentation;
using BilliardGame.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BilliardGame.Editor.Content
{
    /// <summary>
    /// Lobby canvas (docs/UI_UX.md §2–3): top bar, left/right rails, mode+venue selector, PLAY, the friend-room panel
    /// and status/cancel of <see cref="MainMenuController"/>, plus the crate, inventory and credits screens.
    /// </summary>
    internal static class LobbyBuilder
    {
        internal static readonly Color Gold = new Color(1f, 0.69f, 0.13f);
        internal static readonly Color Blue = new Color(0.2f, 0.45f, 0.75f);
        internal static readonly Color Slate = new Color(0.16f, 0.2f, 0.3f);
        internal static readonly Color Glass = new Color(0.04f, 0.06f, 0.1f, 0.88f);
        private static readonly string[] ModeNames = { "Hot-seat", "Do'st bilan", "Tezkor o'yin" };

        internal static Sprite Rounded => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        public static void Build(GameController game, OnlineMatchController online, HudController hud, GameObject hudRoot,
            GameServices services, VenueLoader venues, LobbyStage stage, Camera matchCamera)
        {
            var canvasObject = new GameObject("Lobby UI");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;
            canvasObject.AddComponent<GraphicRaycaster>();

            RectTransform panel = Stretch("Lobby Panel", canvasObject.transform, null);
            panel.gameObject.AddComponent<SafeAreaFitter>();
            Vector2 topLeft = new Vector2(0f, 1f), topRight = Vector2.one, left = new Vector2(0f, 0.5f), right = new Vector2(1f, 0.5f), bottomRight = new Vector2(1f, 0f), centre = new Vector2(0.5f, 0.5f);

            // Top bar: nickname, balances with mock "+" top-ups, settings (credits live there).
            Image(panel, "Top Bar", new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -110f), Vector2.zero, new Color(0f, 0f, 0f, 0.45f));
            TMP_InputField nickname = Input("Nickname", panel, topLeft, new Vector2(250f, -55f), new Vector2(420f, 80f), "Ism");
            TMP_Text coins = Balance(panel, "Coins", new Vector2(-780f, -55f), new Color(1f, 0.82f, 0.3f), "Coin", out Button addCoins);
            TMP_Text gems = Balance(panel, "Gems", new Vector2(-440f, -55f), new Color(0.45f, 0.85f, 1f), "Gem", out Button addGems);
            Button settings = HudBuilder.Button("Settings", "SOZLAMALAR", panel, Rounded, topRight, new Vector2(-150f, -55f), new Vector2(240f, 80f), Slate);
            Caption(settings, 30);

            RectTransform mainButtons = Stretch("Main Buttons", panel, null);
            Button pass = Rail(mainButtons, "Pass", "MAVSUM\nPASS", left, new Vector2(140f, 110f), Slate);
            Button tasks = Rail(mainButtons, "Tasks", "VAZIFALAR", left, new Vector2(140f, -50f), Slate);
            Button shop = Rail(mainButtons, "Shop", "DO'KON", right, new Vector2(-140f, 190f), Blue);
            Button crates = Rail(mainButtons, "Crates", "YASHIKLAR", right, new Vector2(-140f, 30f), Gold);
            Button inventory = Rail(mainButtons, "Inventory", "INVENTAR", right, new Vector2(-140f, -130f), Slate);

            Button selector = HudBuilder.Button("Selector", "8-Ball", mainButtons, Rounded, bottomRight, new Vector2(-330f, 250f), new Vector2(560f, 120f), Glass);
            TMP_Text selectorLabel = Caption(selector, 36);
            selectorLabel.fontStyle = FontStyles.Normal;
            Button play = HudBuilder.Button("Play", "O'YNASH", mainButtons, Rounded, bottomRight, new Vector2(-330f, 105f), new Vector2(560f, 150f), Gold);
            Caption(play, 64).color = new Color(0.1f, 0.07f, 0.02f);

            RectTransform friendPanel = Modal("Friend Panel", panel, new Vector2(760f, 520f));
            HudBuilder.Label("Friend Title", friendPanel, new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(700f, 60f), TextAlignmentOptions.Top, 44).text = "Do'st bilan o'ynash";
            Button create = HudBuilder.Button("Create Room", "XONA OCHISH", friendPanel, Rounded, centre, new Vector2(0f, 90f), new Vector2(640f, 110f), Gold);
            TMP_InputField code = Input("Room Code", friendPanel, centre, new Vector2(-120f, -40f), new Vector2(400f, 110f), "6 raqamli kod");
            Button join = HudBuilder.Button("Join Room", "KIRISH", friendPanel, Rounded, centre, new Vector2(210f, -40f), new Vector2(220f, 110f), Blue);
            Button back = HudBuilder.Button("Back", "ORQAGA", friendPanel, Rounded, centre, new Vector2(0f, -170f), new Vector2(640f, 100f), Slate);
            friendPanel.parent.gameObject.SetActive(false);

            TMP_Text status = HudBuilder.Label("Menu Status", panel, new Vector2(0.5f, 0f), new Vector2(0f, 330f), new Vector2(1100f, 130f), TextAlignmentOptions.Bottom, 38);
            status.text = string.Empty;
            status.textWrappingMode = TextWrappingModes.Normal;
            Button cancel = HudBuilder.Button("Cancel", "BEKOR QILISH", panel, Rounded, new Vector2(0.5f, 0f), new Vector2(0f, 230f), new Vector2(380f, 90f), Slate);
            cancel.gameObject.SetActive(false);

            RectTransform sheet = Modal("Mode Sheet", panel, new Vector2(1180f, 640f));
            HudBuilder.Label("Mode Title", sheet, new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(1000f, 60f), TextAlignmentOptions.Top, 40).text = "8-Ball — rejim";
            var modeButtons = new Button[ModeNames.Length];
            for (int i = 0; i < ModeNames.Length; i++)
            {
                modeButtons[i] = HudBuilder.Button("Mode " + i, ModeNames[i], sheet, Rounded, centre, new Vector2((i - 1) * 370f, 150f), new Vector2(350f, 100f), Slate);
                Caption(modeButtons[i], 36);
            }

            HudBuilder.Label("Venue Title", sheet, centre, new Vector2(0f, 60f), new Vector2(1000f, 60f), TextAlignmentOptions.Center, 40).text = "Zal";
            var venueButtons = new Button[services.Catalog.Venues.Count];
            for (int i = 0; i < venueButtons.Length; i++)
            {
                float x = (i - (venueButtons.Length - 1) * 0.5f) * 540f;
                venueButtons[i] = HudBuilder.Button("Venue " + i, services.Catalog.Venues[i].DisplayName, sheet, Rounded, centre, new Vector2(x, -50f), new Vector2(520f, 110f), Slate);
                Caption(venueButtons[i], 36);
            }

            HudBuilder.Label("Fair Play", sheet, centre, new Vector2(0f, -140f), new Vector2(1100f, 50f), TextAlignmentOptions.Center, 26).text =
                "Zal va skinlar faqat ko'rinish: fizika, qoidalar va taymer hamma uchun bir xil.";
            Button sheetDone = HudBuilder.Button("Sheet Done", "TAYYOR", sheet, Rounded, new Vector2(0.5f, 0f), new Vector2(0f, 80f), new Vector2(420f, 100f), Gold);
            Caption(sheetDone, 40).color = new Color(0.1f, 0.07f, 0.02f);

            TMP_Text toast = HudBuilder.Label("Toast", panel, new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(1200f, 70f), TextAlignmentOptions.Center, 36);
            toast.color = new Color(1f, 0.9f, 0.6f);
            toast.outlineWidth = 0.2f;
            toast.outlineColor = Color.black;

            CrateScreen crateScreen = LobbyScreensBuilder.Crates(canvasObject, panel, mainButtons.gameObject, services, stage);
            InventoryScreen inventoryScreen = LobbyScreensBuilder.Inventory(canvasObject, panel, mainButtons.gameObject, services, stage);
            CreditsScreen credits = LobbyScreensBuilder.Credits(canvasObject, panel);
            toast.transform.SetAsLastSibling();

            var menu = canvasObject.AddComponent<MainMenuController>();
            SceneWiring.Set(menu, "game", game);
            SceneWiring.Set(menu, "online", online);
            SceneWiring.Set(menu, "hud", hud);
            SceneWiring.Set(menu, "menuRoot", panel.gameObject);
            SceneWiring.Set(menu, "hudRoot", hudRoot);
            SceneWiring.Set(menu, "mainButtons", mainButtons.gameObject);
            SceneWiring.Set(menu, "friendPanel", friendPanel.parent.gameObject);
            SceneWiring.Set(menu, "nicknameInput", nickname);
            SceneWiring.Set(menu, "codeInput", code);
            SceneWiring.SetArray(menu, "lockWhileWaiting", new Object[] { play, selector, shop, crates, inventory });
            SceneWiring.Set(menu, "createRoomButton", create);
            SceneWiring.Set(menu, "joinRoomButton", join);
            SceneWiring.Set(menu, "backButton", back);
            SceneWiring.Set(menu, "cancelButton", cancel);
            SceneWiring.Set(menu, "statusLabel", status);

            var lobby = canvasObject.AddComponent<LobbyController>();
            SceneWiring.Set(lobby, "services", services);
            SceneWiring.Set(lobby, "menu", menu);
            SceneWiring.Set(lobby, "online", online);
            SceneWiring.Set(lobby, "game", game);
            SceneWiring.Set(lobby, "venues", venues);
            SceneWiring.Set(lobby, "stage", stage);
            SceneWiring.Set(lobby, "matchCamera", matchCamera);
            SceneWiring.Set(lobby, "crates", crateScreen);
            SceneWiring.Set(lobby, "inventory", inventoryScreen);
            SceneWiring.Set(lobby, "credits", credits);
            SceneWiring.Set(lobby, "coinsLabel", coins);
            SceneWiring.Set(lobby, "gemsLabel", gems);
            SceneWiring.Set(lobby, "addCoins", addCoins);
            SceneWiring.Set(lobby, "addGems", addGems);
            SceneWiring.Set(lobby, "settingsButton", settings);
            SceneWiring.Set(lobby, "passButton", pass);
            SceneWiring.Set(lobby, "tasksButton", tasks);
            SceneWiring.Set(lobby, "shopButton", shop);
            SceneWiring.Set(lobby, "crateButton", crates);
            SceneWiring.Set(lobby, "inventoryButton", inventory);
            SceneWiring.Set(lobby, "selectorButton", selector);
            SceneWiring.Set(lobby, "selectorLabel", selectorLabel);
            SceneWiring.Set(lobby, "playButton", play);
            SceneWiring.Set(lobby, "sheet", sheet.parent.gameObject);
            SceneWiring.SetArray(lobby, "modeButtons", modeButtons);
            SceneWiring.SetArray(lobby, "venueButtons", venueButtons);
            SceneWiring.Set(lobby, "sheetDone", sheetDone);
            SceneWiring.Set(lobby, "toast", toast);
            SceneWiring.Set(crateScreen, "lobby", lobby);

            var pilot = canvasObject.AddComponent<DevAutoPilot>();
            SceneWiring.Set(pilot, "game", game);
            SceneWiring.Set(pilot, "menu", menu);
        }

        private static TMP_Text Balance(RectTransform parent, string name, Vector2 position, Color tint, string unit, out Button add)
        {
            Vector2 anchor = Vector2.one;
            Image(parent, name + " Pill", anchor, anchor, position + new Vector2(-150f, -36f), position + new Vector2(150f, 36f), new Color(0f, 0f, 0f, 0.55f));
            TMP_Text unitLabel = HudBuilder.Label(name + " Unit", parent, anchor, position + new Vector2(-130f, 0f), new Vector2(90f, 60f), TextAlignmentOptions.Left, 26);
            unitLabel.rectTransform.pivot = new Vector2(0f, 0.5f);
            unitLabel.text = unit;
            unitLabel.color = tint;
            TMP_Text value = HudBuilder.Label(name + " Value", parent, anchor, position + new Vector2(70f, 0f), new Vector2(190f, 60f), TextAlignmentOptions.Right, 38);
            value.rectTransform.pivot = new Vector2(1f, 0.5f);
            value.fontStyle = FontStyles.Bold;
            add = HudBuilder.Button(name + " Add", "+", parent, Rounded, anchor, position + new Vector2(118f, 0f), new Vector2(56f, 56f), new Color(0.2f, 0.65f, 0.3f));
            Caption(add, 44);
            return value;
        }

        private static Button Rail(RectTransform parent, string name, string caption, Vector2 anchor, Vector2 position, Color color)
        {
            Button button = HudBuilder.Button(name, caption, parent, Rounded, anchor, position, new Vector2(220f, 130f), color);
            Caption(button, 30);
            return button;
        }

        internal static TMP_Text Caption(Button button, float size)
        {
            var text = button.GetComponentInChildren<TMP_Text>();
            text.fontSize = size;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }

        /// <summary>A centred opaque card on a full-screen blocker; toggle <c>card.parent</c> to show or hide both.</summary>
        internal static RectTransform Modal(string name, RectTransform parent, Vector2 size)
        {
            RectTransform blocker = Stretch(name, parent, new Color(0f, 0f, 0f, 0.55f));
            var go = new GameObject("Card", typeof(RectTransform), typeof(Image));
            RectTransform card = HudBuilder.Place(go, blocker, new Vector2(0.5f, 0.5f), Vector2.zero, size);
            Image image = go.GetComponent<Image>();
            image.sprite = Rounded;
            image.type = UnityEngine.UI.Image.Type.Sliced;
            image.color = new Color(0.07f, 0.09f, 0.14f, 0.98f);
            return card;
        }

        internal static RectTransform Stretch(string name, Transform parent, Color? color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            if (color.HasValue) go.AddComponent<Image>().color = color.Value;
            return rect;
        }

        internal static Image Image(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static TMP_InputField Input(string name, RectTransform parent, Vector2 anchor, Vector2 position, Vector2 size, string placeholder)
        {
            Sprite background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd");
            GameObject go = TMP_DefaultControls.CreateInputField(new TMP_DefaultControls.Resources { inputField = background });
            go.name = name;
            HudBuilder.Place(go, parent, anchor, position, size);
            var input = go.GetComponent<TMP_InputField>();
            input.pointSize = 40;
            input.textComponent.alignment = TextAlignmentOptions.Center;
            var hint = (TMP_Text)input.placeholder;
            hint.text = placeholder;
            hint.alignment = TextAlignmentOptions.Center;
            return input;
        }
    }
}
