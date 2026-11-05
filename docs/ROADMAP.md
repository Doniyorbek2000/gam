# ROADMAP — Milestone'lar

> Holat: v1.0 (2026-11-05). Ish tartibi: har sessiyada "keyingi milestone" — birinchi `⬜` holatdagi milestone. Har milestone oxirida: Unity'da MCP orqali tekshiruv (play mode, skrinshot, Console xatosiz), testlar (`EditMode` NUnit + `Server/nakama/modules` `npm test`), commit, qisqa hisobot va bu fayldagi holatni `✅` ga o'zgartirish.
>
> Hajm: **S** ≈ 1 sessiya, **M** ≈ 1–2 sessiya, **L** ≈ 2–3 sessiya (agar milestone bir sessiyaga sig'masa, `Mx.1`, `Mx.2` ga bo'linadi va shu yerda yoziladi).

## Bajarilgan (oldingi bosqichlar)
- ✅ Stage 1 — fizika yadrosi, stol, kiy aim/power/spin, 8-ball qoidalari, hot-seat, APK build.
- ✅ Stage 2 — Nakama online 1v1 (xona kodi, server-authoritative, C#/TS cross-check, E2E bot).

## Umumiy jadval

| # | Milestone | Asosiy natija | Hajm | Holat |
|---|-----------|---------------|------|-------|
| M1 | Vertical slice | Premium zal + 3 kiy + 2 stol skini + PUBG uslubidagi lobby + yashik ochish (offline mock) + 8-ball shu zalda | L | ✅ |
| M1.5 | Professional UI/UX | Design system, barcha lobbi ekranlari, Profil, Mening xonam, Stikerlar, Sozlamalar, match HUD/natija, uz/ru/en, ko'p o'lchamda MCP tekshiruv | L | ✅ |
| M2 | Server iqtisod yadrosi | Wallet/ledger, inventar, katalog, yashik RNG+pity, do'kon, login — serverda; mock → Nakama | L | ⬜ |
| M3 | Kontent yetkazish + venue'lar + progressiya | Addressables remote/CDN, Remote Config, XP/level, 4 venue (kirish/sovrin), MMR quick match | L | ⬜ |
| M4 | Ranked + leaderboard | Daraja tizimi, mavsum, Glicko-2, global/mamlakat/do'st leaderboard | M | ⬜ |
| M5 | Offline rejimlar + 9-ball | vs AI (4 daraja), Mashq, 9-ball qoidalari (C# + TS) | M | ⬜ |
| M6 | To'lovlar | Build flavor'lar, Google Play Billing + validatsiya, NestJS + veb-do'kon (Payme/Click), country rules | L | ⬜ |
| M7 | LiveOps meta | Season Pass, vazifalar, achievementlar, kunlik do'kon, event yashik, craft, Mythic upgrade | L | ⬜ |
| M8 | Ijtimoiy | Do'stlar, klublar, chat, quick chat, Vivox ovoz, spectate, replay | L | ⬜ |
| M9 | Turnirlar + Admin MVP | 8/16 knockout, admin panel (katalog, yashik, narx, user, ban, tranzaksiya, analitika) | L | ⬜ |
| M10 | Avatar va effektlar | To'liq avatar kastomizatsiya, emotsiyalar, trail, luza effekti, g'alaba animatsiyasi, nik ranglari | M | ⬜ |
| M11 | Launch tayyorgarligi | Lokalizatsiya uz/ru/en, analitika, crash, anti-cheat, unumdorlik, compliance checklist, soft launch (UZ) | L | ⬜ |
| M12 | Snooker + iOS | 12 ft stol, snooker qoidalari, App Store build va IAP | L | ⬜ |

---

## M1 — Vertical slice ✅
**Maqsad:** o'yinning "ko'rinishi va hissi"ni bitta to'liq yo'lakda isbotlash: lobby → yashik → jihozlash → premium zalda 8-ball.

**Natijalar**
1. **Asset pipeline (minimal):** `ArtSource/LICENSES.md` (format: [ART_PIPELINE §4](ART_PIPELINE.md#4-litsenziya-reyestri-va-credits)), `BilliardGame ▸ Art ▸ Import Incoming` (litsenziya tekshiruvi, tri/tekstura budjeti, ASTC, URP Lit, prefab, `SkinDefinition`), `Generate Credits` + Credits ekrani.
2. **Kontent:** `BilliardGame.Content` assembly; `SkinDefinition` (cue, table_cloth+rails), `VenueDefinition` SO; 3 kiy (Rare / Epic / Legendary), 2 stol skini (Classic, Modern), sharlar — mavjud procedural to'plam.
3. **Silk Road Lounge zali:** venue sahnasi (baked lighting, reflection probe, lampa), stol zalga joylashtirilgan; mavjud 8-ball (hot-seat va online xona) shu zalda ishlaydi. Garaj — mavjud Main sahnasi (default zal).
4. **PUBG uslubidagi lobby** ([UI_UX §2](UI_UX.md#2-lobby)): 3D personaj (avatar-base yoki placeholder), swipe bilan aylantirish, yonida jihozlangan kiy; yuqori bar (profil, Coin/Gem), chap/o'ng relslar (Pass/Vazifa — "tez orada" holati; Do'kon/Yashik/Inventar — ishlaydi), "O'YNASH" + rejim/venue tanlash (Hot-seat, Do'st bilan, Silk Road Lounge).
5. **Offline mock iqtisod:** `BilliardGame.Meta` — `IEconomyService` + `MockEconomyService` (lokal JSON saqlash, boshlang'ich 5 000 Coin + 500 Gem), [ECONOMY §5](ECONOMY.md#5-yashiklar-crates) dagi Premium/Classic jadvallari va pity bilan lokal RNG. Kod interfeysi M2 da Nakama implementatsiyasi bilan almashtiriladi.
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

**Natija (2026-11-05):** EditMode 65/65 (shu jumladan Meta 13), `npm test` 9/9; match kadri Silk Road Lounge'da 59k tri / 96 draw call; online xona E2E (editor + Windows bot) shu zalda "game over"gacha o'ynaldi. Kontent: 7 kiy (Common…Mythic), 4 stol skini, 2 zal. Placeholder'lar (protsedural): stollar, kiylar, zal geometriyasi, lampa, kiy tokchasi; personaj — Quaternius UAL manekeni ([ART_PIPELINE §6](ART_PIPELINE.md#6-birinchi-asset-royxati-m1--vertical-slice) holati). Keyinga qoldirildi: Addressables (M3), Mythic maxsus animatsiyasi, 10× da eng yuqori raritetning alohida reveal'i, venue bottom-sheet o'rniga oddiy modal.

## M1.5 — Professional UI/UX (PUBG/eFootball darajasi) ✅

### M1.5.1 — UI kit, barcha ekranlar, HUD, MCP tekshiruv ✅
UITheme + UIKit (tugma holatlari, tween, ovoz/haptic), ikonka SpriteAtlas, 1920×1080 / match 0.5 / safe area, uz/ru/en (Unity Localization). Lobbi (3D avatar + kiy, rail'lar, PLAY), rejim+zal ekrani, Profil, Mening xonam (3D xona + tahrir), Inventar (9 tab, detal, Mythic upgrade), Stikerlar (kiy/rail decal), Sozlamalar (6 bo'lim, darhol til almashuvi), match HUD + natija, splash/dialog/offline/toast.
**Natija (2026-11-05):** MCP'da 1920×1080, 2400×1080, 1440×1080 (4:3) va Punch Hole (2340×1080, safe area) skrinshotlari; to'liq oqim splash → lobbi → profil → xona → inventar → sozlamalar (til) → rejim → match → natija → lobbi, Console toza; TMP audit (bo'sh/kesilgan matn) uz/ru da toza. EditMode 86/86, `npm test` 9/9.

### M1.5.2 — Hujjatlar va litsenziyalar ✅
[DESIGN_SYSTEM.md](DESIGN_SYSTEM.md) (yangi), UI_UX.md (lobbi layout, rejim ekrani, Profil, Mening xonam, Stikerlar, Sozlamalar), [ECONOMY §4.4](ECONOMY.md#44-stikerlar-m15) stikerlar; ArtSource/LICENSES.md ga 4 ta `type: icon` yozuvi (Material Symbols — Apache 2.0; game-icons.net — Delapouite, Lorc, Skoll, CC BY 3.0), 10 ta stiker original ish deb qayd etildi; LicenseRegistry'ga CC-BY-3.0/Apache-2.0 (faqat ikonkalar) va `icon` turi; CLAUDE.md, ART_PIPELINE §2, COMPLIANCE yangilandi.
**Natija (2026-11-05):** Validate All Art toza, Credits 22 yozuv ("Ikonkalar" guruhi bilan), EditMode 86/86.



## M2 — Server iqtisod yadrosi ⬜
**Natijalar:** Nakama TS: `economy/wallet|catalog|crates|shop|craft`, `config.ts` (Remote Config `config_get`), `profile_get`, `inventory_list`, `equip`, `crate_open`, `crate_odds`, `shop_list`, `shop_buy`, `daily_login_claim`; HMAC RNG + pity; ledger metadata; `request_id` idempotentlik; klientda `NakamaEconomyService` (mock editor'da qoladi). `BilliardGame.Meta.Rules` — odds/pity kalkulyatori C# va TS da, cross-check.
**Qabul:** server testlari: 10⁶ ochish simulyatsiyasi ±0.1% ([ECONOMY §5.2](ECONOMY.md#52-pity-bilan-samarali-ehtimol-kutilgan-qiymat)), bir xil `request_id` ikki marta → bitta debit, klient wallet'ni o'zgartira olmaydi (storage permission testi), ledger har harakatda; Unity'da lobby balansi serverdan, yashik natijasi serverdan; Docker stack'da E2E. **Hajm:** L.

## M3 — Kontent yetkazish + venue'lar + progressiya ⬜
**Natijalar:** Addressables (Remote guruhlar, remote catalog, CDN URL Remote Config'dan, matchmaking paytida prefetch, default fallback); XP/level ([GDD §6.1](GDD.md#61-level--xp)); venue 1–4 (kirish escrow, sovrin, 5% rake) va venue tanlash ekrani; matchmaker MMR + venue bo'yicha (Quick Match); natija ekrani (coin/XP count-up).
**Qabul:** yangi kiy CDN'dan build'siz paydo bo'ladi (content update build testi); uzilishda escrow to'g'ri; balans yetmasa venue yopiq; 2 klient bilan quick match E2E. **Hajm:** L.

## M4 — Ranked + leaderboard ⬜
**Natijalar:** Glicko-2 MMR, RP va darajalar Bronze→Legend ([GDD §6.2](GDD.md#62-ranked-darajalari)), mavsum (8 hafta) va soft reset, rage-quit jarimasi, leaderboard global/mamlakat/do'st, Leaderboard ekrani.
**Qabul:** MMR birlik testlari (ma'lum misollar), mavsum almashinuvi simulyatsiyasi, leaderboard ekrani serverdan. **Hajm:** M.

## M5 — Offline rejimlar + 9-ball ⬜
**Natijalar:** AI shot planner (Gameplay, [GDD §2.1](GDD.md#21-ai-qiyinlik-darajalari)), vs AI 4 daraja, Mashq rejimi (erkin joylashtirish, 5 trick-shot vazifa), 9-ball qoidalari C# + TS (cross-check), tutorial (FTUE [GDD §11](GDD.md#11-onboarding-ftue)).
**Qabul:** "Usta" AI 100 simulyatsiya qilingan matchda "Yangi" AI'ni ≥ 90% yutadi; 9-ball qoida testlari; cross-check yashil. **Hajm:** M.

## M6 — To'lovlar ⬜
**Natijalar:** build flavor'lar (`STORE_PLAY/DIRECT/MOCK`), Unity IAP + `iap_google_validate`, refund revoke; yangi repo (NestJS + Next.js veb-do'kon): Payme Merchant API, Click SHOP-API, `web_orders`, `web_topup_credit`; `country_rules` (BE: `paid_random_disabled`); Gem paketlari, birinchi xarid bonusi, Starter Pack.
**Qabul:** Play internal testing'da test xarid → Gem kreditlanadi, takroriy receipt rad; Payme/Click sandbox'da to'lov → bitta kredit (takroriy webhook → kredit yo'q); Play AAB'da Direct satrlari yo'q (avtomatik skan); BE konfiguratsiyasida yashik xaridi yopiq. **Hajm:** L.

## M7 — LiveOps meta ⬜
**Natijalar:** Season Pass (bepul/premium/premium+, 50 level), kunlik/haftalik vazifalar, login 7-kun tsikli, achievementlar, kunlik aylanadigan do'kon, event yashik (cheklangan vaqt), craft, Mythic upgrade (Kristall), Season Pass va Vazifalar ekranlari.
**Qabul:** Pass XP oqimi bo'yicha 45 kunlik simulyatsiya ([ECONOMY §6](ECONOMY.md#6-season-pass)), kunlik reset UTC+5, do'kon seed har o'yinchiga barqaror, barcha mukofotlar bir martalik. **Hajm:** L.

## M8 — Ijtimoiy ⬜
**Natijalar:** do'stlar, klublar (30 a'zo, rollar, chat), shaxsiy chat + so'kish filtri, report/block, quick chat/emoji, Vivox match ovozi (server token), spectate, replay (oxirgi 20 match).
**Qabul:** 2 klientda: do'st qo'shish → taklif → match; ovoz ulanadi/mute ishlaydi; replay natijasi asl match bilan bir xil (deterministik). **Hajm:** L.

## M9 — Turnirlar + Admin panel MVP ⬜
**Natijalar:** 8/16 o'yinchilik knockout turnir (kirish, bracket, sovrin), Turnir ekrani; admin repo: katalog, yashik jadvali (publish + simulyatsiya), narxlar/takliflar, foydalanuvchilar (grant/ban), tranzaksiyalar, Dashboard (DAU, retention, ARPDAU, konversiya), audit log.
**Qabul:** 8 bot bilan turnir oxirigacha; admin'da jadval o'zgartirish → klientda build'siz yangi ehtimol; har admin harakati audit'da. **Hajm:** L.

## M10 — Avatar va effektlar ⬜
**Natijalar:** avatar slotlari (bosh/ust/shim/poyabzal/aksessuar/set), emotsiyalar, zarba trail, luza effekti, g'alaba animatsiyasi, nik ranglari, ramkalar, raqib portreti (avtomatik render).
**Qabul:** har slot uchun ≥ 2 item; effektlar past sifatda 60 FPS ni buzmaydi (profiler). **Hajm:** M.

## M11 — Launch tayyorgarligi ⬜
**Natijalar:** Unity Localization (uz/ru/en), analitika eventlari + daily_metrics, crash reporting, anti-cheat (ShotParams validatsiyasi, rate limit, Play Integrity), grafika sifat darajalari + Auto, akkaunt o'chirish, maxfiylik siyosati, [COMPLIANCE §8](COMPLIANCE.md#8-launch-oldidan-checklist-m11) checklist, PEGI/EEA yashik qarori, soft launch (O'zbekiston, Play).
**Qabul:** checklist to'liq; o'rta segment qurilmada 60 FPS, past segmentda 30 FPS (real qurilma hisoboti); crash-free ≥ 99.5% (ichki test). **Hajm:** L.

## M12 — Snooker + iOS ⬜
**Natijalar:** 12 ft stol `PhysicsConfig`, snooker qoidalari (C# + TS), iOS build, StoreKit + `purchaseValidateApple`.
**Qabul:** snooker qoida testlari; TestFlight build; test xarid. **Hajm:** L.
