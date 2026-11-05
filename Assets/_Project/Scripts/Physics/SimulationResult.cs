using System;
using System.Collections.Generic;
using System.Linq;

namespace BilliardGame.Physics
{
    /// <summary>Immutable snapshot of every ball at one instant.</summary>
    public sealed class SimulationFrame
    {
        public SimulationFrame(double time, IEnumerable<BallState> balls)
        {
            Time = time;
            Balls = Array.AsReadOnly((balls ?? throw new ArgumentNullException(nameof(balls))).ToArray());
        }

        public double Time { get; }

        /// <summary>Balls in the same order as the simulation input.</summary>
        public IReadOnlyList<BallState> Balls { get; }

        public BallState GetBall(int id)
        {
            foreach (BallState ball in Balls)
            {
                if (ball.Id == id) return ball;
            }

            throw new KeyNotFoundException($"Ball {id} is not in this frame.");
        }
    }

    /// <summary>Full outcome of one shot: a frame per fixed step (plus the initial frame) and the event log.</summary>
    public sealed class SimulationResult
    {
        public SimulationResult(IEnumerable<SimulationFrame> frames, IEnumerable<SimulationEvent> events, bool endedAtRest)
        {
            Frames = Array.AsReadOnly(frames.ToArray());
            Events = Array.AsReadOnly(events.ToArray());
            if (Frames.Count == 0) throw new ArgumentException("A simulation result needs at least one frame.", nameof(frames));
            EndedAtRest = endedAtRest;
        }

        public IReadOnlyList<SimulationFrame> Frames { get; }
        public IReadOnlyList<SimulationEvent> Events { get; }

        /// <summary>False when <see cref="PhysicsConfig.MaxSimulationTime"/> ran out before every ball stopped.</summary>
        public bool EndedAtRest { get; }

        public SimulationFrame FinalFrame => Frames[Frames.Count - 1];
        public double Duration => FinalFrame.Time;
    }
}
