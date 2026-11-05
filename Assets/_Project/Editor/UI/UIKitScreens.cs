using BilliardGame.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BilliardGame.Editor.UI
{
    internal enum Backdrop
    {
        Opaque,
        Scrim,
        None,
    }

    /// <summary>Screen scaffolding: full screens, modals, bottom sheets, headers, cards and item tiles.</summary>
    internal static partial class UIKit
    {
        public const float HeaderHeight = 120f;
        public const float Gutter = 40f;

        /// <summary>
        /// Full screen with a dark gradient backdrop, kept inside the safe area. Returns the view and its content rect
        /// (anchored to the safe area, under the header).
        /// </summary>
        public static T Screen<T>(Transform parent, string name, Transition transition, bool hidesLobby, out RectTransform content, Backdrop backdrop = Backdrop.Opaque) where T : ScreenView
        {
            RectTransform root = Node(name, parent).Fill();
            root.gameObject.AddComponent<CanvasGroup>();
            if (backdrop == Backdrop.Opaque)
            {
                Image back = Image(root, "Backdrop", null, Theme.background, true);
                back.rectTransform.Fill();
                Image(root, "Tint", UIAssets.GradientUp, new Color(0.18f, 0.24f, 0.4f, 0.35f)).rectTransform.Fill();
                Image(root, "Vignette", UIAssets.Vignette, new Color(0f, 0f, 0f, 0.6f)).rectTransform.Fill();
            }
            else if (backdrop == Backdrop.Scrim)
            {
                Image(root, "Scrim", null, Theme.scrim, true).rectTransform.Fill();
            }
            else
            {
                // 3D shows through (crates, My Room): only soft edge shading so the chrome stays readable.
                Image(root, "Edge Shade", UIAssets.Vignette, new Color(0f, 0f, 0f, 0.45f)).rectTransform.Fill();
            }

            content = Node("Safe Area", root).Fill();
            content.gameObject.AddComponent<SafeAreaFitter>();
            var view = root.gameObject.AddComponent<T>();
            view.ConfigureView(transition, content, Theme.screenDuration, hidesLobby);
            root.gameObject.SetActive(false);
            return view;
        }

        /// <summary>Header bar: back button, icon, title. Returns the back button.</summary>
        public static Button Header(RectTransform safe, string titleKey, string icon, out RectTransform bar)
        {
            bar = Node("Header", safe);
            bar.Span(new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -HeaderHeight), Vector2.zero);
            Image(bar, "Fade", UIAssets.GradientUp, new Color(0f, 0f, 0f, 0.5f)).rectTransform.Fill();
            ButtonParts back = IconButton(bar, "Back", "back", 88f);
            back.Rect.At(new Vector2(0f, 0.5f), new Vector2(Gutter, 0f), new Vector2(88f, 88f), new Vector2(0f, 0.5f));
            Image glyph = Icon(bar, icon, 52f, Theme.accent);
            glyph.rectTransform.At(new Vector2(0f, 0.5f), new Vector2(Gutter + 112f, 0f), new Vector2(52f, 52f), new Vector2(0f, 0.5f));
            TMP_Text title = Text(bar, "Title", titleKey, TextStyle.H1, null, TextAlignmentOptions.MidlineLeft, true);
            title.rectTransform.Span(new Vector2(0f, 0f), new Vector2(0.6f, 1f), new Vector2(Gutter + 180f, 0f), Vector2.zero);
            return back.Button;
        }

        /// <summary>Centred modal card over a scrim (pop transition). Returns the view; <paramref name="card"/> is the card.</summary>
        public static T Modal<T>(Transform parent, string name, Vector2 size, out RectTransform card) where T : ScreenView
        {
            RectTransform root = Node(name, parent).Fill();
            root.gameObject.AddComponent<CanvasGroup>();
            Image(root, "Scrim", null, Theme.scrim, true).rectTransform.Fill();
            Image panel = Panel(root, "Card", Theme.surface, 32, true);
            card = (RectTransform)panel.transform.parent;
            card.At(new Vector2(0.5f, 0.5f), Vector2.zero, size);
            panel.color = new Color(Theme.surface.r, Theme.surface.g, Theme.surface.b, 0.98f);
            Image(card, "Top Glow", UIAssets.GradientUp, new Color(Theme.accent.r, Theme.accent.g, Theme.accent.b, 0.08f)).rectTransform.Span(new Vector2(0f, 0.6f), Vector2.one, new Vector2(4f, 0f), new Vector2(-4f, -4f));
            var view = root.gameObject.AddComponent<T>();
            view.ConfigureView(Transition.Pop, card, Theme.modalDuration, false);
            root.gameObject.SetActive(false);
            return view;
        }

        /// <summary>Bottom sheet (slides up) of the given height over a scrim.</summary>
        public static T Sheet<T>(Transform parent, string name, float height, out RectTransform sheet) where T : ScreenView
        {
            RectTransform root = Node(name, parent).Fill();
            root.gameObject.AddComponent<CanvasGroup>();
            Image(root, "Scrim", null, Theme.scrim, true).rectTransform.Fill();
            Image panel = Panel(root, "Sheet", Theme.surface, 32, true);
            sheet = (RectTransform)panel.transform.parent;
            sheet.Span(Vector2.zero, new Vector2(1f, 0f), new Vector2(0f, -40f), new Vector2(0f, height));
            Image grip = Image(sheet, "Grip", UIAssets.Pill, new Color(1f, 1f, 1f, 0.25f));
            grip.rectTransform.At(new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(96f, 8f));
            var view = root.gameObject.AddComponent<T>();
            view.ConfigureView(Transition.SlideUp, sheet, Theme.screenDuration, false);
            root.gameObject.SetActive(false);
            return view;
        }

        /// <summary>Raised card with title row (icon + text). Content goes under <c>card</c> below 72 px.</summary>
        public static RectTransform Card(Transform parent, string name, string titleKey, string icon)
        {
            Image fill = Panel(parent, name, Theme.surface, 20, true);
            var card = (RectTransform)fill.transform.parent;
            if (titleKey == null) return card;
            if (icon != null) Icon(card, icon, 40f, Theme.accent).rectTransform.At(new Vector2(0f, 1f), new Vector2(24f, -20f), new Vector2(40f, 40f), new Vector2(0f, 1f));
            TMP_Text title = Text(card, "Title", titleKey, TextStyle.H2, null, TextAlignmentOptions.MidlineLeft, true);
            title.fontSize = title.fontSizeMax = 28f;
            title.rectTransform.Span(new Vector2(0f, 1f), Vector2.one, new Vector2(icon != null ? 76f : 24f, -60f), new Vector2(-24f, -20f));
            return card;
        }

        /// <summary>Item tile (rarity frame, gradient, icon, name, pips, badges, lock, selection, glow).</summary>
        public static ItemTile Tile(Transform parent, string name)
        {
            RectTransform root = Node(name, parent);
            root.sizeDelta = new Vector2(200f, 236f);
            Image glow = Image(root, "Glow", UIAssets.Glow, Color.white);
            glow.rectTransform.Fill(-36f, -36f, -36f, -36f);
            Image frame = Image(root, "Frame", UIAssets.Rounded(20), Color.white, true);
            frame.rectTransform.Fill();
            Image inner = Image(root, "Inner", UIAssets.Rounded(12), new Color(0.06f, 0.08f, 0.13f));
            inner.rectTransform.Fill(4f, 4f, 4f, 4f);
            Image gradient = Image(root, "Gradient", UIAssets.GradientUp, Color.white);
            gradient.rectTransform.Fill(4f, 4f, 4f, 4f);
            gradient.rectTransform.localScale = new Vector3(1f, -1f, 1f);
            Image icon = Image(root, "Icon", null, Color.white);
            icon.preserveAspect = true;
            icon.rectTransform.Span(new Vector2(0f, 0.28f), Vector2.one, new Vector2(16f, 0f), new Vector2(-16f, -16f));
            TMP_Text title = Text(root, "Name", null, TextStyle.Caption, Theme.textPrimary, TextAlignmentOptions.Center);
            title.font = Theme.Font(TextStyle.H2);
            title.rectTransform.Span(Vector2.zero, new Vector2(1f, 0f), new Vector2(10f, 30f), new Vector2(-10f, 64f));
            RectTransform pipRow = Row(root, "Pips", 6f, TextAnchor.MiddleCenter);
            pipRow.Span(Vector2.zero, new Vector2(1f, 0f), new Vector2(0f, 12f), new Vector2(0f, 28f));
            var pips = new Image[5];
            for (int i = 0; i < pips.Length; i++)
            {
                pips[i] = Image(pipRow, "Pip " + i, UIAssets.Circle, Color.white);
                Size(pips[i], 12f, 12f);
            }

            GameObject newBadge = Tag(root, "New", "common.new", Theme.danger, new Vector2(0f, 1f), new Vector2(10f, -10f));
            Image equipped = Image(root, "Equipped", UIAssets.Circle, Theme.success);
            equipped.rectTransform.At(Vector2.one, new Vector2(-10f, -10f), new Vector2(40f, 40f), Vector2.one);
            Icon(equipped.transform, "check", 28f, Color.white).rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(28f, 28f));
            Image lockOverlay = Image(root, "Lock", UIAssets.Rounded(20), new Color(0f, 0f, 0f, 0.45f));
            lockOverlay.rectTransform.Fill();
            Icon(lockOverlay.transform, "lock", 44f, new Color(1f, 1f, 1f, 0.85f)).rectTransform.At(new Vector2(0.5f, 0.62f), Vector2.zero, new Vector2(44f, 44f));
            Image selected = Image(root, "Selected", UIAssets.Stroke(20), Color.white);
            selected.rectTransform.Fill(-6f, -6f, -6f, -6f);
            TMP_Text note = Text(root, "Note", null, TextStyle.Caption, Theme.accent, TextAlignmentOptions.Center);
            note.fontSize = note.fontSizeMax = 18f;
            note.rectTransform.Span(new Vector2(0f, 0.28f), new Vector2(1f, 0.28f), new Vector2(6f, 0f), new Vector2(-6f, 26f));

            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = frame;
            button.transition = Selectable.Transition.None;
            var tile = root.gameObject.AddComponent<ItemTile>();
            tile.Configure(frame, gradient, icon, title, pips, newBadge, equipped.gameObject, lockOverlay.gameObject, selected.gameObject, glow, note, button);
            return tile;
        }

        /// <summary>Small rounded label ("NEW", "SOON").</summary>
        public static GameObject Tag(Transform parent, string name, string key, Color color, Vector2 anchor, Vector2 position)
        {
            Image back = Image(parent, name, UIAssets.Pill, color);
            back.rectTransform.At(anchor, position, new Vector2(88f, 32f), anchor);
            TMP_Text label = Text(back.transform, "Label", key, TextStyle.Caption, Color.white, TextAlignmentOptions.Center, true);
            label.font = Theme.Font(TextStyle.Button);
            label.fontSize = label.fontSizeMax = 18f;
            label.rectTransform.Fill(8f, 0f, 8f, 0f);
            return back.gameObject;
        }

        /// <summary>Round avatar portrait with the equipped frame ring and a level chip.</summary>
        public static Image Avatar(Transform parent, string name, float size, out Image frame, out TMP_Text level)
        {
            RectTransform root = Node(name, parent);
            root.sizeDelta = new Vector2(size, size);
            Image(root, "Back", UIAssets.Circle, Theme.surfaceRaised).rectTransform.Fill();
            Image portrait = Image(root, "Portrait", UIAssets.Icon("person"), Theme.textSecondary);
            portrait.preserveAspect = true;
            portrait.rectTransform.Fill(size * 0.16f, size * 0.12f, size * 0.16f, size * 0.16f);
            frame = Image(root, "Frame", UIAssets.Ring, Theme.accent);
            frame.rectTransform.Fill(-4f, -4f, -4f, -4f);
            Image chip = Image(root, "Level", UIAssets.Pill, Theme.accent);
            chip.rectTransform.At(new Vector2(0.5f, 0f), new Vector2(0f, -10f), new Vector2(size * 0.62f, 30f), new Vector2(0.5f, 0.5f));
            level = Text(chip.transform, "Value", null, TextStyle.Caption, Theme.textOnAccent, TextAlignmentOptions.Center);
            level.font = Theme.Font(TextStyle.Button);
            level.fontSize = level.fontSizeMax = 20f;
            level.rectTransform.Fill();
            return portrait;
        }
    }
}
