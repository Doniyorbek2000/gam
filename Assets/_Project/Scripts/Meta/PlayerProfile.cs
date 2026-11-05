using System;
using System.Collections.Generic;

namespace BilliardGame.Meta
{
    /// <summary>Persisted mock profile (JSON). Public fields so Unity's JsonUtility can serialize it. M2 moves it to Nakama.</summary>
    [Serializable]
    public sealed class ProfileState
    {
        public int version = 1;
        public string playerId;
        public string nickname;
        public string countryCode = "UZ";
        public string clubName;
        public int xp;
        public int rankPoints;
        public ProfileStats stats = new ProfileStats();
        public List<MatchRecord> history = new List<MatchRecord>();
        public List<string> showcase = new List<string>();
        public RoomLayout room = new RoomLayout();
    }

    [Serializable]
    public sealed class ProfileStats
    {
        public int matches;
        public int wins;
        public int currentStreak;
        public int bestStreak;
        public int shots;
        public int pottingShots;
        public int ballsPotted;
        public int maxBallsInOneShot;
        public List<CueUse> cueUses = new List<CueUse>();
    }

    [Serializable]
    public sealed class CueUse
    {
        public string cueId;
        public int matches;
    }

    /// <summary>One finished match from the local player's side.</summary>
    [Serializable]
    public sealed class MatchRecord
    {
        public string opponent;
        public bool won;
        public string venueId;
        public string cueId;
        public long unixTime;
        public int shots;
        public int pottingShots;
        public int ballsPotted;
        public int maxBallsInOneShot;
        public int xpGained;
        public int coinsGained;
    }

    /// <summary>
    /// The player's own 3D room (docs/UI_UX.md §11.1). Pure data so a friend's room can later be fetched from the server and
    /// shown by the same view; only items the owner has are ever placed (<see cref="Sanitize"/>).
    /// </summary>
    [Serializable]
    public sealed class RoomLayout
    {
        public const int PosterSlots = 3;

        public string tableId;
        public string wallId;
        public string floorId;
        public List<string> posters = new List<string>();

        public RoomLayout Clone() => new RoomLayout { tableId = tableId, wallId = wallId, floorId = floorId, posters = new List<string>(posters ?? new List<string>()) };

        /// <summary>Returns a copy where every slot holds an owned item; unknown or unowned ids fall back to the given defaults.</summary>
        public RoomLayout Sanitize(Func<string, bool> owns, string defaultTable, string defaultWall, string defaultFloor)
        {
            var clean = new RoomLayout
            {
                tableId = owns(tableId) ? tableId : defaultTable,
                wallId = owns(wallId) ? wallId : defaultWall,
                floorId = owns(floorId) ? floorId : defaultFloor,
            };
            foreach (string poster in posters ?? new List<string>())
            {
                if (clean.posters.Count == PosterSlots) break;
                clean.posters.Add(poster != null && owns(poster) ? poster : null);
            }

            while (clean.posters.Count < PosterSlots) clean.posters.Add(null);
            return clean;
        }
    }

    public enum RankTier
    {
        Bronze = 0,
        Silver = 1,
        Gold = 2,
        Platinum = 3,
        Diamond = 4,
        Crown = 5,
        Legend = 6,
    }

    /// <summary>Level, rank and stat formulas (docs/GDD.md §6.1–6.2). Editor-testable, no UnityEngine.</summary>
    public static class ProfileRules
    {
        public const int HistorySize = 20;
        public const int WinXp = 100;
        public const int LossXp = 40;
        public const int XpPerBall = 5;
        public const int MinNickname = 3;
        public const int MaxNickname = 16;

        private static readonly int[] TierFloors = { 0, 300, 700, 1200, 1800, 2500, 3300 };
        private static readonly int[] TierDivisions = { 3, 3, 3, 4, 4, 5, 1 };

        /// <summary>XP needed to go from level <paramref name="level"/> to the next: 200 + 150·(n−1)^1.15, rounded.</summary>
        public static int XpToNext(int level) => (int)Math.Round(200 + 150 * Math.Pow(Math.Max(0, level - 1), 1.15));

        public static int Level(int totalXp, out int intoLevel, out int needed)
        {
            int level = 1;
            int remaining = Math.Max(0, totalXp);
            while (remaining >= XpToNext(level))
            {
                remaining -= XpToNext(level);
                level++;
            }

            intoLevel = remaining;
            needed = XpToNext(level);
            return level;
        }

        public static int MatchXp(bool won, int ballsPotted) => (won ? WinXp : LossXp) + XpPerBall * Math.Max(0, ballsPotted);

        /// <summary>Tier and division (1 = top of the tier) for ranked points.</summary>
        public static RankTier Rank(int rankPoints, out int division)
        {
            int tier = TierFloors.Length - 1;
            while (tier > 0 && rankPoints < TierFloors[tier]) tier--;
            if (tier == (int)RankTier.Legend)
            {
                division = 0;
                return RankTier.Legend;
            }

            int span = TierFloors[tier + 1] - TierFloors[tier];
            int step = span / TierDivisions[tier];
            int index = Math.Min(TierDivisions[tier] - 1, (Math.Max(0, rankPoints) - TierFloors[tier]) / step);
            division = TierDivisions[tier] - index;
            return (RankTier)tier;
        }

        public static float WinRate(ProfileStats stats) => stats.matches == 0 ? 0f : (float)stats.wins / stats.matches;

        /// <summary>Share of shots that potted at least one own ball.</summary>
        public static float Accuracy(ProfileStats stats) => stats.shots == 0 ? 0f : (float)stats.pottingShots / stats.shots;

        public static string FavouriteCue(ProfileStats stats)
        {
            CueUse best = null;
            foreach (CueUse use in stats.cueUses)
            {
                if (best == null || use.matches > best.matches) best = use;
            }

            return best?.cueId;
        }

        /// <summary>Adds one match to the stats and the capped history (newest first) and grants its XP.</summary>
        public static void Record(ProfileState profile, MatchRecord match)
        {
            ProfileStats s = profile.stats;
            s.matches++;
            if (match.won)
            {
                s.wins++;
                s.currentStreak++;
                s.bestStreak = Math.Max(s.bestStreak, s.currentStreak);
            }
            else
            {
                s.currentStreak = 0;
            }

            s.shots += match.shots;
            s.pottingShots += match.pottingShots;
            s.ballsPotted += match.ballsPotted;
            s.maxBallsInOneShot = Math.Max(s.maxBallsInOneShot, match.maxBallsInOneShot);
            if (!string.IsNullOrEmpty(match.cueId))
            {
                CueUse use = s.cueUses.Find(u => u.cueId == match.cueId);
                if (use == null) s.cueUses.Add(use = new CueUse { cueId = match.cueId });
                use.matches++;
            }

            profile.xp += match.xpGained;
            profile.history.Insert(0, match);
            if (profile.history.Count > HistorySize) profile.history.RemoveRange(HistorySize, profile.history.Count - HistorySize);
        }

        /// <summary>Null when valid, else a localization key describing the problem.</summary>
        public static string ValidateNickname(string name)
        {
            if (name == null) return "profile.nick_short";
            name = name.Trim();
            if (name.Length < MinNickname) return "profile.nick_short";
            if (name.Length > MaxNickname) return "profile.nick_long";
            foreach (char c in name)
            {
                if (!char.IsLetterOrDigit(c) && c != '_' && c != ' ' && c != '\'' && c != '.') return "profile.nick_chars";
            }

            return null;
        }
    }
}
