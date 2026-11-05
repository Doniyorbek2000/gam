using System;

namespace BilliardGame.Physics
{
    /// <summary>
    /// Time-of-impact queries for straight-line motion within one step.
    /// Every query returns <see cref="double.PositiveInfinity"/> when there is no hit in [0, maxTime].
    /// </summary>
    internal static class CollisionDetector
    {
        /// <summary>Contacts approaching slower than this (m/s) are treated as resting, not impacts.</summary>
        public const double MinApproachSpeed = 1e-6;

        /// <summary>
        /// First time a point at <paramref name="p"/> moving with <paramref name="v"/> comes within
        /// <paramref name="radius"/> of <paramref name="target"/> while approaching it.
        /// Ball-ball contact uses relative position/velocity and radius 2R; jaw contact uses R.
        /// </summary>
        public static double SweepCircle(Vec2 p, Vec2 v, Vec2 target, double radius, double maxTime)
        {
            Vec2 d = p - target;
            double a = v.LengthSquared;
            double halfB = Vec2.Dot(d, v);
            if (a <= 0.0 || halfB >= 0.0) return double.PositiveInfinity;

            double c = d.LengthSquared - radius * radius;
            double t;
            if (c <= 0.0)
            {
                t = 0.0;
            }
            else
            {
                double discriminant = halfB * halfB - a * c;
                if (discriminant < 0.0) return double.PositiveInfinity;
                t = (-halfB - Math.Sqrt(discriminant)) / a;
            }

            if (t > maxTime) return double.PositiveInfinity;

            Vec2 contact = d + v * t;
            double approach = -Vec2.Dot(v, contact) / contact.Length;
            return approach > MinApproachSpeed ? t : double.PositiveInfinity;
        }

        /// <summary>First time the ball touches the interior of a cushion segment from the playing side.</summary>
        public static double SweepSegment(Vec2 p, Vec2 v, CushionSegment segment, double radius, double maxTime)
        {
            double vn = Vec2.Dot(v, segment.Normal);
            if (vn > -MinApproachSpeed) return double.PositiveInfinity;

            double distance = Vec2.Dot(p - segment.Start, segment.Normal);
            if (distance < 0.0) return double.PositiveInfinity;

            double t = Math.Max(0.0, (distance - radius) / -vn);
            if (t > maxTime) return double.PositiveInfinity;

            double along = Vec2.Dot(p + v * t - segment.Start, segment.Direction);
            return along >= 0.0 && along <= segment.Length ? t : double.PositiveInfinity;
        }

        /// <summary>First time the point enters the circle (0 if already inside).</summary>
        public static double EnterCircle(Vec2 p, Vec2 v, Vec2 center, double radius, double maxTime)
        {
            Vec2 d = p - center;
            double c = d.LengthSquared - radius * radius;
            if (c <= 0.0) return 0.0;

            double halfB = Vec2.Dot(d, v);
            if (halfB >= 0.0) return double.PositiveInfinity;

            double a = v.LengthSquared;
            double discriminant = halfB * halfB - a * c;
            if (discriminant < 0.0) return double.PositiveInfinity;

            double t = (-halfB - Math.Sqrt(discriminant)) / a;
            return t <= maxTime ? t : double.PositiveInfinity;
        }
    }
}
