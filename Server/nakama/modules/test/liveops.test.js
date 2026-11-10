// LiveOps (ROADMAP M3.2) against a fake Nakama: missions from match results, reroll, Season Pass XP / claims / premium,
// timed events and their multipliers, the daily rotating shop, storage permissions.
const test = require("node:test");
const assert = require("node:assert/strict");
const loadModule = require("./load");
const fakeNakama = require("./fake-nakama");

const m = loadModule();
const logger = { debug() {}, info() {}, warn() {}, error() {} };
const ctx = (userId) => ({ userId, username: "u_" + userId, env: { CRATE_SECRET: "test-secret-0123456789abcdef" } });
const DAY = 24 * 3600 * 1000;
const WED = Date.UTC(2026, 10, 11, 10, 0, 0);   // Wed 15:00 Tashkent: season 1 running, no weekend event
const SAT = Date.UTC(2026, 10, 14, 10, 0, 0);   // Sat 15:00 Tashkent: "weekend 2x coins"
let counter = 0;
// Values built inside the module's VM context have other prototypes: compare them as JSON.
const same = (actual, expected, message) => assert.equal(JSON.stringify(actual), JSON.stringify(expected), message);
const rid = () => "lo-" + (++counter).toString().padStart(6, "0");

function player(nk, userId) {
    m.afterAuthenticateDevice(ctx(userId), logger, nk, { created: true }, {});
    return m.liveOpsState(ctx(userId), nk, userId, WED);
}

const play = (nk, userId, stats, now) => m.recordMatchResult(ctx(userId), nk, logger, userId, Object.assign({ won: false, potted: 0, venue: "" }, stats), now);
const missionsOf = (nk, userId, now) => m.liveOpsState(ctx(userId), nk, userId, now).missions;

/** Plays won matches (5 pots each) in the lounge until every listed mission is complete. */
function completeAll(nk, userId, list, now) {
    for (let i = 0; i < 70 && missionsOf(nk, userId, now)[list].some((x) => x.progress < x.target); i++) {
        play(nk, userId, { won: true, potted: 5, venue: "silk_road_lounge" }, now);
    }
}

test("3 daily + 5 weekly distinct missions, stable within the day, new ones at 00:00 Tashkent and on Monday", () => {
    const nk = fakeNakama();
    const first = player(nk, "a").missions;
    assert.equal(first.daily.length, 3);
    assert.equal(first.weekly.length, 5);
    assert.equal(new Set(first.daily.map((x) => x.id)).size, 3);
    assert.equal(new Set(first.weekly.map((x) => x.id)).size, 5);
    same(missionsOf(nk, "a", WED + 3600 * 1000).daily, first.daily, "same day, same missions");
    assert.ok(first.dayResetInMs > 0 && first.dayResetInMs <= DAY);

    play(nk, "a", { won: true, potted: 4 }, WED);
    const sameWeek = missionsOf(nk, "a", WED + DAY);   // Thursday: daily reset, weekly kept
    assert.ok(sameWeek.daily.every((x) => x.progress === 0), "new day starts at zero");
    assert.ok(sameWeek.weekly.some((x) => x.progress > 0), "weekly progress survives the day change");
    const monday = Date.UTC(2026, 10, 15, 19, 0, 30);   // Mon 00:00:30 Tashkent
    assert.ok(missionsOf(nk, "a", monday - 60000).weekly.some((x) => x.progress > 0), "Sunday 23:59 is still last week");
    assert.ok(missionsOf(nk, "a", monday).weekly.every((x) => x.progress === 0), "new week");
});

test("match results advance missions; a reward is paid once, with a ledger reason and Pass XP", () => {
    const nk = fakeNakama();
    player(nk, "a");
    assert.throws(() => m.claimMission(ctx("a"), nk, "a", "daily", 0, WED), /not complete/);
    completeAll(nk, "a", "daily", WED);
    const mission = missionsOf(nk, "a", WED).daily[0];
    const before = nk.wallets.get("a").coins;
    const xpBefore = m.liveOpsState(ctx("a"), nk, "a", WED).pass.xp;
    const paid = m.claimMission(ctx("a"), nk, "a", "daily", 0, WED);
    assert.equal(paid.coins, mission.coins);
    assert.equal(nk.wallets.get("a").coins, before + mission.coins);
    assert.equal(m.liveOpsState(ctx("a"), nk, "a", WED).pass.xp, xpBefore + mission.xp);
    same(m.claimMission(ctx("a"), nk, "a", "daily", 0, WED), paid, "replay returns the same result");
    assert.equal(nk.wallets.get("a").coins, before + mission.coins, "no second credit");
    assert.equal(nk.ledger.filter((u) => u.metadata.reason === "mission_reward").length, 1);
    assert.ok(missionsOf(nk, "a", WED).daily[0].claimed);
});

test("weekend event doubles mission coins; matches give 50/80 Pass XP", () => {
    const nk = fakeNakama();
    player(nk, "a");
    play(nk, "a", { won: false }, SAT);
    play(nk, "a", { won: true }, SAT);
    assert.equal(m.liveOpsState(ctx("a"), nk, "a", SAT).pass.xp, 130);
    completeAll(nk, "a", "daily", SAT);
    const mission = missionsOf(nk, "a", SAT).daily[1];
    assert.equal(m.claimMission(ctx("a"), nk, "a", "daily", 1, SAT).coins, mission.coins * 2);
    const events = m.liveOpsState(ctx("a"), nk, "a", SAT).events;
    const weekend = events.find((e) => e.id === "weekend_coins");
    assert.ok(weekend.active && weekend.coinMultiplier === 2);
    assert.ok(weekend.endsInMs > 0 && weekend.endsInMs <= 48 * 3600 * 1000);
});

test("event windows repeat weekly and show the next start; a finished one-off is hidden", () => {
    const nk = fakeNakama();
    const weekend = m.SEED_CONFIG.events.events.find((e) => e.id === "weekend_coins");
    const w = m.eventWindow(weekend, WED + 14 * DAY);
    assert.equal(w.start, Date.parse(weekend.start) + 14 * DAY, "two weeks later: the third weekend is next");
    assert.equal(w.end - w.start, 48 * 3600 * 1000);
    const wed = m.eventList(nk, WED).find((e) => e.id === "weekend_coins");
    assert.equal(wed.active, false);
    assert.equal(wed.startsInMs, Date.parse(weekend.start) - WED);
    nk.storageWrite([{ collection: "config", key: "events", userId: "00000000-0000-0000-0000-000000000000", version: "*", permissionRead: 0, permissionWrite: 0,
        value: { events: [{ id: "old", start: "2026-01-01T00:00:00Z", hours: 24, repeatDays: 0, coinMultiplier: 3, xpMultiplier: 1, crate: "" }] } }]);
    same(m.eventList(nk, WED), []);
    assert.equal(m.multipliers(nk, WED).coins, 1, "expired event gives no boost");
});

test("an instant forfeit counts nothing; a real match counts for both players", () => {
    const nk = fakeNakama();
    player(nk, "a");
    player(nk, "b");
    const match = (seq) => ({ seq, venue: "silk_road_lounge", potted: [3, 1], players: [{ userId: "a" }, { userId: "b" }] });
    m.reportResult(ctx("a"), logger, nk, match(2), 0);
    assert.equal(m.liveOpsState(ctx("a"), nk, "a", Date.now()).pass.xp, 0);
    m.reportResult(ctx("a"), logger, nk, match(9), 0);
    const now = Date.now();
    const a = m.liveOpsState(ctx("a"), nk, "a", now);
    const b = m.liveOpsState(ctx("b"), nk, "b", now);
    if (a.pass.active) {
        assert.equal(a.pass.xp, 80);
        assert.equal(b.pass.xp, 50);
    }
    assert.ok(a.missions.daily.some((x) => x.progress > 0));
});

test("one free reroll per day swaps an unclaimed daily mission for a new one", () => {
    const nk = fakeNakama();
    const before = player(nk, "a").missions.daily.map((x) => x.id);
    m.rerollMission(ctx("a"), nk, "a", 2, WED);
    const after = missionsOf(nk, "a", WED);
    assert.ok(!before.includes(after.daily[2].id), "a template not already on the list");
    assert.equal(after.rerollFree, false);
    assert.throws(() => m.rerollMission(ctx("a"), nk, "a", 1, WED), /already used/);
    assert.equal(missionsOf(nk, "a", WED + DAY).rerollFree, true, "free again tomorrow");
});

test("Season Pass: XP levels, free claims, premium for 800 gems once, claim-all grants items and duplicate shards", () => {
    const nk = fakeNakama();
    const start = player(nk, "a").pass;
    assert.equal(start.level, 1);
    assert.equal(start.levels.length, 50);
    assert.ok(start.active);
    assert.throws(() => m.claimPass(nk, "a", 2, "free", rid()), /not reached/);
    assert.throws(() => m.claimPass(nk, "a", 1, "premium", rid()), /premium pass required/);
    const coins = nk.wallets.get("a").coins;
    m.claimPass(nk, "a", 1, "free", rid());
    assert.equal(nk.wallets.get("a").coins, coins + 500);
    assert.throws(() => m.claimPass(nk, "a", 1, "free", rid()), /already claimed/);

    assert.throws(() => m.buyPassPremium(nk, "a", WED), /not enough gems/);
    nk.wallets.set("a", { ...nk.wallets.get("a"), gems: 1000 });
    m.buyPassPremium(nk, "a", WED);
    assert.equal(nk.wallets.get("a").gems, 200);
    same(m.buyPassPremium(nk, "a", WED + 1000), { premium: true, price: 800 }, "a retry replays the purchase");
    assert.equal(nk.wallets.get("a").gems, 200, "charged once per season");

    // Own the level-10 premium table already: it must pay Epic duplicate shards instead.
    m.buyItem(nk, "a", "cue_maple", "coin", rid());
    const inv = nk.storageRead([{ collection: "inventory", key: "items", userId: "a" }])[0];
    nk.storageWrite([{ ...inv, value: { ...inv.value, owned: inv.value.owned.concat(["table_modern"]) } }]);
    for (let i = 0; i < 125; i++) play(nk, "a", { won: true }, WED);   // 10 000 XP
    assert.equal(m.liveOpsState(ctx("a"), nk, "a", WED).pass.level, 11);
    const all = m.claimPass(nk, "a", 0, "", rid());
    assert.equal(all.claimed.length, 10 + 11, "levels 2-11 free + 1-11 premium");
    const owned = nk.storageRead([{ collection: "inventory", key: "items", userId: "a" }])[0].value.owned;
    assert.ok(owned.includes("cue_carbon"), "level 1 premium item");
    assert.ok(all.rewards.some((r) => r.id === "table_modern" && r.duplicateShards === 60));
    assert.throws(() => m.claimPass(nk, "a", 0, "", rid()), /nothing to claim/);
    assert.ok(nk.ledger.some((u) => u.metadata.reason === "pass_premium"));
    assert.ok(nk.ledger.some((u) => u.metadata.reason === "pass_reward"));
});

test("a new season starts the pass from zero; buying outside a season is refused", () => {
    const nk = fakeNakama();
    player(nk, "a");
    for (let i = 0; i < 20; i++) play(nk, "a", { won: true }, WED);
    assert.equal(m.liveOpsState(ctx("a"), nk, "a", WED).pass.xp, 1600);
    const next = { ...m.SEED_CONFIG.pass, season: 2, start: "2027-01-04T19:00:00Z" };
    nk.storageWrite([{ collection: "config", key: "pass", userId: "00000000-0000-0000-0000-000000000000", version: "*", permissionRead: 0, permissionWrite: 0, value: next }]);
    const pass = m.liveOpsState(ctx("a"), nk, "a", WED).pass;
    assert.equal(pass.season, 2);
    assert.equal(pass.xp, 0);
    assert.equal(pass.active, false);
    play(nk, "a", { won: true }, WED);
    assert.equal(m.liveOpsState(ctx("a"), nk, "a", WED).pass.xp, 0, "no XP before the season starts");
    assert.throws(() => m.buyPassPremium(nk, "a", WED), /not running/);
});

test("daily shop: 6 offers of unowned items with ECONOMY §4.3 prices, bought once, new offers tomorrow", () => {
    const nk = fakeNakama();
    const shop = player(nk, "a").shop;
    const catalog = m.SEED_CONFIG.catalog.items;
    const rarity = (id) => catalog.find((c) => c.id === id).rarity;
    assert.equal(shop.offers.length, 6);
    assert.equal(new Set(shop.offers.map((o) => o.id)).size, 6);
    assert.ok(shop.offers.every((o) => !catalog.find((c) => c.id === o.id).default), "no default items");
    same(shop.offers.map((o) => o.currency), ["coin", "coin", "gem", "gem", "gem", "gem"]);
    assert.ok(shop.offers.slice(0, 2).every((o) => rarity(o.id) <= 1));
    assert.equal(rarity(shop.offers[4].id), 3);
    assert.equal(shop.offers[5].id, "cue_dragon");
    assert.equal(shop.offers[5].price, 6000);
    assert.ok(shop.offers[5].featured);

    const coins = nk.wallets.get("a").coins;
    const bought = m.buyDailyOffer(ctx("a"), nk, "a", 0, rid(), WED);
    assert.equal(nk.wallets.get("a").coins, coins - shop.offers[0].price);
    assert.equal(bought.id, shop.offers[0].id);
    const after = m.liveOpsState(ctx("a"), nk, "a", WED).shop;
    same(after.offers.map((o) => o.id), shop.offers.map((o) => o.id), "offers stay for the day");
    assert.ok(after.offers[0].bought);
    assert.throws(() => m.buyDailyOffer(ctx("a"), nk, "a", 0, rid(), WED), /already bought/);
    assert.throws(() => m.buyDailyOffer(ctx("a"), nk, "a", 5, rid(), WED), /not enough gems/);
    assert.ok(nk.ledger.some((u) => u.metadata.reason === "daily_shop_buy"));
    const tomorrow = m.liveOpsState(ctx("a"), nk, "a", WED + DAY).shop;
    assert.ok(tomorrow.offers.every((o) => !o.bought));
    assert.ok(!tomorrow.offers.some((o) => o.id === bought.id), "owned items leave the rotation");
});

test("LiveOps storage is server-write only", () => {
    const nk = fakeNakama();
    player(nk, "a");
    play(nk, "a", { won: true, potted: 3 }, WED);
    m.buyDailyOffer(ctx("a"), nk, "a", 0, rid(), WED);
    m.claimPass(nk, "a", 1, "free", rid());
    for (const o of nk.objects.values()) assert.equal(o.permissionWrite, 0, o.collection + "/" + o.key);
    assert.throws(() => m.rpcLiveOpsState({ userId: "a", env: {} }, logger, nk, "{}"), /CRATE_SECRET/);
});
