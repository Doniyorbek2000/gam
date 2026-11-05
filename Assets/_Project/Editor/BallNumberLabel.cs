using System;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore;

namespace BilliardGame.Editor
{
    /// <summary>
    /// Renders a number as an anti-aliased coverage bitmap from TMP's default SDF font atlas,
    /// which is not CPU-readable, so it is copied once through a RenderTexture.
    /// </summary>
    internal sealed class BallNumberLabel
    {
        /// <summary>Atlas pixels kept around a glyph rect so its anti-aliased edge survives the crop.</summary>
        private const float GlyphMargin = 2f;

        private readonly TMP_FontAsset font;
        private readonly byte[] sdf;
        private readonly int atlasWidth;
        private readonly int atlasHeight;

        public BallNumberLabel()
        {
            font = TMP_Settings.defaultFontAsset;
            if (font == null) throw new InvalidOperationException("TMP default font asset missing; import TMP Essential Resources.");

            Texture2D atlas = font.atlasTexture;
            atlasWidth = atlas.width;
            atlasHeight = atlas.height;
            RenderTexture rt = RenderTexture.GetTemporary(atlasWidth, atlasHeight, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            RenderTexture previous = RenderTexture.active;
            try
            {
                Graphics.Blit(atlas, rt);
                RenderTexture.active = rt;
                var copy = new Texture2D(atlasWidth, atlasHeight, TextureFormat.RGBA32, false, true);
                copy.ReadPixels(new Rect(0, 0, atlasWidth, atlasHeight), 0, 0);
                copy.Apply();
                Color32[] pixels = copy.GetPixels32();
                sdf = new byte[pixels.Length];
                for (int i = 0; i < pixels.Length; i++) sdf[i] = pixels[i].a;
                UnityEngine.Object.DestroyImmediate(copy);
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(rt);
            }
        }

        /// <summary>Square coverage map (0..1, row 0 at the bottom) with the text centred at the given height fraction.</summary>
        public float[] Render(string text, int size, float heightFraction)
        {
            var rects = new GlyphRect[text.Length];
            float gap = 0f;
            float totalWidth = 0f;
            float maxHeight = 0f;
            for (int i = 0; i < text.Length; i++)
            {
                if (!font.characterLookupTable.TryGetValue(text[i], out TMP_Character character))
                {
                    throw new InvalidOperationException($"Glyph '{text[i]}' missing from {font.name}.");
                }

                rects[i] = character.glyph.glyphRect;
                totalWidth += rects[i].width;
                maxHeight = Mathf.Max(maxHeight, rects[i].height);
            }

            gap = maxHeight * 0.08f;
            totalWidth += gap * (text.Length - 1);
            float scale = size * heightFraction / maxHeight;   // output px per atlas px
            float sharpness = 2f * font.atlasPadding * scale;   // SDF units -> one output pixel of anti-aliasing
            float originX = size * 0.5f - totalWidth * scale * 0.5f;
            float originY = size * 0.5f - maxHeight * scale * 0.5f;

            var coverage = new float[size * size];
            float cursor = 0f;
            for (int g = 0; g < rects.Length; g++)
            {
                GlyphRect r = rects[g];
                float glyphLeft = originX + cursor * scale;
                float glyphBottom = originY + (maxHeight - r.height) * 0.5f * scale;
                int pad = font.atlasPadding;
                int x0 = Mathf.Max(0, Mathf.FloorToInt(glyphLeft - pad * scale));
                int x1 = Mathf.Min(size - 1, Mathf.CeilToInt(glyphLeft + (r.width + pad) * scale));
                int y0 = Mathf.Max(0, Mathf.FloorToInt(glyphBottom - pad * scale));
                int y1 = Mathf.Min(size - 1, Mathf.CeilToInt(glyphBottom + (r.height + pad) * scale));

                for (int y = y0; y <= y1; y++)
                {
                    for (int x = x0; x <= x1; x++)
                    {
                        float ax = r.x + (x + 0.5f - glyphLeft) / scale;
                        float ay = r.y + (y + 0.5f - glyphBottom) / scale;
                        // Only this glyph's cell: the padding band can hold a neighbouring glyph's field.
                        if (ax < r.x - GlyphMargin || ax > r.x + r.width + GlyphMargin || ay < r.y - GlyphMargin || ay > r.y + r.height + GlyphMargin) continue;
                        float value = SampleSdf(ax, ay);
                        float c = Mathf.Clamp01((value - 0.5f) * sharpness + 0.5f);
                        int index = y * size + x;
                        coverage[index] = Mathf.Max(coverage[index], c);
                    }
                }

                cursor += r.width + gap;
            }

            return coverage;
        }

        private float SampleSdf(float x, float y)
        {
            x -= 0.5f;
            y -= 0.5f;
            int ix = Mathf.FloorToInt(x);
            int iy = Mathf.FloorToInt(y);
            float fx = x - ix;
            float fy = y - iy;
            float a = Texel(ix, iy), b = Texel(ix + 1, iy), c = Texel(ix, iy + 1), d = Texel(ix + 1, iy + 1);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        private float Texel(int x, int y)
        {
            if (x < 0 || y < 0 || x >= atlasWidth || y >= atlasHeight) return 0f;
            return sdf[y * atlasWidth + x] / 255f;
        }
    }
}
