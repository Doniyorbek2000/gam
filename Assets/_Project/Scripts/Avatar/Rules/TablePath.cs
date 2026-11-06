using System;
using System.Collections.Generic;
using BilliardGame.Physics;

namespace BilliardGame.Avatar.Rules
{
    /// <summary>
    /// Walking route around the table when no NavMesh is baked (fallback; the NavMesh also routes around furniture).
    /// The table is a rectangle centred on the origin; the route goes straight or via the corners of the inflated
    /// rectangle, whichever way round is shorter.
    /// </summary>
    public static class TablePath
    {
        /// <summary>Fills <paramref name="route"/> with the waypoints after <paramref name="from"/>, ending at <paramref name="to"/>.</summary>
        public static void Route(Vec2 from, Vec2 to, double halfX, double halfY, List<Vec2> route)
        {
            route.Clear();
            if (!SegmentHitsBox(from, to, halfX, halfY))
            {
                route.Add(to);
                return;
            }

            // Corners counter-clockwise; walk from the corner nearest the start to the one nearest the goal.
            var corners = new[] { new Vec2(halfX, halfY), new Vec2(-halfX, halfY), new Vec2(-halfX, -halfY), new Vec2(halfX, -halfY) };
            int start = Nearest(corners, from), end = Nearest(corners, to);
            double ccw = Walk(corners, start, end, +1, from, to), cw = Walk(corners, start, end, -1, from, to);
            int step = ccw <= cw ? +1 : -1;
            for (int i = start; ; i = (i + step + 4) % 4)
            {
                route.Add(corners[i]);
                if (i == end) break;
            }

            route.Add(to);
        }

        private static double Walk(Vec2[] corners, int start, int end, int step, Vec2 from, Vec2 to)
        {
            double length = Vec2.Distance(from, corners[start]);
            for (int i = start; i != end; i = (i + step + 4) % 4) length += Vec2.Distance(corners[i], corners[(i + step + 4) % 4]);
            return length + Vec2.Distance(corners[end], to);
        }

        private static int Nearest(Vec2[] corners, Vec2 p)
        {
            int best = 0;
            for (int i = 1; i < corners.Length; i++)
            {
                if (Vec2.Distance(corners[i], p) < Vec2.Distance(corners[best], p)) best = i;
            }

            return best;
        }

        /// <summary>Slab test: does the open segment pass through the box interior (touching an edge does not count)?</summary>
        public static bool SegmentHitsBox(Vec2 a, Vec2 b, double halfX, double halfY)
        {
            const double inset = 1e-6;
            double t0 = 0.0, t1 = 1.0;
            Vec2 d = b - a;
            return Clip(-d.X, a.X - (-halfX + inset), ref t0, ref t1) && Clip(d.X, (halfX - inset) - a.X, ref t0, ref t1)
                && Clip(-d.Y, a.Y - (-halfY + inset), ref t0, ref t1) && Clip(d.Y, (halfY - inset) - a.Y, ref t0, ref t1);
        }

        private static bool Clip(double p, double q, ref double t0, ref double t1)
        {
            if (Math.Abs(p) < 1e-12) return q >= 0.0;
            double r = q / p;
            if (p < 0.0)
            {
                if (r > t1) return false;
                if (r > t0) t0 = r;
            }
            else
            {
                if (r < t0) return false;
                if (r < t1) t1 = r;
            }

            return true;
        }
    }
}
