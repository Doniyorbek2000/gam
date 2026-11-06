# COMPLIANCE — Siyosatlar, qonunlar, litsenziyalar

> Holat: v1.0 (2026-11-05). Bu hujjat yuridik maslahat emas; launch'dan oldin yurist (O'zbekiston + maqsadli bozorlar) bilan tekshirish kerak. Har bir bo'limda manba va oxirgi tekshiruv sanasi. Siyosatlar tez o'zgaradi — har milestone'da "Tekshirish" ustunidagi havolalarni qayta ko'rib chiqing.

## 1. To'lov siyosati (Google Play / App Store)

### 1.1 Google Play Payments
- Ilova ichida raqamli tovar/xizmat (shu jumladan virtual valyuta — bizning Gem, Coin paketlari, Pass) sotilsa, **Google Play billing** ishlatilishi shart ([Payments policy](https://support.google.com/googleplay/android-developer/answer/9858738), [Play billing FAQ](https://support.google.com/googleplay/android-developer/answer/10281818)).
- Istisno: Play orqali tarqatilgan ilovada iste'mol qilinmaydigan va unga kira olmaydigan raqamli tovarlar ([9858738](https://support.google.com/googleplay/android-developer/answer/9858738)). Bizning Gem o'yinda ishlatiladi → istisnoga kirmaydi.
- AQSh, UK va EEA'da 2025–2026 yillarda muqobil billing va tashqi havola (External Content Links) dasturlari kiritildi: ro'yxatdan o'tish, tranzaksiyalarni hisobot qilish va xizmat haqi to'lash talab qilinadi; AQSh'da hisobot majburiyati 2026-yil oktyabrdan ([Play Console Help 15582165](https://support.google.com/googleplay/android-developer/answer/15582165), [9to5google](https://9to5google.com/2026/06/24/google-play-store-external-billing-june-30/), [appsops](https://appsops.store/news/google-play-external-billing-october-reporting-2026)). O'zbekiston bu dasturlarga kirmaydi.

**Bizning qarorlar:**
1. Play build (`STORE_PLAY`) — faqat Google Play Billing. Veb-do'kon, Payme/Click/Uzum, "arzonroq saytda" kabi matn, havola, QR yoki tugma **yo'q**; kod `#if STORE_DIRECT` ichida — Play build'ga umuman kompilyatsiya qilinmaydi.
2. Veb-do'kon mustaqil sayt: o'yinchi ID orqali top-up. Uni Play build ichidan reklama qilmaymiz. Saytdan sotib olingan Gem Play build'da ham ko'rinadi — bu cross-platform hisob; bu holat siyosatning "ilova ichida yo'naltirish" qismiga tegishli emas, lekin har yili qayta tekshiriladi (anti-steering qoidalari bozorga qarab o'zgaradi).
3. Direct APK build (`STORE_DIRECT`) Google Play'da tarqatilmaydi → Play Payments siyosati unga tegishli emas; ilova ichida Payme/Click/Uzum mumkin. Paket nomi boshqacha (`...direct`) yoki bir xil imzo bilan emas — Play va APK bir-birini "yangilamaydi".
4. Play Store listing va skrinshotlarda ham veb-do'kon haqida ma'lumot yo'q.

### 1.2 Apple App Store (keyin)
- 3.1.1: raqamli kontent IAP orqali; loot box ehtimollari xariddan oldin oshkor qilinadi (2017-dekabrdan) ([TouchArcade](https://toucharcade.com/2017/12/21/apple-quietly-updated-the-app-store-review-guidelines-to-require-disclosure-of-loot-box-iap-odds/), [Fenwick](https://www.fenwick.com/insights/publications/apple-now-requires-disclosure-of-loot-box-odds)). Joriy matnni [developer.apple.com/app-store/review/guidelines](https://developer.apple.com/app-store/review/guidelines/) da tekshirish.

## 2. Loot box (yashik) qoidalari

### 2.1 Platformalar
- **Google Play:** random virtual itemlar sotiladigan mexanizmlar uchun ehtimollar xariddan oldin aniq oshkor qilinishi shart (2019-yildan, Monetization and Ads siyosati) ([Fenwick](https://fenwick.com/insights/publications/google-play-now-requires-disclosure-of-loot-box-odds), [pocketgamer.biz](https://www.pocketgamer.biz/google-play-developers-must-now-disclose-loot-box-odds)).
- **Apple:** yuqorida §1.2.

### 2.2 Mamlakatlar

| Yurisdiksiya | Holat | Manba | Bizning konfiguratsiya |
|--------------|-------|-------|------------------------|
| **Belgiya** | Pulli loot box'lar qimor sifatida taqiqlangan (2018 regulyator pozitsiyasi; 2025-yanvar Antverpen sudi qarori, CJEU'ga savol C-47/25). Ijro zaif, lekin yirik nashriyotlar pulli yashiklarni BE uchun o'chirgan. | [siege.gg](https://siege.gg/news/several-eu-countries-have-introduced-stricter-regulations-on-loot-boxes-in-games-in-2025), [Leon Xiao](https://sites.google.com/view/leon-xiao/policy/belgium), [ITU](https://pure.itu.dk/en/publications/breaking-ban-belgiums-ineffective-gambling-law-regulation-of-vide/), [PC Gamer](https://www.pcgamer.com/au/blizzard-removes-paid-loot-boxes-from-overwatch-and-heroes-of-the-storm-in-belgium) | `paid_random_disabled` |
| **Niderlandiya** | Manbalar ziddiyatli (2022 Davlat kengashi qarori bo'yicha ijro bekor qilingan; qonun loyihalari muhokamada). | [promise.legal](https://blog.promise.legal/loot-box-laws-game-developers/) | `review_required` (default yoqilgan) |
| **Janubiy Koreya** | 2024-03-22 dan ehtimollar xarid ekranida majburiy; xorijiy kompaniyalarga ham; buzilishda tuzatish buyrug'i, jinoiy jarima; 2025-08 dan 3× zarar to'lovi. | [gameworldobserver](https://gameworldobserver.com/2023/02/28/south-korea-loot-boxes-probability-disclosure-law), [266 o'yin buzilishi](https://gameworldobserver.com/2024/07/08/266-games-violated-loot-box-rules-south-korea), [PMC tadqiqot](https://www.ncbi.nlm.nih.gov/pmc/articles/PMC12583229/) | `odds_on_purchase_screen` (hamma joyda) |
| **Xitoy** | 2017-yildan ehtimollarni oshkor qilish qonuniy majburiyat; foizlar haqiqatga mos bo'lishi kerak. Biz Xitoyga chiqmaymiz (alohida litsenziya kerak). | [ITU / Xiao](https://pure.itu.dk/en/publications/drafting-video-game-loot-box-regulation-for-dummies-a-chinese-les/), [Wikipedia](https://wikipedia.com/wiki/Regulations_protecting_consumers_from_microtransactions) | Bozor emas |
| **Yaponiya** | "Kompu gacha" (random itemlar to'plamini to'ldirish uchun mukofot) taqiqlangan; ehtimol oshkor qilish o'z-o'zini tartibga solish. | [DiGRA 2026](https://dl.digra.org/index.php/dl/article/view/2749) | `no_complete_set_gacha` (dizayn darajasida — bunday mexanika yo'q) |
| **Buyuk Britaniya** | Itemlarni real pulga sotib bo'lmasa qimor hisoblanmaydi. | [promise.legal](https://blog.promise.legal/loot-box-laws-game-developers/) | Savdo/almashuv yo'q |
| **AQSh** | Federal regulyatsiya yo'q. | xuddi shu | ESRB label |
| **O'zbekiston** | Loot box'ga oid maxsus qonun topilmadi (2026-11). Qimor va reklama qonunchiligi bo'yicha yurist bilan tekshirish kerak. | — | Default |

### 2.3 Dizayndagi kafolatlar
- Ehtimollar (asosiy + pity bilan konsolidatsiyalangan) xarid **ekranining o'zida** va "i" tugmasida ([ECONOMY §5](ECONOMY.md#5-yashiklar-crates)).
- Ehtimollar server jadvalidan generatsiya qilinadi — UI'da qo'lda yozilgan raqam yo'q (nomuvofiqlik xavfi). Admin'da publish oldidan yig'indi = 100% tekshiruvi va 10⁶ simulyatsiya.
- Pity va dublikat qoidalari ochiq yozilgan.
- Itemlarni almashish, sotish yoki chiqarib olish (cash-out) yo'q → itemlar real pul qiymatiga ega emas.
- Coin matchlar (venue stavkalari) — faqat virtual, naqd pulga almashtirilmaydi; turnirlarda real pul/mukofot yo'q (Google Play "Real-money gambling, games and contests" siyosati doirasidan tashqarida qolish uchun).

## 3. Yosh reytingi (IARC, PEGI, ESRB)
- Google Play IARC so'rovnomasi to'ldiriladi: "In-game purchases" = Ha, "random items" = Ha. ESRB label: **"In-Game Purchases (Includes Random Items)"** — loot box, gacha, prize wheel, sandiq va boshqa random pulli elementlar uchun (2020-04 dan) ([ESRB](https://www.esrb.org/news/in-game-purchases-includes-random-items/), [ESRB guide](https://www.esrb.org/ratings-guide/in-game-purchases)).
- **PEGI (Yevropa):** 2026-yil iyundan pulli random itemli o'yinlar default **kamida PEGI 16**; countdown/cheklangan takliflar va battle pass PEGI 12 trigger ([VGC](https://www.videogameschronicle.com/news/all-games-with-loot-boxes-in-them-will-be-rated-minimum-pegi-16-starting-this-summer), [pocketgamer.biz](https://www.pocketgamer.biz/pegi-expands-age-ratings-to-cover-loot-boxes-and-in-game-spending), [Bird & Bird](https://mediawrites.twobirds.com/post/102mn2b/pegi-age-rating-classification-update)). Mavjud o'yinlarni qayta reytinglash bo'yicha manbalar ziddiyatli.
- **Qaror:** o'yin 16+ auditoriyaga mo'ljallanadi. Agar Yevropada pastroq reyting kerak bo'lsa — `paid_random_disabled` ni EEA uchun yoqish (yashiklar faqat bepul/to'g'ridan-to'g'ri sotuv) varianti mavjud; bu biznes qarori (ROADMAP M15 da).
- Families (bolalar) dasturiga kirmaymiz; birinchi ishga tushirishda yosh so'rovi (neutral age gate) — 16 yoshdan kichiklarga ovozli chat va xaridlar o'chiq (konservativ default).

## 4. Google Play texnik talablari
- **Target API:** 2026-08-31 dan yangi ilovalar va yangilanishlar Android 16 (API 36) ni target qilishi shart ([developer.android.com](https://developer.android.com/google/play/requirements/target-sdk)). Mavjud `minSdk 26` qoladi.
- **Akkauntni o'chirish:** akkaunt yaratadigan ilovalar ilova ichida va vebda o'chirish so'rovi imkonini berishi, Data safety formasida veb havola ko'rsatilishi shart; deaktivatsiya o'chirish hisoblanmaydi ([TechCrunch](https://techcrunch.com/2023/04/05/google-follows-apple-by-introducing-account-deletion-policy-for-android-apps), [Android Authority](https://www.androidauthority.com/google-play-data-safety-3309792/)). Bizda: Sozlamalar → "Akkauntni o'chirish" + veb sahifa (veb-do'kon domenida). Saqlanadigan ma'lumotlar (to'lov yozuvlari — buxgalteriya/fraud uchun) Data safety'da ochiq yoziladi.
- **Data safety** formasi: to'planadigan ma'lumotlar — device ID, Google akkaunt ID/email (ixtiyoriy), xarid tarixi, o'yin statistikasi, crash loglari, ovoz (Vivox orqali real-time uzatiladi, saqlanmaydi — Vivox shartlariga qarab tekshirish), chat xabarlari.
- **User Generated Content:** chat/ovoz → report, block, moderatsiya majburiy.

## 5. Shaxsiy ma'lumotlar
- **O'zbekiston "Shaxsga doir ma'lumotlar to'g'risida"gi qonun, 27¹-modda (2021):** O'zbekiston fuqarolarining shaxsiy ma'lumotlari O'zbekiston hududidagi texnik vositalarda saqlanishi va davlat reyestrida ro'yxatdan o'tishi kerak edi ([Mondaq](https://mondaq.com/data-protection/1197754/the-law-on-personal-data-has-been-amended), [kun.uz](https://kun.uz/en/news/2024/12/26/uzbekistans-data-localization-law-hinders-entry-of-apple-pay-and-google-pay)).
- **2026-mart o'zgartirishi:** ko'p toifalar uchun chet elda saqlashga ruxsat (davlat ro'yxatidagi "adekvat" mamlakatlar va boshqa mezonlar), lekin biometrik/genetik ma'lumotlar va telekom operatorlari mijozlari ma'lumotlari faqat O'zbekistonda ([kun.uz 2026-03-27](https://kun.uz/en/news/2026/03/27/uzbekistan-amends-personal-data-law-to-facilitate-global-payment-systems)). Qonun matni rasmiy manbada (lex.uz) tekshirilishi kerak.
- **Qaror:** asosiy bozor O'zbekiston bo'lgani uchun prod Postgres (Nakama + admin DB) **O'zbekistondagi data-markazda** (konservativ variant) yoki adekvat ro'yxatdagi mamlakatda — yurist xulosasidan keyin. Ovoz yozuvi saqlanmaydi; biometrik ma'lumot to'planmaydi. Shaxsiy ma'lumotlar bazasini reyestrda ro'yxatdan o'tkazish zarurati yurist bilan aniqlanadi.
- **GDPR (EEA foydalanuvchilari, global launch'da):** maxfiylik siyosati, rozilik (analitika/marketing uchun), ma'lumotga kirish/o'chirish huquqi (yuqoridagi akkaunt o'chirish oqimi), DPA'lar (Vivox, CDN, crash provider).
- Maxfiylik siyosati va foydalanish shartlari: uz/ru/en, ilova ichida va saytda.

## 6. Asset litsenziyalari
- Ruxsat: CC0, CC-BY (attribution bilan), Sketchfab Standard, CGTrader Royalty Free, OFL (shriftlar), UI ikonkalar uchun CC BY 3.0 (game-icons.net) va Apache 2.0 (Material Symbols). Taqiq: NC, Editorial, ND, SA, noma'lum ([ART_PIPELINE §2](ART_PIPELINE.md#2-litsenziya-siyosati)).
- Manbalar: [Sketchfab licenses](https://sketchfab.com/licenses), [Sketchfab Store FAQ](https://help.sketchfab.com/en/articles/16152225-store-license-usage-faq), [CGTrader RF vs Editorial](https://help.cgtrader.com/hc/en-us/articles/360015193418-What-is-the-difference-between-Royalty-Free-and-Editorial-licenses). Real avtomobil/brend nusxalari RF litsenziya bilan ham huquq egasi ruxsatisiz ishlatilmaydi ([CGTrader forum](https://subscriptions.cgtrader.com/forum/selling-buying-3d-models/does-buying-cgtrader-car-models-create-a-royalty-problem)).
- Har bir asset `ArtSource/LICENSES.md` da; Credits ekrani avtomatik; build'da reyestrsiz asset bo'lsa build xato beradi.
- Haqiqiy shaxslar portretlari va brend logotiplari ishlatilmaydi (shaxsiy hayot huquqi va savdo belgisi xavfi).
- Royalty-free modellar o'yindan "ajratib olinmasligi" uchun: Addressables bundle'lar (siqilgan), xom FBX build'ga kirmaydi.

## 7. Iste'molchi himoyasi va adolatli dizayn
- Narxlar real valyutada ham ko'rsatiladi (Gem paketlarida); itemlar narxi Gem'da, lekin xarid oynasida taxminiy real ekvivalent ko'rsatish (ixtiyoriy, EEA CPC tavsiyalariga mos).
- Soxta taymer, soxta "faqat 2 ta qoldi" yo'q.
- Yashik ochish animatsiyasi har doim skip qilinadi.
- Refund: Play — Google qoidalari bo'yicha; veb — maxfiylik/oferta shartlarida (sarflanmagan Gem 14 kun ichida).
- Oferta (ommaviy shartnoma) veb-do'konda UZS to'lovlar uchun; fiskal chek (Payme `detail` / OFD) — O'zbekiston talabi.

## 8. Launch oldidan checklist (M15)
- [ ] IARC so'rovnomasi (random items = Ha), reytinglar tekshirildi
- [ ] Data safety formasi, maxfiylik siyosati URL, akkaunt o'chirish URL
- [ ] targetSdk 36, 64-bit, AAB
- [ ] Play build'da `STORE_DIRECT` kodi yo'qligini avtomatik tekshirish (string scan: "payme", "click.uz", "uzum", veb-do'kon domeni)
- [ ] Ehtimollar UI = server jadvali (avtotest)
- [ ] `country_rules` BE uchun ishlaydi (VPN test)
- [ ] LICENSES.md to'liq, Credits ekrani
- [ ] Chat/ovoz: report, block, mute
- [ ] Yurist xulosasi: data localization, oferta, loot box (UZ)
