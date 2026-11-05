using BilliardGame.Content;
using UnityEngine;
using UnityEngine.UI;

namespace BilliardGame.UI
{
    /// <summary>
    /// Full-screen mode select (docs/UI_UX.md §3): six mode cards (three playable, three "coming soon") and the hall
    /// carousel. The choice is applied to the lobby only on "Select".
    /// </summary>
    public sealed class ModeSelectScreen : ScreenView
    {
        [SerializeField] private LobbyController lobby;
        [SerializeField] private GameServices services;
        [SerializeField] private Button[] modeCards = new Button[0];
        [SerializeField] private VenueCardView[] venueCards = new VenueCardView[0];
        [SerializeField] private Button selectButton;

        private LobbyController.Mode pendingMode;
        private VenueDefinition pendingVenue;

        protected override void Awake()
        {
            base.Awake();
            for (int i = 0; i < modeCards.Length; i++)
            {
                var candidate = (LobbyController.Mode)i;
                modeCards[i].onClick.AddListener(() => PickMode(candidate));
            }

            foreach (VenueCardView card in venueCards)
            {
                VenueCardView target = card;
                card.Button.onClick.AddListener(() => PickVenue(target));
            }

            selectButton.onClick.AddListener(Apply);
        }

        protected override void OnShown()
        {
            pendingMode = lobby.SelectedMode;
            pendingVenue = lobby.SelectedVenue;
            Refresh();
        }

        private void PickMode(LobbyController.Mode candidate)
        {
            if (!LobbyController.IsPlayable(candidate))
            {
                Dialogs.Instance.Toast(Loc.Get("common.soon_toast"));
                return;
            }

            pendingMode = candidate;
            Refresh();
        }

        private void PickVenue(VenueCardView card)
        {
            if (!card.Refresh(services.Profile.Level, false))
            {
                Dialogs.Instance.Toast(card.Venue.ComingSoon ? Loc.Get("common.soon_toast") : Loc.Format("mode.min_level", card.Venue.MinLevel));
                Refresh();
                return;
            }

            pendingVenue = card.Venue;
            Refresh();
        }

        private void Apply()
        {
            lobby.Select(pendingMode, pendingVenue);
            ScreenRouter.Instance.Close(this);
        }

        private void Refresh()
        {
            for (int i = 0; i < modeCards.Length; i++) modeCards[i].GetComponent<UIButtonFx>().SetSelected(i == (int)pendingMode);
            int level = services.Profile.Level;
            foreach (VenueCardView card in venueCards) card.Refresh(level, card.Venue == pendingVenue);
        }
    }
}
