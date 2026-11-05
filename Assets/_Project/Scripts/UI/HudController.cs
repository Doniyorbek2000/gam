using System;
using BilliardGame.Gameplay;
using BilliardGame.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BilliardGame.UI
{
    /// <summary>Match HUD: player panels, status line, shot clock, power slider, spin pad, shoot, new-game and menu buttons.</summary>
    public sealed class HudController : MonoBehaviour
    {
        private static readonly Color ActiveColor = new Color(1f, 0.85f, 0.3f);
        private static readonly Color IdleColor = new Color(1f, 1f, 1f, 0.6f);

        [SerializeField] private GameController game;
        [SerializeField] private CueController cue;
        [SerializeField] private Slider powerSlider;
        [SerializeField] private SpinPad spinPad;
        [SerializeField] private Button spinIcon;
        [SerializeField] private FineTuneWheel fineTuneWheel;
        [SerializeField] private Button shootButton;
        [SerializeField] private Button newGameButton;
        [SerializeField] private TMP_Text player0Label;
        [SerializeField] private TMP_Text player1Label;
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private TMP_Text turnTimerLabel;
        [SerializeField] private Button menuButton;

        private int shownSeconds = int.MinValue;

        /// <summary>The player wants the main menu (leaving an online match forfeits it).</summary>
        public event Action MenuRequested;

        private void OnEnable()
        {
            game.StateChanged += Refresh;
            shootButton.onClick.AddListener(OnShoot);
            newGameButton.onClick.AddListener(OnNewGame);
            menuButton.onClick.AddListener(OnMenu);
            powerSlider.onValueChanged.AddListener(cue.SetPullback);
            spinIcon.onClick.AddListener(ToggleSpinPad);
            fineTuneWheel.Rotated += cue.RotateAim;
            cue.SetPullback(powerSlider.value);
            if (game.State != null) Refresh();
        }

        private void OnDisable()
        {
            game.StateChanged -= Refresh;
            shootButton.onClick.RemoveListener(OnShoot);
            newGameButton.onClick.RemoveListener(OnNewGame);
            menuButton.onClick.RemoveListener(OnMenu);
            powerSlider.onValueChanged.RemoveListener(cue.SetPullback);
            spinIcon.onClick.RemoveListener(ToggleSpinPad);
            fineTuneWheel.Rotated -= cue.RotateAim;
        }

        private void OnShoot()
        {
            game.Shoot(powerSlider.value, spinPad.Spin);
            spinPad.ResetSpin();
            spinPad.gameObject.SetActive(false);
        }

        private void Update()
        {
            int seconds = game.TurnSecondsLeft;
            if (seconds == shownSeconds) return;
            shownSeconds = seconds;
            turnTimerLabel.text = seconds < 0 ? string.Empty : seconds.ToString();
            turnTimerLabel.color = seconds <= 5 ? new Color(1f, 0.35f, 0.3f) : Color.white;
        }

        private void OnNewGame()
        {
            if (game.IsOnline) OnMenu();
            else game.NewGame();
        }

        private void OnMenu() => MenuRequested?.Invoke();

        private void ToggleSpinPad() => spinPad.gameObject.SetActive(!spinPad.gameObject.activeSelf);

        private void Refresh()
        {
            EightBallState state = game.State;
            if (state == null) return;
            shootButton.interactable = game.CanShoot;
            newGameButton.gameObject.SetActive(state.IsGameOver);
            newGameButton.GetComponentInChildren<TMP_Text>().text = game.IsOnline ? "MAIN MENU" : "NEW GAME";
            UpdatePlayer(player0Label, state, 0);
            UpdatePlayer(player1Label, state, 1);
            statusLabel.text = game.Banner ?? (game.IsShotInProgress ? string.Empty : Status(state, game.LastOutcome));
        }

        private void UpdatePlayer(TMP_Text label, EightBallState state, int player)
        {
            BallGroup group = state.GroupOf(player);
            string groupText = group == BallGroup.None ? "Open table" : $"{group} ({state.RemainingInGroup(group)} left)";
            if (state.IsOnEightBall(player)) groupText = "On the 8";
            string you = game.LocalPlayer == player ? " (you)" : string.Empty;
            label.text = $"<noparse>{game.PlayerName(player)}</noparse>{you}\n<size=70%>{groupText}</size>";
            label.color = !state.IsGameOver && state.CurrentPlayer == player ? ActiveColor : IdleColor;
        }

        private string Status(EightBallState state, ShotOutcome last)
        {
            bool mine = game.LocalPlayer == state.CurrentPlayer;
            string shooter = mine ? "You" : game.PlayerName(state.CurrentPlayer);
            if (state.IsGameOver)
            {
                if (game.LocalPlayer < 0) return $"{game.PlayerName(state.Winner)} wins!";
                return game.LocalPlayer == state.Winner ? "You win!" : "You lose";
            }

            if (last != null && last.IsFoul)
            {
                string where = state.BallInHandKitchenOnly ? " behind the head string" : string.Empty;
                if (!state.BallInHand) return $"Foul: {FoulText(last.Foul)}. {shooter} to break";
                return $"Foul: {FoulText(last.Foul)}. {shooter} {(mine ? "have" : "has")} ball in hand{where}";
            }

            if (state.IsBreakShot) return $"{shooter} to break";
            if (last != null && last.GroupsAssigned) return $"{shooter} takes {state.GroupOf(state.CurrentPlayer)}";
            if (last != null && last.EightBallRespotted) return $"8-ball respotted. {shooter} to shoot";
            return mine && game.IsOnline ? "Your turn" : $"{shooter} to shoot";
        }

        private static string FoulText(FoulType foul)
        {
            switch (foul)
            {
                case FoulType.Scratch: return "cue ball pocketed";
                case FoulType.NoBallHit: return "no ball hit";
                case FoulType.WrongBallFirst: return "wrong ball hit first";
                case FoulType.NoRailAfterContact: return "no cushion after contact";
                case FoulType.IllegalBreak: return "illegal break";
                case FoulType.Timeout: return "time ran out";
                default: return "foul";
            }
        }
    }
}
