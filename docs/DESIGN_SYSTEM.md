# Design System (M1.5)

UI'ning yagona manbasi: `Assets/_Project/UI/Theme/UITheme.asset` (`BilliardGame.UI.UITheme`). Barcha ekranlar kod orqali quriladi (`Editor/UI/*Builder.cs`, `UIKit*.cs`), shuning uchun token o'zgarsa `BilliardGame ▸ Build Main Scene` butun UI'ni qayta bo'yaydi. Ekranlar tartibi: [UI_UX.md](UI_UX.md).

## 1. Tamoyillar

- Qorong'i ko'k-qora fon, oltin aksent (PUBG Mobile / eFootball uslubidagi "premium" lobbi).
- Bitta asosiy harakat har ekranda oltin (`Primary`) tugma; qolganlari `Secondary` / `Ghost`.
- 3D sahna ko'rinadigan ekranlarda (lobbi, yashik, xona) faqat chet soyasi, to'liq fon yo'q.
- Kosmetika hech qachon o'yin qoidasini o'zgartirmaydi; har bir skin/stiker ekranida "faqat ko'rinish" yozuvi turadi (ECONOMY §2).

## 2. Ranglar

| Token | Hex | Ishlatilishi |
|-------|-----|--------------|
| `background` | `#0B0F1A` | Ekran foni |
| `surface` | `#151B2B` (92%) | Kartalar, panellar |
| `surfaceRaised` | `#1E2638` | Ko'tarilgan karta, tanlangan tab |
| `surfaceSunken` | `#0E1320` (90%) | Trek, input, progress foni |
| `stroke` | `#FFFFFF` (8%) | Karta chegarasi |
| `scrim` | `#05070C` (72%) | Modal/sheet orqasi |
| `textPrimary` / `textSecondary` / `textMuted` | `#F4F6FB` / `#A7B0C4` / `#6B7489` | Matn darajalari |
| `textOnAccent` | `#1A1204` | Oltin tugma ustidagi matn |
| `accent` / `accentDeep` | `#FFC24A` / `#FF7A00` | Asosiy harakat, tanlov, PLAY |
| `secondary` | `#2E7BFF` | XP, ma'lumot |
| `success` / `warning` / `danger` | `#2ECC71` / `#FFB020` / `#FF3B5C` | Holatlar, "+" tugmalar, o'chirish |
| `coin` / `gem` | `#FFD15C` / `#5CD6FF` | Valyuta raqamlari |

**Raritet** (ECONOMY §3): Common `#9AA3AE`, Rare `#3D8BFF`, Epic `#A64DFF`, Legendary `#FFB020`, Mythic gradient `#FF3B5C → #FF8A3D` (qizil). Raritet rangi tile foni, chegara, nuqtalar (●) va yorliqda bir xil ishlatiladi; Mythic yagona gradientli raritet.

## 3. Tipografiya

Shrift: **Montserrat** (Bold / SemiBold / Medium, lotin + kirill, OFL). TMP SDF atlaslari `Art/Fonts/font-montserrat/`.

| Uslub | O'lcham (ref px) | Og'irlik | Misol |
|-------|-----------------:|----------|-------|
| Display | 72 | Bold | "G'ALABA!" |
| H1 | 48 | Bold | Ekran sarlavhasi |
| H2 | 36 | SemiBold | Karta sarlavhasi, zal nomi |
| Body | 28 | Medium | Tavsif |
| Caption | 22 | Medium | Yorliq, izoh |
| Button | 30 | Bold, BOSH HARF | Tugma matni |
| Number | 36 | Bold | Coin/gem, statistika |

Qoidalar: barcha matn auto-size (`fontSizeMin` ~60%), overflow = Ellipsis. **Diqqat:** rect balandligi shrift qatoridan past bo'lsa TMP hech narsa chizmaydi — label balandligi ≥ shrift o'lchami × 1.25.

## 4. Grid va o'lchamlar

- Canvas: reference **1920×1080**, `Scale With Screen Size`, match **0.5**. 4:3 da canvas ≈ 1663×1247, 20:9 da ≈ 2147×966 — layout ikkala chekkada ham sinovdan o'tadi.
- Safe area: har ekranning `Safe Area` node'ida `SafeAreaFitter` (notch / punch-hole).
- Bo'shliq: **8 pt** qadam (`UITheme.Space(n)`), ekran cheti 32–40, karta ichki padding 16–24.
- Touch: minimal **48 dp ≈ 72 ref px** (`UITheme.MinTouch`), asosiy tugmalar 88–140.
- Radiuslar: 12 / 20 / 32; soya `#000` 45%, offset 6.

## 5. Komponentlar (`Editor/UI/UIKit*.cs` + `Scripts/UI/Kit/`)

| Komponent | Runtime skript | Izoh |
|-----------|----------------|------|
| Button (Primary, Secondary, Ghost, Danger, Success) | `UIButtonFx` | Holatlar: normal / pressed (scale 0.95, 80 ms) / disabled (50%); bosilganda ovoz + haptic (`UIFeedback`) |
| Icon button, Soon tag, Badge | `UIButtonFx`, `BadgeView` | "TEZ KUNDA" teg (`UIKit.SoonTag`) kartaning yuqori chetida; `BadgeView` — son/nuqta |
| Currency pill | `CurrencyPill`, `CountUpText` | Coin/gem + "+" (do'kon) |
| Tab bar (gorizontal / vertikal) | `TabBar` | Tanlangan tab oltin |
| Toggle, Slider, Dropdown, Input | `ToggleSwitch` | Slider tutqichi 48 px doira |
| Item tile | `ItemTile` | Raritet foni + nuqtalar, qulf, jihozlangan ✓, "yangi" nuqta |
| Progress bar | `ProgressBar` | XP, pity |
| Row (sozlamalar qatori) | `RowView` | Ikonka + sarlavha + izoh + boshqaruv |
| Empty state, Spinner | `EmptyStateView`, `Spinner` | Bo'sh ro'yxat, yuklanish |
| Screen / Modal / Bottom sheet | `ScreenView`, `ScreenRouter` | Stack router, Back = Android orqaga |
| Dialogs (confirm + gem narxi, input, toast, loading, offline) | `Dialogs` | Gem sarflashdan oldin har doim tasdiq |

Komponentlar alohida prefab emas: builder metodlari prefab vazifasini bajaradi (bitta kod — bitta ko'rinish, prefab va kod ajralib ketmaydi). Prefab kerak bo'lsa (masalan, Addressables orqali remote UI) `UIKit` chaqiruvini prefab sifatida saqlash qo'shiladi.

## 6. Harakat (tween)

`Tween` (allocation'siz, unscaled time): ekran o'tishi 250 ms (fade / slide), modal pop 220 ms (OutBack), raqam count-up 600 ms (OutCubic + 1.12 pulse), PLAY tugmasi `PulseFx` (3.5%, 1.6 s), yashik ochilishi — nurlar aylanishi + spinner + pulse.

## 7. Ikonkalar

- Manba SVG'lar: `ArtSource/Icons/` (har biri `manifest.json` da URL bilan), PNG'ga `Tools/UI/icons.mjs` (resvg) bilan render qilinadi → `Assets/_Project/UI/Icons/` → bitta **SpriteAtlas** `UI/Icons.spriteatlasv2`.
- 78 ta **Material Symbols** Rounded filled (Apache 2.0, Google) — tizim ikonkalari; 17 ta **game-icons.net** (CC BY 3.0: Delapouite, Lorc, Skoll) — o'yin ikonkalari (shar, yashik, kubok, coin, gem…).
- Ikonkalar oq rangda saqlanadi, rang `Image.color` orqali tokenlardan beriladi.
- Litsenziyalar: `ArtSource/LICENSES.md` (`type: icon`) → Credits'dagi "Ikonkalar" guruhi.

## 8. Lokalizatsiya

Unity Localization, jadval `UI`, tillar **uz / ru / en**. Manba: `Assets/_Project/Localization/strings.psv` (`key|uz|ru|en`) → `BilliardGame ▸ UI ▸ Build Localization`. Matn komponentlarda `LocalizedText` (kalit + bosh harf), kodda `Loc.Get/Format/Number`. Til Sozlamalar ▸ Til da darhol almashadi va saqlanadi. Placeholder matn sahnada qolmaydi: har label kalit bilan yoki runtime'da to'ldiriladi.

## 9. Tekshiruv (har UI o'zgarishidan keyin)

1. `Build Localization` → `Build UI Assets` → `Build Content` → `Build Main Scene`.
2. Play mode, MCP: 1920×1080, 2400×1080, 1440×1080 (4:3), Device Simulator "Punch Hole" — `ScreenCapture.CaptureScreenshot` (overlay UI bilan).
3. TMP audit: barcha faol `TMP_Text` uchun `ForceMeshUpdate()`, so'ng `characterCount == 0` yoki `isTextTruncated` → tuzatish (har bir tab va har bir til).
4. Console toza, EditMode testlar.
