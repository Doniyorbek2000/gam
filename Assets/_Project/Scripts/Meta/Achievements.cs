using System;
using System.Collections.Generic;

namespace BilliardGame.Meta
{
    /// <summary>One-off goal (docs/GDD.md §6.6). Progress is read from the profile, so it needs no extra state.</summary>
    public sealed class AchievementDef
    {
        public AchievementDef(string id, string icon, int target, Func<ProfileState, int> progress)
        {
            Id = id;
            Icon = icon;
            Target = target;
            Progress = progress;
        }

        /// <summary>Also the localization key suffix: achievement.&lt;id&gt; and achievement.&lt;id&gt;.desc.</summary>
        public string Id { get; }
        public string Icon { get; }
        public int Target { get; }
        public Func<ProfileState, int> Progress { get; }

        public int Current(ProfileState profile) => Math.Min(Target, Progress(profile));

        public bool IsDone(ProfileState profile) => Progress(profile) >= Target;
    }

    public static class Achievements
    {
        public static readonly IReadOnlyList<AchievementDef> All = new[]
        {
            new AchievementDef("first_win", "trophy", 1, p => p.stats.wins),
            new AchievementDef("wins_10", "cup", 10, p => p.stats.wins),
            new AchievementDef("wins_100", "crown", 100, p => p.stats.wins),
            new AchievementDef("matches_25", "eight_ball", 25, p => p.stats.matches),
            new AchievementDef("streak_5", "fire", 5, p => p.stats.bestStreak),
            new AchievementDef("multi_pot_3", "target", 3, p => p.stats.maxBallsInOneShot),
            new AchievementDef("balls_100", "rack", 100, p => p.stats.ballsPotted),
            new AchievementDef("level_10", "medal", 10, p => ProfileRules.Level(p.xp, out _, out _)),
        };

        public static AchievementDef Find(string id)
        {
            foreach (AchievementDef def in All)
            {
                if (def.Id == id) return def;
            }

            return null;
        }

        /// <summary>The three badges on the profile: the chosen ones that are done, then the most recently done others.</summary>
        public static List<AchievementDef> Showcase(ProfileState profile, int count = 3)
        {
            var shown = new List<AchievementDef>();
            foreach (string id in profile.showcase)
            {
                AchievementDef def = Find(id);
                if (def != null && def.IsDone(profile) && !shown.Contains(def) && shown.Count < count) shown.Add(def);
            }

            for (int i = All.Count - 1; i >= 0 && shown.Count < count; i--)
            {
                if (All[i].IsDone(profile) && !shown.Contains(All[i])) shown.Add(All[i]);
            }

            return shown;
        }
    }

    /// <summary>"Cues 7/40": owned vs. known items per category.</summary>
    public static class CollectionProgress
    {
        public static void Count(IReadOnlyList<CatalogItem> catalog, Func<string, bool> owns, ItemType type, out int owned, out int total)
        {
            owned = 0;
            total = 0;
            foreach (CatalogItem item in catalog)
            {
                if (item.Type != type) continue;
                total++;
                if (owns(item.Id)) owned++;
            }
        }
    }
}
