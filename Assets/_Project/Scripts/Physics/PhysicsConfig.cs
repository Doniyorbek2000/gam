using System;

namespace BilliardGame.Physics
{
    /// <summary>Immutable physical constants for the simulation. SI units throughout.</summary>
    public sealed class PhysicsConfig
    {
        public const double StandardTimeStep = 1.0 / 240.0;

        public static readonly PhysicsConfig Default = new PhysicsConfig();

        public PhysicsConfig(
            double ballRadius = 0.028575,
            double ballMass = 0.17,
            double gravity = 9.81,
            double slidingFriction = 0.2,
            double rollingFriction = 0.01,
            double spinDeceleration = 10.0,
            double ballRestitution = 0.95,
            double ballFriction = 0.05,
            double cushionRestitution = 0.75,
            double cushionFriction = 0.2,
            double timeStep = StandardTimeStep,
            double maxSimulationTime = 60.0,
            int maxEventsPerStep = 256)
        {
            BallRadius = RequirePositive(ballRadius, nameof(ballRadius));
            BallMass = RequirePositive(ballMass, nameof(ballMass));
            Gravity = RequirePositive(gravity, nameof(gravity));
            SlidingFriction = RequirePositive(slidingFriction, nameof(slidingFriction));
            RollingFriction = RequirePositive(rollingFriction, nameof(rollingFriction));
            SpinDeceleration = RequireRange(spinDeceleration, 0.0, double.MaxValue, nameof(spinDeceleration));
            BallRestitution = RequireRange(ballRestitution, 0.0, 1.0, nameof(ballRestitution));
            BallFriction = RequireRange(ballFriction, 0.0, double.MaxValue, nameof(ballFriction));
            CushionRestitution = RequireRange(cushionRestitution, 0.0, 1.0, nameof(cushionRestitution));
            CushionFriction = RequireRange(cushionFriction, 0.0, double.MaxValue, nameof(cushionFriction));
            TimeStep = RequirePositive(timeStep, nameof(timeStep));
            MaxSimulationTime = RequirePositive(maxSimulationTime, nameof(maxSimulationTime));
            if (maxEventsPerStep < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxEventsPerStep), maxEventsPerStep, "Must be at least 1.");
            }

            MaxEventsPerStep = maxEventsPerStep;
        }

        public double BallRadius { get; }
        public double BallMass { get; }
        public double Gravity { get; }

        /// <summary>Ball-cloth friction coefficient while the contact point slips.</summary>
        public double SlidingFriction { get; }

        /// <summary>Rolling resistance coefficient once the ball rolls without slipping.</summary>
        public double RollingFriction { get; }

        /// <summary>Decay of side spin (vertical-axis spin) from cloth contact, rad/s^2.</summary>
        public double SpinDeceleration { get; }

        public double BallRestitution { get; }

        /// <summary>Ball-ball contact friction; produces throw.</summary>
        public double BallFriction { get; }

        public double CushionRestitution { get; }

        /// <summary>Ball-cushion contact friction; lets side spin change the rebound angle.</summary>
        public double CushionFriction { get; }

        public double TimeStep { get; }
        public double MaxSimulationTime { get; }
        public int MaxEventsPerStep { get; }

        /// <summary>Moment of inertia of a solid sphere about its centre.</summary>
        public double BallInertia => 0.4 * BallMass * BallRadius * BallRadius;

        private static double RequirePositive(double value, string name)
        {
            if (!double.IsFinite(value) || value <= 0.0)
            {
                throw new ArgumentOutOfRangeException(name, value, "Must be a finite positive number.");
            }

            return value;
        }

        private static double RequireRange(double value, double min, double max, string name)
        {
            if (!double.IsFinite(value) || value < min || value > max)
            {
                throw new ArgumentOutOfRangeException(name, value, $"Must be within [{min}, {max}].");
            }

            return value;
        }
    }
}
