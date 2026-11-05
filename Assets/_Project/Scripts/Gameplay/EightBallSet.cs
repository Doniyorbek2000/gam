namespace BilliardGame.Gameplay
{
    public enum BallGroup
    {
        None,
        Solids,
        Stripes,
    }

    /// <summary>Ball numbering for 8-ball: 0 cue, 1-7 solids, 8 the eight ball, 9-15 stripes.</summary>
    public static class EightBallSet
    {
        public const int CueBall = 0;
        public const int EightBall = 8;
        public const int BallCount = 16;

        public static BallGroup GroupOf(int ballId)
        {
            if (ballId >= 1 && ballId <= 7) return BallGroup.Solids;
            if (ballId >= 9 && ballId <= 15) return BallGroup.Stripes;
            return BallGroup.None;
        }

        public static BallGroup Opposite(BallGroup group)
        {
            switch (group)
            {
                case BallGroup.Solids: return BallGroup.Stripes;
                case BallGroup.Stripes: return BallGroup.Solids;
                default: return BallGroup.None;
            }
        }
    }
}
