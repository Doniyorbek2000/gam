using BilliardGame.Content;
using BilliardGame.Meta;
using UnityEngine;

namespace BilliardGame.UI
{
    /// <summary>Rarity colour + shape (pips), so rarity never relies on colour alone (docs/ECONOMY.md §3).</summary>
    public static class RarityStyle
    {
        /// <summary>Same values as the <see cref="UITheme"/> rarity tokens; static so editor builders and runtime agree.</summary>
        public static Color Color(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Rare: return UITheme.Hex(0x3D8BFF);
                case Rarity.Epic: return UITheme.Hex(0xA64DFF);
                case Rarity.Legendary: return UITheme.Hex(0xFFB020);
                case Rarity.Mythic: return UITheme.Hex(0xFF3B5C);
                default: return UITheme.Hex(0x9AA3AE);
            }
        }

        public static string Name(Rarity rarity) => Loc.Get("rarity." + rarity.ToString().ToLowerInvariant());

        /// <summary>"Epik •••" — the dot count is the shape cue next to the colour.</summary>
        public static string Label(Rarity rarity) => Name(rarity) + " " + new string('•', (int)rarity + 1);

        public static string Tagged(Rarity rarity, string text) => $"<color=#{ColorUtility.ToHtmlStringRGB(Color(rarity))}>{text}</color>";

        /// <summary>Localized item name (key item.&lt;id&gt;), falling back to the catalog name.</summary>
        public static string ItemName(SkinDefinition item) => item == null ? string.Empty : Loc.Get("item." + item.Id, item.DisplayName);
    }
}
