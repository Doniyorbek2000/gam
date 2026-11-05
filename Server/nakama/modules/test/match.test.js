// Match handler flow with fake Nakama APIs: join, turn order, shot broadcast, shot clock, disconnect, forfeit, rooms.
const test = require("node:test");
const assert = require("node:assert/strict");
const loadModule = require("./load");

const m = loadModule();
const TICKS_PER_SECOND = m.TICK_RATE;
const logger = { debug() {}, info() {}, warn() {}, error() {} };

function fakeNakama(names) {
    return {
        usersGetId: (ids) => ids.map((id) => ({ userId: id, username: "u_" + id, displayName: names[id] || "" })),
        binaryToString: (data) => data,
    };
}

function fakeDispatcher() {
    const sent = [];
    return {
        sent,
        broadcastMessage(op, data, presences) { sent.push({ op, data: JSON.parse(data), to: presences ? presences.map((p) => p.userId) : null }); },
        matchLabelUpdate(label) { this.label = label; },
        lastSnapshot() { return sent.filter((s) => s.op === m.OP_SNAPSHOT).pop().data; },
    };
}

const presence = (id, session) => ({ userId: id, sessionId: session || "s_" + id, username: "u_" + id, node: "n" });

function startedMatch() {
    const nk = fakeNakama({ a: "Alice", b: "  Bob<script>  " });
    const d = fakeDispatcher();
    let { state } = m.matchInit({}, logger, nk, { users: "a,b" });
    for (const id of ["a", "b"]) {
        assert.equal(m.matchJoinAttempt({}, logger, nk, d, 1, state, presence(id), {}).accept, true);
        state = m.matchJoin({}, logger, nk, d, 1, state, [presence(id)]).state;
    }
    return { nk, d, state };
}

const shotMessage = (id, body) => ({ sender: presence(id), opCode: m.OP_SHOT, data: JSON.stringify(body), reliable: true, persistence: false, status: "", receiveTimeMs: 0 });

test("two invited players start a game; strangers are refused", () => {
    const { nk, d, state } = startedMatch();
    const snap = d.lastSnapshot();
    assert.equal(snap.phase, m.PHASE_PLAYING);
    assert.deepEqual(snap.players.map((p) => p.name), ["Alice", "Bobscript"]);
    assert.equal(snap.game.brk, true);
    assert.equal(snap.turnMs, 30000);
    assert.equal(m.matchJoinAttempt({}, logger, nk, d, 2, state, presence("c"), {}).accept, false);
    assert.equal(m.matchJoinAttempt({}, logger, nk, d, 2, state, presence("a", "s2"), {}).accept, true, "reconnect allowed");
});

test("only the current player may shoot; a valid shot broadcasts frames, events and the new state", () => {
    const { nk, d, state } = startedMatch();
    const shooter = state.game.current === 0 ? "a" : "b";
    const other = shooter === "a" ? "b" : "a";

    m.matchLoop({}, logger, nk, d, 5, state, [shotMessage(other, { dx: 1, dy: 0, speed: 9, side: 0, top: 0 })]);
    assert.equal(d.sent.pop().op, m.OP_REJECTED);
    m.matchLoop({}, logger, nk, d, 5, state, [shotMessage(shooter, { dx: 1, dy: 0, speed: 99, side: 0, top: 0 })]);
    assert.equal(d.sent.pop().op, m.OP_REJECTED, "speed out of range");
    m.matchLoop({}, logger, nk, d, 5, state, [shotMessage(shooter, { dx: "x" })]);
    assert.equal(d.sent.pop().op, m.OP_REJECTED, "malformed");

    m.matchLoop({}, logger, nk, d, 6, state, [shotMessage(shooter, { dx: 1, dy: 0.004, speed: 10, side: 0, top: 0.1 })]);
    const snap = d.lastSnapshot();
    assert.equal(snap.hasShot, true);
    assert.equal(snap.shot.seq, 1);
    assert.ok(snap.shot.ev.length > 10, "a break produces many events");

    // Decode delta-coded frames: the last frame must equal the final state (quantised to 0.1 mm).
    const balls = snap.game.balls.length;
    const frameCount = snap.shot.fs.length;
    assert.equal(snap.shot.f.length, frameCount * balls * 2);
    const sum = new Array(balls * 2).fill(0);
    for (let f = 0; f < frameCount; f++) for (let k = 0; k < balls * 2; k++) sum[k] += snap.shot.f[f * balls * 2 + k];
    snap.game.balls.forEach((b, i) => {
        if (b.pk >= 0) return;
        assert.ok(Math.abs(sum[2 * i] / 10000 - b.x) <= 0.00005 + 1e-12, "x of ball " + b.id);
        assert.ok(Math.abs(sum[2 * i + 1] / 10000 - b.y) <= 0.00005 + 1e-12, "y of ball " + b.id);
    });
    assert.ok(snap.turnMs > 30000, "shot clock starts after the replay");

    // Too early: the replay is still running on the clients.
    const next = snap.game.cur === 0 ? "a" : "b";
    m.matchLoop({}, logger, nk, d, 7, state, [shotMessage(next, { dx: -1, dy: 0, speed: 3, side: 0, top: 0, place: true, cx: -0.6, cy: 0 })]);
    assert.equal(d.sent.pop().op, m.OP_REJECTED);
});

test("shot clock expiry passes the turn with ball in hand", () => {
    const { nk, d, state } = startedMatch();
    // Make it a regular (non-break) shot so the timeout grants ball in hand.
    state.game = m.makeState(state.game.balls, 0, 0, 0, false, false, false, -1);
    m.matchLoop({}, logger, nk, d, 1 + 30 * TICKS_PER_SECOND, state, []);
    const snap = d.lastSnapshot();
    assert.equal(snap.game.cur, 1);
    assert.equal(snap.game.bih, true);
    assert.equal(snap.last.foul, m.FOUL_TIMEOUT);
});

test("ball in hand needs a legal placement", () => {
    const { nk, d, state } = startedMatch();
    state.game = m.makeState(state.game.balls, 0, 0, 0, false, true, false, -1);
    m.matchLoop({}, logger, nk, d, 5, state, [shotMessage("a", { dx: 1, dy: 0, speed: 3, side: 0, top: 0 })]);
    assert.equal(d.sent.pop().data.reason, "place the cue ball");
    const apex = state.game.balls[1];
    m.matchLoop({}, logger, nk, d, 5, state, [shotMessage("a", { dx: 1, dy: 0, speed: 3, side: 0, top: 0, place: true, cx: apex.x, cy: apex.y })]);
    assert.equal(d.sent.pop().op, m.OP_REJECTED, "overlapping a ball");
    m.matchLoop({}, logger, nk, d, 5, state, [shotMessage("a", { dx: 1, dy: 0, speed: 3, side: 0, top: 0, place: true, cx: -0.9, cy: 0.3 })]);
    assert.equal(d.lastSnapshot().hasShot, true);
});

test("a player who stays disconnected past the grace period loses", () => {
    const { nk, d, state } = startedMatch();
    m.matchLeave({}, logger, nk, d, 10, state, [presence("b")]);
    assert.equal(d.lastSnapshot().players[1].online, false);
    m.matchLoop({}, logger, nk, d, 10 + 29 * TICKS_PER_SECOND, state, []);
    assert.equal(state.phase, m.PHASE_PLAYING, "still inside the grace period");

    // Reconnect and drop again restarts the grace timer.
    m.matchJoin({}, logger, nk, d, 300, state, [presence("b", "s2")]);
    m.matchLeave({}, logger, nk, d, 301, state, [presence("b", "old-session")]);
    assert.equal(state.players[1].presence.sessionId, "s2", "stale session leave ignored");
    m.matchLeave({}, logger, nk, d, 301, state, [presence("b", "s2")]);
    m.matchLoop({}, logger, nk, d, 301 + 30 * TICKS_PER_SECOND, state, []);
    const snap = d.lastSnapshot();
    assert.equal(snap.phase, m.PHASE_OVER);
    assert.equal(snap.reason, m.END_DISCONNECT);
    assert.equal(snap.game.win, 0);
});

test("forfeit hands the win to the opponent", () => {
    const { nk, d, state } = startedMatch();
    m.matchLoop({}, logger, nk, d, 3, state, [{ sender: presence("a"), opCode: m.OP_FORFEIT, data: "", reliable: true }]);
    const snap = d.lastSnapshot();
    assert.equal(snap.reason, m.END_FORFEIT);
    assert.equal(snap.game.win, 1);
});

test("room codes: create, find by code, refuse bad codes", () => {
    const created = [];
    const nk = {
        matchList: (limit, auth, label, min, max, query) => created.filter((c) => query === "+label.code:" + c.code).map((c) => ({ matchId: c.id, size: 1, label: "", authoritative: true })),
        matchCreate: (module, params) => { created.push({ id: "match-" + params.code, code: params.code }); return "match-" + params.code; },
    };
    const room = JSON.parse(m.rpcCreateRoom({ userId: "a" }, logger, nk, ""));
    assert.match(room.code, /^[1-9]\d{5}$/);
    assert.equal(JSON.parse(m.rpcJoinRoom({ userId: "b" }, logger, nk, JSON.stringify({ code: room.code }))).matchId, room.matchId);
    assert.throws(() => m.rpcJoinRoom({ userId: "b" }, logger, nk, JSON.stringify({ code: "12" })), /6 digits/);
    assert.throws(() => m.rpcJoinRoom({ userId: "b" }, logger, nk, JSON.stringify({ code: "999999" === room.code ? "888888" : "999999" })), /not found/);
    assert.throws(() => m.rpcCreateRoom({}, logger, nk, ""), /authentication/);
});
