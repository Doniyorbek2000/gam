# ROADMAP — Milestone'lar

> Holat: v2.0 (2026-11-06) — yo'nalish kengaydi: 3D personajlar va "haqiqiy zal" (M1.6), online 1v1 to'liq (M2), server iqtisodi + Player ID + barcha "Tez kunda" bo'limlari (M3), admin panel (M4), veb-do'kon (M5), infra/deploy (M6). Eski M2–M12 shu tartibda M7–M16 ga surildi.
>
> Ish tartibi: har sessiyada "keyingi milestone" — birinchi `⬜` (yoki `🟨` — boshlangan) holatdagi milestone. Har milestone oxirida: Unity'da MCP orqali tekshiruv (play mode, skrinshot, Console xatosiz), testlar (`EditMode` NUnit + `Server/nakama/modules` `npm test` + yangi `admin/`, `web/` testlari), bu fayldagi holatni `✅` ga o'zgartirish, commit, `origin main` ga push, qisqa hisobot (nima qilindi, nima tekshirildi, nima foydalanuvchiga bog'liq — asset/kalit/shartnoma, xavflar).
>
> Hajm: **S** ≈ 1 sessiya, **M** ≈ 1–2 sessiya, **L** ≈ 2–3 sessiya. Bir sessiyaga sig'masa `Mx.1`, `Mx.2` ga bo'linadi va qayerda to'xtalgani shu yerda yoziladi.

## Bajarilgan (oldingi bosqichlar)
- ✅ Stage 1 — fizika yadrosi, stol, kiy aim/power/spin, 8-ball qoidalari, hot-seat, APK build.
- ✅ Stage 2 — Nakama online 1v1 (xona kodi, server-authoritative, C#/TS cross-check, E2E bot).

## Umumiy jadval

| # | Milestone | Asosiy natija | Hajm | Holat |
|---|-----------|---------------|------|-------|
| M1 | Vertical slice | Premium zal + kiy/stol skinlari + lobby + yashik (offline mock) + 8-ball shu zalda | L | ✅ |
| M1.5 | Professional UI/UX | Design system, barcha lobbi ekranlari, HUD/natija, uz/ru/en | L | ✅ |
| M1.6 | 3D personajlar va "haqiqiy zal" | 4+ personaj, tanlash ekrani, match ichida yurish/bo'r/zarba IK/stulda o'tirish, 4 kamera rejimi, fon NPC, ambient ovoz | L | ✅ |
| M2 | Online 1v1 (to'liq) | Tezkor o'yin (matchmaker), xona kodi, reconnect 60 s, zarba taymeri, raqib personaji holat eventlari bilan sinxron | M | ✅ |
| M3 | Server iqtisodi, Player ID, "Tez kunda" bo'limlari | 10 xonali Player ID, wallet+ledger, inventar, yashik RNG+pity, do'kon, vazifalar, Season Pass, eventlar, 7 kunlik login, klub/chat/do'stlar — kontent serverdan | L | ⬜ |
| M4 | Admin panel | `admin/` NestJS + Next.js: 2FA, rollar, o'yinchilar, kontent (skin/yashik/do'kon/vazifa/pass/event/login/e'lon), Addressables → CDN → admin oqimi, analitika, audit | L | ⬜ |
| M5 | Veb-do'kon | `web/` Next.js: ID → tasdiq → Gem paketi → Payme/Click/Uzum (sandbox + mock) → kredit + o'yinda bildirishnoma; uz/ru/en, huquqiy sahifalar | M | ⬜ |
| M6 | Infra va deploy | Lokal Docker Compose (nakama, postgres, admin-api, admin-web, web-shop, caddy), `deploy/` Contabo bir buyruqli deploy, Caddy subdomenlar, admin/console himoyasi | S | ⬜ |
| M7 | Venue'lar + progressiya | XP/level, venue 1–4 (kirish escrow, sovrin, 5% rake), MMR'li quick match, natija count-up | M | ⬜ |
| M8 | Ranked + leaderboard | Glicko-2, RP/darajalar, mavsum, leaderboard global/mamlakat/do'st | M | ⬜ |
| M9 | Offline rejimlar + 9-ball | vs AI (4 daraja), Mashq, 9-ball (C# + TS), FTUE tutorial | M | ⬜ |
| M10 | Play Billing va country rules | Build flavor'lar, Google Play Billing + validatsiya, refund revoke, `country_rules`, Starter Pack | M | ⬜ |
| M11 | LiveOps (qolgan) | Achievementlar, kunlik aylanadigan do'kon, craft, Mythic upgrade | M | ⬜ |
| M12 | Ijtimoiy (qolgan) | Vivox ovoz, quick chat/emoji, so'kish filtri, report/block, spectate, replay | L | ⬜ |
| M13 | Turnirlar | 8/16 knockout, bracket, sovrin, Turnir ekrani | M | ⬜ |
| M14 | Avatar va effektlar | Kiyim slotlari (bosh/ust/shim/poyabzal/aksessuar/set), emotsiyalar, trail, luza effekti, g'alaba animatsiyasi, nik ranglari | M | ⬜ |
| M15 | Launch tayyorgarligi | Analitika, crash, anti-cheat, unumdorlik, compliance checklist, soft launch (UZ) | L | ⬜ |
| M16 | Snooker + iOS | 12 ft stol, snooker qoidalari, App Store build va IAP | L | ⬜ |

---

## M1 — Vertical slice ✅
**Maqsad:** o'yinning "ko'rinishi va hissi"ni bitta to'liq yo'lakda isbotlash: lobby → yashik → jihozlash → premium zalda 8-ball.

**Natijalar**
1. **Asset pipeline (minimal):** `ArtSource/LICENSES.md` (format: [ART_PIPELINE §4](ART_PIPELINE.md#4-litsenziya-reyestri-va-credits)), `BilliardGame ▸ Art ▸ Import Incoming` (litsenziya tekshiruvi, tri/tekstura budjeti, ASTC, URP Lit, prefab, `SkinDefinition`), `Generate Credits` + Credits ekrani.
2. **Kontent:** `BilliardGame.Content` assembly; `SkinDefinition` (cue, table_cloth+rails), `VenueDefinition` SO; 3 kiy (Rare / Epic / Legendary), 2 stol skini (Classic, Modern), sharlar — mavjud procedural to'plam.
3. **Silk Road Lounge zali:** venue sahnasi (baked lighting, reflection probe, lampa), stol zalga joylashtirilgan; mavjud 8-ball (hot-seat va online xona) shu zalda ishlaydi. Garaj — mavjud Main sahnasi (default zal).
4. **PUBG uslubidagi lobby** ([UI_UX §2](UI_UX.md#2-lobby)): 3D personaj (avatar-base yoki placeholder), swipe bilan aylantirish, yonida jihozlangan kiy; yuqori bar (profil, Coin/Gem), chap/o'ng relslar (Pass/Vazifa — "tez orada" holati; Do'kon/Yashik/Inventar — ishlaydi), "O'YNASH" + rejim/venue tanlash (Hot-seat, Do'st bilan, Silk Road Lounge).
5. **Offline mock iqtisod:** `BilliardGame.Meta` — `IEconomyService` + `MockEconomyService` (lokal JSON saqlash, boshlang'ich 5 000 Coin + 500 Gem), [ECONOMY §5](ECONOMY.md#5-yashiklar-crates) dagi Premium/Classic jadvallari va pity bilan lokal RNG. Kod interfeysi M3 da Nakama implementatsiyasi bilan almashtiriladi.
6. **Yashik ochish:** yashik kartasi, `[i]` ehtimollar oynasi (jadvaldan generatsiya), pity hisoblagichi, 1×/10×, reveal animatsiyasi (oqish → ochilish → karta, raritet ranglari, skip), dublikat → Shard.
7. **Inventar (minimal):** Kiylar va Stol tablari, 3D preview, Jihozlash → match va lobbyda darhol ko'rinadi.

**Qabul qilish mezonlari**
- [x] Unity Console'da play mode'da xato yo'q; barcha mavjud EditMode testlari + yangi testlar (mock RNG taqsimoti 10⁵ ochishda jadvalga ±0.5% mos, pity N-ochishda kafolat, dublikat→shard) yashil; `npm test` yashil (server o'zgarmagan bo'lsa ham).
- [x] MCP orqali skrinshotlar: lobby, ehtimollar oynasi, yashik reveal (Epic+), inventar, Silk Road Lounge'da match.
- [x] Oqim: Lobby → Yashik → 10× ochish → yangi kiyni jihozlash → Silk Road Lounge hot-seat → kiy ko'rinadi → break va 8-ball qoidalari ishlaydi.
- [x] Online xona (Stage 2 E2E) Silk Road Lounge zalida ishlaydi.
- [x] Budjetlar: zal ≤ 150k tri, stol ≤ 40k, kiy ≤ 5k; match kadri ≤ 250k tri, ≤ 150 draw call (Frame Debugger/Profiler hisoboti); Android APK build muvaffaqiyatli.
- [x] Har bir import qilingan asset `LICENSES.md` da; Credits ekranida ko'rinadi.
- [x] Gameplay/Physics'ga skin ma'lumoti kirmagan (no pay-to-win).
- Bog'liqlik: [ART_PIPELINE §6](ART_PIPELINE.md#6-birinchi-asset-royxati-m1--vertical-slice) dagi assetlar. Yetib kelmagan asset o'rniga placeholder ishlatiladi va hisobotda yoziladi.

**Hajm:** L (M1.1: pipeline + kontent + zal; M1.2: lobby + mock iqtisod + yashik + inventar).

**Natija (2026-11-05):** EditMode 65/65 (shu jumladan Meta 13), `npm test` 9/9; match kadri Silk Road Lounge'da 59k tri / 96 draw call; online xona E2E (editor + Windows bot) shu zalda "game over"gacha o'ynaldi. Kontent: 7 kiy (Common…Mythic), 4 stol skini, 2 zal. Placeholder'lar (protsedural): stollar, kiylar, zal geometriyasi, lampa, kiy tokchasi; personaj — Quaternius UAL manekeni ([ART_PIPELINE §6](ART_PIPELINE.md#6-birinchi-asset-royxati-m1--vertical-slice) holati). Keyinga qoldirildi: Addressables (M4), Mythic maxsus animatsiyasi, 10× da eng yuqori raritetning alohida reveal'i, venue bottom-sheet o'rniga oddiy modal.

## M1.5 — Professional UI/UX (PUBG/eFootball darajasi) ✅

### M1.5.1 — UI kit, barcha ekranlar, HUD, MCP tekshiruv ✅
UITheme + UIKit (tugma holatlari, tween, ovoz/haptic), ikonka SpriteAtlas, 1920×1080 / match 0.5 / safe area, uz/ru/en (Unity Localization). Lobbi (3D avatar + kiy, rail'lar, PLAY), rejim+zal ekrani, Profil, Mening xonam (3D xona + tahrir), Inventar (9 tab, detal, Mythic upgrade), Stikerlar (kiy/rail decal), Sozlamalar (6 bo'lim, darhol til almashuvi), match HUD + natija, splash/dialog/offline/toast.
**Natija (2026-11-05):** MCP'da 1920×1080, 2400×1080, 1440×1080 (4:3) va Punch Hole (2340×1080, safe area) skrinshotlari; to'liq oqim splash → lobbi → profil → xona → inventar → sozlamalar (til) → rejim → match → natija → lobbi, Console toza; TMP audit (bo'sh/kesilgan matn) uz/ru da toza. EditMode 86/86, `npm test` 9/9.

### M1.5.2 — Hujjatlar va litsenziyalar ✅
[DESIGN_SYSTEM.md](DESIGN_SYSTEM.md) (yangi), UI_UX.md (lobbi layout, rejim ekrani, Profil, Mening xonam, Stikerlar, Sozlamalar), [ECONOMY §4.4](ECONOMY.md#44-stikerlar-m15) stikerlar; ArtSource/LICENSES.md ga 4 ta `type: icon` yozuvi (Material Symbols — Apache 2.0; game-icons.net — Delapouite, Lorc, Skoll, CC BY 3.0), 10 ta stiker original ish deb qayd etildi; LicenseRegistry'ga CC-BY-3.0/Apache-2.0 (faqat ikonkalar) va `icon` turi; CLAUDE.md, ART_PIPELINE §2, COMPLIANCE yangilandi.
**Natija (2026-11-05):** Validate All Art toza, Credits 22 yozuv ("Ikonkalar" guruhi bilan), EditMode 86/86.

### M1.5.3 — Tuzatishlar: "Yana o'ynash", standart til, uz audit ✅
"Yana o'ynash"dan keyin lobbi/Profil (Tarix) ochilib qolishi tuzatildi: UI'da klaviatura/gamepad navigatsiyasi va Submit o'chirildi (WASD/strelka tanlovni yashirin tugmalarga surib, Enter uni bosardi). Standart til — O'zbek: tizim tili selector'i olib tashlandi (CommandLine → saqlangan tanlov → uz), ru/en faqat Sozlamalar'dan, PlayerPrefs `language` da saqlanadi va splash'dan boshlab qo'llanadi. Uz matnlar: break/Spin/Foul/Lobby/Chat/Ranked/Season Pass va boshqalar tarjima qilindi, "8-BALL" kalitga o'tdi, "Versiya {0}" xom satri va zal kartasidagi nom/kirish ustma-ustligi tuzatildi, tarixdagi mehmon nomi joriy tilda ko'rsatiladi.
**Natija (2026-11-06):** uz tilida 37 ekran/holat skrinshoti — TMP audit toza; qurilma tili Russian bo'lganda birinchi ishga tushirish uz; ru tanlovi qayta ishga tushirishdan keyin saqlanadi. Console toza, EditMode 86/86, `npm test` 9/9. Qolganlar: credits'dagi litsenziya atributsiyalari asl (inglizcha) matnda qoldirildi; "NICE SHOT!" stikerining rasmidagi yozuv — art.

## M1.6 — 3D personajlar va "haqiqiy billiard zali" tajribasi ✅
**Maqsad:** o'yinchi haqiqiy zalda o'ynayotgandek his qilsin. **Hammasi faqat vizual:** zarba natijasi doim `BilliardGame.Physics` dan keladi, animatsiya natijaga ta'sir qilmaydi (online'da ham); personaj kodi `ShotParams`/`PhysicsConfig`/taymerga tegmaydi ([CLAUDE.md](../CLAUDE.md) qoida 4–5).

**Natijalar**
1. **Personajlar** ([GDD §8](GDD.md#8-avatarlar-va-personajlar)): kamida 4 ta har xil (erkak/ayol, turli kiyim uslubi, soch, teri rangi), Humanoid rig, **bitta** `PlayerCharacter.controller` (Animator). Lobby'da "Personaj" ekrani (tanlash + mavjud kiyim skinlari bilan birlashtirilgan, M14 slotlariga tayyor). Match'da har ikki o'yinchi o'z personaji bilan zalda (hot-seat'da ikkalasi, online'da raqib personaji `character_id` orqali).
2. **Match ichidagi holat mashinasi** (`BilliardGame.Avatar` → `MatchCharacterDirector`): `Idle → WalkToShot (NavMesh, stol/mebel atrofida) → [Chalk, ixtiyoriy, ~30%, skip qilinadi] → Address (egilish, chap qo'l bridge stolda, o'ng qo'l kiyda — Animation Rigging Two Bone IK + Multi-Aim) → Stroke (orqaga tortish kuch slayderiga bog'liq, oldinga zarba fizika `Shoot()` bilan bir kadrda) → Watch`; raqib navbatida `WalkToSeat → Sit (ichimlik ho'plash, o'ylash, kuzatish) → React (qarsak / xafa)`. Oyoq sirpanmasligi: yurish tezligi animatsiya stride'iga moslanadi + foot IK.
3. **Kamera to'sig'i:** personaj stol va kamera orasida qolsa — dithered shaffoflik (URP), juda yaqin bo'lsa renderer yashiriladi.
4. **Kamera rejimlari** (HUD tugmasi bilan aylanadi, Sozlamalar ▸ Boshqaruv'da standarti): Klassik (yuqoridan, hozirgi), 3-shaxs (yelka ortidan, kiy yo'nalishida), 1-shaxs (ko'z balandligi, kiy chizig'i bo'ylab — nishonlash shu rejimda ham ishlaydi), Kino (zarbadan keyin sharlarni kuzatish, luzaga tushishda yaqinlashish, tegish bilan skip). Cinemachine 3, silliq blend, devor/mebel orqali o'tmaslik (Deoccluder/Decollider).
5. **Zal hayoti:** fon NPC'lar (boshqa stollarda o'ynayotgan, bar yonida turgan, barmen — kam poly, LOD, 3–8 ta), ambient ovoz (zal shovqini, past musiqa), Sozlamalar sifatiga qarab NPC soni: Yuqori 8 / O'rta 4 / Past 0.
6. **Animatsiyalar:** CC0 Quaternius Universal Animation Library (yurish, turish, o'tirish, gaplashish, ko'tarish) + procedural IK (zarba holati, bridge, bo'rlash, ichish, qarsak, xafa bo'lish). Yetishmaganlari ro'yxati foydalanuvchiga beriladi; Mixamo kliplari faqat mixamo.com dan to'g'ridan-to'g'ri yuklangan FBX bo'lsa ([ART_PIPELINE §2](ART_PIPELINE.md#2-litsenziya-siyosati)). Yuz blendshape'lari bo'lsa ishlatiladi (hozirgi CC0 modellarda yo'q — procedural bosh/qosh harakati).

**Qabul qilish mezonlari**
- [x] 4+ personaj tanlash ekranida (MCP skrinshot), tanlov saqlanadi va match'da ko'rinadi.
- [x] Hot-seat match'da: navbat almashganda personaj stol atrofida NavMesh bo'ylab zarba nuqtasiga yuradi (stol/mebelga kirmaydi), egiladi, chap qo'l stolda, kiy aim yo'nalishida; raqib stulga o'tiradi; reaksiyalar ishlaydi (skrinshotlar).
- [x] Zarba vaqtida kiy harakati va fizika `Shoot()` bir kadrda (EditMode test: `StrokeTimeline` kontakt kadri = shoot kadri).
- [x] 4 kamera rejimi, Sozlamalar'dagi standart saqlanadi; 1-shaxsda nishonlash ishlaydi; kino kamera skip qilinadi.
- [x] Personaj kamerani to'ssa shaffof bo'ladi (skrinshot).
- [x] NPC'lar + ambient ovoz; Past sifatda NPC 0; match kadri ≤ 300k tri, ≤ 180 draw call (Profiler hisoboti — O'rta sifat; quyida).
- [x] Animatsiya natijaga ta'sir qilmasligi testi: bir xil `ShotParams` personaj bilan va personajsiz bir xil `SimulationResult` beradi; online cross-check (`npm test`) yashil.
- [x] Barcha yangi assetlar `LICENSES.md` da, Credits'da.

**Holat (2026-11-06):** **M1.6.1 ✅** — 4 personaj (Timur, Aziz, Malika, Dilnoza; Quaternius CC0 UBC + Fantasy Outfits, Humanoid), bitta `PlayerCharacter.controller` (Locomotion blend, Address, Sit Enter/Idle/Exit, Disappointed/Celebrate — Address/reaksiyalar procedural `.anim`), lobby ▸ "Personaj" ekrani (portretlar, turntable preview, Tanlash → profil `characterId`, "Kiyimlar" → Inventar ▸ Outfit), match'da `MatchCharacterDirector`: NavMesh bo'ylab zarba nuqtasiga yurish (tezlik = yurish klipi stride 1.03 m/s, sirpanishsiz), ~35% bo'rlash (skip), egilish + Two Bone IK (o'ng qo'l kiyda, chap qo'l bridge), zarba `StrokeTimeline` kontakt kadrida fizika bilan, raqib kreslada o'tiradi va choy ho'playdi, qarsak/xafa/xursand reaksiyalar; stolga oyoqlar qo'shildi. Farqlar: Multi-Aim o'rniga Humanoid `SetLookAt` (bosh/ko'krak), foot IK yo'q (yurish tezligi stride'ga moslangan). **M1.6.2 ✅** — `MatchCameraRig` (Cinemachine 3: brain + Classic/Aim/Follow kameralar, 0.6 s EaseInOut blend): Klassik (eski yuqori ko'rinish), 3-shaxs (to'p ortidan 1.05 m, 0.55 m balandlik), 1-shaxs (0.6 m, 0.2 m — kiy chizig'ida), Kino (3-shaxsda nishon, zarbadan keyin harakatlanayotgan sharlarni kuzatadi, luzaga tushganda 1.3 s yaqinlashadi, ekranga tegish → yuqori ko'rinish). 3-/1-shaxsda gorizontal swipe kiyni buradi (`CueController.SwipeAim`). HUD'da kamera tugmasi (keyingi rejim, standart sifatida saqlanadi), Sozlamalar ▸ Boshqaruv ▸ Kamera. `CharacterOcclusion`: kamera→cue ball / stol markazi chizig'i personaj kapsulasini (tos→bosh) kesib o'tsa — Bayer dither shader (`BilliardGame/Dithered Character`), kamera 0.35 m ichida bo'lsa — faqat soya. Deoccluder kerak emas: aim kamera stol chetidan ≤ 1.1 m. **M1.6.3 ✅** — `HallLife` (`BilliardGame.Avatar`): venue'dagi `CharacterSpot` Npc (turadi) / NpcSeat (o'tiradi) joylarini Index tartibida to'ldiradi, soni `HallRules.NpcCount` (Yuqori 8 / O'rta 4 / Past 0, batareya tejash = Past; EditMode test). NPC'lar shu 4 personaj prefabi: Unity 6 Mesh LOD (FBX'da `generateMeshLods`, NPC'da `forceMeshLod = 2` ≈ 4–6k tri), soyasiz, qoshlar o'chiq, kiyim/soch `MaterialPropertyBlock` bilan 6 xil rangga bo'yalgan, Animator `CullCompletely` va tasodifiy faza; turganlar cue ball'ga qaraydi, luzaga tushganda ~30% qarsak chaladi. Silk Road: choy peshtaxtasi (samovar, stakanlar) ortida barmen, divan/kreslo/kursida o'tirganlar, devor yonida turganlar (`BilliardGame ▸ Venue ▸ Add Hall NPC Spots + NavMesh` — lightmap qayta bake qilinmaydi); garajda 2 tomoshabin. Ovoz: `ProceduralAudio.HallAmbience` (12 s loop: jigarrang shovqin + uzoq gap-so'z + stakan jaranglashi) va `HallMusic` (16 s, Am–F–C–G yumshoq pad, original), 2D AudioSource, ovoz balandligi Sozlamalar effektlar/musiqa slayderlaridan, match davomida ham. Unumdorlik (Editor Game view 1112×567, Silk Road, Klassik kamera, `UnityStats`): NPC'siz 195 → stol mayda detallar (18 diamond bitta meshga, inlay/casting/mato soyasiz) → **O'rta (4 NPC): 177 draw call, 58 SetPass, 76k tri**; Yuqori (8 NPC): 198 draw call, 87k tri (Yuqori faqat kuchli qurilmalarda — 180 budjeti o'rta Android uchun); Past: NPC yo'q. 60 FPS ni haqiqiy o'rta Android qurilmada o'lchash hali qilinmadi (qurilma kerak). **Keyingi: M2.**
Mixamo papkasidagi 13 zip Sketchfab'dan uchinchi shaxs qayta yuklamalari (3 tasi *Neverness to Everness* o'yin personaji) — ishlatilmadi (CLAUDE.md qoida 8). Kerak bo'lsa mixamo.com dan to'g'ridan-to'g'ri (FBX, Without Skin, In Place): `Sitting Idle`, `Sitting Clap`, `Sitting Drinking`, `Clapping`, `Disappointed`, `Victory Idle`, `Thinking`, `Leaning` — `ArtSource/Incoming/mixamo/` ga.

**Bo'linish:** M1.6.1 personajlar + import + tanlash ekrani + match holat mashinasi (yurish, o'tirish, zarba IK, bo'r, reaksiya); M1.6.2 kamera rejimlari (Cinemachine) + shaffoflik; M1.6.3 zal hayoti (NPC, ambient, sifat sozlamasi) + unumdorlik hisobot. **Hajm:** L.

## M2 — Online 1v1 (to'liq) ✅
Stage 2 da bor: xona kodi, server-authoritative match, C#/TS cross-check, E2E bot. Qo'shiladi:
**Natijalar:** Tezkor o'yin — Nakama matchmaker (ticket `{mode, venue}`, M7 da MMR qo'shiladi), "Raqib qidirilmoqda" ekrani (taymer, bekor qilish); xona kodi oqimi yangi UI bilan; **reconnect** — 60 s oyna, socket qayta yaratiladi, server holatni (`OpState` — rack, navbat, guruhlar, taymer) qayta yuboradi, oynadan keyin texnik mag'lubiyat; **zarba taymeri** — server tomonida 30 s (5 s qolganda ogohlantirish), tugasa navbat o'tadi (foul emas, ball-in-hand yo'q — [GDD §4](GDD.md#4-core-loop-bitta-match)); **raqib personaji sinxron** — faqat holat eventlari (`OpAvatarEvent {kind: walk_to(spot_id), address, stroke_start, sit, react(kind), chalk}`, har kadrda pozitsiya yuborilmaydi; server faqat relay qiladi va rate-limit, natijaga ta'sir qilmaydi), `character_id` match boshida.
**Qabul:** 2 klient (editor + Windows bot) quick match → o'yin → "game over"; bitta klient tarmog'i 20 s uzilib qaytganda o'yin davom etadi; 60 s dan keyin texnik mag'lubiyat; taymer tugashi navbatni o'tkazadi (server testi); raqib personaji eventlar bo'yicha yuradi/o'tiradi (skrinshot); `npm test` + cross-check yashil. **Hajm:** M.
**Holat (2026-11-06) ✅:** Quick match tiketi `{mode: eightball, venue}` xossalari bilan (faqat bir xil venue juftlanadi), "Raqib qidirilmoqda 0:12" taymeri + bekor qilish; reconnect oynasi 60 s (klient 70 s urinadi, yangi socket, join metadata qayta yuboriladi, server to'liq snapshot beradi), keyin `END_DISCONNECT`; zarba taymeri serverda 30 s, 5 s qolganda halqa qizil + signal/vibratsiya. `character_id` join metadata (`ch`, regex bilan tozalanadi) → snapshot `players[].ch` → raqib personaji. `OP_AVATAR` (aim, chalk) faqat raqibga relay, ≤ 4/s, holatga tegmaydi; yurish/o'tirish/reaksiya ikkala klientda bir xil snapshot'dan. E2E (editor + Windows bot, lokal Nakama 3.41): quick match → o'yin → "game over: winner 0, reason 1"; editor socketi 32 s uzildi → o'yin davom etdi (shu orada editorning taymeri tugab navbat o'tdi), qayta ulanganda yakuniy holat keldi; ikkinchi match'da 60 s uzilish → "reason 3" (texnik mag'lubiyat), qayta ulangan klient natija ekranini ko'rsatdi; bot'ning `aim`/`chalk` eventlari editorga keldi. Server testlari 12/12 (60 s grace, 20 s drop, `ch` tozalash, avatar relay/rate-limit). Farq: taymer tugashi standart qoidadek ball-in-hand beradi (C#/TS bir xil, mavjud test) — "foul emas" varianti kerak bo'lsa ikkala tomonda birga o'zgartiriladi. Ma'lum: Windows bot log'ida `SelectedLocale is null` (Addressables localization Windows uchun build qilinmagan, faqat dev bot).

## M3 — Server iqtisodi, Player ID va "Tez kunda" bo'limlarini jonlantirish ⬜
**Natijalar**
1. **Player ID:** ro'yxatdan o'tishda (Nakama `afterAuthenticateDevice` hook) unikal 10 xonali raqamli ID: birinchi raqam 0 emas, tasodifiy (CSPRNG), ketma-ket emas, to'qnashuv — `player_ids` storage (system owner) yozuvi `version: "*"` (faqat yangi) bilan, to'qnashsa qayta urinish. Profilda ko'rsatiladi + nusxalash; do'st qo'shish ID orqali (`friend_add_by_pid` RPC).
2. **Wallet + ledger:** `economy/wallet.ts` — har harakat `walletUpdate` + metadata `{reason, ref_id, request_id}`, idempotency (`idempotency/<request_id>`), inventar, yashik RNG (HMAC) + pity, do'kon, dublikat → shard; klientda `NakamaEconomyService` (mock faqat editor/`STORE_MOCK`). `BilliardGame.Meta.Rules` (C#) ↔ TS cross-check.
3. **"Tez kunda" bo'limlari ishlaydi:** Vazifalar (kunlik 3 + haftalik 5, progress match eventlaridan serverda, mukofot olish, reset taymeri UTC+5); Season Pass (bepul + premium yo'lak, 50 daraja, XP, mukofotlar, sezon taymeri); Eventlar (vaqtli, masalan "Hafta oxiri 2× coin", maxsus yashik — banner + taymer); Kunlik mukofot (7 kunlik login kalendari); Klub (yaratish/qo'shilish, klub chati — Nakama groups + channel), Do'stlar ro'yxati, chat.
4. **Kontent serverdan:** barcha katalog/yashik/do'kon/vazifa shablonlari/pass/event/login jadvallari Nakama `config/<name>` storage'da (system owner, klient faqat o'qiydi), `config_get` (versiya/ETag) — admin panel (M4) yozadi. Kodda faqat seed (birinchi ishga tushirish uchun `config_seed`).
**Qabul:** server testlari — 10⁶ yashik ochish ±0.1% ([ECONOMY §5.2](ECONOMY.md#52-pity-bilan-samarali-ehtimol-kutilgan-qiymat)), bir xil `request_id` ikki marta → bitta debit, klient wallet/storage'ni yozolmaydi (permission testi), ledger har harakatda, Player ID 10⁵ generatsiyada formati to'g'ri va to'qnashuvsiz, vazifa/pass/login mukofotlari bir martalik, reset vaqti UTC+5; Unity'da lobby balansi, yashik natijasi, vazifalar, pass, eventlar, login kalendari, klub serverdan (skrinshotlar); config o'zgarsa klient build'siz yangilanadi. **Hajm:** L (M3.1 Player ID + wallet/inventar/yashik/do'kon/login; M3.2 vazifalar/pass/eventlar; M3.3 do'stlar/klub/chat).

## M4 — Admin panel ⬜
`admin/` papkasida: `admin/api` (NestJS) + `admin/web` (Next.js), Docker.
**Natijalar:** Login + TOTP 2FA, rollar: `superadmin`, `operator` (faqat ko'rish va yordam), `content_manager`. O'yinchilar: ID / nickname qidiruv, profil, balans, inventar, tranzaksiyalar (ledger), valyuta/item berish-olish (sabab majburiy), ban/unban. Kontent: skinlar katalogi (nom, kategoriya, raritet, narx, Addressables bundle kaliti, preview rasm, faol/nofaol, sotuv sanalari), yashiklar (tarkib, ehtimollar — yig'indi 100% tekshiruvi, pity, simulyatsiya preview), do'kon takliflari va bundle'lar, vazifa shablonlari, Season Pass darajalari, eventlar (boshlanish/tugash, mukofotlar), kunlik mukofotlar, push/pochta e'lonlari (Nakama notification). Har kontent o'zgarishi → `admin_config_publish` (server-to-server, http_key) → Nakama `config`. Analitika dashboard: DAU/MAU, yangi o'yinchilar, retention D1/D7, daromad, ARPDAU, eng ko'p sotilgan itemlar, yashik ochishlar. Audit log (kim, qachon, nima, before/after).
**Yangi skin oqimi** (hujjat: [TECH_ARCHITECTURE §2.4](TECH_ARCHITECTURE.md#24-kontent-yetkazish-addressables)): Unity Addressables build (Remote guruh) → `Tools/cdn-upload` CI skripti (S3-mos CDN; lokal — MinIO/Caddy static) → admin'da katalogga ro'yxat → o'yin ilovani yangilamasdan skinni yuklab ko'rsatadi. Bitta test skin bilan boshidan oxirigacha tekshiriladi.
**Qabul:** API e2e testlari (rollar: operator yozolmaydi, 2FA'siz kirish yo'q, yig'indi ≠ 100% publish rad), grant → Nakama wallet + ledger + audit; test skin build'siz o'yinda ko'rinadi (MCP skrinshot); dashboard raqamlari seed ma'lumotdan to'g'ri; brauzer skrinshotlari. **Hajm:** L (M4.1 auth/rollar/o'yinchilar/audit; M4.2 kontent + config publish; M4.3 Addressables/CDN oqimi + analitika).

## M5 — Veb-do'kon ⬜
`web/` papkasida Next.js (+ to'lov API `admin/api` ichidagi `payments` moduli yoki alohida `web/api` — M5 da tanlanadi, hujjatda yoziladi).
**Natijalar:** O'yinchi ID kiritadi → server nickname + avatar qaytaradi (boshqa shaxsiy ma'lumotsiz) → tasdiqlash → Gem paketi → to'lov → Gem hisobga tushadi (`web_topup_credit`) va o'yinda bildirishnoma. `PaymentProvider` interfeysi: Payme, Click, Uzum adapterlari + `MockProvider`; hozircha sandbox/test rejim, kalitlar `.env` da (keyin faqat kalit almashtiriladi). Webhook imzosini tekshirish, idempotentlik (`provider_txn_id UNIQUE`), qayta urinish, to'lov holati sahifasi, chek (email ixtiyoriy). O'z dizayni (o'yin uslubida), uz/ru/en, mobil versiya; ommaviy oferta, maxfiylik siyosati, qaytarish siyosati — shablon matnlar, **"yurist tekshiradi"** belgisi bilan.
**Compliance:** `STORE_PLAY` build'da veb-do'konga havola yo'q (kodda ham kompilyatsiya qilinmaydi, avtomatik skan); `STORE_DIRECT` flavor'ida to'g'ridan-to'g'ri to'lov mumkin ([COMPLIANCE §1](COMPLIANCE.md#1-tolov-siyosati-google-play--app-store)).
**Qabul:** mock va Payme/Click sandbox'da to'lov → bitta kredit; takroriy webhook → kredit yo'q; noto'g'ri imzo → 401; summa narxga teng emas → rad; o'yinda bildirishnoma (MCP skrinshot); sayt skrinshotlari (desktop + mobil, 3 til). **Hajm:** M.

## M6 — Infra va deploy ⬜
**Natijalar:** Ildizda `docker-compose.yml` (nakama, postgres, admin-api, admin-web, web-shop, caddy) — `docker compose up` bilan hammasi lokal ishlaydi; `.env.example`. `deploy/`: Contabo (Ubuntu 24.04, Docker o'rnatilgan, foydalanuvchi `doniyor`) uchun bir buyruqli `deploy.sh` (rsync/ssh, `docker compose pull/up -d`, migratsiya, health check, backup cron), `Caddyfile` — `api.`, `admin.`, `shop.` subdomenlar (avtomatik HTTPS); server IP va domen `.env` orqali. Admin panel va Nakama console internetga ochiq emas yoki IP allowlist + 2FA bilan (`ADMIN_ALLOW_IPS`), Nakama console faqat SSH tunnel orqali.
**Qabul:** lokal `docker compose up` → barcha health check yashil, E2E (o'yin → server, sayt → to'lov mock → kredit, admin → grant); `deploy.sh --dry-run` toza; Caddyfile `caddy validate` dan o'tadi. Real deploy — foydalanuvchi IP/domen berganda. **Hajm:** S.

## M7 — Venue'lar + progressiya ⬜
**Natijalar:** XP/level ([GDD §6.1](GDD.md#61-level--xp)); venue 1–4 (kirish escrow, sovrin, 5% rake) va venue tanlash; matchmaker MMR + venue (Quick Match kengayishi); natija ekrani (coin/XP count-up).
**Qabul:** uzilishda escrow to'g'ri; balans yetmasa venue yopiq; 2 klient bilan MMR quick match E2E. **Hajm:** M.

## M8 — Ranked + leaderboard ⬜
**Natijalar:** Glicko-2 MMR, RP va darajalar Bronze→Legend ([GDD §6.2](GDD.md#62-ranked-darajalari)), mavsum (8 hafta) va soft reset, rage-quit jarimasi, leaderboard global/mamlakat/do'st, Leaderboard ekrani.
**Qabul:** MMR birlik testlari, mavsum almashinuvi simulyatsiyasi, leaderboard ekrani serverdan. **Hajm:** M.

## M9 — Offline rejimlar + 9-ball ⬜
**Natijalar:** AI shot planner (Gameplay, [GDD §2.1](GDD.md#21-ai-qiyinlik-darajalari)), vs AI 4 daraja, Mashq rejimi (erkin joylashtirish, 5 trick-shot), 9-ball qoidalari C# + TS (cross-check), tutorial (FTUE [GDD §11](GDD.md#11-onboarding-ftue)).
**Qabul:** "Usta" AI 100 simulyatsiyada "Yangi" AI'ni ≥ 90% yutadi; 9-ball qoida testlari; cross-check yashil. **Hajm:** M.

## M10 — Play Billing va country rules ⬜
**Natijalar:** build flavor'lar (`STORE_PLAY/DIRECT/MOCK`), Unity IAP + `iap_google_validate`, refund revoke; `country_rules` (BE: `paid_random_disabled`); birinchi xarid bonusi, Starter Pack.
**Qabul:** Play internal testing'da test xarid → Gem kreditlanadi, takroriy receipt rad; Play AAB'da Direct satrlari yo'q (avtomatik skan); BE konfiguratsiyasida yashik xaridi yopiq. **Hajm:** M.

## M11 — LiveOps (qolgan) ⬜
**Natijalar:** achievementlar, kunlik aylanadigan do'kon (seed har o'yinchiga barqaror), craft, Mythic upgrade (Kristall), premium+ pass.
**Qabul:** barcha mukofotlar bir martalik; do'kon seed barqaror; 45 kunlik Pass XP simulyatsiyasi ([ECONOMY §6](ECONOMY.md#6-season-pass)). **Hajm:** M.

## M12 — Ijtimoiy (qolgan) ⬜
**Natijalar:** quick chat/emoji (match stikerlari raqibga ham), so'kish filtri, report/block, Vivox match ovozi (server token), spectate, replay (oxirgi 20 match).
**Qabul:** 2 klientda ovoz ulanadi/mute ishlaydi; replay natijasi asl match bilan bir xil (deterministik). **Hajm:** L.

## M13 — Turnirlar ⬜
**Natijalar:** 8/16 o'yinchilik knockout (kirish, bracket, sovrin), Turnir ekrani, admin'da turnir jadvali.
**Qabul:** 8 bot bilan turnir oxirigacha. **Hajm:** M.

## M14 — Avatar va effektlar ⬜
**Natijalar:** avatar slotlari (bosh/ust/shim/poyabzal/aksessuar/set) M1.6 personajlari ustida, emotsiyalar, zarba trail, luza effekti, g'alaba animatsiyasi, nik ranglari, ramkalar, raqib portreti (avtomatik render).
**Qabul:** har slot uchun ≥ 2 item; effektlar past sifatda 60 FPS ni buzmaydi (profiler). **Hajm:** M.

## M15 — Launch tayyorgarligi ⬜
**Natijalar:** analitika eventlari + daily_metrics (M4 dashboard bilan), crash reporting, anti-cheat (ShotParams validatsiyasi, rate limit, Play Integrity), grafika sifat darajalari + Auto benchmark, akkaunt o'chirish, [COMPLIANCE §8](COMPLIANCE.md#8-launch-oldidan-checklist-m15) checklist, PEGI/EEA yashik qarori, soft launch (O'zbekiston, Play).
**Qabul:** checklist to'liq; o'rta segment qurilmada 60 FPS, past segmentda 30 FPS (real qurilma hisoboti); crash-free ≥ 99.5% (ichki test). **Hajm:** L.

## M16 — Snooker + iOS ⬜
**Natijalar:** 12 ft stol `PhysicsConfig`, snooker qoidalari (C# + TS), iOS build, StoreKit + `purchaseValidateApple`.
**Qabul:** snooker qoida testlari; TestFlight build; test xarid. **Hajm:** L.
