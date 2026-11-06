using System;
using System.Threading.Tasks;
using BilliardGame.Online;
using BilliardGame.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BilliardGame.UI
{
    /// <summary>
    /// Match entry flow: quick match, play with a friend (room code), offline hot-seat, rejoin. Shows the waiting
    /// overlay (spinner, status, cancel) and readable errors; swaps the lobby and the match HUD. The lobby picks the mode.
    /// </summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private GameController game;
        [SerializeField] private OnlineMatchController online;
        [SerializeField] private HudController hud;
        [SerializeField] private GameServices services;
        [SerializeField] private GameObject menuRoot;
        [SerializeField] private GameObject hudRoot;
        [SerializeField] private ScreenView friendView;
        [SerializeField] private TMP_InputField codeInput;
        [SerializeField] private Button createRoomButton;
        [SerializeField] private Button joinRoomButton;
        [SerializeField] private ScreenView waitingView;
        [SerializeField] private TMP_Text waitingStatus;
        [SerializeField] private Button cancelButton;

        private bool busy;

        /// <summary>Raised whenever the lobby comes back (start, cancel, match over); the lobby resets its 3D view.</summary>
        public event Action MenuShown;

        public string Status => waitingStatus.text;

        private float searchStarted;
        private int shownSearchSeconds = -1;
        public string RoomCode { get; private set; }

        private void OnEnable()
        {
            createRoomButton.onClick.AddListener(CreateRoom);
            joinRoomButton.onClick.AddListener(JoinRoomFromInput);
            cancelButton.onClick.AddListener(Cancel);
            online.MatchStarted += OnMatchStarted;
            online.MatchCancelled += OnMatchCancelled;
            hud.MenuRequested += ReturnToMenu;
        }

        private void OnDisable()
        {
            createRoomButton.onClick.RemoveListener(CreateRoom);
            joinRoomButton.onClick.RemoveListener(JoinRoomFromInput);
            cancelButton.onClick.RemoveListener(Cancel);
            online.MatchStarted -= OnMatchStarted;
            online.MatchCancelled -= OnMatchCancelled;
            hud.MenuRequested -= ReturnToMenu;
        }

        private async void Start()
        {
            codeInput.characterLimit = 6;
            codeInput.contentType = TMP_InputField.ContentType.IntegerNumber;
            ShowMenu(null);

            if (!online.Client.HasSavedMatch) return;
            await Run("menu.rejoining", async () =>
            {
                if (!await online.Client.TryResumeAsync(Nickname())) ShowMenu(null);
            });
        }

        public async void QuickMatch()
        {
            searchStarted = Time.unscaledTime;
            shownSearchSeconds = -1;
            await Run("menu.searching", () => online.Client.StartQuickMatchAsync(Nickname()), keepWaiting: true);
        }

        // "Searching… 0:12" while the matchmaker ticket is open.
        private void Update()
        {
            if (!online.Client.IsSearching || !waitingView.IsOpen) return;
            int seconds = Mathf.FloorToInt(Time.unscaledTime - searchStarted);
            if (seconds == shownSearchSeconds) return;
            shownSearchSeconds = seconds;
            waitingStatus.text = $"{Loc.Get("menu.searching")}  {seconds / 60}:{seconds % 60:00}";
        }

        public async void CreateRoom() => await Run("menu.creating", async () =>
        {
            RoomCode = await online.Client.CreateRoomAsync(Nickname());
            waitingStatus.text = Loc.Format("menu.room_code", RoomCode);
        }, keepWaiting: true);

        public async void JoinRoom(string code)
        {
            code = (code ?? string.Empty).Trim();
            if (!OnlineClient.IsRoomCode(code))
            {
                Dialogs.Instance.Toast(Loc.Get("menu.enter_code"));
                return;
            }

            await Run(Loc.Format("menu.joining", code), () => online.Client.JoinRoomAsync(Nickname(), code), keepWaiting: true, literal: true);
        }

        public void PlayOffline()
        {
            HideMenu();
            game.StartOffline();
        }

        public void ShowFriendPanel() => ScreenRouter.Instance.Open(friendView);

        /// <summary>Leaves the current game (an online match is forfeited) and brings the lobby back.</summary>
        public void ReturnToMenu()
        {
            if (game.IsOnline) online.Leave();
            else game.StopGame();
            ShowMenu(null);
        }

        private void JoinRoomFromInput() => JoinRoom(codeInput.text);

        private async void Cancel()
        {
            if (online.Client.IsSearching) await online.Client.CancelQuickMatchAsync();
            else if (online.Client.MatchId != null) online.Leave();
            busy = false;
            RoomCode = null;
            ShowMenu(Loc.Get("menu.cancelled"));
        }

        private void OnMatchStarted()
        {
            busy = false;
            HideMenu();
        }

        private void OnMatchCancelled(string reason)
        {
            busy = false;
            game.StopGame();
            ShowMenu(reason);
        }

        /// <summary>Runs an online action under the waiting overlay; on failure shows a readable error and the lobby again.</summary>
        private async Task Run(string status, Func<Task> action, bool keepWaiting = false, bool literal = false)
        {
            if (busy) return;
            busy = true;
            ScreenRouter.Instance.Close(friendView);
            waitingStatus.text = literal ? status : Loc.Get(status);
            waitingView.Show();
            try
            {
                await action();
                if (!keepWaiting)
                {
                    busy = false;
                    waitingView.Hide();
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Menu] {status} failed: {e}");
                busy = false;
                ShowMenu(null);
                string readable = Readable(e);
                if (readable == null) Dialogs.Instance.ShowOffline(null);
                else Dialogs.Instance.Error("common.error", Loc.Format("menu.failed", readable));
            }
        }

        private string Nickname() => services.Profile.State.nickname;

        private void ShowMenu(string toast)
        {
            menuRoot.SetActive(true);
            hudRoot.SetActive(false);
            if (waitingView.IsOpen) waitingView.Hide();
            if (!string.IsNullOrEmpty(toast)) Dialogs.Instance.Toast(toast);
            MenuShown?.Invoke();
        }

        private void HideMenu()
        {
            if (waitingView.IsOpen) waitingView.HideImmediate();
            ScreenRouter.Instance.CloseAll(true);
            menuRoot.SetActive(false);
            hudRoot.SetActive(true);
        }

        // Server RPC errors arrive as "Error: <message> at rpcJoinRoom (index.js:..)"; show just the message. Null = unreachable.
        private static string Readable(Exception e)
        {
            string message = e.Message;
            int start = message.IndexOf("Error: ", StringComparison.Ordinal);
            if (start >= 0) message = message.Substring(start + 7);
            int at = message.IndexOf(" at ", StringComparison.Ordinal);
            if (at > 0) message = message.Substring(0, at);
            return message.Length > 120 ? null : message;
        }
    }
}
