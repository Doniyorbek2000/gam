using System;
using BilliardGame.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BilliardGame.UI
{
    /// <summary>
    /// Settings (docs/UI_UX.md §11): graphics, sound, controls, language (switches instantly), account (ID, Google link,
    /// log out, delete — Play data-deletion rule) and other (notifications, policies, help, credits, version).
    /// Every change is saved and applied at once through <see cref="GameSettings"/>.
    /// </summary>
    public sealed class SettingsScreen : ScreenView
    {
        private static readonly GraphicsQuality[] QualityOrder = { GraphicsQuality.Low, GraphicsQuality.Medium, GraphicsQuality.High, GraphicsQuality.Auto };

        [SerializeField] private GameServices services;
        [SerializeField] private ScreenView credits;
        [SerializeField] private TabBar tabs;
        [SerializeField] private GameObject[] pages = new GameObject[0];

        [Header("Graphics")]
        [SerializeField] private TabBar quality;
        [SerializeField] private TabBar fps;
        [SerializeField] private ToggleSwitch shadows;
        [SerializeField] private ToggleSwitch postProcessing;
        [SerializeField] private ToggleSwitch batterySaver;

        [Header("Sound")]
        [SerializeField] private Slider master;
        [SerializeField] private Slider music;
        [SerializeField] private Slider effects;
        [SerializeField] private Slider voice;
        [SerializeField] private ToggleSwitch vibration;

        [Header("Controls")]
        [SerializeField] private Slider aimSensitivity;
        [SerializeField] private Slider fineSensitivity;
        [SerializeField] private ToggleSwitch leftHanded;
        [SerializeField] private ToggleSwitch aimGuide;
        [SerializeField] private ToggleSwitch confirmShot;

        [Header("Language: uz, ru, en")]
        [SerializeField] private Button[] languages = new Button[0];

        [Header("Account")]
        [SerializeField] private TMP_Text accountId;
        [SerializeField] private Button copyId;
        [SerializeField] private Button linkGoogle;
        [SerializeField] private Button logout;
        [SerializeField] private Button deleteAccount;

        [Header("Other")]
        [SerializeField] private ToggleSwitch notifications;
        [SerializeField] private Button privacy;
        [SerializeField] private Button terms;
        [SerializeField] private Button help;
        [SerializeField] private Button creditsButton;
        [SerializeField] private TMP_Text version;

        private bool refreshing;

        protected override void Awake()
        {
            base.Awake();
            tabs.Changed += ShowPage;
            quality.Changed += i => Change(s => s.quality = QualityOrder[i]);
            fps.Changed += i => Change(s => s.fps = GameSettings.FpsOptions[i]);
            Toggle(shadows, (s, on) => s.shadows = on);
            Toggle(postProcessing, (s, on) => s.postProcessing = on);
            Toggle(batterySaver, (s, on) => s.batterySaver = on);
            Toggle(vibration, (s, on) => s.vibration = on);
            Toggle(leftHanded, (s, on) => s.leftHanded = on);
            Toggle(aimGuide, (s, on) => s.aimGuide = on);
            Toggle(confirmShot, (s, on) => s.confirmShot = on);
            Toggle(notifications, (s, on) => s.notifications = on);
            Volume(master, (s, v) => s.master = v);
            Volume(music, (s, v) => s.music = v);
            Volume(effects, (s, v) => s.effects = v);
            Volume(voice, (s, v) => s.voice = v);
            Volume(aimSensitivity, (s, v) => s.aimSensitivity = v);
            Volume(fineSensitivity, (s, v) => s.fineSensitivity = v);
            for (int i = 0; i < languages.Length; i++)
            {
                string code = Loc.Languages[i];
                languages[i].onClick.AddListener(() => SetLanguage(code));
            }

            copyId.onClick.AddListener(CopyId);
            linkGoogle.onClick.AddListener(Soon);
            logout.onClick.AddListener(Logout);
            deleteAccount.onClick.AddListener(DeleteAccount);
            privacy.onClick.AddListener(Soon);
            terms.onClick.AddListener(Soon);
            help.onClick.AddListener(Soon);
            creditsButton.onClick.AddListener(() => ScreenRouter.Instance.Open(credits));
        }

        protected override void OnShown()
        {
            Loc.Changed += Refresh;
            if (tabs.Selected < 0) tabs.Select(0, false);
            ShowPage(tabs.Selected);
            Refresh();
        }

        protected override void OnHidden() => Loc.Changed -= Refresh;

        private void ShowPage(int index)
        {
            for (int i = 0; i < pages.Length; i++) pages[i].SetActive(i == index);
        }

        private void Refresh()
        {
            refreshing = true;
            GameSettingsData s = GameSettings.Data;
            quality.Select(Array.IndexOf(QualityOrder, s.quality), false);
            int fpsIndex = Array.IndexOf(GameSettings.FpsOptions, s.fps);
            fps.Select(fpsIndex < 0 ? 1 : fpsIndex, false);
            for (int i = 0; i < GameSettings.FpsOptions.Length; i++) fps.Tab(i).interactable = GameSettings.FpsOptions[i] <= GameSettings.MaxFps;
            shadows.Set(s.shadows);
            postProcessing.Set(s.postProcessing);
            batterySaver.Set(s.batterySaver);
            master.SetValueWithoutNotify(s.master);
            music.SetValueWithoutNotify(s.music);
            effects.SetValueWithoutNotify(s.effects);
            voice.SetValueWithoutNotify(s.voice);
            vibration.Set(s.vibration);
            aimSensitivity.SetValueWithoutNotify(s.aimSensitivity);
            fineSensitivity.SetValueWithoutNotify(s.fineSensitivity);
            leftHanded.Set(s.leftHanded);
            aimGuide.Set(s.aimGuide);
            confirmShot.Set(s.confirmShot);
            notifications.Set(s.notifications);
            for (int i = 0; i < languages.Length; i++) languages[i].GetComponent<UIButtonFx>().SetSelected(Loc.Languages[i] == Loc.Language);
            accountId.text = services.Profile.State.playerId;
            version.text = Loc.Format("settings.version", Application.version);
            refreshing = false;
        }

        private void Change(Action<GameSettingsData> edit)
        {
            if (refreshing) return;
            edit(GameSettings.Data);
            GameSettings.Save();
        }

        private void Toggle(ToggleSwitch toggle, Action<GameSettingsData, bool> edit) => toggle.Changed += on => Change(s => edit(s, on));

        private void Volume(Slider slider, Action<GameSettingsData, float> edit) => slider.onValueChanged.AddListener(v => Change(s => edit(s, v)));

        private void SetLanguage(string code)
        {
            Loc.SetLanguage(code);
            UIFeedback.Confirm();
        }

        private void CopyId()
        {
            GUIUtility.systemCopyBuffer = services.Profile.State.playerId;
            Dialogs.Instance.Toast(Loc.Get("common.copied"));
        }

        // Guest accounts only (Google link arrives with M2): logging out restarts the session on this device.
        private void Logout() =>
            Dialogs.Instance.Confirm("settings.logout", Loc.Get("settings.logout_body"), "settings.logout", Restart);

        private void DeleteAccount() =>
            Dialogs.Instance.Confirm("settings.delete", Loc.Get("settings.delete_body"), "settings.delete", () =>
            {
                services.DeleteLocalAccount();
                GameSettings.Reset();
                Restart();
            }, destructive: true);

        private static void Restart() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

        private static void Soon() => Dialogs.Instance.Toast(Loc.Get("common.soon_toast"));
    }
}
