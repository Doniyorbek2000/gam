using System;
using System.Collections;
using System.Threading.Tasks;
using BilliardGame.Content;
using BilliardGame.Online;
using BilliardGame.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BilliardGame.UI
{
    /// <summary>
    /// PUBG-style lobby (docs/UI_UX.md §2–3): balances, rails (Pass/Tasks "coming soon"; Shop/Crates/Inventory live),
    /// mode + venue sheet, PLAY. Switches between the lobby camera and the match camera and loads the chosen venue.
    /// </summary>
    public sealed class LobbyController : MonoBehaviour
    {
        public enum Mode
        {
            HotSeat = 0,
            Friend = 1,
            Quick = 2,
        }

        private const string ModeKey = "lobby_mode";
        private const string VenueKey = "lobby_venue";
        private const int DevCoins = 10_000;
        private const int DevGems = 1_000;
        private static readonly string[] ModeNames = { "Hot-seat", "Do'st bilan", "Tezkor o'yin" };

        [SerializeField] private GameServices services;
        [SerializeField] private MainMenuController menu;
        [SerializeField] private OnlineMatchController online;
        [SerializeField] private GameController game;
        [SerializeField] private VenueLoader venues;
        [SerializeField] private LobbyStage stage;
        [SerializeField] private Camera matchCamera;
        [SerializeField] private CrateScreen crates;
        [SerializeField] private InventoryScreen inventory;
        [SerializeField] private CreditsScreen credits;

        [SerializeField] private TMP_Text coinsLabel;
        [SerializeField] private TMP_Text gemsLabel;
        [SerializeField] private Button addCoins;
        [SerializeField] private Button addGems;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button passButton;
        [SerializeField] private Button tasksButton;
        [SerializeField] private Button shopButton;
        [SerializeField] private Button crateButton;
        [SerializeField] private Button inventoryButton;
        [SerializeField] private Button selectorButton;
        [SerializeField] private TMP_Text selectorLabel;
        [SerializeField] private Button playButton;
        [SerializeField] private GameObject sheet;
        [SerializeField] private Button[] modeButtons = new Button[0];
        [SerializeField] private Button[] venueButtons = new Button[0];
        [SerializeField] private Button sheetDone;
        [SerializeField] private TMP_Text toast;

        private Mode mode;
        private int venueIndex;
        private Coroutine toastRoutine;

        public Mode SelectedMode => mode;
        public VenueDefinition SelectedVenue => services.Catalog.Venues[venueIndex];

        private void Awake()
        {
            mode = (Mode)Mathf.Clamp(PlayerPrefs.GetInt(ModeKey, 0), 0, ModeNames.Length - 1);
            venueIndex = Mathf.Clamp(PlayerPrefs.GetInt(VenueKey, services.Catalog.Venues.Count - 1), 0, services.Catalog.Venues.Count - 1);

            // Dev/E2E: "-venue silk_road_lounge" picks the hall without touching the UI.
            string[] args = Environment.GetCommandLineArgs();
            int venueArg = Array.IndexOf(args, "-venue");
            if (venueArg >= 0 && venueArg + 1 < args.Length) SelectVenue(args[venueArg + 1]);
        }

        private void OnEnable()
        {
            services.Economy.Changed += RefreshBalances;
            menu.MenuShown += OnMenuShown;
            online.MatchStarted += OnOnlineMatchStarted;
            addCoins.onClick.AddListener(GrantCoins);
            addGems.onClick.AddListener(GrantGems);
            settingsButton.onClick.AddListener(credits.Open);
            passButton.onClick.AddListener(ComingSoon);
            tasksButton.onClick.AddListener(ComingSoon);
            shopButton.onClick.AddListener(crates.Open);
            crateButton.onClick.AddListener(crates.Open);
            inventoryButton.onClick.AddListener(inventory.Open);
            selectorButton.onClick.AddListener(OpenSheet);
            sheetDone.onClick.AddListener(CloseSheet);
            playButton.onClick.AddListener(Play);
            for (int i = 0; i < modeButtons.Length; i++)
            {
                int index = i;
                modeButtons[i].onClick.AddListener(() => SelectMode((Mode)index));
            }

            for (int i = 0; i < venueButtons.Length; i++)
            {
                int index = i;
                venueButtons[i].onClick.AddListener(() => SelectVenue(index));
            }
        }

        private void OnDisable()
        {
            services.Economy.Changed -= RefreshBalances;
            menu.MenuShown -= OnMenuShown;
            online.MatchStarted -= OnOnlineMatchStarted;
            foreach (Button b in new[] { addCoins, addGems, settingsButton, passButton, tasksButton, shopButton, crateButton, inventoryButton, selectorButton, sheetDone, playButton }) b.onClick.RemoveAllListeners();
            foreach (Button b in modeButtons) b.onClick.RemoveAllListeners();
            foreach (Button b in venueButtons) b.onClick.RemoveAllListeners();
        }

        private void Start()
        {
            sheet.SetActive(false);
            toast.gameObject.SetActive(false);
            RefreshBalances();
            RefreshSelector();
        }

        public void SelectMode(Mode selected)
        {
            mode = selected;
            PlayerPrefs.SetInt(ModeKey, (int)mode);
            RefreshSelector();
        }

        public void SelectVenue(string venueId)
        {
            for (int i = 0; i < services.Catalog.Venues.Count; i++)
            {
                if (services.Catalog.Venues[i].Id == venueId) SelectVenue(i);
            }
        }

        private void SelectVenue(int index)
        {
            venueIndex = index;
            PlayerPrefs.SetInt(VenueKey, index);
            RefreshSelector();
        }

        /// <summary>PLAY: hot-seat loads the hall first; online modes load it once the match starts.</summary>
        public async void Play()
        {
            switch (mode)
            {
                case Mode.HotSeat:
                    playButton.interactable = false;
                    await EnterVenue();
                    playButton.interactable = true;
                    menu.PlayOffline();
                    break;
                case Mode.Friend:
                    menu.ShowFriendPanel();
                    break;
                default:
                    menu.QuickMatch();
                    break;
            }
        }

        private async void OnOnlineMatchStarted() => await EnterVenue();

        private async Task EnterVenue()
        {
            await venues.LoadAsync(SelectedVenue);
            SetLobbyView(false);
        }

        private async void OnMenuShown()
        {
            SetLobbyView(true);
            stage.ShowCharacter();
            await venues.UnloadAsync();
        }

        private void SetLobbyView(bool lobby)
        {
            stage.Camera.enabled = lobby;
            stage.gameObject.SetActive(lobby);
            matchCamera.enabled = !lobby;
        }

        private void RefreshBalances()
        {
            coinsLabel.text = services.Economy.Coins.ToString("N0");
            gemsLabel.text = services.Economy.Gems.ToString("N0");
        }

        private void RefreshSelector()
        {
            selectorLabel.text = $"8-Ball · {ModeNames[(int)mode]}\n<size=70%>{SelectedVenue.DisplayName}</size>";
            for (int i = 0; i < modeButtons.Length; i++) Highlight(modeButtons[i], i == (int)mode);
            for (int i = 0; i < venueButtons.Length; i++) Highlight(venueButtons[i], i == venueIndex);
        }

        private static void Highlight(Button button, bool on)
        {
            var image = button.GetComponent<Image>();
            image.color = on ? new Color(1f, 0.69f, 0.13f) : new Color(0.16f, 0.2f, 0.3f);
        }

        private void OpenSheet() => sheet.SetActive(true);

        private void CloseSheet() => sheet.SetActive(false);

        private void GrantCoins()
        {
            services.Mock.DevGrant(DevCoins, 0);
            ShowToast($"Test rejimi: +{DevCoins:N0} Coin");
        }

        private void GrantGems()
        {
            services.Mock.DevGrant(0, DevGems);
            ShowToast($"Test rejimi: +{DevGems:N0} Gem");
        }

        private void ComingSoon() => ShowToast("Tez orada — keyingi mavsumda");

        public void ShowToast(string text)
        {
            if (toastRoutine != null) StopCoroutine(toastRoutine);
            toastRoutine = StartCoroutine(Toast(text));
        }

        private IEnumerator Toast(string text)
        {
            toast.text = text;
            toast.gameObject.SetActive(true);
            yield return new WaitForSecondsRealtime(2f);
            toast.gameObject.SetActive(false);
            toastRoutine = null;
        }
    }
}
