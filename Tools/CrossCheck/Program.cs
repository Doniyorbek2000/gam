using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using BilliardGame.Gameplay;
using BilliardGame.Physics;

namespace BilliardGame.Tools
{
    /// <summary>
    /// Plays seeded random 8-ball games with the C# simulator and rules and writes every shot's inputs and
    /// outputs to JSON. The TypeScript server port must reproduce them exactly.
    /// Usage: dotnet run --project Tools/CrossCheck -- &lt;output.json&gt;
    /// </summary>
    internal static class Program
    {
        private const int SampleEvery = 60;
        private const int Games = 6;
        private const int MaxShotsPerGame = 14;
        private static readonly int[] RackSeeds = { 0, 1, 42, 12345, -7, int.MaxValue, int.MinValue };

        private static readonly TableGeometry Table = TableGeometry.CreateStandard();
        private static readonly PhysicsConfig Config = PhysicsConfig.Default;

        private static int Main(string[] args)
        {
            string output = args.Length > 0 ? args[0] : "fixtures.json";
            var simulator = new BilliardSimulator(Table, Config);
            var random = new Random(20261105);

            var racks = RackSeeds.Select(seed => new { seed, balls = EightBallRack.Create(Table, Config, seed).Select(Ball).ToArray() }).ToArray();

            var shots = new List<object>();
            for (int game = 0; game < Games; game++)
            {
                EightBallState state = EightBallState.NewGame(Table, Config, game % 2, random.Next());
                for (int n = 0; n < MaxShotsPerGame && !state.IsGameOver; n++)
                {
                    EightBallState before = state;
                    double[] place = null;
                    if (state.BallInHand)
                    {
                        Vec2 spot = Placement(state, random);
                        place = new[] { spot.X, spot.Y };
                        state = state.WithCueBallPlaced(spot, Table, Config);
                    }

                    Vec2 direction = AimDirection(state, random);
                    double speed = state.IsBreakShot ? 8.0 + random.NextDouble() * 4.0 : 0.5 + random.NextDouble() * 9.5;
                    double angle = random.NextDouble() * 2.0 * Math.PI;
                    double tip = Math.Sqrt(random.NextDouble()) * 0.49;
                    double side = tip * Math.Cos(angle);
                    double top = tip * Math.Sin(angle);
                    var shot = new ShotParams(direction, speed, side, top);

                    SimulationResult result = simulator.Simulate(state.Balls, shot);
                    ShotOutcome outcome = EightBallRules.ResolveShot(state, result.Events, result.FinalFrame, Table, Config);

                    shots.Add(new
                    {
                        state = State(before),
                        place,
                        shot = new { dx = direction.X, dy = direction.Y, speed, side, top },
                        frames = SampledFrames(result),
                        events = result.Events.Select(Event).ToArray(),
                        endedAtRest = result.EndedAtRest,
                        duration = result.Duration,
                        outcome = new
                        {
                            foul = (int)outcome.Foul,
                            firstHit = outcome.FirstHitBallId,
                            pocketed = outcome.PocketedBallIds.ToArray(),
                            groupsAssigned = outcome.GroupsAssigned,
                            turnPassed = outcome.TurnPassed,
                            respotted = outcome.EightBallRespotted,
                            next = State(outcome.NextState),
                        },
                    });
                    state = outcome.NextState;
                }

                if (!state.IsGameOver)
                {
                    ShotOutcome timeout = EightBallRules.ResolveTimeout(state);
                    shots.Add(new { timeoutOf = State(state), next = State(timeout.NextState) });
                }
            }

            string json = JsonSerializer.Serialize(new { sampleEvery = SampleEvery, racks, shots });
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
            File.WriteAllText(output, json);
            Console.WriteLine($"Wrote {racks.Length} racks and {shots.Count} shots to {output}");
            return 0;
        }

        /// <summary>Half the shots aim straight at a random object ball, the rest anywhere.</summary>
        private static Vec2 AimDirection(EightBallState state, Random random)
        {
            Vec2 cue = state.GetBall(EightBallSet.CueBall).Position;
            if (state.IsBreakShot) return new Vec2(1.0, (random.NextDouble() - 0.5) * 0.02);

            BallState[] targets = state.Balls.Where(b => !b.IsPocketed && b.Id != EightBallSet.CueBall).ToArray();
            if (targets.Length > 0 && random.NextDouble() < 0.5)
            {
                Vec2 target = targets[random.Next(targets.Length)].Position;
                return target - cue;
            }

            return new Vec2(random.NextDouble() * 2.0 - 1.0, random.NextDouble() * 2.0 - 1.0);
        }

        private static Vec2 Placement(EightBallState state, Random random)
        {
            for (int attempt = 0; attempt < 50; attempt++)
            {
                var candidate = new Vec2((random.NextDouble() - 0.5) * Table.Length, (random.NextDouble() - 0.5) * Table.Width);
                if (state.CanPlaceCueBall(candidate, Table, Config)) return candidate;
            }

            return EightBallRack.FindFreeSpot(EightBallRack.HeadSpot(Table), -1, state.Balls, EightBallSet.CueBall, Table, Config);
        }

        private static object[] SampledFrames(SimulationResult result)
        {
            var frames = new List<object>();
            for (int i = 0; i < result.Frames.Count; i++)
            {
                if (i % SampleEvery != 0 && i != result.Frames.Count - 1) continue;
                frames.Add(new { step = i, time = result.Frames[i].Time, balls = result.Frames[i].Balls.Select(Ball).ToArray() });
            }

            return frames.ToArray();
        }

        private static object State(EightBallState s) => new
        {
            balls = s.Balls.Select(Ball).ToArray(),
            cur = s.CurrentPlayer,
            g0 = (int)s.Player0Group,
            g1 = (int)s.Player1Group,
            brk = s.IsBreakShot,
            bih = s.BallInHand,
            kit = s.BallInHandKitchenOnly,
            win = s.Winner,
        };

        private static object Ball(BallState b) => new
        {
            id = b.Id,
            x = b.Position.X,
            y = b.Position.Y,
            vx = b.Velocity.X,
            vy = b.Velocity.Y,
            wx = b.AngularVelocity.X,
            wy = b.AngularVelocity.Y,
            wz = b.AngularVelocity.Z,
            pk = b.IsPocketed ? b.PocketIndex : -1,
        };

        private static object Event(SimulationEvent e) => new
        {
            type = (int)e.Type,
            time = e.Time,
            ball = e.BallId,
            other = e.OtherBallId,
            cushion = e.CushionIndex,
            pocket = e.PocketIndex,
            speed = e.Speed,
        };
    }
}
