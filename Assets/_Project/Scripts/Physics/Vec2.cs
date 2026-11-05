using System;
using System.Globalization;

namespace BilliardGame.Physics
{
    /// <summary>Immutable 2D vector in table space (metres), double precision.</summary>
    public readonly struct Vec2 : IEquatable<Vec2>
    {
        public static readonly Vec2 Zero = new Vec2(0.0, 0.0);
        public static readonly Vec2 UnitX = new Vec2(1.0, 0.0);
        public static readonly Vec2 UnitY = new Vec2(0.0, 1.0);

        public Vec2(double x, double y)
        {
            X = x;
            Y = y;
        }

        public double X { get; }
        public double Y { get; }

        public double LengthSquared => X * X + Y * Y;
        public double Length => Math.Sqrt(LengthSquared);

        /// <summary>Angle from the +X axis in radians, in (-PI, PI].</summary>
        public double Angle => Math.Atan2(Y, X);

        /// <summary>This vector rotated 90 degrees counter-clockwise.</summary>
        public Vec2 Perp => new Vec2(-Y, X);

        public bool IsFinite => double.IsFinite(X) && double.IsFinite(Y);

        /// <summary>Unit vector in the same direction, or <see cref="Zero"/> for a zero vector.</summary>
        public Vec2 Normalized
        {
            get
            {
                double length = Length;
                return length > 0.0 ? new Vec2(X / length, Y / length) : Zero;
            }
        }

        public static Vec2 FromAngle(double radians) => new Vec2(Math.Cos(radians), Math.Sin(radians));

        public static double Dot(Vec2 a, Vec2 b) => a.X * b.X + a.Y * b.Y;

        /// <summary>Z component of the 3D cross product of (a, 0) and (b, 0).</summary>
        public static double Cross(Vec2 a, Vec2 b) => a.X * b.Y - a.Y * b.X;

        public static double Distance(Vec2 a, Vec2 b) => (a - b).Length;

        public static Vec2 operator +(Vec2 a, Vec2 b) => new Vec2(a.X + b.X, a.Y + b.Y);
        public static Vec2 operator -(Vec2 a, Vec2 b) => new Vec2(a.X - b.X, a.Y - b.Y);
        public static Vec2 operator -(Vec2 a) => new Vec2(-a.X, -a.Y);
        public static Vec2 operator *(Vec2 a, double s) => new Vec2(a.X * s, a.Y * s);
        public static Vec2 operator *(double s, Vec2 a) => new Vec2(a.X * s, a.Y * s);
        public static Vec2 operator /(Vec2 a, double s) => new Vec2(a.X / s, a.Y / s);
        public static bool operator ==(Vec2 a, Vec2 b) => a.Equals(b);
        public static bool operator !=(Vec2 a, Vec2 b) => !a.Equals(b);

        public bool Equals(Vec2 other) => X.Equals(other.X) && Y.Equals(other.Y);
        public override bool Equals(object obj) => obj is Vec2 other && Equals(other);
        public override int GetHashCode() => (X.GetHashCode() * 397) ^ Y.GetHashCode();

        public override string ToString() =>
            string.Format(CultureInfo.InvariantCulture, "({0:0.#####}, {1:0.#####})", X, Y);
    }
}
