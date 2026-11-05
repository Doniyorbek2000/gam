# UI_UX — Ekranlar tuzilmasi va patternlar

> Holat: v1.0 (2026-11-05). Orientatsiya: landscape. Asosiy rezolyutsiya maketi: 2400×1080 (20:9), safe area hisobga olinadi. Unity uGUI + TextMeshPro, Canvas Scaler "Scale With Screen Size" (ref 1920×1080, match 0.5).

## 1. Umumiy tamoyillar

| Tamoyil | Amalda |
|---------|--------|
| **2 tapda o'yin** | Lobby'dan katta "O'YNASH" tugmasi → match qidirish. Rejim/venue oxirgi tanlov saqlanadi. |
| **Markazda qahramon** | PUBG Mobile/eFootball kabi lobby markazida 3D personaj va kiy — kosmetika ko'rinadigan "vitrina". |
| **Chetlarda tez kirishlar** | Chap rels — progress (Pass, vazifalar, eventlar), o'ng rels — monetizatsiya (do'kon, yashik, inventar). |
| **Yuqorida resurslar** | Coin/Gem har doim yuqori o'ngda, "+" do'konning tegishli bo'limiga olib boradi. |
| **Qizil nuqta tizimi** | Olinmagan mukofot = qizil nuqta. Ierarxik: bo'lim ichida nuqta bo'lsa tugmada ham. Ortiqcha ishlatilmaydi (faqat haqiqiy harakat bo'lsa). |
| **Bir vaqtda bitta popup** | Kirishda maksimum 1 ta popup (login mukofoti), qolganlari "Inbox"/banner karuseliga. |
| **Shaffoflik** | Har bir random xaridda "i" (ehtimollar) va pity hisoblagichi ko'rinadi. |
| **Mahalliylashtirish** | Matn konteynerlari +35% kengayishga tayyor (rus tili uzunroq), auto-size min/max bilan. |
| **Qulaylik** | Tap maydoni ≥ 48 dp; raritet = rang + shakl; kontrast ≥ 4.5:1 matnda; "Effektlarni kamaytirish" sozlamasi. |

### 1.1 PUBG Mobile / eFootball'dan olingan patternlar
- **3D lobby vitrinasi:** personaj, aylantirish, yorug'lik va mavsum temasi. PUBG Mobile New Era yangilanishida lobby funksiyalari sahifalarga ajratilgan va interfeys tozalangan ([sportskeeda](https://www.sportskeeda.com/esports/pubg-mobile-new-era-1-0-character-improvements-upgraded-graphics-new-lobby-ui)).
- **Mavsum temasi:** lobby fon/musiqa har mavsum almashadi (Season Pass bilan birga).
- **Yashik ochish dramaturgiyasi:** kutish → raritet "oqishi" (rang) → reveal → 10× uchun xulosa grid. Skip har doim mavjud.
- **Upgrade ko'rinishi:** Mythic item kartasida 1–5 bosqich vizual "zinapoyasi" (PUBG upgradable skin uslubi).
- **eFootball "box" ko'rinishi:** event yashiklarida jami itemlar va qolganlarini ko'rsatish g'oyasi ([gamingonphone](https://gamingonphone.com/guides/efootball-2025-new-year-epic-pack-review)) — bizda "pity hisoblagichi" sifatida.
- **Pass yo'lagi:** gorizontal skroll, ikki qator (bepul/premium), joriy level markazda, katta mukofotlar oldindan ko'rsatiladi.
- **Match HUD minimal:** faqat kerakli boshqaruvlar, qolganlari bitta menyuga yig'ilgan. PUBG Mobile HUD layoutini moslashtirish imkoniyatidan ijobiy UX olingan ([uxdesign.cc](https://uxdesign.cc/what-i-learned-about-ux-research-on-pubg-mobile-as-a-player-57a673d19fe1)) → bizda power slider tomonini (chap/o'ng) tanlash.

### 1.2 Vizual tokenlar
Ranglar, Montserrat tip shkalasi, 8 pt grid, komponentlar, harakat va ikonkalar: **[DESIGN_SYSTEM.md](DESIGN_SYSTEM.md)** (manba — `UITheme.asset`). Raritet ranglari: [ECONOMY §3](ECONOMY.md#3-raritet).

### 1.3 Amalga oshirilgan holat (M1.5)
Lobbi, Rejim+Zal, Profil, Mening xonam, Inventar (9 tab), Yashiklar, Sozlamalar, Credits, match HUD, natija, splash va umumiy dialoglar quriladi (`Editor/UI/*`). Lobbi va match UI alohida canvas'larda: match HUD faqat match paytida yoqiladi. "TEZ KUNDA" tegli tugmalar (Pass, Vazifalar, Eventlar, Kunlik, Klub, Chat, Ranked, Bot, Mashq) hozircha bosilmaydi.

## 2. Lobby

```
┌──────────────────────────────────────────────────────────────────────────┐
│[Avatar(ramka)|Nik|Rank ▓▓░ Lv]         [🪙 5 000 +][💎 1 500 +][✉][👥][⚙]│
│ ┌────────┐                                                   ┌────────┐ │
│ │ PASS   │                                                   │ DO'KON │ │
│ ├────────┤            ╔══════════════════╗                   ├────────┤ │
│ │VAZIFALAR│           ║   3D PERSONAJ    ║                   │YASHIKLAR│ │
│ ├────────┤            ║ jihozlangan kiy  ║                   ├────────┤ │
│ │EVENTLAR│            ║ barmoq bilan ↔   ║                   │INVENTAR│ │
│ ├────────┤            ╚══════════════════╝                   ├────────┤ │
│ │ KUNLIK │                                                   │ XONAM  │ │
│ └────────┘                                  ┌──────────────────────────┐│
│                                             │ 8-BALL · Rejim | Zal  ›  ││
│ [KLUB][CHAT] (tez kunda)                    │      ▶  O'YNASH (puls)   ││
│                                             └──────────────────────────┘│
└──────────────────────────────────────────────────────────────────────────┘
```

- Markaz: 3D sahna (alohida kamera, RenderTexture emas — to'g'ridan-to'g'ri sahna, UI overlay). Personaj idle animatsiya, 10 s da bir emotsiya.
- "O'YNASH" bloki: yuqori qatorda rejim+zal kartasi (to'liq ekranli Rejim va Zal ekranini ochadi, §3), pastda pulsatsiyalanuvchi katta tugma.
- Pastki chap: ijtimoiy qator. Do'stlar tugmasida onlayn soni.
- Yuqori chap: profil kartasi → Profil ekrani.
- Fon: tanlangan zal temasi (xira, bokeh), mavsum effekti.
- Ishlash: lobby sahnasi ≤ 60k tri, 1 real-time yorug'lik + baked; 60 FPS.

## 3. Rejim / Venue tanlash (to'liq ekran)

```
┌──────────────────────────────────────────────────────────────┐
│ [Tezkor] [Ranked] [Do'st bilan] [Turnir] [AI] [Mashq]         │
│ ◀ ┌─────────┐ ┌─────────┐ ┌═════════┐ ┌─────────┐ ┌─────────┐ ▶│
│   │ Garaj   │ │Eski Shah│ ║SilkRoad ║ │Bukhara 🔒│ │Neon 🔒  │  │
│   │ 50→95   │ │200→380  │ ║1000→1900║ │Lv12+Gold│ │Lv20+Plat│  │
│   └─────────┘ └─────────┘ ╚═════════╝ └─────────┘ └─────────┘  │
│   [i] Qoidalar va sovrinlar           Onlayn: 1 204   [TANLASH]│
└──────────────────────────────────────────────────────────────┘
```
- Karta: zal rasmi (prerender), kirish → sovrin, onlayn o'yinchilar, qulf sharti.
- Balans yetmasa karta xira, tugma "Coin olish".

## 4. Matchmaking
- To'liq ekran: chapda o'yinchi kartasi, o'ngda "?" aylanuvchi slot → raqib topilganda flip animatsiya (avatar portreti, ramka, daraja, bayroq).
- Taymer (0:00 → yuqoriga), "Bekor qilish". 20 s dan keyin "Qidiruv kengaytirilmoqda".
- Raqib topilgach 3 s "VS" ekrani → zal yuklanadi (Addressables, oldindan cache).

## 5. Match HUD

```
┌──────────────────────────────────────────────────────────────────────────┐
│[◉Men 🔵🔵🔵⚪] Lv12 Gold II  ⏱(halqa)      ⏱  [Raqib Lv14 ⚪🔴🔴🔴◉] [≡]│
│ ┌──┐                                                                ┌───┐│
│ │P │                                                                │ ⊕ ││
│ │O │                 3D STOL (yuqori-perspektiv kamera)             │SPIN│
│ │W │                    aim chizig'i + ghost ball                   └───┘│
│ │E │                                                                     │
│ │R │                                                          [🎤][💬]   │
│ └──┘  [Nozik burchak ◀ ▶]                              [Kamera ⟲ 2D/3D]  │
└──────────────────────────────────────────────────────────────────────────┘
```
- Yuqori: ikki o'yinchi paneli, avatar atrofida navbat taymeri halqasi (yashil → sariq → qizil), guruh sharlari (qolganlari).
- Chap: vertikal power slider (pastga tortib qo'yib yuborish = zarba). Sozlamada o'ngga ko'chiriladi.
- O'ng yuqori: spin (cue ball) widget — tap → kattalashgan doira, nuqtani surish.
- Aim: stolda barmoq bilan surish (tez), pastki "nozik burchak" g'ildiragi (0.05°/qadam).
- Pastki o'ng: mikrofon (default o'chiq), quick chat/emoji, kamera rejimi.
- `≡` menyu: ovoz, qoidalar, taslim bo'lish (tasdiqlash bilan).
- Foul / ball-in-hand: yuqori markazda banner 2 s ("Foul: oq shar tushdi — raqibda ball in hand").
- Quick chat pufakchalari avatar yonida 2.5 s.

## 6. Natija ekrani
```
┌───────────────────────────────────────────────────────┐
│                  G'ALABA!  (g'alaba animatsiyasi)        │
│   [Men 3D poza]                      [Raqib portret]    │
│   🪙 +1 900   XP +100 ▓▓▓▓▓▓░ Lv12→13   RP +24 Gold II ▲│
│   Vazifalar: "1 g'alaba" ✓  Pass XP +80 ▓▓▓░            │
│        [REVANSH]   [Lobby]   [Replay]   [Do'st qo'shish] │
└───────────────────────────────────────────────────────┘
```
- Raqamlar ketma-ket "sanaladi" (count-up 0.6 s), level up bo'lsa alohida 1.5 s overlay.
- Revansh: raqib ham bosganda darhol yangi match (bir xil stavka).
- Mag'lubiyatda: "Yaxshi zarbalar: 4" va maslahat (masalan "Spin bilan pozitsiya").

## 7. Do'kon
```
┌───────────────────────────────────────────────────────────────┐
│[Tavsiya][Kunlik][Yashiklar][Gem][Coin][Bundle]   ⏱ yangilanish 05:12:40│
│ ┌──────────────┐ ┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐          │
│ │ FEATURED     │ │Rare │ │Epic │ │Leg. │ │Rare │ │Comm.│          │
│ │ (katta karta)│ │150💎│ │450💎│ │1400💎│ │6000🪙│ │1500🪙│          │
│ └──────────────┘ └─────┘ └─────┘ └─────┘ └─────┘ └─────┘          │
└───────────────────────────────────────────────────────────────┘
```
- Karta: item 3D preview (tap → to'liq ekran inspektor: aylantirish, stolda ko'rish), raritet ramkasi, narx tugmasi.
- Gem bo'limi: paketlar (§ [ECONOMY 4.1](ECONOMY.md#41-gem-paketlari)), "Birinchi xarid ×2" yorlig'i.
- Xarid tasdiqlash oynasi: narx, nima olinadi, (random bo'lsa) ehtimollar havolasi.
- **Play build'da:** faqat Google Play Billing. Veb-do'kon, Payme/Click haqida hech narsa yo'q. **APK (direct) build'da:** "Payme / Click / Uzum orqali" tugmalari ([TECH_ARCHITECTURE §6](TECH_ARCHITECTURE.md#6-tolovlar)).

## 8. Yashik ochish animatsiyasi

Ketma-ketlik (1× ochish, ~3 s, istalgan joyda tap = tezlashtirish, "Skip" = darhol natija):
1. **Tanlash** (yashik kartasi): narx, `[i] Ehtimollar`, `Legendary'gacha: 23`, `[1×] [10×]`.
2. **Kutish** (0.8 s): yashik stol ustiga tushadi, kamera yaqinlashadi, past chastotali "hum".
3. **Oqish** (0.6 s): yashik tirqishidan raritet rangidagi nur. Epic+ da qo'shimcha zarba (camera shake 0.1, chang zarrachalar). Mythic: ekran qorayadi, toj belgisi.
4. **Ochilish** (0.8 s): qopqoq ochiladi, item yuqoriga ko'tariladi va aylanadi.
5. **Reveal kartasi**: item nomi, raritet, "YANGI" yoki "Dublikat → +60 Shard". Tugmalar: `[Jihozlash] [Yana ochish] [Yopish]`.
6. **10× rejim**: yashiklar tez ketma-ket, keyin 2×5 grid xulosasi, eng yuqori raritet oxirida alohida reveal.

Texnik: Timeline + VFX Graph (mobil uchun Particle System yetarli), natija serverdan kelmaguncha 2-bosqichdan o'tmaydi (offline mock M1 da lokal RNG).

## 9. Inventar
```
┌──────────────────────────────────────────────────────────────┐
│[Kiylar][Stol][Sharlar][Zal][Avatar][Ramka][Emotsiya][Effekt]   │
│ Filtr: [Hammasi▾] Saralash: [Raritet▾]          Shard: 1 240   │
│ ┌──┐┌──┐┌──┐┌──┐┌──┐┌──┐   │  ┌───────────────────────────┐   │
│ │✓ ││  ││  ││🔒││  ││  │   │  │  3D PREVIEW (aylanadi)     │   │
│ └──┘└──┘└──┘└──┘└──┘└──┘   │  │  Nomi · Legendary          │   │
│ ┌──┐┌──┐┌──┐┌──┐┌──┐┌──┐   │  │  [Jihozlash] [Upgrade] [Craft]│ │
│ └──┘└──┘└──┘└──┘└──┘└──┘   │  └───────────────────────────┘   │
└──────────────────────────────────────────────────────────────┘
```
- Qulflangan (yo'q) itemlar ham ko'rinadi → "Qayerdan olish" (Pass/yashik/do'kon/craft).
- Mythic upgrade paneli: 5 bosqich zinapoyasi, har bosqich preview, Kristall narxi.

## 10. Season Pass
```
┌─────────────────────────────────────────────────────────────────┐
│ MAVSUM 1: "Ipak yo'li"   ⏱ 41 kun     Lv 23 ▓▓▓▓▓▓░░ 640/1000 XP │
│        ◀  21    22   [23]   24    25 ★   26   ...  ▶              │
│ BEPUL   🪙    📦    💎20    🪙    💎30                              │
│ PREMIUM 💎    🎯    👕     🔑    🎱★                               │
│ [Vazifalar] [Level sotib olish]        [PREMIUM OLISH 800💎]      │
└─────────────────────────────────────────────────────────────────┘
```
- Chap tomonda katta preview: final mukofot (mavsum kiyi evolyutsiyasi).
- "Hammasini olish" tugmasi.
- Premium olinmagan bo'lsa premium qatordagi mukofotlar qulf ostida, lekin ko'rinadi.

## 11. Boshqa ekranlar (qisqa)
- **Profil (M1.5):** chapda sarlavha kartasi — avatar + ramka, tahrirlanadigan nik (3–16 belgi), ID + nusxalash, daraja/XP, rank, bayroq, klub. O'ngda tablar: Statistika (o'yinlar, g'alaba %, eng uzun seriya, aniqlik, bir zarbada max shar, sevimli kiy), Tarix (oxirgi 20 match), Yutuqlar (3 tasi vitrinada + to'liq ro'yxat), Kolleksiya (har tur bo'yicha progress).
- **Mening xonam (M1.5):** alohida 3D xona — stol, 6 kiyli tokcha, 8 kubokli javon, 3 poster, devor/pol temalari. "Tahrirlash" rejimida faqat egalik qilingan itemlar; saqlash/bekor qilish. Ma'lumot `RoomLayout` da, keyin do'stlar tashrifi uchun serverga ko'chadi.
- **Stikerlar (M1.5):** inventarda alohida tab; kiy dastasi va stol bortiga decal, match ichida stiker paneli ([ECONOMY §4.4](ECONOMY.md#44-stikerlar-m15)).
- **Vazifalar:** Kunlik/Haftalik/Mavsumiy tablar, progress bar, "Almashtirish" (1/kun).
- **Login mukofotlari:** 7 katak, bugungi katak pulsatsiya.
- **Leaderboard:** Global/Mamlakat/Do'stlar × Ranked/Haftalik; o'z o'rning pastda sticky.
- **Do'stlar / Klub:** ro'yxat, onlayn holat, taklif, chat (TMP + emoji sprite asset).
- **Turnirlar:** bracket ko'rinishi (8/16), kirish, sovrin fondi.
- **Sozlamalar (M1.5):** chapda 6 bo'lim — Grafika (sifat Past/O'rta/Yuqori/Avto, FPS 30/60/90/120 — displey qo'llagani, soyalar, post-processing, quvvat tejash), Ovoz (umumiy, musiqa, effektlar, ovozli chat, vibratsiya), Boshqaruv (nishon va g'ildirak sezgirligi, chapaqay rejim — power slider o'ngga, nishon chizig'i, zarba tasdig'i), Til (O'zbek/Русский/English, darhol almashadi), Akkaunt (ID nusxa, Google ulash — tez kunda, chiqish, **akkauntni o'chirish** tasdiq bilan), Boshqa (bildirishnomalar, yordam, maxfiylik, shartlar, Credits, versiya).
- **Credits:** `ArtSource/LICENSES.md` dan avtomatik generatsiya ([ART_PIPELINE §4](ART_PIPELINE.md#4-litsenziya-reyestri-va-credits)).

## 12. Holatlar (har ekran uchun majburiy)
Loading (skeleton), bo'sh holat (masalan "Do'stlar yo'q → Qo'shish"), xato (offline banner + "Qayta urinish"), server texnik ishlar ekrani, majburiy yangilanish (min versiya Remote Config'dan).
