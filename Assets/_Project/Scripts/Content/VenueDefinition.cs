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

        public string Id => id;
        public string DisplayName => displayName;
        public string SceneName => sceneName;
        public Sprite Preview => preview;

        public void Configure(string venueId, string name, string scene, Sprite image)
        {
            id = venueId;
            displayName = name;
            sceneName = scene;
            preview = image;
        }
    }
}
