using System;
using BilliardGame.Content;
using BilliardGame.Meta;

namespace BilliardGame.UI
{
    /// <summary>Localized text for LiveOps data: countdowns, mission titles, rewards, server errors.</summary>
    public static class LiveOpsText
    {
        /// <summary>"2 kun 04 soat" from a day up, else "05:12:40".</summary>
        public static string Countdown(long ms)
        {
            TimeSpan t = TimeSpan.FromMilliseconds(Math.Max(0L, ms));
            if (t.TotalDays >= 1d) return Loc.Format("time.days_hours", (int)t.TotalDays, t.Hours.ToString("00", Loc.Culture));
            return ((int)t.TotalHours).ToString("00", Loc.Culture) + ":" + t.Minutes.ToString("00", Loc.Culture) + ":" + t.Seconds.ToString("00", Loc.Culture);
        }

        public static string MissionTitle(MissionInfo mission, SkinCatalog catalog)
        {
            if (mission.type != "win_venue") return Loc.Format("mission." + mission.type, mission.target);
            VenueDefinition venue = null;
            foreach (VenueDefinition v in catalog.Venues) if (v.Id == mission.venue) venue = v;
            return Loc.Format("mission.win_venue", mission.target, venue != null ? LobbyController.VenueName(venue) : mission.venue);
        }

        /// <summary>"+300 Coin", "Classic kalit ×2", or the item's name.</summary>
        public static string Reward(RewardInfo reward, SkinCatalog catalog)
        {
            if (reward.IsItem) return RarityStyle.ItemName(catalog.Find(reward.id));
            return "+" + Loc.Number(reward.amount) + " " + Loc.Get("reward." + reward.kind, reward.kind);
        }

        public static string Rewards(RewardInfo[] rewards, SkinCatalog catalog)
        {
            var parts = new string[rewards.Length];
            for (int i = 0; i < rewards.Length; i++) parts[i] = Reward(rewards[i], catalog);
            return string.Join(", ", parts);
        }

        /// <summary>Server messages are English and technical; known ones get a localized line, the rest a generic one.</summary>
        public static string Error(string serverMessage)
        {
            if (serverMessage == null) return null;
            if (serverMessage.Contains("not enough coins")) return Loc.Get("crates.not_enough_coins");
            if (serverMessage.Contains("not enough gems")) return Loc.Get("crates.not_enough_gems");
            if (serverMessage.Contains("already")) return Loc.Get("liveops.already");
            return Loc.Get("liveops.failed");
        }
    }
}
