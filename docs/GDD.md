# GDD — Game Design Document

> Holat: v1.0 (2026-11-05). Bog'liq hujjatlar: [ECONOMY](ECONOMY.md) · [UI_UX](UI_UX.md) · [ART_PIPELINE](ART_PIPELINE.md) · [TECH_ARCHITECTURE](TECH_ARCHITECTURE.md) · [COMPLIANCE](COMPLIANCE.md) · [ROADMAP](ROADMAP.md)

## 1. Vizyon

Mobil uchun realistik 3D billiard: haqiqiy zallar atmosferasi, aniq fizika, adolatli raqobat va PUBG Mobile / eFootball darajasidagi meta (kosmetika, Season Pass, ranked, klublar, ovozli chat).

**Elevator pitch:** "Telefoningizdagi eng chiroyli billiard zali — do'stingiz bilan ovozli chatda, adolatli ranked'da, o'z uslubingizdagi kiy bilan."

### 1.1 Dizayn ustunlari (pillars)

| # | Ustun | Ma'nosi | Nimani taqiqlaydi |
|---|-------|---------|-------------------|
| 1 | **Mahorat hal qiladi** | Natija faqat zarba sifatiga bog'liq. Server bir xil fizikani hammaga qo'llaydi. | Kiy statlari, pulli aim-line uzaytirish, pulli "ko'proq vaqt" |
| 2 | **Premium his** | Har bir zal, kiy, shar — "tegib ko'rgim keladi" darajasida. Ovoz, yorug'lik, animatsiya. | Arzon placeholder UI'ni release'ga chiqarish |
| 3 | **Tez kirish, uzoq qolish** | Lobbydan o'yinga ≤ 2 tap, match 3–6 daqiqa. Meta uzoq maqsad beradi. | Majburiy uzun tutorial, kirishda 3+ popup |
| 4 | **Ijtimoiy** | Do'st bilan o'ynash, klub, ovoz — asosiy sabab qaytib kelish uchun. | Ijtimoiy funksiyani paywall ortiga yashirish |
| 5 | **Shaffof iqtisod** | Ehtimollar, pity, narxlar ochiq. | Yashirin ehtimollar, aldovchi taymerlar |

### 1.2 Maqsadli auditoriya va platforma
- Asosiy bozor: O'zbekiston va MDH (o'zbek, rus, ingliz tillari), keyin global.
- Yosh: 16+ (paid random items sababli Yevropada PEGI 16 — [COMPLIANCE](COMPLIANCE.md#3-yosh-reytingi-iarc-pegi-esrb)).
- Qurilma: Android API 26+ (Play'da targetSdk 36), o'rta segment (Snapdragon 6/7-gen, Helio G99 sinfi) da 60 FPS, past segmentda 30 FPS. iOS keyinroq.
- Orientatsiya: landscape (match va lobby).

## 2. O'yin rejimlari

| Rejim | Tavsif | Valyuta oqimi | Holat |
|-------|--------|---------------|-------|
| **Tezkor o'yin (Quick Match)** | Venue tanlanadi → MMR'ga yaqin raqib. 8-ball (keyin 9-ball). | Coin kirish to'lovi → g'olib sovrini (venue jadvali) | M3 |
| **Do'st bilan (xona kodi)** | 6 belgili kod yoki do'stlar ro'yxatidan taklif. Stavka ixtiyoriy (0 yoki venue kirishi). | Ixtiyoriy | Mavjud (Stage 2), M8'da do'stlar bilan |
| **Ranked** | Mavsumiy. Kirish to'lovi yo'q. Ball (RP) yutib/yo'qotib daraja oshadi. | XP + mavsum oxiri mukofoti | M4 |
| **Turnirlar** | 8 yoki 16 o'yinchi, olib tashlanish (knockout). Kirish coin, mukofot fondi. | Kirish × N × 0.9 → 1/2/3–4 o'rinlar | M9 |
| **Offline vs AI** | 4 qiyinlik: Yangi boshlovchi, O'rta, Tajribali, Usta. Internetsiz. | Coin yo'q, faqat kichik XP (kunlik limit) | M5 |
| **Mashq** | Erkin shar joylashtirish, aim yordamchisi, vazifalar (trick shots). | Yo'q | M5 |
| **Hot-seat** | Bitta telefonda 2 kishi. | Yo'q | Mavjud |

O'yin turlari: **8-ball** (mavjud, WPA asosida), **9-ball** (M5), **snooker** (M12, 12 ft stol — fizika konfiguratsiyasi alohida).

### 2.1 AI qiyinlik darajalari
AI o'sha server/klient fizikasidan foydalanadi: nomzod zarbalarni `AimPredictor` bilan baholaydi, keyin shovqin qo'shadi.

| Daraja | Burchak xatosi (σ) | Kuch xatosi | Spin ishlatadi | Xavfsiz zarba (safety) | Fikrlash vaqti |
|--------|-------------------|-------------|----------------|------------------------|---------------|
| Yangi | 2.5° | ±20% | yo'q | yo'q | 1.5 s |
| O'rta | 1.2° | ±10% | faqat stop/follow | yo'q | 2 s |
| Tajribali | 0.5° | ±5% | ha | ba'zan | 2.5 s |
| Usta | 0.15° | ±2% | ha, keyingi shar pozitsiyasi uchun | ha | 3 s |

## 3. Venue (zal) tizimi

Har bir venue — o'z 3D xonasi, stol skini, musiqasi, kirish to'lovi va sovrini. Yuqori zallar **level** va **ranked darajasi** bilan ochiladi. Platforma komissiyasi (rake) 5% — asosiy coin sink.

| # | Venue | Kirish (coin) | G'olib oladi | Ochilish sharti | Kayfiyat |
|---|-------|---------------|--------------|-----------------|----------|
| 1 | Garaj | 50 | 95 | Level 1 | Oddiy garaj, lampochka |
| 2 | Eski Shahar Klubi | 200 | 380 | Level 3 | Yog'och, eski plakatlar |
| 3 | **Silk Road Lounge** (M1 premium zal) | 1 000 | 1 900 | Level 6 + Silver | Koshinkor naqshlar, iliq yorug'lik |
| 4 | Bukhara Palace | 5 000 | 9 500 | Level 12 + Gold | Saroy, gumbaz, oltin |
| 5 | Neon Arena | 25 000 | 47 500 | Level 20 + Platinum | Kiberpank neon |
| 6 | Skyline Tower | 100 000 | 190 000 | Level 30 + Diamond | Osmono'par, tungi shahar |
| 7 | Royal Hall | 500 000 | 950 000 | Level 40 + Crown | Klassik Yevropa saroyi |
| 8 | Legend Sanctum | 2 500 000 | 4 750 000 | Legend | Afsonaviy, animatsiyali |

Qoidalar:
- Kirish to'lovi match boshida serverda "escrow" ga olinadi; uzilish/taslim — raqib g'olib.
- Balans yetmasa venue kulrang, "Coin olish" tugmasi (vazifalar, do'kon).
- Venue zal temasi (xona skini) kosmetika sifatida alohida sotilishi mumkin: "Do'st bilan" rejimida xonani host tanlaydi.
- Haqiqiy brend/logolar yo'q; shahar va binolar to'qima nomlar.

## 4. Core loop (bitta match)

```
Lobby → Rejim/venue tanlash → Matchmaking (≤ 20 s, keyin AI-bot emas, kengroq MMR)
  → Break → [Aim → Spin → Power → Shot → Server simulyatsiya → Frames/Events]×N
  → Natija ekrani (coin, XP, RP, vazifa progressi) → Rematch / Lobby
```

- Zarba vaqti: 30 s (ranked 25 s), tugashiga 5 s qolganda ogohlantirish.
- Aim yordami: birinchi to'qnashuvgacha chiziq + obyekt shar yo'nalishi (qisqa). Ranked'da hamma uchun bir xil uzunlik. Uzunlik hech qachon pulga sotilmaydi.
- Match davomiyligi maqsadi: 3–6 daqiqa.

## 5. Meta loop

```
O'ynash → Coin/XP/RP/vazifa → Level up, Pass level, daraja
   → Yashik/do'kon/pass mukofotlari → Yangi kosmetika → Lobbyda ko'rsatish, do'stlarga
   → Yuqori venue ochiladi → Kattaroq stavka → ...
```

Kunlik sessiya maqsadi: 2–3 sessiya × 10–15 daqiqa. Qaytish sabablari: kunlik login, kunlik vazifalar, aylanadigan do'kon, Pass, klub, do'stlar.

## 6. Progressiya

### 6.1 Level / XP
- XP manbalari: g'alaba 100, mag'lubiyat 40, har bir tushirilgan shar 5, vazifalar, Pass.
- Level uchun kerakli XP: `XP(n) = 200 + 150·(n−1)^1.15` (yaxlitlangan). Level 10 ≈ 1 600 XP, level 30 ≈ 5 300 XP.
- Har bir level: coin mukofoti (`100·n`, max 5 000), har 5 levelda Classic yashik.

### 6.2 Ranked darajalari

| Daraja | Bo'linmalar | RP oralig'i | Izoh |
|--------|-------------|-------------|------|
| Bronze | III, II, I | 0–299 | Yo'qotishda RP tushmaydi (Bronze III) |
| Silver | III, II, I | 300–699 | |
| Gold | III, II, I | 700–1 199 | |
| Platinum | IV–I | 1 200–1 799 | |
| Diamond | IV–I | 1 800–2 499 | Daraja himoyasi: 3 match |
| Crown | V–I | 2 500–3 299 | |
| Legend | — (top 500) | 3 300+ | Leaderboard o'rni ko'rsatiladi |

- RP o'zgarishi: g'alaba +20..+30, mag'lubiyat −15..−25 (yashirin MMR farqiga bog'liq). Matchmaking yashirin **MMR** (Glicko-2) bo'yicha, RP faqat ko'rsatish uchun.
- Mavsum: 8 hafta (Season Pass bilan bir xil). Mavsum oxiri: daraja bo'yicha mukofot (ramka, coin, gem kichik miqdorda), soft reset (masalan Diamond → Gold I).

### 6.3 Season Pass
50 level, 8 hafta, bepul + premium yo'lak. Tafsilot: [ECONOMY §6](ECONOMY.md#6-season-pass).

### 6.4 Vazifalar (missions)
- **Kunlik** (3 ta, har kuni 00:00 Toshkent vaqti): "2 match o'yna", "5 shar tushir", "1 g'alaba". Har biri coin + Pass XP.
- **Haftalik** (5 ta): "Silk Road Lounge'da 3 g'alaba", "Bank shot bilan 3 shar". Ko'proq Pass XP.
- **Mavsumiy (Pass)**: Pass'ga bog'langan uzun vazifalar.
- Vazifani almashtirish: kuniga 1 marta bepul.

### 6.5 Login mukofotlari
7 kunlik tsikl (o'tkazib yuborilsa progress saqlanadi, faqat ketma-ketlik bonusi yo'qoladi): 1-kun 200 coin, 2-kun 300, 3-kun Classic yashik, 4-kun 500, 5-kun 10 gem, 6-kun 800, 7-kun Premium yashik kaliti 1 ta. Yangi o'yinchi uchun birinchi 7 kun — kattaroq "Welcome" tsikli (Epic kiy 7-kunda).

### 6.6 Achievementlar
Bir martalik maqsadlar (masalan "100 g'alaba", "Break'da 8-ball", "Har bir venue'da g'alaba", "Usta AI'ni yutish"). Mukofot: coin, ramka, nickname rangi. Profilda nishonlar (badges).

## 7. Kosmetika

Turlari: kiylar, stol matosi, stol bortlari (rails), shar to'plamlari, zal temalari, avatar kiyimlari, avatar ramkalari, emotsiyalar, zarba effekti (trail), luzaga tushish effekti, g'alaba animatsiyasi, nickname ranglari.

Raritet: Common · Rare · Epic · Legendary · Mythic. Mythic — animatsiyali va 5 bosqichda upgrade qilinadi (har bosqich yangi vizual qatlam: 1 asos → 2 rang → 3 trail → 4 luza effekti → 5 g'alaba animatsiyasi va maxsus nom).

**Barcha kosmetika faqat vizual.** Asoslash: [ECONOMY §2](ECONOMY.md#2-no-pay-to-win-tamoyili).

## 8. Avatarlar (3D personaj)
- Lobby markazida aylantiriladigan (barmoq bilan surish) 3D personaj; uning yonida tanlangan kiy.
- Slotlar: bosh kiyim, ustki kiyim, shim, poyabzal, aksessuar, to'liq kostyum (set).
- Emotsiyalar: lobbyda va match oxirida (masalan "kiyni aylantirish", "chalk surtish").
- Match'da raqib: 2D portret (avatar renderidan avtomatik), ramka, daraja nishoni, bayroq (ixtiyoriy).
- Haqiqiy shaxslar portretlari ishlatilmaydi.

## 9. Ijtimoiy

| Funksiya | Tavsif | Milestone |
|----------|--------|-----------|
| Do'stlar | ID/nik bo'yicha qidirish, so'rov, onlayn holati, taklif | M8 |
| Klublar | 30 a'zo, rollar (lider, o'rinbosar, a'zo), klub chati, haftalik klub ochkosi, klub leaderboard | M8 |
| Chat | Klub va shaxsiy chat, so'kish filtri, report/block | M8 |
| Quick chat / emoji | Match ichida 8 ta tayyor ibora + emoji (lokalizatsiyalangan) | M8 |
| Ovozli chat | Match ichida (Vivox), mikrofon default o'chiq, raqibni mute | M8 |
| Spectate | Do'st yoki klub a'zosi matchini jonli kuzatish (server frames, 2 s kechikish) | M8 |
| Replay | Oxirgi 20 match: ShotParams + boshlang'ich holat saqlanadi, klientda qayta simulyatsiya | M8 |
| Leaderboard | Global / mamlakat / do'stlar; ranked RP, haftalik g'alabalar | M4 |

## 10. Ekranlar ro'yxati
Splash → Login (device/Google) → Tutorial (birinchi match AI bilan) → **Lobby** → Rejim tanlash / Venue xaritasi → Matchmaking → **Match HUD** → **Natija** → Profil → **Inventar** → **Do'kon** → **Yashik ochish** → **Season Pass** → Vazifalar → Login mukofotlari → Leaderboard → Do'stlar → Klub → Turnirlar → Sozlamalar (til, ovoz, grafika sifati, akkauntni o'chirish) → Credits.
Har bir ekran tuzilmasi: [UI_UX](UI_UX.md).

## 11. Onboarding (FTUE)
1. Ilova ochiladi → device auth avtomatik (ro'yxatdan o'tish talab qilinmaydi).
2. 1-match: "Yangi" AI bilan, qo'llanma ko'rsatkichlari (aim → power → shot). Yutqazish deyarli imkonsiz emas, lekin oson.
3. Natija → birinchi Classic yashik (bepul) ochiladi → kiy jihozlanadi.
4. Lobby: faqat Play, Inventar, Vazifalar yoritilgan; qolganlari level 2–5 da bosqichma-bosqich ochiladi.
5. Level 3: Garaj'dan keyin Eski Shahar Klubi ochiladi; level 5: ranked.

## 12. Fair play va anti-cheat (dizayn darajasida)
- Server-authoritative: klient faqat `ShotParams` yuboradi; server cheklovlarni tekshiradi (kuch, spin radiusi, vaqt, navbat).
- Uzilish: 60 s qayta ulanish oynasi; keyin texnik mag'lubiyat.
- Ranked'da "rage quit" jarimasi: 3 marta tark etish → 10 daqiqa matchmaking bloki.
- Report: harakat sababli (haqorat, cheat gumon), 3+ tasdiqlangan report → admin ko'rib chiqadi.
