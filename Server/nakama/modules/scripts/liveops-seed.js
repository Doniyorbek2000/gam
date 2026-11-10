// First-run LiveOps content (ROADMAP M3.2): missions, Season Pass, events, daily shop. gen-seed.js puts it into
// src/seed.ts; at runtime the server reads config/<name> (admin panel M4 edits it), never these constants.
// Numbers follow docs/GDD.md §6.3–6.4 and docs/ECONOMY.md §4.3, §6, §7.

const coins = (amount) => ({ kind: "coins", amount });
const gems = (amount) => ({ kind: "gems", amount });
const shards = (amount) => ({ kind: "shards", amount });
const crystals = (amount) => ({ kind: "crystals", amount });
const key = (crate, amount) => ({ kind: "key_" + crate, amount });
const item = (id) => ({ kind: "item", amount: 1, id });

// Daily: 3 of the pool, ~300 coins + ~200 Pass XP each. Weekly: 5 of the pool, more XP (ECONOMY §6: ~3 000 a week).
const missions = {
    dailyCount: 3,
    weeklyCount: 5,
    daily: [
        { id: "d_play2", type: "play", target: 2, coins: 300, xp: 200 },
        { id: "d_play3", type: "play", target: 3, coins: 300, xp: 200 },
        { id: "d_win1", type: "win", target: 1, coins: 300, xp: 200 },
        { id: "d_win2", type: "win", target: 2, coins: 400, xp: 250 },
        { id: "d_pot5", type: "pot", target: 5, coins: 300, xp: 200 },
        { id: "d_pot10", type: "pot", target: 10, coins: 400, xp: 250 },
    ],
    weekly: [
        { id: "w_play10", type: "play", target: 10, coins: 1000, xp: 600 },
        { id: "w_play20", type: "play", target: 20, coins: 1500, xp: 800 },
        { id: "w_win5", type: "win", target: 5, coins: 1000, xp: 600 },
        { id: "w_win10", type: "win", target: 10, coins: 1500, xp: 800 },
        { id: "w_pot30", type: "pot", target: 30, coins: 1000, xp: 600 },
        { id: "w_pot60", type: "pot", target: 60, coins: 1500, xp: 800 },
        { id: "w_venue_silk3", type: "win_venue", target: 3, venue: "silk_road_lounge", coins: 1200, xp: 700 },
    ],
};

// ECONOMY §6 table; the levels in between alternate small Coin / Shard rewards.
const passFree = {
    1: [coins(500)], 5: [key("classic", 1)], 10: [gems(20)], 15: [coins(2000)], 20: [item("sticker_break_bolt")],
    25: [gems(30)], 30: [key("classic", 2)], 35: [coins(5000)], 40: [gems(50)], 45: [item("frame_azure")], 50: [gems(50)],
};
const passPremium = {
    1: [item("cue_carbon")], 5: [gems(100)], 10: [item("table_modern")], 15: [key("premium", 2)], 20: [item("cue_ornate")],
    25: [item("table_silk")], 30: [crystals(20)], 35: [item("sticker_crown_cue")], 40: [key("premium", 3), gems(100)],
    45: [item("frame_gold")], 50: [item("cue_dragon"), crystals(30)],
};
const levels = [];
for (let level = 1; level <= 50; level++) {
    levels.push({
        free: passFree[level] || [level % 2 === 0 ? shards(20) : coins(300)],
        premium: passPremium[level] || [level % 2 === 0 ? shards(40) : coins(600)],
    });
}

const pass = {
    season: 1,
    // Season 1 "Ipak yo'li": 8 weeks from 2026-11-10 00:00 Tashkent (UTC+5).
    start: "2026-11-09T19:00:00Z",
    days: 56,
    xpPerLevel: 1000,
    premiumPrice: 800,
    matchXp: 50,
    winXp: 80,
    levels,
};

// Times are real windows (no fake countdowns, ECONOMY §4.2). repeatDays > 0 repeats the window.
const events = {
    events: [
        { id: "weekend_coins", start: "2026-11-13T19:00:00Z", hours: 48, repeatDays: 7, coinMultiplier: 2, xpMultiplier: 1, crate: "" },
        { id: "premium_crate_week", start: "2026-11-09T19:00:00Z", hours: 168, repeatDays: 28, coinMultiplier: 1, xpMultiplier: 1, crate: "premium" },
    ],
};

// ECONOMY §4.3: 6 slots a day — 2 Common/Rare (coin), 2 Rare/Epic (gem), 1 Legendary (gem), 1 featured (Mythic).
const dailyShop = {
    slots: [
        { rarities: [0, 1], currency: "coin", featured: false },
        { rarities: [0, 1], currency: "coin", featured: false },
        { rarities: [1, 2], currency: "gem", featured: false },
        { rarities: [1, 2], currency: "gem", featured: false },
        { rarities: [3], currency: "gem", featured: false },
        { rarities: [4], currency: "gem", featured: true },
    ],
    coin: [1500, 6000, 0, 0, 0],
    gem: [0, 150, 450, 1400, 6000],
};

module.exports = { missions, pass, events, daily_shop: dailyShop };
