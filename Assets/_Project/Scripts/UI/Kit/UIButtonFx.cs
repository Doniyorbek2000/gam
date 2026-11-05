using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BilliardGame.UI
{
    /// <summary>
    /// Button states of the design system: normal, pressed (scale 0.95 + light overlay), disabled (dimmed), selected
    /// (outline + accent). Plays the tap sound and haptic on click.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class UIButtonFx : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private const float PressScale = 0.95f;
        private const float PressSeconds = 0.08f;
        private const float DisabledAlpha = 0.45f;

        [SerializeField] private Graphic highlight;
        [SerializeField] private GameObject selectedMark;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private bool strongFeedback;

        private Button button;
        private bool pressed;
        private bool wasInteractable = true;

        public bool Selected { get; private set; }

        private void Awake()
        {
            button = GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(OnClick);
            if (highlight != null) highlight.canvasRenderer.SetAlpha(0f);
            ApplyInteractable(true);
        }

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(OnClick);
        }

        public void Configure(Graphic overlay, GameObject selection, CanvasGroup canvasGroup, bool strong)
        {
            highlight = overlay;
            selectedMark = selection;
            group = canvasGroup;
            strongFeedback = strong;
        }

        public void SetSelected(bool selected)
        {
            Selected = selected;
            if (selectedMark != null) selectedMark.SetActive(selected);
        }

        private void LateUpdate()
        {
            bool interactable = button.IsInteractable();
            if (interactable != wasInteractable) ApplyInteractable(false);
        }

        private void ApplyInteractable(bool instant)
        {
            wasInteractable = button.IsInteractable();
            if (group != null) group.alpha = wasInteractable ? 1f : DisabledAlpha;
            if (!wasInteractable && pressed) Release();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!button.IsInteractable()) return;
            pressed = true;
            Tween.Scale(transform, Vector3.one * PressScale, PressSeconds);
            if (highlight != null) highlight.CrossFadeAlpha(0.18f, PressSeconds, true);
        }

        public void OnPointerUp(PointerEventData eventData) => Release();

        public void OnPointerExit(PointerEventData eventData)
        {
            if (pressed) Release();
        }

        private void Release()
        {
            pressed = false;
            Tween.Scale(transform, Vector3.one, PressSeconds * 1.6f, Ease.OutBack);
            if (highlight != null) highlight.CrossFadeAlpha(0f, PressSeconds * 2f, true);
        }

        private void OnClick()
        {
            if (strongFeedback) UIFeedback.Confirm();
            else UIFeedback.Tap();
        }

        private void OnDisable()
        {
            pressed = false;
            Tween.Kill(transform);
            transform.localScale = Vector3.one;
        }
    }
}
