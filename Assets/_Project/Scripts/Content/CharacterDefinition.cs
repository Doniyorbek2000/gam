using UnityEngine;

namespace BilliardGame.Content
{
    /// <summary>A selectable 3D player character (docs/GDD.md §8). Purely visual: never reaches ShotParams or the rules.</summary>
    [CreateAssetMenu(menuName = "BilliardGame/Character")]
    public sealed class CharacterDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private bool female;
        [Tooltip("Humanoid prefab with CharacterRig (built by BilliardGame ▸ Build Characters).")]
        [SerializeField] private GameObject prefab;
        [SerializeField] private Sprite portrait;

        public string Id => id;
        public string DisplayName => displayName;
        public bool Female => female;
        public GameObject Prefab => prefab;
        public Sprite Portrait => portrait;

        public void Configure(string characterId, string name, bool isFemale, GameObject characterPrefab, Sprite characterPortrait)
        {
            id = characterId;
            displayName = name;
            female = isFemale;
            prefab = characterPrefab;
            portrait = characterPortrait;
        }
    }
}
