using BilliardGame.Presentation;
using BilliardGame.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace BilliardGame.Editor
{
    /// <summary>Landscape HUD: player panels, status line, shot clock, spin pad, power slider, shoot, new-game and menu buttons.</summary>
    internal static class HudBuilder
    {
        private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

        /// <summary>Builds the HUD; <paramref name="hudRoot"/> is the part hidden while the main menu shows.</summary>
        public static HudController Build(GameController game, CueController cue, out GameObject hudRoot)
        {
            var events = new GameObject("EventSystem");
            events.AddComponent<EventSystem>();
            events.AddComponent<InputSystemUIInputModule>();

            var canvasObject = new GameObject("HUD");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.matchWidthOrHeight = 1f;
            canvasObject.AddComponent<GraphicRaycaster>();

            // Everything lives inside the safe area so notches and rounded corners never cover a control.
            var safeObject = new GameObject("Safe Area", typeof(RectTransform));
            RectTransform root = safeObject.GetComponent<RectTransform>();
            root.SetParent(canvasObject.transform, false);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
            safeObject.AddComponent<SafeAreaFitter>();

            Sprite standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            Sprite background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            Sprite knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

            TMP_Text player0 = Label("Player 1", root, new Vector2(0f, 1f), new Vector2(40f, -30f), new Vector2(560f, 130f), TextAlignmentOptions.TopLeft, 46);
            TMP_Text player1 = Label("Player 2", root, new Vector2(1f, 1f), new Vector2(-40f, -30f), new Vector2(560f, 130f), TextAlignmentOptions.TopRight, 46);
            TMP_Text status = Label("Status", root, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(900f, 90f), TextAlignmentOptions.Top, 36);
            TMP_Text timer = Label("Turn Timer", root, new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(200f, 90f), TextAlignmentOptions.Top, 64);
            timer.text = string.Empty;
            timer.fontStyle = FontStyles.Bold;

            // Left thumb: power. Right thumb: fine aim, spin and shoot.
            Slider power = PowerSlider(root, background, standard, knob);
            Label("Power Label", root, new Vector2(0f, 0.5f), new Vector2(0f, 330f), new Vector2(200f, 50f), TextAlignmentOptions.Center, 30).text = "POWER";

            FineTuneWheel wheel = Wheel(root);

            Button spinIcon = SpinIcon(root, knob, out RectTransform previewArea, out RectTransform previewDot);
            SpinPad spin = SpinPad(root, knob, previewArea, previewDot);
            spin.gameObject.SetActive(false);

            Button shoot = Button("Shoot", "SHOOT", root, standard, new Vector2(1f, 0f), new Vector2(-190f, 100f), new Vector2(280f, 120f), new Color(0.9f, 0.55f, 0.1f));
            Button newGame = Button("New Game", "NEW GAME", root, standard, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420f, 120f), new Color(0.2f, 0.6f, 0.3f));
            newGame.gameObject.SetActive(false);
            Button menu = Button("Menu", "MENU", root, standard, new Vector2(0f, 0f), new Vector2(130f, 75f), new Vector2(200f, 90f), new Color(0.25f, 0.27f, 0.32f));
            menu.GetComponentInChildren<TextMeshProUGUI>().fontSize = 34;

            var hud = canvasObject.AddComponent<HudController>();
            SceneWiring.Set(hud, "game", game);
            SceneWiring.Set(hud, "cue", cue);
            SceneWiring.Set(hud, "powerSlider", power);
            SceneWiring.Set(hud, "spinPad", spin);
            SceneWiring.Set(hud, "spinIcon", spinIcon);
            SceneWiring.Set(hud, "fineTuneWheel", wheel);
            SceneWiring.Set(hud, "shootButton", shoot);
            SceneWiring.Set(hud, "newGameButton", newGame);
            SceneWiring.Set(hud, "player0Label", player0);
            SceneWiring.Set(hud, "player1Label", player1);
            SceneWiring.Set(hud, "statusLabel", status);
            SceneWiring.Set(hud, "turnTimerLabel", timer);
            SceneWiring.Set(hud, "menuButton", menu);
            hudRoot = safeObject;
            return hud;
        }

        internal static TMP_Text Label(string name, RectTransform parent, Vector2 anchor, Vector2 position, Vector2 size, TextAlignmentOptions alignment, float fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform));
            RectTransform rect = Place(go, parent, anchor, position, size);
            rect.pivot = anchor;
            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = name;
            text.alignment = alignment;
            text.fontSize = fontSize;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            return text;
        }

        private static Slider PowerSlider(RectTransform parent, Sprite background, Sprite fill, Sprite knob)
        {
            GameObject go = DefaultControls.CreateSlider(new DefaultControls.Resources { background = background, standard = fill, knob = knob });
            go.name = "Power";
            Place(go, parent, new Vector2(0f, 0.5f), new Vector2(100f, -20f), new Vector2(70f, 600f));
            var slider = go.GetComponent<Slider>();
            slider.direction = Slider.Direction.BottomToTop;
            slider.value = 0.5f;
            // DefaultControls lays the slider out horizontally (25% vertical insets, x-only padding); redo it for a vertical bar.
            Stretch(go.transform.Find("Background"), new Vector2(0.3f, 0f), new Vector2(0.7f, 1f), Vector2.zero);
            Stretch(go.transform.Find("Fill Area"), new Vector2(0.3f, 0f), new Vector2(0.7f, 1f), Vector2.zero);
            Stretch(go.transform.Find("Handle Slide Area"), Vector2.zero, Vector2.one, new Vector2(0f, 35f));
            var fillRect = (RectTransform)go.transform.Find("Fill Area/Fill");
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(1f, 0.5f);
            fillRect.sizeDelta = Vector2.zero;
            fillRect.GetComponent<Image>().color = new Color(0.95f, 0.45f, 0.1f);
            // Thumb-sized round handle spanning the bar's width (the default is a 20 px sliver).
            var handle = (RectTransform)go.transform.Find("Handle Slide Area/Handle");
            handle.anchorMin = new Vector2(0f, 0.5f);
            handle.anchorMax = new Vector2(1f, 0.5f);
            handle.sizeDelta = new Vector2(0f, 70f);
            return slider;
        }

        private static void Stretch(Transform child, Vector2 anchorMin, Vector2 anchorMax, Vector2 inset)
        {
            var rect = (RectTransform)child;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = inset;
            rect.offsetMax = -inset;
        }

        private static FineTuneWheel Wheel(RectTransform parent)
        {
            var frame = new GameObject("Fine Tune", typeof(RectTransform), typeof(Image));
            RectTransform frameRect = Place(frame, parent, new Vector2(1f, 0.5f), new Vector2(-95f, 40f), new Vector2(120f, 540f));
            frame.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.06f, 0.85f);

            var strip = new GameObject("Ridges", typeof(RectTransform), typeof(RawImage));
            RectTransform stripRect = strip.GetComponent<RectTransform>();
            stripRect.SetParent(frameRect, false);
            stripRect.anchorMin = Vector2.zero;
            stripRect.anchorMax = Vector2.one;
            stripRect.offsetMin = new Vector2(10f, 10f);
            stripRect.offsetMax = new Vector2(-10f, -10f);
            var ridges = strip.GetComponent<RawImage>();
            ridges.texture = ProceduralTextures.Ridges();
            ridges.color = new Color(0.75f, 0.75f, 0.78f);
            ridges.uvRect = new Rect(0f, 0f, 1f, 4f);
            ridges.raycastTarget = false;

            var wheel = frame.AddComponent<FineTuneWheel>();
            SceneWiring.Set(wheel, "ridges", ridges);
            return wheel;
        }

        /// <summary>Small cue-ball icon showing the current tip offset; tapping it opens the full spin pad.</summary>
        private static Button SpinIcon(RectTransform parent, Sprite knob, out RectTransform area, out RectTransform dot)
        {
            var icon = new GameObject("Spin Icon", typeof(RectTransform), typeof(Image), typeof(Button));
            area = Place(icon, parent, new Vector2(1f, 0f), new Vector2(-430f, 100f), new Vector2(130f, 130f));
            Image face = icon.GetComponent<Image>();
            face.sprite = knob;
            face.color = new Color(0.95f, 0.95f, 0.92f);

            var tip = new GameObject("Tip", typeof(RectTransform), typeof(Image));
            dot = tip.GetComponent<RectTransform>();
            dot.SetParent(area, false);
            dot.sizeDelta = new Vector2(26f, 26f);
            Image tipImage = tip.GetComponent<Image>();
            tipImage.sprite = knob;
            tipImage.color = new Color(0.85f, 0.1f, 0.1f);
            tipImage.raycastTarget = false;
            return icon.GetComponent<Button>();
        }

        private static SpinPad SpinPad(RectTransform parent, Sprite knob, RectTransform previewArea, RectTransform previewDot)
        {
            var pad = new GameObject("Spin Pad", typeof(RectTransform), typeof(Image));
            RectTransform padRect = Place(pad, parent, new Vector2(1f, 0f), new Vector2(-430f, 340f), new Vector2(300f, 300f));
            Image face = pad.GetComponent<Image>();
            face.sprite = knob;
            face.color = new Color(0.95f, 0.95f, 0.92f);

            var dot = new GameObject("Tip", typeof(RectTransform), typeof(Image));
            RectTransform dotRect = dot.GetComponent<RectTransform>();
            dotRect.SetParent(padRect, false);
            dotRect.sizeDelta = new Vector2(40f, 40f);
            Image tip = dot.GetComponent<Image>();
            tip.sprite = knob;
            tip.color = new Color(0.85f, 0.1f, 0.1f);
            tip.raycastTarget = false;

            var spin = pad.AddComponent<SpinPad>();
            SceneWiring.Set(spin, "area", padRect);
            SceneWiring.Set(spin, "dot", dotRect);
            SceneWiring.Set(spin, "previewArea", previewArea);
            SceneWiring.Set(spin, "previewDot", previewDot);
            return spin;
        }

        internal static Button Button(string name, string caption, RectTransform parent, Sprite sprite, Vector2 anchor, Vector2 position, Vector2 size, Color color)
        {
            GameObject go = TMP_DefaultControls.CreateButton(new TMP_DefaultControls.Resources { standard = sprite });
            go.name = name;
            Place(go, parent, anchor, position, size);
            go.GetComponent<Image>().color = color;
            var text = go.GetComponentInChildren<TextMeshProUGUI>();
            text.text = caption;
            text.fontSize = 44;
            text.fontStyle = FontStyles.Bold;
            text.color = Color.white;
            return go.GetComponent<Button>();
        }

        internal static RectTransform Place(GameObject go, RectTransform parent, Vector2 anchor, Vector2 position, Vector2 size)
        {
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }
    }
}
