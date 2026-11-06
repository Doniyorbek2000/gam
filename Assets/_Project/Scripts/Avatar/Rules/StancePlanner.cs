using System;
using BilliardGame.Physics;

namespace BilliardGame.Avatar.Rules
{
    /// <summary>
    /// Where the shooter stands for a shot, in table coordinates (docs/GDD.md §8.1). Pure geometry, visual only:
    /// nothing here feeds back into ShotParams or the rules.
    /// </summary>
    public static class StancePlanner
    {
        /// <summary>Cue ball to the shooter's hips along the cue for a comfortable bridge.</summary>
        public const double PreferredDistance = 1.05;

        /// <summary>Hips keep this far from the playing surface edge (frame + rail + a little air).</summary>
        public const double TableClearance = 0.35;

        /// <summary>Right-handed: the cue runs under the right shoulder, so the hips sit this far left of the cue line.</summary>
        public const double LateralOffset = 0.18;

        /// <summary>Hip point behind the cue ball, pushed out until it clears the table rectangle.</summary>
        public static Vec2 StandPoint(Vec2 cueBall, Vec2 aim, double halfLength, double halfWidth)
        {
            Vec2 forward = aim.Normalized;
            if (forward == Vec2.Zero) forward = Vec2.UnitX;
            Vec2 back = -forward;
            double exit = ExitDistance(cueBall, back, halfLength + TableClearance, halfWidth + TableClearance);
            double distance = Math.Max(PreferredDistance, exit);
            return cueBall + back * distance + forward.Perp * LateralOffset;
        }

        /// <summary>How far a ray from <paramref name="origin"/> (inside the rectangle) travels before leaving it.</summary>
        public static double ExitDistance(Vec2 origin, Vec2 direction, double halfX, double halfY)
        {
            double tx = direction.X > 1e-9 ? (halfX - origin.X) / direction.X : direction.X < -1e-9 ? (-halfX - origin.X) / direction.X : double.PositiveInfinity;
            double ty = direction.Y > 1e-9 ? (halfY - origin.Y) / direction.Y : direction.Y < -1e-9 ? (-halfY - origin.Y) / direction.Y : double.PositiveInfinity;
            return Math.Max(0.0, Math.Min(tx, ty));
        }

        /// <summary>True when the stance moved enough to walk again instead of shuffling in place.</summary>
        public static bool NeedsReposition(Vec2 current, Vec2 target, double threshold = 0.35) => Vec2.Distance(current, target) > threshold;
    }
}
