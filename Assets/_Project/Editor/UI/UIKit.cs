using BilliardGame.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BilliardGame.Editor.UI
{
    internal enum ButtonKind
    {
        Primary,
        Secondary,
        Ghost,
        Danger,
        Success,
    }

    internal sealed class ButtonParts
    {
        public Button Button;
        public RectTransform Rect;
        public Image Fill;
        public TMP_Text Label;
        public Image Icon;
        public BadgeView Badge;
    }

    /// <summary>
    /// Design-system factory (docs/DESIGN_SYSTEM.md §3). Every screen is assembled from these pieces so spacing, type,
    /// states and touch sizes stay consistent; <see cref="UIKitPrefabs"/> saves the same pieces as prefabs.
    /// </summary>
    internal static partial class UIKit
    {
        public static UITheme Theme => UIAssets.Theme;

        // ---- Layout primitives ---------------------------------------------------------------------------------------

        public static RectTransform Node(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        public static RectTransform At(this RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size, Vector2? pivot = null)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot ?? anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        /// <summary>Stretch to the parent with insets (left, bottom, right, top).</summary>
        public static RectTransform Fill(this RectTransform rect, float left = 0f, float bottom = 0f, float right = 0f, float top = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
            return rect;
        }

        public static RectTransform Span(this RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return rect;
        }

        public static Image Image(Transform parent, string name, Sprite sprite, Color color, bool raycast = false)
        {
            RectTransform rect = Node(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = raycast;
            if (sprite != null && sprite.border.sqrMagnitude > 0f) image.type = UnityEngine.UI.Image.Type.Sliced;
            return image;
        }

        public static Image Panel(Transform parent, string name, Color color, int radius = 20, bool shadow = false, bool stroke = true)
        {
            RectTransform root = Node(name, parent);
            if (shadow) Image(root, "Shadow", UIAssets.Shadow, Theme.shadow).rectTransform.Fill(-18f, -24f, -18f, -12f);
            Image fill = Image(root, "Fill", UIAssets.Rounded(radius), color, true);
            fill.rectTransform.Fill();
            if (stroke) Image(root, "Stroke", UIAssets.Stroke(radius), Theme.stroke).rectTransform.Fill();
            return fill;
        }

        public static RectTransform Row(Transform parent, string name, float spacing, TextAnchor align = TextAnchor.MiddleLeft, bool controlHeight = true)
        {
            RectTransform rect = Node(name, parent);
            var row = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = spacing;
            row.childAlignment = align;
            row.childControlWidth = true;
            row.childControlHeight = controlHeight;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;
            return rect;
        }

        public static RectTransform Column(Transform parent, string name, float spacing, TextAnchor align = TextAnchor.UpperLeft)
        {
            RectTransform rect = Node(name, parent);
            var column = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            column.spacing = spacing;
            column.childAlignment = align;
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;
            return rect;
        }

        public static LayoutElement Size(Component target, float width = -1f, float height = -1f, float flexibleWidth = -1f)
        {
            var element = target.GetComponent<LayoutElement>() ?? target.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = width;
            element.minWidth = width >= 0f && flexibleWidth < 0f ? width : -1f;
            element.preferredHeight = height;
            element.minHeight = height;
            element.flexibleWidth = flexibleWidth;
            // Rows that do not control child height (controlHeight: false) use sizeDelta, so keep it in step.
            if (target.transform is RectTransform rect) rect.sizeDelta = new Vector2(width >= 0f ? width : rect.sizeDelta.x, height >= 0f ? height : rect.sizeDelta.y);
            return element;
        }

        // ---- Text and icons ------------------------------------------------------------------------------------------

        /// <summary>Label from the theme type scale. A key becomes a <see cref="LocalizedText"/>; null key = dynamic text.</summary>
        public static TMP_Text Text(Transform parent, string name, string key, TextStyle style, Color? color = null, TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft, bool upper = false)
        {
            RectTransform rect = Node(name, parent);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = Theme.Font(style);
            text.fontSize = Theme.Size(style);
            text.color = color ?? (style == TextStyle.Caption ? Theme.textSecondary : Theme.textPrimary);
            text.alignment = align;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.enableAutoSizing = true;
            text.fontSizeMax = text.fontSize;
            text.fontSizeMin = Mathf.Round(text.fontSize * 0.62f);
            if (style == TextStyle.Display || style == TextStyle.H1) text.characterSpacing = 1f;
            if (style == TextStyle.Button) text.characterSpacing = 2f;
            text.text = string.Empty;
            if (key == null) return text;
            string preview = LocalizationBuilder.Uz(key);
            text.text = upper ? preview.ToUpperInvariant() : preview;
            rect.gameObject.AddComponent<LocalizedText>().Configure(key, upper);
            return text;
        }

        public static TMP_Text Wrap(TMP_Text text)
        {
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            return text;
        }

        public static Image Icon(Transform parent, string name, float size, Color? color = null)
        {
            Image image = Image(parent, "Icon " + name, UIAssets.Icon(name), color ?? Theme.textPrimary);
            image.preserveAspect = true;
            image.rectTransform.sizeDelta = new Vector2(size, size);
            return image;
        }

        // ---- Buttons -------------------------------------------------------------------------------------------------

        /// <summary>Pill/rounded button with optional icon; all four states via <see cref="UIButtonFx"/>.</summary>
        public static ButtonParts Button(Transform parent, string name, ButtonKind kind, string labelKey, string icon, Vector2 size, int radius = 20, float iconSize = 0f)
        {
            RectTransform root = Node(name, parent);
            root.sizeDelta = size;
            var group = root.gameObject.AddComponent<CanvasGroup>();
            bool filled = kind != ButtonKind.Ghost && kind != ButtonKind.Secondary;
            if (kind != ButtonKind.Ghost) Image(root, "Shadow", UIAssets.Shadow, new Color(0f, 0f, 0f, filled ? 0.5f : 0.3f)).rectTransform.Fill(-14f, -20f, -14f, -6f);
            Image lip = Image(root, "Lip", UIAssets.Rounded(radius), Darken(ButtonColor(kind), 0.55f));
            lip.rectTransform.Fill(0f, -6f, 0f, 0f);
            lip.gameObject.SetActive(filled);
            Image fill = Image(root, "Fill", UIAssets.Rounded(radius), ButtonColor(kind), true);
            fill.rectTransform.Fill();
            if (filled) Image(root, "Gloss", UIAssets.GradientUp, new Color(1f, 1f, 1f, 0.22f)).rectTransform.Span(new Vector2(0.02f, 0.5f), new Vector2(0.98f, 0.96f), Vector2.zero, Vector2.zero);
            if (!filled) Image(root, "Stroke", UIAssets.Stroke(radius), kind == ButtonKind.Ghost ? new Color(1f, 1f, 1f, 0.22f) : Theme.stroke).rectTransform.Fill();
            Image highlight = Image(root, "Highlight", UIAssets.Rounded(radius), Color.white);
            highlight.rectTransform.Fill();
            Image selected = Image(root, "Selected", UIAssets.Stroke(radius), Theme.accent);
            selected.rectTransform.Fill(-4f, -4f, -4f, -4f);
            selected.gameObject.SetActive(false);

            RectTransform content = Row(root, "Content", 12f, TextAnchor.MiddleCenter);
            content.Fill(20f, 0f, 20f, 0f);
            var parts = new ButtonParts { Rect = root, Fill = fill };
            float glyph = iconSize > 0f ? iconSize : Mathf.Min(size.y * 0.5f, 52f);
            if (icon != null)
            {
                parts.Icon = Icon(content, icon, glyph, kind == ButtonKind.Primary ? Theme.textOnAccent : Theme.textPrimary);
                Size(parts.Icon, glyph, glyph);
            }

            if (labelKey != null || icon == null)
            {
                Color ink = kind == ButtonKind.Primary ? Theme.textOnAccent : Theme.textPrimary;
                parts.Label = Text(content, "Label", labelKey, TextStyle.Button, ink, TextAlignmentOptions.Center, true);
                parts.Label.fontSize = parts.Label.fontSizeMax = Mathf.Min(Theme.buttonSize, size.y * 0.42f);
                parts.Label.fontSizeMin = parts.Label.fontSize * 0.6f;
                Size(parts.Label, -1f, size.y, 1f);
            }

            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = fill;
            parts.Button = button;
            root.gameObject.AddComponent<UIButtonFx>().Configure(highlight, selected.gameObject, group, kind == ButtonKind.Primary);
            return parts;
        }

        /// <summary>Square icon-only button (≥ 72 px target) with an optional red badge.</summary>
        public static ButtonParts IconButton(Transform parent, string name, string icon, float size = 88f, bool badge = false, bool circle = false)
        {
            ButtonParts parts = Button(parent, name, ButtonKind.Secondary, null, icon, new Vector2(size, size), circle ? 32 : 20, size * 0.5f);
            if (circle) parts.Fill.sprite = UIAssets.Circle;
            if (badge) parts.Badge = Badge(parts.Rect, new Vector2(-6f, -6f));
            return parts;
        }

        public static Color ButtonColor(ButtonKind kind)
        {
            switch (kind)
            {
                case ButtonKind.Primary: return Theme.accent;
                case ButtonKind.Danger: return Theme.danger;
                case ButtonKind.Success: return Theme.success;
                case ButtonKind.Ghost: return new Color(1f, 1f, 1f, 0.04f);
                default: return Theme.surfaceRaised;
            }
        }

        public static Color Darken(Color color, float factor) => new Color(color.r * factor, color.g * factor, color.b * factor, color.a);

        public static void SetLabel(ButtonParts parts, string key)
        {
            var loc = parts.Label.GetComponent<LocalizedText>();
            if (loc != null) loc.Configure(key, true);
            parts.Label.text = LocalizationBuilder.Uz(key).ToUpperInvariant();
        }

        // ---- Widgets -------------------------------------------------------------------------------------------------

        public static BadgeView Badge(RectTransform parent, Vector2 offset)
        {
            Image dot = Image(parent, "Badge", UIAssets.Circle, Theme.danger);
            dot.rectTransform.At(Vector2.one, offset, new Vector2(34f, 34f), new Vector2(0.5f, 0.5f));
            Image(dot.transform, "Ring", UIAssets.Ring, new Color(0f, 0f, 0f, 0.45f)).rectTransform.Fill(-2f, -2f, -2f, -2f);
            TMP_Text count = Text(dot.transform, "Count", null, TextStyle.Caption, Color.white, TextAlignmentOptions.Center);
            count.fontSize = count.fontSizeMax = 20f;
            count.rectTransform.Fill();
            var badge = dot.gameObject.AddComponent<BadgeView>();
            badge.Configure(count);
            dot.gameObject.SetActive(false);
            return badge;
        }

        public static CurrencyPill Pill(Transform parent, string name, string icon, Color tint, float width = 260f)
        {
            Image back = Panel(parent, name, Theme.surfaceSunken, 32);
            RectTransform root = (RectTransform)back.transform.parent;
            root.sizeDelta = new Vector2(width, 72f);
            Image glyph = Icon(root, icon, 56f, tint);
            glyph.rectTransform.At(new Vector2(0f, 0.5f), new Vector2(10f, 0f), new Vector2(56f, 56f), new Vector2(0f, 0.5f));
            TMP_Text value = Text(root, "Value", null, TextStyle.Number, Theme.textPrimary, TextAlignmentOptions.MidlineRight);
            value.fontSize = value.fontSizeMax = 32f;
            value.rectTransform.Span(Vector2.zero, Vector2.one, new Vector2(72f, 0f), new Vector2(-72f, 0f));
            var count = value.gameObject.AddComponent<CountUpText>();
            count.Configure(string.Empty, Theme.countUpDuration);
            ButtonParts plus = Button(root, "Plus", ButtonKind.Success, null, "add", new Vector2(56f, 56f), 20, 36f);
            plus.Rect.At(new Vector2(1f, 0.5f), new Vector2(-8f, 0f), new Vector2(56f, 56f), new Vector2(1f, 0.5f));
            var pill = root.gameObject.AddComponent<CurrencyPill>();
            pill.Configure(count, plus.Button);
            return pill;
        }

        public static ProgressBar Progress(Transform parent, string name, Color color, float height = 14f)
        {
            RectTransform root = Node(name, parent);
            root.sizeDelta = new Vector2(200f, height);
            Image(root, "Track", UIAssets.Pill, new Color(0f, 0f, 0f, 0.45f)).rectTransform.Fill();
            Image fill = Image(root, "Fill", UIAssets.Pill, color);
            fill.rectTransform.Fill(2f, 2f, 2f, 2f);
            // Filled + sliced is not supported, so the fill uses a horizontal fill on a plain rounded sprite.
            fill.type = UnityEngine.UI.Image.Type.Filled;
            fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            fill.fillAmount = 0.5f;
            var bar = root.gameObject.AddComponent<ProgressBar>();
            bar.Configure(fill);
            return bar;
        }

        public static TabBar Tabs(Transform parent, string name, string[] keys, string[] icons, Vector2 tabSize, bool vertical, float spacing = 12f)
        {
            RectTransform root = vertical ? Column(parent, name, spacing) : Row(parent, name, spacing, TextAnchor.MiddleLeft, false);
            var buttons = new Button[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                ButtonParts tab = Button(root, "Tab " + keys[i], ButtonKind.Ghost, keys[i], icons?[i], tabSize, 20, 36f);
                if (vertical) tab.Rect.GetComponentInChildren<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleLeft;
                tab.Label.alignment = vertical ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.Center;
                Size(tab.Rect, tabSize.x, tabSize.y);
                // Selected tab = accent fill under the ghost outline.
                Image on = Image(tab.Rect.Find("Selected"), "On", UIAssets.Rounded(20), new Color(Theme.accent.r, Theme.accent.g, Theme.accent.b, 0.2f));
                on.rectTransform.Fill(4f, 4f, 4f, 4f);
                buttons[i] = tab.Button;
            }

            var bar = root.gameObject.AddComponent<TabBar>();
            bar.Configure(buttons);
            return bar;
        }

        public static ToggleSwitch Toggle(Transform parent, string name)
        {
            RectTransform root = Node(name, parent);
            root.sizeDelta = new Vector2(96f, 52f);
            Image track = Image(root, "Track", UIAssets.Pill, Theme.surfaceSunken, true);
            track.rectTransform.Fill();
            Image knob = Image(root, "Knob", UIAssets.Circle, Color.white);
            knob.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(-22f, 0f), new Vector2(42f, 42f));
            var toggle = root.gameObject.AddComponent<ToggleSwitch>();
            toggle.Configure(track, knob.rectTransform, Theme.success, new Color(0.3f, 0.34f, 0.44f));
            Size(root, 96f, 52f);
            return toggle;
        }

        public static Slider Slider(Transform parent, string name, float width)
        {
            RectTransform root = Node(name, parent);
            root.sizeDelta = new Vector2(width, 56f);
            Image track = Image(root, "Track", UIAssets.Pill, new Color(0f, 0f, 0f, 0.5f), true);
            track.rectTransform.Span(new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, -7f), new Vector2(0f, 7f));
            RectTransform fillArea = Node("Fill Area", root).Span(new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, -7f), new Vector2(0f, 7f));
            Image fill = Image(fillArea, "Fill", UIAssets.Pill, Theme.accent);
            fill.rectTransform.Fill();
            // Zero-height area: the Slider stretches the handle to the area's height, so this keeps it a 48px circle.
            RectTransform handleArea = Node("Handle Area", root).Span(new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(20f, 0f), new Vector2(-20f, 0f));
            Image handle = Image(handleArea, "Handle", UIAssets.Circle, Color.white, true);
            handle.rectTransform.sizeDelta = new Vector2(48f, 48f);
            Image(handle.transform, "Dot", UIAssets.Circle, Theme.accent).rectTransform.Fill(14f, 14f, 14f, 14f);
            var slider = root.gameObject.AddComponent<UnityEngine.UI.Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            slider.transition = Selectable.Transition.None;
            Size(root, width, 56f);
            return slider;
        }

        public static TMP_Dropdown Dropdown(Transform parent, string name, Vector2 size)
        {
            GameObject go = TMP_DefaultControls.CreateDropdown(new TMP_DefaultControls.Resources
            {
                standard = UIAssets.Rounded(20), background = UIAssets.Rounded(20), inputField = UIAssets.Rounded(20),
                knob = UIAssets.Circle, checkmark = UIAssets.Icon("check"), dropdown = UIAssets.Icon("chevron_right"), mask = UIAssets.Rounded(12),
            });
            go.name = name;
            go.layer = 5;
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.sizeDelta = size;
            go.GetComponent<Image>().color = Theme.surfaceRaised;
            var dropdown = go.GetComponent<TMP_Dropdown>();
            foreach (TMP_Text text in go.GetComponentsInChildren<TMP_Text>(true))
            {
                text.font = Theme.Font(TextStyle.Body);
                text.fontSize = Theme.bodySize;
                text.color = Theme.textPrimary;
            }

            Transform arrow = rect.Find("Arrow");
            arrow.localRotation = Quaternion.Euler(0f, 0f, -90f);
            ((RectTransform)arrow).sizeDelta = new Vector2(36f, 36f);
            arrow.GetComponent<Image>().color = Theme.textSecondary;
            var template = (RectTransform)rect.Find("Template");
            template.GetComponent<Image>().color = Theme.surface;
            template.sizeDelta = new Vector2(0f, 300f);
            Transform item = template.Find("Viewport/Content/Item");
            ((RectTransform)item).sizeDelta = new Vector2(0f, 72f);
            ((RectTransform)template.Find("Viewport/Content")).sizeDelta = new Vector2(0f, 72f);
            item.Find("Item Background").GetComponent<Image>().color = Theme.surfaceRaised;
            Size(rect, size.x, size.y);
            return dropdown;
        }

        public static RectTransform Spinner(Transform parent, float size)
        {
            Image ring = Image(parent, "Spinner", UIAssets.Ring, Theme.accent);
            ring.type = UnityEngine.UI.Image.Type.Filled;
            ring.fillMethod = UnityEngine.UI.Image.FillMethod.Radial360;
            ring.fillAmount = 0.7f;
            ring.rectTransform.sizeDelta = new Vector2(size, size);
            ring.gameObject.AddComponent<global::BilliardGame.UI.Spinner>();
            return ring.rectTransform;
        }

        public static EmptyStateView EmptyState(Transform parent)
        {
            RectTransform root = Column(parent, "Empty State", 16f, TextAnchor.MiddleCenter);
            root.GetComponent<VerticalLayoutGroup>().childForceExpandWidth = false;
            Image halo = Image(root, "Halo", UIAssets.Circle, new Color(1f, 1f, 1f, 0.06f));
            Size(halo, 160f, 160f);
            Image icon = Image(halo.transform, "Icon", UIAssets.Icon("inventory"), Theme.textMuted);
            icon.preserveAspect = true;
            icon.rectTransform.Fill(36f, 36f, 36f, 36f);
            TMP_Text title = Text(root, "Title", null, TextStyle.H2, null, TextAlignmentOptions.Center);
            Size(title, 700f, 52f);
            TMP_Text hint = Wrap(Text(root, "Hint", null, TextStyle.Body, Theme.textSecondary, TextAlignmentOptions.Center));
            Size(hint, 640f, 80f);
            ButtonParts action = Button(root, "Action", ButtonKind.Primary, null, null, new Vector2(360f, 88f));
            Size(action.Rect, 360f, 88f);
            var view = root.gameObject.AddComponent<EmptyStateView>();
            view.Configure(icon, title, hint, action.Button, action.Label);
            return view;
        }

        public static ScrollRect Scroll(Transform parent, string name, bool vertical, out RectTransform content)
        {
            RectTransform root = Node(name, parent);
            var mask = root.gameObject.AddComponent<RectMask2D>();
            mask.softness = new Vector2Int(0, vertical ? 24 : 0);
            root.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);   // catches drags anywhere in the viewport
            var scroll = root.gameObject.AddComponent<ScrollRect>();
            content = Node("Content", root);
            content.anchorMin = vertical ? new Vector2(0f, 1f) : new Vector2(0f, 0f);
            content.anchorMax = vertical ? Vector2.one : new Vector2(0f, 1f);
            content.pivot = vertical ? new Vector2(0.5f, 1f) : new Vector2(0f, 0.5f);
            content.offsetMin = content.offsetMax = Vector2.zero;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            if (vertical) fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            else fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = content;
            scroll.viewport = root;
            scroll.horizontal = !vertical;
            scroll.vertical = vertical;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 30f;
            return scroll;
        }

        public static GridLayoutGroup Grid(RectTransform content, Vector2 cell, int columns, float spacing)
        {
            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = cell;
            grid.spacing = new Vector2(spacing, spacing);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.padding = new RectOffset(8, 8, 8, 24);
            return grid;
        }

        public static void Save(Object target) => EditorUtility.SetDirty(target);
    }
}
