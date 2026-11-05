// Port of BilliardGame.Physics (C#). Every floating-point expression keeps the C# operation order so both
// sides produce bit-identical results; test/crosscheck.test.js holds the C# reference fixtures.
// Balls are plain objects. The simulator copies its input and mutates only its private copies.

interface Ball {
    id: number;
    x: number;
    y: number;
    vx: number;
    vy: number;
    wx: number;
    wy: number;
    wz: number;
    pocketed: boolean;
    pocket: number; // -1 when on the table
}

interface Shot {
    dx: number; // unit aim direction
    dy: number;
    speed: number;
    side: number;
    top: number;
    cueBallId: number;
}

interface Cushion {
    sx: number;
    sy: number;
    ex: number;
    ey: number;
    length: number;
    dirX: number;
    dirY: number;
    nx: number;
    ny: number;
}

interface PocketHole {
    x: number;
    y: number;
    radius: number;
}

interface Table {
    length: number;
    width: number;
    halfLength: number;
    halfWidth: number;
    cushions: Cushion[];
    pockets: PocketHole[];
}

var EVENT_BALL = 0;
var EVENT_CUSHION = 1;
var EVENT_POCKET = 2;

interface SimEvent {
    type: number;
    time: number;
    ball: number;
    other: number;
    cushion: number;
    pocket: number;
    speed: number;
}

interface SimFrame {
    step: number;
    time: number;
    balls: Ball[];
}

interface SimResult {
    frames: SimFrame[]; // sampled every `sampleEvery` steps plus the final frame
    events: SimEvent[];
    endedAtRest: boolean;
    duration: number;
    finalBalls: Ball[];
}

var PHYS = {
    ballRadius: 0.028575,
    ballMass: 0.17,
    gravity: 9.81,
    slidingFriction: 0.2,
    rollingFriction: 0.01,
    spinDeceleration: 10.0,
    ballRestitution: 0.95,
    ballFriction: 0.05,
    cushionRestitution: 0.8,
    cushionFriction: 0.2,
    cushionHeight: 0.0366,
    timeStep: 1.0 / 240.0,
    maxSimulationTime: 60.0,
    maxEventsPerStep: 256,
};

var SHOT_MAX_SPEED = 12.0;
var SHOT_MAX_TIP_OFFSET = 0.5;
var SLIP_EPSILON_ROLL = 1e-9;
var SLIP_EPSILON_COLLISION = 1e-12;
var MIN_APPROACH_SPEED = 1e-6;
var NO_HIT = Infinity;

function ballInertia(): number {
    return 0.4 * PHYS.ballMass * PHYS.ballRadius * PHYS.ballRadius;
}

function atRest(id: number, x: number, y: number): Ball {
    return { id: id, x: x, y: y, vx: 0, vy: 0, wx: 0, wy: 0, wz: 0, pocketed: false, pocket: -1 };
}

function copyBall(b: Ball): Ball {
    return { id: b.id, x: b.x, y: b.y, vx: b.vx, vy: b.vy, wx: b.wx, wy: b.wy, wz: b.wz, pocketed: b.pocketed, pocket: b.pocket };
}

function isStationary(b: Ball): boolean {
    return b.vx * b.vx + b.vy * b.vy === 0.0 && b.wx * b.wx + b.wy * b.wy + b.wz * b.wz === 0.0;
}

/** Validates a client shot (trust boundary) and normalises its direction, like the C# ShotParams constructor. */
function makeShot(dx: number, dy: number, speed: number, side: number, top: number, cueBallId: number): Shot {
    if (!isFinite(dx) || !isFinite(dy) || dx * dx + dy * dy < 1e-24) throw new Error("Direction must be a finite, non-zero vector.");
    if (!isFinite(speed) || speed <= 0.0 || speed > SHOT_MAX_SPEED) throw new Error("Speed out of range.");
    if (!isFinite(side) || !isFinite(top) || side * side + top * top > SHOT_MAX_TIP_OFFSET * SHOT_MAX_TIP_OFFSET) {
        throw new Error("Tip offset out of range.");
    }

    var length = Math.sqrt(dx * dx + dy * dy);
    return { dx: dx / length, dy: dy / length, speed: speed, side: side, top: top, cueBallId: cueBallId };
}

// ---- Table ------------------------------------------------------------------------------------------------

function makeCushion(sx: number, sy: number, ex: number, ey: number, inX: number, inY: number): Cushion {
    var spanX = ex - sx;
    var spanY = ey - sy;
    var length = Math.sqrt(spanX * spanX + spanY * spanY);
    var nLen = Math.sqrt(inX * inX + inY * inY);
    return { sx: sx, sy: sy, ex: ex, ey: ey, length: length, dirX: spanX / length, dirY: spanY / length, nx: inX / nLen, ny: inY / nLen };
}

function createStandardTable(): Table {
    var length = 2.54;
    var width = 1.27;
    var cornerGap = 0.08;
    var sideHalfGap = 0.065;
    var cornerR = 0.06;
    var sideR = 0.045;
    var sideOffset = 0.02;
    var hl = length * 0.5;
    var hw = width * 0.5;

    // C# negates unit vectors (-up, -right), which yields signed zeros; keep them.
    var cushions = [
        makeCushion(-hl + cornerGap, -hw, -sideHalfGap, -hw, 0.0, 1.0),
        makeCushion(sideHalfGap, -hw, hl - cornerGap, -hw, 0.0, 1.0),
        makeCushion(hl, -hw + cornerGap, hl, hw - cornerGap, -1.0, -0.0),
        makeCushion(hl - cornerGap, hw, sideHalfGap, hw, -0.0, -1.0),
        makeCushion(-sideHalfGap, hw, -hl + cornerGap, hw, -0.0, -1.0),
        makeCushion(-hl, hw - cornerGap, -hl, -hw + cornerGap, 1.0, 0.0),
    ];
    var pockets = [
        { x: -hl, y: -hw, radius: cornerR },
        { x: 0.0, y: -hw - sideOffset, radius: sideR },
        { x: hl, y: -hw, radius: cornerR },
        { x: hl, y: hw, radius: cornerR },
        { x: 0.0, y: hw + sideOffset, radius: sideR },
        { x: -hl, y: hw, radius: cornerR },
    ];
    return { length: length, width: width, halfLength: hl, halfWidth: hw, cushions: cushions, pockets: pockets };
}

// ---- Ball motion ------------------------------------------------------------------------------------------

function applyCueStrike(b: Ball, shot: Shot): void {
    var leftX = -shot.dy;
    var leftY = shot.dx;
    var k = 2.5 * shot.speed / PHYS.ballRadius;
    b.vx = shot.dx * shot.speed;
    b.vy = shot.dy * shot.speed;
    b.wx = k * shot.top * leftX;
    b.wy = k * shot.top * leftY;
    b.wz = k * shot.side;
}

function decayTowardZero(value: number, amount: number): number {
    if (Math.abs(value) <= amount) return 0.0;
    return value - (value > 0 ? 1 : -1) * amount;
}

function applyFriction(b: Ball, dt: number): void {
    if (b.pocketed || dt <= 0.0 || isStationary(b)) return;

    var r = PHYS.ballRadius;
    var vx = b.vx;
    var vy = b.vy;
    var wx = b.wx;
    var wy = b.wy;
    var wz = decayTowardZero(b.wz, PHYS.spinDeceleration * dt);
    var remaining = dt;

    var slipX = b.vx - r * b.wy;
    var slipY = b.vy + r * b.wx;
    var slipSpeed = Math.sqrt(slipX * slipX + slipY * slipY);
    if (slipSpeed > SLIP_EPSILON_ROLL) {
        var slideDecel = PHYS.slidingFriction * PHYS.gravity;
        var timeToRoll = 2.0 * slipSpeed / (7.0 * slideDecel);
        var t = Math.min(remaining, timeToRoll);
        var ux = slipX / slipSpeed;
        var uy = slipY / slipSpeed;
        var dv = slideDecel * t;
        vx = vx - ux * dv;
        vy = vy - uy * dv;
        var angularDelta = 2.5 * slideDecel * t / r;
        wx -= angularDelta * uy;
        wy += angularDelta * ux;
        remaining -= t;

        if (remaining <= 0.0) {
            b.vx = vx; b.vy = vy; b.wx = wx; b.wy = wy; b.wz = wz;
            return;
        }
    }

    var speed = Math.sqrt(vx * vx + vy * vy);
    var speedLoss = PHYS.rollingFriction * PHYS.gravity * remaining;
    if (speed <= speedLoss) {
        vx = 0.0;
        vy = 0.0;
    } else {
        var scale = 1.0 - speedLoss / speed;
        vx = vx * scale;
        vy = vy * scale;
    }

    b.vx = vx; b.vy = vy; b.wx = -vy / r; b.wy = vx / r; b.wz = wz;
}

// ---- Collision detection ----------------------------------------------------------------------------------

function sweepCircle(px: number, py: number, vx: number, vy: number, tx: number, ty: number, radius: number, maxTime: number): number {
    var dx = px - tx;
    var dy = py - ty;
    var a = vx * vx + vy * vy;
    var halfB = dx * vx + dy * vy;
    if (a <= 0.0 || halfB >= 0.0) return NO_HIT;

    var c = (dx * dx + dy * dy) - radius * radius;
    var t: number;
    if (c <= 0.0) {
        t = 0.0;
    } else {
        var discriminant = halfB * halfB - a * c;
        if (discriminant < 0.0) return NO_HIT;
        t = (-halfB - Math.sqrt(discriminant)) / a;
    }

    if (t > maxTime) return NO_HIT;

    var cx = dx + vx * t;
    var cy = dy + vy * t;
    var approach = -(vx * cx + vy * cy) / Math.sqrt(cx * cx + cy * cy);
    return approach > MIN_APPROACH_SPEED ? t : NO_HIT;
}

function sweepSegment(px: number, py: number, vx: number, vy: number, s: Cushion, radius: number, maxTime: number): number {
    var vn = vx * s.nx + vy * s.ny;
    if (vn > -MIN_APPROACH_SPEED) return NO_HIT;

    var distance = (px - s.sx) * s.nx + (py - s.sy) * s.ny;
    if (distance < 0.0) return NO_HIT;

    var t = Math.max(0.0, (distance - radius) / -vn);
    if (t > maxTime) return NO_HIT;

    var along = ((px + vx * t) - s.sx) * s.dirX + ((py + vy * t) - s.sy) * s.dirY;
    return along >= 0.0 && along <= s.length ? t : NO_HIT;
}

function enterCircle(px: number, py: number, vx: number, vy: number, cx: number, cy: number, radius: number, maxTime: number): number {
    var dx = px - cx;
    var dy = py - cy;
    var c = (dx * dx + dy * dy) - radius * radius;
    if (c <= 0.0) return 0.0;

    var halfB = dx * vx + dy * vy;
    if (halfB >= 0.0) return NO_HIT;

    var a = vx * vx + vy * vy;
    var discriminant = halfB * halfB - a * c;
    if (discriminant < 0.0) return NO_HIT;

    var t = (-halfB - Math.sqrt(discriminant)) / a;
    return t <= maxTime ? t : NO_HIT;
}

// ---- Collision response -----------------------------------------------------------------------------------

function normalize(x: number, y: number): number[] {
    var length = Math.sqrt(x * x + y * y);
    return length > 0.0 ? [x / length, y / length] : [0.0, 0.0];
}

function resolveBallBall(a: Ball, b: Ball): void {
    var n = normalize(b.x - a.x, b.y - a.y);
    var nx = n[0];
    var ny = n[1];
    var approach = (a.vx - b.vx) * nx + (a.vy - b.vy) * ny;
    if ((nx === 0 && ny === 0) || approach <= 0.0) return;

    var m = PHYS.ballMass;
    var r = PHYS.ballRadius;
    var jn = 0.5 * m * (1.0 + PHYS.ballRestitution) * approach;
    var dn = jn / m;
    var vax = a.vx - nx * dn;
    var vay = a.vy - ny * dn;
    var vbx = b.vx + nx * dn;
    var vby = b.vy + ny * dn;
    var wza = a.wz;
    var wzb = b.wz;

    var perpX = -ny;
    var perpY = nx;
    var sa = wza * r;
    var sb = wzb * r;
    var relX = (a.vx + perpX * sa) - (b.vx - perpX * sb);
    var relY = (a.vy + perpY * sa) - (b.vy - perpY * sb);
    var relN = relX * nx + relY * ny;
    var tanX = relX - nx * relN;
    var tanY = relY - ny * relN;
    var slip = Math.sqrt(tanX * tanX + tanY * tanY);
    if (slip > SLIP_EPSILON_COLLISION) {
        var tx = tanX / slip;
        var ty = tanY / slip;
        var jt = Math.min(PHYS.ballFriction * jn, m * slip / 7.0);
        var dt = jt / m;
        vax = vax - tx * dt;
        vay = vay - ty * dt;
        vbx = vbx + tx * dt;
        vby = vby + ty * dt;
        var spinDelta = -2.5 * jt * (nx * ty - ny * tx) / (m * r);
        wza += spinDelta;
        wzb += spinDelta;
    }

    a.vx = vax; a.vy = vay; a.wz = wza;
    b.vx = vbx; b.vy = vby; b.wz = wzb;
}

function resolveBallCushion(b: Ball, normalX: number, normalY: number): void {
    var n = normalize(normalX, normalY);
    var nx = n[0];
    var ny = n[1];
    var vn = b.vx * nx + b.vy * ny;
    if ((nx === 0 && ny === 0) || vn >= 0.0) return;

    var m = PHYS.ballMass;
    var r = PHYS.ballRadius;
    var jn = m * (1.0 + PHYS.cushionRestitution) * -vn;
    var dn = jn / m;
    var vx = b.vx + nx * dn;
    var vy = b.vy + ny * dn;
    var wz = b.wz;

    var roll = jn * (PHYS.cushionHeight - r) / ballInertia();
    var wx = b.wx - roll * ny;
    var wy = b.wy + roll * nx;

    var s = wz * r;
    var cvx = b.vx - (-ny) * s;
    var cvy = b.vy - nx * s;
    var cvn = cvx * nx + cvy * ny;
    var tanX = cvx - nx * cvn;
    var tanY = cvy - ny * cvn;
    var slip = Math.sqrt(tanX * tanX + tanY * tanY);
    if (slip > SLIP_EPSILON_COLLISION) {
        var tx = tanX / slip;
        var ty = tanY / slip;
        var jt = Math.min(PHYS.cushionFriction * jn, 2.0 * m * slip / 7.0);
        var dt = jt / m;
        vx = vx - tx * dt;
        vy = vy - ty * dt;
        wz += 2.5 * jt * (nx * ty - ny * tx) / (m * r);
    }

    b.vx = vx; b.vy = vy; b.wx = wx; b.wy = wy; b.wz = wz;
}

// ---- Simulator --------------------------------------------------------------------------------------------

var CONTACT_NONE = 0;
var CONTACT_BALL = 1;
var CONTACT_CUSHION = 2;
var CONTACT_POCKET = 3;

interface Contact {
    kind: number;
    time: number;
    i: number;
    j: number;
    feature: number;
    nx: number;
    ny: number;
}

function findEarliestContact(table: Table, states: Ball[], maxTime: number): Contact {
    var r = PHYS.ballRadius;
    var best: Contact = { kind: CONTACT_NONE, time: maxTime, i: -1, j: -1, feature: -1, nx: 0, ny: 0 };

    for (var i = 0; i < states.length; i++) {
        var a = states[i];
        if (a.pocketed) continue;
        var aMoving = a.vx * a.vx + a.vy * a.vy > 0.0;

        for (var j = i + 1; j < states.length; j++) {
            var b = states[j];
            if (b.pocketed || (!aMoving && b.vx * b.vx + b.vy * b.vy === 0.0)) continue;

            var tb = sweepCircle(a.x - b.x, a.y - b.y, a.vx - b.vx, a.vy - b.vy, 0.0, 0.0, 2.0 * r, best.time);
            if (tb <= best.time) best = { kind: CONTACT_BALL, time: tb, i: i, j: j, feature: -1, nx: 0, ny: 0 };
        }

        if (!aMoving) continue;

        for (var k = 0; k < table.cushions.length; k++) {
            var s = table.cushions[k];
            var tc = sweepSegment(a.x, a.y, a.vx, a.vy, s, r, best.time);
            if (tc <= best.time) best = { kind: CONTACT_CUSHION, time: tc, i: i, j: -1, feature: k, nx: s.nx, ny: s.ny };

            best = earliestJawContact(a, i, k, s.sx, s.sy, best);
            best = earliestJawContact(a, i, k, s.ex, s.ey, best);
        }

        for (var p = 0; p < table.pockets.length; p++) {
            var pocket = table.pockets[p];
            var tp = enterCircle(a.x, a.y, a.vx, a.vy, pocket.x, pocket.y, pocket.radius, best.time);
            if (tp <= best.time) best = { kind: CONTACT_POCKET, time: tp, i: i, j: -1, feature: p, nx: 0, ny: 0 };
        }
    }

    return best;
}

function earliestJawContact(ball: Ball, i: number, k: number, jx: number, jy: number, best: Contact): Contact {
    var t = sweepCircle(ball.x, ball.y, ball.vx, ball.vy, jx, jy, PHYS.ballRadius, best.time);
    if (t > best.time) return best;

    var n = normalize((ball.x + ball.vx * t) - jx, (ball.y + ball.vy * t) - jy);
    return { kind: CONTACT_CUSHION, time: t, i: i, j: -1, feature: k, nx: n[0], ny: n[1] };
}

function resolveContact(states: Ball[], c: Contact, time: number): SimEvent {
    var a = states[c.i];
    if (c.kind === CONTACT_BALL) {
        var b = states[c.j];
        var n = normalize(b.x - a.x, b.y - a.y);
        var speed = (a.vx - b.vx) * n[0] + (a.vy - b.vy) * n[1];
        resolveBallBall(a, b);
        return { type: EVENT_BALL, time: time, ball: a.id, other: b.id, cushion: -1, pocket: -1, speed: speed };
    }

    if (c.kind === CONTACT_CUSHION) {
        var cushionSpeed = -(a.vx * c.nx + a.vy * c.ny);
        resolveBallCushion(a, c.nx, c.ny);
        return { type: EVENT_CUSHION, time: time, ball: a.id, other: -1, cushion: c.feature, pocket: -1, speed: cushionSpeed };
    }

    var pocketSpeed = Math.sqrt(a.vx * a.vx + a.vy * a.vy);
    a.vx = 0; a.vy = 0; a.wx = 0; a.wy = 0; a.wz = 0;
    a.pocketed = true;
    a.pocket = c.feature;
    return { type: EVENT_POCKET, time: time, ball: a.id, other: -1, cushion: -1, pocket: c.feature, speed: pocketSpeed };
}

function moveBalls(states: Ball[], dt: number): void {
    if (dt <= 0.0) return;
    for (var i = 0; i < states.length; i++) {
        var b = states[i];
        if (b.pocketed || b.vx * b.vx + b.vy * b.vy === 0.0) continue;
        b.x = b.x + b.vx * dt;
        b.y = b.y + b.vy * dt;
    }
}

function advance(table: Table, states: Ball[], startTime: number, dt: number, events: SimEvent[]): void {
    var elapsed = 0.0;
    for (var i = 0; i < PHYS.maxEventsPerStep; i++) {
        var contact = findEarliestContact(table, states, dt - elapsed);
        if (contact.kind === CONTACT_NONE) break;

        moveBalls(states, contact.time);
        elapsed += contact.time;
        events.push(resolveContact(states, contact, startTime + elapsed));
    }

    moveBalls(states, Math.max(0.0, dt - elapsed));
}

function allStationary(states: Ball[]): boolean {
    for (var i = 0; i < states.length; i++) if (!isStationary(states[i])) return false;
    return true;
}

function snapshot(states: Ball[]): Ball[] {
    var out: Ball[] = [];
    for (var i = 0; i < states.length; i++) out.push(copyBall(states[i]));
    return out;
}

/** Strikes the shot's cue ball and runs until everything stops. Frames are sampled every `sampleEvery` steps. */
function simulateShot(table: Table, balls: Ball[], shot: Shot, sampleEvery: number): SimResult {
    var states = snapshot(balls);
    var cueIndex = -1;
    for (var i = 0; i < states.length; i++) if (states[i].id === shot.cueBallId) cueIndex = i;
    if (cueIndex < 0) throw new Error("Cue ball " + shot.cueBallId + " is not on the table.");
    if (states[cueIndex].pocketed) throw new Error("Cue ball is pocketed and cannot be struck.");

    applyCueStrike(states[cueIndex], shot);

    var dt = PHYS.timeStep;
    var maxSteps = Math.ceil(PHYS.maxSimulationTime / dt);
    var frames: SimFrame[] = [{ step: 0, time: 0.0, balls: snapshot(states) }];
    var events: SimEvent[] = [];
    var rest = allStationary(states);
    var step = 0;
    var lastTime = 0.0;

    for (; step < maxSteps && !rest; step++) {
        var startTime = step * dt;
        for (var f = 0; f < states.length; f++) applyFriction(states[f], dt * 0.5);
        advance(table, states, startTime, dt, events);
        for (var g = 0; g < states.length; g++) applyFriction(states[g], dt * 0.5);
        lastTime = startTime + dt;
        rest = allStationary(states);
        if ((step + 1) % sampleEvery === 0 || rest || step + 1 >= maxSteps) {
            frames.push({ step: step + 1, time: lastTime, balls: snapshot(states) });
        }
    }

    return { frames: frames, events: events, endedAtRest: rest, duration: lastTime, finalBalls: snapshot(states) };
}
