using System;

namespace BilliardGame.Meta
{
    /// <summary>
    /// What the server's <c>liveops_state</c> RPC returns (ROADMAP M3.2, Server/nakama/modules/src/liveops.ts): missions,
    /// Season Pass, events and the daily shop. The client only shows it (CLAUDE.md rule 3). Public fields so
    /// JsonUtility reads the JSON; times are milliseconds left when the server answered.
    /// </summary>
    [Serializable]
    public sealed class LiveOpsState
    {
        public MissionBoard missions;
        public PassInfo pass;
        public EventInfo[] events = Array.Empty<EventInfo>();
        public DailyShopInfo shop;
    }

    [Serializable]
    public sealed class MissionBoard
    {
        public MissionInfo[] daily = Array.Empty<MissionInfo>();
        public MissionInfo[] weekly = Array.Empty<MissionInfo>();
        public bool rerollFree;
        public long dayResetInMs;
        public long weekResetInMs;
    }

    [Serializable]
    public sealed class MissionInfo
    {
        public int index;
        public string id;
        /// <summary>play, win, pot or win_venue (with <see cref="venue"/>).</summary>
        public string type;
        public int target;
        public string venue;
        public int progress;
        public bool claimed;
        public int coins;
        public int xp;

        public bool Complete => progress >= target;
        public bool Claimable => Complete && !claimed;
    }

    /// <summary>One reward: a wallet amount (coins, gems, shards, crystals, key_classic, key_premium) or an item.</summary>
    [Serializable]
    public sealed class RewardInfo
    {
        public string kind;
        public int amount;
        public string id;

        public bool IsItem => kind == "item";
    }

    [Serializable]
    public sealed class PassLevelInfo
    {
        public int level;
        public RewardInfo[] free = Array.Empty<RewardInfo>();
        public RewardInfo[] premium = Array.Empty<RewardInfo>();
        public bool freeClaimed;
        public bool premiumClaimed;
    }

    [Serializable]
    public sealed class PassInfo
    {
        public int season;
        public bool active;
        public long startsInMs;
        public long endsInMs;
        public int xp;
        public int level;
        public int xpPerLevel;
        public bool premium;
        public int premiumPrice;
        public PassLevelInfo[] levels = Array.Empty<PassLevelInfo>();
    }

    [Serializable]
    public sealed class EventInfo
    {
        public string id;
        public bool active;
        public long startsInMs;
        public long endsInMs;
        public float coinMultiplier = 1f;
        public float xpMultiplier = 1f;
        /// <summary>Crate the event features (opens the crate screen), or empty.</summary>
        public string crate;
    }

    [Serializable]
    public sealed class ShopOfferInfo
    {
        public int slot;
        public string id;
        /// <summary>"coin" or "gem".</summary>
        public string currency;
        public int price;
        public bool featured;
        public bool bought;
    }

    [Serializable]
    public sealed class DailyShopInfo
    {
        public ShopOfferInfo[] offers = Array.Empty<ShopOfferInfo>();
        public long resetInMs;
    }

    /// <summary>Display helpers over <see cref="LiveOpsState"/> (no rules: the server decides every claim).</summary>
    public static class LiveOpsRules
    {
        /// <summary>XP inside the current level and the XP that level needs; a maxed pass reads full.</summary>
        public static void PassProgress(PassInfo pass, out int into, out int needed)
        {
            needed = Math.Max(1, pass.xpPerLevel);
            into = pass.level >= pass.levels.Length ? needed : Math.Max(0, pass.xp - (pass.level - 1) * needed);
        }

        /// <summary>Rewards reached but not yet taken (premium ones only with the premium pass).</summary>
        public static int PassClaimable(PassInfo pass)
        {
            if (pass == null) return 0;
            int count = 0;
            foreach (PassLevelInfo level in pass.levels)
            {
                if (level.level > pass.level) break;
                if (!level.freeClaimed) count++;
                if (pass.premium && !level.premiumClaimed) count++;
            }

            return count;
        }

        public static int MissionsClaimable(MissionBoard board)
        {
            if (board == null) return 0;
            int count = 0;
            foreach (MissionInfo m in board.daily) if (m.Claimable) count++;
            foreach (MissionInfo m in board.weekly) if (m.Claimable) count++;
            return count;
        }

        /// <summary>Milliseconds left after <paramref name="elapsedMs"/> since the server answered, never negative.</summary>
        public static long Remaining(long msAtFetch, long elapsedMs) => Math.Max(0L, msAtFetch - elapsedMs);
    }
}
