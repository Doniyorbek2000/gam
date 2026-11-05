using System.Collections.Generic;
using BilliardGame.Meta;
using UnityEngine;

namespace BilliardGame.Content
{
    /// <summary>Every cosmetic and venue the client knows about. M3 replaces the direct references with Addressables keys.</summary>
    [CreateAssetMenu(menuName = "BilliardGame/Skin Catalog")]
    public sealed class SkinCatalog : ScriptableObject
    {
        [SerializeField] private SkinDefinition[] skins = new SkinDefinition[0];
        [SerializeField] private VenueDefinition[] venues = new VenueDefinition[0];

        public IReadOnlyList<SkinDefinition> Skins => skins;
        public IReadOnlyList<VenueDefinition> Venues => venues;

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
