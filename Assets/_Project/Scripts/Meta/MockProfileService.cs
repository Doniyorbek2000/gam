using System;

namespace BilliardGame.Meta
{
    /// <summary>
    /// Offline profile (nickname, ID, XP, stats, history, room). Like <see cref="MockEconomyService"/> it is a local stand-in:
    /// M3 moves the profile to Nakama, M8 adds real ranked points.
    /// </summary>
    public sealed class MockProfileService
    {
        private readonly Action<ProfileState> persist;

        public MockProfileService(ProfileState saved, Action<ProfileState> persist, Random random = null)
        {
            this.persist = persist;
            State = saved ?? new ProfileState();
            random = random ?? new Random();
            bool generated = false;
            if (string.IsNullOrEmpty(State.playerId))
            {
                State.playerId = random.Next(10_000_000, 100_000_000).ToString();
                generated = true;
            }

            if (ProfileRules.ValidateNickname(State.nickname) != null)
            {
                State.nickname = "Player" + random.Next(1000, 10000);
                generated = true;
            }
            if (State.stats == null) State.stats = new ProfileStats();
            if (State.history == null) State.history = new System.Collections.Generic.List<MatchRecord>();
            if (State.showcase == null) State.showcase = new System.Collections.Generic.List<string>();
            if (State.room == null) State.room = new RoomLayout();
            // Save the generated ID/nickname at once, otherwise every launch invents new ones until something else saves.
            if (generated) persist?.Invoke(State);
        }

        public event Action Changed;

        public ProfileState State { get; }

        public int Level => ProfileRules.Level(State.xp, out _, out _);

        /// <summary>Null on success, else the localization key of the problem.</summary>
        public string SetNickname(string name)
        {
            string error = ProfileRules.ValidateNickname(name);
            if (error != null) return error;
            State.nickname = name.Trim();
            Commit();
            return null;
        }

        public void RecordMatch(MatchRecord match)
        {
            ProfileRules.Record(State, match);
            Commit();
        }

        public void SetRoom(RoomLayout layout)
        {
            State.room = layout.Clone();
            Commit();
        }

        public void SetShowcase(params string[] achievementIds)
        {
            State.showcase.Clear();
            State.showcase.AddRange(achievementIds);
            Commit();
        }

        private void Commit()
        {
            persist?.Invoke(State);
            Changed?.Invoke();
        }
    }
}
