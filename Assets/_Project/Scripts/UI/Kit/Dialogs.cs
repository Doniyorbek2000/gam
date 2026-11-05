using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BilliardGame.UI
{
    /// <summary>
    /// Shared overlays: confirm / error dialog (gem spending always confirms first), text input dialog, toast queue,
    /// loading overlay and the "no connection" screen. One instance on the lobby canvas, reachable as <see cref="Instance"/>.
    /// </summary>
    public sealed class Dialogs : MonoBehaviour
    {
        private const float ToastSeconds = 2.2f;

        [Header("Confirm")]
        [SerializeField] private ScreenView confirmView;
        [SerializeField] private Image confirmIcon;
        [SerializeField] private TMP_Text confirmTitle;
        [SerializeField] private TMP_Text confirmBody;
        [SerializeField] private GameObject costRow;
        [SerializeField] private Image costIcon;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private TMP_Text confirmLabel;
        [SerializeField] private Image confirmFill;
        [SerializeField] private Button cancelButton;
        [SerializeField] private TMP_Text cancelLabel;

        [Header("Input")]
        [SerializeField] private ScreenView inputView;
        [SerializeField] private TMP_Text inputTitle;
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private TMP_Text inputError;
        [SerializeField] private Button inputOk;
        [SerializeField] private Button inputCancel;

        [Header("Toast / loading / offline")]
        [SerializeField] private RectTransform toast;
        [SerializeField] private TMP_Text toastText;
        [SerializeField] private ScreenView loadingView;
        [SerializeField] private TMP_Text loadingText;
        [SerializeField] private ScreenView offlineView;
        [SerializeField] private Button offlineRetry;
        [SerializeField] private Button offlineClose;

        [Header("Look")]
        [SerializeField] private UITheme theme;
        [SerializeField] private Sprite warningSprite;
        [SerializeField] private Sprite errorSprite;
        [SerializeField] private Sprite questionSprite;

        private readonly Queue<string> toasts = new Queue<string>();
        private Action onConfirm;
        private Action onCancel;
        private Func<string, string> inputSubmit;
        private Action onRetry;
        private float toastUntil;
        private bool toastShowing;
        private Vector2 toastHome;

        public static Dialogs Instance { get; private set; }

        public bool IsBusy => confirmView.IsOpen || inputView.IsOpen || loadingView.IsOpen;

        private void Awake()
        {
            Instance = this;
            toastHome = toast.anchoredPosition;
            toast.gameObject.SetActive(false);
            confirmButton.onClick.AddListener(ConfirmPressed);
            cancelButton.onClick.AddListener(CancelPressed);
            inputOk.onClick.AddListener(SubmitInput);
            inputCancel.onClick.AddListener(() => inputView.Hide());
            offlineRetry.onClick.AddListener(Retry);
            offlineClose.onClick.AddListener(() => offlineView.Hide());
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Two-button dialog. <paramref name="cost"/> &gt; 0 shows the price row (gem / coin icon).</summary>
        public void Confirm(string titleKey, string body, string confirmKey, Action confirmed, Sprite currency = null, long cost = 0, bool destructive = false, Action cancelled = null)
        {
            confirmIcon.sprite = destructive ? warningSprite : questionSprite;
            confirmIcon.color = destructive ? theme.danger : theme.accent;
            OpenConfirm(titleKey, body, confirmKey, "common.cancel", confirmed, cancelled, true);
            costRow.SetActive(cost > 0);
            if (cost > 0)
            {
                costIcon.sprite = currency;
                costText.text = Loc.Number(cost);
            }

            confirmFill.color = destructive ? theme.danger : theme.accent;
            confirmLabel.color = destructive ? theme.textPrimary : theme.textOnAccent;
        }

        /// <summary>One-button error dialog with a readable message (never a stack trace).</summary>
        public void Error(string titleKey, string body, Action closed = null)
        {
            confirmIcon.sprite = errorSprite;
            confirmIcon.color = theme.danger;
            OpenConfirm(titleKey, body, "common.ok", null, closed, null, false);
            costRow.SetActive(false);
            confirmFill.color = theme.surfaceRaised;
            confirmLabel.color = theme.textPrimary;
        }

        /// <summary>Text prompt; <paramref name="submit"/> returns null to accept or a localization key to show as the error.</summary>
        public void Input(string titleKey, string value, int maxLength, Func<string, string> submit)
        {
            inputTitle.text = Loc.Get(titleKey);
            inputField.characterLimit = maxLength;
            inputField.text = value;
            inputError.text = string.Empty;
            inputSubmit = submit;
            ScreenRouter.Instance.Open(inputView);
        }

        public void Toast(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            toasts.Enqueue(text);
            if (!toastShowing) NextToast();
        }

        public void ShowLoading(string textKey)
        {
            loadingText.text = Loc.Get(textKey);
            if (!loadingView.IsOpen) loadingView.Show();
        }

        public void HideLoading() => loadingView.Hide();

        public void ShowOffline(Action retry)
        {
            onRetry = retry;
            if (!offlineView.IsOpen) offlineView.Show();
        }

        private void OpenConfirm(string titleKey, string body, string confirmKey, string cancelKey, Action confirmed, Action cancelled, bool twoButtons)
        {
            confirmTitle.text = Loc.Get(titleKey);
            confirmBody.text = body;
            confirmLabel.text = Loc.Get(confirmKey).ToUpper(Loc.Culture);
            cancelLabel.text = Loc.Get(cancelKey).ToUpper(Loc.Culture);
            cancelButton.gameObject.SetActive(twoButtons);
            onConfirm = confirmed;
            onCancel = cancelled;
            ScreenRouter.Instance.Open(confirmView);
        }

        private void ConfirmPressed()
        {
            Action action = onConfirm ?? onCancel;
            onConfirm = onCancel = null;
            ScreenRouter.Instance.Close(confirmView);
            action?.Invoke();
        }

        private void CancelPressed()
        {
            Action action = onCancel;
            onConfirm = onCancel = null;
            ScreenRouter.Instance.Close(confirmView);
            action?.Invoke();
        }

        private void SubmitInput()
        {
            string error = inputSubmit?.Invoke(inputField.text);
            if (error != null)
            {
                inputError.text = Loc.Get(error);
                return;
            }

            ScreenRouter.Instance.Close(inputView);
        }

        private void Retry()
        {
            offlineView.Hide();
            onRetry?.Invoke();
        }

        private void NextToast()
        {
            if (toasts.Count == 0)
            {
                toastShowing = false;
                return;
            }

            toastShowing = true;
            toastText.text = toasts.Dequeue();
            toast.gameObject.SetActive(true);
            toast.SetAsLastSibling();
            toast.anchoredPosition = toastHome + new Vector2(0f, 160f);
            Tween.Move(toast, toastHome, 0.3f, Ease.OutBack);
            toastUntil = Time.unscaledTime + ToastSeconds;
        }

        private void Update()
        {
            if (!toastShowing || Time.unscaledTime < toastUntil) return;
            toastUntil = float.MaxValue;
            Tween.Move(toast, toastHome + new Vector2(0f, 160f), 0.22f, Ease.InOutCubic, () =>
            {
                toast.gameObject.SetActive(false);
                NextToast();
            });
        }
    }
}
