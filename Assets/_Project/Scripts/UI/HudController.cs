using BilliardGame.Gameplay;
using BilliardGame.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BilliardGame.UI
{
    /// <summary>Hot-seat HUD: player panels, status line, power slider, spin pad, shoot and new-game buttons.</summary>
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

        private void OnEnable()
        {
            game.StateChanged += Refresh;
            shootButton.onClick.AddListener(OnShoot);
            newGameButton.onClick.AddListener(game.NewGame);
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
            newGameButton.onClick.RemoveListener(game.NewGame);
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

        private void ToggleSpinPad() => spinPad.gameObject.SetActive(!spinPad.gameObject.activeSelf);

        private void Refresh()
        {
            EightBallState state = game.State;
            shootButton.interactable = game.CanShoot;
            newGameButton.gameObject.SetActive(state.IsGameOver);
            UpdatePlayer(player0Label, state, 0);
            UpdatePlayer(player1Label, state, 1);
            statusLabel.text = game.IsShotInProgress ? string.Empty : Status(state, game.LastOutcome);
        }

        private static void UpdatePlayer(TMP_Text label, EightBallState state, int player)
        {
            BallGroup group = state.GroupOf(player);
            string groupText = group == BallGroup.None ? "Open table" : $"{group} ({state.RemainingInGroup(group)} left)";
            if (state.IsOnEightBall(player)) groupText = "On the 8";
            label.text = $"Player {player + 1}\n<size=70%>{groupText}</size>";
            label.color = !state.IsGameOver && state.CurrentPlayer == player ? ActiveColor : IdleColor;
        }

        private static string Status(EightBallState state, ShotOutcome last)
        {
            string shooter = $"Player {state.CurrentPlayer + 1}";
            if (state.IsGameOver) return $"Player {state.Winner + 1} wins!";
            if (last != null && last.IsFoul)
            {
                string where = state.BallInHandKitchenOnly ? " behind the head string" : string.Empty;
                return $"Foul: {FoulText(last.Foul)}. {shooter} has ball in hand{where}";
            }

            if (state.IsBreakShot) return $"{shooter} to break";
            if (last != null && last.GroupsAssigned) return $"{shooter} takes {state.GroupOf(state.CurrentPlayer)}";
            if (last != null && last.EightBallRespotted) return $"8-ball respotted. {shooter} to shoot";
            return $"{shooter} to shoot";
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
                default: return "foul";
            }
        }
    }
}
