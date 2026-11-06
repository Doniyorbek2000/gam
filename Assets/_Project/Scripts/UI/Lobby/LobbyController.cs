using System;
using System.Threading.Tasks;
using BilliardGame.Content;
using BilliardGame.Meta;
using BilliardGame.Online;
using BilliardGame.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BilliardGame.UI
{
    /// <summary>
    /// PUBG-style lobby (docs/UI_UX.md §2): profile card, currency pills, left/right rails, mode + hall card and PLAY.
    /// Owns the selected mode and hall, switches between the lobby camera and the match camera and loads the hall.
    /// </summary>
    public sealed class LobbyController : MonoBehaviour
    {
        public enum Mode
        {
            HotSeat = 0,
            Friend = 1,
            Quick = 2,
            Ranked = 3,
            Ai = 4,
            Practice = 5,
        }

        private const string ModeKey = "lobby_mode";
        private const string VenueKey = "lobby_venue_id";
        private const string DefaultVenue = "silk_road_lounge";
        private static readonly string[] ModeKeys = { "mode.hotseat", "mode.friend", "mode.quick", "mode.ranked", "mode.ai", "mode.practice" };

        [SerializeField] private GameServices services;
        [SerializeField] private MainMenuController menu;
        [SerializeField] private OnlineMatchController online;
        [SerializeField] private GameController game;
        [SerializeField] private HudController hud;
        [SerializeField] private VenueLoader venues;
        [SerializeField] private LobbyStage stage;
        [SerializeField] private Camera matchCamera;

        [Header("Screens")]
        [SerializeField] private ScreenView chrome;
        [SerializeField] private ModeSelectScreen modeSelect;
        [SerializeField] private ProfileScreen profileScreen;
        [SerializeField] private InventoryScreen inventory;
        [SerializeField] private MyRoomScreen room;
        [SerializeField] private SettingsScreen settings;
        [SerializeField] private CrateScreen crates;

        [Header("Profile card")]
        [SerializeField] private Button profileButton;
        [SerializeField] private Image avatarFrame;
        [SerializeField] private TMP_Text nicknameLabel;
        [SerializeField] private TMP_Text levelLabel;
        [SerializeField] private ProgressBar xpBar;
        [SerializeField] private TMP_Text rankLabel;

        [Header("Top right")]
        [SerializeField] private CurrencyPill coins;
        [SerializeField] private CurrencyPill gems;
        [SerializeField] private Button mailButton;
        [SerializeField] private Button friendsButton;
        [SerializeField] private Button settingsButton;

        [Header("Rails")]
        [SerializeField] private Button passButton;
        [SerializeField] private Button missionsButton;
        [SerializeField] private Button eventsButton;
        [SerializeField] private Button dailyButton;
        [SerializeField] private Button shopButton;
        [SerializeField] private Button cratesButton;
        [SerializeField] private Button inventoryButton;
        [SerializeField] private BadgeView inventoryBadge;
        [SerializeField] private Button roomButton;
        [SerializeField] private Button clubButton;
        [SerializeField] private Button chatButton;

        [Header("Mode card + play")]
        [SerializeField] private Button modeCard;
        [SerializeField] private Image venueImage;
        [SerializeField] private TMP_Text modeLabel;
        [SerializeField] private TMP_Text venueLabel;
        [SerializeField] private TMP_Text entryLabel;
        [SerializeField] private TMP_Text prizeLabel;
        [SerializeField] private Button playButton;

        private Mode mode;
        private VenueDefinition venue;

        public Mode SelectedMode => mode;
        public VenueDefinition SelectedVenue => venue;
        public static bool IsPlayable(Mode candidate) => candidate <= Mode.Quick;
        public static string ModeName(Mode candidate) => Loc.Get(ModeKeys[(int)candidate]);
        public static string ModeKeyOf(Mode candidate) => ModeKeys[(int)candidate];

        private void Awake()
        {
            int saved = PlayerPrefs.GetInt(ModeKey, 0);
            mode = IsPlayable((Mode)saved) ? (Mode)saved : Mode.HotSeat;
            venue = FindVenue(PlayerPrefs.GetString(VenueKey, DefaultVenue)) ?? FindVenue(DefaultVenue) ?? services.Catalog.Venues[0];

            // Dev/E2E: "-venue silk_road_lounge" picks the hall without touching the UI.
            string[] args = Environment.GetCommandLineArgs();
            int venueArg = Array.IndexOf(args, "-venue");
            if (venueArg >= 0 && venueArg + 1 < args.Length) venue = FindVenue(args[venueArg + 1]) ?? venue;
        }

        private void OnEnable()
        {
            services.Economy.Changed += RefreshBalances;
            services.Profile.Changed += RefreshProfile;
            Loc.Changed += RefreshAll;
            menu.MenuShown += OnMenuShown;
            online.MatchStarted += OnOnlineMatchStarted;
            hud.AgainRequested += PlayAgain;

            Bind(profileButton, () => Open(profileScreen));
            Bind(coins.Plus, () => Open(crates));
            Bind(gems.Plus, () => Open(crates));
            Bind(settingsButton, () => Open(settings));
            Bind(shopButton, () => Open(crates));
            Bind(cratesButton, () => Open(crates));
            Bind(inventoryButton, () => Open(inventory));
            Bind(roomButton, () => Open(room));
            Bind(modeCard, () => Open(modeSelect));
            Bind(playButton, Play);
            foreach (Button soon in new[] { mailButton, friendsButton, passButton, missionsButton, eventsButton, dailyButton, clubButton, chatButton }) Bind(soon, ComingSoon);
        }

        private void OnDisable()
        {
            services.Economy.Changed -= RefreshBalances;
            services.Profile.Changed -= RefreshProfile;
            Loc.Changed -= RefreshAll;
            menu.MenuShown -= OnMenuShown;
            online.MatchStarted -= OnOnlineMatchStarted;
            hud.AgainRequested -= PlayAgain;
            foreach (Button b in new[] { profileButton, coins.Plus, gems.Plus, settingsButton, shopButton, cratesButton, inventoryButton, roomButton, modeCard, playButton,
                         mailButton, friendsButton, passButton, missionsButton, eventsButton, dailyButton, clubButton, chatButton })
            {
                b.onClick.RemoveAllListeners();
            }
        }

        private void Start()
        {
            coins.Set(services.Economy.Coins, false);
            gems.Set(services.Economy.Gems, false);
            RefreshAll();
        }

        private static void Bind(Button button, UnityEngine.Events.UnityAction action) => button.onClick.AddListener(action);

        private static void Open(ScreenView view) => ScreenRouter.Instance.Open(view);

        public void Select(Mode selected, VenueDefinition hall)
        {
            if (IsPlayable(selected)) mode = selected;
            if (hall != null && !hall.ComingSoon) venue = hall;
            PlayerPrefs.SetInt(ModeKey, (int)mode);
            PlayerPrefs.SetString(VenueKey, venue.Id);
            RefreshModeCard();
        }

        public void SelectVenue(string venueId) => Select(mode, FindVenue(venueId));

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
                    game.SetPlayerNames(services.Profile.State.nickname, Loc.Get("hud.guest"));
                    break;
                case Mode.Friend:
                    menu.ShowFriendPanel();
                    break;
                case Mode.Quick:
                    // Entry is shown here and charged by the server when the match starts (M2); the mock only checks it.
                    if (services.Economy.Coins < venue.EntryFee)
                    {
                        Dialogs.Instance.Toast(Loc.Get("lobby.no_coins"));
                        return;
                    }

                    menu.QuickMatch();
                    break;
                default:
                    ComingSoon();
                    break;
            }
        }

        /// <summary>Result screen ▸ Play again: hot-seat re-racks in the same hall, online goes back to matchmaking.</summary>
        private void PlayAgain()
        {
            if (!game.IsOnline)
            {
                game.NewGame();
                return;
            }

            menu.ReturnToMenu();
            Play();
        }

        private async void OnOnlineMatchStarted() => await EnterVenue();

        private async Task EnterVenue()
        {
            hud.SetMatchContext(venue);
            await venues.LoadAsync(venue);
            SetLobbyView(false);
        }

        private async void OnMenuShown()
        {
            SetLobbyView(true);
            stage.ShowCharacter();
            RefreshAll();
            await venues.UnloadAsync();
        }

        private void SetLobbyView(bool lobby)
        {
            stage.Camera.enabled = lobby;
            stage.gameObject.SetActive(lobby);
            matchCamera.enabled = !lobby;
            GameSettings.ApplyCamera(lobby ? stage.Camera : matchCamera);
        }

        private void RefreshAll()
        {
            RefreshProfile();
            RefreshBalances();
            RefreshModeCard();
        }

        private void RefreshBalances()
        {
            coins.Set(services.Economy.Coins);
            gems.Set(services.Economy.Gems);
            inventoryBadge.Set(InventoryScreen.NewCount(services));
            RefreshFrame();
        }

        private void RefreshProfile()
        {
            ProfileState profile = services.Profile.State;
            int level = ProfileRules.Level(profile.xp, out int into, out int needed);
            nicknameLabel.text = profile.nickname;
            levelLabel.text = level.ToString(Loc.Culture);
            xpBar.Set(needed > 0 ? (float)into / needed : 1f);
            rankLabel.text = RankText(profile.rankPoints);
            RefreshFrame();
        }

        private void RefreshFrame()
        {
            SkinDefinition frame = services.Equipped(ItemType.Frame);
            if (frame == null || frame.Icon == null) return;
            avatarFrame.sprite = frame.Icon;
            avatarFrame.color = Color.white;
        }

        private void RefreshModeCard()
        {
            modeLabel.text = (Loc.Get("mode.eight_ball") + " · " + ModeName(mode)).ToUpper(Loc.Culture);
            venueLabel.text = VenueName(venue);
            venueImage.sprite = venue.Preview;
            bool stake = mode == Mode.Quick;
            entryLabel.text = stake ? Loc.Number(venue.EntryFee) : Loc.Get("mode.no_stake");
            prizeLabel.text = stake ? Loc.Number(venue.Prize) : "—";
        }

        public static string VenueName(VenueDefinition hall) => Loc.Get("venue." + hall.Id, hall.DisplayName);

        public static string RankText(int rankPoints)
        {
            RankTier tier = ProfileRules.Rank(rankPoints, out int division);
            string name = Loc.Get("rank." + tier.ToString().ToLowerInvariant());
            return division > 0 ? $"{name} {Roman(division)}" : name;
        }

        private static string Roman(int value)
        {
            switch (value)
            {
                case 1: return "I";
                case 2: return "II";
                case 3: return "III";
                case 4: return "IV";
                case 5: return "V";
                default: return value.ToString(Loc.Culture);
            }
        }

        private VenueDefinition FindVenue(string id)
        {
            foreach (VenueDefinition candidate in services.Catalog.Venues)
            {
                if (candidate.Id == id && !candidate.ComingSoon) return candidate;
            }

            return null;
        }

        private static void ComingSoon() => Dialogs.Instance.Toast(Loc.Get("common.soon_toast"));
    }
}
