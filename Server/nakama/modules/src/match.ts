// Server-authoritative 1v1 8-ball match. Clients send only shots (and cue placement); the server
// simulates, judges and broadcasts frames, events and the new state.

var MATCH_MODULE = "eightball";
var TICK_RATE = 10;
var TURN_SECONDS = 30;
var RECONNECT_GRACE_SECONDS = 60;
var MATCHMADE_JOIN_SECONDS = 30;
var ROOM_WAIT_SECONDS = 600;
var EMPTY_ROOM_SECONDS = 10;
var OVER_LINGER_SECONDS = 20;
/** Network and render slack added on top of the replay before the shot clock starts. */
var PLAYBACK_MARGIN_SECONDS = 1.5;
var MAX_NAME_LENGTH = 16;
/** Cosmetic avatar events relayed per player per second; the rest are dropped. */
var AVATAR_EVENTS_PER_SECOND = 4;

interface PlayerSlot {
    userId: string;
    name: string;
    presence: nkruntime.Presence | null;
    leftTick: number;
    character: string;
    avatarWindowTick: number;
    avatarCount: number;
}

interface EightBallMatch {
    table: Table;
    code: string;
    expected: string[];
    players: PlayerSlot[];
    phase: number;
    endReason: number;
    game: GameState | null;
    last: Outcome | null;
    deadlineTick: number;
    readyTick: number;
    overTick: number;
    seq: number;
    /** Character ids sent with join attempts, picked up when the join completes. */
    pendingCharacters: { [userId: string]: string };
}

function matchLabel(code: string): string {
    return JSON.stringify({ code: code });
}

function slotIndex(state: EightBallMatch, userId: string): number {
    for (var i = 0; i < state.players.length; i++) if (state.players[i].userId === userId) return i;
    return -1;
}

function playerName(nk: nkruntime.Nakama, presence: nkruntime.Presence): string {
    var name = presence.username;
    try {
        var users = nk.usersGetId([presence.userId]);
        if (users.length > 0 && users[0].displayName) name = users[0].displayName;
    } catch (e) {
        // Fall back to the username; the name is cosmetic.
    }
    name = String(name).replace(/[\u0000-\u001f<>]/g, "").replace(/^\s+|\s+$/g, "");
    return (name || "Player").substring(0, MAX_NAME_LENGTH);
}

function turnMs(state: EightBallMatch, tick: number): number {
    if (state.phase !== PHASE_PLAYING) return 0;
    return Math.max(0, Math.round((state.deadlineTick - tick) * 1000 / TICK_RATE));
}

function sendSnapshot(dispatcher: nkruntime.MatchDispatcher, state: EightBallMatch, tick: number, playback: object | null, targets: nkruntime.Presence[] | null): void {
    var players: object[] = [];
    for (var i = 0; i < state.players.length; i++) {
        var p = state.players[i];
        players.push({ id: p.userId, name: p.name, online: p.presence !== null, ch: p.character });
    }

    var message = {
        phase: state.phase,
        reason: state.endReason,
        code: state.code,
        players: players,
        turnMs: turnMs(state, tick),
        hasGame: state.game !== null,
        game: state.game ? encodeGame(state.game) : null,
        hasLast: state.last !== null,
        last: state.last ? encodeOutcome(state.last) : null,
        hasShot: playback !== null,
        shot: playback,
    };
    dispatcher.broadcastMessage(OP_SNAPSHOT, JSON.stringify(message), targets, null, true);
}

function startGame(dispatcher: nkruntime.MatchDispatcher, state: EightBallMatch, tick: number): void {
    var seed = Math.floor(Math.random() * 2147483647);
    var breaker = Math.random() < 0.5 ? 0 : 1;
    state.game = newGame(state.table, breaker, seed);
    state.phase = PHASE_PLAYING;
    state.deadlineTick = tick + TURN_SECONDS * TICK_RATE;
    state.readyTick = tick;
    dispatcher.matchLabelUpdate(matchLabel(""));
}

function endGame(logger: nkruntime.Logger, state: EightBallMatch, winner: number, reason: number, tick: number): void {
    var g = state.game;
    if (g) state.game = makeState(g.balls, g.current, g.g0, g.g1, g.breakShot, false, false, winner);
    state.phase = PHASE_OVER;
    state.endReason = reason;
    state.overTick = tick;
    logger.info("game over: winner %d, reason %d", winner, reason);
}

function reject(dispatcher: nkruntime.MatchDispatcher, sender: nkruntime.Presence, reason: string): void {
    dispatcher.broadcastMessage(OP_REJECTED, JSON.stringify({ reason: reason }), [sender], null, true);
}

function handleShot(logger: nkruntime.Logger, dispatcher: nkruntime.MatchDispatcher, state: EightBallMatch, tick: number, index: number, message: nkruntime.MatchMessage, nk: nkruntime.Nakama): void {
    var game = state.game;
    if (state.phase !== PHASE_PLAYING || !game || game.current !== index) return reject(dispatcher, message.sender, "not your turn");
    if (tick < state.readyTick) return reject(dispatcher, message.sender, "previous shot still playing");

    var request = parseShotRequest(nk.binaryToString(message.data));
    if (!request) return reject(dispatcher, message.sender, "malformed shot");

    var result: SimResult;
    var outcome: Outcome;
    try {
        var shot = makeShot(request.dx, request.dy, request.speed, request.side, request.top, CUE_BALL);
        var before = game;
        if (before.inHand) {
            if (!request.place) return reject(dispatcher, message.sender, "place the cue ball");
            before = withCueBallPlaced(before, request.cx, request.cy, state.table);
        }
        result = simulateShot(state.table, before.balls, shot, FRAME_SAMPLE_EVERY);
        outcome = resolveShot(before, result.events, result.finalBalls, state.table);
    } catch (e) {
        return reject(dispatcher, message.sender, String(e && (e as Error).message || e));
    }

    state.seq++;
    state.game = outcome.next;
    state.last = outcome;
    if (outcome.next.winner !== NO_WINNER) {
        state.phase = PHASE_OVER;
        state.endReason = END_EIGHT_BALL;
        state.overTick = tick;
        logger.info("game over: winner %d, reason %d", outcome.next.winner, END_EIGHT_BALL);
    }

    var playbackTicks = Math.ceil(result.duration * TICK_RATE);
    state.readyTick = tick + Math.max(0, playbackTicks - 5);
    state.deadlineTick = tick + playbackTicks + Math.ceil((PLAYBACK_MARGIN_SECONDS + TURN_SECONDS) * TICK_RATE);
    logger.debug("shot %d by %d: %d events, %.2fs, foul %d", state.seq, index, result.events.length, result.duration, outcome.foul);
    sendSnapshot(dispatcher, state, tick, encodePlayback(result, state.seq, request.speed), null);
}

/** Forwards a cosmetic character event to the opponent only, rate-limited; the match state never reads it. */
function relayAvatar(dispatcher: nkruntime.MatchDispatcher, state: EightBallMatch, tick: number, index: number, message: nkruntime.MatchMessage, nk: nkruntime.Nakama): void {
    var slot = state.players[index];
    if (tick - slot.avatarWindowTick >= TICK_RATE) {
        slot.avatarWindowTick = tick;
        slot.avatarCount = 0;
    }
    if (slot.avatarCount >= AVATAR_EVENTS_PER_SECOND) return;

    var event = parseAvatarEvent(nk.binaryToString(message.data));
    var other = state.players[1 - index];
    if (!event || !other || !other.presence) return;
    slot.avatarCount++;
    dispatcher.broadcastMessage(OP_AVATAR, JSON.stringify({ s: index, k: event.k, x: event.x, y: event.y }), [other.presence], null, true);
}

var matchInit: nkruntime.MatchInitFunction<EightBallMatch> = function (ctx, logger, nk, params) {
    var code = typeof params.code === "string" ? params.code : "";
    var expected = typeof params.users === "string" && params.users ? params.users.split(",") : [];
    var state: EightBallMatch = {
        table: createStandardTable(),
        code: code,
        expected: expected,
        players: [],
        phase: PHASE_WAITING,
        endReason: END_NONE,
        game: null,
        last: null,
        deadlineTick: 0,
        readyTick: 0,
        overTick: 0,
        seq: 0,
        pendingCharacters: {},
    };
    return { state: state, tickRate: TICK_RATE, label: matchLabel(code) };
};

var matchJoinAttempt: nkruntime.MatchJoinAttemptFunction<EightBallMatch> = function (ctx, logger, nk, dispatcher, tick, state, presence, metadata) {
    state.pendingCharacters[presence.userId] = parseCharacterId(metadata);
    if (slotIndex(state, presence.userId) >= 0) {
        // A returning player (reconnect) may rejoin until the game is fully over.
        return { state: state, accept: state.phase !== PHASE_OVER || state.overTick + OVER_LINGER_SECONDS * TICK_RATE > tick };
    }
    if (state.phase !== PHASE_WAITING) return { state: state, accept: false, rejectMessage: "match already started" };
    if (state.expected.length > 0 && state.expected.indexOf(presence.userId) < 0) return { state: state, accept: false, rejectMessage: "not invited" };
    if (state.players.length >= 2) return { state: state, accept: false, rejectMessage: "match full" };
    return { state: state, accept: true };
};

var matchJoin: nkruntime.MatchJoinFunction<EightBallMatch> = function (ctx, logger, nk, dispatcher, tick, state, presences) {
    for (var i = 0; i < presences.length; i++) {
        var presence = presences[i];
        var index = slotIndex(state, presence.userId);
        var character = state.pendingCharacters[presence.userId] || "";
        delete state.pendingCharacters[presence.userId];
        if (index >= 0) {
            state.players[index].presence = presence;
            if (character) state.players[index].character = character;
        } else if (state.players.length < 2) {
            state.players.push({ userId: presence.userId, name: playerName(nk, presence), presence: presence, leftTick: 0, character: character, avatarWindowTick: 0, avatarCount: 0 });
        }
    }

    if (state.phase === PHASE_WAITING && state.players.length === 2) {
        startGame(dispatcher, state, tick);
        logger.info("game started: %s vs %s", state.players[0].name, state.players[1].name);
    }
    sendSnapshot(dispatcher, state, tick, null, null);
    return { state: state };
};

var matchLeave: nkruntime.MatchLeaveFunction<EightBallMatch> = function (ctx, logger, nk, dispatcher, tick, state, presences) {
    for (var i = 0; i < presences.length; i++) {
        var index = slotIndex(state, presences[i].userId);
        if (index < 0) continue;
        var slot = state.players[index];
        if (slot.presence && slot.presence.sessionId !== presences[i].sessionId) continue; // an older session of a reconnected player
        if (state.phase === PHASE_WAITING) {
            state.players.splice(index, 1);
        } else {
            slot.presence = null;
            slot.leftTick = tick;
        }
    }

    sendSnapshot(dispatcher, state, tick, null, null);
    return { state: state };
};

var matchLoop: nkruntime.MatchLoopFunction<EightBallMatch> = function (ctx, logger, nk, dispatcher, tick, state, messages) {
    for (var m = 0; m < messages.length; m++) {
        var message = messages[m];
        var index = slotIndex(state, message.sender.userId);
        if (index < 0) continue;
        if (message.opCode === OP_SHOT) {
            handleShot(logger, dispatcher, state, tick, index, message, nk);
        } else if (message.opCode === OP_AVATAR && state.phase === PHASE_PLAYING) {
            relayAvatar(dispatcher, state, tick, index, message, nk);
        } else if (message.opCode === OP_FORFEIT && state.phase === PHASE_PLAYING) {
            endGame(logger, state, 1 - index, END_FORFEIT, tick);
            sendSnapshot(dispatcher, state, tick, null, null);
        }
    }

    if (state.phase === PHASE_WAITING) {
        var abandoned = (state.expected.length > 0 && tick > MATCHMADE_JOIN_SECONDS * TICK_RATE) ||
            (state.players.length === 0 && tick > EMPTY_ROOM_SECONDS * TICK_RATE) ||
            tick > ROOM_WAIT_SECONDS * TICK_RATE;
        if (!abandoned) return { state: state };
        state.phase = PHASE_OVER; // tells a lone waiting player the match was cancelled
        sendSnapshot(dispatcher, state, tick, null, null);
        return null;
    }

    if (state.phase === PHASE_PLAYING) {
        var gone = 0;
        var expired = -1;
        for (var i = 0; i < state.players.length; i++) {
            var slot = state.players[i];
            if (slot.presence) continue;
            gone++;
            if (tick - slot.leftTick >= RECONNECT_GRACE_SECONDS * TICK_RATE) expired = i;
        }

        if (expired >= 0) {
            if (gone === 2) return null; // both players vanished: nobody to award
            endGame(logger, state, 1 - expired, END_DISCONNECT, tick);
            sendSnapshot(dispatcher, state, tick, null, null);
        } else if (state.game && tick >= state.deadlineTick) {
            var outcome = resolveTimeout(state.game);
            state.game = outcome.next;
            state.last = outcome;
            state.deadlineTick = tick + TURN_SECONDS * TICK_RATE;
            sendSnapshot(dispatcher, state, tick, null, null);
        }
        return { state: state };
    }

    var anyoneHere = false;
    for (var k = 0; k < state.players.length; k++) if (state.players[k].presence) anyoneHere = true;
    if (!anyoneHere || tick - state.overTick > OVER_LINGER_SECONDS * TICK_RATE) return null;
    return { state: state };
};

var matchTerminate: nkruntime.MatchTerminateFunction<EightBallMatch> = function (ctx, logger, nk, dispatcher, tick, state, graceSeconds) {
    return { state: state };
};

var matchSignal: nkruntime.MatchSignalFunction<EightBallMatch> = function (ctx, logger, nk, dispatcher, tick, state, data) {
    return { state: state, data: data };
};
