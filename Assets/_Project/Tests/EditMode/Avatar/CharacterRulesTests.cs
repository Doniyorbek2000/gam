using System.Collections.Generic;
using BilliardGame.Avatar.Rules;
using BilliardGame.Gameplay;
using BilliardGame.Physics;
using BilliardGame.Presentation;
using NUnit.Framework;

namespace BilliardGame.Avatar.Tests
{
    /// <summary>M1.6 character rules: stance, walking route, reactions, stroke timing. All visual-only.</summary>
    public sealed class CharacterRulesTests
    {
        private const double HalfX = 1.47, HalfY = 0.83;
        private static readonly TableGeometry Table = TableGeometry.CreateStandard();
        private static readonly PhysicsConfig Config = PhysicsConfig.Default;

        [Test]
        public void StandPoint_IsBehindTheCueBallAndOutsideTheTable()
        {
            var ball = new Vec2(-0.6, 0.1);

            Vec2 stand = StancePlanner.StandPoint(ball, Vec2.UnitX, HalfX, HalfY);

            Assert.That(stand.X, Is.LessThanOrEqualTo(-HalfX - StancePlanner.TableClearance + 1e-9));
            Assert.AreEqual(ball.Y + StancePlanner.LateralOffset, stand.Y, 1e-9, "hips sit left of the cue line");
        }

        [Test]
        public void StandPoint_KeepsPreferredDistanceWhenTheBallIsNearTheShooter()
        {
            var ball = new Vec2(-1.2, 0.0);

            Vec2 stand = StancePlanner.StandPoint(ball, Vec2.UnitX, HalfX, HalfY);

            Assert.AreEqual(ball.X - StancePlanner.PreferredDistance, stand.X, 1e-9);
        }

        [Test]
        public void NeedsReposition_IgnoresSmallAimChanges()
        {
            Assert.IsFalse(StancePlanner.NeedsReposition(new Vec2(2, 0), new Vec2(2.1, 0.1)));
            Assert.IsTrue(StancePlanner.NeedsReposition(new Vec2(2, 0), new Vec2(2, 0.6)));
        }

        [Test]
        public void Route_GoesStraightWhenTheTableIsNotInTheWay()
        {
            var route = new List<Vec2>();

            TablePath.Route(new Vec2(-2.5, 1.5), new Vec2(2.5, 1.5), HalfX, HalfY, route);

            Assert.That(route, Is.EqualTo(new[] { new Vec2(2.5, 1.5) }));
        }

        [Test]
        public void Route_WalksRoundTheCornersInsteadOfThroughTheTable()
        {
            var route = new List<Vec2>();
            var from = new Vec2(-2.2, 0.0);
            var to = new Vec2(2.2, 0.1);

            TablePath.Route(from, to, HalfX, HalfY, route);

            Assert.That(route.Count, Is.GreaterThan(1));
            Assert.That(route[route.Count - 1], Is.EqualTo(to));
            Vec2 previous = from;
            foreach (Vec2 point in route)
            {
                Assert.IsFalse(TablePath.SegmentHitsBox(previous, point, HalfX, HalfY), $"{previous} -> {point} crosses the table");
                previous = point;
            }
        }

        [Test]
        public void Reactions_FoulDisappointsTheShooterAndNobodyClaps()
        {
            ShotOutcome foul = Outcome(FoulType.Scratch, turnPassed: true, winner: EightBallState.NoWinner, 3);

            Assert.That(Reactions.ForShooter(foul, 0), Is.EqualTo(Reaction.Disappointed));
            Assert.That(Reactions.ForWatcher(foul), Is.EqualTo(Reaction.None));
        }

        [Test]
        public void Reactions_MissDisappointsAndAPotThatKeepsTheTurnEarnsApplause()
        {
            Assert.That(Reactions.ForShooter(Outcome(FoulType.None, true, EightBallState.NoWinner), 0), Is.EqualTo(Reaction.Disappointed));
            ShotOutcome pot = Outcome(FoulType.None, false, EightBallState.NoWinner, 5);
            Assert.That(Reactions.ForShooter(pot, 0), Is.EqualTo(Reaction.None));
            Assert.That(Reactions.ForWatcher(pot), Is.EqualTo(Reaction.Clap));
        }

        [Test]
        public void Reactions_WinnerCelebratesAndLoserIsDisappointed()
        {
            ShotOutcome win = Outcome(FoulType.None, false, 1, 8);

            Assert.That(Reactions.ForShooter(win, 1), Is.EqualTo(Reaction.Celebrate));
            Assert.That(Reactions.ForShooter(win, 0), Is.EqualTo(Reaction.Disappointed));
            Assert.That(Reactions.ForWatcher(win), Is.EqualTo(Reaction.None));
        }

        [TestCase(0f)]
        [TestCase(0.5f)]
        [TestCase(1f)]
        public void Stroke_TipReachesTheBallInsideTheStroke(float power)
        {
            var stroke = new StrokeTimeline(0.8f, power, -0.07f);

            Assert.That(stroke.ContactTime, Is.GreaterThan(0f).And.LessThan(stroke.Duration));
            Assert.AreEqual(stroke.ContactPull, stroke.PullAt(stroke.ContactTime), 1e-4f);
            Assert.That(stroke.PullAt(0f), Is.EqualTo(stroke.StartPull));
        }

        [Test]
        public void Stroke_HarderShotsStrikeSooner()
        {
            Assert.That(new StrokeTimeline(0.8f, 1f, -0.07f).ContactTime, Is.LessThan(new StrokeTimeline(0.8f, 0.1f, -0.07f).ContactTime));
        }

        [Test]
        public void Stroke_TimingNeverChangesTheShot()
        {
            // The shot is fixed when Shoot is pressed; the stroke only delays the start. Same ShotParams, same result.
            EightBallState state = EightBallState.NewGame(Table, Config, 0, 7);
            var shot = new ShotParams(Vec2.UnitX, 8.0, 0.0, 0.0, EightBallSet.CueBall);
            var simulator = new BilliardSimulator(Table, Config);

            SimulationResult a = simulator.Simulate(state.Balls, shot);
            SimulationResult b = simulator.Simulate(state.Balls, shot);

            for (int i = 0; i < a.FinalFrame.Balls.Count; i++) Assert.That(b.FinalFrame.Balls[i].Position, Is.EqualTo(a.FinalFrame.Balls[i].Position));
        }

        private static ShotOutcome Outcome(FoulType foul, bool turnPassed, int winner, params int[] pocketed)
        {
            EightBallState start = EightBallState.NewGame(Table, Config);
            var next = new EightBallState(start.Balls, turnPassed ? 1 : 0, BallGroup.None, BallGroup.None, false, foul != FoulType.None, false, winner);
            return new ShotOutcome(next, foul, 1, pocketed, false, turnPassed, false);
        }
    }
}
