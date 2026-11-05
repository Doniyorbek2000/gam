using BilliardGame.Content;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BilliardGame.UI
{
    /// <summary>Hall card in the mode-select carousel: picture, name, entry, prize and the lock (level / coming soon).</summary>
    public sealed class VenueCardView : MonoBehaviour
    {
        [SerializeField] private VenueDefinition venue;
        [SerializeField] private Image picture;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text entry;
        [SerializeField] private TMP_Text prize;
        [SerializeField] private GameObject lockOverlay;
        [SerializeField] private TMP_Text lockText;
        [SerializeField] private Button button;
        [SerializeField] private UIButtonFx fx;

        public VenueDefinition Venue => venue;
        public Button Button => button;

        public void Configure(VenueDefinition hall, Image image, TMP_Text name, TMP_Text entryText, TMP_Text prizeText, GameObject locked, TMP_Text lockLabel, Button tap, UIButtonFx effects)
        {
            venue = hall;
            picture = image;
            title = name;
            entry = entryText;
            prize = prizeText;
            lockOverlay = locked;
            lockText = lockLabel;
            button = tap;
            fx = effects;
        }

        /// <summary>True when the player may pick this hall at <paramref name="level"/>.</summary>
        public bool Refresh(int level, bool selected)
        {
            picture.sprite = venue.Preview;
            title.text = LobbyController.VenueName(venue);
            entry.text = Loc.Number(venue.EntryFee);
            prize.text = Loc.Number(venue.Prize);
            bool open = !venue.ComingSoon && level >= venue.MinLevel;
            lockOverlay.SetActive(!open);
            lockText.text = venue.ComingSoon ? Loc.Get("common.soon").ToUpper(Loc.Culture) : Loc.Format("mode.min_level", venue.MinLevel);
            fx.SetSelected(selected && open);
            return open;
        }
    }
}
