using NUnit.Framework;
using UnityEngine;

namespace BilliardGame.Meta.Tests
{
    public sealed class LiveOpsTests
    {
        private const string Json = "{\"missions\":{\"daily\":[{\"index\":0,\"id\":\"d_win1\",\"type\":\"win\",\"target\":1,\"venue\":\"\",\"progress\":1,\"claimed\":false,\"coins\":300,\"xp\":200}," +
                                    "{\"index\":1,\"id\":\"d_pot5\",\"type\":\"pot\",\"target\":5,\"venue\":\"\",\"progress\":2,\"claimed\":false,\"coins\":300,\"xp\":200}]," +
                                    "\"weekly\":[{\"index\":0,\"id\":\"w_win5\",\"type\":\"win\",\"target\":5,\"progress\":5,\"claimed\":true,\"coins\":1000,\"xp\":600}],\"rerollFree\":true,\"dayResetInMs\":3600000,\"weekResetInMs\":90000000}," +
                                    "\"pass\":{\"season\":1,\"active\":true,\"endsInMs\":4838400000,\"xp\":2350,\"level\":3,\"xpPerLevel\":1000,\"premium\":false,\"premiumPrice\":800,\"levels\":[" +
                                    "{\"level\":1,\"free\":[{\"kind\":\"coins\",\"amount\":500}],\"premium\":[{\"kind\":\"item\",\"amount\":1,\"id\":\"cue_carbon\"}],\"freeClaimed\":true,\"premiumClaimed\":false}," +
                                    "{\"level\":2,\"free\":[{\"kind\":\"shards\",\"amount\":20}],\"premium\":[],\"freeClaimed\":false,\"premiumClaimed\":false}," +
                                    "{\"level\":3,\"free\":[],\"premium\":[],\"freeClaimed\":false,\"premiumClaimed\":false}," +
                                    "{\"level\":4,\"free\":[],\"premium\":[],\"freeClaimed\":false,\"premiumClaimed\":false}]}," +
                                    "\"events\":[{\"id\":\"weekend_coins\",\"active\":true,\"startsInMs\":0,\"endsInMs\":7200000,\"coinMultiplier\":2,\"xpMultiplier\":1,\"crate\":\"\"}]," +
                                    "\"shop\":{\"offers\":[{\"slot\":5,\"id\":\"cue_dragon\",\"currency\":\"gem\",\"price\":6000,\"featured\":true,\"bought\":false}],\"resetInMs\":3600000}}";

        [Test]
        public void ParsesServerStateWithJsonUtility()
        {
            LiveOpsState state = JsonUtility.FromJson<LiveOpsState>(Json);
            Assert.AreEqual(2, state.missions.daily.Length);
            Assert.IsTrue(state.missions.daily[0].Claimable);
            Assert.IsFalse(state.missions.daily[1].Complete);
            Assert.IsTrue(state.pass.levels[0].premium[0].IsItem);
            Assert.AreEqual("cue_carbon", state.pass.levels[0].premium[0].id);
            Assert.AreEqual(2f, state.events[0].coinMultiplier);
            Assert.AreEqual(4838400000L, state.pass.endsInMs);
            Assert.IsTrue(state.shop.offers[0].featured);
        }

        [Test]
        public void PassProgressAndClaimableCounts()
        {
            LiveOpsState state = JsonUtility.FromJson<LiveOpsState>(Json);
            LiveOpsRules.PassProgress(state.pass, out int into, out int needed);
            Assert.AreEqual(350, into);
            Assert.AreEqual(1000, needed);
            Assert.AreEqual(2, LiveOpsRules.PassClaimable(state.pass), "levels 2 and 3 free; premium needs the pass");
            state.pass.premium = true;
            Assert.AreEqual(5, LiveOpsRules.PassClaimable(state.pass));
            state.pass.level = state.pass.levels.Length;
            LiveOpsRules.PassProgress(state.pass, out into, out needed);
            Assert.AreEqual(needed, into, "a maxed pass reads full");
            Assert.AreEqual(1, LiveOpsRules.MissionsClaimable(state.missions), "the weekly one is already claimed");
            Assert.AreEqual(0L, LiveOpsRules.Remaining(1000, 5000));
        }
    }
}
