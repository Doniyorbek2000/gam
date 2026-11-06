using BilliardGame.Avatar.Rules;
using NUnit.Framework;

namespace BilliardGame.Avatar.Tests
{
    /// <summary>M1.6.3: NPC count follows graphics quality and the hall's spots.</summary>
    public sealed class HallRulesTests
    {
        [TestCase(0, 8, 0)]
        [TestCase(1, 8, 4)]
        [TestCase(2, 8, 8)]
        [TestCase(2, 3, 3)]
        [TestCase(1, 0, 0)]
        public void NpcCount_ByQuality(int quality, int spots, int expected)
        {
            Assert.AreEqual(expected, HallRules.NpcCount(quality, spots));
        }
    }
}
