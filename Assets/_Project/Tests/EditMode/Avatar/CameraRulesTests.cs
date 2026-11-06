using BilliardGame.Avatar.Rules;
using BilliardGame.Physics;
using NUnit.Framework;

namespace BilliardGame.Avatar.Tests
{
    /// <summary>M1.6.2 camera modes: cycling and aim-camera placement.</summary>
    public sealed class CameraRulesTests
    {
        [Test]
        public void Next_CyclesThroughAllFourModes()
        {
            MatchCameraMode mode = MatchCameraMode.Classic;
            for (int i = 0; i < CameraRules.ModeCount; i++) mode = CameraRules.Next(mode);

            Assert.AreEqual(MatchCameraMode.Classic, mode);
            Assert.AreEqual(MatchCameraMode.ThirdPerson, CameraRules.Next(MatchCameraMode.Classic));
            Assert.AreEqual(MatchCameraMode.Classic, CameraRules.Next(MatchCameraMode.Cinematic));
        }

        [Test]
        public void Behind_IsOppositeTheAim()
        {
            var ball = new Vec2(0.2, 0.1);

            Vec2 third = CameraRules.Behind(MatchCameraMode.ThirdPerson, ball, Vec2.UnitX);
            Vec2 first = CameraRules.Behind(MatchCameraMode.FirstPerson, ball, Vec2.UnitX);

            Assert.AreEqual(ball.X - CameraRules.ThirdBack, third.X, 1e-9);
            Assert.AreEqual(ball.Y, third.Y, 1e-9);
            Assert.Greater(first.X, third.X, "first person sits closer to the ball");
            Assert.Less(CameraRules.Height(MatchCameraMode.FirstPerson), CameraRules.Height(MatchCameraMode.ThirdPerson));
        }

        [Test]
        public void OnlyClassicAimsFromAbove()
        {
            Assert.IsFalse(CameraRules.AimsFromBehind(MatchCameraMode.Classic));
            Assert.IsTrue(CameraRules.AimsFromBehind(MatchCameraMode.ThirdPerson));
            Assert.IsTrue(CameraRules.AimsFromBehind(MatchCameraMode.FirstPerson));
            Assert.IsTrue(CameraRules.AimsFromBehind(MatchCameraMode.Cinematic));
        }
    }
}
