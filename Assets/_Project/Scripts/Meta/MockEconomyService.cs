using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BilliardGame.Meta
{
    /// <summary>
    /// Offline economy for the editor and M1 (CLAUDE.md rule 3 allows a mock there). Same rules the server will run in M3:
    /// price check, rarity roll with pity floors, 10× guarantee, duplicate → shards.
    /// </summary>
    public sealed class MockEconomyService : IEconomyService
    {
        public const int StartCoins = 5000;
        public const int StartGems = 500;

        private readonly EconomyState state;
        private readonly CatalogItem[] catalog;
        private readonly CrateDefinition[] crates;
        private readonly Action<EconomyState> persist;
        private readonly Func<double> rollOverride;
        private readonly HashSet<string> owned;

        /// <param name="saved">Previous state, or null for a fresh profile.</param>
        /// <param name="persist">Called after every change; may be null.</param>
        /// <param name="rollOverride">Test hook replacing the deterministic RNG.</param>
        public MockEconomyService(EconomyState saved, IReadOnlyList<CatalogItem> catalog, IReadOnlyList<CrateDefinition> crates,
            Action<EconomyState> persist, Func<double> rollOverride = null)
        {
            this.catalog = Copy(catalog ?? throw new ArgumentNullException(nameof(catalog)));
            this.crates = Copy(crates ?? throw new ArgumentNullException(nameof(crates)));
            this.persist = persist;
            this.rollOverride = rollOverride;
            foreach (CrateDefinition crate in this.crates) ValidatePools(crate);

            state = saved ?? NewProfile();
            owned = new HashSet<string>(state.owned);
            foreach (CatalogItem item in this.catalog)
            {
                if (item.OwnedByDefault && owned.Add(item.Id)) state.owned.Add(item.Id);
            }

            if (Find(state.equippedCue) == null) state.equippedCue = DefaultOf(ItemType.Cue);
            if (Find(state.equippedTable) == null) state.equippedTable = DefaultOf(ItemType.Table);
            if (state.slots == null) state.slots = new List<SlotItem>();
            foreach (ItemType type in new[] { ItemType.Frame, ItemType.RoomWall, ItemType.RoomFloor })
            {
                if (Find(Slot(type.ToString())) == null) SetSlot(type.ToString(), DefaultOf(type));
            }
        }

        public event Action Changed;

        public int Coins => state.coins;
        public int Gems => state.gems;
        public int Shards => state.shards;
        public int Crystals => state.crystals;
        public IReadOnlyList<CrateDefinition> Crates => crates;
        public IReadOnlyList<CatalogItem> Catalog => catalog;

        public bool Owns(string itemId) => itemId != null && owned.Contains(itemId);

        public string EquippedId(ItemType type)
        {
            switch (type)
            {
                case ItemType.Cue: return state.equippedCue;
                case ItemType.Table: return state.equippedTable;
                case ItemType.Sticker: return EquippedSticker(StickerSlot.Cue);
                default: return Slot(type.ToString());
            }
        }

        public string EquippedSticker(StickerSlot slot) => Slot(StickerKey(slot));

        public int PityRemaining(string crateId, PityRule rule) => rule.count - Counter(crateId, rule.MinRarity).misses;

        public Task<bool> EquipAsync(string itemId)
        {
            CatalogItem? item = Find(itemId);
            if (item == null || !Owns(itemId)) return Task.FromResult(false);
            switch (item.Value.Type)
            {
                case ItemType.Cue: state.equippedCue = itemId; break;
                case ItemType.Table: state.equippedTable = itemId; break;
                case ItemType.Sticker: SetSlot(StickerKey(StickerSlot.Cue), itemId); break;
                default: SetSlot(item.Value.Type.ToString(), itemId); break;
            }

            Commit();
            return Task.FromResult(true);
        }

        public Task<bool> EquipStickerAsync(string itemId, StickerSlot slot)
        {
            if (itemId != null)
            {
                CatalogItem? item = Find(itemId);
                if (item == null || item.Value.Type != ItemType.Sticker || !Owns(itemId)) return Task.FromResult(false);
            }

            SetSlot(StickerKey(slot), itemId);
            Commit();
            return Task.FromResult(true);
        }

        /// <summary>Mock match prize (M7 moves prizes and entry escrow to the server).</summary>
        public void GrantMatchReward(int coins)
        {
            state.coins += Math.Max(0, coins);
            Commit();
        }

        private static string StickerKey(StickerSlot slot) => "Sticker:" + slot;

        private string Slot(string key) => state.slots.Find(s => s.slot == key)?.itemId;

        private void SetSlot(string key, string itemId)
        {
            SlotItem entry = state.slots.Find(s => s.slot == key);
            if (entry == null) state.slots.Add(new SlotItem { slot = key, itemId = itemId });
            else entry.itemId = itemId;
        }

        public Task<CrateOpenResult> OpenCrateAsync(string crateId, int count) => Task.FromResult(OpenCrate(crateId, count));

        // Shop and login calendar are server features (M3); the offline mock does not fake them.
        public string PlayerId => null;
        public int ShopPrice(string itemId, Currency currency) => 0;
        public Task<string> BuyAsync(string itemId, Currency currency) => Task.FromResult("offline");
        public int LoginDay => 0;
        public bool LoginClaimable => false;
        public Task<IReadOnlyDictionary<string, int>> ClaimLoginAsync() => Task.FromResult<IReadOnlyDictionary<string, int>>(null);

        /// <summary>Editor/mock-only top-up so the crate flow is testable without payments (M5/M10).</summary>
        public void DevGrant(int coins, int gems)
        {
            state.coins += Math.Max(0, coins);
            state.gems += Math.Max(0, gems);
            Commit();
        }

        private CrateOpenResult OpenCrate(string crateId, int count)
        {
            if (count != 1 && count != 10) return CrateOpenResult.Fail("count must be 1 or 10");
            CrateDefinition crate = Array.Find(crates, c => c.id == crateId);
            if (crate == null) return CrateOpenResult.Fail("unknown crate " + crateId);

            int cost = crate.Price(count);
            if (Balance(crate.Currency) < cost) return CrateOpenResult.Fail(crate.Currency == Currency.Gem ? "Not enough gems" : "Not enough coins");
            if (crate.Currency == Currency.Gem) state.gems -= cost;
            else state.coins -= cost;

            PityRule[] rules = crate.pity ?? Array.Empty<PityRule>();
            var misses = new int[rules.Length];
            for (int k = 0; k < rules.Length; k++) misses[k] = Counter(crate.id, rules[k].MinRarity).misses;

            var pools = new List<CatalogItem>[RarityInfo.Count];
            var sizes = new int[RarityInfo.Count];
            for (int r = 0; r < pools.Length; r++) pools[r] = new List<CatalogItem>();
            foreach (CatalogItem item in catalog) pools[(int)item.Rarity].Add(item);
            for (int r = 0; r < pools.Length; r++) sizes[r] = pools[r].Count;

            CratePick[] picks = CrateOpening.Open(crate, sizes, misses, count, NextRoll);
            for (int k = 0; k < rules.Length; k++) Counter(crate.id, rules[k].MinRarity).misses = misses[k];
            var drops = new List<CrateDrop>(count);
            foreach (CratePick pick in picks) drops.Add(Grant(pools[(int)pick.Rarity][pick.PoolIndex]));

            Commit();
            return CrateOpenResult.Ok(drops);
        }

        private CrateDrop Grant(CatalogItem item)
        {
            if (owned.Add(item.Id))
            {
                state.owned.Add(item.Id);
                return new CrateDrop(item, true, 0, 0);
            }

            int shards = RarityInfo.DuplicateShards(item.Rarity);
            int crystals = RarityInfo.DuplicateCrystals(item.Rarity);
            state.shards += shards;
            state.crystals += crystals;
            return new CrateDrop(item, false, shards, crystals);
        }

        private double NextRoll() => rollOverride?.Invoke() ?? CrateRoller.Roll((ulong)state.seed, (ulong)state.rollCounter++);

        private PityCounter Counter(string crateId, Rarity rarity)
        {
            string key = crateId + ":" + rarity;
            PityCounter counter = state.pity.Find(p => p.key == key);
            if (counter != null) return counter;
            counter = new PityCounter { key = key };
            state.pity.Add(counter);
            return counter;
        }

        private int Balance(Currency currency) => currency == Currency.Gem ? state.gems : state.coins;

        private CatalogItem? Find(string id)
        {
            foreach (CatalogItem item in catalog)
            {
                if (item.Id == id) return item;
            }

            return null;
        }

        private string DefaultOf(ItemType type)
        {
            foreach (CatalogItem item in catalog)
            {
                if (item.Type == type && item.OwnedByDefault) return item.Id;
            }

            return null;
        }

        private void ValidatePools(CrateDefinition crate)
        {
            crate.Validate();
            for (int r = 0; r < RarityInfo.Count; r++)
            {
                if (crate.weights[r] > 0 && Array.FindIndex(catalog, i => (int)i.Rarity == r) < 0)
                    throw new FormatException($"{crate.id}: no catalog items for {(Rarity)r}");
            }
        }

        private void Commit()
        {
            persist?.Invoke(state);
            Changed?.Invoke();
        }

        private static EconomyState NewProfile() => new EconomyState
        {
            coins = StartCoins,
            gems = StartGems,
            seed = Guid.NewGuid().GetHashCode() ^ ((long)Environment.TickCount << 32),
        };

        private static T[] Copy<T>(IReadOnlyList<T> source)
        {
            var copy = new T[source.Count];
            for (int i = 0; i < copy.Length; i++) copy[i] = source[i];
            return copy;
        }
    }
}
