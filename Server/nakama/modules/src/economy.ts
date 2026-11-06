// Server economy (ROADMAP M3.1, docs/ECONOMY.md): Player ID, wallet + ledger, inventory, crates (HMAC RNG + pity),
// direct shop, 7-day login. Every currency change goes through nk.multiUpdate together with an idempotency record
// keyed by the client's request_id, so a retried request never debits twice (CLAUDE.md rule 3). The client only
// reads: every storage object here has permissionWrite 0.

var SYSTEM_USER = "00000000-0000-0000-0000-000000000000";
var COL_CONFIG = "config";
var COL_PLAYER_IDS = "player_ids";
var COL_PROFILE = "profile";
var COL_INVENTORY = "inventory";
var COL_LOGIN = "login";
var COL_IDEMPOTENCY = "idempotency";
var READ_NONE: nkruntime.ReadPermissionValues = 0;
var READ_OWNER: nkruntime.ReadPermissionValues = 1;
var WRITE_NONE: nkruntime.WritePermissionValues = 0;

var PLAYER_ID_ATTEMPTS = 12;
var REQUEST_ID_PATTERN = /^[A-Za-z0-9_-]{8,64}$/;
var ITEM_ID_PATTERN = /^[a-z0-9_-]{1,64}$/;
/** Day boundaries for login rewards and resets: 00:00 Asia/Tashkent (UTC+5, no DST). */
var RESET_OFFSET_MS = 5 * 3600 * 1000;
var DAY_MS = 24 * 3600 * 1000;
var RARITY_COUNT = 5;
/** docs/ECONOMY.md §4.3: shards per duplicate, by rarity; Mythic duplicates also give 10 crystals. */
var DUPLICATE_SHARDS = [5, 20, 60, 250, 800];
var MYTHIC_DUPLICATE_CRYSTALS = 10;
var RARITY_NAMES = ["Common", "Rare", "Epic", "Legendary", "Mythic"];

interface CatalogEntry { id: string; type: string; rarity: number; default: boolean; }
interface PityRuleConfig { minRarity: string; count: number; }
interface CrateConfig {
    id: string; displayName: string; currency: string; price: number; price10: number;
    weights: number[]; pity: PityRuleConfig[]; tenGuarantee: string;
}
interface Inventory { owned: string[]; equipped: { [slot: string]: string }; pity: { [crateId: string]: number[] }; rolls: number; }
interface CratePickResult { rarity: number; index: number; }

// ---- config -------------------------------------------------------------------------------------------------

/** Writes any missing config/<name> from the generated seed (first run); never overwrites admin edits. */
function seedConfig(nk: nkruntime.Nakama, logger: nkruntime.Logger): void {
    var names = Object.keys(SEED_CONFIG);
    var existing = nk.storageRead(names.map(function (n) { return { collection: COL_CONFIG, key: n, userId: SYSTEM_USER }; }));
    var have: { [name: string]: boolean } = {};
    for (var i = 0; i < existing.length; i++) have[existing[i].key] = true;
    var writes: nkruntime.StorageWriteRequest[] = [];
    for (var j = 0; j < names.length; j++) {
        if (have[names[j]]) continue;
        writes.push({ collection: COL_CONFIG, key: names[j], userId: SYSTEM_USER, value: SEED_CONFIG[names[j]], version: "*", permissionRead: READ_NONE, permissionWrite: WRITE_NONE });
    }
    if (writes.length === 0) return;
    nk.storageWrite(writes);
    logger.info("config seeded: %s", writes.map(function (w) { return w.key; }).join(","));
}

function config(nk: nkruntime.Nakama, name: string): { value: any; version: string } {
    var found = nk.storageRead([{ collection: COL_CONFIG, key: name, userId: SYSTEM_USER }]);
    if (found.length > 0) return { value: found[0].value, version: found[0].version };
    if (SEED_CONFIG[name] === undefined) throw new Error("unknown config " + name);
    return { value: SEED_CONFIG[name], version: "seed" };
}

function catalogItems(nk: nkruntime.Nakama): CatalogEntry[] { return config(nk, "catalog").value.items; }

function findCrate(nk: nkruntime.Nakama, id: string): CrateConfig {
    var crates: CrateConfig[] = config(nk, "crates").value.crates;
    for (var i = 0; i < crates.length; i++) if (crates[i].id === id) return crates[i];
    throw new Error("unknown crate");
}

// ---- Player ID ----------------------------------------------------------------------------------------------

/**
 * 10 random decimal digits, first one 1-9, drawn by rejection sampling from the hex of nk.uuidv4() (crypto
 * random). Not sequential, so IDs do not reveal sign-up order or player count.
 */
function randomPlayerId(uuid: () => string): string {
    var digits = "";
    while (digits.length < 10) {
        var hex = uuid().replace(/-/g, "");
        for (var i = 0; i < hex.length && digits.length < 10; i++) {
            var v = parseInt(hex.charAt(i), 16);
            if (v > 9 || (digits.length === 0 && v === 0)) continue;
            digits += String(v);
        }
    }
    return digits;
}

function storedPlayerId(nk: nkruntime.Nakama, userId: string): string {
    var found = nk.storageRead([{ collection: COL_PROFILE, key: "pid", userId: userId }]);
    return found.length > 0 ? String(found[0].value.pid) : "";
}

/** Gives the user a unique Player ID once; a collision (version "*" write refused) draws again. */
function ensurePlayerId(nk: nkruntime.Nakama, logger: nkruntime.Logger, userId: string): string {
    var existing = storedPlayerId(nk, userId);
    if (existing) return existing;
    for (var attempt = 0; attempt < PLAYER_ID_ATTEMPTS; attempt++) {
        var pid = randomPlayerId(function () { return nk.uuidv4(); });
        try {
            nk.storageWrite([
                { collection: COL_PLAYER_IDS, key: pid, userId: SYSTEM_USER, value: { userId: userId }, version: "*", permissionRead: READ_NONE, permissionWrite: WRITE_NONE },
                { collection: COL_PROFILE, key: "pid", userId: userId, value: { pid: pid }, version: "*", permissionRead: READ_OWNER, permissionWrite: WRITE_NONE },
            ]);
            return pid;
        } catch (e) {
            var raced = storedPlayerId(nk, userId);
            if (raced) return raced;   // a parallel call won; keep its ID
            logger.debug("player id %s taken, retrying", pid);
        }
    }
    throw new Error("could not allocate a player id");
}

function userByPlayerId(nk: nkruntime.Nakama, pid: string): string {
    if (!/^[1-9][0-9]{9}$/.test(pid)) throw new Error("player id must be 10 digits");
    var found = nk.storageRead([{ collection: COL_PLAYER_IDS, key: pid, userId: SYSTEM_USER }]);
    if (found.length === 0) throw new Error("player not found");
    return String(found[0].value.userId);
}

// ---- inventory ----------------------------------------------------------------------------------------------

function defaultInventory(catalog: CatalogEntry[]): Inventory {
    var inv: Inventory = { owned: [], equipped: {}, pity: {}, rolls: 0 };
    for (var i = 0; i < catalog.length; i++) {
        var item = catalog[i];
        if (!item.default) continue;
        inv.owned.push(item.id);
        if (!inv.equipped[item.type] && item.type !== "Sticker") inv.equipped[item.type] = item.id;
    }
    return inv;
}

function readInventory(nk: nkruntime.Nakama, userId: string): { inv: Inventory; version: string } {
    var found = nk.storageRead([{ collection: COL_INVENTORY, key: "items", userId: userId }]);
    if (found.length > 0) return { inv: found[0].value as Inventory, version: found[0].version };
    return { inv: defaultInventory(catalogItems(nk)), version: "*" };
}

function inventoryWrite(userId: string, inv: Inventory, version: string): nkruntime.StorageWriteRequest {
    // Optimistic concurrency: a parallel change makes this write (and the whole multiUpdate) fail.
    return { collection: COL_INVENTORY, key: "items", userId: userId, value: inv, version: version, permissionRead: READ_OWNER, permissionWrite: WRITE_NONE };
}

function owns(inv: Inventory, id: string): boolean { return inv.owned.indexOf(id) >= 0; }

// ---- idempotent commit ----------------------------------------------------------------------------------------

function requestKey(op: string, requestId: any): string {
    if (typeof requestId !== "string" || !REQUEST_ID_PATTERN.test(requestId)) throw new Error("request_id must be 8-64 letters, digits, '-' or '_'");
    return op + ":" + requestId;
}

function priorResult(nk: nkruntime.Nakama, userId: string, key: string): any {
    var found = nk.storageRead([{ collection: COL_IDEMPOTENCY, key: key, userId: userId }]);
    return found.length > 0 ? found[0].value.result : null;
}

/**
 * Applies storage writes and one wallet changeset atomically with the idempotency record (version "*": exists only
 * once). If the same request already went through, its stored result is returned and nothing is applied again.
 */
function commit(nk: nkruntime.Nakama, userId: string, key: string, result: any, writes: nkruntime.StorageWriteRequest[],
                changeset: { [currency: string]: number } | null, reason: string, refId: string): any {
    var record: nkruntime.StorageWriteRequest = {
        collection: COL_IDEMPOTENCY, key: key, userId: userId, value: { result: result, at: Date.now() },
        version: "*", permissionRead: READ_NONE, permissionWrite: WRITE_NONE,
    };
    var wallet: nkruntime.WalletUpdate[] | null = null;
    if (changeset && Object.keys(changeset).length > 0) {
        var have = balance(nk, userId);
        var whole: { [currency: string]: number } = {};
        for (var c in changeset) {
            // Numbers read back from storage are floats in goja; Nakama only takes whole (int64) amounts.
            var amount = Math.round(changeset[c]);
            if (amount !== changeset[c]) throw new Error("amount must be whole: " + c);
            if (amount < 0 && (have[c] || 0) + amount < 0) throw new Error("not enough " + c);
            whole[c] = amount;
        }
        wallet = [{ userId: userId, changeset: whole, metadata: { reason: reason, ref_id: refId, request_id: key } }];
    }
    try {
        // Nakama also refuses a negative balance here, so a parallel spend cannot slip past the check above.
        nk.multiUpdate(null, writes.concat([record]), null, wallet, true);
    } catch (e) {
        var prior = priorResult(nk, userId, key);
        if (prior !== null) return prior;
        if (/version|negative/i.test(String(e))) throw new Error("economy busy, try again");
        throw e;
    }
    return result;
}

function balance(nk: nkruntime.Nakama, userId: string): { [currency: string]: number } {
    return nk.accountGetId(userId).wallet || {};
}

// ---- crates -------------------------------------------------------------------------------------------------

function rarityIndex(name: string): number { return RARITY_NAMES.indexOf(name); }

/** Rarity at or above floor from roll in [0,1) (C# CrateRoller.RollRarity). */
function rollRarity(weights: number[], roll: number, floor: number): number {
    var total = 0;
    for (var i = floor; i < weights.length; i++) total += weights[i];
    if (total <= 0) return floor;
    var target = roll * total;
    var cumulative = 0;
    var last = floor;
    for (var j = floor; j < weights.length; j++) {
        if (weights[j] <= 0) continue;
        last = j;
        cumulative += weights[j];
        if (target < cumulative) return j;
    }
    return last;
}

/** Port of C# CrateOpening.Open: pity floors, 10x guarantee, rarity, pool index. Updates misses in place. */
function crateRolls(crate: CrateConfig, poolSizes: number[], misses: number[], count: number, roll: () => number): CratePickResult[] {
    var rules = crate.pity || [];
    if (misses.length !== rules.length) throw new Error("one miss counter per pity rule");
    var ten = rarityIndex(crate.tenGuarantee || "");
    var hasTen = ten >= 0 && count === 10;
    var tenMet = false;
    var picks: CratePickResult[] = [];
    for (var i = 0; i < count; i++) {
        var floor = 0;
        for (var k = 0; k < rules.length; k++) {
            var min = rarityIndex(rules[k].minRarity);
            if (misses[k] + 1 >= rules[k].count && min > floor) floor = min;
        }
        if (hasTen && !tenMet && i === count - 1 && ten > floor) floor = ten;
        var rarity = rollRarity(crate.weights, roll(), floor);
        if (hasTen && rarity >= ten) tenMet = true;
        for (var r = 0; r < rules.length; r++) misses[r] = rarity >= rarityIndex(rules[r].minRarity) ? 0 : misses[r] + 1;
        var size = poolSizes[rarity];
        if (!(size > 0)) throw new Error(crate.id + ": no items for rarity " + rarity);
        picks.push({ rarity: rarity, index: Math.min(size - 1, Math.floor(roll() * size)) });
    }
    return picks;
}

/** docs/ECONOMY.md §5.3: first 52 bits of HMAC-SHA256(secret, user:crate:counter) as [0,1). */
function hmacRoll(nk: nkruntime.Nakama, secret: string, userId: string, crateId: string, counter: number): number {
    var bytes = new Uint8Array(nk.hmacSha256Hash(userId + ":" + crateId + ":" + counter, secret));
    var value = 0;
    for (var i = 0; i < 6; i++) value = value * 256 + bytes[i];
    value = value * 16 + (bytes[6] >> 4);
    return value / 4503599627370496; // 2^52
}

function crateSecret(ctx: nkruntime.Context): { secret: string; version: string } {
    var secret = ctx.env && ctx.env["CRATE_SECRET"];
    if (!secret || secret.length < 16) throw new Error("server misconfigured: CRATE_SECRET missing");
    return { secret: secret, version: ctx.env["CRATE_SECRET_VERSION"] || "1" };
}

function pools(catalog: CatalogEntry[]): CatalogEntry[][] {
    var out: CatalogEntry[][] = [];
    for (var r = 0; r < RARITY_COUNT; r++) out.push([]);
    for (var i = 0; i < catalog.length; i++) out[catalog[i].rarity].push(catalog[i]);
    return out;
}

function openCrate(ctx: nkruntime.Context, nk: nkruntime.Nakama, userId: string, crateId: string, count: number, useKey: boolean, requestId: string): any {
    var key = requestKey("crate", requestId);
    var prior = priorResult(nk, userId, key);
    if (prior !== null) return prior;
    if (count !== 1 && count !== 10) throw new Error("count must be 1 or 10");
    if (useKey && count !== 1) throw new Error("a key opens one crate");

    var crate = findCrate(nk, crateId);
    var rng = crateSecret(ctx);
    var catalog = catalogItems(nk);
    var byRarity = pools(catalog);
    var sizes = byRarity.map(function (p) { return p.length; });
    var state = readInventory(nk, userId);
    var inv = state.inv;
    var misses = (inv.pity[crate.id] || []).slice();
    while (misses.length < (crate.pity || []).length) misses.push(0);

    var change: { [currency: string]: number } = {};
    var cost = count === 10 && crate.price10 > 0 ? crate.price10 : crate.price * count;
    var currency = crate.currency === "gem" ? "gems" : "coins";
    if (useKey) change["key_" + crate.id] = -1;
    else change[currency] = -cost;

    var counterStart = inv.rolls;
    var picks = crateRolls(crate, sizes, misses, count, function () { return hmacRoll(nk, rng.secret, userId, crate.id, inv.rolls++); });
    var drops: any[] = [];
    for (var i = 0; i < picks.length; i++) {
        var item = byRarity[picks[i].rarity][picks[i].index];
        var isNew = !owns(inv, item.id);
        var shards = 0, crystals = 0;
        if (isNew) {
            inv.owned.push(item.id);
        } else {
            shards = DUPLICATE_SHARDS[item.rarity];
            crystals = item.rarity === 4 ? MYTHIC_DUPLICATE_CRYSTALS : 0;
            change["shards"] = (change["shards"] || 0) + shards;
            if (crystals) change["crystals"] = (change["crystals"] || 0) + crystals;
        }
        drops.push({ id: item.id, rarity: item.rarity, isNew: isNew, shards: shards, crystals: crystals });
    }
    inv.pity[crate.id] = misses;

    // The idempotency record doubles as the crate_roll audit entry (docs/ECONOMY.md §5.3).
    var result = { drops: drops, crate: crate.id, count: count, secretVersion: rng.version, counter: counterStart, pity: misses };
    return commit(nk, userId, key, result, [inventoryWrite(userId, inv, state.version)], change, "crate_open", crate.id);
}

// ---- shop, equip, login ---------------------------------------------------------------------------------------

function buyItem(nk: nkruntime.Nakama, userId: string, itemId: string, currency: string, requestId: string): any {
    var key = requestKey("shop", requestId);
    var prior = priorResult(nk, userId, key);
    if (prior !== null) return prior;
    if (currency !== "coin" && currency !== "gem") throw new Error("currency must be coin or gem");
    var item: CatalogEntry | null = null;
    var catalog = catalogItems(nk);
    for (var i = 0; i < catalog.length; i++) if (catalog[i].id === itemId) item = catalog[i];
    if (!item) throw new Error("unknown item");
    var price = config(nk, "shop").value[currency][item.rarity] || 0;
    if (price <= 0) throw new Error("not for sale for " + currency);
    var state = readInventory(nk, userId);
    if (owns(state.inv, item.id)) throw new Error("already owned");
    state.inv.owned.push(item.id);
    var change: { [c: string]: number } = {};
    change[currency === "gem" ? "gems" : "coins"] = -price;
    return commit(nk, userId, key, { id: item.id, price: price, currency: currency }, [inventoryWrite(userId, state.inv, state.version)], change, "shop_buy", item.id);
}

/** Equips an owned item in its type's slot; stickers go to "Sticker:Cue"/"Sticker:Table" and may be cleared (null). */
function equip(nk: nkruntime.Nakama, userId: string, itemId: string | null, stickerSlot: string): Inventory {
    var state = readInventory(nk, userId);
    var inv = state.inv;
    var slot: string;
    if (itemId === null) {
        if (stickerSlot !== "Cue" && stickerSlot !== "Table") throw new Error("only a sticker slot can be cleared");
        slot = "Sticker:" + stickerSlot;
        delete inv.equipped[slot];
    } else {
        if (!ITEM_ID_PATTERN.test(itemId) || !owns(inv, itemId)) throw new Error("item not owned");
        var catalog = catalogItems(nk);
        var type = "";
        for (var i = 0; i < catalog.length; i++) if (catalog[i].id === itemId) type = catalog[i].type;
        if (!type) throw new Error("unknown item");
        slot = type === "Sticker" ? "Sticker:" + (stickerSlot === "Table" ? "Table" : "Cue") : type;
        inv.equipped[slot] = itemId;
    }
    nk.storageWrite([inventoryWrite(userId, inv, state.version)]);
    return inv;
}

function dayNumber(nowMs: number): number { return Math.floor((nowMs + RESET_OFFSET_MS) / DAY_MS); }

function loginState(nk: nkruntime.Nakama, userId: string): { day: number; index: number; version: string } {
    var found = nk.storageRead([{ collection: COL_LOGIN, key: "calendar", userId: userId }]);
    if (found.length === 0) return { day: -1, index: 0, version: "*" };
    return { day: found[0].value.day, index: found[0].value.index, version: found[0].version };
}

/** One claim per UTC+5 day; a missed day keeps the position in the 7-day cycle (docs/GDD.md §6.5). */
function claimLogin(nk: nkruntime.Nakama, userId: string, nowMs: number): any {
    var today = dayNumber(nowMs);
    var key = "login:" + today;   // server-made request id: one credit per day whatever the client sends
    var prior = priorResult(nk, userId, key);
    if (prior !== null) return prior;
    var state = loginState(nk, userId);
    if (state.day >= today) throw new Error("already claimed today");
    var days = config(nk, "login").value.days;
    var reward = days[state.index % days.length];
    var write: nkruntime.StorageWriteRequest = {
        collection: COL_LOGIN, key: "calendar", userId: userId, value: { day: today, index: (state.index + 1) % days.length },
        version: state.version, permissionRead: READ_OWNER, permissionWrite: WRITE_NONE,
    };
    return commit(nk, userId, key, { day: state.index, reward: reward }, [write], reward, "daily_login", String(today));
}

/** Start balance once per account (ledger reason "signup"). */
function ensureStartGrant(nk: nkruntime.Nakama, userId: string): void {
    var key = "signup:once";
    if (priorResult(nk, userId, key) !== null) return;
    commit(nk, userId, key, { granted: true }, [], config(nk, "start").value, "signup", userId);
}

function economyState(nk: nkruntime.Nakama, logger: nkruntime.Logger, userId: string, nowMs: number): any {
    var pid = ensurePlayerId(nk, logger, userId);
    ensureStartGrant(nk, userId);
    var inv = readInventory(nk, userId).inv;
    var login = loginState(nk, userId);
    var today = dayNumber(nowMs);
    return {
        pid: pid,
        wallet: balance(nk, userId),
        owned: inv.owned,
        equipped: inv.equipped,
        pity: inv.pity,
        login: { index: login.index, claimable: login.day < today, resetInMs: (today + 1) * DAY_MS - RESET_OFFSET_MS - nowMs },
    };
}

// ---- RPCs ---------------------------------------------------------------------------------------------------

function parsePayload(payload: string): any {
    try {
        var data = JSON.parse(payload || "{}");
        if (data && typeof data === "object") return data;
    } catch (e) {
        // fall through
    }
    throw new Error("invalid request");
}

function userOf(ctx: nkruntime.Context): string {
    if (!ctx.userId) throw new Error("authentication required");
    return ctx.userId;
}

var rpcEconomyState: nkruntime.RpcFunction = function (ctx, logger, nk, payload) {
    return JSON.stringify(economyState(nk, logger, userOf(ctx), Date.now()));
};

var rpcConfigGet: nkruntime.RpcFunction = function (ctx, logger, nk, payload) {
    userOf(ctx);
    var name = String(parsePayload(payload).name || "");
    if (["catalog", "crates", "shop", "login"].indexOf(name) < 0) throw new Error("unknown config");
    var c = config(nk, name);
    return JSON.stringify({ name: name, version: c.version, value: c.value });
};

var rpcCrateOpen: nkruntime.RpcFunction = function (ctx, logger, nk, payload) {
    var data = parsePayload(payload);
    var userId = userOf(ctx);
    return JSON.stringify(openCrate(ctx, nk, userId, String(data.crate_id || ""), Number(data.count), data.use_key === true, data.request_id));
};

var rpcShopBuy: nkruntime.RpcFunction = function (ctx, logger, nk, payload) {
    var data = parsePayload(payload);
    return JSON.stringify(buyItem(nk, userOf(ctx), String(data.item_id || ""), String(data.currency || ""), data.request_id));
};

var rpcEquip: nkruntime.RpcFunction = function (ctx, logger, nk, payload) {
    var data = parsePayload(payload);
    var itemId = data.item_id === null ? null : String(data.item_id || "");
    var inv = equip(nk, userOf(ctx), itemId, String(data.slot || ""));
    return JSON.stringify({ equipped: inv.equipped });
};

var rpcDailyClaim: nkruntime.RpcFunction = function (ctx, logger, nk, payload) {
    return JSON.stringify(claimLogin(nk, userOf(ctx), Date.now()));
};

var rpcFriendAddByPid: nkruntime.RpcFunction = function (ctx, logger, nk, payload) {
    var userId = userOf(ctx);
    var other = userByPlayerId(nk, String(parsePayload(payload).pid || ""));
    if (other === userId) throw new Error("that is your own id");
    nk.friendsAdd(userId, ctx.username || "", [other], [], {});
    return JSON.stringify({ added: true });
};

var afterAuthenticateDevice: nkruntime.AfterHookFunction<nkruntime.Session, nkruntime.AuthenticateDeviceRequest> = function (ctx, logger, nk, data, request) {
    if (!data.created || !ctx.userId) return;
    try {
        ensurePlayerId(nk, logger, ctx.userId);
        ensureStartGrant(nk, ctx.userId);
    } catch (e) {
        // economy_state retries both lazily; sign-in must not fail because of them.
        logger.error("new account setup failed for %s: %s", ctx.userId, String(e));
    }
};
