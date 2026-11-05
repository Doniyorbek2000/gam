using UnityEngine;

namespace BilliardGame.Content
{
    /// <summary>A hall the table is played in. Empty <see cref="SceneName"/> = the garage built into the Main scene.</summary>
    [CreateAssetMenu(menuName = "BilliardGame/Venue Definition")]
    public sealed class VenueDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private string sceneName;
        [SerializeField] private Sprite preview;
        [SerializeField] private int entryFee;
        [SerializeField] private int prize;
        [SerializeField] private int minLevel = 1;
        [SerializeField] private bool comingSoon;

        public string Id => id;
        public string DisplayName => displayName;
        public string SceneName => sceneName;
        public Sprite Preview => preview;
        /// <summary>Coins staked per player (docs/GDD.md §3). Escrow moves to the server in M3.</summary>
        public int EntryFee => entryFee;
        public int Prize => prize;
        public int MinLevel => minLevel;
        /// <summary>Shown locked in the venue carousel; not playable yet.</summary>
        public bool ComingSoon => comingSoon;

        public void ConfigureEconomy(int entry, int winnerPrize, int level, bool soon, Sprite image)
        {
            entryFee = entry;
            prize = winnerPrize;
            minLevel = level;
            comingSoon = soon;
            if (image != null) preview = image;
        }

        public void Configure(string venueId, string name, string scene, Sprite image)
        {
            id = venueId;
            displayName = name;
            sceneName = scene;
            preview = image;
        }
    }
}
