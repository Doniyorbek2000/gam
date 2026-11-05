// C# vs TypeScript cross-check: same inputs must give bit-identical physics and identical rule verdicts.
// Fixtures come from Tools/CrossCheck (npm run fixtures).
const test = require("node:test");
const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const loadModule = require("./load");

const fixturePath = path.join(__dirname, ".fixtures", "fixtures.json");
if (!fs.existsSync(fixturePath)) throw new Error("Missing " + fixturePath + ": run `npm run fixtures` (needs the .NET SDK).");
const fixtures = JSON.parse(fs.readFileSync(fixturePath, "utf8"));
const m = loadModule();
const table = m.createStandardTable();

function toBall(b) {
    return { id: b.id, x: b.x, y: b.y, vx: b.vx, vy: b.vy, wx: b.wx, wy: b.wy, wz: b.wz, pocketed: b.pk >= 0, pocket: b.pk };
}

function toState(s) {
    return m.makeState(s.balls.map(toBall), s.cur, s.g0, s.g1, s.brk, s.bih, s.kit, s.win);
}

function assertBalls(actual, expected, where) {
    assert.equal(actual.length, expected.length, where + ": ball count");
    for (let i = 0; i < expected.length; i++) {
        const a = actual[i];
        const e = toBall(expected[i]);
        for (const key of ["id", "x", "y", "vx", "vy", "wx", "wy", "wz", "pocketed", "pocket"]) {
            assert.equal(a[key], e[key], `${where}: ball ${e.id}.${key} (diff ${a[key] - e[key]})`);
        }
    }
}

function assertState(actual, expected, where) {
    assertBalls(actual.balls, expected.balls, where);
    assert.deepEqual(
        [actual.current, actual.g0, actual.g1, actual.breakShot, actual.inHand, actual.kitchen, actual.winner],
        [expected.cur, expected.g0, expected.g1, expected.brk, expected.bih, expected.kit, expected.win],
        where + ": state fields");
}

test("racks match System.Random-seeded C# racks", () => {
    for (const rack of fixtures.racks) {
        assertBalls(m.createRack(table, rack.seed), rack.balls, "rack seed " + rack.seed);
    }
});

test("every shot simulates and judges identically", () => {
    let compared = 0;
    let events = 0;
    for (let n = 0; n < fixtures.shots.length; n++) {
        const fx = fixtures.shots[n];
        const where = "shot " + n;
        if (fx.timeoutOf) {
            assertState(m.resolveTimeout(toState(fx.timeoutOf)).next, fx.next, where + " timeout");
            continue;
        }

        let state = toState(fx.state);
        if (fx.place) state = m.withCueBallPlaced(state, fx.place[0], fx.place[1], table);
        const shot = m.makeShot(fx.shot.dx, fx.shot.dy, fx.shot.speed, fx.shot.side, fx.shot.top, 0);
        const result = m.simulateShot(table, state.balls, shot, fixtures.sampleEvery);

        assert.equal(result.endedAtRest, fx.endedAtRest, where + ": endedAtRest");
        assert.equal(result.duration, fx.duration, where + ": duration");
        assert.equal(result.events.length, fx.events.length, where + ": event count");
        for (let i = 0; i < fx.events.length; i++) {
            const a = result.events[i];
            const e = fx.events[i];
            assert.deepEqual([a.type, a.ball, a.other, a.cushion, a.pocket], [e.type, e.ball, e.other, e.cushion, e.pocket], `${where}: event ${i} kind`);
            assert.equal(a.time, e.time, `${where}: event ${i} time`);
            assert.equal(a.speed, e.speed, `${where}: event ${i} speed`);
        }

        assert.equal(result.frames.length, fx.frames.length, where + ": frame count");
        for (let f = 0; f < fx.frames.length; f++) {
            assert.equal(result.frames[f].step, fx.frames[f].step, `${where}: frame ${f} step`);
            assert.equal(result.frames[f].time, fx.frames[f].time, `${where}: frame ${f} time`);
            assertBalls(result.frames[f].balls, fx.frames[f].balls, `${where} frame ${f}`);
        }

        const outcome = m.resolveShot(state, result.events, result.finalBalls, table);
        assert.deepEqual(
            [outcome.foul, outcome.firstHit, Array.from(outcome.pocketed), outcome.groupsAssigned, outcome.turnPassed, outcome.respotted],
            [fx.outcome.foul, fx.outcome.firstHit, fx.outcome.pocketed, fx.outcome.groupsAssigned, fx.outcome.turnPassed, fx.outcome.respotted],
            where + ": outcome");
        assertState(outcome.next, fx.outcome.next, where + " next state");
        compared++;
        events += fx.events.length;
    }

    assert.ok(compared >= 50, "expected at least 50 simulated shots, got " + compared);
    console.log(`cross-checked ${compared} shots, ${events} events`);
});
