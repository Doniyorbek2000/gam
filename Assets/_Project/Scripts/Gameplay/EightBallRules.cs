using System;
using System.Collections.Generic;
using BilliardGame.Physics;

namespace BilliardGame.Gameplay
{
    /// <summary>
    /// 8-ball judging (WPA-style, simplified): legal break, open table, groups, fouls with ball in hand, and the 8.
    /// Pure function of the state before the shot and what the simulation reported.
    /// </summary>
    public static class EightBallRules
    {
        public const int MinBreakCushionBalls = 4;

        public static ShotOutcome ResolveShot(
            EightBallState state,
            IReadOnlyList<SimulationEvent> events,
            SimulationFrame finalFrame,
            TableGeometry table,
            PhysicsConfig config)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (events == null) throw new ArgumentNullException(nameof(events));
            if (finalFrame == null) throw new ArgumentNullException(nameof(finalFrame));
            if (state.IsGameOver) throw new InvalidOperationException("The game is already over.");
            if (state.BallInHand) throw new InvalidOperationException("Place the cue ball before shooting.");

            int player = state.CurrentPlayer;
            BallGroup group = state.GroupOf(player);
            bool onEight = state.IsOnEightBall(player);

            int firstHit = FindFirstHit(events, out double firstHitTime);
            var pocketed = new List<int>();
            bool railAfterContact = false;
            var cushionBalls = new HashSet<int>();
            foreach (SimulationEvent e in events)
            {
                if (e.Type == SimulationEventType.BallPocketed) pocketed.Add(e.BallId);
                if (e.Type != SimulationEventType.CushionCollision) continue;
                if (e.BallId != EightBallSet.CueBall) cushionBalls.Add(e.BallId);
                if (firstHit >= 0 && e.Time >= firstHitTime) railAfterContact = true;
            }

            bool cuePocketed = pocketed.Contains(EightBallSet.CueBall);
            bool eightPocketed = pocketed.Contains(EightBallSet.EightBall);

            FoulType foul = FoulType.None;
            if (cuePocketed) foul = FoulType.Scratch;
            else if (firstHit < 0) foul = FoulType.NoBallHit;
            else if (!state.IsBreakShot && !IsLegalFirstHit(firstHit, group, onEight)) foul = FoulType.WrongBallFirst;
            else if (state.IsBreakShot && pocketed.Count == 0 && cushionBalls.Count < MinBreakCushionBalls) foul = FoulType.IllegalBreak;
            else if (!state.IsBreakShot && pocketed.Count == 0 && !railAfterContact) foul = FoulType.NoRailAfterContact;

            BallState[] balls = SettleBalls(finalFrame);
            bool respotted = false;
            int winner = EightBallState.NoWinner;
            if (eightPocketed && state.IsBreakShot)
            {
                Respot(balls, EightBallSet.EightBall, table, config);
                respotted = true;
            }
            else if (eightPocketed)
            {
                winner = onEight && foul == FoulType.None ? player : 1 - player;
            }

            BallGroup p0 = state.Player0Group;
            BallGroup p1 = state.Player1Group;
            bool groupsAssigned = false;
            int firstObject = FirstObjectBall(pocketed);
            if (state.IsTableOpen && !state.IsBreakShot && foul == FoulType.None && firstObject >= 0)
            {
                // shortcut: open-table group follows the first ball pocketed instead of a called shot; add calling for ranked play.
                BallGroup claimed = EightBallSet.GroupOf(firstObject);
                p0 = player == 0 ? claimed : EightBallSet.Opposite(claimed);
                p1 = EightBallSet.Opposite(p0);
                group = claimed;
                groupsAssigned = true;
            }

            bool keepsTurn = foul == FoulType.None && winner == EightBallState.NoWinner &&
                (state.IsBreakShot ? HasNonCueBall(pocketed) : PocketedOwn(pocketed, group));
            int nextPlayer = keepsTurn ? player : 1 - player;
            bool ballInHand = foul != FoulType.None && winner == EightBallState.NoWinner;

            var next = new EightBallState(balls, nextPlayer, p0, p1, isBreakShot: false,
                ballInHand: ballInHand, ballInHandKitchenOnly: state.IsBreakShot, winner: winner);
            return new ShotOutcome(next, foul, firstHit, pocketed.AsReadOnly(), groupsAssigned, !keepsTurn, respotted);
        }

        public static bool IsLegalFirstHit(int ballId, BallGroup group, bool onEight)
        {
            if (group == BallGroup.None) return ballId != EightBallSet.EightBall;
            if (onEight) return ballId == EightBallSet.EightBall;
            return EightBallSet.GroupOf(ballId) == group;
        }

        private static int FindFirstHit(IReadOnlyList<SimulationEvent> events, out double time)
        {
            foreach (SimulationEvent e in events)
            {
                if (e.Type != SimulationEventType.BallCollision || !e.Involves(EightBallSet.CueBall)) continue;
                time = e.Time;
                return e.BallId == EightBallSet.CueBall ? e.OtherBallId : e.BallId;
            }

            time = double.PositiveInfinity;
            return -1;
        }

        // Every ball ends at rest; a 60 s timeout should not leave residual motion in the next shot.
        private static BallState[] SettleBalls(SimulationFrame frame)
        {
            var balls = new BallState[frame.Balls.Count];
            for (int i = 0; i < balls.Length; i++)
            {
                BallState b = frame.Balls[i];
                balls[i] = b.IsPocketed ? b : BallState.AtRest(b.Id, b.Position);
            }

            return balls;
        }

        private static void Respot(BallState[] balls, int id, TableGeometry table, PhysicsConfig config)
        {
            int index = Array.FindIndex(balls, b => b.Id == id);
            Vec2 spot = EightBallRack.FindFreeSpot(EightBallRack.FootSpot(table), +1, balls, id, table, config);
            balls[index] = BallState.AtRest(id, spot);
        }

        private static int FirstObjectBall(List<int> pocketed)
        {
            foreach (int id in pocketed)
            {
                if (EightBallSet.GroupOf(id) != BallGroup.None) return id;
            }

            return -1;
        }

        private static bool HasNonCueBall(List<int> pocketed)
        {
            foreach (int id in pocketed)
            {
                if (id != EightBallSet.CueBall) return true;
            }

            return false;
        }

        private static bool PocketedOwn(List<int> pocketed, BallGroup group)
        {
            foreach (int id in pocketed)
            {
                if (group != BallGroup.None && EightBallSet.GroupOf(id) == group) return true;
            }

            return false;
        }
    }
}
