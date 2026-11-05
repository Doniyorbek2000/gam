using System;
using System.Collections.Generic;
using System.Linq;

namespace BilliardGame.Physics
{
    /// <summary>
    /// Straight cushion nose line. A ball collides with the segment interior or with its
    /// end points (the pocket jaws). The ball centre is kept <see cref="PhysicsConfig.BallRadius"/> away.
    /// </summary>
    public readonly struct CushionSegment
    {
        public CushionSegment(Vec2 start, Vec2 end, Vec2 inwardNormal)
        {
            Vec2 span = end - start;
            double length = span.Length;
            if (!(length > 0.0) || !span.IsFinite)
            {
                throw new ArgumentException("Cushion segment must have a finite, non-zero length.");
            }

            Vec2 normal = inwardNormal.Normalized;
            Vec2 direction = span / length;
            if (normal == Vec2.Zero || Math.Abs(Vec2.Dot(direction, normal)) > 1e-9)
            {
                throw new ArgumentException("Cushion normal must be non-zero and perpendicular to the segment.");
            }

            Start = start;
            End = end;
            Length = length;
            Direction = direction;
            Normal = normal;
        }

        public Vec2 Start { get; }
        public Vec2 End { get; }
        public double Length { get; }

        /// <summary>Unit vector from <see cref="Start"/> to <see cref="End"/>.</summary>
        public Vec2 Direction { get; }

        /// <summary>Unit normal pointing from the cushion into the playing area.</summary>
        public Vec2 Normal { get; }
    }

    /// <summary>A ball is pocketed when its centre enters this circle.</summary>
    public readonly struct Pocket
    {
        public Pocket(Vec2 center, double radius)
        {
            if (!center.IsFinite || !double.IsFinite(radius) || radius <= 0.0)
            {
                throw new ArgumentException("Pocket needs a finite centre and a positive radius.");
            }

            Center = center;
            Radius = radius;
        }

        public Vec2 Center { get; }
        public double Radius { get; }
    }

    /// <summary>
    /// Table layout. Origin at the table centre; the playing area spans
    /// [-Length/2, Length/2] x [-Width/2, Width/2] measured at the cushion noses.
    /// </summary>
    public sealed class TableGeometry
    {
        public const double StandardLength = 2.54;
        public const double StandardWidth = 1.27;

        /// <summary>Distance from a corner along each rail where the cushion stops (corner jaw tip).</summary>
        public const double StandardCornerGap = 0.08;

        /// <summary>Half of the side pocket opening along the long rail.</summary>
        public const double StandardSideHalfGap = 0.065;

        public const double StandardCornerPocketRadius = 0.06;
        public const double StandardSidePocketRadius = 0.045;

        /// <summary>How far the side pocket centre sits behind the cushion line.</summary>
        public const double StandardSidePocketOffset = 0.02;

        // Pocket indices produced by Create(), counter-clockwise from bottom-left.
        public const int BottomLeftPocket = 0;
        public const int BottomSidePocket = 1;
        public const int BottomRightPocket = 2;
        public const int TopRightPocket = 3;
        public const int TopSidePocket = 4;
        public const int TopLeftPocket = 5;

        // Cushion indices produced by Create(), counter-clockwise from bottom-left.
        public const int BottomLeftCushion = 0;
        public const int BottomRightCushion = 1;
        public const int RightCushion = 2;
        public const int TopRightCushion = 3;
        public const int TopLeftCushion = 4;
        public const int LeftCushion = 5;

        private readonly CushionSegment[] cushions;
        private readonly Pocket[] pockets;

        public TableGeometry(double length, double width, IEnumerable<CushionSegment> cushions, IEnumerable<Pocket> pockets)
        {
            if (!double.IsFinite(length) || length <= 0.0) throw new ArgumentOutOfRangeException(nameof(length));
            if (!double.IsFinite(width) || width <= 0.0) throw new ArgumentOutOfRangeException(nameof(width));

            Length = length;
            Width = width;
            this.cushions = (cushions ?? throw new ArgumentNullException(nameof(cushions))).ToArray();
            this.pockets = (pockets ?? throw new ArgumentNullException(nameof(pockets))).ToArray();
            Cushions = Array.AsReadOnly(this.cushions);
            Pockets = Array.AsReadOnly(this.pockets);
        }

        public double Length { get; }
        public double Width { get; }
        public double HalfLength => Length * 0.5;
        public double HalfWidth => Width * 0.5;
        public IReadOnlyList<CushionSegment> Cushions { get; }
        public IReadOnlyList<Pocket> Pockets { get; }

        /// <summary>Standard 9 ft table: 2.54 x 1.27 m playing area with 6 pockets.</summary>
        public static TableGeometry CreateStandard() =>
            Create(StandardLength, StandardWidth, StandardCornerGap, StandardSideHalfGap,
                StandardCornerPocketRadius, StandardSidePocketRadius, StandardSidePocketOffset);

        public static TableGeometry Create(
            double length,
            double width,
            double cornerGap,
            double sideHalfGap,
            double cornerPocketRadius,
            double sidePocketRadius,
            double sidePocketOffset)
        {
            double hl = length * 0.5;
            double hw = width * 0.5;
            if (!(cornerGap > 0.0) || cornerGap >= hw) throw new ArgumentOutOfRangeException(nameof(cornerGap));
            if (!(sideHalfGap > 0.0) || sideHalfGap + cornerGap >= hl) throw new ArgumentOutOfRangeException(nameof(sideHalfGap));
            if (sidePocketOffset < 0.0) throw new ArgumentOutOfRangeException(nameof(sidePocketOffset));

            var up = Vec2.UnitY;
            var right = Vec2.UnitX;
            var segments = new[]
            {
                new CushionSegment(new Vec2(-hl + cornerGap, -hw), new Vec2(-sideHalfGap, -hw), up),
                new CushionSegment(new Vec2(sideHalfGap, -hw), new Vec2(hl - cornerGap, -hw), up),
                new CushionSegment(new Vec2(hl, -hw + cornerGap), new Vec2(hl, hw - cornerGap), -right),
                new CushionSegment(new Vec2(hl - cornerGap, hw), new Vec2(sideHalfGap, hw), -up),
                new CushionSegment(new Vec2(-sideHalfGap, hw), new Vec2(-hl + cornerGap, hw), -up),
                new CushionSegment(new Vec2(-hl, hw - cornerGap), new Vec2(-hl, -hw + cornerGap), right),
            };

            var holes = new[]
            {
                new Pocket(new Vec2(-hl, -hw), cornerPocketRadius),
                new Pocket(new Vec2(0.0, -hw - sidePocketOffset), sidePocketRadius),
                new Pocket(new Vec2(hl, -hw), cornerPocketRadius),
                new Pocket(new Vec2(hl, hw), cornerPocketRadius),
                new Pocket(new Vec2(0.0, hw + sidePocketOffset), sidePocketRadius),
                new Pocket(new Vec2(-hl, hw), cornerPocketRadius),
            };

            return new TableGeometry(length, width, segments, holes);
        }
    }
}
