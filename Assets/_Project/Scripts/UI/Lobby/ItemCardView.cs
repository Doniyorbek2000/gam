using System;
using BilliardGame.Content;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BilliardGame.UI
{
    /// <summary>Item tile used by the inventory grid and the crate results: icon, name, rarity, badge, frame colour.</summary>
    public sealed class ItemCardView : MonoBehaviour
    {
        [SerializeField] private Image frame;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text rarity;
        [SerializeField] private TMP_Text badge;
        [SerializeField] private GameObject lockOverlay;
        [SerializeField] private GameObject selectedMark;
        [SerializeField] private Button button;

        public SkinDefinition Skin { get; private set; }

        public void Bind(SkinDefinition skin, string badgeText, bool locked, bool selected, Action<ItemCardView> onClick)
        {
            Skin = skin;
            Color color = RarityStyle.Color(skin.Rarity);
            frame.color = color;
            icon.sprite = skin.Icon;
            icon.enabled = skin.Icon != null;
            icon.color = locked ? new Color(1f, 1f, 1f, 0.35f) : Color.white;
            title.text = skin.DisplayName;
            rarity.text = RarityStyle.Label(skin.Rarity);
            rarity.color = color;
            badge.text = badgeText ?? string.Empty;
            badge.gameObject.SetActive(!string.IsNullOrEmpty(badgeText));
            lockOverlay.SetActive(locked);
            selectedMark.SetActive(selected);
            button.onClick.RemoveAllListeners();
            if (onClick != null) button.onClick.AddListener(() => onClick(this));
            button.interactable = onClick != null;
        }

        public void SetSelected(bool selected) => selectedMark.SetActive(selected);
    }
}
