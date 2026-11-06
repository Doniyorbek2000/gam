using System;

namespace BilliardGame.Meta
{
    public enum Currency
    {
        Coin,
        Gem,
    }

    /// <summary>
    /// One crate table (docs/ECONOMY.md §5.1). Deserialized from Resources/Economy/crates.json, which stands in for the
    /// server table until M3; the odds window is generated from it, never hand-written (CLAUDE.md rule 6).
    /// </summary>
    [Serializable]
    public sealed class CrateDefinition
    {
        public string id;
        public string displayName;
        public string currency;
        public int price;
        public int price10;
        /// <summary>Percent per rarity, indexed by <see cref="Rarity"/>; must sum to 100.</summary>
        public float[] weights;
        public PityRule[] pity;
        /// <summary>Rarity name guaranteed at least once in a 10× open, or empty.</summary>
        public string tenGuarantee;

        public Currency Currency => currency == "gem" ? Currency.Gem : Currency.Coin;

        public bool HasTenGuarantee(out Rarity rarity) => TryParse(tenGuarantee, out rarity);

        public int Price(int count) => count == 10 && price10 > 0 ? price10 : price * count;

        internal static bool TryParse(string name, out Rarity rarity)
        {
            rarity = Rarity.Common;
            return !string.IsNullOrEmpty(name) && Enum.TryParse(name, out rarity);
        }

        /// <summary>Throws when the table cannot be used, so a bad config fails at load and not mid-open.</summary>
        public void Validate()
        {
            if (string.IsNullOrEmpty(id)) throw new FormatException("crate without id");
            if (weights == null || weights.Length != RarityInfo.Count) throw new FormatException($"{id}: weights must have {RarityInfo.Count} entries");
            float sum = 0f;
            foreach (float w in weights)
            {
                if (w < 0f) throw new FormatException($"{id}: negative weight");
                sum += w;
            }

            if (Math.Abs(sum - 100f) > 0.001f) throw new FormatException($"{id}: weights sum to {sum}, not 100");
            if (price <= 0) throw new FormatException($"{id}: price must be positive");
            foreach (PityRule rule in pity ?? Array.Empty<PityRule>())
            {
                if (!TryParse(rule.minRarity, out _) || rule.count < 1) throw new FormatException($"{id}: bad pity rule {rule.minRarity}/{rule.count}");
            }

            if (!string.IsNullOrEmpty(tenGuarantee) && !TryParse(tenGuarantee, out _)) throw new FormatException($"{id}: bad tenGuarantee");
        }
    }

    /// <summary>The <see cref="count"/>-th open in a row without <see cref="minRarity"/> or better is forced to it.</summary>
    [Serializable]
    public sealed class PityRule
    {
        public string minRarity;
        public int count;

        public Rarity MinRarity
        {
            get
            {
                CrateDefinition.TryParse(minRarity, out Rarity r);
                return r;
            }
        }
    }

    [Serializable]
    public sealed class CrateTableSet
    {
        public CrateDefinition[] crates;
    }
}
