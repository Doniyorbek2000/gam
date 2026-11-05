using System;
using System.Collections.Generic;
using BilliardGame.Physics;

namespace BilliardGame.Gameplay
{
    /// <summary>Immutable snapshot of an 8-ball game between player 0 and player 1 (hot-seat or online).</summary>
    public sealed class EightBallState
    {
        public const int NoWinner = -1;

        private readonly BallState[] balls;

        public EightBallState(
            IReadOnlyList<BallState> balls,
            int currentPlayer,
            BallGroup player0Group,
            BallGroup player1Group,
            bool isBreakShot,
            bool ballInHand,
            bool ballInHandKitchenOnly,
            int winner)
        {
            if (balls == null) throw new ArgumentNullException(nameof(balls));
            if (currentPlayer != 0 && currentPlayer != 1) throw new ArgumentOutOfRangeException(nameof(currentPlayer));
            if (winner < NoWinner || winner > 1) throw new ArgumentOutOfRangeException(nameof(winner));
            if (player1Group != EightBallSet.Opposite(player0Group))
            {
                throw new ArgumentException("Players must both be open or hold opposite groups.");
            }

            this.balls = new BallState[balls.Count];
            for (int i = 0; i < this.balls.Length; i++) this.balls[i] = balls[i];
            if (Array.FindIndex(this.balls, b => b.Id == EightBallSet.CueBall) < 0)
            {
                throw new ArgumentException("The cue ball must be part of the state.", nameof(balls));
            }

            Balls = Array.AsReadOnly(this.balls);
            CurrentPlayer = currentPlayer;
            Player0Group = player0Group;
            Player1Group = player1Group;
            IsBreakShot = isBreakShot;
            BallInHand = ballInHand;
            BallInHandKitchenOnly = ballInHand && ballInHandKitchenOnly;
            Winner = winner;
        }

        public IReadOnlyList<BallState> Balls { get; }
        public int CurrentPlayer { get; }
        public int Opponent => 1 - CurrentPlayer;
        public BallGroup Player0Group { get; }
        public BallGroup Player1Group { get; }
        public bool IsBreakShot { get; }

        /// <summary>The current player must place the cue ball before shooting.</summary>
        public bool BallInHand { get; }

        /// <summary>Ball in hand is restricted to the kitchen (foul on the break).</summary>
        public bool BallInHandKitchenOnly { get; }

        public int Winner { get; }
        public bool IsGameOver => Winner != NoWinner;
        public bool IsTableOpen => Player0Group == BallGroup.None;

        public static EightBallState NewGame(TableGeometry table, PhysicsConfig config, int startingPlayer = 0, int rackSeed = 0) =>
            new EightBallState(EightBallRack.Create(table, config, rackSeed), startingPlayer, BallGroup.None, BallGroup.None,
                isBreakShot: true, ballInHand: false, ballInHandKitchenOnly: false, winner: NoWinner);

        public BallGroup GroupOf(int player) => player == 0 ? Player0Group : Player1Group;

        public BallState GetBall(int id)
        {
            for (int i = 0; i < balls.Length; i++)
            {
                if (balls[i].Id == id) return balls[i];
            }

            throw new KeyNotFoundException($"Ball {id} is not in this state.");
        }

        public int RemainingInGroup(BallGroup group)
        {
            if (group == BallGroup.None) return 0;
            int count = 0;
            for (int i = 0; i < balls.Length; i++)
            {
                if (!balls[i].IsPocketed && EightBallSet.GroupOf(balls[i].Id) == group) count++;
            }

            return count;
        }

        /// <summary>The player has a group and has cleared it, so the 8 is their legal target.</summary>
        public bool IsOnEightBall(int player)
        {
            BallGroup group = GroupOf(player);
            return group != BallGroup.None && RemainingInGroup(group) == 0;
        }

        public bool CanPlaceCueBall(Vec2 position, TableGeometry table, PhysicsConfig config) =>
            BallInHand &&
            (!BallInHandKitchenOnly || position.X <= EightBallRack.HeadStringX(table)) &&
            EightBallRack.IsFreeSpot(position, balls, EightBallSet.CueBall, table, config);

        public EightBallState WithCueBallPlaced(Vec2 position, TableGeometry table, PhysicsConfig config)
        {
            if (!CanPlaceCueBall(position, table, config))
            {
                throw new InvalidOperationException($"Cue ball cannot be placed at {position}.");
            }

            var next = (BallState[])balls.Clone();
            int cue = Array.FindIndex(next, b => b.Id == EightBallSet.CueBall);
            next[cue] = BallState.AtRest(EightBallSet.CueBall, position);
            return new EightBallState(next, CurrentPlayer, Player0Group, Player1Group, IsBreakShot,
                ballInHand: false, ballInHandKitchenOnly: false, winner: Winner);
        }
    }
}
