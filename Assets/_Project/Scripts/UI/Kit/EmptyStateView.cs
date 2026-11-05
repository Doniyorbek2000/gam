using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BilliardGame.UI
{
    /// <summary>"Nothing here yet" block: icon, title, hint and an optional action button.</summary>
    public sealed class EmptyStateView : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text hint;
        [SerializeField] private Button action;
        [SerializeField] private TMP_Text actionLabel;

        public void Configure(Image iconImage, TMP_Text titleText, TMP_Text hintText, Button button, TMP_Text buttonLabel)
        {
            icon = iconImage;
            title = titleText;
            hint = hintText;
            action = button;
            actionLabel = buttonLabel;
        }

        public void Show(Sprite sprite, string titleKey, string hintKey, string actionKey = null, UnityAction onAction = null)
        {
            gameObject.SetActive(true);
            if (sprite != null) icon.sprite = sprite;
            title.text = Loc.Get(titleKey);
            hint.text = Loc.Get(hintKey);
            action.onClick.RemoveAllListeners();
            action.gameObject.SetActive(onAction != null);
            if (onAction == null) return;
            actionLabel.text = Loc.Get(actionKey).ToUpper(Loc.Culture);
            action.onClick.AddListener(onAction);
        }

        public void Hide() => gameObject.SetActive(false);
    }
}
