// Nakama entry point: match handler, room-code RPCs and the quick-match matchmaker hook.

var ROOM_CODE_PATTERN = /^[1-9][0-9]{5}$/;
var ROOM_CODE_ATTEMPTS = 10;

function findOpenRoom(nk: nkruntime.Nakama, code: string): nkruntime.Match | null {
    var matches = nk.matchList(1, true, null, 0, 1, "+label.code:" + code);
    return matches.length > 0 ? matches[0] : null;
}

var rpcCreateRoom: nkruntime.RpcFunction = function (ctx, logger, nk, payload) {
    if (!ctx.userId) throw new Error("authentication required");
    for (var attempt = 0; attempt < ROOM_CODE_ATTEMPTS; attempt++) {
        var code = String(100000 + Math.floor(Math.random() * 900000));
        if (findOpenRoom(nk, code)) continue;
        var matchId = nk.matchCreate(MATCH_MODULE, { code: code });
        return JSON.stringify({ code: code, matchId: matchId });
    }
    throw new Error("could not allocate a room code, try again");
};

var rpcJoinRoom: nkruntime.RpcFunction = function (ctx, logger, nk, payload) {
    if (!ctx.userId) throw new Error("authentication required");
    var code = "";
    try {
        code = String(JSON.parse(payload || "{}").code || "");
    } catch (e) {
        throw new Error("invalid request");
    }
    if (!ROOM_CODE_PATTERN.test(code)) throw new Error("room code must be 6 digits");

    var room = findOpenRoom(nk, code);
    if (!room) throw new Error("room not found or already full");
    return JSON.stringify({ code: code, matchId: room.matchId });
};

/** Quick match: tickets carry {mode, venue} string properties and only match equal ones (query on the client). */
var matchmakerMatched: nkruntime.MatchmakerMatchedFunction = function (ctx, logger, nk, matches) {
    var users: string[] = [];
    for (var i = 0; i < matches.length; i++) users.push(matches[i].presence.userId);
    var venue = matches.length > 0 && matches[0].properties ? String(matches[0].properties.venue || "") : "";
    return nk.matchCreate(MATCH_MODULE, { users: users.join(","), venue: venue.substring(0, 32) });
};

function InitModule(ctx: nkruntime.Context, logger: nkruntime.Logger, nk: nkruntime.Nakama, initializer: nkruntime.Initializer) {
    initializer.registerMatch<EightBallMatch>(MATCH_MODULE, {
        matchInit: matchInit,
        matchJoinAttempt: matchJoinAttempt,
        matchJoin: matchJoin,
        matchLeave: matchLeave,
        matchLoop: matchLoop,
        matchTerminate: matchTerminate,
        matchSignal: matchSignal,
    });
    initializer.registerRpc("create_room", rpcCreateRoom);
    initializer.registerRpc("join_room", rpcJoinRoom);
    initializer.registerMatchmakerMatched(matchmakerMatched);
    // Economy (src/economy.ts). Nakama reads each handler's name from this function's body, so registrations stay here.
    seedConfig(nk, logger);
    initializer.registerRpc("economy_state", rpcEconomyState);
    initializer.registerRpc("config_get", rpcConfigGet);
    initializer.registerRpc("crate_open", rpcCrateOpen);
    initializer.registerRpc("shop_buy", rpcShopBuy);
    initializer.registerRpc("equip", rpcEquip);
    initializer.registerRpc("daily_claim", rpcDailyClaim);
    initializer.registerRpc("friend_add_by_pid", rpcFriendAddByPid);
    initializer.registerAfterAuthenticateDevice(afterAuthenticateDevice);
    // LiveOps (src/liveops.ts): missions, Season Pass, events, daily shop.
    initializer.registerRpc("liveops_state", rpcLiveOpsState);
    initializer.registerRpc("mission_claim", rpcMissionClaim);
    initializer.registerRpc("mission_reroll", rpcMissionReroll);
    initializer.registerRpc("pass_claim", rpcPassClaim);
    initializer.registerRpc("pass_buy", rpcPassBuy);
    initializer.registerRpc("daily_shop_buy", rpcDailyShopBuy);
    logger.info("billiard module loaded");
}
