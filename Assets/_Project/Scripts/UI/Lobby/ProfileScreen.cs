using System;
using System.Collections.Generic;
using BilliardGame.Content;
using BilliardGame.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BilliardGame.UI
{
    /// <summary>
    /// Profile (docs/UI_UX.md §4): header (avatar, frame, editable nickname, ID with copy, level, rank, country, club)
    /// and four tabs — stats, last 20 matches, achievements (showcase of 3 + full list), collection per category.
    /// </summary>
    public sealed class ProfileScreen : ScreenView
    {
        [SerializeField] private GameServices services;
        [SerializeField] private UITheme theme;

        [Header("Header")]
        [SerializeField] private Image avatarFrame;
        [SerializeField] private TMP_Text levelChip;
        [SerializeField] private TMP_Text nickname;
        [SerializeField] private Button editNickname;
        [SerializeField] private TMP_Text playerId;
        [SerializeField] private Button copyId;
        [SerializeField] private TMP_Text rank;
        [SerializeField] private TMP_Text country;
        [SerializeField] private TMP_Text club;
        [SerializeField] private ProgressBar xpBar;
        [SerializeField] private TMP_Text xpText;

        [Header("Tabs")]
        [SerializeField] private TabBar tabs;
        [SerializeField] private GameObject[] pages = new GameObject[0];

        [Header("Stats: matches, wins, win rate, best streak, accuracy, most in one shot, favourite cue")]
        [SerializeField] private TMP_Text[] statValues = new TMP_Text[0];

        [Header("History")]
        [SerializeField] private RowView historyTemplate;
        [SerializeField] private EmptyStateView historyEmpty;

        [Header("Achievements")]
        [SerializeField] private RowView[] achievementRows = new RowView[0];
        [SerializeField] private Image[] showcaseIcons = new Image[0];
        [SerializeField] private TMP_Text[] showcaseNames = new TMP_Text[0];
        [SerializeField] private TMP_Text showcaseHint;

        [Header("Collection")]
        [SerializeField] private RowView[] collectionRows = new RowView[0];

        private readonly List<RowView> historyRows = new List<RowView>();

        protected override void Awake()
        {
            base.Awake();
            tabs.Changed += ShowPage;
            editNickname.onClick.AddListener(EditNickname);
            copyId.onClick.AddListener(CopyId);
            foreach (RowView row in achievementRows)
            {
                string id = row.Id;
                row.Button.onClick.AddListener(() => ToggleShowcase(id));
            }
        }

        protected override void OnShown()
        {
            services.Profile.Changed += Refresh;
            Loc.Changed += Refresh;
            tabs.Select(Mathf.Max(0, tabs.Selected), false);
            ShowPage(Mathf.Max(0, tabs.Selected));
            Refresh();
        }

        protected override void OnHidden()
        {
            services.Profile.Changed -= Refresh;
            Loc.Changed -= Refresh;
        }

        private void ShowPage(int index)
        {
            for (int i = 0; i < pages.Length; i++) pages[i].SetActive(i == index);
        }

        private void Refresh()
        {
            ProfileState profile = services.Profile.State;
            int level = ProfileRules.Level(profile.xp, out int into, out int needed);
            nickname.text = profile.nickname;
            levelChip.text = level.ToString(Loc.Culture);
            playerId.text = Loc.Format("profile.id", profile.playerId);
            rank.text = LobbyController.RankText(profile.rankPoints);
            country.text = Loc.Get("profile.country");
            club.text = string.IsNullOrEmpty(profile.clubName) ? Loc.Get("profile.no_club") : profile.clubName;
            xpBar.Set(needed > 0 ? (float)into / needed : 1f);
            xpText.text = $"{Loc.Number(into)} / {Loc.Number(needed)} XP";
            SkinDefinition frame = services.Equipped(ItemType.Frame);
            if (frame != null && frame.Icon != null) avatarFrame.sprite = frame.Icon;

            RefreshStats(profile.stats);
            RefreshHistory(profile.history);
            RefreshAchievements(profile);
            RefreshCollection();
        }

        private void RefreshStats(ProfileStats stats)
        {
            string favourite = ProfileRules.FavouriteCue(stats);
            SkinDefinition cue = favourite != null ? services.Catalog.Find(favourite) : services.Equipped(ItemType.Cue);
            string[] values =
            {
                Loc.Number(stats.matches),
                Loc.Number(stats.wins),
                Percent(ProfileRules.WinRate(stats)),
                Loc.Number(stats.bestStreak),
                Percent(ProfileRules.Accuracy(stats)),
                Loc.Number(stats.maxBallsInOneShot),
                cue != null ? RarityStyle.ItemName(cue) : "—",
            };
            for (int i = 0; i < statValues.Length && i < values.Length; i++) statValues[i].text = values[i];
        }

        private static string Percent(float value01) => Mathf.RoundToInt(value01 * 100f).ToString(Loc.Culture) + "%";

        private void RefreshHistory(List<MatchRecord> history)
        {
            historyTemplate.gameObject.SetActive(false);
            bool empty = history.Count == 0;
            if (empty) historyEmpty.Show(null, "history.empty", "history.empty_hint");
            else historyEmpty.Hide();

            while (historyRows.Count < history.Count) historyRows.Add(Instantiate(historyTemplate, historyTemplate.transform.parent));
            for (int i = 0; i < historyRows.Count; i++)
            {
                bool used = i < history.Count;
                historyRows[i].gameObject.SetActive(used);
                if (!used) continue;
                MatchRecord match = history[i];
                VenueDefinition hall = FindVenue(match.venueId);
                string date = DateTimeOffset.FromUnixTimeSeconds(match.unixTime).ToLocalTime().ToString("dd.MM.yyyy HH:mm", Loc.Culture);
                string where = hall != null ? LobbyController.VenueName(hall) : match.venueId;
                historyRows[i].Set(Loc.Format("history.vs", OpponentName(match.opponent)), string.IsNullOrEmpty(where) ? date : $"{where}  ·  {date}",
                    Loc.Get(match.won ? "history.won" : "history.lost").ToUpper(Loc.Culture));
                historyRows[i].SetChip(match.won ? theme.success : theme.danger);
            }
        }

        // Hot-seat saves seat 2 as the guest name in the language of that day; show it in today's language.
        // shortcut: matches the three shipped translations of "hud.guest"; store a marker instead once names come from the server (M2).
        private static readonly string[] GuestNames = { "Mehmon", "Гость", "Guest" };

        private static string OpponentName(string saved) => Array.IndexOf(GuestNames, saved) >= 0 ? Loc.Get("hud.guest") : saved;

        private void RefreshAchievements(ProfileState profile)
        {
            foreach (RowView row in achievementRows)
            {
                AchievementDef def = Achievements.Find(row.Id);
                if (def == null) continue;
                bool done = def.IsDone(profile);
                bool shown = profile.showcase.Contains(def.Id);
                row.Set(Loc.Get("achievement." + def.Id), Loc.Get("achievement." + def.Id + ".desc"), $"{def.Current(profile)}/{def.Target}");
                row.SetProgress((float)def.Current(profile) / def.Target, done);
                row.SetIcon(null, done ? (shown ? theme.accent : theme.textPrimary) : theme.textMuted);
            }

            List<AchievementDef> showcase = Achievements.Showcase(profile, showcaseIcons.Length);
            for (int i = 0; i < showcaseIcons.Length; i++)
            {
                bool used = i < showcase.Count;
                showcaseIcons[i].transform.parent.gameObject.SetActive(used);
                if (!used) continue;
                showcaseNames[i].text = Loc.Get("achievement." + showcase[i].Id);
                showcaseIcons[i].sprite = IconOf(showcase[i].Id);
            }

            showcaseHint.gameObject.SetActive(showcase.Count == 0);
        }

        private void RefreshCollection()
        {
            IEconomyService economy = services.Economy;
            foreach (RowView row in collectionRows)
            {
                if (!Enum.TryParse(row.Id, out ItemType type)) continue;
                CollectionProgress.Count(economy.Catalog, economy.Owns, type, out int owned, out int total);
                row.Set(Loc.Get("category." + type.ToString().ToLowerInvariant()), null, Loc.Format("collection.progress", owned, total));
                row.SetProgress(total > 0 ? (float)owned / total : 0f, total > 0 && owned == total);
            }
        }

        /// <summary>Tap an unlocked achievement to pin or unpin it on the showcase (three slots).</summary>
        private void ToggleShowcase(string id)
        {
            ProfileState profile = services.Profile.State;
            AchievementDef def = Achievements.Find(id);
            if (def == null || !def.IsDone(profile)) return;
            var pinned = new List<string>(profile.showcase);
            if (!pinned.Remove(id))
            {
                if (pinned.Count >= showcaseIcons.Length) pinned.RemoveAt(0);
                pinned.Add(id);
            }

            services.Profile.SetShowcase(pinned.ToArray());
        }

        private void EditNickname() =>
            Dialogs.Instance.Input("profile.edit_nick", services.Profile.State.nickname, ProfileRules.MaxNickname, services.Profile.SetNickname);

        private void CopyId()
        {
            GUIUtility.systemCopyBuffer = services.Profile.State.playerId;
            Dialogs.Instance.Toast(Loc.Get("common.copied"));
        }

        private Sprite IconOf(string achievementId)
        {
            foreach (RowView row in achievementRows)
            {
                if (row.Id == achievementId) return row.IconSprite;
            }

            return null;
        }

        private VenueDefinition FindVenue(string id)
        {
            foreach (VenueDefinition hall in services.Catalog.Venues)
            {
                if (hall.Id == id) return hall;
            }

            return null;
        }
    }
}
