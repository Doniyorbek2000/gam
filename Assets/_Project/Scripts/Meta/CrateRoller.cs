using System;

namespace BilliardGame.Meta
{
    /// <summary>Pure crate maths shared by the mock and (in M3) the server: rarity roll, deterministic RNG, odds.</summary>
    public static class CrateRoller
    {
        /// <summary>Picks a rarity from <paramref name="roll"/> in [0,1), considering only rarities at or above <paramref name="floor"/>.</summary>
        public static Rarity RollRarity(float[] weights, double roll, Rarity floor)
        {
            double total = 0;
            for (int i = (int)floor; i < weights.Length; i++) total += weights[i];
            if (total <= 0) return floor;

            double target = roll * total;
            double cumulative = 0;
            int last = (int)floor;
            for (int i = (int)floor; i < weights.Length; i++)
            {
                if (weights[i] <= 0) continue;
                last = i;
                cumulative += weights[i];
                if (target < cumulative) return (Rarity)i;
            }

            return (Rarity)last;
        }

        /// <summary>Deterministic [0,1) from (seed, counter) via SplitMix64; mirrors the server's counter-based HMAC roll.</summary>
        public static double Roll(ulong seed, ulong counter)
        {
            ulong z = seed + (counter + 1) * 0x9E3779B97F4A7C15UL;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            z ^= z >> 31;
            return (z >> 11) * (1.0 / (1UL << 53));
        }

        /// <summary>Base chance of <paramref name="min"/> or better, in percent.</summary>
        public static double ChanceAtLeast(float[] weights, Rarity min)
        {
            double sum = 0;
            for (int i = (int)min; i < weights.Length; i++) sum += weights[i];
            return sum;
        }

        /// <summary>Consolidated chance (0..1) with hard pity N (docs/ECONOMY.md §5.2): 1 / E, E = (1 − (1−p)^N) / p.</summary>
        public static double EffectiveChance(double p, int n)
        {
            if (p <= 0) return 1.0 / n;
            double expected = (1 - Math.Pow(1 - p, n)) / p;
            return 1 / expected;
        }
    }
}
