using System;

namespace BilliardGame.Physics
{
    /// <summary>
    /// Ball-on-cloth dynamics: cue strike, sliding friction, rolling resistance and side-spin decay.
    /// Contact point with the cloth is at -R along Z, so its velocity is v + w x (-R z).
    /// </summary>
    public static class BallMotion
    {
        /// <summary>Slip below this speed (m/s) counts as pure rolling.</summary>
        public const double SlipEpsilon = 1e-9;

        /// <summary>Velocity of the ball's cloth contact point; zero means pure rolling.</summary>
        public static Vec2 ContactSlip(BallState ball, double radius)
        {
            Vec3 w = ball.AngularVelocity;
            return new Vec2(ball.Velocity.X - radius * w.Y, ball.Velocity.Y + radius * w.X);
        }

        public static bool IsRolling(BallState ball, double radius) => ContactSlip(ball, radius).Length <= SlipEpsilon;

        /// <summary>
        /// Applies an instantaneous cue impulse J = m*Speed along the aim, delivered at the tip offset.
        /// w = (r x J) / I gives w = 5*Speed/(2R) * (side * z + top * left), where left is the aim rotated +90 degrees.
        /// </summary>
        public static BallState ApplyCueStrike(BallState cueBall, ShotParams shot, PhysicsConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (cueBall.IsPocketed) throw new InvalidOperationException($"Ball {cueBall.Id} is pocketed and cannot be struck.");
            if (!(shot.Speed > 0.0)) throw new ArgumentException("Shot is uninitialised (zero speed).", nameof(shot));

            Vec2 aim = shot.Direction;
            Vec2 left = aim.Perp;
            double k = 2.5 * shot.Speed / config.BallRadius;
            var spin = new Vec3(k * shot.TopSpin * left.X, k * shot.TopSpin * left.Y, k * shot.SideSpin);
            return cueBall.WithMotion(aim * shot.Speed, spin);
        }

        /// <summary>
        /// Integrates cloth friction over <paramref name="dt"/> exactly for piecewise-constant forces:
        /// slip decays at 7/2 * mu_s * g until the ball rolls, then rolling resistance slows it to rest.
        /// </summary>
        public static BallState ApplyFriction(BallState ball, double dt, PhysicsConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (ball.IsPocketed || dt <= 0.0 || ball.IsStationary) return ball;

            double r = config.BallRadius;
            Vec2 v = ball.Velocity;
            Vec3 w = ball.AngularVelocity;
            double wx = w.X;
            double wy = w.Y;
            double wz = DecayTowardZero(w.Z, config.SpinDeceleration * dt);
            double remaining = dt;

            Vec2 slip = ContactSlip(ball, r);
            double slipSpeed = slip.Length;
            if (slipSpeed > SlipEpsilon)
            {
                double slideDecel = config.SlidingFriction * config.Gravity;
                double timeToRoll = 2.0 * slipSpeed / (7.0 * slideDecel);
                double t = Math.Min(remaining, timeToRoll);
                Vec2 u = slip / slipSpeed;

                v -= u * (slideDecel * t);
                double angularDelta = 2.5 * slideDecel * t / r;
                wx -= angularDelta * u.Y;
                wy += angularDelta * u.X;
                remaining -= t;

                if (remaining <= 0.0)
                {
                    return ball.WithMotion(v, new Vec3(wx, wy, wz));
                }
            }

            double speed = v.Length;
            double speedLoss = config.RollingFriction * config.Gravity * remaining;
            v = speed <= speedLoss ? Vec2.Zero : v * (1.0 - speedLoss / speed);
            return ball.WithMotion(v, new Vec3(-v.Y / r, v.X / r, wz));
        }

        private static double DecayTowardZero(double value, double amount)
        {
            if (Math.Abs(value) <= amount) return 0.0;
            return value - Math.Sign(value) * amount;
        }
    }
}
