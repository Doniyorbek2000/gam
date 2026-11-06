using System;

namespace BilliardGame.Meta
{
    /// <summary>One crate drop before it is granted: the rarity rolled and the index into that rarity's item pool.</summary>
    public readonly struct CratePick
    {
        public CratePick(Rarity rarity, int poolIndex)
        {
            Rarity = rarity;
            PoolIndex = poolIndex;
        }

        public Rarity Rarity { get; }
        public int PoolIndex { get; }
    }

    /// <summary>
    /// The crate open rules (docs/ECONOMY.md §5): pity floors, the 10× guarantee, rarity roll, item pick. Pure, so the
    /// mock and the server (Server/nakama/modules/src/economy.ts <c>crateRolls</c>) run the same maths; Tools/CrossCheck
    /// writes fixtures that the TypeScript port must reproduce exactly.
    /// </summary>
    public static class CrateOpening
    {
        /// <param name="poolSizes">Catalog items per rarity (index = <see cref="Rarity"/>), in catalog order.</param>
        /// <param name="misses">Opens without the rarity of each <c>crate.pity</c> rule; updated in place.</param>
        /// <param name="roll">Uniform [0,1); called twice per drop (rarity, then item).</param>
        public static CratePick[] Open(CrateDefinition crate, int[] poolSizes, int[] misses, int count, Func<double> roll)
        {
            if (crate == null) throw new ArgumentNullException(nameof(crate));
            PityRule[] rules = crate.pity ?? Array.Empty<PityRule>();
            if (misses == null || misses.Length != rules.Length) throw new ArgumentException("one miss counter per pity rule", nameof(misses));

            bool hasTen = crate.HasTenGuarantee(out Rarity tenRarity) && count == 10;
            bool tenMet = false;
            var picks = new CratePick[count];
            for (int i = 0; i < count; i++)
            {
                Rarity floor = Rarity.Common;
                for (int k = 0; k < rules.Length; k++)
                {
                    if (misses[k] + 1 >= rules[k].count && rules[k].MinRarity > floor) floor = rules[k].MinRarity;
                }

                if (hasTen && !tenMet && i == count - 1 && tenRarity > floor) floor = tenRarity;
                Rarity rarity = CrateRoller.RollRarity(crate.weights, roll(), floor);
                if (hasTen && rarity >= tenRarity) tenMet = true;
                for (int k = 0; k < rules.Length; k++) misses[k] = rarity >= rules[k].MinRarity ? 0 : misses[k] + 1;

                int size = poolSizes[(int)rarity];
                if (size <= 0) throw new InvalidOperationException($"{crate.id}: no items for {rarity}");
                picks[i] = new CratePick(rarity, Math.Min(size - 1, (int)(roll() * size)));
            }

            return picks;
        }
    }
}
