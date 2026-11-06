// Wire format shared with the Unity client (BilliardGame.Online.OnlineProtocol). Field names are short
// because frames dominate the payload; keep them in sync with the C# DTOs.

var OP_SNAPSHOT = 1;   // server -> client: full match snapshot (optionally with shot playback)
var OP_SHOT = 2;       // client -> server: ShotParams (+ cue ball placement when in hand)
var OP_FORFEIT = 3;    // client -> server: leave the game, opponent wins
var OP_REJECTED = 4;   // server -> client: last shot was refused
var OP_AVATAR = 5;     // client -> server -> opponent: cosmetic character cue (aim stance, chalk); never read by the sim

var PHASE_WAITING = 0;
var PHASE_PLAYING = 1;
var PHASE_OVER = 2;

var END_NONE = 0;
var END_EIGHT_BALL = 1;
var END_FORFEIT = 2;
var END_DISCONNECT = 3;

/** Positions travel as integers of 0.1 mm. */
var POSITION_SCALE = 10000;

/** Every 4th 240 Hz step = 60 Hz playback frames. */
var FRAME_SAMPLE_EVERY = 4;

interface ShotRequest {
    dx: number;
    dy: number;
    speed: number;
    side: number;
    top: number;
    place: boolean;
    cx: number;
    cy: number;
}

function encodeBalls(balls: Ball[]): object[] {
    var out: object[] = [];
    for (var i = 0; i < balls.length; i++) {
        var b = balls[i];
        out.push({ id: b.id, x: b.x, y: b.y, pk: b.pocketed ? b.pocket : -1 });
    }
    return out;
}

function encodeGame(state: GameState): object {
    return {
        balls: encodeBalls(state.balls),
        cur: state.current,
        g0: state.g0,
        g1: state.g1,
        brk: state.breakShot,
        bih: state.inHand,
        kit: state.kitchen,
        win: state.winner,
    };
}

function encodeOutcome(o: Outcome): object {
    return { foul: o.foul, first: o.firstHit, pocketed: o.pocketed, ga: o.groupsAssigned, tp: o.turnPassed, rs: o.respotted };
}

/** Frames as delta-coded integers (frame 0 absolute), 2 values per ball in ball order. */
function encodePlayback(result: SimResult, seq: number, speed: number): object {
    var values: number[] = [];
    var steps: number[] = [];
    var previous: number[] = [];
    for (var f = 0; f < result.frames.length; f++) {
        var frame = result.frames[f];
        steps.push(frame.step);
        for (var i = 0; i < frame.balls.length; i++) {
            var qx = Math.round(frame.balls[i].x * POSITION_SCALE);
            var qy = Math.round(frame.balls[i].y * POSITION_SCALE);
            var k = 2 * i;
            values.push(f === 0 ? qx : qx - previous[k]);
            values.push(f === 0 ? qy : qy - previous[k + 1]);
            previous[k] = qx;
            previous[k + 1] = qy;
        }
    }

    var events: object[] = [];
    for (var e = 0; e < result.events.length; e++) {
        var ev = result.events[e];
        events.push({ t: ev.time, ty: ev.type, b: ev.ball, o: ev.other, c: ev.cushion, p: ev.pocket, s: ev.speed });
    }

    return { seq: seq, dt: PHYS.timeStep, fs: steps, f: values, ev: events, dur: result.duration, speed: speed };
}

var AVATAR_KINDS = ["aim", "chalk"];
var CHARACTER_ID_PATTERN = /^[a-z0-9_-]{1,32}$/;

interface AvatarEvent {
    k: string;
    x: number;
    y: number;
}

/** Validates a cosmetic character event: known kind, aim direction within the unit square. Null when malformed. */
function parseAvatarEvent(raw: string): AvatarEvent | null {
    var data: any;
    try {
        data = JSON.parse(raw);
    } catch (e) {
        return null;
    }

    if (!data || typeof data !== "object" || AVATAR_KINDS.indexOf(data.k) < 0) return null;
    var x = typeof data.x === "number" && isFinite(data.x) ? data.x : 0;
    var y = typeof data.y === "number" && isFinite(data.y) ? data.y : 0;
    if (Math.abs(x) > 1.01 || Math.abs(y) > 1.01) return null;
    return { k: data.k, x: x, y: y };
}

/** Character id from join metadata; empty when absent or not a plain id. */
function parseCharacterId(metadata: { [key: string]: any } | null | undefined): string {
    var ch = metadata && typeof metadata.ch === "string" ? metadata.ch : "";
    return CHARACTER_ID_PATTERN.test(ch) ? ch : "";
}

/** Parses and range-checks a client shot. Returns null for anything malformed. */
function parseShotRequest(raw: string): ShotRequest | null {
    var data: any;
    try {
        data = JSON.parse(raw);
    } catch (e) {
        return null;
    }

    if (!data || typeof data !== "object") return null;
    var nums = ["dx", "dy", "speed", "side", "top"];
    for (var i = 0; i < nums.length; i++) {
        if (typeof data[nums[i]] !== "number" || !isFinite(data[nums[i]])) return null;
    }

    var place = data.place === true;
    if (place && (typeof data.cx !== "number" || typeof data.cy !== "number")) return null;
    return { dx: data.dx, dy: data.dy, speed: data.speed, side: data.side, top: data.top, place: place, cx: place ? data.cx : 0, cy: place ? data.cy : 0 };
}
