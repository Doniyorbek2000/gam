using System.Collections.Generic;
using BilliardGame.Meta;
using UnityEngine;

namespace BilliardGame.Content
{
    /// <summary>Every cosmetic and venue the client knows about. M4 replaces the direct references with Addressables keys.</summary>
    [CreateAssetMenu(menuName = "BilliardGame/Skin Catalog")]
    public sealed class SkinCatalog : ScriptableObject
    {
        [SerializeField] private SkinDefinition[] skins = new SkinDefinition[0];
        [SerializeField] private VenueDefinition[] venues = new VenueDefinition[0];
        [SerializeField] private CharacterDefinition[] characters = new CharacterDefinition[0];

        public IReadOnlyList<SkinDefinition> Skins => skins;
        public IReadOnlyList<VenueDefinition> Venues => venues;
        public IReadOnlyList<CharacterDefinition> Characters => characters;

        /// <summary>The character with <paramref name="id"/>, else the first one (null only when there are none).</summary>
        public CharacterDefinition FindCharacter(string id)
        {
            foreach (CharacterDefinition c in characters)
            {
                if (c != null && c.Id == id) return c;
            }

            return characters.Length > 0 ? characters[0] : null;
        }

        /// <summary>A character different from <paramref name="avoidId"/> (hot-seat player 2, opponents without one).</summary>
        public CharacterDefinition OtherCharacter(string avoidId)
        {
            CharacterDefinition own = FindCharacter(avoidId);
            foreach (CharacterDefinition c in characters)
            {
                if (c != null && c != own) return c;
            }

            return own;
        }

        public void ConfigureCharacters(CharacterDefinition[] allCharacters) => characters = allCharacters;

        public SkinDefinition Find(string id)
        {
            foreach (SkinDefinition skin in skins)
            {
                if (skin != null && skin.Id == id) return skin;
            }

            return null;
        }

        public CatalogItem[] ToCatalogItems()
        {
            var items = new CatalogItem[skins.Length];
            for (int i = 0; i < skins.Length; i++) items[i] = skins[i].ToCatalogItem();
            return items;
        }

        public void Configure(SkinDefinition[] allSkins, VenueDefinition[] allVenues)
        {
            skins = allSkins;
            venues = allVenues;
        }
    }
}
