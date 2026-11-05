using System;
using System.Collections.Generic;
using BilliardGame.Physics;

namespace BilliardGame.Gameplay
{
    /// <summary>Straight-line aim guide: where the cue ball first touches a ball (ghost ball) or a cushion.</summary>
    public readonly struct AimPrediction
    {
        public AimPrediction(Vec2 ghostPosition, int hitBallId, Vec2 objectBallDirection, Vec2 cueBallDirection)
        {
            GhostPosition = ghostPosition;
            HitBallId = hitBallId;
            ObjectBallDirection = objectBallDirection;
            CueBallDirection = cueBallDirection;
        }

        /// <summary>Cue ball centre at first contact.</summary>
        public Vec2 GhostPosition { get; }

        /// <summary>Ball hit first, or -1 when the line reaches a cushion first.</summary>
        public int HitBallId { get; }

        /// <summary>Unit direction the object ball leaves along (zero when no ball is hit).</summary>
        public Vec2 ObjectBallDirection { get; }

        /// <summary>Stun-shot tangent line of the cue ball after contact (zero for a full hit or no ball).</summary>
        public Vec2 CueBallDirection { get; }

        public bool HitsBall => HitBallId >= 0;
    }

    public static class AimPredictor
    {
        /// <summary>Allocation-free so it can run every frame while aiming.</summary>
        public static AimPrediction Predict(Vec2 cuePosition, Vec2 direction, IReadOnlyList<BallState> balls, int cueBallId, TableGeometry table, PhysicsConfig config)
        {
            Vec2 dir = direction.Normalized;
            if (dir == Vec2.Zero) throw new ArgumentException("Aim direction must be non-zero.", nameof(direction));

            double r = config.BallRadius;
            double best = RailDistance(cuePosition, dir, table.HalfLength - r, table.HalfWidth - r);
            int hitId = -1;
            Vec2 hitCentre = Vec2.Zero;
            double contact = 2.0 * r;

            for (int i = 0; i < balls.Count; i++)
            {
                BallState ball = balls[i];
                if (ball.Id == cueBallId || ball.IsPocketed) continue;

                Vec2 toBall = ball.Position - cuePosition;
                double along = Vec2.Dot(toBall, dir);
                if (along <= 0.0) continue;
                double perpSq = toBall.LengthSquared - along * along;
                double reach = contact * contact - perpSq;
                if (reach < 0.0) continue;
                double t = along - Math.Sqrt(reach);
                if (t < 0.0 || t >= best) continue;

                best = t;
                hitId = ball.Id;
                hitCentre = ball.Position;
            }

            Vec2 ghost = cuePosition + dir * best;
            if (hitId < 0) return new AimPrediction(ghost, -1, Vec2.Zero, Vec2.Zero);

            Vec2 objectDir = (hitCentre - ghost).Normalized;
            Vec2 cueDir = (dir - objectDir * Vec2.Dot(dir, objectDir)).Normalized;
            return new AimPrediction(ghost, hitId, objectDir, cueDir);
        }

        // Distance along dir until the centre reaches the inset rectangle bounding the cushion noses.
        private static double RailDistance(Vec2 p, Vec2 dir, double maxX, double maxY)
        {
            double tx = dir.X > 0.0 ? (maxX - p.X) / dir.X : dir.X < 0.0 ? (-maxX - p.X) / dir.X : double.PositiveInfinity;
            double ty = dir.Y > 0.0 ? (maxY - p.Y) / dir.Y : dir.Y < 0.0 ? (-maxY - p.Y) / dir.Y : double.PositiveInfinity;
            return Math.Max(0.0, Math.Min(tx, ty));
        }
    }
}
