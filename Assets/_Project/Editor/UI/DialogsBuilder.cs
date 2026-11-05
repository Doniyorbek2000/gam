using BilliardGame.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BilliardGame.Editor.UI
{
    /// <summary>Shared dialog layer (docs/UI_UX.md §11): confirm (with gem cost), text input, toast, loading, no connection.</summary>
    internal static class DialogsBuilder
    {
        private static UITheme T => UIKit.Theme;

        public static Dialogs Build(Transform host)
        {
            RectTransform root = UIKit.Node("Dialogs", host).Fill();
            var dialogs = root.gameObject.AddComponent<Dialogs>();

            // Confirm.
            var confirm = UIKit.Modal<ScreenView>(root, "Confirm", new Vector2(900f, 560f), out RectTransform card);
            Image icon = UIKit.Icon(card, "help", 72f, T.accent);
            icon.rectTransform.At(new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(72f, 72f), new Vector2(0.5f, 1f));
            TMP_Text title = UIKit.Text(card, "Title", null, TextStyle.H1, null, TextAlignmentOptions.Center);
            title.rectTransform.Span(new Vector2(0f, 1f), Vector2.one, new Vector2(40f, -186f), new Vector2(-40f, -120f));
            TMP_Text body = UIKit.Wrap(UIKit.Text(card, "Body", null, TextStyle.Body, T.textSecondary, TextAlignmentOptions.Top));
            body.rectTransform.Span(new Vector2(0f, 0f), Vector2.one, new Vector2(48f, 220f), new Vector2(-48f, -196f));
            RectTransform cost = UIKit.Row(card, "Cost", 12f, TextAnchor.MiddleCenter, false);
            cost.Span(Vector2.zero, new Vector2(1f, 0f), new Vector2(40f, 150f), new Vector2(-40f, 206f));
            Image costIcon = UIKit.Icon(cost, "gem", 44f, Color.white);
            UIKit.Size(costIcon, 44f, 44f);
            TMP_Text costText = UIKit.Text(cost, "Value", null, TextStyle.Number, T.gem, TextAlignmentOptions.MidlineLeft);
            costText.fontSize = costText.fontSizeMax = 38f;
            UIKit.Size(costText, 220f, 56f);
            ButtonParts cancel = UIKit.Button(card, "Cancel", ButtonKind.Ghost, "common.cancel", null, new Vector2(380f, 96f));
            cancel.Rect.At(new Vector2(0.5f, 0f), new Vector2(-200f, 36f), new Vector2(380f, 96f), new Vector2(0.5f, 0f));
            ButtonParts ok = UIKit.Button(card, "Confirm", ButtonKind.Primary, "common.confirm", null, new Vector2(380f, 96f));
            ok.Rect.At(new Vector2(0.5f, 0f), new Vector2(200f, 36f), new Vector2(380f, 96f), new Vector2(0.5f, 0f));
            confirm.BindBack(cancel.Button);

            // Text input (nickname).
            var input = UIKit.Modal<ScreenView>(root, "Input", new Vector2(900f, 520f), out RectTransform inputCard);
            TMP_Text inputTitle = UIKit.Text(inputCard, "Title", null, TextStyle.H1, null, TextAlignmentOptions.Center);
            inputTitle.rectTransform.Span(new Vector2(0f, 1f), Vector2.one, new Vector2(40f, -116f), new Vector2(-40f, -40f));
            TMP_InputField field = UIKit.Field(inputCard, "Field", new Vector2(780f, 100f), null);
            ((RectTransform)field.transform).At(new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(780f, 100f), new Vector2(0.5f, 1f));
            field.characterLimit = 16;
            TMP_Text inputError = UIKit.Text(inputCard, "Error", null, TextStyle.Caption, T.danger, TextAlignmentOptions.Center);
            inputError.rectTransform.Span(new Vector2(0f, 1f), Vector2.one, new Vector2(40f, -300f), new Vector2(-40f, -256f));
            ButtonParts inputCancel = UIKit.Button(inputCard, "Cancel", ButtonKind.Ghost, "common.cancel", null, new Vector2(380f, 96f));
            inputCancel.Rect.At(new Vector2(0.5f, 0f), new Vector2(-200f, 36f), new Vector2(380f, 96f), new Vector2(0.5f, 0f));
            ButtonParts inputOk = UIKit.Button(inputCard, "Ok", ButtonKind.Primary, "common.save", "check", new Vector2(380f, 96f));
            inputOk.Rect.At(new Vector2(0.5f, 0f), new Vector2(200f, 36f), new Vector2(380f, 96f), new Vector2(0.5f, 0f));
            input.BindBack(inputCancel.Button);

            // Loading.
            var loading = UIKit.Modal<ScreenView>(root, "Loading", new Vector2(640f, 340f), out RectTransform loadingCard);
            UIKit.Spinner(loadingCard, 110f).At(new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(110f, 110f), new Vector2(0.5f, 1f));
            TMP_Text loadingText = UIKit.Text(loadingCard, "Text", "common.loading", TextStyle.H2, null, TextAlignmentOptions.Center);
            loadingText.rectTransform.Span(Vector2.zero, new Vector2(1f, 0f), new Vector2(30f, 40f), new Vector2(-30f, 120f));

            // No connection.
            var offline = UIKit.Modal<ScreenView>(root, "Offline", new Vector2(900f, 560f), out RectTransform offlineCard);
            UIKit.Icon(offlineCard, "offline", 96f, T.danger).rectTransform.At(new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(96f, 96f), new Vector2(0.5f, 1f));
            TMP_Text offlineTitle = UIKit.Text(offlineCard, "Title", "offline.title", TextStyle.H1, null, TextAlignmentOptions.Center);
            offlineTitle.rectTransform.Span(new Vector2(0f, 1f), Vector2.one, new Vector2(40f, -210f), new Vector2(-40f, -144f));
            TMP_Text offlineBody = UIKit.Wrap(UIKit.Text(offlineCard, "Body", "offline.body", TextStyle.Body, T.textSecondary, TextAlignmentOptions.Top));
            offlineBody.rectTransform.Span(Vector2.zero, Vector2.one, new Vector2(48f, 150f), new Vector2(-48f, -220f));
            ButtonParts offlineClose = UIKit.Button(offlineCard, "Close", ButtonKind.Ghost, "common.close", null, new Vector2(380f, 96f));
            offlineClose.Rect.At(new Vector2(0.5f, 0f), new Vector2(-200f, 36f), new Vector2(380f, 96f), new Vector2(0.5f, 0f));
            ButtonParts retry = UIKit.Button(offlineCard, "Retry", ButtonKind.Primary, "common.retry", "refresh", new Vector2(380f, 96f));
            retry.Rect.At(new Vector2(0.5f, 0f), new Vector2(200f, 36f), new Vector2(380f, 96f), new Vector2(0.5f, 0f));
            offline.BindBack(offlineClose.Button);

            // Toast: pill near the top centre, above everything.
            Image toast = UIKit.Image(root, "Toast", UIAssets.Pill, new Color(0.04f, 0.06f, 0.12f, 0.94f));
            toast.rectTransform.At(new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(900f, 84f), new Vector2(0.5f, 1f));
            UIKit.Image(toast.transform, "Stroke", UIAssets.Stroke(32), new Color(T.accent.r, T.accent.g, T.accent.b, 0.5f)).rectTransform.Fill();
            TMP_Text toastText = UIKit.Text(toast.transform, "Text", null, TextStyle.Body, null, TextAlignmentOptions.Center);
            toastText.rectTransform.Fill(36f, 0f, 36f, 0f);
            toast.gameObject.AddComponent<CanvasGroup>().blocksRaycasts = false;

            UIKit.Wire(dialogs,
                "confirmView", confirm, "confirmIcon", icon, "confirmTitle", title, "confirmBody", body, "costRow", cost.gameObject, "costIcon", costIcon,
                "costText", costText, "confirmButton", ok.Button, "confirmLabel", ok.Label, "confirmFill", ok.Fill, "cancelButton", cancel.Button, "cancelLabel", cancel.Label,
                "inputView", input, "inputTitle", inputTitle, "inputField", field, "inputError", inputError, "inputOk", inputOk.Button, "inputCancel", inputCancel.Button,
                "toast", toast.rectTransform, "toastText", toastText, "loadingView", loading, "loadingText", loadingText,
                "offlineView", offline, "offlineRetry", retry.Button, "offlineClose", offlineClose.Button,
                "theme", T, "warningSprite", UIAssets.Icon("error"), "errorSprite", UIAssets.Icon("error"), "questionSprite", UIAssets.Icon("help"));
            return dialogs;
        }
    }
}
