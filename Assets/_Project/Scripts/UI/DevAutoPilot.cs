using System;
using BilliardGame.Gameplay;
using BilliardGame.Physics;
using BilliardGame.Presentation;
using UnityEngine;

namespace BilliardGame.UI
{
    /// <summary>
    /// Development bot for end-to-end tests: plays the local seat by trying ghost-ball shots at every legal
    /// ball/pocket pair with the local simulator and taking the best verdict. Off unless the player is launched
    /// with <c>-autopilot</c> (which also starts a quick match) or the component is enabled from the editor.
    /// </summary>
    public sealed class DevAutoPilot : MonoBehaviour
    {
        private const string Argument = "-autopilot";

        [SerializeField] private GameController game;
        [SerializeField] private MainMenuController menu;
        [SerializeField] private float thinkSeconds = 1.2f;

        private static readonly double[] Speeds = { 2.5, 4.5, 7.0 };
        private readonly System.Random random = new System.Random();
        private BilliardSimulator simulator;
        private float readySince = -1f;

        private void Awake() => enabled = Array.IndexOf(Environment.GetCommandLineArgs(), Argument) >= 0;

        // -autopilot alone: quick match. With "-join 123456": joins that friend room instead.
        private void Start()
        {
            if (!enabled) return;
            string[] args = Environment.GetCommandLineArgs();
            int join = Array.IndexOf(args, "-join");
            if (join >= 0 && join + 1 < args.Length) menu.JoinRoom(args[join + 1]);
            else menu.QuickMatch();
        }

        private void Update()
        {
            if (!game.CanShoot)
            {
                readySince = -1f;
                return;
            }

            if (readySince < 0f) readySince = Time.unscaledTime;
            if (Time.unscaledTime - readySince < thinkSeconds) return;
            readySince = -1f;

            simulator ??= new BilliardSimulator(game.Table, game.Config);
            ChooseShot(game.State, out ShotParams shot, out Vec2? place);
            game.Shoot(shot, place);
        }

        private void ChooseShot(EightBallState state, out ShotParams bestShot, out Vec2? bestPlace)
        {
            TableGeometry table = game.Table;
            PhysicsConfig config = game.Config;
            int me = state.CurrentPlayer;
            Vec2 cue = state.GetBall(EightBallSet.CueBall).Position;
            bestPlace = null;
            if (state.BallInHand)
            {
                cue = EightBallRack.FindFreeSpot(EightBallRack.HeadSpot(table), -1, state.Balls, EightBallSet.CueBall, table, config);
                bestPlace = cue;
            }

            if (state.IsBreakShot)
            {
                bestShot = new ShotParams(new Vec2(1.0, (random.NextDouble() - 0.5) * 0.01), 10.5);
                return;
            }

            BallGroup group = state.GroupOf(me);
            bool onEight = state.IsOnEightBall(me);
            bestShot = default;
            double bestScore = double.NegativeInfinity;

            foreach (BallState target in state.Balls)
            {
                if (target.IsPocketed || target.Id == EightBallSet.CueBall || !EightBallRules.IsLegalFirstHit(target.Id, group, onEight)) continue;
                foreach (Pocket pocket in table.Pockets)
                {
                    Vec2 toPocket = (pocket.Center - target.Position).Normalized;
                    Vec2 ghost = target.Position - toPocket * (2.0 * config.BallRadius);
                    Vec2 from = cue;
                    Vec2? place = bestPlace;
                    if (state.BallInHand && !state.BallInHandKitchenOnly)
                    {
                        Vec2 behind = ghost - toPocket * 0.25;
                        if (state.CanPlaceCueBall(behind, table, config))
                        {
                            from = behind;
                            place = behind;
                        }
                    }

                    Vec2 aim = ghost - from;
                    if (aim.LengthSquared < 1e-8) continue;
                    foreach (double speed in Speeds)
                    {
                        var shot = new ShotParams(aim, speed);
                        double score = Evaluate(state, place, shot, me) + random.NextDouble();
                        if (score <= bestScore) continue;
                        bestScore = score;
                        bestShot = shot;
                        bestPlace = place;
                    }
                }
            }

            if (bestScore == double.NegativeInfinity) bestShot = new ShotParams(Vec2.FromAngle(random.NextDouble() * 2.0 * Math.PI), 4.0);
        }

        private double Evaluate(EightBallState state, Vec2? place, ShotParams shot, int me)
        {
            EightBallState start = place.HasValue ? state.WithCueBallPlaced(place.Value, game.Table, game.Config) : state;
            SimulationResult result = simulator.Simulate(start.Balls, shot);
            ShotOutcome outcome = EightBallRules.ResolveShot(start, result.Events, result.FinalFrame, game.Table, game.Config);
            if (outcome.NextState.IsGameOver) return outcome.NextState.Winner == me ? 1000.0 : -1000.0;
            double score = outcome.IsFoul ? -50.0 : 0.0;
            if (!outcome.TurnPassed) score += 100.0;
            return score;
        }
    }
}
