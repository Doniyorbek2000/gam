using System;
using System.Collections.Generic;
using BilliardGame.Physics;

namespace BilliardGame.Gameplay
{
    /// <summary>Rack layout, table spots and free-spot search for 8-ball.</summary>
    public static class EightBallRack
    {
        /// <summary>Gap between racked balls so the triangle starts without overlapping contacts.</summary>
        public const double RackGap = 0.0005;

        private const double SpotSearchStep = 0.005;

        // Row by row from the apex: 8 in the middle of the third row, a solid and a stripe in the back corners.
        private static readonly int[] RackOrder = { 1, 9, 2, 10, 8, 3, 11, 4, 12, 5, 6, 13, 14, 7, 15 };

        public static Vec2 FootSpot(TableGeometry table) => new Vec2(table.Length * 0.25, 0.0);

        public static Vec2 HeadSpot(TableGeometry table) => new Vec2(HeadStringX(table), 0.0);

        /// <summary>Balls behind this X are in the kitchen.</summary>
        public static double HeadStringX(TableGeometry table) => -table.Length * 0.25;

        /// <summary>Fresh rack: index in the returned array equals the ball id.</summary>
        public static BallState[] Create(TableGeometry table, PhysicsConfig config)
        {
            if (table == null) throw new ArgumentNullException(nameof(table));
            if (config == null) throw new ArgumentNullException(nameof(config));

            double spacing = 2.0 * config.BallRadius + RackGap;
            double rowStep = spacing * Math.Sqrt(3.0) * 0.5;
            Vec2 foot = FootSpot(table);

            var balls = new BallState[EightBallSet.BallCount];
            balls[EightBallSet.CueBall] = BallState.AtRest(EightBallSet.CueBall, HeadSpot(table));

            int next = 0;
            for (int row = 0; row < 5; row++)
            {
                for (int i = 0; i <= row; i++)
                {
                    int id = RackOrder[next++];
                    var position = new Vec2(foot.X + row * rowStep, foot.Y + (i - row * 0.5) * spacing);
                    balls[id] = BallState.AtRest(id, position);
                }
            }

            return balls;
        }

        /// <summary>True when a ball centre at <paramref name="position"/> is on the cloth, clear of pockets and other balls.</summary>
        public static bool IsFreeSpot(Vec2 position, IReadOnlyList<BallState> balls, int ignoreBallId, TableGeometry table, PhysicsConfig config)
        {
            double r = config.BallRadius;
            if (!position.IsFinite) return false;
            if (Math.Abs(position.X) > table.HalfLength - r || Math.Abs(position.Y) > table.HalfWidth - r) return false;

            for (int p = 0; p < table.Pockets.Count; p++)
            {
                Pocket pocket = table.Pockets[p];
                if (Vec2.Distance(position, pocket.Center) < pocket.Radius + r) return false;
            }

            double minDistance = 2.0 * r;
            for (int i = 0; i < balls.Count; i++)
            {
                BallState ball = balls[i];
                if (ball.Id == ignoreBallId || ball.IsPocketed) continue;
                if (Vec2.Distance(position, ball.Position) < minDistance) return false;
            }

            return true;
        }

        /// <summary>
        /// Walks from <paramref name="start"/> along X in the sign of <paramref name="directionX"/>, then the other way,
        /// and returns the first free spot (spotting along the long string).
        /// </summary>
        public static Vec2 FindFreeSpot(Vec2 start, int directionX, IReadOnlyList<BallState> balls, int ignoreBallId, TableGeometry table, PhysicsConfig config)
        {
            int first = directionX >= 0 ? 1 : -1;
            for (int pass = 0; pass < 2; pass++)
            {
                int sign = pass == 0 ? first : -first;
                for (double x = start.X; Math.Abs(x) <= table.HalfLength; x += sign * SpotSearchStep)
                {
                    var candidate = new Vec2(x, start.Y);
                    if (IsFreeSpot(candidate, balls, ignoreBallId, table, config)) return candidate;
                }
            }

            throw new InvalidOperationException("No free spot on the long string.");
        }
    }
}
