using System;
using System.Collections.Generic;
using BilliardGame.Content;
using BilliardGame.Gameplay;
using BilliardGame.Meta;
using BilliardGame.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BilliardGame.UI
{
    /// <summary>
    /// Match HUD (docs/UI_UX.md §10): two player panels with shot-timer rings and group pips, status banner, power
    /// slider (left, or right in left-handed mode), fine-tune wheel, spin, shoot (optional confirm tap), pause menu and
    /// the sticker picker. Shows the result screen and records the match when it ends. Nothing here touches the
    /// physics inputs except the same power/spin/aim the player sets (no pay-to-win).
    /// </summary>
    public sealed class HudController : MonoBehaviour
    {
        private const float ConfirmWindow = 2.5f;

        [SerializeField] private GameController game;
        [SerializeField] private CueController cue;
        [SerializeField] private GameServices services;
        [SerializeField] private UITheme theme;

        [SerializeField] private PlayerPanelView[] panels = new PlayerPanelView[2];
        [SerializeField] private CanvasGroup banner;
        [SerializeField] private TMP_Text statusLabel;

        [SerializeField] private RectTransform powerColumn;
        [SerializeField] private Slider powerSlider;
        [SerializeField] private TMP_Text powerValue;
        [SerializeField] private RectTransform rightCluster;
        [SerializeField] private FineTuneWheel fineTuneWheel;
        [SerializeField] private SpinPad spinPad;
        [SerializeField] private Button spinIcon;
        [SerializeField] private Button shootButton;
        [SerializeField] private TMP_Text shootLabel;

        [SerializeField] private Button pauseButton;
        [SerializeField] private ScreenView pauseView;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button leaveButton;
        [SerializeField] private Button emoteButton;
        [SerializeField] private ScreenView stickerSheet;
        [SerializeField] private Button stickerTemplate;
        [SerializeField] private ResultScreen result;

        private readonly MatchRecorder recorder = new MatchRecorder();
        private readonly List<Button> stickerButtons = new List<Button>();
        private int shownSeconds = int.MinValue;
        private int turnSeconds;
        private bool mirrored;
        private float confirmUntil = -1f;
        private string venueId = string.Empty;
        private int venuePrize;

        /// <summary>The player wants the lobby (leaving an online match forfeits it).</summary>
        public event Action MenuRequested;

        /// <summary>Result screen ▸ Play again.</summary>
        public event Action AgainRequested;

        public void SetMatchContext(VenueDefinition venue)
        {
            venueId = venue != null ? venue.Id : string.Empty;
            venuePrize = venue != null ? venue.Prize : 0;
        }

        private void OnEnable()
        {
            game.StateChanged += Refresh;
            GameSettings.Changed += ApplySettings;
            Loc.Changed += Refresh;
            shootButton.onClick.AddListener(OnShoot);
            pauseButton.onClick.AddListener(() => ScreenRouter.Instance.Open(pauseView));
            resumeButton.onClick.AddListener(() => ScreenRouter.Instance.Close(pauseView));
            leaveButton.onClick.AddListener(ConfirmLeave);
            emoteButton.onClick.AddListener(OpenStickers);
            powerSlider.onValueChanged.AddListener(OnPower);
            spinIcon.onClick.AddListener(ToggleSpinPad);
            fineTuneWheel.Rotated += cue.RotateAim;
            result.AgainPressed += OnAgain;
            result.LobbyPressed += OnMenu;
            cue.SetPullback(powerSlider.value);
            ApplySettings();
            if (game.State != null) Refresh();
        }

        private void OnDisable()
        {
            game.StateChanged -= Refresh;
            GameSettings.Changed -= ApplySettings;
            Loc.Changed -= Refresh;
            foreach (Button b in new[] { shootButton, pauseButton, resumeButton, leaveButton, emoteButton, spinIcon }) b.onClick.RemoveAllListeners();
            powerSlider.onValueChanged.RemoveListener(OnPower);
            fineTuneWheel.Rotated -= cue.RotateAim;
            result.AgainPressed -= OnAgain;
            result.LobbyPressed -= OnMenu;
        }

        private void ApplySettings()
        {
            GameSettings.ApplyInput(cue, fineTuneWheel);
            bool left = GameSettings.Data.leftHanded;
            if (left == mirrored) return;
            mirrored = left;
            Mirror(powerColumn);
            Mirror(rightCluster);
        }

        /// <summary>Left-handed mode: the power column and the aim/spin/shoot cluster swap sides (children mirror too).</summary>
        private static void Mirror(RectTransform rect)
        {
            Flip(rect);
            for (int i = 0; i < rect.childCount; i++) Flip((RectTransform)rect.GetChild(i));
        }

        private static void Flip(RectTransform rect)
        {
            Vector2 min = rect.anchorMin, max = rect.anchorMax;
            rect.anchorMin = new Vector2(1f - max.x, min.y);
            rect.anchorMax = new Vector2(1f - min.x, max.y);
            rect.pivot = new Vector2(1f - rect.pivot.x, rect.pivot.y);
            rect.anchoredPosition = new Vector2(-rect.anchoredPosition.x, rect.anchoredPosition.y);
        }

        private void OnPower(float value)
        {
            cue.SetPullback(value);
            powerValue.text = Mathf.RoundToInt(value * 100f).ToString(Loc.Culture);
        }

        private void OnShoot()
        {
            if (!game.CanShoot) return;
            if (GameSettings.Data.confirmShot && Time.unscaledTime > confirmUntil)
            {
                confirmUntil = Time.unscaledTime + ConfirmWindow;
                shootLabel.text = Loc.Get("hud.shoot_confirm").ToUpper(Loc.Culture);
                return;
            }

            confirmUntil = -1f;
            game.Shoot(powerSlider.value, spinPad.Spin);
            spinPad.ResetSpin();
            spinPad.gameObject.SetActive(false);
        }

        private void ConfirmLeave()
        {
            ScreenRouter.Instance.Close(pauseView);
            string body = Loc.Get(game.IsOnline ? "hud.leave_body" : "hud.leave_offline");
            Dialogs.Instance.Confirm("hud.leave", body, "hud.leave", OnMenu, destructive: game.IsOnline);
        }

        private void OnMenu()
        {
            ScreenRouter.Instance.CloseAll(true);
            MenuRequested?.Invoke();
        }

        private void OnAgain() => AgainRequested?.Invoke();

        private void ToggleSpinPad() => spinPad.gameObject.SetActive(!spinPad.gameObject.activeSelf);

        // ---- Stickers ------------------------------------------------------------------------------------------------

        private void OpenStickers()
        {
            stickerTemplate.gameObject.SetActive(false);
            var owned = new List<SkinDefinition>();
            foreach (SkinDefinition skin in services.Catalog.Skins)
            {
                if (skin.Type == ItemType.Sticker && services.Economy.Owns(skin.Id)) owned.Add(skin);
            }

            while (stickerButtons.Count < owned.Count) stickerButtons.Add(Instantiate(stickerTemplate, stickerTemplate.transform.parent));
            for (int i = 0; i < stickerButtons.Count; i++)
            {
                bool used = i < owned.Count;
                stickerButtons[i].gameObject.SetActive(used);
                if (!used) continue;
                SkinDefinition sticker = owned[i];
                stickerButtons[i].transform.GetChild(0).GetComponent<Image>().sprite = sticker.Icon;
                stickerButtons[i].onClick.RemoveAllListeners();
                stickerButtons[i].onClick.AddListener(() => SendSticker(sticker));
            }

            ScreenRouter.Instance.Open(stickerSheet);
        }

        // shortcut: the sticker shows on this device only; M7 (voice/chat) sends it to the opponent over the match socket.
        private void SendSticker(SkinDefinition sticker)
        {
            ScreenRouter.Instance.Close(stickerSheet);
            int seat = game.LocalPlayer >= 0 ? game.LocalPlayer : game.State != null ? game.State.CurrentPlayer : 0;
            panels[seat].ShowSticker(sticker.Icon);
        }

        // ---- Refresh -------------------------------------------------------------------------------------------------

        private void Update()
        {
            if (confirmUntil > 0f && Time.unscaledTime > confirmUntil)
            {
                confirmUntil = -1f;
                shootLabel.text = Loc.Get("hud.shoot").ToUpper(Loc.Culture);
            }

            int seconds = game.TurnSecondsLeft;
            if (seconds == shownSeconds) return;
            if (seconds > shownSeconds) turnSeconds = Mathf.Max(seconds, 1);   // a new turn restarts the clock
            shownSeconds = seconds;
            EightBallState state = game.State;
            for (int i = 0; i < panels.Length; i++)
            {
                bool running = seconds >= 0 && state != null && state.CurrentPlayer == i;
                Color color = seconds <= 5 ? theme.danger : theme.accent;
                panels[i].SetTimer(running ? (float)seconds / turnSeconds : -1f, color);
            }
        }

        private void Refresh()
        {
            EightBallState state = game.State;
            if (state == null) return;
            shootButton.interactable = game.CanShoot;
            if (confirmUntil < 0f) shootLabel.text = Loc.Get("hud.shoot").ToUpper(Loc.Culture);
            for (int i = 0; i < panels.Length; i++) UpdatePanel(i, state);
            SkinDefinition frame = services.Equipped(ItemType.Frame);
            if (frame != null) panels[game.LocalPlayer >= 0 ? game.LocalPlayer : 0].SetFrame(frame.Icon);

            string status = game.Banner ?? (game.IsShotInProgress ? string.Empty : Status(state, game.LastOutcome));
            statusLabel.text = status;

            MatchSummary summary = recorder.Observe(game, services, venueId, venuePrize);
            if (summary != null) result.Present(summary);
            // The result screen already names the winner; a banner behind its shade would repeat it.
            banner.alpha = string.IsNullOrEmpty(status) || result.IsOpen ? 0f : 1f;
        }

        private void UpdatePanel(int player, EightBallState state)
        {
            BallGroup group = state.GroupOf(player);
            string groupText = group == BallGroup.None
                ? Loc.Get("hud.open_table")
                : Loc.Format(group == BallGroup.Solids ? "hud.solids" : "hud.stripes", state.RemainingInGroup(group));
            if (state.IsOnEightBall(player)) groupText = Loc.Get("hud.on_eight");
            int remaining = group == BallGroup.None ? -1 : state.RemainingInGroup(group);
            Color pip = group == BallGroup.Solids ? theme.accent : theme.secondary;
            string name = game.PlayerName(player);
            if (game.LocalPlayer == player) name += "  <size=70%><color=#FFC24A>" + Loc.Get("hud.you") + "</color></size>";
            string rank = game.LocalPlayer == player || (!game.IsOnline && player == 0) ? LobbyController.RankText(services.Profile.State.rankPoints) : string.Empty;
            panels[player].Set(name, rank, groupText, remaining, pip, !state.IsGameOver && state.CurrentPlayer == player);
        }

        private string Status(EightBallState state, ShotOutcome last)
        {
            bool mine = game.LocalPlayer == state.CurrentPlayer;
            string shooter = mine ? Loc.Get("hud.you") : game.PlayerName(state.CurrentPlayer);
            if (state.IsGameOver) return Loc.Format("result.winner", game.PlayerName(state.Winner));

            if (last != null && last.IsFoul)
            {
                string next = !state.BallInHand
                    ? Loc.Format("hud.rebreak", shooter)
                    : Loc.Format(state.BallInHandKitchenOnly ? "hud.ball_in_hand_kitchen" : "hud.ball_in_hand", shooter);
                return Loc.Format("hud.foul", FoulText(last.Foul), next);
            }

            if (state.IsBreakShot) return Loc.Format("hud.break", shooter);
            if (last != null && last.GroupsAssigned)
            {
                BallGroup group = state.GroupOf(state.CurrentPlayer);
                return Loc.Format("hud.takes", shooter, Loc.Format(group == BallGroup.Solids ? "hud.solids" : "hud.stripes", state.RemainingInGroup(group)));
            }

            if (last != null && last.EightBallRespotted) return Loc.Format("hud.respot", shooter);
            return mine && game.IsOnline ? Loc.Get("hud.your_turn") : Loc.Format("hud.turn", shooter);
        }

        private static string FoulText(FoulType foul)
        {
            switch (foul)
            {
                case FoulType.Scratch: return Loc.Get("foul.scratch");
                case FoulType.NoBallHit: return Loc.Get("foul.no_hit");
                case FoulType.WrongBallFirst: return Loc.Get("foul.wrong_first");
                case FoulType.NoRailAfterContact: return Loc.Get("foul.no_rail");
                case FoulType.IllegalBreak: return Loc.Get("foul.illegal_break");
                case FoulType.Timeout: return Loc.Get("foul.timeout");
                default: return Loc.Get("foul.other");
            }
        }
    }
}
