using BilliardGame.Content;
using BilliardGame.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BilliardGame.UI
{
    /// <summary>
    /// Character select (docs/UI_UX.md §2, GDD §8): portrait cards preview the character live on the lobby turntable,
    /// Select saves it to the profile, Outfits jumps to the inventory's outfit tab. Purely cosmetic.
    /// </summary>
    public sealed class CharacterScreen : ScreenView
    {
        [SerializeField] private GameServices services;
        [SerializeField] private LobbyStage stage;
        [SerializeField] private InventoryScreen inventory;
        [SerializeField] private Button[] cards = new Button[0];
        [SerializeField] private Image[] portraits = new Image[0];
        [SerializeField] private TMP_Text[] names = new TMP_Text[0];
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private Button selectButton;
        [SerializeField] private TMP_Text selectLabel;
        [SerializeField] private Button outfitsButton;

        private int picked;

        protected override void Awake()
        {
            base.Awake();
            for (int i = 0; i < cards.Length; i++)
            {
                int index = i;
                cards[i].onClick.AddListener(() => Pick(index));
            }

            selectButton.onClick.AddListener(Select);
            outfitsButton.onClick.AddListener(() => inventory.OpenAt(ItemType.Outfit));
        }

        /// <summary>The saved character, or the first one when the profile has none yet.</summary>
        public static CharacterDefinition Saved(GameServices services) =>
            services.Catalog.FindCharacter(services.Profile.State.characterId) ?? (services.Catalog.Characters.Count > 0 ? services.Catalog.Characters[0] : null);

        protected override void OnShown()
        {
            stage.DragFilter = InCentre;
            var characters = services.Catalog.Characters;
            for (int i = 0; i < cards.Length; i++)
            {
                bool used = i < characters.Count;
                cards[i].gameObject.SetActive(used);
                if (!used) continue;
                portraits[i].sprite = characters[i].Portrait;
                names[i].text = DisplayName(characters[i]);
            }

            CharacterDefinition saved = Saved(services);
            Pick(Mathf.Max(0, IndexOf(saved)));
        }

        protected override void OnHidden()
        {
            stage.DragFilter = null;
            stage.SetCharacter(Saved(services));
        }

        private void Pick(int index)
        {
            var characters = services.Catalog.Characters;
            if (index < 0 || index >= characters.Count) return;
            picked = index;
            CharacterDefinition character = characters[index];
            stage.SetCharacter(character);
            for (int i = 0; i < cards.Length; i++) cards[i].GetComponent<UIButtonFx>().SetSelected(i == index);
            nameLabel.text = DisplayName(character);
            bool current = Saved(services) == character;
            selectButton.interactable = !current;
            selectLabel.text = Loc.Get(current ? "character.selected" : "character.select");
        }

        private void Select()
        {
            CharacterDefinition character = services.Catalog.Characters[picked];
            services.Profile.SetCharacter(character.Id);
            Dialogs.Instance.Toast(Loc.Format("character.saved", DisplayName(character)));
            UIFeedback.Confirm();
            Pick(picked);
        }

        private int IndexOf(CharacterDefinition character)
        {
            var characters = services.Catalog.Characters;
            for (int i = 0; i < characters.Count; i++)
            {
                if (characters[i] == character) return i;
            }

            return -1;
        }

        // Proper names: same in every language.
        private static string DisplayName(CharacterDefinition character) => character.DisplayName;

        // Bottom panel and header stay tappable; the middle of the screen spins the turntable.
        private static bool InCentre(Vector2 position) =>
            position.x > Screen.width * 0.25f && position.x < Screen.width * 0.75f && position.y > Screen.height * 0.35f && position.y < Screen.height * 0.85f;
    }
}
