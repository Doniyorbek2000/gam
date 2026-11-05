# ART_PIPELINE — Asset pipeline, budjetlar, litsenziyalar

> Holat: v1.0 (2026-11-05). Import tool va reyestr M1 da yoziladi ([ROADMAP](ROADMAP.md)).

## 1. Oqim

```
Foydalanuvchi yuklaydi (Sketchfab / CGTrader / Poly Haven / ambientCG ...)
   → ArtSource/Incoming/<asset-slug>/  (model + teksturalar + license.txt yoki manba URL)
   → Editor menyu: BilliardGame ▸ Art ▸ Import Incoming
        1. Litsenziya tekshiruvi (LICENSES.md da yozuv bormi, ruxsat etilgan turmi)
        2. Model tahlili (tri soni, UV, scale, pivot, material soni)
        3. Mobil budjetga moslashtirish (LOD0–2, teksturalar ASTC + 1K/2K)
        4. URP Lit materialga o'tkazish (texture map'larni avtomatik ulash)
        5. Prefab + ScriptableObject skin ta'rifi (SkinDefinition)
        6. Addressables guruhiga qo'shish (Remote, tur bo'yicha)
        7. Hisobot (Console + ArtSource/Reports/<slug>.md)
   → Assets/_Project/Art/<Tur>/<slug>/ (FBX/GLB, Materials, Textures, Prefab, SO)
```

`ArtSource/Incoming/` — xom yuklamalar, **git'ga kiritilmaydi** (M1 qarori: `.gitignore` da; har bir slug manba URL'i `LICENSES.md` da, xom faylni qayta yuklash mumkin). Import qilingan nusxa `Assets/_Project/Art/<Type>/<slug>/Source/` da git'da. `ArtSource/LICENSES.md` va `ArtSource/Reports/` — doim git'da.

## 2. Litsenziya siyosati

| Litsenziya | Ruxsat | Shart |
|------------|--------|-------|
| **CC0** (Poly Haven, ambientCG, Kenney, Quaternius) | ✅ | Attribution shart emas, baribir Credits'da yoziladi |
| **CC-BY 4.0** | ✅ | Credits'da muallif, nom, manba, litsenziya havolasi, "o'zgartirilgan" belgisi |
| **Sketchfab Standard** (Store) | ✅ | Royalty-free; fayl sifatida qayta tarqatilmaydi (faqat o'yinga "embedded") ([Sketchfab licenses](https://sketchfab.com/licenses), [Store FAQ](https://help.sketchfab.com/en/articles/16152225-store-license-usage-faq)) |
| **CGTrader Royalty Free** | ✅ | O'yinda ishlatish mumkin, modelni fayl sifatida sotish/berish yoki ajratib olishga imkon berish mumkin emas ([CGTrader help](https://help.cgtrader.com/hc/en-us/articles/360015193418-What-is-the-difference-between-Royalty-Free-and-Editorial-licenses)) |
| SIL OFL (shriftlar) | ✅ | Shrift faylini alohida sotmaslik |
| CC BY 3.0 (faqat UI ikonkalar, game-icons.net) | ✅ | Muallif nomi Credits'da |
| Apache 2.0 (faqat UI ikonkalar, Material Symbols) | ✅ | Litsenziya havolasi Credits'da |
| **CC-BY-NC** (har qanday NC) | ❌ | Tijoriy foydalanish taqiqlangan |
| **Editorial** | ❌ | Faqat tahririy/yangilik maqsadida |
| **CC-BY-ND** | ❌ | O'zgartirish taqiqlangan — biz har doim optimallashtiramiz (decimate, re-texture) |
| **CC-BY-SA** | ❌ | Hosila asarlarni ham SA ostida chiqarish talabi — xavfli |
| Litsenziyasi noma'lum / "free download" | ❌ | Manba aniq bo'lmaguncha import qilinmaydi |

Qo'shimcha taqiqlar: haqiqiy shaxslar portretlari, haqiqiy brend logotiplari va savdo belgilari (stol/kiy ishlab chiqaruvchilar nomlari, pivo/ichimlik brendlari, sport klublari), aniq real binolarning tanib bo'ladigan nusxalari. Bunday elementli model faqat logo olib tashlangandan keyin.

## 3. Mobil budjetlar

### 3.1 Geometriya (triangle)

| Asset turi | LOD0 | LOD1 | LOD2 | Materiallar | Izoh |
|------------|-----:|-----:|-----:|------------:|------|
| Stol (9 ft, bortlar + oyoqlar) | ≤ 40k | ≤ 20k | ≤ 8k | ≤ 3 (yog'och, mato, metall) | Mato yuzasi tekis (fizika 2D) |
| Kiy | ≤ 5k | ≤ 2.5k | ≤ 1k | ≤ 2 | Pivot = uchi (tip), +Z yo'nalishda |
| Shar to'plami | 16 × ≤ 1.2k (umumiy mesh) | 600 | 300 | 1 (atlas/array) | Hozirgi procedural sharlar bilan bir xil mesh |
| Zal (xona) | ≤ 150k jami | ≤ 75k | ≤ 30k (fon kamerasi) | ≤ 12 | Baked lightmap, statik |
| Avatar (personaj) | ≤ 20k | ≤ 10k | ≤ 4k | ≤ 3 | Humanoid rig, ≤ 60 suyak |
| Avatar kiyimi (slot) | ≤ 5k | ≤ 2.5k | — | 1 | Bir xil skelet |
| Prop (stul, lampa, kiy tokchasi) | ≤ 3k | ≤ 1.5k | — | 1 | |
| Lobby sahnasi jami | ≤ 60k | | | | |
| Match kadri jami (ko'rinadigan) | ≤ 250k | | | | Draw calls ≤ 150 |

### 3.2 Teksturalar

| Turi | O'lcham | Format (Android) | Izoh |
|------|---------|------------------|------|
| Stol mato (albedo + normal) | 2K | ASTC 6×6 (albedo), ASTC 5×5 (normal) | Tiling, detail normal |
| Stol yog'och/metall | 2K | ASTC 6×6 | Mask map (metallic/AO/smoothness) bitta faylda |
| Kiy | 1K (Mythic 2K) | ASTC 6×6 | |
| Sharlar | 512 har biri yoki 2K atlas | ASTC 6×6 | |
| Zal | 2K (asosiy), 1K (ikkinchi darajali) | ASTC 8×8 | Lightmap 2K, ASTC 6×6 |
| Avatar | 1K–2K | ASTC 6×6 | |
| UI | atlas 2K | ASTC 4×4 yoki RGBA32 (ikonkalar) | Sprite Atlas |
| HDRI (reflection) | 512 cube | ASTC 6×6 | Poly Haven CC0 |

Material: URP Lit (yoki Simple Lit past sifatda); Mask map kanali: R = Metallic, G = AO, A = Smoothness (URP konventsiyasi). Bitta shader varianti to'plami, keraksiz keyword'lar o'chirilgan.

### 3.3 Masshtab va o'qlar (fizika bilan mos)
- 1 unit = 1 m. Stol o'yin maydoni 2.54 × 1.27 m (9 ft), markaz origin'da, uzunlik +X ([TableSpace](../Assets/_Project/Scripts/Presentation/TableSpace.cs)).
- Shar diametri 57.15 mm. Kiy uzunligi 1.47 m, uchi diametri ~13 mm.
- Y yuqoriga. FBX eksportda "Apply Transform", scale 1.0.

## 4. Litsenziya reyestri va Credits

`ArtSource/LICENSES.md` — bitta markdown jadval emas, balki parse qilish oson bo'lgan bloklar:

```markdown
## silk-road-lounge-room
- name: Oriental Lounge Interior
- type: venue
- source_url: https://sketchfab.com/3d-models/...
- author: <muallif nomi>
- author_url: https://sketchfab.com/<muallif>
- license: CC-BY-4.0            # CC0 | CC-BY-4.0 | CC-BY-3.0 | Apache-2.0 | Sketchfab-Standard | CGTrader-RF | OFL
- license_url: https://creativecommons.org/licenses/by/4.0/
- modified: yes                 # decimated, re-textured
- attribution: "Oriental Lounge Interior" by <muallif> (Sketchfab), CC BY 4.0, modified
- downloaded: 2026-11-10
- used_in: Assets/_Project/Art/Venues/silk-road-lounge-room/
```

- Import tool: yozuv bo'lmasa yoki `license` taqiqlangan bo'lsa → import to'xtaydi (xato).
- Credits generator (Editor, build oldidan avtomatik): `LICENSES.md` → `Resources/credits.json` (yoki Addressables) → Credits ekrani: tur bo'yicha guruhlab `attribution` qatorlari. CC0 ham ko'rsatiladi ("Thanks to Poly Haven").
- Build pre-process tekshiruv: `Assets/_Project/Art/**` dagi har bir model papkasi reyestrda bormi.

## 5. Import tool spetsifikatsiyasi (M1)

Menyu: `BilliardGame ▸ Art ▸ Import Incoming` (+ `Validate All Art`, `Generate Credits`).

| Qadam | Amal | Muvaffaqiyatsizlik |
|-------|------|--------------------|
| Topish | `ArtSource/Incoming/*/` dagi `.fbx/.glb/.gltf/.obj` | — |
| Litsenziya | slug bo'yicha `LICENSES.md` yozuvi | Xato, to'xtaydi |
| Tur | yozuvdagi `type`: table, cue, balls, venue, avatar, outfit, prop, texture, hdri, font (oxirgi uchtasi M1 da qo'shildi: tekstura → URP Lit material, HDRI → cubemap, shrift → TMP SDF) | Xato |
| Nusxalash | `Assets/_Project/Art/<Type>/<slug>/Source/` | — |
| Model import | ModelImporter: scale 1, Read/Write off, mesh compression Medium, tangents import/calc, animation faqat avatar uchun | — |
| Tahlil | tri soni, material soni, bounds (masshtab tekshiruvi: stol ~2.8 m, kiy ~1.47 m) | Ogohlantirish |
| LOD | Agar LOD yo'q bo'lsa: Unity 6 Mesh LOD (avtomatik) yoki meshoptimizer simplify (LOD1 50%, LOD2 20%) + LODGroup | LOD0 budjetdan > 1.5× katta bo'lsa — xato, qo'lda optimallashtirish kerak |
| Tekstura | TextureImporter: max size (1K/2K tur bo'yicha), Android override ASTC (jadval §3.2), normal map turi, sRGB to'g'ri, mipmaps | — |
| Material | URP Lit, map'larni nom bo'yicha ulash (`_BaseColor/_albedo/_diffuse`, `_Normal`, `_Metallic`, `_Roughness` → smoothness = 1 − roughness, Mask map yig'ish) | Ogohlantirish |
| Prefab | `<slug>.prefab` (LODGroup, collider yo'q — fizika o'zimizniki) | — |
| SkinDefinition | `ScriptableObject`: id, type, rarity, displayName (lokalizatsiya kaliti), prefab (AssetReference), preview icon, unlock manbai | — |
| Addressables | Guruh: `Remote_Cues`, `Remote_Tables`, `Remote_Venues`, `Remote_Avatars`; label = rarity. **M3 ga qoldirildi** — M1 da skinlar oddiy asset havolasi, venue sahnasi build'da | — |
| Hisobot | `ArtSource/Reports/<slug>.md` | — |

Ikonka/preview: Icon Studio (preview scene, HDRI reflection) orqali 256×256 shaffof sprite (M1 da kiy va stol uchun).

## 6. Birinchi asset ro'yxati (M1 — Vertical slice)

Format: **FBX** (afzal) yoki **GLB**. Teksturalar PBR (BaseColor, Normal, Roughness/Metallic yoki ORM). Har bir yuklamaga manba URL va muallif nomini `ArtSource/Incoming/<slug>/source.txt` ga yozing (men reyestrga o'tkazaman).

| # | Slug (papka nomi) | Nima kerak | Qidirish so'zlari | Qayerdan | Litsenziya | Budjet |
|---|-------------------|-----------|-------------------|----------|-----------|--------|
| 1 | `table-classic` | 9 ft pool stol, logosiz, alohida mato/bort/oyoq materiallari | "pool table", "billiard table 9ft", "pool table low poly PBR" | Sketchfab (Downloadable + filtr: CC BY / CC0), CGTrader (Royalty Free, Game ready) | CC0 / CC-BY / Standard / RF | ≤ 40k tri (kattaroq bo'lsa ham olib keling, ≤ 120k — men optimallashtiraman) |
| 2 | `table-modern` | Ikkinchi stol: zamonaviy/minimal dizayn (metall oyoqlar) | "modern pool table", "contemporary billiard table" | Sketchfab, CGTrader | xuddi shunday | ≤ 40k |
| 3 | `cue-classic` | Klassik yog'och kiy (2 qismli) | "pool cue", "billiard cue stick", "snooker cue" | Sketchfab, CGTrader, Poly Haven | CC0 / CC-BY / RF | ≤ 5k |
| 4 | `cue-carbon` | Zamonaviy karbon/metall kiy | "carbon pool cue", "sci-fi cue stick", "modern cue" | Sketchfab, CGTrader | xuddi shunday | ≤ 5k |
| 5 | `cue-ornate` | Bezakli (Legendary) kiy — o'ymakor/naqshli | "ornate cue", "engraved pool cue", "decorated cue stick" | Sketchfab, CGTrader | xuddi shunday | ≤ 5k |
| 6 | `venue-silk-road-lounge` | Premium zal interyeri: Sharq/O'rta Osiyo uslubi (koshin, arklar, yog'och ship) yoki istalgan "lounge/bar" interyer (men qayta teksturlayman) | "oriental interior", "arabic room interior", "lounge interior", "pool hall interior", "bar interior game ready" | Sketchfab, CGTrader | CC0 / CC-BY / Standard / RF | ≤ 150k (≤ 400k ham bo'ladi — optimallashtiraman) |
| 7 | `prop-table-lamp` | Stol ustidagi uzun lampa (pool table light) | "pool table light", "billiard lamp", "hanging lamp" | Sketchfab, Poly Haven | CC0 / CC-BY | ≤ 3k |
| 8 | `prop-cue-rack` | Devordagi kiy tokchasi | "cue rack", "wall cue rack" | Sketchfab, CGTrader | xuddi shunday | ≤ 3k |
| 9 | `prop-seating` | 2–3 ta bar stul / kreslo / divan | "bar stool", "armchair", "leather sofa" | Poly Haven (CC0), Sketchfab | CC0 / CC-BY | ≤ 3k har biri |
| 10 | `avatar-base` | Riglangan humanoid personaj (erkak va/yoki ayol), stilize-realistik, kiyim almashtirish uchun qulay | "rigged character", "humanoid rigged low poly", "stylized character rigged CC0" | Quaternius (CC0), Sketchfab, CGTrader | CC0 / CC-BY / RF | ≤ 20k |
| 11 | `avatar-anims` | Idle, "kiyni aylantirish", "g'alaba" animatsiyalari (humanoid) | "idle animation", "victory", "cheering" | Mixamo (Adobe royalty-free shartlari — yuklashda tasdiqlash), Quaternius | RF / CC0 | — |
| 12 | `tex-cloth` | Billiard mato teksturasi (felt) PBR | "felt", "fabric", "baize" | ambientCG, Poly Haven | CC0 | 2K |
| 13 | `tex-wood` | 2–3 yog'och PBR (stol bortlari, kiy) | "wood polished", "walnut", "mahogany" | ambientCG, Poly Haven | CC0 | 2K |
| 14 | `hdri-interior` | Ichki xona HDRI (reflection probe uchun) | "interior HDRI", "lounge HDRI" | Poly Haven | CC0 | 1K–2K .hdr |
| 15 | `font-ui` | Lotin + kirill shrifti (Bold/Medium) | "Inter", "Montserrat", "Manrope" | Google Fonts | OFL | — |

**M1 holati (2026-11-05).** Yuklab olindi va import qilindi: #9 (Poly Haven chinese armchair/sofa/stool/tea table + brass vase), #10 (Quaternius Universal Animation Library — manekin, 46 klip; #11 o'rnida), #12 (ambientCG Fabric034), #13 (ambientCG Wood051, Wood092), #14 (Poly Haven billiard_hall), #15 (Montserrat), zal teksturalari (ambientCG Tiles067/131, PaintedPlaster017, WoodFloor064, Carpet015). Hali yo'q — protsedural placeholder: #1–#5 (stollar va kiylar protsedural lathe/box), #6 (Silk Road Lounge — protsedural xona), #7, #8. Poly Haven ceramic vase (10k tri) budjetdan 1.5× oshgani uchun import tool tomonidan rad etildi.

**Sketchfab partiyasi (2026-11-05, [hisobot](../ArtSource/Reports/incoming-2026-11-05.md)).** Qabul: #1 `table-classic` (fizyman "Pool Table Traditional", CC BY) va #2 `table-modern` (Pieter Ferreira "Pool Table", CC BY) — `table_classic`/`table_modern` skinlari endi model (procedural stol yashiriladi). Rad: snooker_table (CC BY-NC), cue_stick_lp (CC BY-SA), billiard_bar (Evermotion Archmodels qismlari qayta litsenziyalangan + 2.3M tri), Mixamo personajlarining qayta yuklamalari (happy_idle, doctorsurgeon), jeff_johansen_idle (Dead by Daylight personaji), mint_swimsuit (uslub/reyting). Hali yo'q: #3–#6 (kiylar, Silk Road Lounge interyeri), #7, #8, #10 (avatar).

**glTF manbalar.** Sketchfab zip'lari glTF beradi; import tool FBX/OBJ o'qiydi. Konvertatsiya: `Tools/Blender/convert_table.py` (Blender 4.2 headless) — shar/kiy/lampa olib tashlanadi, mato tekisligi va cushion burunlari ray-cast bilan o'lchanib o'yin maydoni 2.54 × 1.27 m ga moslanadi (mato y = 0, pol y = −0.8), teksturalar ≤ 2K, URP mask (R metal, G AO, A smoothness) yig'iladi, `materials.json` yoziladi. Import tool `materials.json` ni ko'rsa har bir FBX materiali uchun URP Lit yaratadi va remap qiladi (cutout alfa, ixtiyoriy `smoothness:` ko'paytiruvchisi LICENSES.md da).

Sketchfab'da qidiruvda "Downloadable" va License filtrini (CC Attribution / CC0) yoqing; "NonCommercial", "NoDerivs", "Editorial" yozuvi bo'lganlarni olmang. Ovozlar hozircha procedural ([ProceduralAudio](../Assets/_Project/Editor/ProceduralAudio.cs)); keyin CC0 SFX (freesound CC0 filtr) ro'yxati alohida beriladi.
