using System;
using BilliardGame.Gameplay;
using BilliardGame.Meta;
using BilliardGame.Presentation;

namespace BilliardGame.UI
{
    /// <summary>What the result screen shows after a match.</summary>
    public sealed class MatchSummary
    {
        public bool Won;
        public string Winner;
        public bool Practice;
        public int XpBefore;
        public int XpGained;
        public int CoinsGained;
        public int BallsPotted;
    }

    /// <summary>
    /// Counts the profile owner's shots during a match (shots, potting shots, balls, most in one shot) by watching
    /// <see cref="GameController"/> verdicts, then writes the match to the profile once it is over.
    /// Hot-seat: the owner is seat 0. Online: the local seat.
    /// </summary>
    public sealed class MatchRecorder
    {
        private int seat;
        private int shooter = -1;
        private ShotOutcome lastSeen;
        private bool active;
        private int shots;
        private int pottingShots;
        private int balls;
        private int maxBalls;

        public bool IsActive => active;

        /// <summary>Call on every game state change; returns a summary once, when the game has just ended.</summary>
        public MatchSummary Observe(GameController game, GameServices services, string venueId, int prize)
        {
            EightBallState state = game.State;
            if (state == null)
            {
                active = false;
                return null;
            }

            if (!active && game.LastOutcome == null && !game.IsShotInProgress && !state.IsGameOver) Begin(game);
            if (!active) return null;

            if (game.IsShotInProgress && shooter < 0) shooter = state.CurrentPlayer;
            ShotOutcome outcome = game.LastOutcome;
            if (outcome != null && outcome != lastSeen)
            {
                lastSeen = outcome;
                if (shooter == seat) Count(outcome);
                shooter = -1;
            }

            return state.IsGameOver ? Finish(game, services, venueId, prize) : null;
        }

        private void Begin(GameController game)
        {
            active = true;
            seat = game.IsOnline ? Math.Max(0, game.LocalPlayer) : 0;
            shooter = -1;
            lastSeen = null;
            shots = pottingShots = balls = maxBalls = 0;
        }

        private void Count(ShotOutcome outcome)
        {
            shots++;
            int potted = 0;
            foreach (int id in outcome.PocketedBallIds)
            {
                if (id != EightBallSet.CueBall) potted++;
            }

            if (potted > 0 && !outcome.IsFoul) pottingShots++;
            balls += potted;
            maxBalls = Math.Max(maxBalls, potted);
        }

        private MatchSummary Finish(GameController game, GameServices services, string venueId, int prize)
        {
            active = false;
            EightBallState state = game.State;
            bool won = state.Winner == seat;
            bool practice = !game.IsOnline;
            // Online prizes move to the server with venue escrow (M7); until then only the offline mock grants them.
            int coins = !practice && won ? prize : 0;
            if (coins > 0) services.Mock?.GrantMatchReward(coins);

            int xpBefore = services.Profile.State.xp;
            var record = new MatchRecord
            {
                opponent = game.PlayerName(1 - seat),
                won = won,
                venueId = venueId,
                cueId = services.Economy.EquippedId(ItemType.Cue),
                unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                shots = shots,
                pottingShots = pottingShots,
                ballsPotted = balls,
                maxBallsInOneShot = maxBalls,
                xpGained = ProfileRules.MatchXp(won, balls),
                coinsGained = coins,
            };
            services.Profile.RecordMatch(record);
            return new MatchSummary
            {
                Won = won,
                Winner = game.PlayerName(state.Winner),
                Practice = practice,
                XpBefore = xpBefore,
                XpGained = record.xpGained,
                CoinsGained = coins,
                BallsPotted = balls,
            };
        }
    }
}
