using BilliardGame.Physics;
using NUnit.Framework;

namespace BilliardGame.Gameplay.Tests
{
    public sealed class AimPredictorTests
    {
        private static readonly TableGeometry Table = TableGeometry.CreateStandard();
        private static readonly PhysicsConfig Config = PhysicsConfig.Default;
        private static readonly double R = Config.BallRadius;

        [Test]
        public void StraightAtBall_GhostTouchesBallAndObjectGoesStraight()
        {
            var balls = new[] { BallState.AtRest(0, new Vec2(-0.5, 0.0)), BallState.AtRest(3, new Vec2(0.0, 0.0)) };

            AimPrediction p = AimPredictor.Predict(balls[0].Position, Vec2.UnitX, balls, 0, Table, Config);

            Assert.That(p.HitBallId, Is.EqualTo(3));
            Assert.AreEqual(-2.0 * R, p.GhostPosition.X, 1e-9);
            Assert.AreEqual(1.0, p.ObjectBallDirection.X, 1e-9);
            Assert.That(p.CueBallDirection, Is.EqualTo(Vec2.Zero));
        }

        [Test]
        public void CutShot_CueDeflectsAtRightAngleToObjectBall()
        {
            var balls = new[] { BallState.AtRest(0, new Vec2(-0.5, 0.0)), BallState.AtRest(3, new Vec2(0.0, R)) };

            AimPrediction p = AimPredictor.Predict(balls[0].Position, Vec2.UnitX, balls, 0, Table, Config);

            Assert.That(p.HitBallId, Is.EqualTo(3));
            Assert.AreEqual(2.0 * R, Vec2.Distance(p.GhostPosition, balls[1].Position), 1e-9);
            Assert.AreEqual(0.0, Vec2.Dot(p.ObjectBallDirection, p.CueBallDirection), 1e-9);
        }

        [Test]
        public void EmptyLine_StopsAtCushion()
        {
            var balls = new[] { BallState.AtRest(0, new Vec2(0.0, 0.0)) };

            AimPrediction p = AimPredictor.Predict(balls[0].Position, Vec2.UnitX, balls, 0, Table, Config);

            Assert.That(p.HitsBall, Is.False);
            Assert.AreEqual(Table.HalfLength - R, p.GhostPosition.X, 1e-9);
        }
    }
}
