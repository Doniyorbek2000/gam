# ECONOMY — Iqtisod dizayni

> Holat: v1.0 (2026-11-05). Barcha balans, inventar, yashik RNG va xarid tekshiruvi **faqat serverda** ([TECH_ARCHITECTURE §4](TECH_ARCHITECTURE.md#4-server-nakama)). Bu hujjatdagi raqamlar — boshlang'ich konfiguratsiya; ular Remote Config (Nakama `config` storage) orqali build'siz o'zgartiriladi.
>
> Valyuta kursi: **1 USD ≈ 11 942 UZS** (O'zR Markaziy banki, 2026-11-03, [zamin.uz](https://zamin.uz/en/economy/226188-exchange-rates-for-november-3-announced.html)). UZS narxlar psixologik yaxlitlangan.

## 1. Valyutalar va resurslar

| Resurs | Turi | Qayerdan | Nimaga sarflanadi | Pulga sotiladimi |
|--------|------|----------|-------------------|------------------|
| **Coin** | soft | match g'alabasi, login, vazifalar, level, Pass, achievement | venue kirish, Classic yashik, Common/Rare do'kon mahsulotlari | Bilvosita (Gem → Coin almashuv, bundle'lar) |
| **Gem** | hard | xarid (Play / veb-do'kon / APK), oz miqdorda bepul (Pass, login, mavsum) | Premium/Event yashik, Season Pass, do'kon, Gem→Coin | Ha |
| **Shard (Bo'lak)** | material | dublikatlar, yashik ichida | Craft (aniq kosmetikani yaratish) | Yo'q |
| **Kristall** | material | Mythic dublikat, Premium Pass, event do'koni | Mythic upgrade | Yo'q |
| **Premium kalit** | token | login 7-kun, Pass | 1 ta Premium yashikni bepul ochish | Yo'q |

Qoidalar:
- O'yinchilar o'rtasida savdo/almashuv **yo'q** (itemlar real pul qiymatiga ega bo'lmasligi uchun — [COMPLIANCE §2](COMPLIANCE.md#2-loot-box-yashik-qoidalari)).
- Coin va Gem naqd pulga qaytarib almashtirilmaydi.
- Gem→Coin kursi: 100 Gem = 25 000 Coin (bundle'larda +20% gacha). Paid loot box cheklangan mamlakatlarda Classic yashik ham o'chadi, chunki coin bilvosita sotiladi (§9).

## 2. No pay-to-win tamoyili

**Qaror:** kosmetika o'yin kuchiga ta'sir qilmaydi. Hech bir kiy, stol, shar yoki effekt fizik parametrni, aim chizig'i uzunligini, zarba vaqtini yoki matchmaking'ni o'zgartirmaydi.

Asoslash:
1. **Raqobat yaxlitligi.** Ranked va turnirlar mahoratga asoslangan bo'lishi kerak; kuchli kiyni sotib olish mahoratni pulga almashtiradi va ranked darajalarini qadrsizlantiradi. Ko'plab mashhur billiard o'yinlarida kiylar kuch/aim/spin/vaqt statlariga ega — bu bizning asosiy farqlovchi xususiyatimiz bo'ladi ("hamma uchun bir xil stol").
2. **Arxitektura buni majbur qiladi.** Server-authoritative simulyatsiya faqat `ShotParams` ni qabul qiladi ([CLAUDE.md](../CLAUDE.md) qoida 1–2). `ShotParams` da skin identifikatori yo'q; `PhysicsConfig` venue/rejim bo'yicha, o'yinchi bo'yicha emas. Shu sabab "pay-to-win" ni tasodifan qo'shib bo'lmaydi.
3. **Uzoq muddatli daromad.** Adolatsizlik hissi bepul o'yinchilarni (DAU ning ~99%) ketkazadi; ular payer'lar uchun raqib va tomoshabin. Kosmetikani "ko'rsatish" qiymati ijtimoiy funksiyalar (lobby, spectate, replay, klub) orqali oshiriladi.
4. **Regulyatsiya va reyting.** Faqat kosmetik random itemlar ko'p yurisdiksiyalarda kamroq xavfli hisoblanadi; Gollandiyada o'yin ustunligini beradigan pulli loot box'lar ayniqsa nazorat ostida ([promise.legal](https://blog.promise.legal/loot-box-laws-game-developers/)).
5. **Monetizatsiya baribir kuchli:** PUBG Mobile va eFootball'da eng ko'p sotiladigan narsalar — kosmetika, pass va cheklangan vaqtli kolleksiyalar.

Tekshirish: Gameplay/Physics testlarida `ShotParams` va `PhysicsConfig` ga skin ma'lumoti kirmasligi kod review qoidasi.

## 3. Raritet

| Raritet | Rang (UI) | Belgisi | Vizual daraja |
|---------|-----------|---------|---------------|
| Common | kulrang `#9AA3AE` | 1 nuqta | Oddiy material |
| Rare | ko'k `#3D8BFF` | 2 nuqta | Rangli material, logotip |
| Epic | binafsha `#A64DFF` | 3 nuqta | Maxsus material, kichik effekt |
| Legendary | oltin `#FFB020` | 4 nuqta | To'liq effekt (trail yoki luza) |
| Mythic | qizil-pushti `#FF3B5C`, animatsiyali | toj | Animatsiyali, 5 bosqich upgrade |

Rang + shakl (nuqtalar) birga — rang ajrata olmaydiganlar uchun.

## 4. Narxlar

### 4.1 Gem paketlari

| Paket | Gem (asos + bonus) | USD (Play) | UZS (veb/APK) | Izoh |
|-------|--------------------|-----------:|--------------:|------|
| Hovuch | 80 | 0.99 | 12 000 | |
| Xalta | 400 + 20 | 4.99 | 59 000 | |
| Quti | 800 + 100 | 9.99 | 119 000 | "Eng ommabop" yorlig'i |
| Sandiq | 1 600 + 300 | 19.99 | 239 000 | |
| Xazina | 4 000 + 1 000 | 49.99 | 599 000 | "Eng foydali" yorlig'i |
| Saroy | 8 000 + 2 500 | 99.99 | 1 199 000 | |

- **Birinchi xarid bonusi:** har bir paketning birinchi xaridida asos gem ×2 (bir martalik, paket bo'yicha).
- Play narxlari Play Console narx shablonlari orqali; UZS lokal narxi Play'da mavjud bo'lsa shu jadval, bo'lmasa USD tier avtomatik konvertatsiyasi (Play Console'da tekshirish kerak).
- Veb-do'konda narx UZS da; Play build ichida veb-do'kon haqida hech qanday ma'lumot yo'q ([COMPLIANCE §1](COMPLIANCE.md#1-tolov-siyosati-google-play--app-store)).

### 4.2 Maxsus takliflar

| Taklif | Tarkibi | Narx | Cheklov |
|--------|---------|------|---------|
| Starter Pack | Epic kiy + 300 Gem + 20 000 Coin + 3 Premium kalit | $2.99 / 35 000 UZS | Bir marta, ro'yxatdan o'tgandan 72 soat |
| Oylik karta | Darhol 300 Gem + 30 kun × 40 Gem (jami 1 500) | $4.99 / 59 000 UZS | Har kuni kirib olish kerak |
| Venue bundle | Zal temasi + mos stol skini + 50 000 Coin | 1 200 Gem | Venue ochilganda 48 soat |
| Haftalik bundle | Aylanadi | 300–1 500 Gem | Haftasiga 1 marta |

Taymerlar faqat haqiqiy muddatni ko'rsatadi (soxta "oxirgi imkoniyat" yo'q). PEGI'da countdown takliflar kamida PEGI 12 ga olib keladi ([pocketgamer.biz](https://www.pocketgamer.biz/pegi-expands-age-ratings-to-cover-loot-boxes-and-in-game-spending)) — biz baribir PEGI 16 bo'lamiz (§9).

### 4.3 Do'kondagi to'g'ridan-to'g'ri narxlar

| Raritet | Coin narxi | Gem narxi | Craft (Shard) | Dublikat → Shard |
|---------|-----------:|----------:|--------------:|-----------------:|
| Common | 1 500 | — | 30 | 5 |
| Rare | 6 000 | 150 | 100 | 20 |
| Epic | — | 450 | 300 | 60 |
| Legendary | — | 1 400 | 1 200 | 250 |
| Mythic | — | 6 000 (faqat "featured", o'tgan mavsum) | 4 000 (o'tgan mavsum) | 800 + 10 Kristall |

**Kunlik aylanadigan do'kon:** har kuni 00:00 (UTC+5) da 6 slot: 2 Common/Rare (coin), 2 Rare/Epic (gem), 1 Legendary (gem), 1 "Featured" (bundle yoki Mythic). Har bir o'yinchi uchun server seed'i bilan; inventarda borlari chiqmaydi.

## 5. Yashiklar (crates)

Ehtimollar jadvali xariddan **oldin**, yashik kartasidagi "i" tugmasida va xarid tasdiqlash oynasida ko'rsatiladi (Google Play va Apple talabi — [COMPLIANCE §2](COMPLIANCE.md#2-loot-box-yashik-qoidalari)).

### 5.1 Turlari va asosiy ehtimollar

| Raritet | Classic (2 500 Coin) | Premium (100 Gem; 10× = 900) | Event (120 Gem; 10× = 1 080) |
|---------|---------------------:|-----------------------------:|-----------------------------:|
| Common | 70.0% | 50.0% | 40.0% |
| Rare | 24.0% | 30.0% | 33.0% |
| Epic | 5.5% | 15.0% | 20.0% |
| Legendary | 0.5% | 4.5% | 6.0% |
| Mythic | — | 0.5% | 1.0% |
| **Pity** | Epic+ kafolati: 15-ochishda | Legendary+: 40; Mythic: 100 | Legendary+: 30; Mythic: 80 |

- Raritet ichida har bir item teng ehtimol (UI har bir itemning ehtimolini alohida ham ko'rsatadi: `raritet% / itemlar soni`).
- Premium 10× ochishda kamida 1 Epic+ kafolatlangan.
- Pity hisoblagichi yashik turi bo'yicha, o'yinchi profilida; UI'da "Legendary'gacha: 23 ta qoldi" ko'rinadi. Event pity keyingi event yashigiga o'tadi (yo'qolmaydi).
- Mythic tushsa Legendary+ hisoblagichi ham nolga tushadi.

### 5.2 Pity bilan samarali ehtimol (kutilgan qiymat)

Hard pity `N` va asosiy ehtimol `p` uchun birinchi tushishgacha kutilgan ochishlar: `E = (1 − (1−p)^N) / p`; samarali ehtimol `1/E`.

| Yashik | Maqsad | p | N | E (ochish) | Samarali | Kutilgan narx |
|--------|--------|---|---|-----------:|---------:|--------------:|
| Classic | Epic+ | 6.0% | 15 | 10.1 | 9.9% | ≈ 25 200 Coin |
| Premium | Legendary+ | 5.0% | 40 | 17.4 | 5.7% | ≈ 1 570 Gem (10× bilan) ≈ $17 |
| Premium | Mythic | 0.5% | 100 | 78.8 | 1.27% | ≈ 7 090 Gem ≈ $78 |
| Event | Legendary+ | 7.0% | 30 | 12.7 | 7.9% | ≈ 1 370 Gem |
| Event | Mythic | 1.0% | 80 | 55.3 | 1.81% | ≈ 5 970 Gem ≈ $65 |

UI'da asosiy ehtimol **va** "pity bilan umumiy (konsolidatsiyalangan) ehtimol" ko'rsatiladi. Server testi (M2) 10⁶ ochishni simulyatsiya qilib shu jadval bilan ±0.1% mosligini tekshiradi.

### 5.3 RNG
- Serverda: `roll = HMAC-SHA256(server_secret_vN, user_id + ":" + crate_type + ":" + roll_counter)` → birinchi 52 bit → [0,1). `server_secret` env'da (klientga hech qachon chiqmaydi), versiyalanadi va rotatsiya qilinadi. Natija deterministik va auditga yaroqli, lekin oldindan bilib bo'lmaydi.
- Har bir ochish `crate_roll` yozuvi sifatida saqlanadi (secret versiyasi, hisoblagich, raritet, item, pity holati) — audit va nizolar uchun.
- Klient hech qachon natijani tanlamaydi; animatsiya natija serverdan kelgandan keyin boshlanadi.

## 6. Season Pass

- Davomiyligi: 8 hafta, 50 level, har level 1 000 Pass XP.
- Pass XP: match 50 (g'alaba 80), kunlik vazifalar jami ~600, haftalik ~3 000. Faol o'yinchi (kuniga ~8 match) ~45 kunda tugatadi.
- **Premium:** 800 Gem. **Premium+:** 2 000 Gem (+15 level darhol, eksklyuziv ramka, +10% Pass XP).
- Level sotib olish: 1 level = 60 Gem (mavsumning oxirgi 2 haftasida 40 Gem).

| Level | Bepul yo'lak | Premium yo'lak |
|------:|--------------|----------------|
| 1 | 500 Coin | Mavsum kiyi (Epic, Pass bilan evolyutsiya qiladi) |
| 5 | Classic yashik | 100 Gem |
| 10 | 20 Gem | Avatar kostyumi (Epic) |
| 15 | 2 000 Coin | Premium kalit ×2 |
| 20 | Rare emotsiya | Mavsum kiyi → Legendary ko'rinish |
| 25 | 30 Gem | Shar to'plami (Legendary) |
| 30 | Classic yashik ×2 | Kristall ×20 |
| 35 | 5 000 Coin | Luza effekti (Legendary) |
| 40 | 50 Gem | Premium kalit ×3 + 100 Gem |
| 45 | Rare ramka | G'alaba animatsiyasi (Legendary) |
| 50 | Mavsum nishoni + 50 Gem | Mavsum kiyi → final ko'rinish + Mythic Kristall ×30 |

- Bepul yo'lak jami: ~150 Gem, ~20 000 Coin, 3 yashik. Premium jami: ~200 Gem qaytadi (pass narxining 25%).
- Oraliq levellarda Coin, Shard, Pass XP boost.

## 7. Coin oqimi (sources / sinks)

| Manba (kunlik, faol bepul o'yinchi, level 10) | Coin |
|-----------------------------------------------|-----:|
| Login (o'rtacha) | 400 |
| Kunlik vazifalar (3 × 300) | 900 |
| Matchlar sof foydasi (Eski Shahar / Silk Road, 55% g'alaba) | ~1 200 |
| Level up / achievement (o'rtacha) | 500 |
| **Jami** | **~3 000** |

| Sink | Mexanizm |
|------|----------|
| Venue rake | 5% har bir coin matchda |
| Classic yashik | 2 500 Coin |
| Do'kon (Common/Rare) | 1 500–6 000 |
| Turnir kirishi | 10% fondga kirmaydi |

Maqsad: bepul o'yinchi har 1–2 kunda 1 Classic yashik ochadi; yuqori venue'lar Gem→Coin talabini yaratadi. Inflyatsiya monitoringi: admin panelda "o'rtacha balans / DAU" va "sink/source nisbati" (maqsad 0.9–1.1).

## 8. Gem bepul manbalari (mavsum, ~8 hafta)
Login (~10/hafta → 80) + bepul Pass (~150) + ranked mavsum oxiri (Gold 50 … Legend 300) + achievementlar (~50) ≈ **300–550 Gem** → mavsumda 3–5 Premium yashik. Bepul o'yinchi ham tizimni his qiladi.

## 9. Mamlakat bo'yicha konfiguratsiya

Server `country_rules` konfiguratsiyasi (IP geolokatsiya + Play billing country + profil), klient faqat natijani ko'rsatadi:

| Qoida | Mamlakatlar (boshlang'ich) | Ta'sir |
|-------|----------------------------|--------|
| `paid_random_disabled` | BE (Belgiya) | Premium/Event/Classic yashik **sotib olish** o'chadi; yashik itemlari do'konda to'g'ridan-to'g'ri narxda (§4.3); Gem→Coin almashuvi qoladi; bepul berilgan yashiklar ham "itemni tanlash" ga almashtiriladi |
| `odds_on_purchase_screen` | Hamma joyda (KR, CN talabi) | Ehtimollar xarid ekranining o'zida |
| `no_complete_set_gacha` | Hamma joyda (JP kompu gacha) | Random itemlar to'plamini to'ldirish uchun mukofot yo'q |
| `review_required` | NL | Default yoqilgan, huquqiy ko'rib chiqishgacha monitoring |

Asoslar: [COMPLIANCE §2](COMPLIANCE.md#2-loot-box-yashik-qoidalari).

## 10. Ledger va idempotentlik (iqtisod tomondan)
- Har bir valyuta harakati — Nakama wallet ledger yozuvi + metadata: `reason` (`match_win`, `venue_entry`, `crate_open`, `iap_google`, `web_topup`, `admin_grant`, `refund_revoke` …), `ref_id` (match/order/roll ID), `balance_after`.
- Har bir pul tushumi o'z `order_id`/`transaction_id` bilan **bir marta** kreditlanadi.
- Refund/chargeback: Gem qaytarib olinadi; balans manfiyga tushishi mumkin (manfiy balansda do'kon yopiq). Batafsil: [TECH_ARCHITECTURE §6](TECH_ARCHITECTURE.md#6-tolovlar).

## 11. Daromad modeli

### 11.1 Benchmarklar (manbalar bilan)
- Retention mediani (2025, 16 000+ mobil o'yin): D1 ≈ 22%, D7 < 4%, D30 ≈ 0.7–0.8%; top 10%: D1 ≈ 40%, D7 ≈ 12% ([GameAnalytics 2026 benchmarks](https://gamedevreports.substack.com/p/gameanalytics-mobile-and-pc-game)).
- Kunlik payer konversiyasi: top 15% o'yinlar 0.8–1.2% ([playio](https://blog.playio.co/arppu-mobile-games)). Janrga xos (billiard/multiplayer casual) ARPDAU ochiq benchmarki topilmadi — quyidagi raqamlar taxmin va soft launch'da tekshiriladi.
- Google Play xizmat haqi: yiliga birinchi $1M uchun 15%, undan yuqori 30% (standart dastur); AQSh/UK/EEA'da 2026-yildan muqobil billing va tashqi havola dasturlari alohida shartlar bilan ([Play Console Help](https://support.google.com/googleplay/android-developer/answer/15582165), [9to5google](https://9to5google.com/2026/06/24/google-play-store-external-billing-june-30/)).

### 11.2 Ssenariylar (launch'dan 6 oy keyin, oylik)

| Ko'rsatkich | Konservativ | Asosiy | Optimistik |
|-------------|------------:|-------:|-----------:|
| DAU | 5 000 | 20 000 | 60 000 |
| Kunlik payer konversiyasi | 0.6% | 1.0% | 1.4% |
| Payer'ning kunlik o'rtacha xaridi | $3.0 | $3.5 | $4.5 |
| ARPDAU | $0.018 | $0.035 | $0.063 |
| Yalpi oylik daromad | $2 700 | $21 000 | $113 400 |
| Kanal ulushi (Play / veb+APK) | 70/30 | 60/40 | 60/40 |
| Komissiyalar (Play 15%, Payme/Click ~2%*) | −$460 | −$3 300 | −$17 900 |
| **Sof (soliqdan oldin)** | **≈ $2 240** | **≈ $17 700** | **≈ $95 500** |
| Server + CDN + monitoring | $150 | $400 | $1 500 |
| Vivox (5 000 PCU gacha bepul) | $0 | $0 | $0–2 000 |

\* Payme/Click komissiyasi merchant shartnomasiga bog'liq — shartnoma tuzishda aniqlash kerak.

Daromad tarkibi (asosiy ssenariy maqsadi): Gem paketlari 55%, Season Pass 20%, Starter/Oylik karta 15%, bundle'lar 10%. Marketing (UA) xarajati kiritilmagan; LTV ≈ ARPDAU × o'rtacha umr (kunlarda). Maqsadli retention (D1 35% / D7 12% / D30 5%) da 90 kunlik o'rtacha umr ~9 kun → LTV ≈ $0.30; UA faqat CPI shundan past bo'lgan kanallarda.

### 11.3 KPI maqsadlari (soft launch, O'zbekiston)
D1 ≥ 35%, D7 ≥ 12%, D30 ≥ 5%, kunlik payer ≥ 0.8%, Pass premium ulushi (MAU dan) ≥ 3%, crash-free sessions ≥ 99.5%, match tugatish darajasi ≥ 90%.
