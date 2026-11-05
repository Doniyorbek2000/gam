using System;
using UnityEngine;

namespace BilliardGame.UI
{
    public enum Transition
    {
        Fade,
        SlideLeft,
        SlideUp,
        Pop,
    }

    /// <summary>
    /// A full screen, modal or sheet: fades/slides in over 200–300 ms and blocks input while hidden. Subclasses override
    /// <see cref="OnShown"/>/<see cref="OnHidden"/>; <see cref="ScreenRouter"/> keeps the back stack.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class ScreenView : MonoBehaviour
    {
        private const float SlideDistance = 120f;

        [SerializeField] private Transition transition = Transition.Fade;
        [SerializeField] private RectTransform content;
        [SerializeField] private float duration = 0.25f;
        [SerializeField] private bool hidesLobby = true;
        [Tooltip("Header back button; closes this screen through the router.")]
        [SerializeField] private UnityEngine.UI.Button backButton;
        [Tooltip("The lobby chrome stays under the screens instead of jumping to the front when shown.")]
        [SerializeField] private bool keepSiblingOrder;

        private CanvasGroup group;
        private Vector2 home;
        private bool homeKnown;

        public bool IsOpen { get; private set; }
        /// <summary>Full screens hide the lobby chrome underneath; modals and sheets keep it visible behind the scrim.</summary>
        public bool HidesLobby => hidesLobby;

        public event Action Opened;
        public event Action Closed;

        protected CanvasGroup Group => group != null ? group : group = GetComponent<CanvasGroup>();

        public void ConfigureView(Transition kind, RectTransform animated, float seconds, bool hidesLobbyChrome, bool keepOrder = false)
        {
            transition = kind;
            content = animated;
            duration = seconds;
            hidesLobby = hidesLobbyChrome;
            keepSiblingOrder = keepOrder;
        }

        public void BindBack(UnityEngine.UI.Button button) => backButton = button;

        protected virtual void Awake()
        {
            if (backButton != null) backButton.onClick.AddListener(() => OnBack());
        }

        public void Show()
        {
            RectTransform target = Target();
            if (!homeKnown)
            {
                home = target.anchoredPosition;
                homeKnown = true;
            }

            gameObject.SetActive(true);
            if (!keepSiblingOrder) transform.SetAsLastSibling();
            IsOpen = true;
            Group.blocksRaycasts = true;
            Group.interactable = true;
            OnShown();
            Opened?.Invoke();

            Group.alpha = 0f;
            Tween.Fade(Group, 1f, duration);
            switch (transition)
            {
                case Transition.SlideLeft:
                    target.anchoredPosition = home + new Vector2(SlideDistance, 0f);
                    Tween.Move(target, home, duration, Ease.OutQuint);
                    break;
                case Transition.SlideUp:
                    target.anchoredPosition = home - new Vector2(0f, target.rect.height > 0f ? target.rect.height : SlideDistance * 4f);
                    Tween.Move(target, home, duration, Ease.OutQuint);
                    break;
                case Transition.Pop:
                    target.localScale = Vector3.one * 0.9f;
                    Tween.Scale(target, Vector3.one, duration, Ease.OutBack);
                    break;
            }
        }

        public void Hide()
        {
            if (!IsOpen) return;
            IsOpen = false;
            Group.blocksRaycasts = false;
            Group.interactable = false;
            OnHidden();
            Closed?.Invoke();
            RectTransform target = Target();
            float seconds = duration * 0.7f;
            if (transition == Transition.SlideUp) Tween.Move(target, home - new Vector2(0f, target.rect.height), seconds, Ease.InOutCubic);
            if (transition == Transition.Pop) Tween.Scale(target, Vector3.one * 0.95f, seconds);
            Tween.Fade(Group, 0f, seconds, () =>
            {
                if (IsOpen) return;
                gameObject.SetActive(false);
                target.anchoredPosition = home;
                target.localScale = Vector3.one;
            });
        }

        /// <summary>Hides without animation (scene start, leaving to a match).</summary>
        public void HideImmediate()
        {
            bool wasOpen = IsOpen;
            IsOpen = false;
            Group.alpha = 0f;
            Group.blocksRaycasts = false;
            // Screens that own 3D state (My Room camera, inventory preview texture) restore it in OnHidden.
            if (wasOpen)
            {
                OnHidden();
                Closed?.Invoke();
            }

            gameObject.SetActive(false);
        }

        /// <summary>Android back / Escape. Return false to ignore it (e.g. during a crate reveal).</summary>
        public virtual bool OnBack()
        {
            ScreenRouter.Instance.Close(this);
            return true;
        }

        protected virtual void OnShown()
        {
        }

        protected virtual void OnHidden()
        {
        }

        private RectTransform Target() => content != null ? content : (RectTransform)transform;
    }
}
