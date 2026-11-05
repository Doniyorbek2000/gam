using System;
using System.Collections.Generic;

namespace BilliardGame.Gameplay
{
    public enum FoulType
    {
        None,
        /// <summary>Cue ball pocketed.</summary>
        Scratch,
        NoBallHit,
        /// <summary>First ball contacted was not a legal target.</summary>
        WrongBallFirst,
        /// <summary>After contact no ball was pocketed and no ball reached a cushion.</summary>
        NoRailAfterContact,
        /// <summary>Break pocketed nothing and drove fewer than four object balls to a cushion.</summary>
        IllegalBreak,
        /// <summary>The shot clock ran out before the player shot.</summary>
        Timeout,
    }

    /// <summary>Result of judging one shot.</summary>
    public sealed class ShotOutcome
    {
        public ShotOutcome(
            EightBallState nextState,
            FoulType foul,
            int firstHitBallId,
            IReadOnlyList<int> pocketedBallIds,
            bool groupsAssigned,
            bool turnPassed,
            bool eightBallRespotted)
        {
            NextState = nextState ?? throw new ArgumentNullException(nameof(nextState));
            Foul = foul;
            FirstHitBallId = firstHitBallId;
            PocketedBallIds = pocketedBallIds ?? throw new ArgumentNullException(nameof(pocketedBallIds));
            GroupsAssigned = groupsAssigned;
            TurnPassed = turnPassed;
            EightBallRespotted = eightBallRespotted;
        }

        public EightBallState NextState { get; }
        public FoulType Foul { get; }
        public bool IsFoul => Foul != FoulType.None;

        /// <summary>First ball the cue ball touched, or -1.</summary>
        public int FirstHitBallId { get; }

        /// <summary>Pocketed during this shot, in pocketing order (may include the cue ball).</summary>
        public IReadOnlyList<int> PocketedBallIds { get; }

        public bool GroupsAssigned { get; }
        public bool TurnPassed { get; }
        public bool EightBallRespotted { get; }
    }
}
