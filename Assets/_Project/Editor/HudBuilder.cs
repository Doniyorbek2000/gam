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
    /// <summary>Landscape HUD: player panels, status line, spin pad, power slider, shoot and new-game buttons.</summary>
    internal static class HudBuilder
    {
        private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

        public static void Build(GameController game, CueController cue)
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
            RectTransform root = canvasObject.GetComponent<RectTransform>();

            Sprite standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            Sprite background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            Sprite knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

            TMP_Text player0 = Label("Player 1", root, new Vector2(0f, 1f), new Vector2(40f, -30f), new Vector2(560f, 130f), TextAlignmentOptions.TopLeft, 46);
            TMP_Text player1 = Label("Player 2", root, new Vector2(1f, 1f), new Vector2(-40f, -30f), new Vector2(560f, 130f), TextAlignmentOptions.TopRight, 46);
            TMP_Text status = Label("Status", root, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(900f, 90f), TextAlignmentOptions.Top, 36);

            Slider power = PowerSlider(root, background, standard, knob);
            Label("Power Label", root, new Vector2(1f, 0.5f), new Vector2(-80f, 300f), new Vector2(200f, 50f), TextAlignmentOptions.Center, 30).text = "POWER";

            SpinPad spin = SpinPad(root, knob);
            Label("Spin Label", root, new Vector2(0f, 0f), new Vector2(160f, 290f), new Vector2(200f, 50f), TextAlignmentOptions.Center, 30).text = "SPIN";

            Button shoot = Button("Shoot", "SHOOT", root, standard, new Vector2(1f, 0f), new Vector2(-190f, 100f), new Vector2(280f, 120f), new Color(0.9f, 0.55f, 0.1f));
            Button newGame = Button("New Game", "NEW GAME", root, standard, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420f, 120f), new Color(0.2f, 0.6f, 0.3f));
            newGame.gameObject.SetActive(false);

            var hud = canvasObject.AddComponent<HudController>();
            SceneWiring.Set(hud, "game", game);
            SceneWiring.Set(hud, "cue", cue);
            SceneWiring.Set(hud, "powerSlider", power);
            SceneWiring.Set(hud, "spinPad", spin);
            SceneWiring.Set(hud, "shootButton", shoot);
            SceneWiring.Set(hud, "newGameButton", newGame);
            SceneWiring.Set(hud, "player0Label", player0);
            SceneWiring.Set(hud, "player1Label", player1);
            SceneWiring.Set(hud, "statusLabel", status);
        }

        private static TMP_Text Label(string name, RectTransform parent, Vector2 anchor, Vector2 position, Vector2 size, TextAlignmentOptions alignment, float fontSize)
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
            Place(go, parent, new Vector2(1f, 0.5f), new Vector2(-80f, -20f), new Vector2(60f, 560f));
            var slider = go.GetComponent<Slider>();
            slider.direction = Slider.Direction.BottomToTop;
            slider.value = 0.5f;
            go.transform.Find("Fill Area/Fill").GetComponent<Image>().color = new Color(0.95f, 0.45f, 0.1f);
            return slider;
        }

        private static SpinPad SpinPad(RectTransform parent, Sprite knob)
        {
            var pad = new GameObject("Spin Pad", typeof(RectTransform), typeof(Image));
            RectTransform padRect = Place(pad, parent, new Vector2(0f, 0f), new Vector2(160f, 160f), new Vector2(220f, 220f));
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
            return spin;
        }

        private static Button Button(string name, string caption, RectTransform parent, Sprite sprite, Vector2 anchor, Vector2 position, Vector2 size, Color color)
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

        private static RectTransform Place(GameObject go, RectTransform parent, Vector2 anchor, Vector2 position, Vector2 size)
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
