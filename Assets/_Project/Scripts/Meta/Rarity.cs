namespace BilliardGame.Meta
{
    /// <summary>Cosmetic tier (docs/ECONOMY.md §3). Order matters: higher value = rarer.</summary>
    public enum Rarity
    {
        Common = 0,
        Rare = 1,
        Epic = 2,
        Legendary = 3,
        Mythic = 4,
    }

    /// <summary>Inventory slot an item equips into. Cosmetic only: nothing here reaches ShotParams or PhysicsConfig.</summary>
    public enum ItemType
    {
        Cue = 0,
        Table = 1,
        Balls = 2,
        VenueTheme = 3,
        Outfit = 4,
        Frame = 5,
        Emote = 6,
        Sticker = 7,
        Effect = 8,
        RoomWall = 9,
        RoomFloor = 10,
    }

    /// <summary>Where an equipped sticker is shown (decal on the cue butt or the table rail). Chat stickers need only ownership.</summary>
    public enum StickerSlot
    {
        Cue = 0,
        Table = 1,
    }

    public static class RarityInfo
    {
        public const int Count = 5;

        /// <summary>Shards granted for a duplicate (docs/ECONOMY.md §4.3).</summary>
        public static int DuplicateShards(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Common: return 5;
                case Rarity.Rare: return 20;
                case Rarity.Epic: return 60;
                case Rarity.Legendary: return 250;
                default: return 800;
            }
        }

        /// <summary>Crystals granted for a duplicate; only Mythic gives any.</summary>
        public static int DuplicateCrystals(Rarity rarity) => rarity == Rarity.Mythic ? 10 : 0;
    }

    /// <summary>What the economy needs to know about an item; visuals live in BilliardGame.Content.</summary>
    public readonly struct CatalogItem
    {
        public CatalogItem(string id, ItemType type, Rarity rarity, bool ownedByDefault)
        {
            Id = id;
            Type = type;
            Rarity = rarity;
            OwnedByDefault = ownedByDefault;
        }

        public string Id { get; }
        public ItemType Type { get; }
        public Rarity Rarity { get; }
        public bool OwnedByDefault { get; }
    }
}
