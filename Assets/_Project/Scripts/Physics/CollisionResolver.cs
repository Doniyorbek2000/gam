using System;

namespace BilliardGame.Physics
{
    /// <summary>
    /// Impulse-based collision response at the ball equator. Normal impulse uses restitution;
    /// tangential impulse is Coulomb friction capped at the impulse that stops contact slip,
    /// and it exchanges side spin (Z) with linear velocity. Top/back spin is left untouched
    /// so it re-emerges as follow/draw through cloth friction afterwards.
    /// </summary>
    public static class CollisionResolver
    {
        private const double SlipEpsilon = 1e-12;

        /// <summary>Resolves a touching ball pair. Returns the inputs unchanged if they are separating.</summary>
        public static (BallState A, BallState B) ResolveBallBall(BallState a, BallState b, PhysicsConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));

            Vec2 n = (b.Position - a.Position).Normalized;
            double approach = Vec2.Dot(a.Velocity - b.Velocity, n);
            if (n == Vec2.Zero || approach <= 0.0) return (a, b);

            double m = config.BallMass;
            double r = config.BallRadius;
            double jn = 0.5 * m * (1.0 + config.BallRestitution) * approach;
            Vec2 va = a.Velocity - n * (jn / m);
            Vec2 vb = b.Velocity + n * (jn / m);
            double wza = a.AngularVelocity.Z;
            double wzb = b.AngularVelocity.Z;

            // Contact point sits at +R*n from A and -R*n from B.
            Vec2 perp = n.Perp;
            Vec2 relative = (a.Velocity + perp * (wza * r)) - (b.Velocity - perp * (wzb * r));
            Vec2 relativeTangent = relative - n * Vec2.Dot(relative, n);
            double slip = relativeTangent.Length;
            if (slip > SlipEpsilon)
            {
                Vec2 t = relativeTangent / slip;
                // Each ball's contact point responds at 7/(2m) per unit tangential impulse, 7/m for the pair.
                double jt = Math.Min(config.BallFriction * jn, m * slip / 7.0);
                va -= t * (jt / m);
                vb += t * (jt / m);
                double spinDelta = -2.5 * jt * Vec2.Cross(n, t) / (m * r);
                wza += spinDelta;
                wzb += spinDelta;
            }

            return (a.WithMotion(va, a.AngularVelocity.WithZ(wza)),
                    b.WithMotion(vb, b.AngularVelocity.WithZ(wzb)));
        }

        /// <summary>
        /// Resolves a ball against a static cushion or jaw. <paramref name="normal"/> points from
        /// the contact towards the ball centre. Returns the input unchanged if the ball is moving away.
        /// </summary>
        public static BallState ResolveBallCushion(BallState ball, Vec2 normal, PhysicsConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));

            Vec2 n = normal.Normalized;
            double vn = Vec2.Dot(ball.Velocity, n);
            if (n == Vec2.Zero || vn >= 0.0) return ball;

            double m = config.BallMass;
            double r = config.BallRadius;
            double jn = m * (1.0 + config.CushionRestitution) * -vn;
            Vec2 v = ball.Velocity + n * (jn / m);
            double wz = ball.AngularVelocity.Z;

            // Contact point sits at -R*n from the centre.
            Vec2 contactVelocity = ball.Velocity - n.Perp * (wz * r);
            Vec2 tangent = contactVelocity - n * Vec2.Dot(contactVelocity, n);
            double slip = tangent.Length;
            if (slip > SlipEpsilon)
            {
                Vec2 t = tangent / slip;
                double jt = Math.Min(config.CushionFriction * jn, 2.0 * m * slip / 7.0);
                v -= t * (jt / m);
                wz += 2.5 * jt * Vec2.Cross(n, t) / (m * r);
            }

            return ball.WithMotion(v, ball.AngularVelocity.WithZ(wz));
        }
    }
}
