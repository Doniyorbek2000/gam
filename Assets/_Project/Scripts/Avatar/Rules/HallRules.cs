namespace BilliardGame.Avatar.Rules
{
    /// <summary>Hall life budget (ROADMAP M1.6 item 5): background NPCs by graphics quality. Visual only.</summary>
    public static class HallRules
    {
        public const int HighNpcs = 8;
        public const int MediumNpcs = 4;

        /// <summary>NPCs to spawn for quality level 0 (Low) / 1 (Medium) / 2 (High), never more than the hall has spots.</summary>
        public static int NpcCount(int qualityLevel, int spots)
        {
            int budget = qualityLevel >= 2 ? HighNpcs : qualityLevel == 1 ? MediumNpcs : 0;
            return budget < spots ? budget : spots;
        }
    }
}
