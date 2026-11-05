using System.Collections.Generic;
using System.Linq;
using BilliardGame.Physics;
using NUnit.Framework;

namespace BilliardGame.Gameplay.Tests
{
    public sealed class EightBallRulesTests
    {
        private static readonly TableGeometry Table = TableGeometry.CreateStandard();
        private static readonly PhysicsConfig Config = PhysicsConfig.Default;
        private const int Cue = EightBallSet.CueBall;
        private const int Eight = EightBallSet.EightBall;

        // ---- Helpers ----------------------------------------------------------------------

        private static EightBallState Racked() => EightBallState.NewGame(Table, Config);

        /// <summary>Open table after the break, player 0 to shoot.</summary>
        private static EightBallState OpenTable() =>
            new EightBallState(Racked().Balls, 0, BallGroup.None, BallGroup.None, false, false, false, EightBallState.NoWinner);

        private static EightBallState WithGroups(BallGroup player0Group, params int[] alreadyPocketed)
        {
            var balls = Racked().Balls.Select(b => alreadyPocketed.Contains(b.Id) ? b.AsPocketed(0) : b).ToArray();
            return new EightBallState(balls, 0, player0Group, EightBallSet.Opposite(player0Group), false, false, false, EightBallState.NoWinner);
        }

        private static readonly int[] AllSolids = { 1, 2, 3, 4, 5, 6, 7 };

        private static SimulationFrame FinalFrame(EightBallState state, params int[] pocketed) =>
            new SimulationFrame(5.0, state.Balls.Select(b => pocketed.Contains(b.Id) ? b.AsPocketed(0) : b));

        private static SimulationEvent Hit(double t, int other) => SimulationEvent.BallCollision(t, Cue, other, 2.0);
        private static SimulationEvent Rail(double t, int ball) => SimulationEvent.CushionCollision(t, ball, 0, 1.0);
        private static SimulationEvent Pot(double t, int ball) => SimulationEvent.BallPocketed(t, ball, 0, 1.0);

        private static ShotOutcome Resolve(EightBallState state, IReadOnlyList<SimulationEvent> events, params int[] pocketed) =>
            EightBallRules.ResolveShot(state, events, FinalFrame(state, pocketed), Table, Config);

        // ---- Rack -------------------------------------------------------------------------

        [Test]
        public void Rack_HasSixteenNonOverlappingBallsWithEightInTheMiddle()
        {
            BallState[] balls = EightBallRack.Create(Table, Config);

            Assert.That(balls.Select(b => b.Id), Is.EqualTo(Enumerable.Range(0, 16)));
            for (int i = 0; i < balls.Length; i++)
            for (int j = i + 1; j < balls.Length; j++)
            {
                // Tight rack: jitter may press balls together by a fraction of a millimetre, never more.
                double maxOverlap = 2.0 * System.Math.Sqrt(2.0) * EightBallRack.RackJitter;
                Assert.That(Vec2.Distance(balls[i].Position, balls[j].Position), Is.GreaterThanOrEqualTo(2.0 * Config.BallRadius - maxOverlap));
            }

            double rowStep = 2.0 * Config.BallRadius * System.Math.Sqrt(3.0) * 0.5;
            Assert.AreEqual(EightBallRack.FootSpot(Table).X + 2.0 * rowStep, balls[Eight].Position.X, 2.0 * EightBallRack.RackJitter);
            Assert.AreEqual(0.0, balls[Eight].Position.Y, 2.0 * EightBallRack.RackJitter);
            Assert.That(balls[Cue].Position.X, Is.LessThanOrEqualTo(EightBallRack.HeadStringX(Table)));
        }

        [Test]
        public void Rack_BackCornersHoldOneSolidAndOneStripe()
        {
            BallState[] balls = EightBallRack.Create(Table, Config);
            double backX = balls.Max(b => b.Position.X);

            var corners = balls.Where(b => System.Math.Abs(b.Position.X - backX) < 0.01)
                .OrderBy(b => b.Position.Y).ToArray();

            Assert.That(corners.Length, Is.EqualTo(5));
            Assert.That(EightBallSet.GroupOf(corners[0].Id), Is.Not.EqualTo(EightBallSet.GroupOf(corners[4].Id)));
        }

        [Test]
        public void Rack_SameSeedIsReproducible_DifferentSeedDiffers()
        {
            BallState[] a = EightBallRack.Create(Table, Config, 7);
            BallState[] b = EightBallRack.Create(Table, Config, 7);
            BallState[] c = EightBallRack.Create(Table, Config, 8);

            Assert.That(a.Select(x => x.Position), Is.EqualTo(b.Select(x => x.Position)));
            Assert.That(a.Select(x => x.Position), Is.Not.EqualTo(c.Select(x => x.Position)));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void HardBreak_SpreadsMostOfTheRack(int seed)
        {
            BallState[] rack = EightBallRack.Create(Table, Config, seed);
            var simulator = new BilliardSimulator(Table, Config);

            SimulationResult result = simulator.Simulate(rack, new ShotParams(Vec2.UnitX, 10.0));

            int spread = result.FinalFrame.Balls.Count(b =>
                b.Id != Cue && (b.IsPocketed || Vec2.Distance(b.Position, rack[b.Id].Position) > 0.3));
            Assert.That(spread, Is.GreaterThanOrEqualTo(10), "A 10 m/s break should scatter at least 10 of 15 object balls.");
        }

        // ---- Break ------------------------------------------------------------------------

        [Test]
        public void Break_PocketingABall_KeepsTurnAndLeavesTableOpen()
        {
            EightBallState state = Racked();
            var events = new[] { Hit(0.1, 1), Pot(0.5, 3) };

            ShotOutcome outcome = Resolve(state, events, 3);

            Assert.That(outcome.Foul, Is.EqualTo(FoulType.None));
            Assert.That(outcome.TurnPassed, Is.False);
            Assert.That(outcome.NextState.CurrentPlayer, Is.EqualTo(0));
            Assert.That(outcome.NextState.IsTableOpen, Is.True);
            Assert.That(outcome.NextState.IsBreakShot, Is.False);
        }

        [Test]
        public void Break_FewerThanFourBallsToCushion_IsIllegalBreak()
        {
            EightBallState state = Racked();
            var events = new[] { Hit(0.1, 1), Rail(0.4, 2), Rail(0.5, 3), Rail(0.6, 4) };

            ShotOutcome outcome = Resolve(state, events);

            Assert.That(outcome.Foul, Is.EqualTo(FoulType.IllegalBreak));
            Assert.That(outcome.NextState.CurrentPlayer, Is.EqualTo(1));
            Assert.That(outcome.NextState.BallInHand, Is.True);
        }

        [Test]
        public void Break_FourBallsToCushion_IsLegalAndPassesTurn()
        {
            EightBallState state = Racked();
            var events = new[] { Hit(0.1, 1), Rail(0.4, 2), Rail(0.5, 3), Rail(0.6, 4), Rail(0.7, 9) };

            ShotOutcome outcome = Resolve(state, events);

            Assert.That(outcome.Foul, Is.EqualTo(FoulType.None));
            Assert.That(outcome.NextState.CurrentPlayer, Is.EqualTo(1));
            Assert.That(outcome.NextState.BallInHand, Is.False);
        }

        [Test]
        public void Break_Scratch_GivesOpponentBallInHandInKitchen()
        {
            EightBallState state = Racked();
            var events = new[] { Hit(0.1, 1), Pot(0.5, 9), Pot(0.9, Cue) };

            ShotOutcome outcome = Resolve(state, events, 9, Cue);

            Assert.That(outcome.Foul, Is.EqualTo(FoulType.Scratch));
            Assert.That(outcome.NextState.CurrentPlayer, Is.EqualTo(1));
            Assert.That(outcome.NextState.BallInHandKitchenOnly, Is.True);
            Assert.That(outcome.NextState.CanPlaceCueBall(new Vec2(0.3, 0.0), Table, Config), Is.False);
            Assert.That(outcome.NextState.CanPlaceCueBall(new Vec2(-0.9, 0.2), Table, Config), Is.True);
        }

        [Test]
        public void Break_EightPocketed_IsRespottedAndGameContinues()
        {
            EightBallState state = Racked();
            var events = new[] { Hit(0.1, 1), Pot(0.6, Eight) };

            ShotOutcome outcome = Resolve(state, events, Eight);

            BallState eight = outcome.NextState.GetBall(Eight);
            Assert.That(outcome.EightBallRespotted, Is.True);
            Assert.That(outcome.NextState.IsGameOver, Is.False);
            Assert.That(eight.IsPocketed, Is.False);
            Assert.AreEqual(0.0, eight.Position.Y, 1e-12);
            Assert.That(EightBallRack.IsFreeSpot(eight.Position, outcome.NextState.Balls, Eight, Table, Config), Is.True);
        }

        // ---- Groups -----------------------------------------------------------------------

        [Test]
        public void OpenTable_PocketingStripe_AssignsStripesToShooter()
        {
            EightBallState state = OpenTable();
            var events = new[] { Hit(0.1, 12), Pot(0.8, 12) };

            ShotOutcome outcome = Resolve(state, events, 12);

            Assert.That(outcome.GroupsAssigned, Is.True);
            Assert.That(outcome.NextState.Player0Group, Is.EqualTo(BallGroup.Stripes));
            Assert.That(outcome.NextState.Player1Group, Is.EqualTo(BallGroup.Solids));
            Assert.That(outcome.NextState.CurrentPlayer, Is.EqualTo(0));
        }

        [Test]
        public void OpenTable_HittingEightFirst_IsFoul()
        {
            EightBallState state = OpenTable();
            var events = new[] { Hit(0.1, Eight), Rail(0.5, Eight) };

            ShotOutcome outcome = Resolve(state, events);

            Assert.That(outcome.Foul, Is.EqualTo(FoulType.WrongBallFirst));
        }

        // ---- Fouls ------------------------------------------------------------------------

        [Test]
        public void HittingOpponentBallFirst_IsFoulWithBallInHandAnywhere()
        {
            EightBallState state = WithGroups(BallGroup.Solids);
            var events = new[] { Hit(0.1, 10), Rail(0.5, 10) };

            ShotOutcome outcome = Resolve(state, events);

            Assert.That(outcome.Foul, Is.EqualTo(FoulType.WrongBallFirst));
            Assert.That(outcome.NextState.CurrentPlayer, Is.EqualTo(1));
            Assert.That(outcome.NextState.BallInHand, Is.True);
            Assert.That(outcome.NextState.BallInHandKitchenOnly, Is.False);
        }

        [Test]
        public void MissingEveryBall_IsFoul()
        {
            ShotOutcome outcome = Resolve(WithGroups(BallGroup.Solids), new[] { Rail(0.4, Cue) });

            Assert.That(outcome.Foul, Is.EqualTo(FoulType.NoBallHit));
        }

        [Test]
        public void NoCushionAfterContact_IsFoul()
        {
            var events = new[] { Rail(0.05, Cue), Hit(0.1, 3) };

            ShotOutcome outcome = Resolve(WithGroups(BallGroup.Solids), events);

            Assert.That(outcome.Foul, Is.EqualTo(FoulType.NoRailAfterContact));
        }

        [Test]
        public void Scratch_GivesBallInHand_AndPlacementIsValidated()
        {
            EightBallState state = WithGroups(BallGroup.Solids);
            var events = new[] { Hit(0.1, 3), Pot(0.6, 3), Pot(0.9, Cue) };

            ShotOutcome outcome = Resolve(state, events, 3, Cue);
            EightBallState next = outcome.NextState;
            Vec2 onTopOfBall = next.GetBall(5).Position;

            Assert.That(outcome.Foul, Is.EqualTo(FoulType.Scratch));
            Assert.That(next.CurrentPlayer, Is.EqualTo(1));
            Assert.That(next.GetBall(Cue).IsPocketed, Is.True);
            Assert.That(next.CanPlaceCueBall(onTopOfBall, Table, Config), Is.False);
            Assert.Throws<System.InvalidOperationException>(() => next.WithCueBallPlaced(onTopOfBall, Table, Config));

            EightBallState placed = next.WithCueBallPlaced(new Vec2(0.3, 0.2), Table, Config);
            Assert.That(placed.BallInHand, Is.False);
            Assert.That(placed.GetBall(Cue).IsPocketed, Is.False);
            Assert.That(placed.GetBall(Cue).Position, Is.EqualTo(new Vec2(0.3, 0.2)));
        }

        [Test]
        public void LegalMiss_PassesTurnWithoutFoul()
        {
            var events = new[] { Hit(0.1, 3), Rail(0.5, 3) };

            ShotOutcome outcome = Resolve(WithGroups(BallGroup.Solids), events);

            Assert.That(outcome.Foul, Is.EqualTo(FoulType.None));
            Assert.That(outcome.NextState.CurrentPlayer, Is.EqualTo(1));
            Assert.That(outcome.NextState.BallInHand, Is.False);
        }

        [Test]
        public void PocketingOnlyOpponentBall_PassesTurn()
        {
            var events = new[] { Hit(0.1, 3), Pot(0.5, 11) };

            ShotOutcome outcome = Resolve(WithGroups(BallGroup.Solids), events, 11);

            Assert.That(outcome.Foul, Is.EqualTo(FoulType.None));
            Assert.That(outcome.NextState.CurrentPlayer, Is.EqualTo(1));
        }

        // ---- Eight ball and winner --------------------------------------------------------

        [Test]
        public void EightPocketedEarly_Loses()
        {
            var events = new[] { Hit(0.1, 3), Pot(0.7, Eight) };

            ShotOutcome outcome = Resolve(WithGroups(BallGroup.Solids), events, Eight);

            Assert.That(outcome.NextState.IsGameOver, Is.True);
            Assert.That(outcome.NextState.Winner, Is.EqualTo(1));
        }

        [Test]
        public void OnEight_OnlyTheEightIsALegalFirstHit()
        {
            EightBallState state = WithGroups(BallGroup.Solids, AllSolids);

            Assert.That(state.IsOnEightBall(0), Is.True);
            Assert.That(EightBallRules.IsLegalFirstHit(10, state.Player0Group, onEight: true), Is.False);
            Assert.That(EightBallRules.IsLegalFirstHit(Eight, state.Player0Group, onEight: true), Is.True);
        }

        [Test]
        public void EightPocketedLegallyWhenOnEight_Wins()
        {
            EightBallState state = WithGroups(BallGroup.Solids, AllSolids);
            var events = new[] { Hit(0.1, Eight), Pot(0.7, Eight) };

            ShotOutcome outcome = Resolve(state, events, Eight);

            Assert.That(outcome.Foul, Is.EqualTo(FoulType.None));
            Assert.That(outcome.NextState.Winner, Is.EqualTo(0));
            Assert.That(outcome.NextState.BallInHand, Is.False);
        }

        [Test]
        public void EightPocketedWithScratchWhenOnEight_Loses()
        {
            EightBallState state = WithGroups(BallGroup.Solids, AllSolids);
            var events = new[] { Hit(0.1, Eight), Pot(0.7, Eight), Pot(0.9, Cue) };

            ShotOutcome outcome = Resolve(state, events, Eight, Cue);

            Assert.That(outcome.Foul, Is.EqualTo(FoulType.Scratch));
            Assert.That(outcome.NextState.Winner, Is.EqualTo(1));
        }

        [Test]
        public void ResolvingAfterGameOver_Throws()
        {
            EightBallState over = new EightBallState(Racked().Balls, 0, BallGroup.Solids, BallGroup.Stripes, false, false, false, 1);

            Assert.Throws<System.InvalidOperationException>(() => Resolve(over, new[] { Hit(0.1, 3) }));
        }

        // ---- Full simulation integration --------------------------------------------------

        [Test]
        public void RealBreak_ThroughSimulator_ProducesConsistentOutcome()
        {
            EightBallState state = Racked();
            var simulator = new BilliardSimulator(Table, Config);

            SimulationResult result = simulator.Simulate(state.Balls, new ShotParams(Vec2.UnitX, 9.0));
            ShotOutcome outcome = EightBallRules.ResolveShot(state, result.Events, result.FinalFrame, Table, Config);

            Assert.That(result.EndedAtRest, Is.True);
            Assert.That(outcome.FirstHitBallId, Is.EqualTo(1));
            Assert.That(outcome.Foul, Is.Not.EqualTo(FoulType.IllegalBreak));
            Assert.That(outcome.NextState.Balls.All(b => b.IsStationary), Is.True);
        }
    }
}
