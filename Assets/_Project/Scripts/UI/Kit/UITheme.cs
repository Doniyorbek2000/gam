using BilliardGame.Meta;
using TMPro;
using UnityEngine;

namespace BilliardGame.UI
{
    public enum TextStyle
    {
        Display = 0,
        H1 = 1,
        H2 = 2,
        Body = 3,
        Caption = 4,
        Button = 5,
        Number = 6,
    }

    /// <summary>
    /// Design tokens (docs/DESIGN_SYSTEM.md): colours, type scale, 8 pt spacing, radii, shadow and glow. Every screen builder
    /// reads from this asset, so a token change re-skins the whole UI after "Build Main Scene".
    /// </summary>
    [CreateAssetMenu(menuName = "BilliardGame/UI Theme")]
    public sealed class UITheme : ScriptableObject
    {
        public const float Unit = 8f;
        /// <summary>Minimum touch target: 48 dp ≈ 72 reference px at 1080 p on a 420 dpi phone; we use 88 for primary controls.</summary>
        public const float MinTouch = 72f;

        [Header("Surfaces")]
        public Color background = Hex(0x0B0F1A);
        public Color surface = Hex(0x151B2B, 0.92f);
        public Color surfaceRaised = Hex(0x1E2638);
        public Color surfaceSunken = Hex(0x0E1320, 0.9f);
        public Color stroke = Hex(0xFFFFFF, 0.08f);
        public Color scrim = Hex(0x05070C, 0.72f);

        [Header("Text")]
        public Color textPrimary = Hex(0xF4F6FB);
        public Color textSecondary = Hex(0xA7B0C4);
        public Color textMuted = Hex(0x6B7489);
        public Color textOnAccent = Hex(0x1A1204);

        [Header("Accents")]
        public Color accent = Hex(0xFFC24A);
        public Color accentDeep = Hex(0xFF7A00);
        public Color secondary = Hex(0x2E7BFF);
        public Color success = Hex(0x2ECC71);
        public Color warning = Hex(0xFFB020);
        public Color danger = Hex(0xFF3B5C);
        public Color coin = Hex(0xFFD15C);
        public Color gem = Hex(0x5CD6FF);

        [Header("Rarity (docs/ECONOMY.md §3)")]
        public Color common = Hex(0x9AA3AE);
        public Color rare = Hex(0x3D8BFF);
        public Color epic = Hex(0xA64DFF);
        public Color legendary = Hex(0xFFB020);
        public Color mythic = Hex(0xFF3B5C);
        public Color mythicEnd = Hex(0xFF8A3D);

        [Header("Type (Montserrat, Latin + Cyrillic)")]
        public TMP_FontAsset bold;
        public TMP_FontAsset semiBold;
        public TMP_FontAsset medium;
        public float displaySize = 72f;
        public float h1Size = 48f;
        public float h2Size = 36f;
        public float bodySize = 28f;
        public float captionSize = 22f;
        public float buttonSize = 30f;

        [Header("Shape")]
        public float radiusSmall = 12f;
        public float radiusMedium = 20f;
        public float radiusLarge = 32f;
        public Color shadow = Hex(0x000000, 0.45f);
        public float shadowOffset = 6f;
        public float glowStrength = 0.85f;

        [Header("Motion (seconds)")]
        public float pressDuration = 0.08f;
        public float pressScale = 0.95f;
        public float screenDuration = 0.25f;
        public float modalDuration = 0.22f;
        public float countUpDuration = 0.6f;

        public static float Space(int units) => units * Unit;

        public Color Rarity(Rarity rarity)
        {
            switch (rarity)
            {
                case Meta.Rarity.Rare: return rare;
                case Meta.Rarity.Epic: return epic;
                case Meta.Rarity.Legendary: return legendary;
                case Meta.Rarity.Mythic: return mythic;
                default: return common;
            }
        }

        public TMP_FontAsset Font(TextStyle style)
        {
            switch (style)
            {
                case TextStyle.Body:
                case TextStyle.Caption:
                    return medium != null ? medium : semiBold;
                case TextStyle.H2:
                    return semiBold != null ? semiBold : bold;
                default:
                    return bold != null ? bold : semiBold;
            }
        }

        public float Size(TextStyle style)
        {
            switch (style)
            {
                case TextStyle.Display: return displaySize;
                case TextStyle.H1: return h1Size;
                case TextStyle.H2: return h2Size;
                case TextStyle.Caption: return captionSize;
                case TextStyle.Button: return buttonSize;
                case TextStyle.Number: return h2Size;
                default: return bodySize;
            }
        }

        public static Color Hex(int rgb, float alpha = 1f) => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, alpha);
    }
}
