using System;
using System.Threading.Tasks;
using BilliardGame.Online;
using BilliardGame.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BilliardGame.UI
{
    /// <summary>Lobby menu flow: nickname, quick match, play with a friend (room code), offline hot-seat. The lobby picks the mode.</summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        private const string NicknameKey = "nickname";
        private const int MaxNicknameLength = 16;

        [SerializeField] private GameController game;
        [SerializeField] private OnlineMatchController online;
        [SerializeField] private HudController hud;
        [SerializeField] private GameObject menuRoot;
        [SerializeField] private GameObject hudRoot;
        [SerializeField] private GameObject mainButtons;
        [SerializeField] private GameObject friendPanel;
        [SerializeField] private TMP_InputField nicknameInput;
        [SerializeField] private TMP_InputField codeInput;
        [SerializeField] private Button[] lockWhileWaiting = new Button[0];
        [SerializeField] private Button createRoomButton;
        [SerializeField] private Button joinRoomButton;
        [SerializeField] private Button backButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private TMP_Text statusLabel;

        private bool busy;

        /// <summary>Raised whenever the lobby comes back (start, cancel, match over); the lobby resets its 3D view.</summary>
        public event Action MenuShown;

        public string Status => statusLabel.text;
        public string RoomCode { get; private set; }

        private void OnEnable()
        {
            createRoomButton.onClick.AddListener(CreateRoom);
            joinRoomButton.onClick.AddListener(JoinRoomFromInput);
            backButton.onClick.AddListener(ShowMainButtons);
            cancelButton.onClick.AddListener(Cancel);
            online.MatchStarted += OnMatchStarted;
            online.MatchCancelled += OnMatchCancelled;
            hud.MenuRequested += BackToMenu;
        }

        private void OnDisable()
        {
            createRoomButton.onClick.RemoveListener(CreateRoom);
            joinRoomButton.onClick.RemoveListener(JoinRoomFromInput);
            backButton.onClick.RemoveListener(ShowMainButtons);
            cancelButton.onClick.RemoveListener(Cancel);
            online.MatchStarted -= OnMatchStarted;
            online.MatchCancelled -= OnMatchCancelled;
            hud.MenuRequested -= BackToMenu;
        }

        private async void Start()
        {
            nicknameInput.characterLimit = MaxNicknameLength;
            nicknameInput.text = PlayerPrefs.GetString(NicknameKey, "Player" + UnityEngine.Random.Range(1000, 10000));
            codeInput.characterLimit = 6;
            codeInput.contentType = TMP_InputField.ContentType.IntegerNumber;
            ShowMenu(string.Empty);

            if (!online.Client.HasSavedMatch) return;
            await Run("Rejoining your match...", async () =>
            {
                if (!await online.Client.TryResumeAsync(Nickname())) ShowMenu(string.Empty);
            });
        }

        public async void QuickMatch() =>
            await Run("Searching for an opponent...", () => online.Client.StartQuickMatchAsync(Nickname()), keepWaiting: true);

        public async void CreateRoom() => await Run("Creating a room...", async () =>
        {
            RoomCode = await online.Client.CreateRoomAsync(Nickname());
            SetStatus($"Room code: <b>{RoomCode}</b>\nShare it with your friend. Waiting for them to join...");
        }, keepWaiting: true);

        public async void JoinRoom(string code)
        {
            code = (code ?? string.Empty).Trim();
            if (!OnlineClient.IsRoomCode(code))
            {
                SetStatus("Enter the 6-digit room code");
                return;
            }

            await Run("Joining room " + code + "...", () => online.Client.JoinRoomAsync(Nickname(), code), keepWaiting: true);
        }

        public void PlayOffline()
        {
            HideMenu();
            game.StartOffline();
        }

        private void JoinRoomFromInput() => JoinRoom(codeInput.text);

        private async void Cancel()
        {
            if (online.Client.IsSearching) await online.Client.CancelQuickMatchAsync();
            else if (online.Client.MatchId != null) online.Leave();
            busy = false;
            RoomCode = null;
            ShowMenu("Cancelled");
        }

        private void BackToMenu()
        {
            if (game.IsOnline) online.Leave();
            else game.StopGame();
            ShowMenu(string.Empty);
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

        /// <summary>Runs an online action with a status line; on failure shows a readable error and re-enables the menu.</summary>
        private async Task Run(string status, Func<Task> action, bool keepWaiting = false)
        {
            if (busy) return;
            busy = true;
            SetWaiting(true);
            SetStatus(status);
            try
            {
                await action();
                if (!keepWaiting) busy = false;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Menu] {status} failed: {e}");
                busy = false;
                ShowMenu("Failed: " + Readable(e));
            }
        }

        private string Nickname()
        {
            string name = nicknameInput.text.Trim();
            if (name.Length == 0) name = "Player";
            if (name.Length > MaxNicknameLength) name = name.Substring(0, MaxNicknameLength);
            nicknameInput.text = name;
            PlayerPrefs.SetString(NicknameKey, name);
            PlayerPrefs.Save();
            return name;
        }

        private void ShowMenu(string status)
        {
            menuRoot.SetActive(true);
            hudRoot.SetActive(false);
            ShowMainButtons();
            SetWaiting(false);
            SetStatus(status);
            MenuShown?.Invoke();
        }

        private void HideMenu()
        {
            menuRoot.SetActive(false);
            hudRoot.SetActive(true);
        }

        public void ShowFriendPanel()
        {
            mainButtons.SetActive(false);
            friendPanel.SetActive(true);
        }

        private void ShowMainButtons()
        {
            mainButtons.SetActive(true);
            friendPanel.SetActive(false);
        }

        private void SetWaiting(bool waiting)
        {
            cancelButton.gameObject.SetActive(waiting);
            nicknameInput.interactable = !waiting;
            foreach (Button button in lockWhileWaiting) button.interactable = !waiting;
            createRoomButton.interactable = !waiting;
            joinRoomButton.interactable = !waiting;
            backButton.interactable = !waiting;
        }

        private void SetStatus(string text) => statusLabel.text = text;

        // Server RPC errors arrive as "Error: <message> at rpcJoinRoom (index.js:..)"; show just the message.
        private static string Readable(Exception e)
        {
            string message = e.Message;
            int start = message.IndexOf("Error: ", StringComparison.Ordinal);
            if (start >= 0) message = message.Substring(start + 7);
            int at = message.IndexOf(" at ", StringComparison.Ordinal);
            if (at > 0) message = message.Substring(0, at);
            return message.Length > 120 ? "could not reach the server" : message;
        }
    }
}
