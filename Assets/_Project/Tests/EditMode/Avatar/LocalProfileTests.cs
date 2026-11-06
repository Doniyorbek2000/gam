using BilliardGame.Presentation;
using NUnit.Framework;

namespace BilliardGame.Avatar.Tests
{
    /// <summary>Editor and dev bot on one PC use separate saves and accounts.</summary>
    public sealed class LocalProfileTests
    {
        [Test]
        public void ExplicitProfileWins_AndIsSanitised()
        {
            Assert.AreEqual("-bob_2x", LocalProfile.Resolve(new[] { "game.exe", "-autopilot", "-profile", "Bob_2/../x" }));
        }

        [Test]
        public void AutopilotBotGetsItsOwnProfile()
        {
            Assert.AreEqual("-bot", LocalProfile.Resolve(new[] { "game.exe", "-autopilot" }));
        }

        [Test]
        public void NormalLaunchKeepsDefaultSave()
        {
            Assert.AreEqual(string.Empty, LocalProfile.Resolve(new[] { "game.exe" }));
            Assert.AreEqual(string.Empty, LocalProfile.Resolve(new[] { "game.exe", "-profile", "../" }), "nothing usable left");
        }
    }
}
