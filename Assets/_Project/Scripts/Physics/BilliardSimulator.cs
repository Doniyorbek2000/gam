using System;
using System.Collections.Generic;

namespace BilliardGame.Physics
{
    /// <summary>
    /// Deterministic fixed-step billiard simulation. Each step applies half a step of cloth friction,
    /// moves balls in straight lines while resolving every contact at its exact time of impact,
    /// then applies the other half of the friction (second-order Strang splitting).
    /// </summary>
    public sealed class BilliardSimulator
    {
        private enum ContactKind
        {
            None,
            Ball,
            Cushion,
            Pocket,
        }

        private readonly struct Contact
        {
            public Contact(ContactKind kind, double time, int ballIndex, int otherIndex, int featureIndex, Vec2 normal)
            {
                Kind = kind;
                Time = time;
                BallIndex = ballIndex;
                OtherIndex = otherIndex;
                FeatureIndex = featureIndex;
                Normal = normal;
            }

            public ContactKind Kind { get; }
            public double Time { get; }
            public int BallIndex { get; }
            public int OtherIndex { get; }
            public int FeatureIndex { get; }
            public Vec2 Normal { get; }
        }

        public BilliardSimulator(TableGeometry table, PhysicsConfig config)
        {
            Table = table ?? throw new ArgumentNullException(nameof(table));
            Config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public TableGeometry Table { get; }
        public PhysicsConfig Config { get; }

        /// <summary>Strikes the cue ball named by <paramref name="shot"/> and runs until everything stops.</summary>
        public SimulationResult Simulate(IReadOnlyList<BallState> balls, ShotParams shot)
        {
            BallState[] states = CopyValidated(balls);
            int cueIndex = Array.FindIndex(states, b => b.Id == shot.CueBallId);
            if (cueIndex < 0) throw new ArgumentException($"Cue ball {shot.CueBallId} is not on the table.", nameof(shot));

            states[cueIndex] = BallMotion.ApplyCueStrike(states[cueIndex], shot, Config);
            return RunStates(states);
        }

        /// <summary>Runs from the given states as-is (no cue strike).</summary>
        public SimulationResult Run(IReadOnlyList<BallState> balls) => RunStates(CopyValidated(balls));

        private SimulationResult RunStates(BallState[] states)
        {
            double dt = Config.TimeStep;
            int maxSteps = (int)Math.Ceiling(Config.MaxSimulationTime / dt);
            var frames = new List<SimulationFrame> { new SimulationFrame(0.0, states) };
            var events = new List<SimulationEvent>();
            bool atRest = AllStationary(states);

            for (int step = 0; step < maxSteps && !atRest; step++)
            {
                double startTime = step * dt;
                ApplyFriction(states, dt * 0.5);
                Advance(states, startTime, dt, events);
                ApplyFriction(states, dt * 0.5);
                frames.Add(new SimulationFrame(startTime + dt, states));
                atRest = AllStationary(states);
            }

            return new SimulationResult(frames, events, atRest);
        }

        private void Advance(BallState[] states, double startTime, double dt, List<SimulationEvent> events)
        {
            double elapsed = 0.0;
            for (int i = 0; i < Config.MaxEventsPerStep; i++)
            {
                Contact contact = FindEarliestContact(states, dt - elapsed);
                if (contact.Kind == ContactKind.None) break;

                Move(states, contact.Time);
                elapsed += contact.Time;
                events.Add(Resolve(states, contact, startTime + elapsed));
            }

            // shortcut: once MaxEventsPerStep is hit the rest of the step ignores contacts; raise the limit if big clusters tunnel.
            Move(states, Math.Max(0.0, dt - elapsed));
        }

        private Contact FindEarliestContact(BallState[] states, double maxTime)
        {
            double r = Config.BallRadius;
            var best = new Contact(ContactKind.None, maxTime, -1, -1, -1, Vec2.Zero);

            for (int i = 0; i < states.Length; i++)
            {
                BallState a = states[i];
                if (a.IsPocketed) continue;
                bool aMoving = a.Velocity.LengthSquared > 0.0;

                for (int j = i + 1; j < states.Length; j++)
                {
                    BallState b = states[j];
                    if (b.IsPocketed || (!aMoving && b.Velocity.LengthSquared == 0.0)) continue;

                    double t = CollisionDetector.SweepCircle(a.Position - b.Position, a.Velocity - b.Velocity, Vec2.Zero, 2.0 * r, best.Time);
                    if (t <= best.Time) best = new Contact(ContactKind.Ball, t, i, j, -1, Vec2.Zero);
                }

                if (!aMoving) continue;

                for (int k = 0; k < Table.Cushions.Count; k++)
                {
                    CushionSegment segment = Table.Cushions[k];
                    double t = CollisionDetector.SweepSegment(a.Position, a.Velocity, segment, r, best.Time);
                    if (t <= best.Time) best = new Contact(ContactKind.Cushion, t, i, -1, k, segment.Normal);

                    best = EarliestJawContact(a, i, k, segment.Start, best);
                    best = EarliestJawContact(a, i, k, segment.End, best);
                }

                for (int p = 0; p < Table.Pockets.Count; p++)
                {
                    Pocket pocket = Table.Pockets[p];
                    double t = CollisionDetector.EnterCircle(a.Position, a.Velocity, pocket.Center, pocket.Radius, best.Time);
                    if (t <= best.Time) best = new Contact(ContactKind.Pocket, t, i, -1, p, Vec2.Zero);
                }
            }

            return best;
        }

        private Contact EarliestJawContact(BallState ball, int ballIndex, int cushionIndex, Vec2 jaw, Contact best)
        {
            double t = CollisionDetector.SweepCircle(ball.Position, ball.Velocity, jaw, Config.BallRadius, best.Time);
            if (t > best.Time) return best;

            Vec2 normal = (ball.Position + ball.Velocity * t - jaw).Normalized;
            return new Contact(ContactKind.Cushion, t, ballIndex, -1, cushionIndex, normal);
        }

        private SimulationEvent Resolve(BallState[] states, Contact contact, double time)
        {
            BallState a = states[contact.BallIndex];
            switch (contact.Kind)
            {
                case ContactKind.Ball:
                {
                    BallState b = states[contact.OtherIndex];
                    Vec2 normal = (b.Position - a.Position).Normalized;
                    double speed = Vec2.Dot(a.Velocity - b.Velocity, normal);
                    var (resolvedA, resolvedB) = CollisionResolver.ResolveBallBall(a, b, Config);
                    states[contact.BallIndex] = resolvedA;
                    states[contact.OtherIndex] = resolvedB;
                    return SimulationEvent.BallCollision(time, a.Id, b.Id, speed);
                }

                case ContactKind.Cushion:
                {
                    double speed = -Vec2.Dot(a.Velocity, contact.Normal);
                    states[contact.BallIndex] = CollisionResolver.ResolveBallCushion(a, contact.Normal, Config);
                    return SimulationEvent.CushionCollision(time, a.Id, contact.FeatureIndex, speed);
                }

                case ContactKind.Pocket:
                    states[contact.BallIndex] = a.AsPocketed(contact.FeatureIndex);
                    return SimulationEvent.BallPocketed(time, a.Id, contact.FeatureIndex, a.Velocity.Length);

                default:
                    throw new InvalidOperationException($"Unexpected contact kind {contact.Kind}.");
            }
        }

        private void ApplyFriction(BallState[] states, double dt)
        {
            for (int i = 0; i < states.Length; i++)
            {
                states[i] = BallMotion.ApplyFriction(states[i], dt, Config);
            }
        }

        private static void Move(BallState[] states, double dt)
        {
            if (dt <= 0.0) return;

            for (int i = 0; i < states.Length; i++)
            {
                BallState ball = states[i];
                if (ball.IsPocketed || ball.Velocity.LengthSquared == 0.0) continue;
                states[i] = ball.WithPosition(ball.Position + ball.Velocity * dt);
            }
        }

        private static bool AllStationary(BallState[] states)
        {
            foreach (BallState ball in states)
            {
                if (!ball.IsStationary) return false;
            }

            return true;
        }

        private static BallState[] CopyValidated(IReadOnlyList<BallState> balls)
        {
            if (balls == null) throw new ArgumentNullException(nameof(balls));

            var states = new BallState[balls.Count];
            var ids = new HashSet<int>();
            for (int i = 0; i < states.Length; i++)
            {
                BallState ball = balls[i];
                if (!ids.Add(ball.Id)) throw new ArgumentException($"Duplicate ball id {ball.Id}.", nameof(balls));
                if (!ball.Position.IsFinite || !ball.Velocity.IsFinite || !ball.AngularVelocity.IsFinite)
                {
                    throw new ArgumentException($"Ball {ball.Id} has a non-finite state.", nameof(balls));
                }

                states[i] = ball;
            }

            return states;
        }
    }
}
