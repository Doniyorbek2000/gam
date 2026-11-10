// Fake Nakama for the economy/LiveOps tests: in-memory storage with Nakama's version rules ("*" = create only, else
// must match), wallets with a ledger, atomic multiUpdate, HMAC like nk.hmacSha256Hash.
const crypto = require("node:crypto");

const json = (v) => JSON.parse(JSON.stringify(v));

/** In-memory storage with Nakama's version rules ("*" = create only, else must match) and atomic multiUpdate. */
module.exports = function fakeNakama() {
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
};
