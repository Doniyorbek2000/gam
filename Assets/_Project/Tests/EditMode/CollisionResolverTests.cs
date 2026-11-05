using System;
using NUnit.Framework;

namespace BilliardGame.Physics.Tests
{
    public sealed class CollisionResolverTests
    {
        private const double Tolerance = 1e-12;
        private static readonly PhysicsConfig Frictionless = new PhysicsConfig(ballRestitution: 1.0, ballFriction: 0.0, cushionFriction: 0.0);
        private static readonly double R = PhysicsConfig.Default.BallRadius;

        [Test]
        public void BallBall_HeadOnElastic_TransfersAllVelocity()
        {
            var cue = new BallState(0, Vec2.Zero, new Vec2(1.0, 0.0), Vec3.Zero);
            var target = BallState.AtRest(1, new Vec2(2.0 * R, 0.0));

            var (a, b) = CollisionResolver.ResolveBallBall(cue, target, Frictionless);

            Assert.AreEqual(0.0, a.Velocity.Length, Tolerance);
            Assert.AreEqual(1.0, b.Velocity.X, Tolerance);
            Assert.AreEqual(0.0, b.Velocity.Y, Tolerance);
        }

        [Test]
        public void BallBall_ThirtyDegreeCut_ObjectBallLeavesAlongLineOfCentres()
        {
            double cut = Math.PI / 6.0;
            var cue = new BallState(0, Vec2.Zero, new Vec2(1.0, 0.0), Vec3.Zero);
            var target = BallState.AtRest(1, Vec2.FromAngle(cut) * (2.0 * R));

            var (a, b) = CollisionResolver.ResolveBallBall(cue, target, Frictionless);

            Assert.AreEqual(cut, b.Velocity.Angle, 1e-12);
            Assert.AreEqual(Math.Cos(cut), b.Velocity.Length, Tolerance);
            Assert.AreEqual(0.0, Vec2.Dot(a.Velocity, b.Velocity), Tolerance, "Elastic equal-mass balls separate at 90 degrees.");
            Assert.AreEqual(1.0, (a.Velocity + b.Velocity).X, Tolerance, "Momentum is conserved.");
        }

        [Test]
        public void BallBall_WithFriction_ThrowsObjectBallTowardsCueDirection()
        {
            double cut = Math.PI / 6.0;
            var cue = new BallState(0, Vec2.Zero, new Vec2(1.0, 0.0), Vec3.Zero);
            var target = BallState.AtRest(1, Vec2.FromAngle(cut) * (2.0 * R));

            var (a, b) = CollisionResolver.ResolveBallBall(cue, target, PhysicsConfig.Default);

            Vec2 momentum = a.Velocity + b.Velocity;
            Assert.AreEqual(1.0, momentum.X, Tolerance);
            Assert.AreEqual(0.0, momentum.Y, Tolerance);
            Assert.That(b.Velocity.Angle, Is.LessThan(cut).And.GreaterThan(cut - 5.0 * Math.PI / 180.0));
            Assert.That(a.Velocity.LengthSquared + b.Velocity.LengthSquared, Is.LessThan(1.0), "Energy is lost, not created.");
        }

        [Test]
        public void BallBall_Separating_IsUnchanged()
        {
            var a = new BallState(0, Vec2.Zero, new Vec2(-1.0, 0.0), Vec3.Zero);
            var b = BallState.AtRest(1, new Vec2(2.0 * R, 0.0));

            var (ra, rb) = CollisionResolver.ResolveBallBall(a, b, PhysicsConfig.Default);

            Assert.AreEqual(a.Velocity, ra.Velocity);
            Assert.AreEqual(b.Velocity, rb.Velocity);
        }

        [Test]
        public void Cushion_Perpendicular_ReflectsWithRestitution()
        {
            var ball = new BallState(0, Vec2.Zero, new Vec2(0.0, 2.0), Vec3.Zero);

            BallState result = CollisionResolver.ResolveBallCushion(ball, new Vec2(0.0, -1.0), PhysicsConfig.Default);

            Assert.AreEqual(0.0, result.Velocity.X, Tolerance);
            Assert.AreEqual(-2.0 * PhysicsConfig.Default.CushionRestitution, result.Velocity.Y, Tolerance);
        }

        [Test]
        public void Cushion_RollingHeadOn_ReversesRollSoBallKeepsMostOfItsSpeed()
        {
            PhysicsConfig config = PhysicsConfig.Default;
            double r = config.BallRadius;
            var ball = new BallState(0, Vec2.Zero, new Vec2(0.0, 2.0), new Vec3(-2.0 / r, 0.0, 0.0));

            BallState result = CollisionResolver.ResolveBallCushion(ball, new Vec2(0.0, -1.0), config);

            // Speed once cloth friction has re-established rolling along the rebound (-Y): (5 v + 2 R w) / 7.
            double rolled = (5.0 * -result.Velocity.Y + 2.0 * r * result.AngularVelocity.X) / 7.0;
            Assert.That(rolled, Is.GreaterThan(0.5 * 2.0).And.LessThanOrEqualTo(config.CushionRestitution * 2.0 + 1e-9));
        }

        [Test]
        public void Cushion_FrictionlessAngled_MirrorsTangentialVelocity()
        {
            var ball = new BallState(0, Vec2.Zero, new Vec2(1.0, 1.0), Vec3.Zero);

            BallState result = CollisionResolver.ResolveBallCushion(ball, new Vec2(0.0, -1.0), Frictionless);

            Assert.AreEqual(1.0, result.Velocity.X, Tolerance);
            Assert.AreEqual(-Frictionless.CushionRestitution, result.Velocity.Y, Tolerance);
        }

        [Test]
        public void Cushion_RightEnglishHeadOn_KicksBallToTheRight()
        {
            // Shooting along +Y, right english is counter-clockwise spin (+Z); shooter's right is +X.
            var ball = new BallState(0, Vec2.Zero, new Vec2(0.0, 1.0), new Vec3(0.0, 0.0, 40.0));

            BallState result = CollisionResolver.ResolveBallCushion(ball, new Vec2(0.0, -1.0), PhysicsConfig.Default);

            Assert.That(result.Velocity.X, Is.GreaterThan(0.0));
            Assert.That(result.Velocity.Y, Is.LessThan(0.0));
            Assert.That(result.AngularVelocity.Z, Is.LessThan(40.0), "Cushion friction consumes side spin.");
        }
    }
}
