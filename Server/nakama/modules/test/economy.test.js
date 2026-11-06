// Server economy against a fake Nakama: Player ID, idempotent wallet changes, crates (C# cross-check + odds), shop,
// daily login, storage permissions.
const test = require("node:test");
const assert = require("node:assert/strict");
const crypto = require("node:crypto");
const fs = require("node:fs");
const path = require("node:path");
const loadModule = require("./load");

const m = loadModule();
const logger = { debug() {}, info() {}, warn() {}, error() {} };
const SECRET = "test-secret-0123456789abcdef";
const json = (v) => JSON.parse(JSON.stringify(v));

/** In-memory storage with Nakama's version rules ("*" = create only, else must match) and atomic multiUpdate. */
function fakeNakama() {
    const objects = new Map();
    const wallets = new Map();
    const ledger = [];
    let version = 0;
    const id = (o) => o.collection + "/" + o.userId + "/" + o.key;

    function checkWrite(w) {
        const existing = objects.get(id(w));
        if (w.version === "*" && existing) throw new Error("version check failed: exists");
        if (w.version && w.version !== "*" && (!existing || existing.version !== w.version)) throw new Error("version check failed: mismatch");
    }
    function applyWrite(w) {
        objects.set(id(w), { ...json(w), version: String(++version) });
    }
    function checkWallet(u) {
        const wallet = wallets.get(u.userId) || {};
        for (const [c, delta] of Object.entries(u.changeset)) {
            if ((wallet[c] || 0) + delta < 0) throw new Error("wallet update rejected negative value");
        }
    }
    function applyWallet(u) {
        const wallet = { ...(wallets.get(u.userId) || {}) };
        for (const [c, delta] of Object.entries(u.changeset)) wallet[c] = (wallet[c] || 0) + delta;
        wallets.set(u.userId, wallet);
        ledger.push(json(u));
    }

    return {
        objects, wallets, ledger,
        uuidv4: () => crypto.randomUUID(),
        hmacSha256Hash: (input, key) => {
            const b = crypto.createHmac("sha256", key).update(input).digest();
            return b.buffer.slice(b.byteOffset, b.byteOffset + b.length);
        },
        storageRead: (keys) => keys.map((k) => objects.get(id(k))).filter(Boolean).map(json),
        storageWrite(writes) {
            writes.forEach(checkWrite);
            writes.forEach(applyWrite);
            return [];
        },
        multiUpdate(accounts, writes, deletes, walletUpdates) {
            (writes || []).forEach(checkWrite);
            (walletUpdates || []).forEach(checkWallet);
            (writes || []).forEach(applyWrite);
            (walletUpdates || []).forEach(applyWallet);
            return {};
        },
        accountGetId: (userId) => ({ wallet: { ...(wallets.get(userId) || {}) } }),
        friendsAdd(userId, username, ids) { this.friends = (this.friends || []).concat(ids.map((f) => [userId, f])); },
    };
}

const ctx = (userId) => ({ userId, username: "u_" + userId, env: { CRATE_SECRET: SECRET } });
const rpc = (fn, nk, userId, body) => JSON.parse(fn(ctx(userId), logger, nk, JSON.stringify(body || {})));
let requestCounter = 0;
const rid = () => "req-" + (++requestCounter).toString().padStart(6, "0");

function newPlayer(nk, userId) {
    m.afterAuthenticateDevice(ctx(userId), logger, nk, { created: true }, {});
    return rpc(m.rpcEconomyState, nk, userId);
}

test("player ids are 10 digits, never start with 0, and the registry keeps them unique", () => {
    // 1e5 raw draws from 9e9 values: ~0.55 birthday collisions are expected, hence the create-only registry.
    const seen = new Set();
    const uuid = () => crypto.randomUUID();
    for (let i = 0; i < 100000; i++) {
        const pid = m.randomPlayerId(uuid);
        assert.match(pid, /^[1-9][0-9]{9}$/);
        seen.add(pid);
    }
    assert.ok(seen.size >= 100000 - 5, "raw collisions stay rare: " + (100000 - seen.size));

    const nk = fakeNakama();
    const pids = new Set();
    for (let i = 0; i < 20000; i++) pids.add(m.ensurePlayerId(nk, logger, "user" + i));
    assert.equal(pids.size, 20000, "allocated ids are unique");
    const a = newPlayer(nk, "a");
    assert.equal(rpc(m.rpcEconomyState, nk, "a").pid, a.pid, "stable across calls");
    assert.equal(m.userByPlayerId(nk, a.pid), "a");
});

test("a taken player id is drawn again", () => {
    const nk = fakeNakama();
    const first = newPlayer(nk, "a").pid;
    const realUuid = nk.uuidv4;
    let calls = 0;
    // The first draw reproduces a's id (digits only, so it maps 1:1), later draws are random.
    nk.uuidv4 = () => (calls++ === 0 ? first + "0000000000000000000000" : realUuid());
    const second = newPlayer(nk, "b").pid;
    assert.notEqual(second, first);
    assert.ok(calls >= 2);
});

test("sign-up grant lands once, with a ledger reason", () => {
    const nk = fakeNakama();
    newPlayer(nk, "a");
    rpc(m.rpcEconomyState, nk, "a");
    m.afterAuthenticateDevice(ctx("a"), logger, nk, { created: true }, {});
    assert.equal(JSON.stringify(nk.wallets.get("a")), JSON.stringify({ coins: 5000, gems: 500 }));
    assert.equal(nk.ledger.length, 1);
    assert.equal(nk.ledger[0].metadata.reason, "signup");
});

test("the same request_id never debits twice; a new one does", () => {
    const nk = fakeNakama();
    newPlayer(nk, "a");
    const request = rid();
    const first = rpc(m.rpcCrateOpen, nk, "a", { crate_id: "classic", count: 1, request_id: request });
    const again = rpc(m.rpcCrateOpen, nk, "a", { crate_id: "classic", count: 1, request_id: request });
    assert.equal(JSON.stringify(again), JSON.stringify(first), "replayed result");
    assert.equal(nk.wallets.get("a").coins, 2500);
    rpc(m.rpcCrateOpen, nk, "a", { crate_id: "classic", count: 1, request_id: rid() });
    assert.equal(nk.wallets.get("a").coins, 0);
    assert.throws(() => rpc(m.rpcCrateOpen, nk, "a", { crate_id: "classic", count: 1, request_id: rid() }), /not enough coins/);
    assert.throws(() => rpc(m.rpcCrateOpen, nk, "a", { crate_id: "classic", count: 1, request_id: "short" }), /request_id/);
    assert.ok(nk.ledger.every((u) => u.metadata.reason && u.metadata.request_id), "every ledger row has reason + request id");
});

test("crate rolls match the C# CrateOpening fixtures", () => {
    const fixtures = JSON.parse(fs.readFileSync(path.join(__dirname, ".fixtures", "fixtures.json"), "utf8")).crates;
    assert.ok(fixtures.length >= 20);
    const crates = m.SEED_CONFIG.crates.crates;
    for (const f of fixtures) {
        const crate = crates.find((c) => c.id === f.crate);
        const misses = f.misses.slice();
        let next = 0;
        const picks = m.crateRolls(crate, f.sizes, misses, f.count, () => f.rolls[next++]);
        assert.equal(JSON.stringify(picks.map((p) => [p.rarity, p.index])), JSON.stringify(f.picks), f.crate + " x" + f.count);
        assert.equal(JSON.stringify(misses), JSON.stringify(f.after), f.crate + " pity after");
        assert.equal(next, f.rolls.length, "same number of rolls");
    }
});

test("1e6 single rolls hit the published rarity weights within 0.1 percentage points", () => {
    const nk = fakeNakama();
    const crate = { ...m.SEED_CONFIG.crates.crates.find((c) => c.id === "premium"), pity: [] };   // raw odds, no pity
    const counts = [0, 0, 0, 0, 0];
    const total = 1000000;
    let counter = 0;
    for (let i = 0; i < total; i++) {
        const r = m.hmacRoll(nk, SECRET, "odds-user", "premium", counter++);
        counts[m.rollRarity(crate.weights, r, 0)]++;
    }
    const weightSum = crate.weights.reduce((a, b) => a + b, 0);
    crate.weights.forEach((w, i) => {
        const expected = (100 * w) / weightSum;
        const actual = (100 * counts[i]) / total;
        assert.ok(Math.abs(actual - expected) < 0.1, `rarity ${i}: ${actual.toFixed(3)}% vs ${expected}%`);
    });
});

test("pity guarantees an Epic within 15 classic opens and duplicates pay shards", () => {
    const nk = fakeNakama();
    newPlayer(nk, "a");
    nk.wallets.set("a", { coins: 2500 * 40 });
    let sinceEpic = 0;
    let shards = 0;
    for (let i = 0; i < 40; i++) {
        const r = rpc(m.rpcCrateOpen, nk, "a", { crate_id: "classic", count: 1, request_id: rid() });
        sinceEpic = r.drops[0].rarity >= 2 ? 0 : sinceEpic + 1;
        assert.ok(sinceEpic < 15, "epic pity");
        shards += r.drops[0].shards;
    }
    assert.ok(shards > 0, "40 opens over a small pool must repeat");
    assert.equal(nk.wallets.get("a").shards, shards);
});

test("ten premium opens cost 900 gems and include an Epic or better", () => {
    const nk = fakeNakama();
    newPlayer(nk, "a");
    nk.wallets.set("a", { gems: 1000 });
    const r = rpc(m.rpcCrateOpen, nk, "a", { crate_id: "premium", count: 10, request_id: rid() });
    assert.equal(r.drops.length, 10);
    assert.ok(r.drops.some((d) => d.rarity >= 2), "10x guarantee");
    assert.equal(nk.wallets.get("a").gems, 100);
    assert.throws(() => rpc(m.rpcCrateOpen, nk, "a", { crate_id: "premium", count: 3, request_id: rid() }), /count/);
});

test("a crate key opens one crate without spending currency", () => {
    const nk = fakeNakama();
    newPlayer(nk, "a");
    nk.wallets.set("a", { coins: 0, key_classic: 1 });
    rpc(m.rpcCrateOpen, nk, "a", { crate_id: "classic", count: 1, use_key: true, request_id: rid() });
    assert.equal(nk.wallets.get("a").key_classic, 0);
    assert.throws(() => rpc(m.rpcCrateOpen, nk, "a", { crate_id: "classic", count: 1, use_key: true, request_id: rid() }), /not enough key_classic/);
});

test("shop sells by rarity price, once per item; equip needs ownership", () => {
    const nk = fakeNakama();
    const state = newPlayer(nk, "a");
    assert.ok(state.owned.includes("cue_house"));
    assert.equal(state.equipped.Cue, "cue_house");
    assert.throws(() => rpc(m.rpcEquip, nk, "a", { item_id: "cue_maple" }), /not owned/);
    const bought = rpc(m.rpcShopBuy, nk, "a", { item_id: "cue_maple", currency: "coin", request_id: rid() });
    assert.equal(bought.price, 1500);
    assert.equal(nk.wallets.get("a").coins, 3500);
    assert.throws(() => rpc(m.rpcShopBuy, nk, "a", { item_id: "cue_maple", currency: "coin", request_id: rid() }), /already owned/);
    assert.throws(() => rpc(m.rpcShopBuy, nk, "a", { item_id: "cue_dragon", currency: "gem", request_id: rid() }), /not for sale/);
    assert.equal(rpc(m.rpcEquip, nk, "a", { item_id: "cue_maple" }).equipped.Cue, "cue_maple");
});

test("daily login: one claim per UTC+5 day, cycle keeps its place after a missed day", () => {
    const nk = fakeNakama();
    newPlayer(nk, "a");
    const day = 24 * 3600 * 1000;
    const t0 = Date.UTC(2026, 10, 6, 18, 59, 0);   // 23:59 in Tashkent
    assert.equal(JSON.stringify(m.claimLogin(nk, "a", t0).reward), JSON.stringify({ coins: 200 }));
    assert.equal(JSON.stringify(m.claimLogin(nk, "a", t0 + 30000)), JSON.stringify({ day: 0, reward: { coins: 200 } }), "same-day replay, no second credit");
    assert.equal(nk.wallets.get("a").coins, 5200);
    const t1 = t0 + 2 * 60 * 1000;   // 00:01 Tashkent: new day
    assert.equal(JSON.stringify(m.claimLogin(nk, "a", t1).reward), JSON.stringify({ coins: 300 }));
    assert.equal(JSON.stringify(m.claimLogin(nk, "a", t1 + 3 * day).reward), JSON.stringify({ key_classic: 1 }), "missed days keep progress");
    assert.equal(nk.wallets.get("a").key_classic, 1);
    assert.equal(nk.ledger.filter((u) => u.metadata.reason === "daily_login").length, 3);
});

test("clients can never write economy storage; secret is required", () => {
    const nk = fakeNakama();
    newPlayer(nk, "a");
    rpc(m.rpcCrateOpen, nk, "a", { crate_id: "classic", count: 1, request_id: rid() });
    m.claimLogin(nk, "a", Date.now());
    for (const o of nk.objects.values()) assert.equal(o.permissionWrite, 0, o.collection + "/" + o.key);
    const noSecret = { userId: "a", env: {} };
    assert.throws(() => m.rpcCrateOpen(noSecret, logger, nk, JSON.stringify({ crate_id: "classic", count: 1, request_id: rid() })), /CRATE_SECRET/);
});

test("friend add by player id", () => {
    const nk = fakeNakama();
    newPlayer(nk, "a");
    const b = newPlayer(nk, "b");
    rpc(m.rpcFriendAddByPid, nk, "a", { pid: b.pid });
    assert.deepEqual(nk.friends, [["a", "b"]]);
    assert.throws(() => rpc(m.rpcFriendAddByPid, nk, "a", { pid: "0123456789" }), /10 digits/);
    assert.throws(() => rpc(m.rpcFriendAddByPid, nk, "a", { pid: "1234567890" }), /not found/);
});
