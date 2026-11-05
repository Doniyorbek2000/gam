using System;
using System.Text;
using System.Threading.Tasks;
using Nakama;
using UnityEngine;

namespace BilliardGame.Online
{
    /// <summary>
    /// Nakama session and socket: anonymous device login with a nickname, quick match, room codes,
    /// match messages and automatic reconnect to the current match.
    /// </summary>
    public sealed class OnlineClient : MonoBehaviour
    {
        private const string MatchIdKey = "online.matchId";
        private const float ReconnectInterval = 2f;
        private const float ReconnectGiveUpSeconds = 45f;

        [SerializeField] private string scheme = "http";
        [Tooltip("Nakama host. 127.0.0.1 works for the editor and a desktop build; a phone needs the PC's LAN IP.")]
        [SerializeField] private string host = "127.0.0.1";
        [SerializeField] private int port = 7350;
        [Tooltip("Must match --socket.server_key of the server (dev default: defaultkey).")]
        [SerializeField] private string serverKey = "defaultkey";

        private IClient client;
        private ISession session;
        private ISocket socket;
        private IMatchmakerTicket ticket;
        private string displayName;
        private bool reconnecting;

        public event Action<MatchSnapshot> SnapshotReceived;
        public event Action<string> ShotRejected;

        /// <summary>Connection problems while in a match ("Reconnecting…"); null when healthy.</summary>
        public event Action<string> ConnectionStatus;

        public string UserId => session?.UserId;
        public string MatchId { get; private set; }
        public bool IsSearching => ticket != null;
        public bool HasSavedMatch => !string.IsNullOrEmpty(PlayerPrefs.GetString(MatchIdKey, string.Empty));

        private void OnDestroy() => socket?.CloseAsync();

        private void OnApplicationPause(bool paused)
        {
            if (!paused && MatchId != null && socket != null && !socket.IsConnected) Reconnect();
        }

        /// <summary>Logs in with this device's id (creating the account once) and sets the nickname.</summary>
        public async Task ConnectAsync(string nickname)
        {
            client ??= new Client(scheme, host, port, serverKey, UnityWebRequestAdapter.Instance);
            if (session == null || session.HasExpired(DateTime.UtcNow.AddMinutes(5)))
            {
                session = await client.AuthenticateDeviceAsync(DeviceId());
            }

            if (nickname != displayName)
            {
                await client.UpdateAccountAsync(session, null, nickname);
                displayName = nickname;
            }

            if (socket == null)
            {
                socket = client.NewSocket(useMainThread: true);
                socket.ReceivedMatchState += OnMatchState;
                socket.ReceivedMatchmakerMatched += OnMatchmakerMatched;
                socket.Closed += OnSocketClosed;
            }

            if (!socket.IsConnected) await socket.ConnectAsync(session, true, 10);
        }

        public async Task StartQuickMatchAsync(string nickname)
        {
            await ConnectAsync(nickname);
            if (ticket != null) return;
            ticket = await socket.AddMatchmakerAsync("*", 2, 2);
        }

        public async Task CancelQuickMatchAsync()
        {
            IMatchmakerTicket current = ticket;
            ticket = null;
            if (current != null && socket != null && socket.IsConnected) await socket.RemoveMatchmakerAsync(current);
        }

        /// <summary>Creates a private room, joins it and returns its 6-digit code.</summary>
        public async Task<string> CreateRoomAsync(string nickname)
        {
            await ConnectAsync(nickname);
            RoomDto room = JsonUtility.FromJson<RoomDto>((await socket.RpcAsync("create_room", "{}")).Payload);
            await JoinAsync(room.matchId);
            return room.code;
        }

        public async Task JoinRoomAsync(string nickname, string code)
        {
            if (!IsRoomCode(code)) throw new ArgumentException("Room code must be 6 digits.", nameof(code));
            await ConnectAsync(nickname);
            RoomDto room = JsonUtility.FromJson<RoomDto>((await socket.RpcAsync("join_room", "{\"code\":\"" + code + "\"}")).Payload);
            await JoinAsync(room.matchId);
        }

        /// <summary>Rejoins the match this device was in before the app closed. False if it is gone.</summary>
        public async Task<bool> TryResumeAsync(string nickname)
        {
            string saved = PlayerPrefs.GetString(MatchIdKey, string.Empty);
            if (string.IsNullOrEmpty(saved)) return false;
            try
            {
                await ConnectAsync(nickname);
                await JoinAsync(saved);
                return true;
            }
            catch (Exception e)
            {
                Debug.Log($"[Online] Saved match {saved} is gone: {e.Message}");
                ForgetMatch();
                return false;
            }
        }

        public void SendShot(string json) => Send(OnlineProtocol.OpShot, json);

        /// <summary>Leaves the current match; with <paramref name="forfeit"/> the opponent is awarded the game first.</summary>
        public async Task LeaveMatchAsync(bool forfeit)
        {
            string id = MatchId;
            ForgetMatch();
            if (id == null || socket == null || !socket.IsConnected) return;
            try
            {
                if (forfeit) await socket.SendMatchStateAsync(id, OnlineProtocol.OpForfeit, "{}");
                await socket.LeaveMatchAsync(id);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Online] Leaving match failed: {e.Message}");
            }
        }

        /// <summary>Stops reconnecting to the current match (it ended).</summary>
        public void ForgetMatch()
        {
            MatchId = null;
            PlayerPrefs.DeleteKey(MatchIdKey);
            PlayerPrefs.Save();
        }

        private async Task JoinAsync(string matchId)
        {
            MatchId = matchId;
            PlayerPrefs.SetString(MatchIdKey, matchId);
            PlayerPrefs.Save();
            await socket.JoinMatchAsync(matchId);
        }

        private async void Send(long opCode, string json)
        {
            if (MatchId == null || socket == null) return;
            try
            {
                await socket.SendMatchStateAsync(MatchId, opCode, json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Online] Send failed: {e.Message}");
                ShotRejected?.Invoke("connection lost");
                Reconnect();
            }
        }

        private async void OnMatchmakerMatched(IMatchmakerMatched matched)
        {
            ticket = null;
            try
            {
                MatchId = matched.MatchId;
                PlayerPrefs.SetString(MatchIdKey, matched.MatchId);
                PlayerPrefs.Save();
                await socket.JoinMatchAsync(matched);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Online] Joining matched game failed: {e.Message}");
                ForgetMatch();
                ConnectionStatus?.Invoke("Could not join the match");
            }
        }

        private void OnMatchState(IMatchState state)
        {
            if (state.MatchId != MatchId) return;
            string json = Encoding.UTF8.GetString(state.State);
            try
            {
                if (state.OpCode == OnlineProtocol.OpSnapshot) SnapshotReceived?.Invoke(JsonUtility.FromJson<MatchSnapshot>(json));
                else if (state.OpCode == OnlineProtocol.OpRejected) ShotRejected?.Invoke(JsonUtility.FromJson<RejectDto>(json).reason);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Online] Bad message {state.OpCode}: {e}");
            }
        }

        private void OnSocketClosed(string reason)
        {
            ticket = null;
            if (MatchId != null) Reconnect();
        }

        private async void Reconnect()
        {
            if (reconnecting) return;
            reconnecting = true;
            float giveUpAt = Time.realtimeSinceStartup + ReconnectGiveUpSeconds;
            while (MatchId != null && Time.realtimeSinceStartup < giveUpAt)
            {
                ConnectionStatus?.Invoke("Connection lost - reconnecting...");
                try
                {
                    // A closed Nakama socket does not reconnect cleanly (pending calls get cancelled); start a fresh one.
                    DropSocket();
                    await ConnectAsync(displayName);
                    await socket.JoinMatchAsync(MatchId);
                    ConnectionStatus?.Invoke(null);
                    reconnecting = false;
                    return;
                }
                catch (Exception e)
                {
                    Debug.Log($"[Online] Reconnect attempt failed: {e.Message}");
                }

                await Task.Delay(TimeSpan.FromSeconds(ReconnectInterval));
            }

            reconnecting = false;
            if (MatchId != null) ConnectionStatus?.Invoke("Disconnected from the match");
        }

        private void DropSocket()
        {
            if (socket == null) return;
            socket.ReceivedMatchState -= OnMatchState;
            socket.ReceivedMatchmakerMatched -= OnMatchmakerMatched;
            socket.Closed -= OnSocketClosed;
            socket.CloseAsync();
            socket = null;
        }

        public static bool IsRoomCode(string code) => code != null && System.Text.RegularExpressions.Regex.IsMatch(code, "^[1-9][0-9]{5}$");

        // The editor gets its own account so it can play a build on the same PC.
        private static string DeviceId() => SystemInfo.deviceUniqueIdentifier + (Application.isEditor ? "-editor" : string.Empty);
    }
}
