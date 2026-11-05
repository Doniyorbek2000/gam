using BilliardGame.Meta;
using UnityEngine;

namespace BilliardGame.Content
{
    /// <summary>
    /// One cosmetic (docs/ART_PIPELINE.md §5). Cue: <see cref="Prefab"/> with its pivot at the tip and the butt along +Z.
    /// Table: cloth and rail materials applied to the shared table. Purely visual: never read by Physics or Gameplay.
    /// </summary>
    [CreateAssetMenu(menuName = "BilliardGame/Skin Definition")]
    public sealed class SkinDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private ItemType type;
        [SerializeField] private Rarity rarity;
        [SerializeField] private string displayName;
        [SerializeField] private bool ownedByDefault;
        [SerializeField] private GameObject prefab;
        [SerializeField] private Material cloth;
        [SerializeField] private Material rail;
        [SerializeField] private Sprite icon;
        [SerializeField] private string unlockSource = "Crate";

        public string Id => id;
        public ItemType Type => type;
        public Rarity Rarity => rarity;
        public string DisplayName => displayName;
        public GameObject Prefab => prefab;
        public Material Cloth => cloth;
        public Material Rail => rail;
        public Sprite Icon => icon;
        public string UnlockSource => unlockSource;

        public CatalogItem ToCatalogItem() => new CatalogItem(id, type, rarity, ownedByDefault);

        /// <summary>Editor-side setup used by the content builder and the art import tool.</summary>
        public void Configure(string skinId, ItemType skinType, Rarity skinRarity, string name, bool isDefault,
            GameObject model, Material clothMaterial, Material railMaterial, Sprite preview, string source)
        {
            id = skinId;
            type = skinType;
            rarity = skinRarity;
            displayName = name;
            ownedByDefault = isDefault;
            prefab = model;
            cloth = clothMaterial;
            rail = railMaterial;
            icon = preview;
            unlockSource = source;
        }

        public void SetIcon(Sprite preview) => icon = preview;
    }
}
