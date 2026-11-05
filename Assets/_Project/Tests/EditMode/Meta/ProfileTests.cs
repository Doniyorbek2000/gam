using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace BilliardGame.Meta.Tests
{
    public sealed class ProfileTests
    {
        private static MockProfileService Service() => new MockProfileService(null, null, new System.Random(7));

        private static MatchRecord Match(bool won, int potted = 0, int shots = 10, int maxInShot = 1) => new MatchRecord
        {
            opponent = "Bot",
            won = won,
            venueId = "garage",
            cueId = "cue_house",
            shots = shots,
            pottingShots = potted > 0 ? 1 : 0,
            ballsPotted = potted,
            maxBallsInOneShot = maxInShot,
            xpGained = ProfileRules.MatchXp(won, potted),
        };

        [Test]
        public void LevelCurveFollowsGdd()
        {
            Assert.AreEqual(200, ProfileRules.XpToNext(1));
            Assert.AreEqual(350, ProfileRules.XpToNext(2));
            Assert.AreEqual(1, ProfileRules.Level(199, out int into, out int needed));
            Assert.AreEqual(199, into);
            Assert.AreEqual(200, needed);
            Assert.AreEqual(2, ProfileRules.Level(200, out into, out _));
            Assert.AreEqual(0, into);
        }

        [TestCase(0, RankTier.Bronze, 3)]
        [TestCase(299, RankTier.Bronze, 1)]
        [TestCase(300, RankTier.Silver, 3)]
        [TestCase(1199, RankTier.Gold, 1)]
        [TestCase(1200, RankTier.Platinum, 4)]
        [TestCase(5000, RankTier.Legend, 0)]
        public void RankTiersMatchTable(int points, RankTier tier, int division)
        {
            Assert.AreEqual(tier, ProfileRules.Rank(points, out int actual));
            Assert.AreEqual(division, actual);
        }

        [Test]
        public void RecordTracksStreaksRateAndBestShot()
        {
            MockProfileService profile = Service();
            foreach (bool won in new[] { true, true, true, false, true }) profile.RecordMatch(Match(won, potted: 2, maxInShot: won ? 2 : 3));

            ProfileStats s = profile.State.stats;
            Assert.AreEqual(5, s.matches);
            Assert.AreEqual(4, s.wins);
            Assert.AreEqual(3, s.bestStreak);
            Assert.AreEqual(1, s.currentStreak);
            Assert.AreEqual(3, s.maxBallsInOneShot);
            Assert.AreEqual(0.8f, ProfileRules.WinRate(s), 1e-5);
            Assert.AreEqual("cue_house", ProfileRules.FavouriteCue(s));
            Assert.AreEqual(4 * 110 + 50, profile.State.xp);
        }

        [Test]
        public void HistoryKeepsNewestTwenty()
        {
            MockProfileService profile = Service();
            for (int i = 0; i < 25; i++)
            {
                MatchRecord m = Match(true);
                m.opponent = "P" + i;
                profile.RecordMatch(m);
            }

            Assert.AreEqual(ProfileRules.HistorySize, profile.State.history.Count);
            Assert.AreEqual("P24", profile.State.history[0].opponent);
            Assert.AreEqual("P5", profile.State.history.Last().opponent);
        }

        [TestCase("ab", "profile.nick_short")]
        [TestCase("ThisNameIsWayTooLong", "profile.nick_long")]
        [TestCase("bad<tag>", "profile.nick_chars")]
        [TestCase("Doniyor_01", null)]
        [TestCase("Алишер", null)]
        public void NicknameValidation(string name, string error)
        {
            Assert.AreEqual(error, ProfileRules.ValidateNickname(name));
        }

        [Test]
        public void NewProfileGetsEightDigitIdAndValidName()
        {
            MockProfileService profile = Service();
            Assert.AreEqual(8, profile.State.playerId.Length);
            Assert.IsNull(ProfileRules.ValidateNickname(profile.State.nickname));
            Assert.AreEqual("profile.nick_short", profile.SetNickname(" a "));
        }

        [Test]
        public void RoomOnlyKeepsOwnedItems()
        {
            var owned = new HashSet<string> { "table_classic", "wall_plain", "floor_wood", "sticker_gg" };
            var layout = new RoomLayout { tableId = "table_royal", wallId = "wall_plain", floorId = null, posters = new List<string> { "sticker_gg", "sticker_choy" } };

            RoomLayout clean = layout.Sanitize(owned.Contains, "table_classic", "wall_plain", "floor_wood");

            Assert.AreEqual("table_classic", clean.tableId, "unowned table falls back to the default");
            Assert.AreEqual("floor_wood", clean.floorId);
            CollectionAssert.AreEqual(new[] { "sticker_gg", null, null }, clean.posters);
            Assert.AreEqual("table_royal", layout.tableId, "input is not mutated");
        }

        [Test]
        public void ShowcasePrefersChosenThenRecent()
        {
            MockProfileService profile = Service();
            for (int i = 0; i < 10; i++) profile.RecordMatch(Match(true, potted: 1));
            profile.SetShowcase("wins_10", "wins_100");

            List<AchievementDef> shown = Achievements.Showcase(profile.State);

            Assert.AreEqual("wins_10", shown[0].Id, "chosen and done");
            Assert.IsFalse(shown.Any(a => a.Id == "wins_100"), "chosen but not done is skipped");
            Assert.AreEqual(3, shown.Count);
        }

        [Test]
        public void ProfileSurvivesJsonRoundTrip()
        {
            ProfileState saved = null;
            var profile = new MockProfileService(null, s => saved = s, new System.Random(1));
            profile.RecordMatch(Match(true, potted: 3));

            var restored = new MockProfileService(JsonUtility.FromJson<ProfileState>(JsonUtility.ToJson(saved)), null);

            Assert.AreEqual(profile.State.playerId, restored.State.playerId);
            Assert.AreEqual(1, restored.State.history.Count);
            Assert.AreEqual(profile.State.xp, restored.State.xp);
        }

        [Test]
        public void NewProfileSavesGeneratedIdentityImmediately()
        {
            ProfileState saved = null;
            var profile = new MockProfileService(null, s => saved = s, new System.Random(3));

            Assert.IsNotNull(saved);
            Assert.AreEqual(profile.State.nickname, saved.nickname);

            int saves = 0;
            new MockProfileService(JsonUtility.FromJson<ProfileState>(JsonUtility.ToJson(saved)), _ => saves++);
            Assert.AreEqual(0, saves);
        }

        [Test]
        public void StickersEquipPerSlotAndOnlyWhenOwned()
        {
            var catalog = new[]
            {
                new CatalogItem("cue_house", ItemType.Cue, Rarity.Common, true),
                new CatalogItem("table_classic", ItemType.Table, Rarity.Common, true),
                new CatalogItem("sticker_gg", ItemType.Sticker, Rarity.Common, true),
                new CatalogItem("sticker_choy", ItemType.Sticker, Rarity.Rare, false),
                new CatalogItem("frame_basic", ItemType.Frame, Rarity.Common, true),
            };
            var crates = new[] { new CrateDefinition { id = "c", currency = "coin", price = 1, weights = new[] { 100f, 0f, 0f, 0f, 0f } } };
            var economy = new MockEconomyService(null, catalog, crates, null);

            Assert.AreEqual("frame_basic", economy.EquippedId(ItemType.Frame), "default frame equipped");
            Assert.IsTrue(economy.EquipStickerAsync("sticker_gg", StickerSlot.Table).Result);
            Assert.AreEqual("sticker_gg", economy.EquippedSticker(StickerSlot.Table));
            Assert.IsNull(economy.EquippedSticker(StickerSlot.Cue));
            Assert.IsFalse(economy.EquipStickerAsync("sticker_choy", StickerSlot.Cue).Result, "not owned");
            Assert.IsFalse(economy.EquipStickerAsync("cue_house", StickerSlot.Cue).Result, "not a sticker");
            Assert.IsTrue(economy.EquipStickerAsync(null, StickerSlot.Table).Result);
            Assert.IsNull(economy.EquippedSticker(StickerSlot.Table));
        }
    }
}
