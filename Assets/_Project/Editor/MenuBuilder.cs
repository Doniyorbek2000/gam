using BilliardGame.Online;
using BilliardGame.Presentation;
using BilliardGame.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BilliardGame.Editor
{
    /// <summary>Main menu overlay: nickname, Quick Match, Play with Friend (room code), Offline, status and cancel.</summary>
    internal static class MenuBuilder
    {
        private static readonly Color Primary = new Color(0.9f, 0.55f, 0.1f);
        private static readonly Color Secondary = new Color(0.2f, 0.45f, 0.75f);
        private static readonly Color Neutral = new Color(0.25f, 0.27f, 0.32f);

        public static void Build(GameController game, OnlineMatchController online, HudController hud, GameObject hudRoot)
        {
            var canvasObject = new GameObject("Main Menu");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;
            canvasObject.AddComponent<GraphicRaycaster>();

            var panel = new GameObject("Menu Panel", typeof(RectTransform), typeof(Image));
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.SetParent(canvasObject.transform, false);
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0.03f, 0.04f, 0.06f, 0.82f);
            panel.AddComponent<SafeAreaFitter>();

            Sprite standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            Sprite inputBackground = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd");
            Vector2 centre = new Vector2(0.5f, 0.5f);

            TMP_Text title = HudBuilder.Label("Title", panelRect, new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(1200f, 140f), TextAlignmentOptions.Top, 110);
            title.text = "BILLIARDS";
            title.fontStyle = FontStyles.Bold;

            TMP_Text nickLabel = HudBuilder.Label("Nickname Label", panelRect, centre, new Vector2(-420f, 300f), new Vector2(260f, 90f), TextAlignmentOptions.Right, 40);
            nickLabel.text = "Nickname";
            TMP_InputField nickname = Input("Nickname", panelRect, inputBackground, new Vector2(70f, 300f), new Vector2(600f, 90f), "Your name");

            RectTransform mainButtons = Group("Main Buttons", panelRect);
            Button quick = HudBuilder.Button("Quick Match", "QUICK MATCH", mainButtons, standard, centre, new Vector2(0f, 150f), new Vector2(640f, 110f), Primary);
            Button friend = HudBuilder.Button("Play With Friend", "PLAY WITH FRIEND", mainButtons, standard, centre, new Vector2(0f, 20f), new Vector2(640f, 110f), Secondary);
            Button offline = HudBuilder.Button("Offline", "OFFLINE (SAME DEVICE)", mainButtons, standard, centre, new Vector2(0f, -110f), new Vector2(640f, 110f), Neutral);

            RectTransform friendPanel = Group("Friend Panel", panelRect);
            Button create = HudBuilder.Button("Create Room", "CREATE ROOM", friendPanel, standard, centre, new Vector2(0f, 150f), new Vector2(640f, 110f), Primary);
            TMP_InputField code = Input("Room Code", friendPanel, inputBackground, new Vector2(-120f, 20f), new Vector2(400f, 110f), "6-digit code");
            Button join = HudBuilder.Button("Join Room", "JOIN", friendPanel, standard, centre, new Vector2(210f, 20f), new Vector2(220f, 110f), Secondary);
            Button back = HudBuilder.Button("Back", "BACK", friendPanel, standard, centre, new Vector2(0f, -110f), new Vector2(640f, 110f), Neutral);
            friendPanel.gameObject.SetActive(false);

            TMP_Text status = HudBuilder.Label("Menu Status", panelRect, centre, new Vector2(0f, -250f), new Vector2(1400f, 130f), TextAlignmentOptions.Top, 40);
            status.text = string.Empty;
            status.textWrappingMode = TextWrappingModes.Normal;
            Button cancel = HudBuilder.Button("Cancel", "CANCEL", panelRect, standard, centre, new Vector2(0f, -400f), new Vector2(320f, 100f), Neutral);
            cancel.gameObject.SetActive(false);

            var menu = canvasObject.AddComponent<MainMenuController>();
            SceneWiring.Set(menu, "game", game);
            SceneWiring.Set(menu, "online", online);
            SceneWiring.Set(menu, "hud", hud);
            SceneWiring.Set(menu, "menuRoot", panel);
            SceneWiring.Set(menu, "hudRoot", hudRoot);
            SceneWiring.Set(menu, "mainButtons", mainButtons.gameObject);
            SceneWiring.Set(menu, "friendPanel", friendPanel.gameObject);
            SceneWiring.Set(menu, "nicknameInput", nickname);
            SceneWiring.Set(menu, "codeInput", code);
            SceneWiring.Set(menu, "quickMatchButton", quick);
            SceneWiring.Set(menu, "friendButton", friend);
            SceneWiring.Set(menu, "offlineButton", offline);
            SceneWiring.Set(menu, "createRoomButton", create);
            SceneWiring.Set(menu, "joinRoomButton", join);
            SceneWiring.Set(menu, "backButton", back);
            SceneWiring.Set(menu, "cancelButton", cancel);
            SceneWiring.Set(menu, "statusLabel", status);

            var pilot = canvasObject.AddComponent<DevAutoPilot>();
            SceneWiring.Set(pilot, "game", game);
            SceneWiring.Set(pilot, "menu", menu);
        }

        private static RectTransform Group(string name, RectTransform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static TMP_InputField Input(string name, RectTransform parent, Sprite background, Vector2 position, Vector2 size, string placeholder)
        {
            GameObject go = TMP_DefaultControls.CreateInputField(new TMP_DefaultControls.Resources { inputField = background });
            go.name = name;
            HudBuilder.Place(go, parent, new Vector2(0.5f, 0.5f), position, size);
            var input = go.GetComponent<TMP_InputField>();
            input.pointSize = 44;
            input.textComponent.alignment = TextAlignmentOptions.Center;
            var hint = (TMP_Text)input.placeholder;
            hint.text = placeholder;
            hint.alignment = TextAlignmentOptions.Center;
            return input;
        }
    }
}
