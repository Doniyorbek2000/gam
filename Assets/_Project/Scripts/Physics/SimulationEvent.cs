using System.Globalization;

namespace BilliardGame.Physics
{
    public enum SimulationEventType
    {
        BallCollision,
        CushionCollision,
        BallPocketed,
    }

    /// <summary>Something that happened during a shot, in time order. Unused indices are -1.</summary>
    public readonly struct SimulationEvent
    {
        public const int None = -1;

        private SimulationEvent(SimulationEventType type, double time, int ballId, int otherBallId, int cushionIndex, int pocketIndex, double speed)
        {
            Type = type;
            Time = time;
            BallId = ballId;
            OtherBallId = otherBallId;
            CushionIndex = cushionIndex;
            PocketIndex = pocketIndex;
            Speed = speed;
        }

        public SimulationEventType Type { get; }

        /// <summary>Seconds since the start of the simulation.</summary>
        public double Time { get; }

        public int BallId { get; }
        public int OtherBallId { get; }

        /// <summary>Index into <see cref="TableGeometry.Cushions"/>; jaw hits report the segment that owns the jaw.</summary>
        public int CushionIndex { get; }

        public int PocketIndex { get; }

        /// <summary>Normal approach speed for collisions, ball speed for pocketing (m/s). Useful for audio.</summary>
        public double Speed { get; }

        public static SimulationEvent BallCollision(double time, int ballId, int otherBallId, double speed) =>
            new SimulationEvent(SimulationEventType.BallCollision, time, ballId, otherBallId, None, None, speed);

        public static SimulationEvent CushionCollision(double time, int ballId, int cushionIndex, double speed) =>
            new SimulationEvent(SimulationEventType.CushionCollision, time, ballId, None, cushionIndex, None, speed);

        public static SimulationEvent BallPocketed(double time, int ballId, int pocketIndex, double speed) =>
            new SimulationEvent(SimulationEventType.BallPocketed, time, ballId, None, None, pocketIndex, speed);

        public bool Involves(int ballId) => BallId == ballId || OtherBallId == ballId;

        public override string ToString() =>
            string.Format(CultureInfo.InvariantCulture, "{0:0.0000}s {1} ball={2} other={3} cushion={4} pocket={5} speed={6:0.###}",
                Time, Type, BallId, OtherBallId, CushionIndex, PocketIndex, Speed);
    }
}
