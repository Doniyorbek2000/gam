using System.Collections.Generic;
using System.IO;
using BilliardGame.Content;
using BilliardGame.Editor.Content;
using BilliardGame.Meta;
using UnityEditor;
using UnityEngine;

namespace BilliardGame.Editor.UI
{
    /// <summary>
    /// M1.5 cosmetics and venue data: 10 original stickers, avatar frames, room wall/floor themes (procedural textures),
    /// venue entry/prize/lock data (docs/GDD.md §3) and the locked "coming soon" venues. Called by Build Content.
    /// </summary>
    internal static class UIContentBuilder
    {
        private const string ThemeArt = "Assets/_Project/Art/Generated/Room";
        private const string VenueArt = UIAssets.Root + "/Venues";

        private struct Spec
        {
            public string Id;
            public ItemType Type;
            public Rarity Rarity;
            public bool Default;
            public string Source;
            public Spec(string id, ItemType type, Rarity rarity, bool owned, string source = "source.crate")
            {
                Id = id;
                Type = type;
                Rarity = rarity;
                Default = owned;
                // Every non-starter item is in the crate pool (MockEconomyService.PickItem); the shop sells them too from M2.
            Source = owned ? "source.starter" : source;
            }
        }

        private static readonly Spec[] Stickers =
        {
            new Spec("sticker_nice_shot", ItemType.Sticker, Rarity.Common, true),
            new Spec("sticker_lol_ball", ItemType.Sticker, Rarity.Common, true),
            new Spec("sticker_pomegranate", ItemType.Sticker, Rarity.Common, false),
            new Spec("sticker_choy", ItemType.Sticker, Rarity.Common, false),
            new Spec("sticker_blue_dome", ItemType.Sticker, Rarity.Rare, false),
            new Spec("sticker_break_bolt", ItemType.Sticker, Rarity.Rare, false),
            new Spec("sticker_gg", ItemType.Sticker, Rarity.Rare, false),
            new Spec("sticker_hot_eight", ItemType.Sticker, Rarity.Epic, false),
            new Spec("sticker_gold_star", ItemType.Sticker, Rarity.Epic, false),
            new Spec("sticker_crown_cue", ItemType.Sticker, Rarity.Legendary, false),
        };

        private static readonly (string id, Rarity rarity, bool owned, Color a, Color b)[] Frames =
        {
            ("frame_basic", Rarity.Common, true, new Color(0.62f, 0.66f, 0.72f), new Color(0.35f, 0.38f, 0.45f)),
            ("frame_azure", Rarity.Rare, false, new Color(0.3f, 0.65f, 1f), new Color(0.1f, 0.3f, 0.8f)),
            ("frame_royal", Rarity.Epic, false, new Color(0.8f, 0.45f, 1f), new Color(0.4f, 0.1f, 0.75f)),
            ("frame_gold", Rarity.Legendary, false, new Color(1f, 0.86f, 0.4f), new Color(0.85f, 0.5f, 0.05f)),
        };

        private enum Pattern { Plain, Brick, Tiles, Planks, Marble, Carpet }

        private static readonly (string id, ItemType type, Rarity rarity, bool owned, Pattern pattern, Color a, Color b)[] Themes =
        {
            ("wall_plain", ItemType.RoomWall, Rarity.Common, true, Pattern.Plain, new Color(0.13f, 0.16f, 0.24f), new Color(0.1f, 0.12f, 0.18f)),
            ("wall_brick", ItemType.RoomWall, Rarity.Rare, false, Pattern.Brick, new Color(0.55f, 0.27f, 0.18f), new Color(0.82f, 0.78f, 0.7f)),
            ("wall_tiles", ItemType.RoomWall, Rarity.Epic, false, Pattern.Tiles, new Color(0.08f, 0.45f, 0.65f), new Color(0.95f, 0.9f, 0.78f)),
            ("floor_wood", ItemType.RoomFloor, Rarity.Common, true, Pattern.Planks, new Color(0.42f, 0.27f, 0.16f), new Color(0.3f, 0.18f, 0.1f)),
            ("floor_marble", ItemType.RoomFloor, Rarity.Rare, false, Pattern.Marble, new Color(0.88f, 0.88f, 0.9f), new Color(0.55f, 0.56f, 0.6f)),
            ("floor_carpet", ItemType.RoomFloor, Rarity.Epic, false, Pattern.Carpet, new Color(0.55f, 0.08f, 0.12f), new Color(0.95f, 0.7f, 0.25f)),
        };

        private static readonly (string id, string name, int entry, int prize, int level)[] VenueEconomy =
        {
            ("garage", "Garaj", 50, 95, 1),
            ("silk_road_lounge", "Silk Road Lounge", 1000, 1900, 1),
        };

        private static readonly (string id, string name, int entry, int prize, int level, Color a, Color b)[] ComingVenues =
        {
            ("old_city_club", "Eski Shahar Klubi", 200, 380, 3, new Color(0.45f, 0.28f, 0.14f), new Color(0.12f, 0.08f, 0.05f)),
            ("bukhara_palace", "Bukhara Palace", 5000, 9500, 12, new Color(0.1f, 0.42f, 0.6f), new Color(0.9f, 0.7f, 0.3f)),
            ("neon_arena", "Neon Arena", 25000, 47500, 20, new Color(0.9f, 0.1f, 0.7f), new Color(0.1f, 0.05f, 0.3f)),
        };

        [MenuItem("BilliardGame/UI/Build UI Content")]
        public static void BuildMenu()
        {
            SkinCatalog catalog = ContentBuilder.LoadCatalog();
            var skins = new List<SkinDefinition>();
            foreach (SkinDefinition skin in catalog.Skins)
            {
                if (skin != null && (skin.Type == ItemType.Cue || skin.Type == ItemType.Table)) skins.Add(skin);
            }

            var venues = new List<VenueDefinition>();
            foreach (VenueDefinition venue in catalog.Venues)
            {
                if (venue != null && !venue.ComingSoon) venues.Add(venue);
            }

            Extend(skins, venues);
            catalog.Configure(skins.ToArray(), venues.ToArray());
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log($"[BilliardGame] UI content: {skins.Count} items, {venues.Count} venues");
        }

        public static void Extend(List<SkinDefinition> skins, List<VenueDefinition> venues)
        {
            BallMaterialFactory.EnsureFolder(ThemeArt);
            BallMaterialFactory.EnsureFolder(VenueArt);
            foreach (Spec spec in Stickers) skins.Add(Item(spec, UIAssets.Sticker(spec.Id), null));
            foreach (var f in Frames) skins.Add(Item(new Spec(f.id, ItemType.Frame, f.rarity, f.owned), FrameIcon(f.id, f.a, f.b), null));
            foreach (var t in Themes)
            {
                Texture2D texture = PatternTexture(t.id, t.pattern, t.a, t.b);
                Material surface = GeneratedArt.Lit($"{ThemeArt}/{t.id}.mat", Color.white, t.pattern == Pattern.Marble ? 0.8f : 0.3f, 0f, texture, t.type == ItemType.RoomWall ? new Vector2(3f, 2f) : new Vector2(3f, 3f));
                skins.Add(Item(new Spec(t.id, t.type, t.rarity, t.owned), SpriteFrom(texture, $"{ThemeArt}/{t.id}_icon.png"), surface));
            }

            foreach (VenueDefinition venue in venues)
            {
                foreach (var e in VenueEconomy)
                {
                    // Captured hall shots (Capture Venue Previews) win; the poster is the fallback before the first capture.
                    if (venue.Id == e.id) venue.ConfigureEconomy(e.entry, e.prize, e.level, false, LoadVenuePreview(e.id) ?? PosterArt(e.id, new Color(0.35f, 0.4f, 0.48f), new Color(0.08f, 0.1f, 0.14f)));
                }

                EditorUtility.SetDirty(venue);
            }

            foreach (var c in ComingVenues)
            {
                VenueDefinition venue = ContentBuilder.Venue(c.id, c.name, string.Empty);
                venue.ConfigureEconomy(c.entry, c.prize, c.level, true, LoadVenuePreview(c.id) ?? PosterArt(c.id, c.a, c.b));
                EditorUtility.SetDirty(venue);
                venues.Add(venue);
            }
        }

        private static SkinDefinition Item(Spec spec, Sprite icon, Material surface)
        {
            SkinDefinition skin = ContentBuilder.Skin(spec.Id);
            string name = LocalizationBuilder.Has("item." + spec.Id) ? LocalizationBuilder.Uz("item." + spec.Id) : spec.Id;
            skin.Configure(spec.Id, spec.Type, spec.Rarity, name, spec.Default, null, surface, null, icon, spec.Source);
            EditorUtility.SetDirty(skin);
            return skin;
        }

        private static Sprite LoadVenuePreview(string id) => AssetDatabase.LoadAssetAtPath<Sprite>($"{VenueArt}/{id}.png");

        // ---- Procedural art (original) -------------------------------------------------------------------------------

        private static Sprite FrameIcon(string id, Color a, Color b)
        {
            const int size = 256;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - size / 2f, dy = y + 0.5f - size / 2f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float ring = Mathf.Clamp01(1f - Mathf.Abs(d - 96f) / 18f);
                float edge = Mathf.Clamp01((18f - Mathf.Abs(d - 96f)) * 1.5f);
                float angle = Mathf.Atan2(dy, dx);
                Color c = Color.Lerp(b, a, 0.5f + 0.5f * Mathf.Sin(angle * 2f + 0.8f)) * (0.75f + 0.35f * ring);
                // Eight studs around the ring read as a "frame" even at icon size.
                float stud = Mathf.Repeat(angle / (Mathf.PI * 2f) * 8f, 1f);
                if (Mathf.Abs(stud - 0.5f) < 0.06f && Mathf.Abs(d - 96f) < 8f) c = Color.Lerp(c, Color.white, 0.6f);
                c.a = edge;
                pixels[y * size + x] = c;
            }

            return SavePixels($"{ThemeArt}/{id}.png", size, pixels);
        }

        private static Texture2D PatternTexture(string id, Pattern pattern, Color a, Color b)
        {
            const int size = 256;
            var pixels = new Color32[size * size];
            var random = new System.Random(id.GetHashCode());
            float[] grain = new float[size];
            for (int i = 0; i < size; i++) grain[i] = (float)random.NextDouble();
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float n = Mathf.PerlinNoise(x * 0.05f + grain[0] * 10f, y * 0.05f);
                Color c;
                switch (pattern)
                {
                    case Pattern.Brick:
                    {
                        int row = y / 32;
                        int bx = (x + (row % 2) * 32) % 64;
                        bool mortar = y % 32 < 3 || bx < 3;
                        c = mortar ? b : a * (0.85f + 0.3f * n);
                        break;
                    }
                    case Pattern.Tiles:
                    {
                        int tx = x % 64, ty = y % 64;
                        float cx = tx - 32f, cy = ty - 32f;
                        bool grout = tx < 2 || ty < 2;
                        bool star = Mathf.Abs(cx) + Mathf.Abs(cy) < 22f && Mathf.Abs(cx) + Mathf.Abs(cy) > 14f;
                        c = grout ? b * 0.8f : star ? b : a * (0.9f + 0.15f * n);
                        break;
                    }
                    case Pattern.Planks:
                    {
                        int plank = x / 42;
                        float stripe = Mathf.PerlinNoise(plank * 3.1f, y * 0.012f) * 0.5f + 0.5f * Mathf.Sin(y * 0.15f + plank) * 0.15f;
                        bool seam = x % 42 < 2 || (y + plank * 97) % 256 < 2;
                        c = seam ? b * 0.6f : Color.Lerp(b, a, 0.5f + stripe);
                        break;
                    }
                    case Pattern.Marble:
                    {
                        float vein = Mathf.Abs(Mathf.Sin((x + y) * 0.03f + Mathf.PerlinNoise(x * 0.02f, y * 0.02f) * 8f));
                        c = Color.Lerp(b, a, Mathf.Pow(vein, 0.25f));
                        break;
                    }
                    case Pattern.Carpet:
                    {
                        float cx = (x % 128) - 64f, cy = (y % 128) - 64f;
                        float diamond = Mathf.Abs(cx) + Mathf.Abs(cy);
                        c = diamond < 40f && diamond > 30f ? b : diamond < 12f ? b * 0.9f : a * (0.9f + 0.1f * n);
                        break;
                    }
                    default:
                        c = Color.Lerp(b, a, n);
                        break;
                }

                c.a = 1f;
                pixels[y * size + x] = c;
            }

            return GeneratedArt.SavePng($"{ThemeArt}/{id}.png", size, size, pixels);
        }

        /// <summary>Stylised poster for venues that have no hall yet: gradient sky, arch silhouette, table glow.</summary>
        private static Sprite PosterArt(string id, Color a, Color b)
        {
            const int w = 512, h = 288;
            var pixels = new Color32[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float v = y / (float)h;
                Color c = Color.Lerp(b, a, v);
                float ax = Mathf.Abs(x - w / 2f) / (w * 0.32f);
                float arch = 0.25f + 0.6f * Mathf.Sqrt(Mathf.Max(0f, 1f - ax * ax));
                if (ax < 1f && v < arch && v > 0.18f) c = Color.Lerp(c, b * 0.35f, 0.75f);
                float gx = (x - w / 2f) / (w * 0.28f), gy = (v - 0.2f) / 0.08f;
                float glow = Mathf.Exp(-(gx * gx + gy * gy));
                c += new Color(0.1f, 0.6f, 0.35f) * glow * 0.8f;
                c.a = 1f;
                pixels[y * w + x] = c;
            }

            return SavePixels($"{VenueArt}/{id}.png", w, pixels, w);
        }

        private static Sprite SpriteFrom(Texture2D source, string path)
        {
            string from = AssetDatabase.GetAssetPath(source);
            File.Copy(from, path, true);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            return ImportSprite(path, 256);
        }

        private static Sprite SavePixels(string path, int size, Color32[] pixels, int width = 0)
        {
            int w = width > 0 ? width : size;
            int h = pixels.Length / w;
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            return ImportSprite(path, Mathf.Max(w, h));
        }

        public static Sprite ImportSprite(string path, int maxSize)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = maxSize;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings { name = "Android", overridden = true, maxTextureSize = maxSize, format = TextureImporterFormat.ASTC_6x6 });
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
