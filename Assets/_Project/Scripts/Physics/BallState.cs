using System.Globalization;

namespace BilliardGame.Physics
{
    /// <summary>
    /// Immutable snapshot of one ball. Position is the ball centre in table space
    /// (origin at the table centre, X along the length, Y along the width).
    /// </summary>
    public readonly struct BallState
    {
        public const int NoPocket = -1;

        public BallState(int id, Vec2 position, Vec2 velocity, Vec3 angularVelocity)
            : this(id, position, velocity, angularVelocity, false, NoPocket)
        {
        }

        private BallState(int id, Vec2 position, Vec2 velocity, Vec3 angularVelocity, bool isPocketed, int pocketIndex)
        {
            Id = id;
            Position = position;
            Velocity = velocity;
            AngularVelocity = angularVelocity;
            IsPocketed = isPocketed;
            PocketIndex = pocketIndex;
        }

        public int Id { get; }
        public Vec2 Position { get; }

        /// <summary>Linear velocity of the centre, m/s.</summary>
        public Vec2 Velocity { get; }

        /// <summary>Angular velocity, rad/s. Z is side spin (positive = counter-clockwise seen from above).</summary>
        public Vec3 AngularVelocity { get; }

        public bool IsPocketed { get; }

        /// <summary>Index into <see cref="TableGeometry.Pockets"/>, or <see cref="NoPocket"/>.</summary>
        public int PocketIndex { get; }

        public bool IsStationary => Velocity.LengthSquared == 0.0 && AngularVelocity.LengthSquared == 0.0;

        public static BallState AtRest(int id, Vec2 position) => new BallState(id, position, Vec2.Zero, Vec3.Zero);

        public BallState WithPosition(Vec2 position) =>
            new BallState(Id, position, Velocity, AngularVelocity, IsPocketed, PocketIndex);

        public BallState WithMotion(Vec2 velocity, Vec3 angularVelocity) =>
            new BallState(Id, Position, velocity, angularVelocity, IsPocketed, PocketIndex);

        public BallState AsPocketed(int pocketIndex) =>
            new BallState(Id, Position, Vec2.Zero, Vec3.Zero, true, pocketIndex);

        public override string ToString() =>
            string.Format(CultureInfo.InvariantCulture, "Ball {0} p={1} v={2} w={3}{4}",
                Id, Position, Velocity, AngularVelocity, IsPocketed ? " pocketed:" + PocketIndex : string.Empty);
    }
}
