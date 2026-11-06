using BilliardGame.Content;
using BilliardGame.Online;
using BilliardGame.Presentation;
using BilliardGame.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BilliardGame.Editor.UI
{
    /// <summary>
    /// Lobby canvas (docs/UI_UX.md §2): lobby chrome, every lobby screen, the dialog layer and the splash, plus the
    /// controllers that drive them. Layout targets 1920×1080 and holds from 4:3 tablets to 20:9 phones with notches.
    /// </summary>
    internal static class LobbyUIBuilder
    {
        private const float Edge = 24f;
        private const float RailWidth = 200f;
        private const float RailCardHeight = 104f;
        private const float TopBand = 168f;

        public static void Build(GameController game, OnlineMatchController online, HudController hud, GameObject hudRoot, GameServices services,
            VenueLoader venues, LobbyStage stage, RoomStage room, Camera matchCamera, BilliardGame.Avatar.MatchCharacterDirector cast)
        {
            Canvas canvas = CreateCanvas("Lobby UI", 10);
            Transform host = canvas.transform;
            var router = host.gameObject.AddComponent<ScreenRouter>();
            RectTransform menuRoot = UIKit.Node("Lobby Root", host).Fill();

            var lobby = host.gameObject.AddComponent<LobbyController>();
            var menu = host.gameObject.AddComponent<MainMenuController>();
            ScreenView chrome = Chrome(menuRoot, lobby);
            UIKit.Wire(router, "lobbyChrome", chrome);

            CrateScreen crates = LobbyScreens.Crates(menuRoot, services, stage);
            InventoryScreen inventory = LobbyScreens.Inventory(menuRoot, services, stage, crates);
            CreditsScreen credits = LobbyScreens.Credits(menuRoot);
            CharacterScreen character = LobbyScreens.Character(menuRoot, services, stage, inventory);
            UIKit.Wire(lobby,
                "services", services, "menu", menu, "online", online, "game", game, "hud", hud, "venues", venues, "stage", stage, "matchCamera", matchCamera, "cast", cast,
                "chrome", chrome,
                "modeSelect", LobbyScreens.ModeSelect(menuRoot, lobby, services),
                "profileScreen", LobbyScreens.Profile(menuRoot, services),
                "inventory", inventory,
                "room", LobbyScreens.Room(menuRoot, services, room, stage),
                "settings", LobbyScreens.Settings(menuRoot, services, credits),
                "crates", crates,
                "characterScreen", character);

            LobbyScreens.MatchEntry(menuRoot, menu);
            UIKit.Wire(menu, "game", game, "online", online, "hud", hud, "services", services, "menuRoot", menuRoot.gameObject, "hudRoot", hudRoot);

            DialogsBuilder.Build(host);
            LobbyScreens.Splash(host);

            var pilot = host.gameObject.AddComponent<DevAutoPilot>();
            UIKit.Wire(pilot, "game", game, "menu", menu);
        }

        public static Canvas CreateCanvas(string name, int order)
        {
            var go = new GameObject(name) { layer = 5 };
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        // ---- Lobby chrome --------------------------------------------------------------------------------------------

        private static ScreenView Chrome(RectTransform parent, LobbyController lobby)
        {
            RectTransform root = UIKit.Node("Lobby Chrome", parent).Fill();
            root.gameObject.AddComponent<CanvasGroup>();
            // Top and bottom shading keeps text readable over the 3D hall without boxing the avatar in.
            Image top = UIKit.Image(root, "Top Shade", UIAssets.GradientUp, new Color(0f, 0f, 0f, 0.55f));
            top.rectTransform.Span(new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -260f), Vector2.zero);
            Image bottom = UIKit.Image(root, "Bottom Shade", UIAssets.GradientUp, new Color(0f, 0f, 0f, 0.6f));
            bottom.rectTransform.Span(Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 320f));
            bottom.rectTransform.localScale = new Vector3(1f, -1f, 1f);   // gradient_up is opaque at the top
            RectTransform safe = UIKit.Node("Safe Area", root).Fill();
            safe.gameObject.AddComponent<SafeAreaFitter>();

            ProfileCard(safe, lobby);
            TopRight(safe, lobby);
            LeftRail(safe, lobby);
            RightRail(safe, lobby);
            BottomLeft(safe, lobby);
            PlayArea(safe, lobby);

            var view = root.gameObject.AddComponent<ScreenView>();
            view.ConfigureView(Transition.Fade, root, UIKit.Theme.screenDuration, false, true);
            return view;
        }

        private static void ProfileCard(RectTransform safe, LobbyController lobby)
        {
            UITheme t = UIKit.Theme;
            ButtonParts card = UIKit.Button(safe, "Profile Card", ButtonKind.Ghost, null, null, new Vector2(540f, 132f), 32);
            card.Rect.At(new Vector2(0f, 1f), new Vector2(Edge, -Edge), new Vector2(540f, 132f), new Vector2(0f, 1f));
            card.Fill.color = new Color(t.surface.r, t.surface.g, t.surface.b, 0.75f);
            Object.DestroyImmediate(card.Label.gameObject);
            Image portrait = UIKit.Avatar(card.Rect, "Avatar", 108f, out Image frame, out TMP_Text level);
            portrait.transform.parent.GetComponent<RectTransform>().At(new Vector2(0f, 0.5f), new Vector2(14f, 4f), new Vector2(108f, 108f), new Vector2(0f, 0.5f));
            TMP_Text name = UIKit.Text(card.Rect, "Nickname", null, TextStyle.H2);
            name.rectTransform.Span(new Vector2(0f, 0.5f), Vector2.one, new Vector2(140f, 4f), new Vector2(-20f, -12f));
            Image rankIcon = UIKit.Icon(card.Rect, "ranked", 30f, t.accent);
            rankIcon.rectTransform.At(new Vector2(0f, 0.5f), new Vector2(140f, -14f), new Vector2(30f, 30f), new Vector2(0f, 0.5f));
            TMP_Text rank = UIKit.Text(card.Rect, "Rank", null, TextStyle.Caption, t.accent);
            rank.rectTransform.Span(new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(178f, -32f), new Vector2(-20f, 4f));
            ProgressBar xp = UIKit.Progress(card.Rect, "XP", t.secondary, 12f);
            ((RectTransform)xp.transform).Span(new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(140f, 22f), new Vector2(-24f, 34f));
            UIKit.Wire(lobby, "profileButton", card.Button, "avatarFrame", frame, "nicknameLabel", name, "levelLabel", level, "xpBar", xp, "rankLabel", rank);
        }

        private static void TopRight(RectTransform safe, LobbyController lobby)
        {
            UITheme t = UIKit.Theme;
            RectTransform row = UIKit.Row(safe, "Top Right", 12f, TextAnchor.MiddleRight, false);
            row.At(Vector2.one, new Vector2(-Edge, -Edge - 6f), new Vector2(900f, 88f), Vector2.one);
            CurrencyPill coins = UIKit.Pill(row, "Coins", "coin", t.coin, 250f);
            UIKit.Size(coins, 250f, 72f);
            CurrencyPill gems = UIKit.Pill(row, "Gems", "gem", t.gem, 230f);
            UIKit.Size(gems, 230f, 72f);
            ButtonParts mail = UIKit.IconButton(row, "Mail", "mail", 80f, true);
            ButtonParts friends = UIKit.IconButton(row, "Friends", "friends", 80f);
            ButtonParts settings = UIKit.IconButton(row, "Settings", "settings", 80f);
            foreach (ButtonParts b in new[] { mail, friends, settings }) UIKit.Size(b.Rect, 80f, 80f);
            UIKit.Wire(lobby, "coins", coins, "gems", gems, "mailButton", mail.Button, "friendsButton", friends.Button, "settingsButton", settings.Button);
        }

        private static RectTransform Rail(RectTransform safe, string name, Vector2 anchor, float x)
        {
            RectTransform rail = UIKit.Column(safe, name, 12f, TextAnchor.UpperCenter);
            rail.GetComponent<VerticalLayoutGroup>().childForceExpandWidth = false;
            rail.At(anchor, new Vector2(x, -TopBand), new Vector2(RailWidth, 4f * RailCardHeight + 36f), anchor);
            return rail;
        }

        private static ButtonParts RailCard(RectTransform rail, string name, string icon, string key, Color iconColor, bool soon)
        {
            ButtonParts card = UIKit.IconCard(rail, name, icon, key, new Vector2(RailWidth, RailCardHeight), ButtonKind.Secondary, 50f, iconColor);
            card.Fill.color = new Color(UIKit.Theme.surfaceRaised.r, UIKit.Theme.surfaceRaised.g, UIKit.Theme.surfaceRaised.b, 0.9f);
            UIKit.Size(card.Rect, RailWidth, RailCardHeight);
            if (soon) UIKit.SoonTag(card.Rect);
            return card;
        }

        private static void LeftRail(RectTransform safe, LobbyController lobby)
        {
            UITheme t = UIKit.Theme;
            RectTransform rail = Rail(safe, "Left Rail", new Vector2(0f, 1f), Edge);
            ButtonParts pass = RailCard(rail, "Season Pass", "pass", "lobby.pass", t.accent, true);
            ProgressBar passBar = UIKit.Progress(pass.Rect, "Pass Progress", t.accent, 8f);
            ((RectTransform)passBar.transform).Span(Vector2.zero, new Vector2(1f, 0f), new Vector2(16f, 8f), new Vector2(-16f, 16f));
            passBar.transform.Find("Fill").GetComponent<Image>().fillAmount = 0.15f;
            ButtonParts missions = RailCard(rail, "Missions", "missions", "lobby.missions", t.textPrimary, true);
            ButtonParts events = RailCard(rail, "Events", "events", "lobby.events", t.textPrimary, true);
            ButtonParts daily = RailCard(rail, "Daily", "daily", "lobby.daily", t.success, true);
            UIKit.Wire(lobby, "passButton", pass.Button, "missionsButton", missions.Button, "eventsButton", events.Button, "dailyButton", daily.Button);
        }

        private static void RightRail(RectTransform safe, LobbyController lobby)
        {
            UITheme t = UIKit.Theme;
            RectTransform rail = Rail(safe, "Right Rail", Vector2.one, -Edge);
            ButtonParts shop = RailCard(rail, "Shop", "shop", "lobby.shop", t.secondary, false);
            ButtonParts crates = RailCard(rail, "Crates", "crate", "lobby.crates", t.accent, false);
            Image glow = UIKit.Image(crates.Rect, "New Crate Glow", UIAssets.Glow, new Color(t.accent.r, t.accent.g, t.accent.b, 0.5f));
            glow.rectTransform.Fill(-30f, -30f, -30f, -30f);
            glow.transform.SetAsFirstSibling();
            crates.Icon.gameObject.AddComponent<PulseFx>().Configure(0.08f, 1.4f, glow);
            ButtonParts inventory = RailCard(rail, "Inventory", "inventory", "lobby.inventory", t.textPrimary, false);
            BadgeView badge = UIKit.Badge(inventory.Rect, new Vector2(-10f, -10f));
            ButtonParts room = RailCard(rail, "My Room", "home", "lobby.room", t.textPrimary, false);
            UIKit.Wire(lobby, "shopButton", shop.Button, "cratesButton", crates.Button, "inventoryButton", inventory.Button, "inventoryBadge", badge, "roomButton", room.Button);
        }

        private static void BottomLeft(RectTransform safe, LobbyController lobby)
        {
            RectTransform row = UIKit.Row(safe, "Social", 12f, TextAnchor.LowerLeft, false);
            row.At(Vector2.zero, new Vector2(Edge, Edge), new Vector2(396f, 104f), Vector2.zero);
            ButtonParts character = UIKit.IconCard(row, "Character", "person", "lobby.character", new Vector2(124f, 104f), ButtonKind.Secondary, 44f);
            UIKit.Size(character.Rect, 124f, 104f);
            ButtonParts club = UIKit.IconCard(row, "Club", "club", "lobby.club", new Vector2(124f, 104f), ButtonKind.Secondary, 44f);
            ButtonParts chat = UIKit.IconCard(row, "Chat", "chat", "lobby.chat", new Vector2(124f, 104f), ButtonKind.Secondary, 44f);
            foreach (ButtonParts b in new[] { club, chat })
            {
                UIKit.Size(b.Rect, 124f, 104f);
                UIKit.SoonTag(b.Rect).GetComponent<RectTransform>().anchoredPosition = new Vector2(6f, 14f);
            }

            UIKit.Wire(lobby, "characterButton", character.Button, "clubButton", club.Button, "chatButton", chat.Button);
        }

        private static void PlayArea(RectTransform safe, LobbyController lobby)
        {
            UITheme t = UIKit.Theme;
            const float width = 480f;
            // The pulse lives on a wrapper so it never fights the button's own press scale.
            RectTransform pulse = UIKit.Node("Play Pulse", safe).At(new Vector2(1f, 0f), new Vector2(-Edge, Edge), new Vector2(width, 140f), new Vector2(1f, 0f));
            Image glow = UIKit.Image(pulse, "Pulse Glow", UIAssets.Glow, new Color(t.accent.r, t.accent.g, t.accent.b, 0.6f));
            glow.rectTransform.Fill(-60f, -60f, -60f, -60f);
            ButtonParts play = UIKit.Button(pulse, "Play", ButtonKind.Primary, "lobby.play", "play", new Vector2(width, 140f), 32, 64f);
            play.Rect.Fill();
            play.Label.fontSize = play.Label.fontSizeMax = 56f;
            play.Label.font = t.bold;
            pulse.gameObject.AddComponent<PulseFx>().Configure(0.035f, 1.6f, glow);

            ButtonParts card = UIKit.Button(safe, "Mode Card", ButtonKind.Secondary, null, null, new Vector2(width, 128f), 20);
            card.Rect.At(new Vector2(1f, 0f), new Vector2(-Edge, Edge + 156f), new Vector2(width, 128f), new Vector2(1f, 0f));
            Object.DestroyImmediate(card.Label.gameObject);
            card.Fill.color = new Color(t.surface.r, t.surface.g, t.surface.b, 0.9f);
            Image picture = UIKit.Image(card.Rect, "Venue Image", null, Color.white);
            picture.rectTransform.At(new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(176f, 104f), new Vector2(0f, 0.5f));
            picture.preserveAspect = false;
            picture.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            TMP_Text mode = UIKit.Text(card.Rect, "Mode", null, TextStyle.Caption, t.accent);
            mode.font = t.Font(TextStyle.Button);
            mode.enableAutoSizing = true;
            mode.fontSizeMin = 13f;
            // Top line clears the mid-height chevron, so it may run to the card edge.
            mode.rectTransform.Span(new Vector2(0f, 0.66f), Vector2.one, new Vector2(204f, 0f), new Vector2(-16f, -10f));
            TMP_Text venue = UIKit.Text(card.Rect, "Venue", null, TextStyle.H2);
            venue.fontSize = venue.fontSizeMax = 30f;
            venue.rectTransform.Span(new Vector2(0f, 0.34f), new Vector2(1f, 0.68f), new Vector2(204f, 0f), new Vector2(-56f, 0f));
            TMP_Text entry = Money(card.Rect, "Entry", "ticket", t.textSecondary, new Vector2(204f, 12f), 140f);
            TMP_Text prize = Money(card.Rect, "Prize", "trophy", t.coin, new Vector2(384f, 12f), 90f);
            UIKit.Icon(card.Rect, "chevron_right", 40f, t.textSecondary).rectTransform.At(new Vector2(1f, 0.5f), new Vector2(-12f, 0f), new Vector2(40f, 40f), new Vector2(1f, 0.5f));
            UIKit.Wire(lobby, "playButton", play.Button, "modeCard", card.Button, "venueImage", picture, "modeLabel", mode, "venueLabel", venue, "entryLabel", entry, "prizeLabel", prize);
        }

        private static TMP_Text Money(RectTransform parent, string name, string icon, Color color, Vector2 position, float width)
        {
            UIKit.Icon(parent, icon, 28f, color).rectTransform.At(Vector2.zero, position, new Vector2(28f, 28f), Vector2.zero);
            TMP_Text text = UIKit.Text(parent, name, null, TextStyle.Number, color);
            text.fontSize = text.fontSizeMax = 24f;
            text.rectTransform.At(Vector2.zero, position + new Vector2(34f, -2f), new Vector2(width, 32f), Vector2.zero);
            return text;
        }
    }
}
