using BilliardGame.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BilliardGame.Editor.UI
{
    /// <summary>Composite pieces shared by several screens: icon cards, rows, stat cards, input fields, wiring helper.</summary>
    internal static partial class UIKit
    {
        /// <summary>Assigns private serialized fields: alternating field name and value (Object or Object[]).</summary>
        public static void Wire(Object target, params object[] pairs)
        {
            for (int i = 0; i + 1 < pairs.Length; i += 2)
            {
                var field = (string)pairs[i];
                if (pairs[i + 1] is Object[] array) SceneWiring.SetArray(target, field, array);
                else SceneWiring.Set(target, field, (Object)pairs[i + 1]);
            }
        }

        /// <summary>Button with the icon above a caption (lobby rails, mode cards, quick actions).</summary>
        public static ButtonParts IconCard(Transform parent, string name, string icon, string key, Vector2 size, ButtonKind kind = ButtonKind.Secondary, float iconSize = 52f, Color? iconColor = null)
        {
            ButtonParts parts = Button(parent, name, kind, null, icon, size, 20, iconSize);
            RectTransform content = (RectTransform)parts.Rect.Find("Content");
            Object.DestroyImmediate(content.GetComponent<HorizontalLayoutGroup>());
            var column = content.gameObject.AddComponent<VerticalLayoutGroup>();
            column.childAlignment = TextAnchor.MiddleCenter;
            column.spacing = 6f;
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = false;
            column.childForceExpandHeight = false;
            content.Fill(10f, 10f, 10f, 10f);
            if (iconColor.HasValue) parts.Icon.color = iconColor.Value;
            parts.Label = Text(content, "Label", key, TextStyle.Caption, Theme.textPrimary, TextAlignmentOptions.Center, true);
            parts.Label.font = Theme.Font(TextStyle.Button);
            parts.Label.fontSize = parts.Label.fontSizeMax = 20f;
            parts.Label.characterSpacing = 1f;
            Size(parts.Label, size.x - 20f, 28f);
            return parts;
        }

        /// <summary>"SOON" chip straddling the top edge of a card, clear of the centred icon.</summary>
        public static GameObject SoonTag(RectTransform parent)
        {
            GameObject tag = Tag(parent, "Soon", "common.soon", new Color(0.32f, 0.36f, 0.48f), Vector2.one, new Vector2(-6f, 13f));
            ((RectTransform)tag.transform).sizeDelta = new Vector2(76f, 26f);
            TMP_Text label = tag.GetComponentInChildren<TMP_Text>();
            label.fontSize = label.fontSizeMax = 15f;
            label.fontSizeMin = 10f;
            label.rectTransform.Fill(6f, 0f, 6f, 0f);
            return tag;
        }

        public static TMP_InputField Field(Transform parent, string name, Vector2 size, string placeholderKey)
        {
            GameObject go = TMP_DefaultControls.CreateInputField(new TMP_DefaultControls.Resources { inputField = UIAssets.Rounded(20) });
            go.name = name;
            go.layer = 5;
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.sizeDelta = size;
            go.GetComponent<Image>().color = Theme.surfaceSunken;
            Image(rect, "Stroke", UIAssets.Stroke(20), Theme.stroke).rectTransform.Fill();
            var input = go.GetComponent<TMP_InputField>();
            input.pointSize = 34f;
            input.caretColor = Theme.accent;
            input.selectionColor = new Color(Theme.accent.r, Theme.accent.g, Theme.accent.b, 0.3f);
            foreach (TMP_Text text in go.GetComponentsInChildren<TMP_Text>(true))
            {
                text.font = Theme.Font(TextStyle.Body);
                text.fontSize = 34f;
                text.alignment = TextAlignmentOptions.Center;
                text.color = Theme.textPrimary;
            }

            var hint = (TMP_Text)input.placeholder;
            hint.text = placeholderKey != null ? LocalizationBuilder.Uz(placeholderKey) : string.Empty;
            hint.color = Theme.textMuted;
            if (placeholderKey != null) hint.gameObject.AddComponent<LocalizedText>().Configure(placeholderKey, false);
            Size(rect, size.x, size.y);
            return input;
        }

        /// <summary>List row: chip (left accent), icon, title + subtitle, value on the right, optional bar and check.</summary>
        public static RowView Row(Transform parent, string name, string id, string icon, bool bar, bool button, float height = 96f)
        {
            Image fill = Panel(parent, name, Theme.surface, 20);
            var root = (RectTransform)fill.transform.parent;
            Size(root, -1f, height);
            Image chip = Image(root, "Chip", UIAssets.Pill, Theme.accent);
            chip.rectTransform.Span(new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(8f, 16f), new Vector2(14f, -16f));
            Image glyph = null;
            float left = 30f;
            if (icon != null)
            {
                glyph = Icon(root, icon, 52f, Theme.textPrimary);
                glyph.rectTransform.At(new Vector2(0f, 0.5f), new Vector2(30f, 0f), new Vector2(52f, 52f), new Vector2(0f, 0.5f));
                left = 100f;
            }

            TMP_Text title = Text(root, "Title", null, TextStyle.Body, Theme.textPrimary);
            title.font = Theme.Font(TextStyle.H2);
            title.rectTransform.Span(new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(left, 0f), new Vector2(-220f, -10f));
            TMP_Text subtitle = Text(root, "Subtitle", null, TextStyle.Caption);
            subtitle.rectTransform.Span(new Vector2(0f, 0f), new Vector2(1f, 0.5f), new Vector2(left, bar ? 26f : 10f), new Vector2(-220f, 0f));
            TMP_Text value = Text(root, "Value", null, TextStyle.Number, Theme.textPrimary, TextAlignmentOptions.MidlineRight);
            value.fontSize = value.fontSizeMax = 28f;
            value.rectTransform.Span(new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-210f, 0f), new Vector2(-24f, 0f));
            ProgressBar progress = null;
            if (bar)
            {
                progress = Progress(root, "Bar", Theme.accent, 10f);
                ((RectTransform)progress.transform).Span(new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(left, 12f), new Vector2(-220f, 22f));
            }

            Image check = Image(root, "Check", UIAssets.Circle, Theme.success);
            check.rectTransform.At(new Vector2(1f, 1f), new Vector2(-10f, -10f), new Vector2(30f, 30f), Vector2.one);
            Icon(check.transform, "check", 22f, Color.white).rectTransform.At(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(22f, 22f));
            check.gameObject.SetActive(false);

            Button tap = null;
            if (button)
            {
                tap = root.gameObject.AddComponent<Button>();
                tap.targetGraphic = fill;
                tap.transition = Selectable.Transition.ColorTint;
            }

            var row = root.gameObject.AddComponent<RowView>();
            row.Configure(id, glyph, chip, title, subtitle, value, progress, check.gameObject, tap);
            return row;
        }

        /// <summary>Stat card: icon, big value, caption.</summary>
        public static TMP_Text StatCard(Transform parent, string name, string icon, string key)
        {
            Image fill = Panel(parent, name, Theme.surface, 20, true);
            var root = (RectTransform)fill.transform.parent;
            Image glyph = Icon(root, icon, 44f, Theme.accent);
            glyph.rectTransform.At(new Vector2(0f, 1f), new Vector2(22f, -20f), new Vector2(44f, 44f), new Vector2(0f, 1f));
            TMP_Text label = Text(root, "Label", key, TextStyle.Caption, Theme.textSecondary, TextAlignmentOptions.MidlineLeft, true);
            label.rectTransform.Span(new Vector2(0f, 1f), Vector2.one, new Vector2(78f, -64f), new Vector2(-16f, -20f));
            TMP_Text value = Text(root, "Value", null, TextStyle.H1, Theme.textPrimary, TextAlignmentOptions.BottomLeft);
            value.rectTransform.Span(Vector2.zero, new Vector2(1f, 0.6f), new Vector2(22f, 14f), new Vector2(-16f, 0f));
            value.fontSizeMin = 18f;
            value.text = "0";
            return value;
        }

        /// <summary>Settings row: icon, label (+hint), control slot on the right. Returns the control slot.</summary>
        public static RectTransform SettingRow(Transform parent, string name, string icon, string key, string hintKey = null, float controlWidth = 420f)
        {
            Image fill = Panel(parent, name, Theme.surface, 20);
            var root = (RectTransform)fill.transform.parent;
            Size(root, -1f, hintKey != null ? 108f : 92f);
            Image glyph = Icon(root, icon, 44f, Theme.accent);
            glyph.rectTransform.At(new Vector2(0f, 0.5f), new Vector2(26f, 0f), new Vector2(44f, 44f), new Vector2(0f, 0.5f));
            TMP_Text label = Text(root, "Label", key, TextStyle.Body);
            label.font = Theme.Font(TextStyle.H2);
            label.rectTransform.Span(new Vector2(0f, hintKey != null ? 0.45f : 0f), Vector2.one, new Vector2(92f, 0f), new Vector2(-controlWidth - 40f, 0f));
            if (hintKey != null)
            {
                TMP_Text hint = Text(root, "Hint", hintKey, TextStyle.Caption);
                hint.rectTransform.Span(Vector2.zero, new Vector2(1f, 0.48f), new Vector2(92f, 10f), new Vector2(-controlWidth - 40f, 0f));
            }

            RectTransform slot = Node("Control", root);
            slot.Span(new Vector2(1f, 0f), Vector2.one, new Vector2(-controlWidth - 24f, 0f), new Vector2(-24f, 0f));
            return slot;
        }

        /// <summary>Small segmented control (quality, FPS) sized to fill <paramref name="slot"/>.</summary>
        public static TabBar Segmented(RectTransform slot, string name, string[] keys, float segmentWidth)
        {
            TabBar bar = Tabs(slot, name, keys, null, new Vector2(segmentWidth, 64f), false, 8f);
            var rect = (RectTransform)bar.transform;
            rect.Fill();
            rect.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleRight;
            foreach (TMP_Text label in rect.GetComponentsInChildren<TMP_Text>(true))
            {
                label.fontSize = label.fontSizeMax = 24f;
            }

            return bar;
        }

        /// <summary>Plain number labels without a key (FPS segments).</summary>
        public static void Literal(TabBar bar, string[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                TMP_Text label = bar.Tab(i).GetComponentInChildren<TMP_Text>(true);
                var loc = label.GetComponent<LocalizedText>();
                if (loc != null) Object.DestroyImmediate(loc);
                label.text = values[i];
            }
        }
    }
}
