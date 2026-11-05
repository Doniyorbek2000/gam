// Port of BilliardGame.Gameplay (C#): 8-ball rack, state and judging. Same evaluation order as C#.

var GROUP_NONE = 0;
var GROUP_SOLIDS = 1;
var GROUP_STRIPES = 2;

var CUE_BALL = 0;
var EIGHT_BALL = 8;
var BALL_COUNT = 16;
var NO_WINNER = -1;
var MIN_BREAK_CUSHION_BALLS = 4;
var RACK_JITTER = 0.00015;
var SPOT_SEARCH_STEP = 0.005;
var RACK_ORDER = [1, 9, 2, 10, 8, 3, 11, 4, 12, 5, 6, 13, 14, 7, 15];

// Same numbering as the C# FoulType enum.
var FOUL_NONE = 0;
var FOUL_SCRATCH = 1;
var FOUL_NO_BALL_HIT = 2;
var FOUL_WRONG_BALL_FIRST = 3;
var FOUL_NO_RAIL_AFTER_CONTACT = 4;
var FOUL_ILLEGAL_BREAK = 5;
var FOUL_TIMEOUT = 6;

interface GameState {
    balls: Ball[];
    current: number;
    g0: number;
    g1: number;
    breakShot: boolean;
    inHand: boolean;
    kitchen: boolean;
    winner: number;
}

interface Outcome {
    next: GameState;
    foul: number;
    firstHit: number;
    pocketed: number[];
    groupsAssigned: boolean;
    turnPassed: boolean;
    respotted: boolean;
}

function groupOfBall(id: number): number {
    if (id >= 1 && id <= 7) return GROUP_SOLIDS;
    if (id >= 9 && id <= 15) return GROUP_STRIPES;
    return GROUP_NONE;
}

function oppositeGroup(group: number): number {
    if (group === GROUP_SOLIDS) return GROUP_STRIPES;
    if (group === GROUP_STRIPES) return GROUP_SOLIDS;
    return GROUP_NONE;
}

function makeState(balls: Ball[], current: number, g0: number, g1: number, breakShot: boolean, inHand: boolean, kitchen: boolean, winner: number): GameState {
    return { balls: balls, current: current, g0: g0, g1: g1, breakShot: breakShot, inHand: inHand, kitchen: inHand && kitchen, winner: winner };
}

function footSpotX(table: Table): number { return table.length * 0.25; }
function headStringX(table: Table): number { return -table.length * 0.25; }

function createRack(table: Table, seed: number): Ball[] {
    var random = dotNetRandom(seed);
    var spacing = 2.0 * PHYS.ballRadius;
    var rowStep = spacing * Math.sqrt(3.0) * 0.5;
    var footX = footSpotX(table);
    var footY = 0.0;

    var balls: Ball[] = [];
    for (var n = 0; n < BALL_COUNT; n++) balls.push(atRest(n, 0, 0));
    balls[CUE_BALL] = atRest(CUE_BALL, headStringX(table), 0.0);

    var next = 0;
    for (var row = 0; row < 5; row++) {
        for (var i = 0; i <= row; i++) {
            var id = RACK_ORDER[next++];
            var jx = (dotNetNextDouble(random) * 2.0 - 1.0) * RACK_JITTER;
            var jy = (dotNetNextDouble(random) * 2.0 - 1.0) * RACK_JITTER;
            var px = footX + row * rowStep;
            var py = footY + (i - row * 0.5) * spacing;
            balls[id] = atRest(id, px + jx, py + jy);
        }
    }

    return balls;
}

function newGame(table: Table, startingPlayer: number, seed: number): GameState {
    return makeState(createRack(table, seed), startingPlayer, GROUP_NONE, GROUP_NONE, true, false, false, NO_WINNER);
}

function distance(ax: number, ay: number, bx: number, by: number): number {
    var dx = ax - bx;
    var dy = ay - by;
    return Math.sqrt(dx * dx + dy * dy);
}

function isFreeSpot(x: number, y: number, balls: Ball[], ignoreId: number, table: Table): boolean {
    var r = PHYS.ballRadius;
    if (!isFinite(x) || !isFinite(y)) return false;
    if (Math.abs(x) > table.halfLength - r || Math.abs(y) > table.halfWidth - r) return false;

    for (var p = 0; p < table.pockets.length; p++) {
        var pocket = table.pockets[p];
        if (distance(x, y, pocket.x, pocket.y) < pocket.radius + r) return false;
    }

    var minDistance = 2.0 * r;
    for (var i = 0; i < balls.length; i++) {
        var b = balls[i];
        if (b.id === ignoreId || b.pocketed) continue;
        if (distance(x, y, b.x, b.y) < minDistance) return false;
    }

    return true;
}

function findFreeSpot(startX: number, startY: number, directionX: number, balls: Ball[], ignoreId: number, table: Table): number[] {
    var first = directionX >= 0 ? 1 : -1;
    for (var pass = 0; pass < 2; pass++) {
        var sign = pass === 0 ? first : -first;
        for (var x = startX; Math.abs(x) <= table.halfLength; x += sign * SPOT_SEARCH_STEP) {
            if (isFreeSpot(x, startY, balls, ignoreId, table)) return [x, startY];
        }
    }

    throw new Error("No free spot on the long string.");
}

function groupOfPlayer(state: GameState, player: number): number {
    return player === 0 ? state.g0 : state.g1;
}

function remainingInGroup(state: GameState, group: number): number {
    if (group === GROUP_NONE) return 0;
    var count = 0;
    for (var i = 0; i < state.balls.length; i++) {
        if (!state.balls[i].pocketed && groupOfBall(state.balls[i].id) === group) count++;
    }
    return count;
}

function isOnEightBall(state: GameState, player: number): boolean {
    var group = groupOfPlayer(state, player);
    return group !== GROUP_NONE && remainingInGroup(state, group) === 0;
}

function findBallIndex(balls: Ball[], id: number): number {
    for (var i = 0; i < balls.length; i++) if (balls[i].id === id) return i;
    return -1;
}

function canPlaceCueBall(state: GameState, x: number, y: number, table: Table): boolean {
    return state.inHand &&
        (!state.kitchen || x <= headStringX(table)) &&
        isFreeSpot(x, y, state.balls, CUE_BALL, table);
}

function withCueBallPlaced(state: GameState, x: number, y: number, table: Table): GameState {
    if (!canPlaceCueBall(state, x, y, table)) throw new Error("Cue ball cannot be placed there.");
    var balls = snapshot(state.balls);
    balls[findBallIndex(balls, CUE_BALL)] = atRest(CUE_BALL, x, y);
    return makeState(balls, state.current, state.g0, state.g1, state.breakShot, false, false, state.winner);
}

function isLegalFirstHit(ballId: number, group: number, onEight: boolean): boolean {
    if (group === GROUP_NONE) return ballId !== EIGHT_BALL;
    if (onEight) return ballId === EIGHT_BALL;
    return groupOfBall(ballId) === group;
}

function contains(list: number[], value: number): boolean {
    for (var i = 0; i < list.length; i++) if (list[i] === value) return true;
    return false;
}

function resolveShot(state: GameState, events: SimEvent[], finalBalls: Ball[], table: Table): Outcome {
    if (state.winner !== NO_WINNER) throw new Error("The game is already over.");
    if (state.inHand) throw new Error("Place the cue ball before shooting.");

    var player = state.current;
    var group = groupOfPlayer(state, player);
    var onEight = isOnEightBall(state, player);

    var firstHit = -1;
    var firstHitTime = Infinity;
    for (var e0 = 0; e0 < events.length; e0++) {
        var ev = events[e0];
        if (ev.type !== EVENT_BALL || (ev.ball !== CUE_BALL && ev.other !== CUE_BALL)) continue;
        firstHitTime = ev.time;
        firstHit = ev.ball === CUE_BALL ? ev.other : ev.ball;
        break;
    }

    var pocketed: number[] = [];
    var railAfterContact = false;
    var cushionBalls: number[] = [];
    for (var e = 0; e < events.length; e++) {
        var x = events[e];
        if (x.type === EVENT_POCKET) pocketed.push(x.ball);
        if (x.type !== EVENT_CUSHION) continue;
        if (x.ball !== CUE_BALL && !contains(cushionBalls, x.ball)) cushionBalls.push(x.ball);
        if (firstHit >= 0 && x.time >= firstHitTime) railAfterContact = true;
    }

    var cuePocketed = contains(pocketed, CUE_BALL);
    var eightPocketed = contains(pocketed, EIGHT_BALL);

    var foul = FOUL_NONE;
    if (cuePocketed) foul = FOUL_SCRATCH;
    else if (firstHit < 0) foul = FOUL_NO_BALL_HIT;
    else if (!state.breakShot && !isLegalFirstHit(firstHit, group, onEight)) foul = FOUL_WRONG_BALL_FIRST;
    else if (state.breakShot && pocketed.length === 0 && cushionBalls.length < MIN_BREAK_CUSHION_BALLS) foul = FOUL_ILLEGAL_BREAK;
    else if (!state.breakShot && pocketed.length === 0 && !railAfterContact) foul = FOUL_NO_RAIL_AFTER_CONTACT;

    // Every ball ends at rest; a timeout must not leave residual motion in the next shot.
    var balls: Ball[] = [];
    for (var s = 0; s < finalBalls.length; s++) {
        var fb = finalBalls[s];
        balls.push(fb.pocketed ? copyBall(fb) : atRest(fb.id, fb.x, fb.y));
    }

    var respotted = false;
    var winner = NO_WINNER;
    if (eightPocketed && state.breakShot) {
        var spot = findFreeSpot(footSpotX(table), 0.0, +1, balls, EIGHT_BALL, table);
        balls[findBallIndex(balls, EIGHT_BALL)] = atRest(EIGHT_BALL, spot[0], spot[1]);
        respotted = true;
    } else if (eightPocketed) {
        winner = onEight && foul === FOUL_NONE ? player : 1 - player;
    }

    var p0 = state.g0;
    var p1 = state.g1;
    var groupsAssigned = false;
    var firstObject = -1;
    for (var f = 0; f < pocketed.length; f++) {
        if (groupOfBall(pocketed[f]) !== GROUP_NONE) { firstObject = pocketed[f]; break; }
    }

    if (state.g0 === GROUP_NONE && !state.breakShot && foul === FOUL_NONE && firstObject >= 0) {
        var claimed = groupOfBall(firstObject);
        p0 = player === 0 ? claimed : oppositeGroup(claimed);
        p1 = oppositeGroup(p0);
        group = claimed;
        groupsAssigned = true;
    }

    var scored = false;
    for (var k = 0; k < pocketed.length; k++) {
        var id = pocketed[k];
        if (state.breakShot ? id !== CUE_BALL : (group !== GROUP_NONE && groupOfBall(id) === group)) scored = true;
    }

    var keepsTurn = foul === FOUL_NONE && winner === NO_WINNER && scored;
    var nextPlayer = keepsTurn ? player : 1 - player;
    var ballInHand = foul !== FOUL_NONE && winner === NO_WINNER;
    var next = makeState(balls, nextPlayer, p0, p1, false, ballInHand, state.breakShot, winner);
    return { next: next, foul: foul, firstHit: firstHit, pocketed: pocketed, groupsAssigned: groupsAssigned, turnPassed: !keepsTurn, respotted: respotted };
}

/** Shot clock expired: opponent gets ball in hand anywhere, except on the break, which just passes. */
function resolveTimeout(state: GameState): Outcome {
    if (state.winner !== NO_WINNER) throw new Error("The game is already over.");
    var next = makeState(state.balls, 1 - state.current, state.g0, state.g1, state.breakShot, !state.breakShot, false, NO_WINNER);
    return { next: next, foul: FOUL_TIMEOUT, firstHit: -1, pocketed: [], groupsAssigned: false, turnPassed: true, respotted: false };
}
