using BilliardGame.Meta;
using UnityEngine;

namespace BilliardGame.UI
{
    /// <summary>Rarity colour + shape (dots), so rarity never relies on colour alone (docs/ECONOMY.md §3).</summary>
    public static class RarityStyle
    {
        public static Color Color(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Rare: return Hex(0x3D8BFF);
                case Rarity.Epic: return Hex(0xA64DFF);
                case Rarity.Legendary: return Hex(0xFFB020);
                case Rarity.Mythic: return Hex(0xFF3B5C);
                default: return Hex(0x9AA3AE);
            }
        }

        public static string Name(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Rare: return "Noyob";
                case Rarity.Epic: return "Epik";
                case Rarity.Legendary: return "Afsonaviy";
                case Rarity.Mythic: return "Mifik";
                default: return "Oddiy";
            }
        }

        /// <summary>"Epik •••" — Mythic gets five dots in place of the crown glyph the UI font lacks.</summary>
        public static string Label(Rarity rarity) => Name(rarity) + " " + new string('•', (int)rarity + 1);

        public static string Tagged(Rarity rarity, string text) => $"<color=#{ColorUtility.ToHtmlStringRGB(Color(rarity))}>{text}</color>";

        private static Color Hex(int rgb) => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f);
    }
}
