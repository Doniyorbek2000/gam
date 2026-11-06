using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BilliardGame.Meta
{
    /// <summary>
    /// Client view of the player's economy. The client only displays it (CLAUDE.md rule 3): the M1 implementation is
    /// <see cref="MockEconomyService"/> (local file), M3 swaps in a Nakama-backed one with the same interface.
    /// </summary>
    public interface IEconomyService
    {
        event Action Changed;

        int Coins { get; }
        int Gems { get; }
        int Shards { get; }
        int Crystals { get; }
        IReadOnlyList<CrateDefinition> Crates { get; }
        IReadOnlyList<CatalogItem> Catalog { get; }

        bool Owns(string itemId);
        string EquippedId(ItemType type);
        /// <summary>Sticker shown in <paramref name="slot"/>, or null.</summary>
        string EquippedSticker(StickerSlot slot);

        /// <summary>Opens left until <paramref name="rule"/> guarantees its rarity (1 = the next open).</summary>
        int PityRemaining(string crateId, PityRule rule);

        Task<CrateOpenResult> OpenCrateAsync(string crateId, int count);
        Task<bool> EquipAsync(string itemId);
        /// <summary>Puts an owned sticker on the cue or the table rail; null clears the slot.</summary>
        Task<bool> EquipStickerAsync(string itemId, StickerSlot slot);
    }

    public sealed class CrateDrop
    {
        public CrateDrop(CatalogItem item, bool isNew, int shards, int crystals)
        {
            Item = item;
            IsNew = isNew;
            Shards = shards;
            Crystals = crystals;
        }

        public CatalogItem Item { get; }
        public bool IsNew { get; }
        public int Shards { get; }
        public int Crystals { get; }
    }

    public sealed class CrateOpenResult
    {
        private CrateOpenResult(string error, IReadOnlyList<CrateDrop> drops)
        {
            Error = error;
            Drops = drops;
        }

        public bool Success => Error == null;
        public string Error { get; }
        public IReadOnlyList<CrateDrop> Drops { get; }

        public static CrateOpenResult Ok(IReadOnlyList<CrateDrop> drops) => new CrateOpenResult(null, drops);
        public static CrateOpenResult Fail(string error) => new CrateOpenResult(error, Array.Empty<CrateDrop>());
    }

    /// <summary>Persisted mock profile (JSON). Public fields so Unity's JsonUtility can serialize it.</summary>
    [Serializable]
    public sealed class EconomyState
    {
        public int version = 1;
        public int coins;
        public int gems;
        public int shards;
        public int crystals;
        public List<string> owned = new List<string>();
        public string equippedCue;
        public string equippedTable;
        /// <summary>Equipped items for every other slot ("Frame", "RoomWall", "Sticker:Cue", ...).</summary>
        public List<SlotItem> slots = new List<SlotItem>();
        public List<PityCounter> pity = new List<PityCounter>();
        public long seed;
        public long rollCounter;
    }

    [Serializable]
    public sealed class SlotItem
    {
        public string slot;
        public string itemId;
    }

    [Serializable]
    public sealed class PityCounter
    {
        public string key;
        public int misses;
    }
}
