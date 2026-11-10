// LiveOps (ROADMAP M3.2, docs/GDD.md §6.3–6.4, docs/ECONOMY.md §4.3, §6): missions, Season Pass, timed events and the
// daily rotating shop. Content comes from config/<name> (missions, pass, events, daily_shop); progress comes only from
// server-run matches (recordMatchResult). Rewards go through economy.ts commit(): wallet ledger + idempotency record.

var COL_LIVEOPS = "liveops";
var HOUR_MS = 3600 * 1000;
/** A match must have at least this many shots to count, so an instant forfeit cannot farm missions or Pass XP. */
var MIN_SHOTS_TO_COUNT = 3;
var STATE_WRITE_ATTEMPTS = 3;
var REWARD_WALLET_KINDS = ["coins", "gems", "shards", "crystals", "key_classic", "key_premium"];

interface Reward { kind: string; amount: number; id?: string; }
interface MissionTemplate { id: string; type: string; target: number; venue?: string; coins: number; xp: number; }
interface MissionsConfig { dailyCount: number; weeklyCount: number; daily: MissionTemplate[]; weekly: MissionTemplate[]; }
interface MissionSlot { id: string; progress: number; claimed: boolean; }
interface MissionsState { day: number; week: number; daily: MissionSlot[]; weekly: MissionSlot[]; rerollDay: number; }
interface PassLevel { free: Reward[]; premium: Reward[]; }
interface PassConfig {
    season: number; start: string; days: number; xpPerLevel: number; premiumPrice: number;
    matchXp: number; winXp: number; levels: PassLevel[];
}
interface PassState { season: number; xp: number; premium: boolean; free: number[]; paid: number[]; }
interface EventConfig { id: string; start: string; hours: number; repeatDays: number; coinMultiplier: number; xpMultiplier: number; crate: string; }
interface ShopSlotConfig { rarities: number[]; currency: string; featured: boolean; }
interface DailyShopConfig { slots: ShopSlotConfig[]; coin: number[]; gem: number[]; }
interface ShopOffer { id: string; currency: string; price: number; featured: boolean; }
interface ShopDayState { day: number; offers: (ShopOffer | null)[]; bought: number[]; }
interface Stored<T> { value: T; version: string; }
interface MatchStats { won: boolean; potted: number; venue: string; }

function weekNumber(day: number): number { return Math.floor((day + 3) / 7); }   // weeks start on Monday (day 0 = Thu)

function readState<T>(nk: nkruntime.Nakama, userId: string, key: string): Stored<T> | null {
    var found = nk.storageRead([{ collection: COL_LIVEOPS, key: key, userId: userId }]);
    return found.length > 0 ? { value: found[0].value as T, version: found[0].version } : null;
}

function stateWrite(userId: string, key: string, value: any, version: string): nkruntime.StorageWriteRequest {
    return { collection: COL_LIVEOPS, key: key, userId: userId, value: value, version: version, permissionRead: READ_OWNER, permissionWrite: WRITE_NONE };
}

// ---- events -------------------------------------------------------------------------------------------------

/** The current or next window of an event: repeatDays > 0 repeats it, otherwise it runs once. */
function eventWindow(e: EventConfig, nowMs: number): { start: number; end: number } {
    var start = Date.parse(e.start);
    var length = e.hours * HOUR_MS;
    if (isNaN(start) || !(length > 0)) throw new Error("event " + e.id + ": bad start/hours");
    var period = e.repeatDays > 0 ? e.repeatDays * DAY_MS : 0;
    if (period > 0 && nowMs >= start) {
        start += Math.floor((nowMs - start) / period) * period;
        if (nowMs >= start + length) start += period;
    }
    return { start: start, end: start + length };
}

function eventList(nk: nkruntime.Nakama, nowMs: number): any[] {
    var events: EventConfig[] = config(nk, "events").value.events || [];
    var out: any[] = [];
    for (var i = 0; i < events.length; i++) {
        var e = events[i];
        var w = eventWindow(e, nowMs);
        if (w.end <= nowMs) continue;   // a one-off that is over
        out.push({
            id: e.id, active: w.start <= nowMs, startsInMs: Math.max(0, w.start - nowMs), endsInMs: w.end - nowMs,
            coinMultiplier: e.coinMultiplier || 1, xpMultiplier: e.xpMultiplier || 1, crate: e.crate || "",
        });
    }
    return out;
}

/** Highest active multiplier per kind (events do not stack). */
function multipliers(nk: nkruntime.Nakama, nowMs: number): { coins: number; xp: number } {
    var list = eventList(nk, nowMs);
    var m = { coins: 1, xp: 1 };
    for (var i = 0; i < list.length; i++) {
        if (!list[i].active) continue;
        m.coins = Math.max(m.coins, list[i].coinMultiplier);
        m.xp = Math.max(m.xp, list[i].xpMultiplier);
    }
    return m;
}

// ---- missions -----------------------------------------------------------------------------------------------

/** Picks count distinct templates with HMAC rolls (server secret), so a player cannot predict or steer them. */
function drawMissions(nk: nkruntime.Nakama, secret: string, userId: string, pool: MissionTemplate[], count: number, tag: string): MissionSlot[] {
    var left = pool.slice();
    var out: MissionSlot[] = [];
    for (var i = 0; i < count && left.length > 0; i++) {
        var pick = Math.floor(hmacRoll(nk, secret, userId, tag, i) * left.length);
        out.push({ id: left[pick].id, progress: 0, claimed: false });
        left.splice(pick, 1);
    }
    return out;
}

/** Today's and this week's missions; a new day or week draws fresh ones (changed = needs a write). */
function currentMissions(ctx: nkruntime.Context, nk: nkruntime.Nakama, userId: string, nowMs: number): { state: MissionsState; version: string; changed: boolean } {
    var cfg: MissionsConfig = config(nk, "missions").value;
    var stored = readState<MissionsState>(nk, userId, "missions");
    var state: MissionsState = stored ? stored.value : { day: -1, week: -1, daily: [], weekly: [], rerollDay: -1 };
    var today = dayNumber(nowMs);
    var week = weekNumber(today);
    var changed = false;
    if (state.day !== today) {
        state.daily = drawMissions(nk, crateSecret(ctx).secret, userId, cfg.daily, cfg.dailyCount, "missions:d" + today);
        state.day = today;
        changed = true;
    }
    if (state.week !== week) {
        state.weekly = drawMissions(nk, crateSecret(ctx).secret, userId, cfg.weekly, cfg.weeklyCount, "missions:w" + week);
        state.week = week;
        changed = true;
    }
    return { state: state, version: stored ? stored.version : "*", changed: changed };
}

function findTemplate(pool: MissionTemplate[], id: string): MissionTemplate | null {
    for (var i = 0; i < pool.length; i++) if (pool[i].id === id) return pool[i];
    return null;
}

function missionGain(t: MissionTemplate, stats: MatchStats): number {
    if (t.type === "play") return 1;
    if (t.type === "win") return stats.won ? 1 : 0;
    if (t.type === "pot") return stats.potted;
    if (t.type === "win_venue") return stats.won && stats.venue === t.venue ? 1 : 0;
    return 0;
}

function advanceMissions(slots: MissionSlot[], pool: MissionTemplate[], stats: MatchStats): void {
    for (var i = 0; i < slots.length; i++) {
        var t = findTemplate(pool, slots[i].id);
        if (!t || slots[i].claimed) continue;
        slots[i].progress = Math.min(t.target, slots[i].progress + missionGain(t, stats));
    }
}

function missionView(slots: MissionSlot[], pool: MissionTemplate[]): any[] {
    var out: any[] = [];
    for (var i = 0; i < slots.length; i++) {
        var t = findTemplate(pool, slots[i].id);
        if (!t) continue;   // template removed by the admin: hide the slot
        out.push({ index: i, id: t.id, type: t.type, target: t.target, venue: t.venue || "", progress: slots[i].progress, claimed: slots[i].claimed, coins: t.coins, xp: t.xp });
    }
    return out;
}

// ---- Season Pass --------------------------------------------------------------------------------------------

function passWindow(cfg: PassConfig): { start: number; end: number } {
    var start = Date.parse(cfg.start);
    if (isNaN(start)) throw new Error("pass: bad start");
    return { start: start, end: start + cfg.days * DAY_MS };
}

function passActive(cfg: PassConfig, nowMs: number): boolean {
    var w = passWindow(cfg);
    return nowMs >= w.start && nowMs < w.end;
}

/** The player's pass for the configured season; a new season starts from zero. */
function currentPass(nk: nkruntime.Nakama, userId: string, cfg: PassConfig): Stored<PassState> {
    var stored = readState<PassState>(nk, userId, "pass");
    if (stored && stored.value.season === cfg.season) return stored;
    return { value: { season: cfg.season, xp: 0, premium: false, free: [], paid: [] }, version: stored ? stored.version : "*" };
}

/** Level 1 at 0 XP, one level per xpPerLevel, capped at the last level. */
function passLevel(cfg: PassConfig, xp: number): number {
    return Math.min(cfg.levels.length, 1 + Math.floor(xp / cfg.xpPerLevel));
}

/** Adds rewards to the wallet changeset and inventory; an owned item becomes duplicate shards (ECONOMY §4.3). */
function grantRewards(rewards: Reward[], inv: Inventory, catalog: CatalogEntry[], change: { [c: string]: number }): any[] {
    var granted: any[] = [];
    for (var i = 0; i < rewards.length; i++) {
        var r = rewards[i];
        if (r.kind === "item") {
            var entry: CatalogEntry | null = null;
            for (var k = 0; k < catalog.length; k++) if (catalog[k].id === r.id) entry = catalog[k];
            if (!entry) throw new Error("reward item " + r.id + " is not in the catalog");
            if (owns(inv, entry.id)) {
                var shards = DUPLICATE_SHARDS[entry.rarity];
                change["shards"] = (change["shards"] || 0) + shards;
                granted.push({ kind: "item", id: entry.id, amount: 1, duplicateShards: shards });
            } else {
                inv.owned.push(entry.id);
                granted.push({ kind: "item", id: entry.id, amount: 1, duplicateShards: 0 });
            }
            continue;
        }
        if (REWARD_WALLET_KINDS.indexOf(r.kind) < 0 || !(r.amount > 0)) throw new Error("bad reward " + r.kind);
        change[r.kind] = (change[r.kind] || 0) + r.amount;
        granted.push({ kind: r.kind, amount: r.amount });
    }
    return granted;
}

function passView(cfg: PassConfig, pass: PassState, nowMs: number): any {
    var w = passWindow(cfg);
    var levels: any[] = [];
    for (var i = 0; i < cfg.levels.length; i++) {
        var level = i + 1;
        levels.push({ level: level, free: cfg.levels[i].free, premium: cfg.levels[i].premium, freeClaimed: pass.free.indexOf(level) >= 0, premiumClaimed: pass.paid.indexOf(level) >= 0 });
    }
    return {
        season: cfg.season, active: nowMs >= w.start && nowMs < w.end, startsInMs: Math.max(0, w.start - nowMs), endsInMs: Math.max(0, w.end - nowMs),
        xp: pass.xp, level: passLevel(cfg, pass.xp), xpPerLevel: cfg.xpPerLevel, premium: pass.premium, premiumPrice: cfg.premiumPrice, levels: levels,
    };
}

/** Claims one reward (level + track) or, with level 0, everything reached so far. */
function claimPass(nk: nkruntime.Nakama, userId: string, level: number, track: string, requestId: string): any {
    var key = requestKey("pass", requestId);
    var prior = priorResult(nk, userId, key);
    if (prior !== null) return prior;
    var cfg: PassConfig = config(nk, "pass").value;
    var stored = currentPass(nk, userId, cfg);
    var pass = stored.value;
    var reached = passLevel(cfg, pass.xp);
    var picks: { level: number; premium: boolean }[] = [];
    if (level === 0) {
        for (var l = 1; l <= reached; l++) {
            if (pass.free.indexOf(l) < 0) picks.push({ level: l, premium: false });
            if (pass.premium && pass.paid.indexOf(l) < 0) picks.push({ level: l, premium: true });
        }
    } else {
        if (!(level >= 1 && level <= cfg.levels.length) || Math.floor(level) !== level) throw new Error("bad level");
        if (track !== "free" && track !== "premium") throw new Error("track must be free or premium");
        if (level > reached) throw new Error("level not reached yet");
        var premium = track === "premium";
        if (premium && !pass.premium) throw new Error("premium pass required");
        if ((premium ? pass.paid : pass.free).indexOf(level) >= 0) throw new Error("already claimed");
        picks.push({ level: level, premium: premium });
    }
    if (picks.length === 0) throw new Error("nothing to claim");

    var inventory = readInventory(nk, userId);
    var catalog = catalogItems(nk);
    var change: { [c: string]: number } = {};
    var granted: any[] = [];
    for (var i = 0; i < picks.length; i++) {
        var def = cfg.levels[picks[i].level - 1];
        granted = granted.concat(grantRewards(picks[i].premium ? def.premium : def.free, inventory.inv, catalog, change));
        (picks[i].premium ? pass.paid : pass.free).push(picks[i].level);
    }
    var writes = [stateWrite(userId, "pass", pass, stored.version), inventoryWrite(userId, inventory.inv, inventory.version)];
    return commit(nk, userId, key, { claimed: picks, rewards: granted }, writes, change, "pass_reward", "s" + cfg.season);
}

function buyPassPremium(nk: nkruntime.Nakama, userId: string, nowMs: number): any {
    var cfg: PassConfig = config(nk, "pass").value;
    var key = "pass_buy:" + cfg.season;   // server-made: one purchase per season whatever the client retries
    var prior = priorResult(nk, userId, key);
    if (prior !== null) return prior;
    if (!passActive(cfg, nowMs)) throw new Error("season is not running");
    var stored = currentPass(nk, userId, cfg);
    if (stored.value.premium) throw new Error("premium already active");
    stored.value.premium = true;
    return commit(nk, userId, key, { premium: true, price: cfg.premiumPrice }, [stateWrite(userId, "pass", stored.value, stored.version)],
        { gems: -cfg.premiumPrice }, "pass_premium", "s" + cfg.season);
}

// ---- match results ------------------------------------------------------------------------------------------

/** Mission progress and Pass XP for one finished online match (called by match.ts). Retries a version race. */
function recordMatchResult(ctx: nkruntime.Context, nk: nkruntime.Nakama, logger: nkruntime.Logger, userId: string, stats: MatchStats, nowMs: number): void {
    var mcfg: MissionsConfig = config(nk, "missions").value;
    var pcfg: PassConfig = config(nk, "pass").value;
    var xpBoost = multipliers(nk, nowMs).xp;
    for (var attempt = 0; attempt < STATE_WRITE_ATTEMPTS; attempt++) {
        var missions = currentMissions(ctx, nk, userId, nowMs);
        advanceMissions(missions.state.daily, mcfg.daily, stats);
        advanceMissions(missions.state.weekly, mcfg.weekly, stats);
        var writes = [stateWrite(userId, "missions", missions.state, missions.version)];
        if (passActive(pcfg, nowMs)) {
            var pass = currentPass(nk, userId, pcfg);
            pass.value.xp += Math.round((stats.won ? pcfg.winXp : pcfg.matchXp) * xpBoost);
            writes.push(stateWrite(userId, "pass", pass.value, pass.version));
        }
        try {
            nk.storageWrite(writes);
            return;
        } catch (e) {
            logger.debug("match result for %s raced (%s), retrying", userId, String(e));
        }
    }
    logger.error("match result for %s not saved after %d attempts", userId, STATE_WRITE_ATTEMPTS);
}

// ---- mission claim / reroll ---------------------------------------------------------------------------------

function claimMission(ctx: nkruntime.Context, nk: nkruntime.Nakama, userId: string, kind: string, index: number, nowMs: number): any {
    if (kind !== "daily" && kind !== "weekly") throw new Error("kind must be daily or weekly");
    var missions = currentMissions(ctx, nk, userId, nowMs);
    var state = missions.state;
    var period = kind === "daily" ? "d" + state.day : "w" + state.week;
    var key = "mission:" + period + ":" + index;   // server-made: one reward per slot and period
    var prior = priorResult(nk, userId, key);
    if (prior !== null) return prior;
    var slots = kind === "daily" ? state.daily : state.weekly;
    var slot = slots[index];
    var t = slot ? findTemplate(kind === "daily" ? config(nk, "missions").value.daily : config(nk, "missions").value.weekly, slot.id) : null;
    if (!slot || !t) throw new Error("no such mission");
    if (slot.claimed) throw new Error("already claimed");
    if (slot.progress < t.target) throw new Error("mission not complete");
    slot.claimed = true;

    var boost = multipliers(nk, nowMs);
    var coins = Math.round(t.coins * boost.coins);
    var xp = Math.round(t.xp * boost.xp);
    var writes = [stateWrite(userId, "missions", state, missions.version)];
    var pcfg: PassConfig = config(nk, "pass").value;
    if (passActive(pcfg, nowMs)) {
        var pass = currentPass(nk, userId, pcfg);
        pass.value.xp += xp;
        writes.push(stateWrite(userId, "pass", pass.value, pass.version));
    } else {
        xp = 0;
    }
    return commit(nk, userId, key, { id: t.id, coins: coins, xp: xp }, writes, { coins: coins }, "mission_reward", t.id);
}

/** Swaps one unclaimed daily mission for another from the pool; free once per day (docs/GDD.md §6.4). */
function rerollMission(ctx: nkruntime.Context, nk: nkruntime.Nakama, userId: string, index: number, nowMs: number): MissionsState {
    var cfg: MissionsConfig = config(nk, "missions").value;
    var missions = currentMissions(ctx, nk, userId, nowMs);
    var state = missions.state;
    if (state.rerollDay === state.day) throw new Error("free reroll already used today");
    var slot = state.daily[index];
    if (!slot) throw new Error("no such mission");
    if (slot.claimed) throw new Error("already claimed");
    var left: MissionTemplate[] = [];
    for (var i = 0; i < cfg.daily.length; i++) {
        var taken = false;
        for (var j = 0; j < state.daily.length; j++) if (state.daily[j].id === cfg.daily[i].id) taken = true;
        if (!taken) left.push(cfg.daily[i]);
    }
    if (left.length === 0) throw new Error("no other mission available");
    var pick = Math.floor(hmacRoll(nk, crateSecret(ctx).secret, userId, "missions:r" + state.day, index) * left.length);
    state.daily[index] = { id: left[pick].id, progress: 0, claimed: false };
    state.rerollDay = state.day;
    nk.storageWrite([stateWrite(userId, "missions", state, missions.version)]);
    return state;
}

// ---- daily shop ---------------------------------------------------------------------------------------------

/** Today's 6 offers, drawn once per player per UTC+5 day (HMAC rolls) from items they do not own yet. */
function currentShop(ctx: nkruntime.Context, nk: nkruntime.Nakama, userId: string, inv: Inventory, nowMs: number): { state: ShopDayState; version: string; changed: boolean } {
    var stored = readState<ShopDayState>(nk, userId, "shop");
    var today = dayNumber(nowMs);
    if (stored && stored.value.day === today) return { state: stored.value, version: stored.version, changed: false };
    var cfg: DailyShopConfig = config(nk, "daily_shop").value;
    var catalog = catalogItems(nk);
    var secret = crateSecret(ctx).secret;
    var picked: string[] = [];
    var offers: (ShopOffer | null)[] = [];
    for (var s = 0; s < cfg.slots.length; s++) {
        var slot = cfg.slots[s];
        var prices = slot.currency === "gem" ? cfg.gem : cfg.coin;
        var pool: CatalogEntry[] = [];
        for (var i = 0; i < catalog.length; i++) {
            var c = catalog[i];
            if (c.default || owns(inv, c.id) || picked.indexOf(c.id) >= 0) continue;
            if (slot.rarities.indexOf(c.rarity) < 0 || !(prices[c.rarity] > 0)) continue;
            pool.push(c);
        }
        if (pool.length === 0) {
            offers.push(null);
            continue;
        }
        var pick = pool[Math.floor(hmacRoll(nk, secret, userId, "shop:" + today, s) * pool.length)];
        picked.push(pick.id);
        offers.push({ id: pick.id, currency: slot.currency, price: prices[pick.rarity], featured: slot.featured === true });
    }
    return { state: { day: today, offers: offers, bought: [] }, version: stored ? stored.version : "*", changed: true };
}

function buyDailyOffer(ctx: nkruntime.Context, nk: nkruntime.Nakama, userId: string, slot: number, requestId: string, nowMs: number): any {
    var key = requestKey("dshop", requestId);
    var prior = priorResult(nk, userId, key);
    if (prior !== null) return prior;
    var inventory = readInventory(nk, userId);
    var shop = currentShop(ctx, nk, userId, inventory.inv, nowMs);
    var offer = shop.state.offers[slot];
    if (!offer) throw new Error("no such offer");
    if (shop.state.bought.indexOf(slot) >= 0) throw new Error("already bought today");
    if (owns(inventory.inv, offer.id)) throw new Error("already owned");
    inventory.inv.owned.push(offer.id);
    shop.state.bought.push(slot);
    var change: { [c: string]: number } = {};
    change[offer.currency === "gem" ? "gems" : "coins"] = -offer.price;
    var writes = [stateWrite(userId, "shop", shop.state, shop.version), inventoryWrite(userId, inventory.inv, inventory.version)];
    return commit(nk, userId, key, { slot: slot, id: offer.id, price: offer.price, currency: offer.currency }, writes, change, "daily_shop_buy", offer.id);
}

// ---- state --------------------------------------------------------------------------------------------------

/** Everything the lobby shows; new-day missions and shop offers are saved here on first read. */
function liveOpsState(ctx: nkruntime.Context, nk: nkruntime.Nakama, userId: string, nowMs: number, retried?: boolean): any {
    var mcfg: MissionsConfig = config(nk, "missions").value;
    var pcfg: PassConfig = config(nk, "pass").value;
    var inv = readInventory(nk, userId).inv;
    var missions = currentMissions(ctx, nk, userId, nowMs);
    var shop = currentShop(ctx, nk, userId, inv, nowMs);
    var writes: nkruntime.StorageWriteRequest[] = [];
    if (missions.changed) writes.push(stateWrite(userId, "missions", missions.state, missions.version));
    if (shop.changed) writes.push(stateWrite(userId, "shop", shop.state, shop.version));
    if (writes.length > 0) {
        try {
            nk.storageWrite(writes);
        } catch (e) {
            // A parallel request saved the same rollover first (draws are deterministic): read its copy once.
            if (!retried) return liveOpsState(ctx, nk, userId, nowMs, true);
            throw e;
        }
    }

    var today = dayNumber(nowMs);
    var dayReset = (today + 1) * DAY_MS - RESET_OFFSET_MS - nowMs;
    var weekReset = ((weekNumber(today) + 1) * 7 - 3) * DAY_MS - RESET_OFFSET_MS - nowMs;
    var offers: any[] = [];
    for (var s = 0; s < shop.state.offers.length; s++) {
        var o = shop.state.offers[s];
        if (o) offers.push({ slot: s, id: o.id, currency: o.currency, price: o.price, featured: o.featured, bought: shop.state.bought.indexOf(s) >= 0 });
    }
    return {
        missions: {
            daily: missionView(missions.state.daily, mcfg.daily), weekly: missionView(missions.state.weekly, mcfg.weekly),
            rerollFree: missions.state.rerollDay !== missions.state.day, dayResetInMs: dayReset, weekResetInMs: weekReset,
        },
        pass: passView(pcfg, currentPass(nk, userId, pcfg).value, nowMs),
        events: eventList(nk, nowMs),
        shop: { offers: offers, resetInMs: dayReset },
    };
}

// ---- RPCs ---------------------------------------------------------------------------------------------------

function slotNumber(value: any): number {
    var n = Number(value);
    if (!(n >= 0 && n < 64) || Math.floor(n) !== n) throw new Error("bad index");
    return n;
}

var rpcLiveOpsState: nkruntime.RpcFunction = function (ctx, logger, nk, payload) {
    return JSON.stringify(liveOpsState(ctx, nk, userOf(ctx), Date.now()));
};

var rpcMissionClaim: nkruntime.RpcFunction = function (ctx, logger, nk, payload) {
    var data = parsePayload(payload);
    return JSON.stringify(claimMission(ctx, nk, userOf(ctx), String(data.kind || ""), slotNumber(data.index), Date.now()));
};

var rpcMissionReroll: nkruntime.RpcFunction = function (ctx, logger, nk, payload) {
    rerollMission(ctx, nk, userOf(ctx), slotNumber(parsePayload(payload).index), Date.now());
    return JSON.stringify({ rerolled: true });
};

var rpcPassClaim: nkruntime.RpcFunction = function (ctx, logger, nk, payload) {
    var data = parsePayload(payload);
    var level = data.level === undefined || data.level === null ? 0 : Number(data.level);
    return JSON.stringify(claimPass(nk, userOf(ctx), level, String(data.track || ""), data.request_id));
};

var rpcPassBuy: nkruntime.RpcFunction = function (ctx, logger, nk, payload) {
    return JSON.stringify(buyPassPremium(nk, userOf(ctx), Date.now()));
};

var rpcDailyShopBuy: nkruntime.RpcFunction = function (ctx, logger, nk, payload) {
    var data = parsePayload(payload);
    return JSON.stringify(buyDailyOffer(ctx, nk, userOf(ctx), slotNumber(data.slot), data.request_id, Date.now()));
};
