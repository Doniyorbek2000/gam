using System;
using BilliardGame.Gameplay;
using BilliardGame.Physics;
using UnityEngine;

namespace BilliardGame.Presentation
{
    /// <summary>Hot-seat match loop: takes a shot, simulates it, replays it, then applies the rules' verdict.</summary>
    public sealed class GameController : MonoBehaviour
    {
        [SerializeField] private TablePlayback playback;
        [SerializeField] private CueController cue;
        [SerializeField] private float minShotSpeed = 0.4f;
        [SerializeField] private float maxShotSpeed = 9f;

        private TableGeometry table;
        private PhysicsConfig config;
        private BilliardSimulator simulator;
        private int nextBreaker;

        public event Action StateChanged;

        public EightBallState State { get; private set; }

        /// <summary>Verdict of the last finished shot, or null at the start of a game.</summary>
        public ShotOutcome LastOutcome { get; private set; }

        public bool IsShotInProgress => playback.IsPlaying;
        public bool CanShoot => State != null && !State.IsGameOver && !IsShotInProgress;

        private void Awake()
        {
            table = TableGeometry.CreateStandard();
            config = PhysicsConfig.Default;
            simulator = new BilliardSimulator(table, config);
        }

        private void Start() => NewGame();

        /// <summary>Re-racks; the break alternates between the two players.</summary>
        public void NewGame()
        {
            LastOutcome = null;
            ApplyState(EightBallState.NewGame(table, config, nextBreaker));
            nextBreaker = 1 - nextBreaker;
        }

        public void Shoot(float power01, Vector2 spin)
        {
            if (!CanShoot) return;

            EightBallState state = State;
            if (state.BallInHand) state = state.WithCueBallPlaced(cue.CuePosition, table, config);

            double speed = Mathf.Lerp(minShotSpeed, maxShotSpeed, Mathf.Clamp01(power01));
            Vector2 tip = Vector2.ClampMagnitude(spin, 1f) * (float)(ShotParams.MaxTipOffset * 0.999);
            var shot = new ShotParams(cue.AimDirection, speed, tip.x, tip.y, EightBallSet.CueBall);

            // shortcut: simulates synchronously on the main thread; move to a worker thread if breaks hitch on low-end phones.
            SimulationResult result = simulator.Simulate(state.Balls, shot);
            ShotOutcome outcome = EightBallRules.ResolveShot(state, result.Events, result.FinalFrame, table, config);

            State = state;
            cue.End();
            playback.Play(result, () =>
            {
                LastOutcome = outcome;
                ApplyState(outcome.NextState);
            });
            StateChanged?.Invoke();
        }

        private void ApplyState(EightBallState next)
        {
            State = next;
            playback.Snap(next.Balls);

            if (next.IsGameOver)
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
