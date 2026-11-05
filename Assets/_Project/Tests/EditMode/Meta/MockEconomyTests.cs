using System;
using System.IO;
using System.Linq;
using BilliardGame.Physics;
using NUnit.Framework;
using UnityEngine;

namespace BilliardGame.Meta.Tests
{
    public sealed class MockEconomyTests
    {
        private const string CratesPath = "Assets/_Project/Resources/Economy/crates.json";

        private static readonly CatalogItem[] Catalog =
        {
            new CatalogItem("cue_house", ItemType.Cue, Rarity.Common, true),
            new CatalogItem("cue_maple", ItemType.Cue, Rarity.Common, false),
            new CatalogItem("table_classic", ItemType.Table, Rarity.Common, true),
            new CatalogItem("cue_classic", ItemType.Cue, Rarity.Rare, false),
            new CatalogItem("cue_carbon", ItemType.Cue, Rarity.Epic, false),
            new CatalogItem("cue_ornate", ItemType.Cue, Rarity.Legendary, false),
            new CatalogItem("cue_dragon", ItemType.Cue, Rarity.Mythic, false),
        };

        private static CrateDefinition[] Crates() => JsonUtility.FromJson<CrateTableSet>(File.ReadAllText(CratesPath)).crates;

        private static CrateDefinition Crate(string id) => Crates().Single(c => c.id == id);

        private static MockEconomyService Service(Func<double> roll = null) => new MockEconomyService(null, Catalog, Crates(), null, roll);

        [Test]
        public void ShippedTablesAreValid()
        {
            foreach (CrateDefinition crate in Crates()) Assert.DoesNotThrow(crate.Validate, crate.id);
        }

        [TestCase("classic")]
        [TestCase("premium")]
        public void RarityDistributionMatchesTableWithinHalfPercentOver100kRolls(string crateId)
        {
            CrateDefinition crate = Crate(crateId);
            const int rolls = 100_000;
            var counts = new int[RarityInfo.Count];
            for (int i = 0; i < rolls; i++) counts[(int)CrateRoller.RollRarity(crate.weights, CrateRoller.Roll(12345UL, (ulong)i), Rarity.Common)]++;

            for (int r = 0; r < RarityInfo.Count; r++)
            {
                double percent = counts[r] * 100.0 / rolls;
                Assert.AreEqual(crate.weights[r], percent, 0.5, $"{crateId} {(Rarity)r}");
            }
        }

        [Test]
        public void ClassicGuaranteesEpicOnFifteenthOpen()
        {
            MockEconomyService economy = Service(() => 0.0);
            economy.DevGrant(100_000, 0);
            PityRule rule = Crate("classic").pity[0];

            for (int i = 1; i <= 30; i++)
            {
                Rarity rarity = economy.OpenCrateAsync("classic", 1).Result.Drops[0].Item.Rarity;
                if (i % 15 == 0) Assert.GreaterOrEqual(rarity, Rarity.Epic, $"open {i}");
                else Assert.AreEqual(Rarity.Common, rarity, $"open {i}");
            }

            Assert.AreEqual(15, economy.PityRemaining("classic", rule));
        }

        [Test]
        public void PremiumGuaranteesLegendaryAtFortyAndMythicAtHundred()
        {
            MockEconomyService economy = Service(() => 0.0);
            economy.DevGrant(0, 10_000);

            Rarity[] rarities = Enumerable.Range(0, 100).Select(_ => economy.OpenCrateAsync("premium", 1).Result.Drops[0].Item.Rarity).ToArray();

            Assert.AreEqual(Rarity.Legendary, rarities[39]);
            Assert.AreEqual(Rarity.Legendary, rarities[79]);
            Assert.AreEqual(Rarity.Mythic, rarities[99]);
            Assert.AreEqual(97, rarities.Count(r => r == Rarity.Common));
        }

        [Test]
        public void MythicResetsLegendaryPity()
        {
            int call = 0;
            // First open rolls Mythic (roll just below 1), everything after rolls Common.
            MockEconomyService economy = Service(() => call++ == 0 ? 0.9999 : 0.0);
            economy.DevGrant(0, 1000);
            PityRule legendary = Crate("premium").pity[0];

            Assert.AreEqual(Rarity.Mythic, economy.OpenCrateAsync("premium", 1).Result.Drops[0].Item.Rarity);
            Assert.AreEqual(legendary.count, economy.PityRemaining("premium", legendary));
        }

        [Test]
        public void PremiumTenOpenContainsEpicOrBetter()
        {
            MockEconomyService economy = Service(() => 0.0);
            economy.DevGrant(0, 1000);
            CrateOpenResult result = economy.OpenCrateAsync("premium", 10).Result;

            Assert.IsTrue(result.Success);
            Assert.AreEqual(10, result.Drops.Count);
            Assert.GreaterOrEqual(result.Drops.Max(d => d.Item.Rarity), Rarity.Epic);
            Assert.AreEqual(MockEconomyService.StartGems + 1000 - 900, economy.Gems);
        }

        [Test]
        public void DuplicateBecomesShards()
        {
            MockEconomyService economy = Service(() => 0.0);
            CrateDrop drop = economy.OpenCrateAsync("classic", 1).Result.Drops[0];

            Assert.AreEqual("cue_house", drop.Item.Id, "roll 0 picks the first Common, which is owned by default");
            Assert.IsFalse(drop.IsNew);
            Assert.AreEqual(RarityInfo.DuplicateShards(Rarity.Common), drop.Shards);
            Assert.AreEqual(5, economy.Shards);
        }

        [Test]
        public void NewItemCanBeEquipped()
        {
            int call = 0;
            // Rarity roll lands on Epic, the item roll picks the only Epic.
            MockEconomyService economy = Service(() => call++ % 2 == 0 ? 0.96 : 0.0);
            CrateDrop drop = economy.OpenCrateAsync("classic", 1).Result.Drops[0];

            Assert.AreEqual("cue_carbon", drop.Item.Id);
            Assert.IsTrue(drop.IsNew);
            Assert.IsTrue(economy.EquipAsync("cue_carbon").Result);
            Assert.AreEqual("cue_carbon", economy.EquippedId(ItemType.Cue));
            Assert.IsFalse(economy.EquipAsync("cue_dragon").Result, "cannot equip what is not owned");
        }

        [Test]
        public void NotEnoughCurrencyChargesNothing()
        {
            MockEconomyService economy = Service();
            CrateOpenResult result = economy.OpenCrateAsync("classic", 10).Result;

            Assert.IsFalse(result.Success);
            Assert.AreEqual(MockEconomyService.StartCoins, economy.Coins);
            Assert.AreEqual(0, result.Drops.Count);
        }

        [Test]
        public void StateSurvivesJsonRoundTrip()
        {
            EconomyState saved = null;
            var economy = new MockEconomyService(null, Catalog, Crates(), s => saved = s, () => 0.0);
            economy.OpenCrateAsync("classic", 1).Wait();

            EconomyState restored = JsonUtility.FromJson<EconomyState>(JsonUtility.ToJson(saved));
            var reloaded = new MockEconomyService(restored, Catalog, Crates(), null);

            Assert.AreEqual(economy.Coins, reloaded.Coins);
            Assert.AreEqual(economy.Shards, reloaded.Shards);
            PityRule rule = Crate("classic").pity[0];
            Assert.AreEqual(economy.PityRemaining("classic", rule), reloaded.PityRemaining("classic", rule));
        }

        [Test]
        public void EffectiveChanceMatchesEconomyDoc()
        {
            Assert.AreEqual(0.099, CrateRoller.EffectiveChance(0.06, 15), 0.001);
            Assert.AreEqual(0.057, CrateRoller.EffectiveChance(0.05, 40), 0.001);
            Assert.AreEqual(0.0127, CrateRoller.EffectiveChance(0.005, 100), 0.0001);
        }

        [Test]
        public void SimulationNeverSeesCosmetics()
        {
            // No pay-to-win (CLAUDE.md rule 5): physics and rules cannot even reference the meta/content assemblies.
            foreach (Type anchor in new[] { typeof(ShotParams), typeof(Gameplay.EightBallRules) })
            {
                string[] references = anchor.Assembly.GetReferencedAssemblies().Select(a => a.Name).ToArray();
                Assert.IsFalse(references.Any(n => n.StartsWith("BilliardGame.Meta") || n.StartsWith("BilliardGame.Content")), anchor.Assembly.GetName().Name);
            }
        }
    }
}
