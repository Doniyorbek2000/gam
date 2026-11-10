using BilliardGame.Meta;
using TMPro;
using UnityEngine;

namespace BilliardGame.UI
{
    /// <summary>
    /// Missions (docs/GDD.md §6.4, UI_UX §11): daily and weekly tabs, progress from server-run matches, Claim (coins +
    /// Pass XP, doubled coins during a coin event) and one free daily reroll. All from the server.
    /// </summary>
    public sealed class MissionsScreen : LiveOpsScreen
    {
        [SerializeField] private TabBar tabs;
        [SerializeField] private MissionRowView[] rows = new MissionRowView[0];
        [SerializeField] private TMP_Text resetLabel;
        [SerializeField] private TMP_Text hintLabel;

        private bool weekly;

        protected override void Awake()
        {
            base.Awake();
            tabs.Changed += index =>
            {
                weekly = index == 1;
                if (LiveOps?.State != null) Render();
            };
        }

        protected override void OnShown()
        {
            tabs.Select(weekly ? 1 : 0, false);
            base.OnShown();
        }

        protected override void Render()
        {
            MissionBoard board = LiveOps.State.missions;
            MissionInfo[] list = weekly ? board.weekly : board.daily;
            for (int i = 0; i < rows.Length; i++)
            {
                bool used = i < list.Length;
                rows[i].gameObject.SetActive(used);
                if (!used) continue;
                MissionInfo mission = list[i];
                bool canReroll = !weekly && board.rerollFree && !mission.claimed;
                rows[i].Bind(mission, LiveOpsText.MissionTitle(mission, services.Catalog), canReroll,
                    () => Run(LiveOps.ClaimMissionAsync(weekly, mission.index), Loc.Format("missions.claimed", Loc.Number(mission.coins), Loc.Number(mission.xp))),
                    () => Run(LiveOps.RerollMissionAsync(mission.index), Loc.Get("missions.rerolled")));
            }

            hintLabel.text = Loc.Get(weekly ? "missions.hint_weekly" : board.rerollFree ? "missions.hint_reroll" : "missions.hint_daily");
        }

        protected override void Tick()
        {
            MissionBoard board = LiveOps.State.missions;
            resetLabel.text = Loc.Format("missions.reset", LiveOpsText.Countdown(LiveOps.Remaining(weekly ? board.weekResetInMs : board.dayResetInMs)));
        }
    }
}
