using System;
using System.Collections.Generic;
using System.Globalization;
using BilliardGame.Gameplay;
using BilliardGame.Physics;

namespace BilliardGame.Online
{
    // Wire format of Server/nakama/modules/src/protocol.ts. Field names must match the server's JSON keys.
#pragma warning disable 0649 // fields are filled by JsonUtility

    [Serializable] public sealed class PlayerDto { public string id; public string name; public bool online; }

    [Serializable] public sealed class BallDto { public int id; public double x; public double y; public int pk; }

    [Serializable]
    public sealed class GameDto
    {
        public BallDto[] balls;
        public int cur, g0, g1, win;
        public bool brk, bih, kit;
    }

    [Serializable]
    public sealed class OutcomeDto
    {
        public int foul, first;
        public int[] pocketed;
        public bool ga, tp, rs;
    }

    [Serializable] public sealed class EventDto { public double t, s; public int ty, b, o, c, p; }

    [Serializable]
    public sealed class ShotDto
    {
        public int seq;
        public double dt, dur, speed;
        public int[] fs;
        public int[] f;
        public EventDto[] ev;
    }

    [Serializable]
    public sealed class MatchSnapshot
    {
        public int phase, reason, turnMs;
        public string code;
        public PlayerDto[] players;
        public bool hasGame, hasLast, hasShot;
        public GameDto game;
        public OutcomeDto last;
        public ShotDto shot;
    }

    [Serializable] public sealed class RejectDto { public string reason; }

    [Serializable] public sealed class RoomDto { public string code; public string matchId; }

#pragma warning restore 0649

    public static class OnlineProtocol
    {
        public const long OpSnapshot = 1;
        public const long OpShot = 2;
        public const long OpForfeit = 3;
        public const long OpRejected = 4;

        public const int PhaseWaiting = 0;
        public const int PhasePlaying = 1;
        public const int PhaseOver = 2;

        public const int EndNone = 0;
        public const int EndEightBall = 1;
        public const int EndForfeit = 2;
        public const int EndDisconnect = 3;

        private const double PositionScale = 10000.0;

        public static EightBallState ToState(GameDto g)
        {
            var balls = new BallState[g.balls.Length];
            for (int i = 0; i < balls.Length; i++)
            {
                BallDto b = g.balls[i];
                BallState ball = BallState.AtRest(b.id, new Vec2(b.x, b.y));
                balls[i] = b.pk >= 0 ? ball.AsPocketed(b.pk) : ball;
            }

            return new EightBallState(balls, g.cur, (BallGroup)g.g0, (BallGroup)g.g1, g.brk, g.bih, g.kit, g.win);
        }

        public static ShotOutcome ToOutcome(OutcomeDto o, EightBallState next) =>
            new ShotOutcome(next, (FoulType)o.foul, o.first, o.pocketed ?? Array.Empty<int>(), o.ga, o.tp, o.rs);

        /// <summary>
        /// Rebuilds a replayable result from delta-coded frames. Ball order follows <paramref name="order"/>
        /// (the server's state order); a ball counts as pocketed from its pocket event onwards.
        /// </summary>
        public static SimulationResult ToPlayback(ShotDto shot, IReadOnlyList<BallState> order)
        {
            int count = order.Count;
            int frameCount = shot.fs.Length;
            if (shot.f.Length != frameCount * count * 2) throw new FormatException("Frame data does not match the ball count.");

            var events = new SimulationEvent[shot.ev.Length];
            var pocketTime = new Dictionary<int, double>();
            var pocketIndex = new Dictionary<int, int>();
            for (int i = 0; i < events.Length; i++)
            {
                EventDto e = shot.ev[i];
                switch ((SimulationEventType)e.ty)
                {
                    case SimulationEventType.BallCollision: events[i] = SimulationEvent.BallCollision(e.t, e.b, e.o, e.s); break;
                    case SimulationEventType.CushionCollision: events[i] = SimulationEvent.CushionCollision(e.t, e.b, e.c, e.s); break;
                    case SimulationEventType.BallPocketed:
                        events[i] = SimulationEvent.BallPocketed(e.t, e.b, e.p, e.s);
                        pocketTime[e.b] = e.t;
                        pocketIndex[e.b] = e.p;
                        break;
                    default: throw new FormatException("Unknown event type " + e.ty);
                }
            }

            var frames = new SimulationFrame[frameCount];
            var position = new long[count * 2];
            var balls = new BallState[count];
            for (int f = 0; f < frameCount; f++)
            {
                double time = shot.fs[f] * shot.dt;
                for (int i = 0; i < count; i++)
                {
                    position[2 * i] += shot.f[(f * count + i) * 2];
                    position[2 * i + 1] += shot.f[(f * count + i) * 2 + 1];
                    int id = order[i].Id;
                    var ball = BallState.AtRest(id, new Vec2(position[2 * i] / PositionScale, position[2 * i + 1] / PositionScale));
                    // Pocketed during this shot: from its event on. Pocketed earlier: throughout.
                    bool gone = pocketTime.TryGetValue(id, out double t) ? t <= time : order[i].IsPocketed;
                    balls[i] = gone ? ball.AsPocketed(pocketIndex.TryGetValue(id, out int p) ? p : order[i].PocketIndex) : ball;
                }

                frames[f] = new SimulationFrame(time, balls);
            }

            return new SimulationResult(frames, events, true);
        }

        public static string ShotJson(ShotParams shot, Vec2? place)
        {
            var c = CultureInfo.InvariantCulture;
            string json = string.Format(c, "{{\"dx\":{0:R},\"dy\":{1:R},\"speed\":{2:R},\"side\":{3:R},\"top\":{4:R}",
                shot.Direction.X, shot.Direction.Y, shot.Speed, shot.SideSpin, shot.TopSpin);
            if (place.HasValue) json += string.Format(c, ",\"place\":true,\"cx\":{0:R},\"cy\":{1:R}", place.Value.X, place.Value.Y);
            return json + "}";
        }
    }
}
