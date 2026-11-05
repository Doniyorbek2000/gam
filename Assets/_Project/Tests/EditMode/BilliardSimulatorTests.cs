using System;
using System.Linq;
using NUnit.Framework;

namespace BilliardGame.Physics.Tests
{
    public sealed class BilliardSimulatorTests
    {
        private static readonly PhysicsConfig Config = PhysicsConfig.Default;
        private static readonly double R = Config.BallRadius;
        private static readonly double RailY = TableGeometry.StandardWidth * 0.5 - R;

        private static BilliardSimulator CreateSimulator(PhysicsConfig config = null) =>
            new BilliardSimulator(TableGeometry.CreateStandard(), config ?? Config);

        private static BallState Rolling(int id, Vec2 position, Vec2 velocity) =>
            new BallState(id, position, velocity, new Vec3(-velocity.Y / R, velocity.X / R, 0.0));

        private static BallState Sliding(int id, Vec2 position, Vec2 velocity) =>
            new BallState(id, position, velocity, Vec3.Zero);

        private static SimulationFrame FirstFrameAfter(SimulationResult result, double time) =>
            result.Frames.First(f => f.Time > time);

        // ---- Straight shots -------------------------------------------------------------

        [Test]
        public void CueStrike_NaturalRollOffset_LeavesBallRolling()
        {
            var shot = new ShotParams(Vec2.UnitX, 2.0, topSpin: ShotParams.NaturalRollTopSpin);

            BallState ball = BallMotion.ApplyCueStrike(BallState.AtRest(0, Vec2.Zero), shot, Config);

            Assert.That(BallMotion.IsRolling(ball, R), Is.True);
        }

        [Test]
        public void StraightShot_NaturalRoll_StopsAtRollingResistanceDistance()
        {
            var start = new Vec2(-1.0, 0.0);
            const double speed = 0.5;
            var shot = new ShotParams(Vec2.UnitX, speed, topSpin: ShotParams.NaturalRollTopSpin);

            SimulationResult result = CreateSimulator().Simulate(new[] { BallState.AtRest(0, start) }, shot);

            double expected = speed * speed / (2.0 * Config.RollingFriction * Config.Gravity);
            BallState final = result.FinalFrame.GetBall(0);
            Assert.That(result.EndedAtRest, Is.True);
            Assert.That(result.Events, Is.Empty);
            Assert.AreEqual(expected, final.Position.X - start.X, 2e-3);
            Assert.AreEqual(0.0, final.Position.Y, 1e-12);
        }

        [Test]
        public void StraightShot_HeadOn_DrivesObjectBallAlongTheLine()
        {
            var cue = BallState.AtRest(0, new Vec2(-0.6, 0.0));
            var target = BallState.AtRest(1, new Vec2(-0.3, 0.0));
            var shot = new ShotParams(Vec2.UnitX, 1.0);

            SimulationResult result = CreateSimulator().Simulate(new[] { cue, target }, shot);

            SimulationEvent first = result.Events.First();
            Assert.AreEqual(SimulationEventType.BallCollision, first.Type);
            Assert.That(first.Involves(0) && first.Involves(1), Is.True);

            SimulationFrame after = FirstFrameAfter(result, first.Time);
            Assert.That(after.GetBall(1).Velocity.X, Is.GreaterThan(0.9 * first.Speed), "Object ball takes almost all the speed.");
            Assert.That(Math.Abs(after.GetBall(0).Velocity.X), Is.LessThan(0.1 * first.Speed), "Cue ball nearly stops (stun).");

            Assert.That(result.EndedAtRest, Is.True);
            foreach (BallState ball in result.FinalFrame.Balls)
            {
                Assert.AreEqual(0.0, ball.Position.Y, 1e-9, $"Ball {ball.Id} left the line of the shot.");
            }
        }

        [Test]
        public void StraightShot_Draw_PullsCueBallBackAfterImpact()
        {
            var cue = BallState.AtRest(0, new Vec2(-0.8, 0.0));
            var target = BallState.AtRest(1, new Vec2(-0.4, 0.0));
            var shot = new ShotParams(Vec2.UnitX, 2.0, topSpin: -0.4);

            SimulationResult result = CreateSimulator().Simulate(new[] { cue, target }, shot);

            SimulationEvent impact = result.Events.First();
            Assert.AreEqual(SimulationEventType.BallCollision, impact.Type);
            double nextCueEventTime = result.Events.Skip(1).Where(e => e.Involves(0)).Select(e => e.Time).DefaultIfEmpty(double.MaxValue).First();
            double impactX = FirstFrameAfter(result, impact.Time).GetBall(0).Position.X;
            double minX = result.Frames
                .Where(f => f.Time > impact.Time && f.Time < nextCueEventTime)
                .Min(f => f.GetBall(0).Position.X);
            Assert.That(minX, Is.LessThan(impactX - 0.1), "Back spin should draw the cue ball back.");
        }

        // ---- Angled collision -----------------------------------------------------------

        [Test]
        public void CutShot_ThirtyDegrees_ObjectBallTravelsAlongLineOfCentres()
        {
            double cut = Math.PI / 6.0;
            var cue = Sliding(0, new Vec2(-0.6, 0.0), new Vec2(1.5, 0.0));
            // Offset of R between the cue path and the object ball centre gives a 30 degree cut.
            var target = BallState.AtRest(1, new Vec2(-0.3, R));

            SimulationResult result = CreateSimulator(new PhysicsConfig(ballFriction: 0.0)).Run(new[] { cue, target });

            SimulationEvent impact = result.Events.First();
            Assert.AreEqual(SimulationEventType.BallCollision, impact.Type);
            BallState objectBall = FirstFrameAfter(result, impact.Time).GetBall(1);
            Assert.AreEqual(cut, objectBall.Velocity.Angle, 1e-9);
        }

        [Test]
        public void CutShot_WithBallFriction_ConservesMomentumAtImpact()
        {
            var cue = Sliding(0, new Vec2(-0.6, 0.0), new Vec2(1.5, 0.0));
            var target = BallState.AtRest(1, new Vec2(-0.3, R));

            SimulationResult result = CreateSimulator().Run(new[] { cue, target });

            SimulationEvent impact = result.Events.First();
            SimulationFrame after = FirstFrameAfter(result, impact.Time);
            SimulationFrame before = result.Frames.Last(f => f.Time < impact.Time);
            Vec2 momentumBefore = before.GetBall(0).Velocity + before.GetBall(1).Velocity;
            Vec2 momentumAfter = after.GetBall(0).Velocity + after.GetBall(1).Velocity;
            // Only cloth friction acts between the two frames (at most one step of sliding deceleration).
            Assert.AreEqual(0.0, (momentumAfter - momentumBefore).Length, 2.0 * Config.SlidingFriction * Config.Gravity * Config.TimeStep);
            Assert.That(after.GetBall(1).Velocity.Angle, Is.LessThan(Math.PI / 6.0), "Contact friction throws the object ball forward.");
        }

        // ---- Cushions -------------------------------------------------------------------

        [Test]
        public void Cushion_PerpendicularHit_BouncesStraightBack()
        {
            var ball = Sliding(0, new Vec2(-0.6, 0.3), new Vec2(0.0, 1.5));

            SimulationResult result = CreateSimulator().Run(new[] { ball });

            SimulationEvent hit = result.Events.First();
            Assert.AreEqual(SimulationEventType.CushionCollision, hit.Type);
            Assert.AreEqual(TableGeometry.TopLeftCushion, hit.CushionIndex);

            BallState after = FirstFrameAfter(result, hit.Time).GetBall(0);
            Assert.AreEqual(-Config.CushionRestitution * hit.Speed, after.Velocity.Y, 0.01);
            Assert.AreEqual(-0.6, result.FinalFrame.GetBall(0).Position.X, 1e-12);
            Assert.That(result.FinalFrame.GetBall(0).Position.Y, Is.LessThan(RailY));
        }

        [Test]
        public void Cushion_AngledHit_ReflectsAndLosesTangentialSpeedToFriction()
        {
            var ball = Sliding(0, new Vec2(-0.6, 0.3), new Vec2(0.5, 1.0));

            SimulationResult result = CreateSimulator().Run(new[] { ball });

            SimulationEvent hit = result.Events.First();
            Assert.AreEqual(SimulationEventType.CushionCollision, hit.Type);
            BallState before = result.Frames.Last(f => f.Time < hit.Time).GetBall(0);
            BallState after = FirstFrameAfter(result, hit.Time).GetBall(0);
            Assert.That(after.Velocity.Y, Is.LessThan(0.0));
            Assert.That(after.Velocity.X, Is.GreaterThan(0.0).And.LessThan(before.Velocity.X));
        }

        // ---- Pockets --------------------------------------------------------------------

        [Test]
        public void Pocket_ShotAtCorner_IsPocketed()
        {
            var ball = Sliding(0, new Vec2(0.77, 0.135), new Vec2(1.0, 1.0));

            SimulationResult result = CreateSimulator().Run(new[] { ball });

            SimulationEvent pocketed = result.Events.Single(e => e.Type == SimulationEventType.BallPocketed);
            Assert.AreEqual(TableGeometry.TopRightPocket, pocketed.PocketIndex);
            Assert.That(result.Events.Any(e => e.Type == SimulationEventType.CushionCollision), Is.False, "Ball should not touch the jaws.");
            BallState final = result.FinalFrame.GetBall(0);
            Assert.That(final.IsPocketed, Is.True);
            Assert.AreEqual(TableGeometry.TopRightPocket, final.PocketIndex);
            Assert.That(result.EndedAtRest, Is.True);
        }

        [Test]
        public void Pocket_ShotAtSidePocket_IsPocketed()
        {
            var ball = Sliding(0, Vec2.Zero, new Vec2(0.0, 1.2));

            SimulationResult result = CreateSimulator().Run(new[] { ball });

            BallState final = result.FinalFrame.GetBall(0);
            Assert.That(final.IsPocketed, Is.True);
            Assert.AreEqual(TableGeometry.TopSidePocket, final.PocketIndex);
        }

        [Test]
        public void Pocket_SlowBall_StopsShortOfCorner()
        {
            var ball = Rolling(0, new Vec2(0.77, 0.135), new Vec2(0.3, 0.3) / Math.Sqrt(2.0));

            SimulationResult result = CreateSimulator().Run(new[] { ball });

            Assert.That(result.EndedAtRest, Is.True);
            Assert.That(result.FinalFrame.GetBall(0).IsPocketed, Is.False);
        }

        [Test]
        public void Pocket_BallFrozenOnRail_RollsPastSidePocketIntoCorner()
        {
            var ball = Rolling(0, new Vec2(-0.4, RailY), new Vec2(1.0, 0.0));

            SimulationResult result = CreateSimulator().Run(new[] { ball });

            SimulationEvent pocketed = result.Events.Single(e => e.Type == SimulationEventType.BallPocketed);
            Assert.AreEqual(TableGeometry.TopRightPocket, pocketed.PocketIndex);
        }

        // ---- Input validation -----------------------------------------------------------

        [Test]
        public void ShotParams_TipOffsetBeyondMiscueLimit_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ShotParams(Vec2.UnitX, 1.0, sideSpin: 0.4, topSpin: 0.4));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ShotParams(Vec2.UnitX, 0.0));
            Assert.Throws<ArgumentException>(() => new ShotParams(Vec2.Zero, 1.0));
        }

        [Test]
        public void Simulate_UnknownCueBallOrDuplicateIds_Throws()
        {
            var shot = new ShotParams(Vec2.UnitX, 1.0, cueBallId: 7);
            BilliardSimulator simulator = CreateSimulator();

            Assert.Throws<ArgumentException>(() => simulator.Simulate(new[] { BallState.AtRest(0, Vec2.Zero) }, shot));
            Assert.Throws<ArgumentException>(() => simulator.Run(new[] { BallState.AtRest(1, Vec2.Zero), BallState.AtRest(1, Vec2.UnitX) }));
        }
    }
}
