using System.Collections.Generic;
using BilliardGame.Editor.UI;
using BilliardGame.Presentation;
using BilliardGame.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace BilliardGame.Editor
{
    /// <summary>
    /// Match HUD (docs/UI_UX.md §10): player panels with timer rings and group pips, status banner, pause/emote,
    /// power column on the left, fine-tune wheel + spin + shoot on the right (mirrored in left-hand mode), pause
    /// modal, sticker sheet and the result screen. Built from the same kit as the lobby.
    /// </summary>
    internal static class HudBuilder
    {
        private const float Edge = 24f;
        private const float PanelWidth = 560f;
        private const float PanelHeight = 132f;

        private static UITheme T => UIKit.Theme;

        /// <summary>Builds the HUD; <paramref name="hudRoot"/> is the in-match part (hidden in the lobby).</summary>
        public static HudController Build(GameController game, CueController cue, GameServices services, out GameObject hudRoot)
        {
            var events = new GameObject("EventSystem");
            events.AddComponent<EventSystem>();
            var module = events.AddComponent<InputSystemUIInputModule>();
            // Touch-only UI: keyboard/gamepad navigation would walk the selection onto hidden screens' buttons and
            // Enter/Space would press them (e.g. "Play again" landing in the lobby). Escape stays with ScreenRouter.
            module.move = null;
            module.submit = null;
            module.cancel = null;

            Canvas canvas = LobbyUIBuilder.CreateCanvas("HUD", 5);
            RectTransform root = UIKit.Node("Match HUD", canvas.transform).Fill();
            RectTransform safe = UIKit.Node("Safe Area", root).Fill();
            safe.gameObject.AddComponent<SafeAreaFitter>();

            // Top: shading, player panels, pause + emote, status banner.
            Image shade = UIKit.Image(root, "Top Shade", UIAssets.GradientUp, new Color(0f, 0f, 0f, 0.5f));
            shade.rectTransform.Span(new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -220f), Vector2.zero);
            shade.transform.SetAsFirstSibling();
            PlayerPanelView left = Panel(safe, "Player 1", false);
            PlayerPanelView right = Panel(safe, "Player 2", true);
            ButtonParts pause = UIKit.IconButton(safe, "Pause", "pause", 88f);
            pause.Rect.At(new Vector2(0.5f, 1f), new Vector2(-56f, -Edge), new Vector2(88f, 88f), new Vector2(0.5f, 1f));
            ButtonParts emote = UIKit.IconButton(safe, "Emote", "emote", 88f);
            emote.Rect.At(new Vector2(0.5f, 1f), new Vector2(56f, -Edge), new Vector2(88f, 88f), new Vector2(0.5f, 1f));

            Image bannerBack = UIKit.Image(safe, "Banner", UIAssets.Pill, new Color(0.03f, 0.05f, 0.1f, 0.82f));
            bannerBack.rectTransform.At(new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(860f, 68f), new Vector2(0.5f, 1f));
            var banner = bannerBack.gameObject.AddComponent<CanvasGroup>();
            banner.blocksRaycasts = false;
            TMP_Text status = UIKit.Text(bannerBack.transform, "Status", null, TextStyle.H2, null, TextAlignmentOptions.Center);
            status.fontSize = status.fontSizeMax = 30f;
            status.rectTransform.Fill(28f, 0f, 28f, 0f);

            // Left thumb: power.
            RectTransform power = UIKit.Node("Power Column", safe);
            power.At(new Vector2(0f, 0.5f), new Vector2(Edge, -40f), new Vector2(140f, 600f), new Vector2(0f, 0.5f));
            TMP_Text powerLabel = UIKit.Text(power, "Label", "hud.power", TextStyle.Caption, T.textSecondary, TextAlignmentOptions.Center, true);
            powerLabel.font = T.Font(TextStyle.Button);
            powerLabel.rectTransform.Span(new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -36f), Vector2.zero);
            Slider slider = PowerSlider(power);
            TMP_Text powerValue = UIKit.Text(power, "Value", null, TextStyle.Number, T.accent, TextAlignmentOptions.Center);
            powerValue.fontSize = powerValue.fontSizeMax = 34f;
            powerValue.rectTransform.Span(Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 44f));

            // Right thumb: fine-tune wheel, spin, shoot.
            RectTransform cluster = UIKit.Node("Right Cluster", safe);
            cluster.At(new Vector2(1f, 0f), new Vector2(-Edge, Edge), new Vector2(560f, 680f), new Vector2(1f, 0f));
            FineTuneWheel wheel = Wheel(cluster);
            Button spinIcon = SpinIcon(cluster, out RectTransform previewArea, out RectTransform previewDot);
            SpinPad spin = SpinPad(cluster, previewArea, previewDot);
            spin.gameObject.SetActive(false);
            ButtonParts shoot = UIKit.Button(cluster, "Shoot", ButtonKind.Primary, "hud.shoot", "target", new Vector2(300f, 132f), 32, 56f);
            shoot.Rect.At(new Vector2(1f, 0f), Vector2.zero, new Vector2(300f, 132f), new Vector2(1f, 0f));
            shoot.Label.fontSize = shoot.Label.fontSizeMax = 40f;

            ScreenView pauseView = PauseModal(canvas.transform, out Button resume, out Button leave);
            ScreenView stickers = StickerSheet(canvas.transform, out Button stickerTemplate);
            ResultScreen result = Result(canvas.transform);

            var hud = canvas.gameObject.AddComponent<HudController>();
            UIKit.Wire(hud, "game", game, "cue", cue, "services", services, "theme", T,
                "panels", new Object[] { left, right }, "banner", banner, "statusLabel", status,
                "powerColumn", power, "powerSlider", slider, "powerValue", powerValue, "rightCluster", cluster,
                "fineTuneWheel", wheel, "spinPad", spin, "spinIcon", spinIcon, "shootButton", shoot.Button, "shootLabel", shoot.Label,
                "pauseButton", pause.Button, "pauseView", pauseView, "resumeButton", resume, "leaveButton", leave,
                "emoteButton", emote.Button, "stickerSheet", stickers, "stickerTemplate", stickerTemplate, "result", result);

            hudRoot = root.gameObject;
            hudRoot.SetActive(false);
            return hud;
        }

        // ---- Player panel --------------------------------------------------------------------------------------------

        private static PlayerPanelView Panel(RectTransform parent, string name, bool mirrored)
        {
            Vector2 corner = mirrored ? Vector2.one : new Vector2(0f, 1f);
            float side = mirrored ? -1f : 1f;
            Image fill = UIKit.Panel(parent, name, new Color(T.surface.r, T.surface.g, T.surface.b, 0.9f), 20, true);
            var root = (RectTransform)fill.transform.parent;
            root.At(corner, new Vector2(Edge * side, -Edge), new Vector2(PanelWidth, PanelHeight), corner);
            var group = root.gameObject.AddComponent<CanvasGroup>();
            Image glow = UIKit.Image(root, "Active", UIAssets.Stroke(20), T.accent);
            glow.rectTransform.Fill(-4f, -4f, -4f, -4f);

            // Avatar with the shot-clock ring around it.
            var avatarAnchor = new Vector2(mirrored ? 1f : 0f, 0.5f);
            Image portrait = UIKit.Avatar(root, "Avatar", 100f, out Image frame, out TMP_Text level);
            var avatar = (RectTransform)portrait.transform.parent;
            avatar.At(avatarAnchor, new Vector2(20f * side, 6f), new Vector2(100f, 100f), avatarAnchor);
            Object.DestroyImmediate(level.transform.parent.gameObject);
            Image ring = UIKit.Image(avatar, "Timer Ring", UIAssets.Ring, T.success);
            ring.rectTransform.Fill(-12f, -12f, -12f, -12f);
            ring.type = Image.Type.Filled;
            ring.fillMethod = Image.FillMethod.Radial360;
            ring.fillOrigin = (int)Image.Origin360.Top;
            ring.fillClockwise = false;

            TextAlignmentOptions align = mirrored ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
            float inner = 140f;
            TMP_Text player = UIKit.Text(root, "Name", null, TextStyle.H2, null, align);
            player.rectTransform.Span(new Vector2(0f, 1f), Vector2.one, Inset(mirrored, inner, -50f, 16f), Inset(mirrored, inner, -10f, 16f, true));
            TMP_Text rank = UIKit.Text(root, "Rank", null, TextStyle.Caption, T.accent, align);
            rank.rectTransform.Span(new Vector2(0f, 1f), Vector2.one, Inset(mirrored, inner, -82f, 16f), Inset(mirrored, inner, -50f, 16f, true));
            TMP_Text groupText = UIKit.Text(root, "Group", null, TextStyle.Caption, T.textSecondary, mirrored ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.MidlineRight);
            groupText.rectTransform.Span(new Vector2(0f, 1f), Vector2.one, Inset(!mirrored, 16f, -82f, inner), Inset(!mirrored, 16f, -50f, inner, true));
            RectTransform pipRow = UIKit.Row(root, "Pips", 6f, mirrored ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft, false);
            pipRow.Span(Vector2.zero, new Vector2(1f, 0f), Inset(mirrored, inner, 12f, 16f), Inset(mirrored, inner, 40f, 16f, true));
            var pips = new Image[7];
            for (int i = 0; i < pips.Length; i++)
            {
                pips[i] = UIKit.Image(pipRow, "Pip " + i, UIAssets.Circle, Color.white);
                UIKit.Size(pips[i], 24f, 24f);
            }

            // Sticker bubble under the panel.
            Image bubble = UIKit.Image(root, "Sticker Bubble", UIAssets.Rounded(20), new Color(T.surfaceRaised.r, T.surfaceRaised.g, T.surfaceRaised.b, 0.95f));
            bubble.rectTransform.At(new Vector2(mirrored ? 1f : 0f, 0f), new Vector2(40f * side, -16f), new Vector2(160f, 160f), new Vector2(mirrored ? 1f : 0f, 1f));
            Image sticker = UIKit.Image(bubble.transform, "Sticker", null, Color.white);
            sticker.preserveAspect = true;
            sticker.rectTransform.Fill(14f, 14f, 14f, 14f);
            bubble.gameObject.SetActive(false);

            var view = root.gameObject.AddComponent<PlayerPanelView>();
            view.Configure(player, rank, groupText, frame, ring, glow.gameObject, pips, bubble.rectTransform, sticker, group);
            return view;
        }

        /// <summary>Horizontal inset pair for a panel row: <paramref name="near"/> from the avatar side, <paramref name="far"/> from the other.</summary>
        private static Vector2 Inset(bool mirrored, float near, float y, float far, bool max = false)
        {
            float leftInset = mirrored ? far : near;
            float rightInset = mirrored ? near : far;
            return max ? new Vector2(-rightInset, y) : new Vector2(leftInset, y);
        }

        // ---- Controls ------------------------------------------------------------------------------------------------

        private static Slider PowerSlider(RectTransform column)
        {
            RectTransform root = UIKit.Node("Power", column);
            root.Span(new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(-46f, 56f), new Vector2(46f, -48f));
            Image track = UIKit.Image(root, "Track", UIAssets.Pill, new Color(0.03f, 0.05f, 0.1f, 0.85f), true);
            track.rectTransform.Fill(14f, 0f, 14f, 0f);
            UIKit.Image(root, "Stroke", UIAssets.Stroke(32), T.stroke).rectTransform.Fill(14f, 0f, 14f, 0f);
            RectTransform fillArea = UIKit.Node("Fill Area", root).Fill(20f, 6f, 20f, 6f);
            Image fill = UIKit.Image(fillArea, "Fill", UIAssets.Pill, T.accent);
            fill.rectTransform.Fill();
            Image gloss = UIKit.Image(fill.transform, "Heat", UIAssets.GradientUp, new Color(T.danger.r, T.danger.g, T.danger.b, 0.85f));
            gloss.rectTransform.Fill();
            RectTransform handleArea = UIKit.Node("Handle Area", root).Fill(0f, 40f, 0f, 40f);
            Image handle = UIKit.Image(handleArea, "Handle", UIAssets.Circle, Color.white, true);
            handle.rectTransform.sizeDelta = new Vector2(0f, 92f);
            handle.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            handle.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            UIKit.Icon(handle.transform, "bolt", 44f, T.textOnAccent).rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(44f, 44f));
            var slider = root.gameObject.AddComponent<Slider>();
            slider.direction = Slider.Direction.BottomToTop;
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.value = 0.5f;
            return slider;
        }

        private static FineTuneWheel Wheel(RectTransform cluster)
        {
            Image frame = UIKit.Panel(cluster, "Fine Tune", new Color(0.03f, 0.05f, 0.1f, 0.85f), 32);
            var root = (RectTransform)frame.transform.parent;
            root.At(Vector2.one, new Vector2(0f, 0f), new Vector2(124f, 500f), Vector2.one);
            frame.raycastTarget = true;
            var strip = new GameObject("Ridges", typeof(RectTransform), typeof(RawImage));
            strip.layer = 5;
            var stripRect = (RectTransform)strip.transform;
            stripRect.SetParent(root, false);
            stripRect.Fill(14f, 14f, 14f, 14f);
            var ridges = strip.GetComponent<RawImage>();
            ridges.texture = ProceduralTextures.Ridges();
            ridges.color = new Color(0.78f, 0.8f, 0.86f);
            ridges.uvRect = new Rect(0f, 0f, 1f, 4f);
            ridges.raycastTarget = false;
            Image shine = UIKit.Image(root, "Shine", UIAssets.GradientRight, new Color(1f, 1f, 1f, 0.08f));
            shine.rectTransform.Fill(14f, 14f, 14f, 14f);
            var wheel = root.gameObject.AddComponent<FineTuneWheel>();
            UIKit.Wire(wheel, "ridges", ridges);
            return wheel;
        }

        /// <summary>Small cue-ball icon showing the tip offset; tapping it toggles the full spin pad.</summary>
        private static Button SpinIcon(RectTransform cluster, out RectTransform area, out RectTransform dot)
        {
            Image face = UIKit.Image(cluster, "Spin Icon", UIAssets.Circle, new Color(0.96f, 0.95f, 0.9f), true);
            area = face.rectTransform;
            area.At(new Vector2(1f, 0f), new Vector2(-324f, 0f), new Vector2(132f, 132f), new Vector2(1f, 0f));
            UIKit.Image(area, "Ring", UIAssets.Ring, T.accent).rectTransform.Fill(-6f, -6f, -6f, -6f);
            Image tip = UIKit.Image(area, "Tip", UIAssets.Circle, T.danger);
            dot = tip.rectTransform;
            dot.sizeDelta = new Vector2(28f, 28f);
            TMP_Text label = UIKit.Text(area, "Label", "hud.spin", TextStyle.Caption, T.textSecondary, TextAlignmentOptions.Center, true);
            label.fontSize = label.fontSizeMax = 18f;
            label.rectTransform.At(new Vector2(0.5f, 1f), new Vector2(0f, 8f), new Vector2(160f, 28f), new Vector2(0.5f, 0f));
            var button = face.gameObject.AddComponent<Button>();
            button.targetGraphic = face;
            return button;
        }

        private static SpinPad SpinPad(RectTransform cluster, RectTransform previewArea, RectTransform previewDot)
        {
            Image back = UIKit.Image(cluster, "Spin Pad", UIAssets.Circle, new Color(0f, 0f, 0f, 0.5f), true);
            back.rectTransform.At(new Vector2(1f, 0f), new Vector2(-164f, 180f), new Vector2(340f, 340f), new Vector2(1f, 0f));
            Image face = UIKit.Image(back.transform, "Ball", UIAssets.Circle, new Color(0.96f, 0.95f, 0.9f), true);
            face.rectTransform.Fill(10f, 10f, 10f, 10f);
            UIKit.Image(face.transform, "Cross H", null, new Color(0f, 0f, 0f, 0.15f)).rectTransform.Span(new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(20f, -1f), new Vector2(-20f, 1f));
            UIKit.Image(face.transform, "Cross V", null, new Color(0f, 0f, 0f, 0.15f)).rectTransform.Span(new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(-1f, 20f), new Vector2(1f, -20f));
            Image tip = UIKit.Image(face.transform, "Tip", UIAssets.Circle, T.danger);
            tip.rectTransform.sizeDelta = new Vector2(44f, 44f);
            var spin = back.gameObject.AddComponent<SpinPad>();
            UIKit.Wire(spin, "area", face.rectTransform, "dot", tip.rectTransform, "previewArea", previewArea, "previewDot", previewDot);
            return spin;
        }

        // ---- Overlays ------------------------------------------------------------------------------------------------

        private static ScreenView PauseModal(Transform parent, out Button resume, out Button leave)
        {
            var view = UIKit.Modal<ScreenView>(parent, "Pause", new Vector2(760f, 520f), out RectTransform card);
            UIKit.Icon(card, "pause", 56f, T.accent).rectTransform.At(new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(56f, 56f), new Vector2(0.5f, 1f));
            TMP_Text title = UIKit.Text(card, "Title", "hud.pause", TextStyle.H1, null, TextAlignmentOptions.Center, true);
            title.rectTransform.Span(new Vector2(0f, 1f), Vector2.one, new Vector2(40f, -170f), new Vector2(-40f, -104f));
            ButtonParts resumeButton = UIKit.Button(card, "Resume", ButtonKind.Primary, "hud.resume", "play", new Vector2(600f, 100f));
            resumeButton.Rect.At(new Vector2(0.5f, 0f), new Vector2(0f, 156f), new Vector2(600f, 100f), new Vector2(0.5f, 0f));
            ButtonParts leaveButton = UIKit.Button(card, "Leave", ButtonKind.Danger, "hud.leave", "logout", new Vector2(600f, 96f));
            leaveButton.Rect.At(new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(600f, 96f), new Vector2(0.5f, 0f));
            view.BindBack(resumeButton.Button);
            resume = resumeButton.Button;
            leave = leaveButton.Button;
            return view;
        }

        private static ScreenView StickerSheet(Transform parent, out Button template)
        {
            var view = UIKit.Sheet<ScreenView>(parent, "Stickers", 320f, out RectTransform sheet);
            TMP_Text title = UIKit.Text(sheet, "Title", "hud.emotes", TextStyle.H2, null, TextAlignmentOptions.MidlineLeft, true);
            title.rectTransform.Span(new Vector2(0f, 1f), Vector2.one, new Vector2(40f, -96f), new Vector2(-140f, -32f));
            ButtonParts close = UIKit.IconButton(sheet, "Close", "close", 72f);
            close.Rect.At(Vector2.one, new Vector2(-24f, -24f), new Vector2(72f, 72f), Vector2.one);
            view.BindBack(close.Button);
            ScrollRect scroll = UIKit.Scroll(sheet, "Scroll", false, out RectTransform content);
            ((RectTransform)scroll.transform).Fill(32f, 64f, 32f, 108f);
            var row = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 16f;
            row.childControlWidth = row.childControlHeight = false;
            row.childForceExpandWidth = row.childForceExpandHeight = false;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.padding = new RectOffset(4, 4, 4, 4);

            // The tile background sits on the root so child 0 is the icon (HudController sets its sprite).
            Image item = UIKit.Image(content, "Sticker Template", UIAssets.Rounded(20), T.surfaceSunken, true);
            item.rectTransform.sizeDelta = new Vector2(140f, 140f);
            Image icon = UIKit.Image(item.transform, "Icon", null, Color.white);
            icon.preserveAspect = true;
            icon.rectTransform.Fill(8f, 8f, 8f, 8f);
            var button = item.gameObject.AddComponent<Button>();
            button.targetGraphic = item;
            template = button;
            return view;
        }

        private static ResultScreen Result(Transform parent)
        {
            var view = UIKit.Screen<ResultScreen>(parent, "Result", Transition.Fade, false, out RectTransform safe, Backdrop.Scrim);
            // Extra darkening so the live HUD under the result does not compete with it.
            Image shade = UIKit.Image(view.transform, "Shade", null, new Color(0.01f, 0.02f, 0.05f, 0.55f));
            shade.rectTransform.Fill();
            shade.transform.SetSiblingIndex(safe.GetSiblingIndex());
            RectTransform banner = UIKit.Node("Banner", safe);
            banner.At(new Vector2(0.5f, 0.62f), Vector2.zero, new Vector2(1100f, 520f));
            Image rays = UIKit.Image(banner, "Rays", UIAssets.Rays, new Color(T.accent.r, T.accent.g, T.accent.b, 0.4f));
            rays.rectTransform.At(new Vector2(0.5f, 0.6f), Vector2.zero, new Vector2(1000f, 1000f));
            Image glow = UIKit.Image(banner, "Glow", UIAssets.Glow, new Color(T.accent.r, T.accent.g, T.accent.b, 0.55f));
            glow.rectTransform.At(new Vector2(0.5f, 0.6f), Vector2.zero, new Vector2(760f, 520f));
            UIKit.Icon(banner, "trophy", 150f, T.accent).rectTransform.At(new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(150f, 150f), new Vector2(0.5f, 1f));
            TMP_Text title = UIKit.Text(banner, "Title", null, TextStyle.Display, T.accent, TextAlignmentOptions.Center);
            title.fontSize = title.fontSizeMax = 110f;
            title.characterSpacing = 8f;
            title.rectTransform.Span(new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -300f), new Vector2(0f, -160f));
            TMP_Text subtitle = UIKit.Text(banner, "Subtitle", null, TextStyle.H2, T.textPrimary, TextAlignmentOptions.Center);
            subtitle.rectTransform.Span(new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -356f), new Vector2(0f, -304f));
            TMP_Text potted = UIKit.Text(banner, "Potted", null, TextStyle.Caption, T.textSecondary, TextAlignmentOptions.Center);
            potted.rectTransform.Span(new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -400f), new Vector2(0f, -360f));

            // Rewards card.
            RectTransform rewards = UIKit.Card(safe, "Rewards", null, null);
            rewards.At(new Vector2(0.5f, 0f), new Vector2(0f, 170f), new Vector2(900f, 180f), new Vector2(0.5f, 0f));
            UIKit.Icon(rewards, "star", 48f, T.secondary).rectTransform.At(new Vector2(0f, 1f), new Vector2(32f, -24f), new Vector2(48f, 48f), new Vector2(0f, 1f));
            TMP_Text xpCaption = UIKit.Text(rewards, "XP Caption", "result.xp", TextStyle.Caption, T.textSecondary);
            xpCaption.rectTransform.At(new Vector2(0f, 1f), new Vector2(92f, -22f), new Vector2(160f, 26f), new Vector2(0f, 1f));
            TMP_Text xp = UIKit.Text(rewards, "XP", null, TextStyle.Number, T.textPrimary);
            xp.fontSize = xp.fontSizeMax = 36f;
            xp.rectTransform.At(new Vector2(0f, 1f), new Vector2(92f, -46f), new Vector2(240f, 40f), new Vector2(0f, 1f));
            var xpCount = xp.gameObject.AddComponent<CountUpText>();
            xpCount.Configure("+", 0.9f);
            ProgressBar xpBar = UIKit.Progress(rewards, "XP Bar", T.secondary, 14f);
            ((RectTransform)xpBar.transform).Span(Vector2.zero, new Vector2(1f, 0f), new Vector2(32f, 32f), new Vector2(-32f, 46f));
            TMP_Text level = UIKit.Text(rewards, "Level", null, TextStyle.Caption, T.textSecondary, TextAlignmentOptions.MidlineLeft);
            level.rectTransform.Span(Vector2.zero, new Vector2(0.5f, 0f), new Vector2(32f, 52f), new Vector2(0f, 84f));

            RectTransform coinRow = UIKit.Node("Coins", rewards);
            coinRow.Span(new Vector2(0.5f, 1f), Vector2.one, new Vector2(40f, -96f), new Vector2(-32f, -16f));
            UIKit.Icon(coinRow, "coin", 48f, Color.white).rectTransform.At(new Vector2(0f, 0.5f), Vector2.zero, new Vector2(48f, 48f), new Vector2(0f, 0.5f));
            TMP_Text coinCaption = UIKit.Text(coinRow, "Caption", "result.coins", TextStyle.Caption, T.textSecondary);
            coinCaption.rectTransform.Span(new Vector2(0f, 0.5f), Vector2.one, new Vector2(60f, 0f), Vector2.zero);
            TMP_Text coins = UIKit.Text(coinRow, "Value", null, TextStyle.Number, T.coin);
            coins.fontSize = coins.fontSizeMax = 36f;
            coins.rectTransform.Span(Vector2.zero, new Vector2(1f, 0.5f), new Vector2(60f, 0f), Vector2.zero);
            var coinCount = coins.gameObject.AddComponent<CountUpText>();
            coinCount.Configure("+", 0.9f);
            TMP_Text practice = UIKit.Wrap(UIKit.Text(rewards, "Practice", "result.practice", TextStyle.Caption, T.textSecondary, TextAlignmentOptions.MidlineRight));
            practice.rectTransform.Span(new Vector2(0.5f, 1f), Vector2.one, new Vector2(40f, -96f), new Vector2(-32f, -16f));

            RectTransform actions = UIKit.Row(safe, "Actions", 24f, TextAnchor.MiddleCenter, false);
            actions.Span(Vector2.zero, new Vector2(1f, 0f), new Vector2(40f, 40f), new Vector2(-40f, 140f));
            ButtonParts lobby = UIKit.Button(actions, "Lobby", ButtonKind.Secondary, "result.lobby", "home", new Vector2(360f, 100f));
            ButtonParts again = UIKit.Button(actions, "Again", ButtonKind.Primary, "result.again", "refresh", new Vector2(420f, 100f));
            UIKit.Size(lobby.Rect, 360f, 100f);
            UIKit.Size(again.Rect, 420f, 100f);

            UIKit.Wire(view, "theme", T, "title", title, "subtitle", subtitle, "banner", banner, "rays", rays.rectTransform, "glow", glow,
                "xpCount", xpCount, "xpBar", xpBar, "levelLabel", level, "coinRow", coinRow.gameObject, "coinCount", coinCount,
                "practiceNote", practice, "potted", potted, "againButton", again.Button, "lobbyButton", lobby.Button);
            return view;
        }
    }
}
