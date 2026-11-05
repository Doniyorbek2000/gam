using System;
using BilliardGame.Gameplay;
using BilliardGame.Physics;
using UnityEngine;

namespace BilliardGame.Presentation
{
    /// <summary>
    /// Match loop. Hot-seat: takes a shot, simulates it, replays it, then applies the rules' verdict.
    /// Online: hands the shot to the server and replays the frames, events and state the server sends back.
    /// </summary>
    public sealed class GameController : MonoBehaviour
    {
        [SerializeField] private TablePlayback playback;
        [SerializeField] private CueController cue;
        [SerializeField] private TableAudio tableAudio;
        [SerializeField] private float minShotSpeed = 0.4f;
        [SerializeField] private float maxShotSpeed = 11f;
        [Tooltip("Power curve exponent: >1 gives finer control of soft shots on the slider.")]
        [SerializeField] private float powerCurve = 1.6f;

        private static readonly string[] HotSeatNames = { "Player 1", "Player 2" };

        private TableGeometry table;
        private PhysicsConfig config;
        private BilliardSimulator simulator;
        private int nextBreaker;
        private readonly System.Random rackSeeds = new System.Random();
        private readonly string[] playerNames = (string[])HotSeatNames.Clone();

        private Action<ShotParams, Vec2?> sendShot;
        private bool awaitingServer;
        private EightBallState pendingState;
        private ShotOutcome pendingOutcome;
        private float turnDeadline = float.NaN;

        public event Action StateChanged;

        /// <summary>Null while no game is running (main menu).</summary>
        public EightBallState State { get; private set; }

        /// <summary>Verdict of the last finished shot, or null at the start of a game.</summary>
        public ShotOutcome LastOutcome { get; private set; }

        public TableGeometry Table => table;
        public PhysicsConfig Config => config;
        public bool IsOnline => sendShot != null;

        /// <summary>This device's seat in an online match; -1 for hot-seat, where both seats play here.</summary>
        public int LocalPlayer { get; private set; } = -1;

        /// <summary>Connection or match-end message that overrides the normal status line, or null.</summary>
        public string Banner { get; private set; }

        public bool IsShotInProgress => playback.IsPlaying || awaitingServer;
        public bool IsLocalTurn => State != null && (LocalPlayer < 0 || State.CurrentPlayer == LocalPlayer);
        public bool CanShoot => State != null && !State.IsGameOver && !IsShotInProgress && IsLocalTurn;

        /// <summary>Whole seconds left on the shot clock, or -1 when there is none or a shot is replaying.</summary>
        public int TurnSecondsLeft =>
            float.IsNaN(turnDeadline) || IsShotInProgress || State == null || State.IsGameOver
                ? -1
                : Mathf.Max(0, Mathf.CeilToInt(turnDeadline - Time.realtimeSinceStartup));

        public string PlayerName(int player) => playerNames[player];

        private void Awake()
        {
            table = TableGeometry.CreateStandard();
            config = PhysicsConfig.Default;
            simulator = new BilliardSimulator(table, config);
        }

        // Racked table behind the main menu; no game runs until a mode is chosen.
        private void Start()
        {
            playback.Snap(EightBallRack.Create(table, config));
            cue.End();
        }

        public void StartOffline()
        {
            StopGame();
            NewGame();
        }

        /// <summary>Hands shots to <paramref name="shotSender"/>; the board waits for <see cref="ApplyRemoteState"/>.</summary>
        public void StartOnline(int localPlayer, Action<ShotParams, Vec2?> shotSender)
        {
            StopGame();
            sendShot = shotSender ?? throw new ArgumentNullException(nameof(shotSender));
            LocalPlayer = localPlayer;
        }

        public void StopGame()
        {
            playback.Cancel();
            sendShot = null;
            awaitingServer = false;
            pendingState = null;
            pendingOutcome = null;
            LocalPlayer = -1;
            Banner = null;
            turnDeadline = float.NaN;
            HotSeatNames.CopyTo(playerNames, 0);
            State = null;
            LastOutcome = null;
            cue.End();
            StateChanged?.Invoke();
        }

        /// <summary>Re-racks; the break alternates between the two players.</summary>
        public void NewGame()
        {
            if (IsOnline) return;
            LastOutcome = null;
            ApplyState(EightBallState.NewGame(table, config, nextBreaker, rackSeeds.Next()));
            nextBreaker = 1 - nextBreaker;
        }

        public void SetPlayerNames(string player0, string player1)
        {
            playerNames[0] = player0;
            playerNames[1] = player1;
            StateChanged?.Invoke();
        }

        public void SetBanner(string text)
        {
            Banner = string.IsNullOrEmpty(text) ? null : text;
            StateChanged?.Invoke();
        }

        /// <summary>Shot clock in seconds from now; NaN hides it.</summary>
        public void SetTurnTimer(float seconds) => turnDeadline = float.IsNaN(seconds) ? float.NaN : Time.realtimeSinceStartup + seconds;

        /// <summary>Shoots along the current aim with slider power and spin-pad offset.</summary>
        public void Shoot(float power01, Vector2 spin)
        {
            if (!CanShoot) return;

            double speed = Mathf.Lerp(minShotSpeed, maxShotSpeed, Mathf.Pow(Mathf.Clamp01(power01), powerCurve));
            Vector2 tip = Vector2.ClampMagnitude(spin, 1f) * (float)(ShotParams.MaxTipOffset * 0.999);
            var shot = new ShotParams(cue.AimDirection, speed, tip.x, tip.y, EightBallSet.CueBall);
            Shoot(shot, State.BallInHand ? cue.CuePosition : (Vec2?)null);
        }

        /// <summary>Shoots exactly <paramref name="shot"/>; <paramref name="cuePlacement"/> is required when the cue ball is in hand.</summary>
        public void Shoot(ShotParams shot, Vec2? cuePlacement)
        {
            if (!CanShoot) return;
            if (State.BallInHand && (!cuePlacement.HasValue || !State.CanPlaceCueBall(cuePlacement.Value, table, config))) return;

            float power01 = Mathf.Clamp01((float)(shot.Speed / maxShotSpeed));
            cue.End();
            if (IsOnline)
            {
                awaitingServer = true;
                tableAudio.PlayCueStrike(power01);
                sendShot(shot, State.BallInHand ? cuePlacement : null);
                StateChanged?.Invoke();
                return;
            }

            EightBallState state = State;
            if (state.BallInHand) state = state.WithCueBallPlaced(cuePlacement.Value, table, config);

            // shortcut: simulates synchronously on the main thread; move to a worker thread if breaks hitch on low-end phones.
            SimulationResult result = simulator.Simulate(state.Balls, shot);
            ShotOutcome outcome = EightBallRules.ResolveShot(state, result.Events, result.FinalFrame, table, config);

            State = state;
            tableAudio.PlayCueStrike(power01);
            Replay(result, outcome);
        }

        /// <summary>Server snapshot. Arriving mid-replay it is held until the replay ends.</summary>
        public void ApplyRemoteState(EightBallState state, ShotOutcome last)
        {
            awaitingServer = false;
            if (playback.IsPlaying)
            {
                pendingState = state;
                pendingOutcome = last;
                StateChanged?.Invoke();
                return;
            }

            LastOutcome = last;
            ApplyState(state);
        }

        /// <summary>Replays a shot the server simulated; <paramref name="outcome"/> carries the state after it.</summary>
        public void PlayRemoteShot(SimulationResult result, ShotOutcome outcome, bool shotByOpponent)
        {
            awaitingServer = false;
            cue.End();
            if (shotByOpponent) tableAudio.PlayCueStrike(0.6f);
            Replay(result, outcome);
        }

        /// <summary>The server refused the shot; let the player try again.</summary>
        public void RejectShot()
        {
            awaitingServer = false;
            if (State != null) ApplyState(State);
        }

        private void Replay(SimulationResult result, ShotOutcome outcome)
        {
            pendingState = null;
            pendingOutcome = null;
            playback.Play(result, () =>
            {
                LastOutcome = pendingState != null ? pendingOutcome : outcome;
                EightBallState next = pendingState ?? outcome.NextState;
                pendingState = null;
                pendingOutcome = null;
                ApplyState(next);
            });
            StateChanged?.Invoke();
        }

        private void ApplyState(EightBallState next)
        {
            State = next;
            playback.Snap(next.Balls);

            if (next.IsGameOver || !IsLocalTurn)
            {
                cue.End();
            }
            else
            {
                BallState cueBall = next.GetBall(EightBallSet.CueBall);
                Vec2 cuePosition = cueBall.Position;
                if (next.BallInHand && (cueBall.IsPocketed || !next.CanPlaceCueBall(cuePosition, table, config)))
                {
                    cuePosition = EightBallRack.FindFreeSpot(EightBallRack.HeadSpot(table), -1, next.Balls, EightBallSet.CueBall, table, config);
                }

                cue.Begin(next.Balls, cuePosition, next.BallInHand, table, config, p => next.CanPlaceCueBall(p, table, config));
            }

            StateChanged?.Invoke();
        }
    }
}
