# TECH_ARCHITECTURE — Texnik arxitektura

> Holat: v1.0 (2026-11-05). Mavjud: Unity 6 (6000.5) URP klient, o'z 2D fizikasi (`BilliardGame.Physics`, 1/240 s), 8-ball qoidalari (`BilliardGame.Gameplay`), Nakama 3.41 + TS match moduli (bit-exact C#/TS fizika, `Tools/CrossCheck`), xona kodi bilan online 1v1.

## 1. Umumiy ko'rinish

```
┌──────────────── Unity klient (Android/iOS) ────────────────┐
│ UI · Presentation · Meta (lobby/shop/pass) · Avatar         │
│ Gameplay (qoidalar) · Physics (sim) · Online (Nakama SDK)   │
│ Addressables (skin/zal) · Billing (Play/Direct) · Vivox      │
└───────┬────────────────────────┬────────────────┬──────────┘
        │ HTTPS RPC + WebSocket  │ CDN (bundles)  │ Vivox (ovoz)
┌───────▼────────────────────┐ ┌─▼──────────┐ ┌───▼────────┐
│ Nakama (TS runtime)        │ │ CDN / CCD  │ │ Unity Vivox│
│ auth · match · matchmaker  │ └────────────┘ └────────────┘
│ wallet+ledger · storage    │
│ leaderboard · tournament   │◄──── server-to-server (http_key) ────┐
│ groups · chat · IAP valid. │                                      │
└───────┬────────────────────┘                                      │
        │ PostgreSQL 16                                             │
┌───────▼──────┐   read replica    ┌──────────────────────────────┐ │
│ nakama DB    │──────────────────►│ NestJS backend (alohida repo)│─┘
└──────────────┘                   │ admin API · webhooks (Payme, │
                                   │ Click, Uzum, Google RTDN)    │
                                   │ analytics ingest · audit     │
                                   │ admin DB (PostgreSQL)        │
                                   └──────┬───────────────┬───────┘
                                          │               │
                                 ┌────────▼─────┐  ┌──────▼──────────┐
                                 │ Admin panel  │  │ Veb-do'kon      │
                                 │ Next.js      │  │ Next.js (top-up)│
                                 └──────────────┘  └─────────────────┘
```

Tamoyillar ([CLAUDE.md](../CLAUDE.md)): fizika — o'z simulyatsiyamiz; online — server-authoritative; iqtisod — faqat serverda; Presentation'da qoida yo'q.

## 2. Klient

### 2.1 Assembly'lar (har Scripts papkasi uchun bitta .asmdef)

| Assembly | Mavjud | Vazifa | UnityEngine |
|----------|--------|--------|-------------|
| `BilliardGame.Physics` | ✅ | Sim, `ShotParams` → frames + events | ❌ |
| `BilliardGame.Gameplay` | ✅ | 8-ball qoidalari, keyin 9-ball, AI shot planner | ❌ |
| `BilliardGame.Online` | ✅ | Nakama klient, match protokoli | ✅ |
| `BilliardGame.Presentation` | ✅ | Stol/shar/kiy ko'rinishi, kamera | ✅ |
| `BilliardGame.UI` | ✅ | HUD, menyular | ✅ |
| `BilliardGame.Meta` | M1 | Klient tomondagi iqtisod modeli (faqat ko'rsatish), `IEconomyService` (Mock / Nakama), katalog, inventar holati | ✅ (minimal) |
| `BilliardGame.Meta.Rules` | M2 | Server bilan umumiy sof logika: crate odds/pity kalkulyatori, Pass level hisoblash (C# ↔ TS cross-check) | ❌ |
| `BilliardGame.Content` | M1 | `SkinDefinition`, `VenueDefinition` SO, Addressables yuklovchi, equip qo'llash | ✅ |
| `BilliardGame.Avatar` | M1 (oddiy) / M10 | Lobby personaji, kiyim slotlari, emotsiyalar | ✅ |
| `BilliardGame.Billing` | M6 | `IStoreChannel`: `PlayBillingChannel`, `DirectPayChannel`, `EditorMockChannel` | ✅ |
| `BilliardGame.Social` | M8 | Do'stlar, klublar, chat, Vivox | ✅ |
| `BilliardGame.Services` | M11 | Analitika, crash, lokalizatsiya, Remote Config klienti | ✅ |

### 2.2 Build flavor'lar
Scripting define symbols + `BuildProfile` (Unity 6 Build Profiles):

| Flavor | Define | To'lov | Veb-do'kon havolasi | Tarqatish |
|--------|--------|--------|---------------------|-----------|
| Play | `STORE_PLAY` | Google Play Billing | **Yo'q** (kodda ham kompilyatsiya qilinmaydi) | Google Play (AAB) |
| Direct | `STORE_DIRECT` | Payme / Click / Uzum (veb checkout, ilova ichida WebView yoki tashqi brauzer) | Ha | Sayt (APK), imzo boshqa kalit bilan |
| App Store | `STORE_APPLE` | StoreKit (keyin) | Yo'q | App Store |
| Dev | `STORE_MOCK` | Editor mock | — | Ichki |

Klient auth paytida `store_channel` ni yuboradi; server ham faqat shu kanalga mos to'lov usullarini qaytaradi (ikki qavatli himoya).

### 2.3 Rendering va unumdorlik (URP mobil)
- Maqsad: o'rta segmentda 60 FPS, past segmentda 30 FPS; termal throttling'da avtomatik sifat pasaytirish (Adaptive Performance moduli mavjud).
- **SRP Batcher** yoqilgan; **GPU Resident Drawer** (Unity 6) statik zal geometriyasi uchun — u bilan static batching ishlatilmaydi; URP'da `MaterialPropertyBlock` dan qochish, rang variantlari uchun Material Variants ([Unity docs: draw call usullari](https://docs.unity3d.com/kr/current/Manual/optimizing-draw-calls-choose-method.html), [URP: CPU ishini kamaytirish](https://docs.unity3d.com/cn/current/Manual/urp/reduce-rendering-work-on-cpu.html)). Eslatma: GPU Resident Drawer Vulkan/compute talab qiladi — GLES qurilmalarda SRP Batcher'ga tushadi.
- Yoritish: zal — baked lightmap + light probes; stol ustida 1 ta real-time spot/directional (soya faqat sharlar uchun, soft shadow o'chiq past sifatda); reflection probe (baked).
- Post-processing: Bloom (past sifatda o'chiq), Tonemapping ACES, Vignette; MSAA 2× (yuqori) / FXAA (past).
- Sifat darajalari: Past / O'rta / Yuqori / Auto (birinchi ishga tushirishda GPU benchmark 3 s).
- Render scale: 0.8 (past) – 1.0 (yuqori); 120 Hz qurilmalarda 60 Hz cap (batareya).
- Profiling: Unity Profiler + Frame Debugger, Android'da `adb` orqali real qurilma; M1 dan boshlab har milestone'da FPS/draw call/tri hisobot.
- Sim: allokatsiyasiz (mavjud qoida), Update'da LINQ yo'q.

### 2.4 Kontent yetkazish (Addressables)
- Lokal (APK ichida): asosiy UI, default stol/kiy/sharlar, Garaj zali, shriftlar, lokalizatsiya. Maqsad: base module ≤ 150 MB (Play limiti 200 MB — Play Console'da tekshirish), birinchi yuklash ≤ 80 MB.
- Remote: barcha kosmetika, zallar, avatar kiyimlari, mavsum temalari. Guruhlar `Remote_*`, Build Remote Catalog yoqilgan, content update build faqat o'zgargan bundle'larni yuklatadi ([Addressables remote content](https://docs.unity3d.com/Packages/com.unity.addressables@2.8/manual/remote-content-intro.html)).
- CDN: Unity Cloud Content Delivery ([Addressables + CCD](https://docs.unity.com/en-us/ccd/use-addressables-with-ccd)) yoki istalgan S3-mos CDN (Cloudflare R2). Katalog URL Remote Config'dan → CDN almashtirish build'siz.
- Play Asset Delivery (ixtiyoriy, katta zallar uchun): `com.unity.addressables.android` ([docs](https://docs.unity3d.com/Packages/com.unity.addressables.android@1.0/manual/index.html)).
- Oldindan yuklash: matchmaking paytida raqibning jihozlari (kiy, shar to'plami) + venue bundle; yuklanmasa default skin (o'yin hech qachon kutmaydi).

### 2.5 Remote Config
Nakama `config` storage kolleksiyasi (system user egasi, faqat o'qish), `config_get` RPC: katalog versiyasi, narxlar, yashik jadvallari, eventlar, `min_client_version`, CDN URL, feature flag'lar, `country_rules`. Klient keshlaydi (ETag/versiya). Admin panel yozadi (NestJS → Nakama server-to-server RPC `admin_config_publish`). Alohida Unity Remote Config qo'shilmaydi — bitta haqiqat manbai.

## 3. Online o'yin (mavjud + kengaytirish)
- Match: Nakama authoritative match handler (TS), tick'siz voqea asosida; klient `OpShot` (ShotParams) yuboradi → server `physics.ts` bilan simulyatsiya → `OpSnapshot` (frames/events). Mavjud opcode'lar: `OnlineProtocol.cs` / `protocol.ts`.
- Matchmaker: ticket properties `{mode, venue, mmr, region, store_channel}`, query `+properties.mode:8ball +properties.venue:3 properties.mmr:>=1350 properties.mmr:<=1650`; 10 s/20 s da oraliq kengayadi ([Nakama matchmaker](https://heroiclabs.com/docs/nakama/concepts/multiplayer/matchmaker/)). `registerMatchmakerMatched` mavjud.
- Ranked: match oxirida server MMR (Glicko-2) va RP ni hisoblaydi, leaderboard'ga yozadi.
- Spectate: match handler'ga "spectator" presence (faqat qabul qiladi), 2 s kechikish.
- Replay: `match_history` storage: boshlang'ich rack seed + ShotParams ro'yxati (fizika deterministik → klientda qayta ijro).
- Anti-cheat: `ShotParams` validatsiyasi (power ∈ [0, max], spin |v| ≤ R·0.5, burchak chekli son, navbat egasi, zarba vaqti ≤ limit + 2 s), RPC rate limit (o'yinchi/daqiqa), g'ayritabiiy statistika (masalan 98% aniqlik × 50 match) → admin'ga flag. Play Integrity API (M11).

## 4. Server (Nakama)

### 4.1 Modullar (Server/nakama/modules/src)

| Fayl | Mavjud | Vazifa |
|------|--------|--------|
| `main.ts` | ✅ | InitModule, ro'yxatdan o'tkazish |
| `match.ts`, `physics.ts`, `rules.ts`, `protocol.ts`, `random.ts` | ✅ | 8-ball match |
| `economy/wallet.ts` | M2 | `walletUpdate` o'rami: reason/ref_id/idempotency |
| `economy/catalog.ts` | M2 | Config'dan katalog, narxlar |
| `economy/crates.ts` | M2 | RNG (HMAC), pity, dublikat → shard |
| `economy/shop.ts` | M2 | Kunlik do'kon, xarid |
| `economy/craft.ts` | M2 | Craft, Mythic upgrade |
| `progress/*.ts` | M3/M4/M7 | XP/level, ranked, vazifalar, login, Pass, achievements |
| `venues.ts` | M3 | Kirish escrow, sovrin, rake |
| `iap/google.ts` | M6 | `nk.purchaseValidateGoogle`, kreditlash |
| `iap/web.ts` | M6 | NestJS'dan kelgan veb top-up (server-to-server) |
| `social/*.ts` | M8 | Klub hooklari, chat moderatsiya |
| `tournaments.ts` | M9 | 8/16 bracket |
| `config.ts` | M2 | Remote Config |

### 4.2 RPC API (klient → server)

Barcha javoblar: `{ ok: boolean, data?: T, error?: { code: string, message: string } }`. O'zgartiruvchi RPC'lar `request_id` (UUID) qabul qiladi — idempotentlik (bir xil `request_id` → saqlangan javob qaytadi, 24 soat).

| RPC | Kirish | Chiqish | Milestone |
|-----|--------|---------|-----------|
| `create_room` / `join_room` | mavjud | mavjud | ✅ |
| `config_get` | `{version?}` | config (yoki `not_modified`) | M2 |
| `profile_get` | — | level, xp, rank, equipped, wallet | M2 |
| `inventory_list` | `{type?}` | itemlar, shard, kristall | M2 |
| `equip` | `{slot, item_id}` | equipped | M2 |
| `crate_open` | `{crate_id, count: 1\|10, request_id}` | `[{item_id, rarity, duplicate, shards}]`, pity, wallet | M2 |
| `crate_odds` | `{crate_id}` | jadval + pity holati | M2 |
| `shop_list` | — | kunlik do'kon, bundle'lar | M2 |
| `shop_buy` | `{offer_id, request_id}` | wallet, items | M2 |
| `craft` / `upgrade` | `{item_id, request_id}` | item, materiallar | M2/M7 |
| `daily_login_claim` | `{request_id}` | mukofot | M2 |
| `venue_list` / `venue_enter` | `{venue_id}` | matchmaker ticket ma'lumoti | M3 |
| `ranked_status` | — | RP, daraja, mavsum | M4 |
| `missions_list` / `missions_claim` / `missions_reroll` | | | M7 |
| `pass_status` / `pass_claim` / `pass_buy` | | | M7 |
| `iap_google_validate` | `{product_id, receipt, request_id}` | wallet, grant | M6 |
| `web_store_link` (faqat `STORE_DIRECT`) | — | imzolangan checkout URL | M6 |
| `friends_*`, `club_*` | Nakama built-in API + hooklar | | M8 |
| `tournament_*` | | | M9 |
| `account_delete_request` | — | o'chirish navbatga qo'yiladi | M11 |

Server-to-server (http_key, faqat NestJS'dan): `web_topup_credit`, `admin_grant`, `admin_ban`, `admin_config_publish`, `iap_refund_revoke`.

### 4.3 Ma'lumotlar modeli

Nakama o'rnatilgan jadvallari: `users`, `user_device`, `wallet` (users.wallet JSON), `wallet_ledger`, `storage`, `leaderboard`, `leaderboard_record`, `groups`, `group_edge`, `user_edge`, `message`, `purchase`, `subscription`, `notification`.

Storage kolleksiyalari (owner = user, read 1 / write 0 — klient yozolmaydi):

| Kolleksiya / key | Maydonlar |
|------------------|-----------|
| `profile/main` | `level, xp, mmr, rd, rp, rank_tier, season_id, equipped{cue,table_cloth,rails,balls,venue_theme,outfit{...},frame,emote[4],trail,pocket_fx,victory,nick_color}` |
| `inventory/items` | `items: {item_id: {acquired_at, source, level?}}`, `shards, crystals, keys` (OCC versiya bilan) |
| `pity/<crate_type>` | `since_legendary, since_mythic, since_epic, total_opens` |
| `progress/daily` | `login_streak, last_login_day, missions[{id, progress, claimed}], reroll_used` |
| `pass/<season_id>` | `xp, level, premium, premium_plus, claimed_free[], claimed_premium[]` |
| `idempotency/<request_id>` | `rpc, response, created_at` (TTL tozalash cron) |
| `crate_rolls/<roll_id>` | `crate, secret_ver, counter, rarity, item, pity_before` |
| `match_history/<match_id>` | `players, mode, venue, rack_seed, shots[], result` |

System-owner kolleksiyalar: `config/<name>` (katalog, crates, shop, pass, events, country_rules), `tournaments/<id>`.

Leaderboardlar: `ranked_<season>` (score = RP), `ranked_<season>_<country>`, `weekly_wins` (reset har dushanba), klub `club_weekly`.

### 4.4 Ledger
Nakama `walletUpdate(userId, changeset, metadata, updateLedger=true)` har chaqiruvda `wallet_ledger` ga yozadi. `metadata = {reason, ref_id, request_id, store_channel}`. Admin panel "Tranzaksiyalar" shu jadvaldan (read replica) o'qiydi. Bir nechta valyuta bir vaqtda o'zgarsa (masalan crate: −Gem, +Shard) — bitta `walletsUpdate` + `storageWrite` OCC bilan, xatoda butun operatsiya bekor.

## 5. NestJS backend + Admin + Veb-do'kon (alohida repo)

### 5.1 NestJS
- Modullar: `auth` (admin SSO + 2FA, rollar: owner, admin, support, analyst), `catalog` (skinlar, raritet, Addressables kalitlari), `crates` (jadval, pity, versiya — publish oldidan ehtimollar yig'indisi = 100% tekshiruvi), `pricing`, `events`, `season-pass`, `users` (qidiruv, profil, inventar, grant, ban), `transactions` (ledger, purchases, web orders), `payments` (Payme/Click/Uzum webhook'lari), `google-rtdn` (refund/void), `analytics` (ingest + agregatlar), `audit` (har admin harakati).
- Admin DB (PostgreSQL) jadvallari:

```sql
admin_users(id, email, role, totp_secret, created_at)
audit_log(id, admin_id, action, target_type, target_id, before jsonb, after jsonb, created_at)
catalog_items(id, type, rarity, name_key, addressable_key, tradeable=false, active, season_id)
crate_tables(id, crate_id, version, odds jsonb, pity jsonb, published_at, published_by)
offers(id, kind, items jsonb, price_currency, price_amount, starts_at, ends_at, country_rules jsonb)
web_orders(id, provider, provider_txn_id UNIQUE, user_id, gem_amount, amount_uzs, state, created_at, credited_at)
analytics_events(ts, user_id, session_id, name, props jsonb) PARTITION BY RANGE (ts)
daily_metrics(day, dau, new_users, d1, d7, d30, revenue_usd, payers, arpdau, conversion)
```

### 5.2 Admin panel (Next.js)
Sahifalar: Dashboard (DAU, retention, ARPDAU, konversiya, daromad kanal bo'yicha), Katalog, Yashiklar va ehtimollar (preview + simulyatsiya), Narxlar/Takliflar, Eventlar, Season Pass, Foydalanuvchilar (inventar, ledger, grant, ban/unban), Tranzaksiyalar, Config publish (diff + tasdiqlash), Audit.

### 5.3 Veb-do'kon (Next.js)
- Kirish: o'yinchi ID (profilda nusxalash) yoki deep-link token (Direct build'dan). ID bo'yicha nik va avatar ko'rsatiladi (tasdiqlash uchun), shaxsiy ma'lumot ko'rsatilmaydi.
- Paket tanlash (UZS) → provider checkout → webhook → NestJS → Nakama `web_topup_credit` → o'yinchiga Nakama notification.
- Play build bilan bog'liq hech qanday havola yo'q; sayt o'zi mustaqil marketing kanali ([COMPLIANCE §1](COMPLIANCE.md#1-tolov-siyosati-google-play--app-store)).

## 6. To'lovlar

### 6.1 Google Play Billing (Play build)
1. Klient: Unity IAP (Google Play Billing Library 7+) → `purchase.originalJson` + signature.
2. `iap_google_validate` RPC → `nk.purchaseValidateGoogle` (service account: `client_email`, `private_key` Nakama config'da) → Nakama normallashtirib `purchase` jadvaliga yozadi, `seenBefore` flag replay'ni bloklaydi ([Nakama Google IAP](https://heroiclabs.com/docs/nakama/concepts/iap-validation/google/)).
3. `seenBefore == false` bo'lsa → `walletUpdate` (reason `iap_google`, ref `orderId`), keyin klient `consume`/`acknowledge` qiladi (3 kun ichida acknowledge bo'lmasa Google refund qiladi).
4. Refund/void: Google RTDN → Nakama purchase holatini yangilaydi ([Nakama IAP](https://heroiclabs.com/docs/nakama/concepts/iap-validation)); NestJS cron `voided purchases` → `iap_refund_revoke`.

### 6.2 Payme / Click / Uzum (veb-do'kon va Direct build)
- **Payme Merchant API** (JSON-RPC, Payme → bizning endpoint): `CheckPerformTransaction` → `CreateTransaction` → `PerformTransaction` (kreditlash shu yerda) → `CheckTransaction`/`CancelTransaction`/`GetStatement`; Basic auth (merchant key), Payme IP allowlist ([Payme docs](https://developer.help.paycom.uz/metody-merchant-api/performtransaction/)). Fiskal ma'lumot (`detail`) — O'zbekiston talabi.
- **Click SHOP-API**: `Prepare` (action 0) → `Complete` (action 1), imzo `md5(click_trans_id + service_id + SECRET_KEY + merchant_trans_id + [merchant_prepare_id] + amount + action + sign_time)` ([Click docs](https://docs.click.uz/en/?p=15)).
- **Uzum**: merchant integratsiyasi shartnomadan keyin (hujjatlari merchant kabinetida).
- Idempotentlik: `web_orders.provider_txn_id UNIQUE`; kreditlash `state=performed` ga o'tishda **bir marta**; Nakama tomonda `request_id = provider:txn_id`.
- Fraud: summa katalogdagi narxga teng bo'lishi shart, bir user'ga kunlik limit (masalan 5 mln UZS), yangi akkauntga birinchi 24 soat limit, chargeback → `iap_refund_revoke`.

## 7. Ovozli chat (Vivox)
- Paket `com.unity.services.vivox`; kanal = match ID (2 kishi, positional emas). Token serverda (Nakama RPC `vivox_token`) imzolanadi — klientda secret yo'q.
- Narx: oyiga 5 000 PCU gacha bepul, keyin $2 000 / 5 000 PCU ([Unity FAQ](https://support.unity.com/hc/en-us/articles/31045802890260), [pricing](https://unity.com/ru/products/gaming-services/pricing)). PCU monitoring + feature flag (`voice_enabled`) bilan cheklash. Muqobil: LiveKit (self-host).
- Mikrofon default o'chiq, raqibni mute, report.

## 8. Auth
- Device ID (birinchi ishga tushish), keyin Google (Play Games / Google Sign-In) ulash. Telegram/telefon — keyin.
- Session 24 soat (mavjud), refresh token. Ban: Nakama `banUsers` + NestJS audit.

## 9. Analitika, crash, lokalizatsiya
- **Analitika:** klient eventlari → Nakama RPC `events_batch` (yoki Nakama Events API) → NestJS `analytics_events`. Asosiy eventlar: `session_start/end`, `tutorial_step`, `match_start/end {mode, venue, result, duration, shots}`, `crate_open {crate, rarity}`, `shop_view/purchase`, `iap_start/success/fail`, `pass_buy`, `level_up`, `rank_change`, `ad_hoc errors`. Agregatlar kunlik cron'da `daily_metrics` ga.
- **Crash reporting:** Firebase Crashlytics (Unity SDK) yoki Unity Cloud Diagnostics — M11 da tanlov (IL2CPP symbol upload majburiy).
- **Lokalizatsiya:** Unity Localization paketi, String Tables: `uz` (lotin), `ru`, `en`; server matnlari (katalog nomlari) kalit sifatida keladi. Shrift: lotin+kirill.

## 10. Infratuzilma
- Dev: `Server/docker-compose.yml` (Postgres 16.14 + Nakama 3.41).
- Prod (boshlang'ich): 1 × Nakama (4 vCPU/8 GB) + managed PostgreSQL (kunlik backup, PITR), NestJS 1 × (2 vCPU), CDN. Hosting joylashuvi — shaxsiy ma'lumotlar qonuni bo'yicha ([COMPLIANCE §5](COMPLIANCE.md#5-shaxsiy-malumotlar)).
- Secret'lar: env/secret manager (server_key, http_key, Google service account, Payme/Click kalitlari, crate HMAC secret). Repo'da faqat dev default'lar (mavjud holat).
- Monitoring: Nakama Prometheus metrikalari + Grafana, alert: xato darajasi, match tugatish %, to'lov webhook xatolari.
