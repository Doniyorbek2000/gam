using System.Collections.Generic;
using BilliardGame.Content;
using BilliardGame.Meta;
using BilliardGame.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BilliardGame.Editor.UI
{
    /// <summary>Lobby screens built from the design-system kit: mode select, profile, settings (this file).</summary>
    internal static partial class LobbyScreens
    {
        private const float Gutter = UIKit.Gutter;
        private const float Header = UIKit.HeaderHeight;
        private const float SideTabs = 300f;

        private static UITheme T => UIKit.Theme;

        private static RectTransform Body(RectTransform safe, float top = Header + 8f, float bottom = Gutter) =>
            UIKit.Node("Body", safe).Fill(Gutter, bottom, Gutter, top);

        // ---- Mode select ---------------------------------------------------------------------------------------------

        private static readonly (string icon, string key)[] Modes =
        {
            ("friends", "mode.hotseat"), ("link", "mode.friend"), ("bolt", "mode.quick"), ("ranked", "mode.ranked"), ("controls", "mode.ai"), ("target", "mode.practice"),
        };

        public static ModeSelectScreen ModeSelect(RectTransform parent, LobbyController lobby, GameServices services)
        {
            var view = UIKit.Screen<ModeSelectScreen>(parent, "Mode Select", Transition.SlideLeft, true, out RectTransform safe);
            view.BindBack(UIKit.Header(safe, "mode.title", "eight_ball", out _));
            RectTransform body = Body(safe, Header + 8f, Gutter + 112f);

            UIKit.Text(body, "Modes Title", "mode.section", TextStyle.Caption, T.textSecondary, TextAlignmentOptions.MidlineLeft, true)
                .rectTransform.Span(new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -36f), Vector2.zero);
            ScrollRect modeScroll = UIKit.Scroll(body, "Modes", false, out RectTransform modeContent);
            ((RectTransform)modeScroll.transform).Span(new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -36f - 228f), new Vector2(0f, -36f));
            var modeRow = modeContent.gameObject.AddComponent<HorizontalLayoutGroup>();
            modeRow.spacing = 16f;
            modeRow.childControlWidth = modeRow.childControlHeight = true;
            modeRow.childForceExpandWidth = modeRow.childForceExpandHeight = false;
            modeRow.padding = new RectOffset(4, 4, 6, 6);
            var cards = new List<Object>();
            for (int i = 0; i < Modes.Length; i++)
            {
                bool playable = LobbyController.IsPlayable((LobbyController.Mode)i);
                ButtonParts card = UIKit.IconCard(modeContent, "Mode " + Modes[i].key, Modes[i].icon, Modes[i].key, new Vector2(248f, 212f), ButtonKind.Secondary, 72f,
                    playable ? T.accent : T.textMuted);
                UIKit.Size(card.Rect, 248f, 212f);
                card.Label.fontSize = card.Label.fontSizeMax = 24f;
                TMP_Text desc = UIKit.Wrap(UIKit.Text(card.Rect.Find("Content"), "Desc", Modes[i].key + ".desc", TextStyle.Caption, T.textSecondary, TextAlignmentOptions.Center));
                desc.fontSize = desc.fontSizeMax = 18f;
                desc.enableAutoSizing = false;
                UIKit.Size(desc, 220f, 48f);
                if (!playable) UIKit.SoonTag(card.Rect);
                cards.Add(card.Button);
            }

            UIKit.Text(body, "Venues Title", "mode.venue", TextStyle.Caption, T.textSecondary, TextAlignmentOptions.MidlineLeft, true)
                .rectTransform.Span(new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -320f), new Vector2(0f, -284f));
            ScrollRect venueScroll = UIKit.Scroll(body, "Venues", false, out RectTransform venueContent);
            ((RectTransform)venueScroll.transform).Span(Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, -324f));
            var venueRow = venueContent.gameObject.AddComponent<HorizontalLayoutGroup>();
            venueRow.spacing = 20f;
            venueRow.childControlWidth = venueRow.childControlHeight = true;
            venueRow.childForceExpandWidth = venueRow.childForceExpandHeight = false;
            venueRow.childAlignment = TextAnchor.MiddleLeft;
            venueRow.padding = new RectOffset(4, 4, 8, 8);
            var venues = new List<Object>();
            foreach (VenueDefinition hall in services.Catalog.Venues) venues.Add(VenueCard(venueContent, hall));

            TMP_Text fair = UIKit.Wrap(UIKit.Text(safe, "Fair Play", "mode.fair", TextStyle.Caption, T.textSecondary));
            fair.rectTransform.Span(Vector2.zero, new Vector2(0.62f, 0f), new Vector2(Gutter, Gutter), new Vector2(0f, Gutter + 80f));
            ButtonParts select = UIKit.Button(safe, "Select", ButtonKind.Primary, "mode.select", "check", new Vector2(380f, 96f));
            select.Rect.At(new Vector2(1f, 0f), new Vector2(-Gutter, Gutter), new Vector2(380f, 96f), new Vector2(1f, 0f));
            UIKit.Wire(view, "lobby", lobby, "services", services, "modeCards", cards.ToArray(), "venueCards", venues.ToArray(), "selectButton", select.Button);
            return view;
        }

        private static VenueCardView VenueCard(RectTransform parent, VenueDefinition hall)
        {
            ButtonParts card = UIKit.Button(parent, "Venue " + hall.Id, ButtonKind.Secondary, null, null, new Vector2(420f, 300f), 20);
            Object.DestroyImmediate(card.Label.gameObject);
            UIKit.Size(card.Rect, 420f, 300f);
            Image picture = UIKit.Image(card.Rect, "Picture", hall.Preview, Color.white);
            picture.rectTransform.Span(new Vector2(0f, 1f), Vector2.one, new Vector2(8f, -204f), new Vector2(-8f, -8f));
            Image fade = UIKit.Image(picture.transform, "Fade", UIAssets.GradientUp, new Color(0f, 0f, 0f, 0.7f));
            fade.rectTransform.Span(Vector2.zero, new Vector2(1f, 0.5f), Vector2.zero, Vector2.zero);
            fade.rectTransform.localScale = new Vector3(1f, -1f, 1f);
            TMP_Text title = UIKit.Text(card.Rect, "Name", null, TextStyle.H2);
            title.fontSize = title.fontSizeMax = 30f;
            title.rectTransform.Span(new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(20f, 56f), new Vector2(-20f, 96f));
            TMP_Text entry = MoneyRow(card.Rect, "Entry", "ticket", "lobby.entry", T.textSecondary, new Vector2(20f, 12f));
            TMP_Text prize = MoneyRow(card.Rect, "Prize", "trophy", "lobby.prize", T.coin, new Vector2(220f, 12f));
            Image lockOverlay = UIKit.Image(card.Rect, "Lock", UIAssets.Rounded(20), new Color(0f, 0f, 0f, 0.62f));
            lockOverlay.rectTransform.Fill();
            UIKit.Icon(lockOverlay.transform, "lock", 64f, Color.white).rectTransform.At(new Vector2(0.5f, 0.74f), Vector2.zero, new Vector2(64f, 64f));
            TMP_Text lockText = UIKit.Text(lockOverlay.transform, "Lock Text", null, TextStyle.Button, Color.white, TextAlignmentOptions.Center);
            lockText.rectTransform.Span(new Vector2(0f, 0.44f), new Vector2(1f, 0.6f), Vector2.zero, Vector2.zero);
            var view = card.Rect.gameObject.AddComponent<VenueCardView>();
            view.Configure(hall, picture, title, entry, prize, lockOverlay.gameObject, lockText, card.Button, card.Rect.GetComponent<UIButtonFx>());
            return view;
        }

        private static TMP_Text MoneyRow(RectTransform parent, string name, string icon, string key, Color color, Vector2 position)
        {
            UIKit.Icon(parent, icon, 26f, color).rectTransform.At(Vector2.zero, position + new Vector2(0f, 4f), new Vector2(26f, 26f), Vector2.zero);
            TMP_Text caption = UIKit.Text(parent, name + " Caption", key, TextStyle.Caption, T.textSecondary);
            caption.fontSize = caption.fontSizeMax = 18f;
            caption.rectTransform.At(Vector2.zero, position + new Vector2(32f, 20f), new Vector2(150f, 24f), Vector2.zero);
            TMP_Text value = UIKit.Text(parent, name, null, TextStyle.Number, color);
            value.fontSize = value.fontSizeMax = 24f;
            value.fontSizeMin = 14f;
            value.rectTransform.At(Vector2.zero, position + new Vector2(32f, -8f), new Vector2(150f, 32f), Vector2.zero);
            return value;
        }

        // ---- Profile -------------------------------------------------------------------------------------------------

        private static readonly (string icon, string key)[] Stats =
        {
            ("eight_ball", "stat.matches"), ("trophy", "stat.wins"), ("stats", "stat.winrate"), ("streak", "stat.streak"),
            ("accuracy", "stat.accuracy"), ("rack", "stat.multi"), ("cue_icon", "stat.favourite"),
        };

        private static readonly ItemType[] CollectionTypes =
        {
            ItemType.Cue, ItemType.Table, ItemType.Sticker, ItemType.Frame, ItemType.RoomWall, ItemType.RoomFloor, ItemType.Balls, ItemType.Outfit, ItemType.Emote,
        };

        private static readonly string[] CollectionIcons = { "cue_icon", "table", "sticker", "frame", "wall", "floor", "eight_ball", "outfit", "emote" };

        public static ProfileScreen Profile(RectTransform parent, GameServices services)
        {
            var view = UIKit.Screen<ProfileScreen>(parent, "Profile", Transition.SlideLeft, true, out RectTransform safe);
            view.BindBack(UIKit.Header(safe, "profile.title", "person", out _));
            RectTransform body = Body(safe);

            // Left: identity card.
            RectTransform card = UIKit.Card(body, "Identity", null, null);
            card.Span(Vector2.zero, new Vector2(0f, 1f), Vector2.zero, new Vector2(520f, 0f));
            Image portrait = UIKit.Avatar(card, "Avatar", 168f, out Image frame, out TMP_Text level);
            ((RectTransform)portrait.transform.parent).At(new Vector2(0.5f, 1f), new Vector2(0f, -32f), new Vector2(168f, 168f), new Vector2(0.5f, 1f));
            TMP_Text nickname = UIKit.Text(card, "Nickname", null, TextStyle.H1, null, TextAlignmentOptions.Center);
            nickname.rectTransform.Span(new Vector2(0f, 1f), Vector2.one, new Vector2(70f, -290f), new Vector2(-70f, -232f));
            ButtonParts edit = UIKit.IconButton(card, "Edit Nickname", "edit", 64f);
            edit.Rect.At(Vector2.one, new Vector2(-12f, -228f), new Vector2(64f, 64f), Vector2.one);
            TMP_Text id = UIKit.Text(card, "Player Id", null, TextStyle.Caption, T.textSecondary, TextAlignmentOptions.Center);
            id.rectTransform.Span(new Vector2(0f, 1f), Vector2.one, new Vector2(70f, -340f), new Vector2(-70f, -298f));
            ButtonParts copy = UIKit.IconButton(card, "Copy Id", "copy", 56f);
            copy.Rect.At(Vector2.one, new Vector2(-16f, -292f), new Vector2(56f, 56f), Vector2.one);
            ProgressBar xp = UIKit.Progress(card, "XP", T.secondary, 16f);
            ((RectTransform)xp.transform).Span(new Vector2(0f, 1f), Vector2.one, new Vector2(40f, -388f), new Vector2(-40f, -372f));
            TMP_Text xpText = UIKit.Text(card, "XP Text", null, TextStyle.Caption, T.textSecondary, TextAlignmentOptions.Center);
            xpText.rectTransform.Span(new Vector2(0f, 1f), Vector2.one, new Vector2(40f, -428f), new Vector2(-40f, -394f));
            RectTransform facts = UIKit.Column(card, "Facts", 8f);
            facts.Span(Vector2.zero, new Vector2(1f, 0f), new Vector2(24f, 24f), new Vector2(-24f, 230f));
            TMP_Text rank = Fact(facts, "ranked", T.accent);
            TMP_Text country = Fact(facts, "flag", T.textPrimary);
            TMP_Text club = Fact(facts, "club", T.textPrimary);

            // Right: tabs + pages.
            RectTransform right = UIKit.Node("Right", body).Fill(544f, 0f, 0f, 0f);
            TabBar tabs = UIKit.Tabs(right, "Tabs", new[] { "profile.tab_stats", "profile.tab_history", "profile.tab_achievements", "profile.tab_collection" },
                new[] { "stats", "history", "trophy", "inventory" }, new Vector2(250f, 76f), false, 10f);
            ((RectTransform)tabs.transform).Span(new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -76f), Vector2.zero);
            RectTransform pages = UIKit.Node("Pages", right).Fill(0f, 0f, 0f, 96f);

            RectTransform stats = UIKit.Node("Stats", pages).Fill();
            var grid = stats.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(300f, 132f);
            grid.spacing = new Vector2(16f, 16f);
            var statValues = new List<Object>();
            foreach (var stat in Stats) statValues.Add(UIKit.StatCard(stats, "Stat " + stat.key, stat.icon, stat.key));

            RectTransform history = UIKit.Node("History", pages).Fill();
            ScrollRect historyScroll = UIKit.Scroll(history, "Scroll", true, out RectTransform historyContent);
            ((RectTransform)historyScroll.transform).Fill();
            Stack(historyContent.gameObject.AddComponent<VerticalLayoutGroup>(), 12f);
            RowView historyRow = UIKit.Row(historyContent, "Match Template", null, null, false, false);
            EmptyStateView historyEmpty = UIKit.EmptyState(history);
            ((RectTransform)historyEmpty.transform).Fill();

            RectTransform achievements = UIKit.Node("Achievements", pages).Fill();
            RectTransform showcase = UIKit.Card(achievements, "Showcase", "achievements.showcase", "star");
            showcase.Span(new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -176f), Vector2.zero);
            RectTransform slots = UIKit.Row(showcase, "Slots", 16f, TextAnchor.MiddleLeft, false);
            slots.Span(Vector2.zero, Vector2.one, new Vector2(24f, 12f), new Vector2(-24f, -64f));
            var showIcons = new List<Object>();
            var showNames = new List<Object>();
            for (int i = 0; i < 3; i++)
            {
                Image slot = UIKit.Panel(slots, "Badge " + i, T.surfaceSunken, 20, false, true);
                RectTransform slotRoot = (RectTransform)slot.transform.parent;
                UIKit.Size(slotRoot, 300f, 92f);
                Image glyph = UIKit.Icon(slotRoot, "trophy", 56f, T.accent);
                glyph.rectTransform.At(new Vector2(0f, 0.5f), new Vector2(16f, 0f), new Vector2(56f, 56f), new Vector2(0f, 0.5f));
                TMP_Text label = UIKit.Text(slotRoot, "Name", null, TextStyle.Body);
                label.rectTransform.Span(Vector2.zero, Vector2.one, new Vector2(84f, 0f), new Vector2(-12f, 0f));
                showIcons.Add(glyph);
                showNames.Add(label);
            }

            TMP_Text showHint = UIKit.Text(showcase, "Hint", "achievements.showcase_empty", TextStyle.Caption, T.textSecondary);
            showHint.rectTransform.Span(Vector2.zero, Vector2.one, new Vector2(24f, 12f), new Vector2(-24f, -64f));
            ScrollRect achScroll = UIKit.Scroll(achievements, "Scroll", true, out RectTransform achContent);
            ((RectTransform)achScroll.transform).Fill(0f, 0f, 0f, 192f);
            Stack(achContent.gameObject.AddComponent<VerticalLayoutGroup>(), 12f);
            var achRows = new List<Object>();
            foreach (AchievementDef def in Achievements.All) achRows.Add(UIKit.Row(achContent, "Achievement " + def.Id, def.Id, def.Icon, true, true));

            RectTransform collection = UIKit.Node("Collection", pages).Fill();
            ScrollRect colScroll = UIKit.Scroll(collection, "Scroll", true, out RectTransform colContent);
            ((RectTransform)colScroll.transform).Fill();
            Stack(colContent.gameObject.AddComponent<VerticalLayoutGroup>(), 12f);
            var colRows = new List<Object>();
            for (int i = 0; i < CollectionTypes.Length; i++) colRows.Add(UIKit.Row(colContent, "Collection " + CollectionTypes[i], CollectionTypes[i].ToString(), CollectionIcons[i], true, false, 88f));

            UIKit.Wire(view, "services", services, "theme", T,
                "avatarFrame", frame, "levelChip", level, "nickname", nickname, "editNickname", edit.Button, "playerId", id, "copyId", copy.Button,
                "rank", rank, "country", country, "club", club, "xpBar", xp, "xpText", xpText,
                "tabs", tabs, "pages", new Object[] { stats.gameObject, history.gameObject, achievements.gameObject, collection.gameObject },
                "statValues", statValues.ToArray(), "historyTemplate", historyRow, "historyEmpty", historyEmpty,
                "achievementRows", achRows.ToArray(), "showcaseIcons", showIcons.ToArray(), "showcaseNames", showNames.ToArray(), "showcaseHint", showHint,
                "collectionRows", colRows.ToArray());
            return view;
        }

        private static TMP_Text Fact(RectTransform column, string icon, Color color)
        {
            RectTransform row = UIKit.Node("Fact " + icon, column);
            UIKit.Size(row, -1f, 56f);
            UIKit.Icon(row, icon, 40f, T.accent).rectTransform.At(new Vector2(0f, 0.5f), Vector2.zero, new Vector2(40f, 40f), new Vector2(0f, 0.5f));
            TMP_Text text = UIKit.Text(row, "Text", null, TextStyle.Body, color);
            text.rectTransform.Span(Vector2.zero, Vector2.one, new Vector2(56f, 0f), Vector2.zero);
            return text;
        }

        private static void Stack(VerticalLayoutGroup list, float spacing)
        {
            list.spacing = spacing;
            list.childControlWidth = list.childControlHeight = true;
            list.childForceExpandWidth = true;
            list.childForceExpandHeight = false;
            list.padding = new RectOffset(4, 4, 4, 24);
        }

        // ---- Settings ------------------------------------------------------------------------------------------------

        public static SettingsScreen Settings(RectTransform parent, GameServices services, CreditsScreen credits)
        {
            var view = UIKit.Screen<SettingsScreen>(parent, "Settings", Transition.SlideLeft, true, out RectTransform safe);
            view.BindBack(UIKit.Header(safe, "settings.title", "settings", out _));
            RectTransform body = Body(safe);
            TabBar tabs = UIKit.Tabs(body, "Tabs", new[] { "settings.tab_graphics", "settings.tab_sound", "settings.tab_controls", "settings.tab_language", "settings.tab_account", "settings.tab_other" },
                new[] { "graphics", "volume", "controls", "language", "account", "info" }, new Vector2(SideTabs, 84f), true, 10f);
            ((RectTransform)tabs.transform).Span(Vector2.zero, new Vector2(0f, 1f), Vector2.zero, new Vector2(SideTabs, 0f));
            RectTransform area = UIKit.Node("Pages", body).Fill(SideTabs + 32f, 0f, 0f, 0f);

            RectTransform graphics = Page(area, "Graphics");
            TabBar quality = UIKit.Segmented(UIKit.SettingRow(graphics, "Quality", "quality", "settings.quality", null, 600f), "Quality",
                new[] { "settings.quality_low", "settings.quality_medium", "settings.quality_high", "settings.quality_auto" }, 140f);
            TabBar fps = UIKit.Segmented(UIKit.SettingRow(graphics, "FPS", "fps", "settings.fps", null, 600f), "FPS", new string[4], 140f);
            UIKit.Literal(fps, new[] { "30", "60", "90", "120" });
            ToggleSwitch shadows = ToggleRow(graphics, "shadows", "settings.shadows");
            ToggleSwitch post = ToggleRow(graphics, "effects_fx", "settings.post");
            ToggleSwitch battery = ToggleRow(graphics, "battery", "settings.battery", "settings.battery_hint");

            RectTransform sound = Page(area, "Sound");
            Slider master = SliderRow(sound, "volume", "settings.master", 0f, 1f);
            Slider music = SliderRow(sound, "music", "settings.music", 0f, 1f);
            Slider effects = SliderRow(sound, "sfx", "settings.effects", 0f, 1f);
            Slider voice = SliderRow(sound, "mic", "settings.voice", 0f, 1f);
            ToggleSwitch vibration = ToggleRow(sound, "vibration", "settings.vibration");

            RectTransform controls = Page(area, "Controls");
            Slider aim = SliderRow(controls, "touch", "settings.aim_sens", 0.5f, 2f);
            Slider fine = SliderRow(controls, "controls", "settings.fine_sens", 0.5f, 2f);
            ToggleSwitch left = ToggleRow(controls, "left_hand", "settings.left_hand", "settings.left_hand_hint");
            ToggleSwitch guide = ToggleRow(controls, "visibility", "settings.aim_guide");
            ToggleSwitch confirm = ToggleRow(controls, "check", "settings.confirm_shot", "settings.confirm_shot_hint");

            RectTransform language = Page(area, "Language");
            RectTransform langRow = UIKit.SettingRow(language, "Language", "language", "settings.language", null, 0f);
            UIKit.Size(langRow.parent, -1f, 92f);
            var languages = new List<Object>();
            foreach (var (code, label) in new[] { ("uz", "O'zbek"), ("ru", "Русский"), ("en", "English") })
            {
                ButtonParts option = UIKit.Button(language, "Language " + code, ButtonKind.Secondary, null, null, new Vector2(600f, 100f));
                option.Label.text = label;
                UIKit.Size(option.Rect, -1f, 100f);
                languages.Add(option.Button);
            }

            RectTransform account = Page(area, "Account");
            RectTransform idSlot = UIKit.SettingRow(account, "Account Id", "account", "settings.account_id", null, 520f);
            TMP_Text accountId = UIKit.Text(idSlot, "Id", null, TextStyle.Number, T.textPrimary, TextAlignmentOptions.MidlineRight);
            accountId.rectTransform.Fill(0f, 0f, 84f, 0f);
            ButtonParts copy = UIKit.IconButton(idSlot, "Copy", "copy", 64f);
            copy.Rect.At(new Vector2(1f, 0.5f), Vector2.zero, new Vector2(64f, 64f), new Vector2(1f, 0.5f));
            Button google = ActionRow(account, "link", "settings.google", ButtonKind.Secondary, "common.soon");
            Button logout = ActionRow(account, "logout", "settings.logout", ButtonKind.Secondary, "settings.logout");
            Button delete = ActionRow(account, "delete", "settings.delete", ButtonKind.Danger, "settings.delete");

            RectTransform other = Page(area, "Other");
            ToggleSwitch notifications = ToggleRow(other, "notifications", "settings.notifications");
            Button privacy = ActionRow(other, "privacy", "settings.privacy", ButtonKind.Ghost, null);
            Button terms = ActionRow(other, "terms", "settings.terms", ButtonKind.Ghost, null);
            Button help = ActionRow(other, "help", "settings.help", ButtonKind.Ghost, null);
            Button creditsButton = ActionRow(other, "credits", "settings.credits", ButtonKind.Ghost, null);
            RectTransform versionSlot = UIKit.SettingRow(other, "Version", "version", "settings.version", null, 420f);
            TMP_Text version = UIKit.Text(versionSlot, "Version", null, TextStyle.Caption, T.textSecondary, TextAlignmentOptions.MidlineRight);
            version.rectTransform.Fill();

            UIKit.Wire(view, "services", services, "credits", credits, "tabs", tabs,
                "pages", new Object[] { graphics.parent.gameObject, sound.parent.gameObject, controls.parent.gameObject, language.parent.gameObject, account.parent.gameObject, other.parent.gameObject },
                "quality", quality, "fps", fps, "shadows", shadows, "postProcessing", post, "batterySaver", battery,
                "master", master, "music", music, "effects", effects, "voice", voice, "vibration", vibration,
                "aimSensitivity", aim, "fineSensitivity", fine, "leftHanded", left, "aimGuide", guide, "confirmShot", confirm,
                "languages", languages.ToArray(), "accountId", accountId, "copyId", copy.Button, "linkGoogle", google, "logout", logout, "deleteAccount", delete,
                "notifications", notifications, "privacy", privacy, "terms", terms, "help", help, "creditsButton", creditsButton, "version", version);
            return view;
        }

        /// <summary>Scrollable settings page; returns the list content (page root = content.parent, the scroll view).</summary>
        private static RectTransform Page(RectTransform area, string name)
        {
            ScrollRect scroll = UIKit.Scroll(area, name, true, out RectTransform content);
            ((RectTransform)scroll.transform).Fill();
            Stack(content.gameObject.AddComponent<VerticalLayoutGroup>(), 12f);
            return content;
        }

        private static ToggleSwitch ToggleRow(RectTransform page, string icon, string key, string hint = null)
        {
            RectTransform slot = UIKit.SettingRow(page, key, icon, key, hint, 120f);
            ToggleSwitch toggle = UIKit.Toggle(slot, "Toggle");
            ((RectTransform)toggle.transform).At(new Vector2(1f, 0.5f), Vector2.zero, new Vector2(96f, 52f), new Vector2(1f, 0.5f));
            return toggle;
        }

        private static Slider SliderRow(RectTransform page, string icon, string key, float min, float max)
        {
            RectTransform slot = UIKit.SettingRow(page, key, icon, key, null, 520f);
            Slider slider = UIKit.Slider(slot, "Slider", 500f);
            ((RectTransform)slider.transform).At(new Vector2(1f, 0.5f), Vector2.zero, new Vector2(500f, 56f), new Vector2(1f, 0.5f));
            slider.minValue = min;
            slider.maxValue = max;
            return slider;
        }

        private static Button ActionRow(RectTransform page, string icon, string key, ButtonKind kind, string actionKey)
        {
            RectTransform slot = UIKit.SettingRow(page, key, icon, key, null, actionKey != null ? 320f : 80f);
            if (actionKey == null)
            {
                // Whole row is the button; a chevron shows it opens something.
                var row = (RectTransform)slot.parent;
                var tap = row.gameObject.AddComponent<Button>();
                tap.targetGraphic = row.Find("Fill").GetComponent<Image>();
                row.gameObject.AddComponent<UIButtonFx>();
                UIKit.Icon(slot, "chevron_right", 40f, T.textSecondary).rectTransform.At(new Vector2(1f, 0.5f), Vector2.zero, new Vector2(40f, 40f), new Vector2(1f, 0.5f));
                return tap;
            }

            ButtonParts button = UIKit.Button(slot, "Action", kind, actionKey, null, new Vector2(300f, 72f));
            button.Rect.At(new Vector2(1f, 0.5f), Vector2.zero, new Vector2(300f, 72f), new Vector2(1f, 0.5f));
            return button.Button;
        }
    }
}
