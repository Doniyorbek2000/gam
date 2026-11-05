using System;
using BilliardGame.Content;
using BilliardGame.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BilliardGame.UI
{
    /// <summary>
    /// Inventory / reward tile: rarity frame and gradient, icon, name, rarity pips (shape as well as colour), "NEW" and
    /// "equipped" badges, lock overlay, selected outline and a glow for Epic and above.
    /// </summary>
    public sealed class ItemTile : MonoBehaviour
    {
        [SerializeField] private Image frame;
        [SerializeField] private Image gradient;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text title;
        [SerializeField] private Image[] pips = new Image[0];
        [SerializeField] private GameObject newBadge;
        [SerializeField] private GameObject equippedBadge;
        [SerializeField] private GameObject lockOverlay;
        [SerializeField] private GameObject selectedMark;
        [SerializeField] private Image glow;
        [SerializeField] private TMP_Text note;
        [SerializeField] private Button button;

        public SkinDefinition Item { get; private set; }
        public Button Button => button;

        public void Configure(Image frameImage, Image gradientImage, Image iconImage, TMP_Text name, Image[] rarityPips, GameObject isNew,
            GameObject isEquipped, GameObject locked, GameObject selected, Image glowImage, TMP_Text noteText, Button tap)
        {
            frame = frameImage;
            gradient = gradientImage;
            icon = iconImage;
            title = name;
            pips = rarityPips;
            newBadge = isNew;
            equippedBadge = isEquipped;
            lockOverlay = locked;
            selectedMark = selected;
            glow = glowImage;
            note = noteText;
            button = tap;
        }

        public void Bind(SkinDefinition item, bool owned, bool equipped, bool isNew, bool selected, Action<ItemTile> onClick, string noteText = null)
        {
            Item = item;
            Color color = RarityStyle.Color(item.Rarity);
            frame.color = color;
            gradient.color = new Color(color.r, color.g, color.b, 0.55f);
            icon.sprite = item.Icon;
            icon.enabled = item.Icon != null;
            icon.color = owned ? Color.white : new Color(1f, 1f, 1f, 0.35f);
            title.text = RarityStyle.ItemName(item);
            for (int i = 0; i < pips.Length; i++)
            {
                pips[i].gameObject.SetActive(i <= (int)item.Rarity);
                pips[i].color = color;
            }

            newBadge.SetActive(isNew && owned);
            equippedBadge.SetActive(equipped);
            lockOverlay.SetActive(!owned);
            selectedMark.SetActive(selected);
            glow.gameObject.SetActive(item.Rarity >= Rarity.Epic);
            glow.color = new Color(color.r, color.g, color.b, 0.5f);
            note.gameObject.SetActive(!string.IsNullOrEmpty(noteText));
            note.text = noteText ?? string.Empty;
            button.onClick.RemoveAllListeners();
            if (onClick != null) button.onClick.AddListener(() => onClick(this));
            button.interactable = onClick != null;
        }

        public void SetSelected(bool selected) => selectedMark.SetActive(selected);

        /// <summary>Reveal pop for crate results: tiles appear one after another.</summary>
        public void PlayReveal(float delay)
        {
            transform.localScale = Vector3.zero;
            float start = Time.unscaledTime + delay;
            Tween.Run(this, 0.35f + delay, Ease.Linear, t =>
            {
                float local = Mathf.Clamp01((Time.unscaledTime - start) / 0.35f);
                transform.localScale = Vector3.one * Tween.Evaluate(Ease.OutBack, local);
            });
        }
    }
}
