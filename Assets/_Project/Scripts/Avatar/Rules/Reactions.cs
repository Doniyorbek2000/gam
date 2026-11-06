using BilliardGame.Gameplay;

namespace BilliardGame.Avatar.Rules
{
    public enum Reaction { None, Clap, Disappointed, Celebrate }

    /// <summary>Who reacts to a finished shot and how (docs/GDD.md §8.1). Display only.</summary>
    public static class Reactions
    {
        /// <summary>The shooter: a foul or a miss hangs the head; winning the rack celebrates.</summary>
        public static Reaction ForShooter(ShotOutcome outcome, int shooter)
        {
            if (outcome == null) return Reaction.None;
            EightBallState next = outcome.NextState;
            if (next.IsGameOver) return next.Winner == shooter ? Reaction.Celebrate : Reaction.Disappointed;
            if (outcome.IsFoul) return Reaction.Disappointed;
            return outcome.TurnPassed && outcome.PocketedBallIds.Count == 0 ? Reaction.Disappointed : Reaction.None;
        }

        /// <summary>The seated player applauds a good shot by the opponent: two or more balls, or a pot that keeps the turn.</summary>
        public static Reaction ForWatcher(ShotOutcome outcome)
        {
            if (outcome == null || outcome.IsFoul || outcome.NextState.IsGameOver) return Reaction.None;
            int potted = outcome.PocketedBallIds.Count;
            return potted >= 2 || (potted == 1 && !outcome.TurnPassed) ? Reaction.Clap : Reaction.None;
        }
    }
}
