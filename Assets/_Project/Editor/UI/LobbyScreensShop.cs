using System.Collections.Generic;
using BilliardGame.Meta;
using BilliardGame.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BilliardGame.Editor.UI
{
    /// <summary>Lobby screens, part 2: inventory, My Room, crates, credits, friend room / waiting, splash.</summary>
    internal static partial class LobbyScreens
    {
        private const float DetailWidth = 540f;
        private const float CategoryRail = 270f;

        private static readonly string[] CategoryIcons = { "cue_icon", "table", "eight_ball", "palette", "outfit", "frame", "emote", "sticker", "effects_fx" };

        private static readonly string[] CategoryKeys =
        {
            "category.cue", "category.table", "category.balls", "category.venuetheme", "category.outfit", "category.frame", "category.emote", "category.sticker", "category.effect",
        };

        // ---- Inventory -----------------------------------------------------------------------------------------------

        public static InventoryScreen Inventory(RectTransform parent, GameServices services, LobbyStage stage, ScreenView crates)
        {
            var view = UIKit.Screen<InventoryScreen>(parent, "Inventory", Transition.SlideLeft, true, out RectTransform safe);
            view.BindBack(UIKit.Header(safe, "inventory.title", "inventory", out RectTransform bar));

            // Header right: shards, owned-only, sort.
            RectTransform tools = UIKit.Row(bar, "Tools", 16f, TextAnchor.MiddleRight, false);
            tools.Span(new Vector2(0.4f, 0f), Vector2.one, Vector2.zero, new Vector2(-Gutter, 0f));
            RectTransform shardBox = UIKit.Row(tools, "Shards", 8f, TextAnchor.MiddleLeft, false);
            UIKit.Size(UIKit.Icon(shardBox, "upgrade", 36f, T.secondary), 36f, 36f);
            TMP_Text shards = UIKit.Text(shardBox, "Value", null, TextStyle.Number, T.textPrimary);
            shards.fontSize = shards.fontSizeMax = 26f;
            shards.enableAutoSizing = true;
            shards.fontSizeMin = 18f;
            // 4:3 leaves ~1000 units for the tools row, so this width is the budget, not the text length.
            UIKit.Size(shards, 330f, 48f);
            RectTransform ownedBox = UIKit.Row(tools, "Owned Only", 10f, TextAnchor.MiddleLeft, false);
            TMP_Text ownedLabel = UIKit.Text(ownedBox, "Label", "inventory.owned_only", TextStyle.Caption, T.textSecondary, TextAlignmentOptions.MidlineRight);
            UIKit.Size(ownedLabel, 190f, 48f);
            ToggleSwitch owned = UIKit.Toggle(ownedBox, "Toggle");
            UIKit.Size(owned, 96f, 52f);
            TMP_Dropdown sort = UIKit.Dropdown(tools, "Sort", new Vector2(260f, 72f));

            RectTransform body = Body(safe);
            TabBar categories = UIKit.Tabs(body, "Categories", CategoryKeys, CategoryIcons, new Vector2(CategoryRail, 76f), true, 8f);
            ((RectTransform)categories.transform).Span(Vector2.zero, new Vector2(0f, 1f), Vector2.zero, new Vector2(CategoryRail, 0f));

            RectTransform gridArea = UIKit.Node("Grid Area", body).Fill(CategoryRail + 24f, 0f, DetailWidth + 24f, 0f);
            ScrollRect scroll = UIKit.Scroll(gridArea, "Grid", true, out RectTransform content);
            ((RectTransform)scroll.transform).Fill();
            GridLayoutGroup grid = UIKit.Grid(content, new Vector2(200f, 236f), 3, 18f);
            grid.constraint = GridLayoutGroup.Constraint.Flexible;
            ItemTile tile = UIKit.Tile(content, "Tile Template");
            EmptyStateView empty = UIKit.EmptyState(gridArea);
            ((RectTransform)empty.transform).Fill();

            // Detail panel.
            RectTransform detail = UIKit.Card(body, "Detail", null, null);
            detail.Span(new Vector2(1f, 0f), Vector2.one, new Vector2(-DetailWidth, 0f), Vector2.zero);
            Image stageBack = UIKit.Image(detail, "Preview Back", UIAssets.Rounded(20), T.surfaceSunken);
            stageBack.rectTransform.Span(new Vector2(0f, 1f), Vector2.one, new Vector2(16f, -376f), new Vector2(-16f, -16f));
            RawImage preview = new GameObject("Preview", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            preview.gameObject.layer = 5;
            preview.rectTransform.SetParent(stageBack.transform, false);
            preview.rectTransform.Fill(4f, 4f, 4f, 4f);
            preview.raycastTarget = true;
            Image previewIcon = UIKit.Image(stageBack.transform, "Icon", null, Color.white);
            previewIcon.preserveAspect = true;
            previewIcon.rectTransform.Fill(60f, 50f, 60f, 30f);
            TMP_Text hint = UIKit.Text(stageBack.transform, "Hint", "inventory.preview_drag", TextStyle.Caption, T.textSecondary, TextAlignmentOptions.Center);
            hint.fontSize = hint.fontSizeMax = 18f;
            hint.rectTransform.Span(Vector2.zero, new Vector2(1f, 0f), new Vector2(0f, 6f), new Vector2(0f, 36f));

            TMP_Text name = UIKit.Text(detail, "Name", null, TextStyle.H1);
            name.rectTransform.Span(new Vector2(0f, 1f), Vector2.one, new Vector2(24f, -440f), new Vector2(-24f, -388f));
            Image rarityBar = UIKit.Image(detail, "Rarity Bar", UIAssets.Pill, T.accent);
            rarityBar.rectTransform.Span(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -476f), new Vector2(32f, -448f));
            TMP_Text rarity = UIKit.Text(detail, "Rarity", null, TextStyle.Caption, T.textPrimary, TextAlignmentOptions.MidlineLeft, true);
            rarity.font = T.Font(TextStyle.Button);
            rarity.rectTransform.Span(new Vector2(0f, 1f), Vector2.one, new Vector2(44f, -478f), new Vector2(-24f, -446f));
            TMP_Text description = UIKit.Wrap(UIKit.Text(detail, "Description", null, TextStyle.Body, T.textSecondary, TextAlignmentOptions.TopLeft));
            description.fontSize = description.fontSizeMax = 24f;
            description.rectTransform.Span(new Vector2(0f, 1f), Vector2.one, new Vector2(24f, -580f), new Vector2(-24f, -490f));
            TMP_Text source = UIKit.Wrap(UIKit.Text(detail, "Source", null, TextStyle.Caption, T.accent, TextAlignmentOptions.TopLeft));
            source.rectTransform.Span(new Vector2(0f, 1f), Vector2.one, new Vector2(24f, -630f), new Vector2(-24f, -586f));

            ButtonParts equip = UIKit.Button(detail, "Equip", ButtonKind.Primary, "inventory.equip", "check", new Vector2(300f, 88f));
            equip.Rect.Span(Vector2.zero, new Vector2(1f, 0f), new Vector2(24f, 20f), new Vector2(-24f, 108f));
            ButtonParts upgrade = UIKit.Button(detail, "Upgrade", ButtonKind.Secondary, "inventory.upgrade", "upgrade", new Vector2(300f, 72f));
            upgrade.Rect.Span(Vector2.zero, new Vector2(1f, 0f), new Vector2(24f, 120f), new Vector2(-24f, 192f));
            RectTransform stickerRow = UIKit.Row(detail, "Sticker Row", 12f, TextAnchor.MiddleCenter, true);
            stickerRow.Span(Vector2.zero, new Vector2(1f, 0f), new Vector2(24f, 20f), new Vector2(-24f, 108f));
            stickerRow.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = true;
            Button onCue = UIKit.Button(stickerRow, "On Cue", ButtonKind.Primary, "inventory.on_cue", "cue_icon", new Vector2(150f, 88f)).Button;
            Button onTable = UIKit.Button(stickerRow, "On Table", ButtonKind.Secondary, "inventory.on_table", "table", new Vector2(150f, 88f)).Button;
            Button remove = UIKit.Button(stickerRow, "Remove", ButtonKind.Ghost, "inventory.remove", "delete", new Vector2(150f, 88f)).Button;
            stickerRow.gameObject.SetActive(false);

            UIKit.Wire(view, "services", services, "stage", stage, "crates", crates, "categories", categories, "sort", sort, "ownedOnly", owned, "shards", shards,
                "tileTemplate", tile, "empty", empty, "detail", detail.gameObject, "preview", preview, "previewIcon", previewIcon, "previewHint", hint,
                "itemName", name, "rarityLabel", rarity, "rarityBar", rarityBar, "description", description, "source", source,
                "equipButton", equip.Button, "equipLabel", equip.Label, "stickerRow", stickerRow.gameObject, "stickerCue", onCue, "stickerTable", onTable,
                "stickerRemove", remove, "upgradeButton", upgrade.Button, "coinSprite", UIAssets.Icon("coin"), "gemSprite", UIAssets.Icon("gem"));
            return view;
        }

        // ---- My Room -------------------------------------------------------------------------------------------------

        public static MyRoomScreen Room(RectTransform parent, GameServices services, RoomStage room, LobbyStage lobbyStage)
        {
            var view = UIKit.Screen<MyRoomScreen>(parent, "My Room", Transition.Fade, true, out RectTransform safe, Backdrop.None);
            view.BindBack(UIKit.Header(safe, "room.title", "home", out RectTransform bar));
            RectTransform trophyPill = UIKit.Row(bar, "Trophies", 10f, TextAnchor.MiddleRight, false);
            trophyPill.Span(new Vector2(0.5f, 0f), Vector2.one, Vector2.zero, new Vector2(-Gutter, 0f));
            UIKit.Size(UIKit.Icon(trophyPill, "trophy", 44f, T.coin), 44f, 44f);
            TMP_Text trophies = UIKit.Text(trophyPill, "Value", null, TextStyle.H2, T.textPrimary, TextAlignmentOptions.MidlineLeft);
            UIKit.Size(trophies, 240f, 56f);

            ButtonParts edit = UIKit.Button(safe, "Edit", ButtonKind.Primary, "room.edit", "edit", new Vector2(320f, 96f));
            edit.Rect.At(new Vector2(1f, 0f), new Vector2(-Gutter, Gutter), new Vector2(320f, 96f), new Vector2(1f, 0f));
            TMP_Text visit = UIKit.Text(safe, "Visit Soon", "room.visit_soon", TextStyle.Caption, T.textSecondary);
            visit.rectTransform.At(Vector2.zero, new Vector2(Gutter, Gutter), new Vector2(700f, 48f), Vector2.zero);

            // Edit panel: bottom sheet-like strip over the room (room stays visible above it).
            Image panelFill = UIKit.Panel(safe, "Edit Panel", new Color(T.surface.r, T.surface.g, T.surface.b, 0.96f), 32, true);
            var panel = (RectTransform)panelFill.transform.parent;
            panel.Span(Vector2.zero, new Vector2(1f, 0f), new Vector2(Gutter, Gutter), new Vector2(-Gutter, Gutter + 420f));
            TabBar tabs = UIKit.Tabs(panel, "Tabs", new[] { "room.tab_table", "room.tab_wall", "room.tab_floor", "room.tab_decor" },
                new[] { "table", "wall", "floor", "decor" }, new Vector2(240f, 68f), false, 10f);
            ((RectTransform)tabs.transform).Span(new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -84f), new Vector2(-560f, -16f));
            ButtonParts cancel = UIKit.Button(panel, "Cancel", ButtonKind.Ghost, "common.cancel", null, new Vector2(240f, 68f));
            cancel.Rect.At(Vector2.one, new Vector2(-284f, -16f), new Vector2(240f, 68f), Vector2.one);
            ButtonParts save = UIKit.Button(panel, "Save", ButtonKind.Primary, "common.save", "check", new Vector2(240f, 68f));
            save.Rect.At(Vector2.one, new Vector2(-20f, -16f), new Vector2(240f, 68f), Vector2.one);

            RectTransform slots = UIKit.Row(panel, "Slots", 12f, TextAnchor.MiddleLeft, false);
            slots.Span(new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -160f), new Vector2(-20f, -96f));
            var posterSlots = new List<Object>();
            for (int i = 0; i < 3; i++)
            {
                ButtonParts slot = UIKit.Button(slots, "Slot " + i, ButtonKind.Secondary, null, null, new Vector2(220f, 60f));
                slot.Label.text = (i + 1).ToString();
                UIKit.Size(slot.Rect, 220f, 60f);
                posterSlots.Add(slot.Button);
            }

            ScrollRect scroll = UIKit.Scroll(panel, "Items", false, out RectTransform content);
            ((RectTransform)scroll.transform).Fill(20f, 16f, 20f, 170f);
            var row = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 16f;
            row.childControlWidth = row.childControlHeight = false;
            row.childForceExpandWidth = row.childForceExpandHeight = false;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.padding = new RectOffset(4, 4, 4, 4);
            ItemTile tile = UIKit.Tile(content, "Tile Template");
            panel.gameObject.SetActive(false);

            UIKit.Wire(view, "services", services, "room", room, "lobbyStage", lobbyStage, "trophiesLabel", trophies, "editButton", edit.Button,
                "editPanel", panel.gameObject, "editTabs", tabs, "tileTemplate", tile, "slotRow", slots.gameObject, "posterSlots", posterSlots.ToArray(),
                "saveButton", save.Button, "cancelButton", cancel.Button);
            return view;
        }

        // ---- Character -----------------------------------------------------------------------------------------------

        public static CharacterScreen Character(RectTransform parent, GameServices services, LobbyStage stage, InventoryScreen inventory)
        {
            const int Slots = 4;
            var view = UIKit.Screen<CharacterScreen>(parent, "Character", Transition.Fade, true, out RectTransform safe, Backdrop.None);
            view.BindBack(UIKit.Header(safe, "character.title", "person", out RectTransform bar));
            TMP_Text nameLabel = UIKit.Text(bar, "Name", null, TextStyle.H1, T.textPrimary, TextAlignmentOptions.MidlineRight);
            nameLabel.rectTransform.Span(new Vector2(0.5f, 0f), Vector2.one, Vector2.zero, new Vector2(-Gutter, 0f));

            Image panelFill = UIKit.Panel(safe, "Panel", new Color(T.surface.r, T.surface.g, T.surface.b, 0.92f), 32, true);
            var panel = (RectTransform)panelFill.transform.parent;
            panel.Span(Vector2.zero, new Vector2(1f, 0f), new Vector2(Gutter, Gutter), new Vector2(-Gutter, Gutter + 300f));

            RectTransform row = UIKit.Row(panel, "Cards", 20f, TextAnchor.MiddleLeft, false);
            row.Fill(24f, 20f, 420f, 20f);
            var cards = new List<Object>();
            var portraits = new List<Object>();
            var names = new List<Object>();
            for (int i = 0; i < Slots; i++)
            {
                ButtonParts card = UIKit.Button(row, "Card " + i, ButtonKind.Secondary, null, null, new Vector2(210f, 260f), 24);
                UIKit.Size(card.Rect, 210f, 260f);
                ((RectTransform)card.Label.transform.parent).Span(Vector2.zero, new Vector2(1f, 0f), new Vector2(8f, 6f), new Vector2(-8f, 56f));
                card.Label.fontSize = card.Label.fontSizeMax = 30f;
                Image portrait = UIKit.Image(card.Rect, "Portrait", null, Color.white);
                portrait.preserveAspect = true;
                portrait.rectTransform.Fill(14f, 60f, 14f, 14f);
                cards.Add(card.Button);
                portraits.Add(portrait);
                names.Add(card.Label);
            }

            ButtonParts select = UIKit.Button(panel, "Select", ButtonKind.Primary, "character.select", "check", new Vector2(360f, 104f));
            select.Rect.At(new Vector2(1f, 0.5f), new Vector2(-24f, 10f), new Vector2(360f, 104f), new Vector2(1f, 0f));
            ButtonParts outfits = UIKit.Button(panel, "Outfits", ButtonKind.Secondary, "character.outfits", "outfit", new Vector2(360f, 88f));
            outfits.Rect.At(new Vector2(1f, 0.5f), new Vector2(-24f, -10f), new Vector2(360f, 88f), new Vector2(1f, 1f));

            UIKit.Wire(view, "services", services, "stage", stage, "inventory", inventory, "cards", cards.ToArray(), "portraits", portraits.ToArray(),
                "names", names.ToArray(), "nameLabel", nameLabel, "selectButton", select.Button, "selectLabel", select.Label, "outfitsButton", outfits.Button);
            return view;
        }

        // ---- Crates --------------------------------------------------------------------------------------------------

        public static CrateScreen Crates(RectTransform parent, GameServices services, LobbyStage stage)
        {
            var view = UIKit.Screen<CrateScreen>(parent, "Crates", Transition.SlideLeft, true, out RectTransform safe);
            view.BindBack(UIKit.Header(safe, "crates.title", "crate", out RectTransform bar));
            RectTransform wallet = UIKit.Row(bar, "Wallet", 16f, TextAnchor.MiddleRight, false);
            wallet.Span(new Vector2(0.45f, 0f), Vector2.one, Vector2.zero, new Vector2(-Gutter, 0f));
            CurrencyPill coins = UIKit.Pill(wallet, "Coins", "coin", T.coin, 250f);
            UIKit.Size(coins, 250f, 72f);
            CurrencyPill gems = UIKit.Pill(wallet, "Gems", "gem", T.gem, 230f);
            UIKit.Size(gems, 230f, 72f);
            Sprite coin = UIAssets.Icon("coin");
            Sprite gem = UIAssets.Icon("gem");

            RectTransform cardsRoot = UIKit.Row(safe, "Cards", 32f, TextAnchor.MiddleCenter, false);
            cardsRoot.Fill(Gutter, Gutter + 60f, Gutter, Header + 16f);
            var cards = new List<Object>();
            for (int i = 0; i < 3; i++) cards.Add(CrateCard(cardsRoot, i, coin, gem));
            TMP_Text fair = UIKit.Wrap(UIKit.Text(safe, "Fair", "crates.equal", TextStyle.Caption, T.textSecondary, TextAlignmentOptions.Center));
            fair.rectTransform.Span(Vector2.zero, new Vector2(1f, 0f), new Vector2(Gutter, Gutter), new Vector2(-Gutter, Gutter + 52f));

            // Reveal: glowing crate while the server rolls (skip jumps to the result).
            RectTransform reveal = UIKit.Node("Reveal", safe).Fill();
            Image burst = UIKit.Image(reveal, "Rays", UIAssets.Rays, new Color(T.accent.r, T.accent.g, T.accent.b, 0.35f));
            burst.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 900f));
            burst.gameObject.AddComponent<Spinner>();
            Image crateGlyph = UIKit.Icon(reveal, "crate", 300f, T.accent);
            crateGlyph.rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300f, 300f));
            crateGlyph.gameObject.AddComponent<PulseFx>().Configure(0.06f, 0.5f, burst);
            ButtonParts skip = UIKit.Button(reveal, "Skip", ButtonKind.Ghost, "crates.skip", null, new Vector2(240f, 80f));
            skip.Rect.At(new Vector2(1f, 0f), new Vector2(-Gutter, Gutter), new Vector2(240f, 80f), new Vector2(1f, 0f));
            reveal.gameObject.SetActive(false);

            // Result: up to 10 tiles, best drop highlighted by the tile glow.
            RectTransform result = UIKit.Node("Result", safe).Fill(0f, 0f, 0f, Header);
            UIKit.Image(result, "Scrim", null, new Color(0f, 0f, 0f, 0.55f), true).rectTransform.Fill(0f, 0f, 0f, -Header);
            TMP_Text resultTitle = UIKit.Text(result, "Title", "crates.result_best", TextStyle.Display, T.accent, TextAlignmentOptions.Center, true);
            resultTitle.rectTransform.Span(new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -100f), Vector2.zero);
            RectTransform tiles = UIKit.Node("Tiles", result);
            tiles.Span(new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(Gutter, 140f), new Vector2(-Gutter, -116f));
            GridLayoutGroup tileGrid = UIKit.Grid(tiles, new Vector2(200f, 236f), 5, 24f);
            tileGrid.childAlignment = TextAnchor.MiddleCenter;
            var resultTiles = new List<Object>();
            for (int i = 0; i < 10; i++) resultTiles.Add(UIKit.Tile(tiles, "Drop " + i));
            RectTransform actions = UIKit.Row(result, "Actions", 24f, TextAnchor.MiddleCenter, false);
            actions.Span(Vector2.zero, new Vector2(1f, 0f), new Vector2(Gutter, Gutter), new Vector2(-Gutter, Gutter + 96f));
            ButtonParts close = UIKit.Button(actions, "Close", ButtonKind.Ghost, "common.close", null, new Vector2(280f, 96f));
            ButtonParts again = UIKit.Button(actions, "Again", ButtonKind.Secondary, "crates.again", "refresh", new Vector2(320f, 96f));
            ButtonParts equip = UIKit.Button(actions, "Equip", ButtonKind.Primary, "crates.equip", "check", new Vector2(340f, 96f));
            foreach (ButtonParts b in new[] { close, again, equip }) UIKit.Size(b.Rect, b.Rect.sizeDelta.x, 96f);
            result.gameObject.SetActive(false);

            ScreenView odds = OddsModal(parent, out TMP_Text oddsTitle, out TMP_Text oddsText);
            UIKit.Wire(view, "services", services, "stage", stage, "coinSprite", coin, "gemSprite", gem, "coins", coins, "gems", gems,
                "cardsRoot", cardsRoot.gameObject, "cards", cards.ToArray(), "oddsView", odds, "oddsTitle", oddsTitle, "oddsText", oddsText,
                "revealPanel", reveal.gameObject, "skipButton", skip.Button, "resultPanel", result.gameObject, "resultTitle", resultTitle,
                "resultTiles", resultTiles.ToArray(), "equipButton", equip.Button, "equipLabel", equip.Label, "againButton", again.Button, "resultClose", close.Button);
            return view;
        }

        private static CrateCardView CrateCard(RectTransform parent, int index, Sprite coin, Sprite gem)
        {
            RectTransform card = UIKit.Card(parent, "Crate " + index, null, null);
            UIKit.Size(card, 520f, 640f);   // also sets sizeDelta: the card row does not control height
            Image glow = UIKit.Image(card, "Glow", UIAssets.Glow, new Color(T.accent.r, T.accent.g, T.accent.b, 0.25f));
            glow.rectTransform.At(new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(420f, 360f), new Vector2(0.5f, 1f));
            Image glyph = UIKit.Icon(card, "crate", 220f, T.accent);
            glyph.rectTransform.At(new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(220f, 220f), new Vector2(0.5f, 1f));
            glyph.gameObject.AddComponent<PulseFx>().Configure(0.03f, 2.2f, glow);
            ButtonParts info = UIKit.IconButton(card, "Odds", "info", 72f);
            info.Rect.At(Vector2.one, new Vector2(-14f, -14f), new Vector2(72f, 72f), Vector2.one);
            TMP_Text title = UIKit.Text(card, "Title", null, TextStyle.H1, null, TextAlignmentOptions.Center, true);
            title.rectTransform.Span(new Vector2(0f, 1f), Vector2.one, new Vector2(20f, -370f), new Vector2(-20f, -306f));
            TMP_Text pity = UIKit.Wrap(UIKit.Text(card, "Pity", null, TextStyle.Caption, T.textSecondary, TextAlignmentOptions.Center));
            pity.rectTransform.Span(new Vector2(0f, 1f), Vector2.one, new Vector2(24f, -460f), new Vector2(-24f, -378f));
            ButtonParts one = PriceButton(card, "Open One", ButtonKind.Secondary, new Vector2(24f, 24f), out TMP_Text priceOne, out Image currencyOne);
            ButtonParts ten = PriceButton(card, "Open Ten", ButtonKind.Primary, new Vector2(272f, 24f), out TMP_Text priceTen, out Image currencyTen);
            var view = card.gameObject.AddComponent<CrateCardView>();
            UIKit.Wire(view, "title", title, "pity", pity, "infoButton", info.Button, "openOne", one.Button, "priceOne", priceOne, "currencyOne", currencyOne,
                "openTen", ten.Button, "priceTen", priceTen, "currencyTen", currencyTen, "coinSprite", coin, "gemSprite", gem);
            return view;
        }

        /// <summary>Open button: currency icon + "1×  2 500" (CrateCardView writes the count and price).</summary>
        private static ButtonParts PriceButton(RectTransform card, string name, ButtonKind kind, Vector2 position, out TMP_Text price, out Image currency)
        {
            ButtonParts button = UIKit.Button(card, name, kind, null, "coin", new Vector2(224f, 96f), 20, 40f);
            button.Rect.At(Vector2.zero, position, new Vector2(224f, 96f), Vector2.zero);
            currency = button.Icon;
            currency.color = Color.white;
            Color ink = kind == ButtonKind.Primary ? T.textOnAccent : T.textPrimary;
            price = UIKit.Text(button.Rect.Find("Content"), "Price", null, TextStyle.Number, ink, TextAlignmentOptions.MidlineLeft);
            price.fontSize = price.fontSizeMax = 28f;
            price.fontSizeMin = 16f;
            UIKit.Size(price, 140f, 48f);
            return button;
        }

        private static ScreenView OddsModal(RectTransform parent, out TMP_Text title, out TMP_Text body)
        {
            var view = UIKit.Modal<ScreenView>(parent, "Crate Odds", new Vector2(1100f, 800f), out RectTransform card);
            title = UIKit.Text(card, "Title", null, TextStyle.H1, null, TextAlignmentOptions.MidlineLeft, true);
            title.rectTransform.Span(new Vector2(0f, 1f), Vector2.one, new Vector2(40f, -110f), new Vector2(-140f, -24f));
            ButtonParts close = UIKit.IconButton(card, "Close", "close", 80f);
            close.Rect.At(Vector2.one, new Vector2(-24f, -24f), new Vector2(80f, 80f), Vector2.one);
            view.BindBack(close.Button);
            ScrollRect scroll = UIKit.Scroll(card, "Scroll", true, out RectTransform content);
            ((RectTransform)scroll.transform).Fill(40f, 32f, 40f, 124f);
            Stack(content.gameObject.AddComponent<VerticalLayoutGroup>(), 0f);
            body = UIKit.Wrap(UIKit.Text(content, "Body", null, TextStyle.Body, T.textPrimary, TextAlignmentOptions.TopLeft));
            body.fontSize = body.fontSizeMax = 24f;
            body.richText = true;
            return view;
        }

        // ---- Credits -------------------------------------------------------------------------------------------------

        public static CreditsScreen Credits(RectTransform parent)
        {
            var view = UIKit.Modal<CreditsScreen>(parent, "Credits", new Vector2(1300f, 860f), out RectTransform card);
            TMP_Text title = UIKit.Text(card, "Title", "credits.title", TextStyle.H1, null, TextAlignmentOptions.MidlineLeft, true);
            title.rectTransform.Span(new Vector2(0f, 1f), Vector2.one, new Vector2(40f, -110f), new Vector2(-140f, -24f));
            ButtonParts close = UIKit.IconButton(card, "Close", "close", 80f);
            close.Rect.At(Vector2.one, new Vector2(-24f, -24f), new Vector2(80f, 80f), Vector2.one);
            view.BindBack(close.Button);
            ScrollRect scroll = UIKit.Scroll(card, "Scroll", true, out RectTransform content);
            ((RectTransform)scroll.transform).Fill(40f, 32f, 40f, 124f);
            Stack(content.gameObject.AddComponent<VerticalLayoutGroup>(), 0f);
            TMP_Text body = UIKit.Wrap(UIKit.Text(content, "Body", null, TextStyle.Body, T.textPrimary, TextAlignmentOptions.TopLeft));
            body.fontSize = body.fontSizeMax = 22f;
            body.richText = true;
            UIKit.Wire(view, "body", body);
            return view;
        }

        // ---- Friend room + waiting -----------------------------------------------------------------------------------

        public static void MatchEntry(RectTransform parent, MainMenuController menu)
        {
            var friend = UIKit.Modal<ScreenView>(parent, "Friend Room", new Vector2(900f, 640f), out RectTransform card);
            UIKit.Icon(card, "link", 52f, T.accent).rectTransform.At(new Vector2(0f, 1f), new Vector2(40f, -40f), new Vector2(52f, 52f), new Vector2(0f, 1f));
            TMP_Text title = UIKit.Text(card, "Title", "friend.title", TextStyle.H1, null, TextAlignmentOptions.MidlineLeft, true);
            title.rectTransform.Span(new Vector2(0f, 1f), Vector2.one, new Vector2(108f, -110f), new Vector2(-140f, -24f));
            ButtonParts close = UIKit.IconButton(card, "Close", "close", 80f);
            close.Rect.At(Vector2.one, new Vector2(-24f, -24f), new Vector2(80f, 80f), Vector2.one);
            friend.BindBack(close.Button);
            ButtonParts create = UIKit.Button(card, "Create", ButtonKind.Primary, "friend.create", "add", new Vector2(820f, 100f));
            create.Rect.At(new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(820f, 100f), new Vector2(0.5f, 1f));
            TMP_Text or = UIKit.Text(card, "Or", "friend.or", TextStyle.Caption, T.textSecondary, TextAlignmentOptions.Center, true);
            or.rectTransform.At(new Vector2(0.5f, 1f), new Vector2(0f, -284f), new Vector2(400f, 40f), new Vector2(0.5f, 1f));
            TMP_InputField code = UIKit.Field(card, "Code", new Vector2(500f, 100f), "friend.code");
            ((RectTransform)code.transform).At(new Vector2(0f, 1f), new Vector2(40f, -350f), new Vector2(500f, 100f), new Vector2(0f, 1f));
            code.contentType = TMP_InputField.ContentType.IntegerNumber;
            code.characterLimit = 6;
            ButtonParts join = UIKit.Button(card, "Join", ButtonKind.Secondary, "friend.join", "play", new Vector2(300f, 100f));
            join.Rect.At(Vector2.one, new Vector2(-40f, -350f), new Vector2(300f, 100f), Vector2.one);

            var waiting = UIKit.Modal<ScreenView>(parent, "Waiting", new Vector2(820f, 480f), out RectTransform wait);
            RectTransform spinner = UIKit.Spinner(wait, 120f);
            spinner.At(new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(120f, 120f), new Vector2(0.5f, 1f));
            TMP_Text status = UIKit.Wrap(UIKit.Text(wait, "Status", null, TextStyle.H2, null, TextAlignmentOptions.Center));
            status.rectTransform.Span(new Vector2(0f, 0f), Vector2.one, new Vector2(40f, 150f), new Vector2(-40f, -200f));
            ButtonParts cancel = UIKit.Button(wait, "Cancel", ButtonKind.Ghost, "common.cancel", "close", new Vector2(320f, 88f));
            cancel.Rect.At(new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(320f, 88f), new Vector2(0.5f, 0f));

            UIKit.Wire(menu, "friendView", friend, "codeInput", code, "createRoomButton", create.Button, "joinRoomButton", join.Button,
                "waitingView", waiting, "waitingStatus", status, "cancelButton", cancel.Button);
        }

        // ---- Splash --------------------------------------------------------------------------------------------------

        public static SplashScreen Splash(Transform host)
        {
            var view = UIKit.Screen<SplashScreen>(host, "Splash", Transition.Fade, true, out RectTransform safe);
            RectTransform logo = UIKit.Node("Logo", safe);
            logo.At(new Vector2(0.5f, 0.58f), Vector2.zero, new Vector2(900f, 360f));
            logo.localScale = Vector3.one * 0.8f;
            Image glow = UIKit.Image(logo, "Glow", UIAssets.Glow, new Color(T.accent.r, T.accent.g, T.accent.b, 0.3f));
            glow.rectTransform.At(new Vector2(0.5f, 0.62f), Vector2.zero, new Vector2(520f, 520f));
            UIKit.Icon(logo, "eight_ball", 200f, T.textPrimary).rectTransform.At(new Vector2(0.5f, 1f), Vector2.zero, new Vector2(200f, 200f), new Vector2(0.5f, 1f));
            TMP_Text name = UIKit.Text(logo, "Name", null, TextStyle.Display, T.accent, TextAlignmentOptions.Center);
            name.text = "BILLIARD";
            name.characterSpacing = 18f;
            name.rectTransform.Span(Vector2.zero, new Vector2(1f, 0f), new Vector2(0f, 50f), new Vector2(0f, 150f));
            TMP_Text tagline = UIKit.Text(logo, "Tagline", "splash.tagline", TextStyle.Body, T.textSecondary, TextAlignmentOptions.Center);
            tagline.rectTransform.Span(Vector2.zero, new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 48f));

            ProgressBar progress = UIKit.Progress(safe, "Progress", T.accent, 12f);
            ((RectTransform)progress.transform).Span(new Vector2(0.2f, 0f), new Vector2(0.8f, 0f), new Vector2(0f, 150f), new Vector2(0f, 162f));
            TMP_Text status = UIKit.Text(safe, "Status", "splash.loading", TextStyle.Caption, T.textSecondary, TextAlignmentOptions.Center);
            status.rectTransform.Span(new Vector2(0.2f, 0f), new Vector2(0.8f, 0f), new Vector2(0f, 172f), new Vector2(0f, 212f));
            TMP_Text tip = UIKit.Wrap(UIKit.Text(safe, "Tip", null, TextStyle.Body, T.textPrimary, TextAlignmentOptions.Center));
            tip.rectTransform.Span(new Vector2(0.12f, 0f), new Vector2(0.88f, 0f), new Vector2(0f, 50f), new Vector2(0f, 130f));

            UIKit.Wire(view, "progress", progress, "tip", tip, "status", status, "logo", logo);
            // Start() opens it through the router, so it must be active in the saved scene.
            view.gameObject.SetActive(true);
            return view;
        }
    }
}
