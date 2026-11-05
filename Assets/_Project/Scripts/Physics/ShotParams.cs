using System;

namespace BilliardGame.Physics
{
    /// <summary>
    /// Cue strike description. Tip offsets are fractions of the ball radius measured
    /// on the face of the ball the cue sees: side to the shooter's right, top upwards.
    /// </summary>
    public readonly struct ShotParams
    {
        public const double MaxSpeed = 12.0;

        /// <summary>Beyond about half a radius the tip miscues on a real table.</summary>
        public const double MaxTipOffset = 0.5;

        /// <summary>Top offset at which a centre-struck ball leaves the cue in pure rolling (h = 7/5 R).</summary>
        public const double NaturalRollTopSpin = 0.4;

        public ShotParams(Vec2 direction, double speed, double sideSpin = 0.0, double topSpin = 0.0, int cueBallId = 0)
        {
            if (!direction.IsFinite || direction.LengthSquared < 1e-24)
            {
                throw new ArgumentException("Direction must be a finite, non-zero vector.", nameof(direction));
            }

            if (!double.IsFinite(speed) || speed <= 0.0 || speed > MaxSpeed)
            {
                throw new ArgumentOutOfRangeException(nameof(speed), speed, $"Speed must be within (0, {MaxSpeed}] m/s.");
            }

            if (!double.IsFinite(sideSpin) || !double.IsFinite(topSpin) ||
                sideSpin * sideSpin + topSpin * topSpin > MaxTipOffset * MaxTipOffset)
            {
                throw new ArgumentOutOfRangeException(nameof(sideSpin), $"Tip offset must be finite and within {MaxTipOffset} of the centre.");
            }

            Direction = direction.Normalized;
            Speed = speed;
            SideSpin = sideSpin;
            TopSpin = topSpin;
            CueBallId = cueBallId;
        }

        /// <summary>Unit aiming direction in table space.</summary>
        public Vec2 Direction { get; }

        /// <summary>Cue ball speed right after the strike, m/s.</summary>
        public double Speed { get; }

        /// <summary>Positive = right english, negative = left english.</summary>
        public double SideSpin { get; }

        /// <summary>Positive = follow (top spin), negative = draw (back spin).</summary>
        public double TopSpin { get; }

        public int CueBallId { get; }

        public static ShotParams FromAngle(double angleRadians, double speed, double sideSpin = 0.0, double topSpin = 0.0, int cueBallId = 0) =>
            new ShotParams(Vec2.FromAngle(angleRadians), speed, sideSpin, topSpin, cueBallId);
    }
}
