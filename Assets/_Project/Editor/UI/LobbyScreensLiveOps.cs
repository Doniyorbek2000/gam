using System.Collections.Generic;
using BilliardGame.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BilliardGame.Editor.UI
{
    /// <summary>LiveOps screens (ROADMAP M3.2): missions, Season Pass, events, daily shop (this file).</summary>
    internal static partial class LobbyScreens
    {
        private const int MissionRows = 5;
        private const int ShopSlots = 6;

        /// <summary>Text the screen writes at runtime: drop the builder's LocalizedText so it never overwrites it.</summary>
        private static TMP_Text Dynamic(TMP_Text text)
        {
            var localized = text.GetComponent<LocalizedText>();
            if (localized != null) Object.DestroyImmediate(localized);
            return text;
        }

        /// <summary>Content root (shown with server data) + empty state (offline / server-only), both inside the body.</summary>
        private static RectTransform LiveOpsBody(RectTransform safe, out EmptyStateView empty)
        {
            RectTransform body = Body(safe);
            RectTransform content = UIKit.Node("Content", body).Fill();
            empty = UIKit.EmptyState(body);
            ((RectTransform)empty.transform).Fill();
            empty.gameObject.SetActive(false);
            return content;
        }

        private static TMP_Text HeaderNote(RectTransform bar, string name, float from, float to)
        {
            TMP_Text note = UIKit.Text(bar, name, null, TextStyle.Body, T.textSecondary, TextAlignmentOptions.MidlineRight);
            note.rectTransform.Span(new Vector2(from, 0f), new Vector2(to, 1f), Vector2.zero, new Vector2(-Gutter, 0f));
            return note;
        }

        // ---- Missions ------------------------------------------------------------------------------------------------

        public static MissionsScreen Missions(RectTransform parent, GameServices services)
        {
            var view = UIKit.Screen<MissionsScreen>(parent, "Missions", Transition.SlideLeft, true, out RectTransform safe);
            view.BindBack(UIKit.Header(safe, "missions.title", "missions", out RectTransform bar));
            TMP_Text reset = HeaderNote(bar, "Reset", 0.5f, 1f);
            RectTransform content = LiveOpsBody(safe, out EmptyStateView empty);

            TabBar tabs = UIKit.Tabs(content, "Tabs", new[] { "missions.daily", "missions.weekly" }, new[] { "daily", "missions" }, new Vector2(300f, 76f), false);
            ((RectTransform)tabs.transform).Span(new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -76f), Vector2.zero);
            RectTransform list = UIKit.Column(content, "List", 14f);
            list.Fill(0f, 56f, 0f, 96f);
            var rows = new List<Object>();
            for (int i = 0; i < MissionRows; i++) rows.Add(MissionRow(list, i));
            TMP_Text hint = Dynamic(UIKit.Text(content, "Hint", "missions.hint_daily", TextStyle.Caption, T.textSecondary, TextAlignmentOptions.Center));
            hint.rectTransform.Span(Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 44f));

            UIKit.Wire(view, "services", services, "body", content.gameObject, "empty", empty, "tabs", tabs, "rows", rows.ToArray(), "resetLabel", reset, "hintLabel", hint);
            return view;
        }

        private static MissionRowView MissionRow(RectTransform list, int index)
        {
            RowView row = UIKit.Row(list, "Mission " + index, null, "target", true, false, 124f);
            var root = (RectTransform)row.transform;
            // Right side: count, reroll, claim; the text and bar stop before them.
            foreach (string part in new[] { "Title", "Subtitle", "Bar" })
            {
                var rect = (RectTransform)root.Find(part);
                rect.offsetMax = new Vector2(-600f, rect.offsetMax.y);
            }

            var value = (RectTransform)root.Find("Value");
            value.offsetMin = new Vector2(-580f, value.offsetMin.y);
            value.offsetMax = new Vector2(-380f, value.offsetMax.y);
            ButtonParts reroll = UIKit.IconButton(root, "Reroll", "refresh", 76f);
            reroll.Rect.At(new Vector2(1f, 0.5f), new Vector2(-284f, 0f), new Vector2(76f, 76f), new Vector2(1f, 0.5f));
            ButtonParts claim = UIKit.Button(root, "Claim", ButtonKind.Primary, "missions.claim", null, new Vector2(240f, 84f));
            claim.Rect.At(new Vector2(1f, 0.5f), new Vector2(-24f, 0f), new Vector2(240f, 84f), new Vector2(1f, 0.5f));
            var view = root.gameObject.AddComponent<MissionRowView>();
            UIKit.Wire(view, "row", row, "claim", claim.Button, "claimLabel", Dynamic(claim.Label), "reroll", reroll.Button);
            return view;
        }

        // ---- Season Pass ---------------------------------------------------------------------------------------------

        public static PassScreen Pass(RectTransform parent, GameServices services)
        {
            var view = UIKit.Screen<PassScreen>(parent, "Season Pass", Transition.SlideLeft, true, out RectTransform safe);
            view.BindBack(UIKit.Header(safe, "pass.title", "pass", out RectTransform bar));
            TMP_Text timer = HeaderNote(bar, "Timer", 0.5f, 1f);
            RectTransform content = LiveOpsBody(safe, out EmptyStateView empty);

            RectTransform summary = UIKit.Card(content, "Summary", null, null);
            summary.Span(new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -196f), Vector2.zero);
            TMP_Text season = Dynamic(UIKit.Text(summary, "Season", null, TextStyle.H2, T.accent));
            season.rectTransform.Span(new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(176f, -64f), new Vector2(0f, -16f));
            Image badge = UIKit.Image(summary, "Level Badge", UIAssets.Circle, T.accent);
            badge.rectTransform.At(new Vector2(0f, 0.5f), new Vector2(28f, 0f), new Vector2(128f, 128f), new Vector2(0f, 0.5f));
            TMP_Text level = UIKit.Text(badge.transform, "Level", null, TextStyle.Display, T.textOnAccent, TextAlignmentOptions.Center);
            level.rectTransform.Fill(8f, 8f, 8f, 8f);
            ProgressBar xp = UIKit.Progress(summary, "XP", T.accent, 22f);
            ((RectTransform)xp.transform).Span(new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(176f, -24f), new Vector2(-720f, -2f));
            TMP_Text xpText = UIKit.Text(summary, "XP Text", null, TextStyle.Caption, T.textSecondary);
            xpText.rectTransform.Span(new Vector2(0f, 0f), new Vector2(1f, 0.5f), new Vector2(176f, 16f), new Vector2(-720f, -30f));

            ButtonParts premium = UIKit.Button(summary, "Premium", ButtonKind.Primary, "pass.buy_premium", "gem", new Vector2(380f, 100f), 20, 44f);
            premium.Rect.At(new Vector2(1f, 0.5f), new Vector2(-24f, 0f), new Vector2(380f, 100f), new Vector2(1f, 0.5f));
            GameObject premiumOn = UIKit.Tag(summary, "Premium Active", "pass.premium_on", T.success, new Vector2(1f, 0.5f), new Vector2(-24f, 0f));
            ((RectTransform)premiumOn.transform).sizeDelta = new Vector2(260f, 56f);
            ButtonParts claimAll = UIKit.Button(summary, "Claim All", ButtonKind.Secondary, "pass.claim_all", "gift", new Vector2(300f, 100f), 20, 40f);
            claimAll.Rect.At(new Vector2(1f, 0.5f), new Vector2(-424f, 0f), new Vector2(300f, 100f), new Vector2(1f, 0.5f));

            RectTransform track = UIKit.Node("Track", content).Fill(0f, 0f, 0f, 216f);
            TrackLabel(track, "Free Label", "pass.track_free", T.textSecondary, -96f - 56f);
            TrackLabel(track, "Premium Label", "pass.track_premium", T.accent, -96f - 56f - 248f);
            ScrollRect scroll = UIKit.Scroll(track, "Levels", false, out RectTransform columns);
            ((RectTransform)scroll.transform).Fill(196f, 0f, 0f, 0f);
            var row = columns.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 16f;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = false;
            row.padding = new RectOffset(8, 40, 0, 0);
            PassColumnView template = PassColumn(columns);

            UIKit.Wire(view, "services", services, "body", content.gameObject, "empty", empty, "seasonLabel", season, "timerLabel", timer, "levelLabel", level,
                "xpLabel", xpText, "xpBar", xp, "premiumButton", premium.Button, "premiumLabel", Dynamic(premium.Label), "premiumActive", premiumOn,
                "claimAllButton", claimAll.Button, "scroll", scroll, "columnsRoot", columns, "columnTemplate", template, "gemSprite", UIAssets.Icon("gem"));
            return view;
        }

        private static void TrackLabel(RectTransform track, string name, string key, Color color, float centreY)
        {
            Image back = UIKit.Panel(track, name, T.surfaceSunken, 20, false, true);
            var root = (RectTransform)back.transform.parent;
            root.At(new Vector2(0f, 1f), new Vector2(0f, centreY), new Vector2(180f, 120f), new Vector2(0f, 0.5f));
            TMP_Text label = UIKit.Text(root, "Label", key, TextStyle.Button, color, TextAlignmentOptions.Center, true);
            label.fontSize = label.fontSizeMax = 24f;
            label.rectTransform.Fill(10f, 0f, 10f, 0f);
        }

        private static PassColumnView PassColumn(RectTransform columns)
        {
            RectTransform column = UIKit.Node("Level Template", columns);
            UIKit.Size(column, 190f, 560f);
            column.sizeDelta = new Vector2(190f, 560f);
            Image plate = UIKit.Image(column, "Plate", UIAssets.Pill, T.surfaceRaised);
            plate.rectTransform.At(new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(120f, 56f), new Vector2(0.5f, 1f));
            TMP_Text level = UIKit.Text(plate.transform, "Level", null, TextStyle.Number, T.textPrimary, TextAlignmentOptions.Center);
            level.fontSize = level.fontSizeMax = 30f;
            level.rectTransform.Fill();
            RewardTileView free = RewardTile(column, "Free", -80f);
            RewardTileView premium = RewardTile(column, "Premium", -328f);
            var view = column.gameObject.AddComponent<PassColumnView>();
            UIKit.Wire(view, "levelLabel", level, "plate", plate, "free", free, "premium", premium);
            return view;
        }

        private static RewardTileView RewardTile(RectTransform column, string name, float top)
        {
            RectTransform root = UIKit.Node(name, column).At(new Vector2(0.5f, 1f), new Vector2(0f, top), new Vector2(180f, 228f), new Vector2(0.5f, 1f));
            Image glow = UIKit.Image(root, "Glow", UIAssets.Glow, new Color(T.accent.r, T.accent.g, T.accent.b, 0.55f));
            glow.rectTransform.Fill(-34f, -34f, -34f, -34f);
            Image frame = UIKit.Image(root, "Frame", UIAssets.Rounded(20), Color.white, true);
            frame.rectTransform.Fill();
            UIKit.Image(root, "Inner", UIAssets.Rounded(14), new Color(0.06f, 0.08f, 0.13f)).rectTransform.Fill(4f, 4f, 4f, 4f);
            Image icon = UIKit.Image(root, "Icon", null, Color.white);
            icon.preserveAspect = true;
            icon.rectTransform.Span(new Vector2(0f, 0.3f), Vector2.one, new Vector2(22f, 0f), new Vector2(-22f, -18f));
            TMP_Text amount = UIKit.Text(root, "Amount", null, TextStyle.Number, T.textPrimary, TextAlignmentOptions.Center);
            amount.fontSize = amount.fontSizeMax = 30f;
            amount.rectTransform.Span(Vector2.zero, new Vector2(1f, 0.3f), new Vector2(8f, 6f), new Vector2(-8f, 0f));
            Image locked = UIKit.Image(root, "Lock", UIAssets.Rounded(20), new Color(0f, 0f, 0f, 0.5f));
            locked.rectTransform.Fill();
            UIKit.Icon(locked.transform, "lock", 44f, new Color(1f, 1f, 1f, 0.85f)).rectTransform.At(Vector2.one, new Vector2(-12f, -12f), new Vector2(44f, 44f), Vector2.one);
            Image claimed = UIKit.Image(root, "Claimed", UIAssets.Circle, T.success);
            claimed.rectTransform.At(Vector2.one, new Vector2(-10f, -10f), new Vector2(48f, 48f), Vector2.one);
            UIKit.Icon(claimed.transform, "check", 32f, Color.white).rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(32f, 32f));
            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = frame;
            button.transition = Selectable.Transition.None;
            var view = root.gameObject.AddComponent<RewardTileView>();
            UIKit.Wire(view, "frame", frame, "icon", icon, "amount", amount, "lockMark", locked.gameObject, "claimedMark", claimed.gameObject, "glow", glow, "button", button,
                "coinSprite", UIAssets.Icon("coin"), "gemSprite", UIAssets.Icon("gem"), "shardSprite", UIAssets.Icon("upgrade"),
                "crystalSprite", UIAssets.Icon("crown"), "keySprite", UIAssets.Icon("crate"));
            return view;
        }

        // ---- Events --------------------------------------------------------------------------------------------------

        public static EventsScreen Events(RectTransform parent, GameServices services, ScreenView crates)
        {
            var view = UIKit.Screen<EventsScreen>(parent, "Events", Transition.SlideLeft, true, out RectTransform safe);
            view.BindBack(UIKit.Header(safe, "events.title", "events", out _));
            RectTransform content = LiveOpsBody(safe, out EmptyStateView empty);
            ScrollRect scroll = UIKit.Scroll(content, "Scroll", true, out RectTransform list);
            ((RectTransform)scroll.transform).Fill();
            Stack(list.gameObject.AddComponent<VerticalLayoutGroup>(), 20f);
            EventCardView template = EventCard(list);
            TMP_Text none = UIKit.Text(content, "None", "events.none", TextStyle.H2, T.textSecondary, TextAlignmentOptions.Center);
            none.rectTransform.Fill();
            UIKit.Wire(view, "services", services, "body", content.gameObject, "empty", empty, "list", list, "cardTemplate", template, "noneLabel", none.gameObject, "crates", crates);
            return view;
        }

        private static EventCardView EventCard(RectTransform list)
        {
            RectTransform card = UIKit.Card(list, "Event Template", null, null);
            UIKit.Size(card, -1f, 220f);
            Image accent = UIKit.Image(card, "Accent", UIAssets.Pill, T.accent);
            accent.rectTransform.Span(Vector2.zero, new Vector2(0f, 1f), new Vector2(12f, 20f), new Vector2(22f, -20f));
            Image glow = UIKit.Image(card, "Glow", UIAssets.Glow, new Color(T.accent.r, T.accent.g, T.accent.b, 0.25f));
            glow.rectTransform.At(new Vector2(0f, 0.5f), new Vector2(-10f, 0f), new Vector2(280f, 280f), new Vector2(0f, 0.5f));
            Image icon = UIKit.Icon(card, "events", 132f, T.accent);
            icon.rectTransform.At(new Vector2(0f, 0.5f), new Vector2(52f, 0f), new Vector2(132f, 132f), new Vector2(0f, 0.5f));
            TMP_Text title = UIKit.Text(card, "Title", null, TextStyle.H1, T.textPrimary);
            title.rectTransform.Span(new Vector2(0f, 1f), Vector2.one, new Vector2(220f, -84f), new Vector2(-420f, -22f));
            TMP_Text body = UIKit.Wrap(UIKit.Text(card, "Body", null, TextStyle.Body, T.textSecondary, TextAlignmentOptions.TopLeft));
            body.rectTransform.Span(Vector2.zero, Vector2.one, new Vector2(220f, 60f), new Vector2(-420f, -92f));
            TMP_Text timer = UIKit.Text(card, "Timer", null, TextStyle.Caption, T.textSecondary);
            timer.rectTransform.Span(Vector2.zero, new Vector2(1f, 0f), new Vector2(220f, 16f), new Vector2(-420f, 56f));
            TMP_Text boost = UIKit.Text(card, "Boost", null, TextStyle.Display, T.coin, TextAlignmentOptions.MidlineRight);
            boost.rectTransform.Span(new Vector2(1f, 0.5f), Vector2.one, new Vector2(-400f, 0f), new Vector2(-32f, -20f));
            GameObject live = UIKit.Tag(card, "Live", "event.live", T.danger, new Vector2(0f, 1f), new Vector2(40f, 14f));
            ButtonParts open = UIKit.Button(card, "Open Crates", ButtonKind.Primary, "event.open_crates", "crate", new Vector2(340f, 84f));
            open.Rect.At(new Vector2(1f, 0f), new Vector2(-32f, 22f), new Vector2(340f, 84f), new Vector2(1f, 0f));
            var view = card.gameObject.AddComponent<EventCardView>();
            UIKit.Wire(view, "icon", icon, "accent", accent, "title", title, "body", body, "timer", timer, "boost", boost, "liveTag", live, "action", open.Button,
                "eventSprite", UIAssets.Icon("events"), "crateSprite", UIAssets.Icon("crate"));
            return view;
        }

        // ---- Daily shop ----------------------------------------------------------------------------------------------

        public static ShopScreen Shop(RectTransform parent, GameServices services)
        {
            var view = UIKit.Screen<ShopScreen>(parent, "Shop", Transition.SlideLeft, true, out RectTransform safe);
            view.BindBack(UIKit.Header(safe, "shop.title", "shop", out RectTransform bar));
            RectTransform wallet = UIKit.Row(bar, "Wallet", 16f, TextAnchor.MiddleRight, false);
            wallet.Span(new Vector2(0.55f, 0f), Vector2.one, Vector2.zero, new Vector2(-Gutter, 0f));
            CurrencyPill coins = UIKit.Pill(wallet, "Coins", "coin", T.coin, 250f);
            UIKit.Size(coins, 250f, 72f);
            CurrencyPill gems = UIKit.Pill(wallet, "Gems", "gem", T.gem, 230f);
            UIKit.Size(gems, 230f, 72f);
            RectTransform content = LiveOpsBody(safe, out EmptyStateView empty);

            TMP_Text reset = UIKit.Text(content, "Reset", null, TextStyle.Body, T.textSecondary);
            reset.rectTransform.Span(new Vector2(0f, 1f), Vector2.one, new Vector2(8f, -56f), Vector2.zero);
            RectTransform row = UIKit.Row(content, "Offers", 20f, TextAnchor.MiddleCenter, false);
            row.Fill(0f, 0f, 0f, 64f);
            var offers = new List<Object>();
            for (int i = 0; i < ShopSlots; i++) offers.Add(Offer(row, i));
            TMP_Text none = UIKit.Text(content, "None", "shop.none", TextStyle.H2, T.textSecondary, TextAlignmentOptions.Center);
            none.rectTransform.Fill();
            UIKit.Wire(view, "services", services, "body", content.gameObject, "empty", empty, "offers", offers.ToArray(), "resetLabel", reset,
                "coins", coins, "gems", gems, "noneLabel", none.gameObject);
            return view;
        }

        private static OfferView Offer(RectTransform row, int index)
        {
            RectTransform root = UIKit.Node("Offer " + index, row);
            UIKit.Size(root, 260f, 470f);
            root.sizeDelta = new Vector2(260f, 470f);
            ItemTile tile = UIKit.Tile(root, "Tile");
            ((RectTransform)tile.transform).At(new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(240f, 300f), new Vector2(0.5f, 1f));
            GameObject featured = UIKit.Tag(root, "Featured", "shop.featured", T.accentDeep, new Vector2(0.5f, 1f), new Vector2(0f, -6f));
            ((RectTransform)featured.transform).sizeDelta = new Vector2(180f, 36f);
            Image sold = UIKit.Image(tile.transform, "Sold", UIAssets.Rounded(20), new Color(0f, 0f, 0f, 0.6f));
            sold.rectTransform.Fill();
            UIKit.Text(sold.transform, "Label", "shop.sold", TextStyle.H2, T.success, TextAlignmentOptions.Center, true).rectTransform.Fill();
            ButtonParts buy = PriceButton((RectTransform)root, "Buy", ButtonKind.Primary, new Vector2(18f, 24f), out TMP_Text price, out Image currency);
            var view = root.gameObject.AddComponent<OfferView>();
            UIKit.Wire(view, "tile", tile, "buy", buy.Button, "price", price, "currency", currency, "featured", featured, "sold", sold.gameObject,
                "coinSprite", UIAssets.Icon("coin"), "gemSprite", UIAssets.Icon("gem"));
            return view;
        }
    }
}
