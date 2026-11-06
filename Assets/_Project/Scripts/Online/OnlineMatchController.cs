using System;
using BilliardGame.Gameplay;
using BilliardGame.Physics;
using BilliardGame.Presentation;
using UnityEngine;

namespace BilliardGame.Online
{
    /// <summary>Feeds server snapshots into the <see cref="GameController"/> and sends the local player's shots.</summary>
    public sealed class OnlineMatchController : MonoBehaviour
    {
        [SerializeField] private OnlineClient client;
        [SerializeField] private GameController game;

        private bool started;
        private int lastShotSeq;
        private MatchSnapshot lastSnapshot;

        /// <summary>The game started (or was resumed) and the board is live.</summary>
        public event Action MatchStarted;

        /// <summary>The match ended before it started (cancelled, opponent never came).</summary>
        public event Action<string> MatchCancelled;

        public OnlineClient Client => client;
        public bool IsInMatch => started;

        /// <summary>Character id the player in <paramref name="seat"/> joined with ("" when unknown).</summary>
        public string CharacterOf(int seat) =>
            lastSnapshot != null && seat >= 0 && seat < lastSnapshot.players.Length ? lastSnapshot.players[seat].ch ?? string.Empty : string.Empty;

        private void OnEnable()
        {
            client.SnapshotReceived += OnSnapshot;
            client.ShotRejected += OnRejected;
            client.ConnectionStatus += OnConnectionStatus;
        }

        private void OnDisable()
        {
            client.SnapshotReceived -= OnSnapshot;
            client.ShotRejected -= OnRejected;
            client.ConnectionStatus -= OnConnectionStatus;
        }

        /// <summary>Leaves the match; an unfinished game is forfeited.</summary>
        public async void Leave()
        {
            bool forfeit = started && game.State != null && !game.State.IsGameOver;
            started = false;
            lastShotSeq = 0;
            game.StopGame();
            await client.LeaveMatchAsync(forfeit);
        }

        private void OnSnapshot(MatchSnapshot s)
        {
            int me = Array.FindIndex(s.players, p => p.id == client.UserId);
            lastSnapshot = s;
            if (!started)
            {
                if (s.phase == OnlineProtocol.PhaseOver && !s.hasGame)
                {
                    client.ForgetMatch();
                    MatchCancelled?.Invoke("The match was cancelled");
                    return;
                }

                if (!s.hasGame || me < 0) return; // still waiting for the opponent
                started = true;
                lastShotSeq = 0;
                game.StartOnline(me, SendShot);
                MatchStarted?.Invoke();
            }

            game.SetPlayerNames(Name(s, 0), Name(s, 1));
            game.SetTurnTimer(s.phase == OnlineProtocol.PhasePlaying ? s.turnMs / 1000f : float.NaN);
            game.SetBanner(Banner(s, me));

            EightBallState state = OnlineProtocol.ToState(s.game);
            ShotOutcome last = s.hasLast ? OnlineProtocol.ToOutcome(s.last, state) : null;
            if (s.hasShot && s.shot.seq > lastShotSeq)
            {
                lastShotSeq = s.shot.seq;
                bool byOpponent = !game.IsShotInProgress;
                game.PlayRemoteShot(OnlineProtocol.ToPlayback(s.shot, state.Balls), last, byOpponent);
            }
            else
            {
                game.ApplyRemoteState(state, last);
            }

            if (s.phase == OnlineProtocol.PhaseOver) client.ForgetMatch();
        }

        private void SendShot(ShotParams shot, Vec2? place) => client.SendShot(OnlineProtocol.ShotJson(shot, place));

        private void OnRejected(string reason)
        {
            Debug.LogWarning($"[Online] Shot rejected: {reason}");
            game.RejectShot();
        }

        private void OnConnectionStatus(string status)
        {
            if (started) game.SetBanner(status);
        }

        private static string Name(MatchSnapshot s, int seat) => seat < s.players.Length ? s.players[seat].name : "Opponent";

        private static string Banner(MatchSnapshot s, int me)
        {
            int winner = s.hasGame ? s.game.win : -1;
            bool won = winner == me;
            if (s.phase == OnlineProtocol.PhaseOver)
            {
                switch (s.reason)
                {
                    case OnlineProtocol.EndForfeit: return won ? "Opponent left - you win!" : "You forfeited";
                    case OnlineProtocol.EndDisconnect: return won ? "Opponent disconnected - you win!" : "You were disconnected - you lose";
                    default: return null;
                }
            }

            for (int i = 0; i < s.players.Length; i++)
            {
                if (i != me && !s.players[i].online) return "Opponent disconnected - waiting for them to return...";
            }

            return null;
        }
    }
}
