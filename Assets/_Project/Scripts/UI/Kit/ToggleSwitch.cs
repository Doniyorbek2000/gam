using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BilliardGame.UI
{
    /// <summary>Switch control: pill track, sliding knob, accent colour when on.</summary>
    public sealed class ToggleSwitch : MonoBehaviour, IPointerClickHandler
    {
        private const float Travel = 22f;

        [SerializeField] private Image track;
        [SerializeField] private RectTransform knob;
        [SerializeField] private Color onColor;
        [SerializeField] private Color offColor;

        public bool IsOn { get; private set; }
        public event Action<bool> Changed;

        public void Configure(Image trackImage, RectTransform knobRect, Color on, Color off)
        {
            track = trackImage;
            knob = knobRect;
            onColor = on;
            offColor = off;
        }

        public void Set(bool on, bool notify = false)
        {
            IsOn = on;
            Tween.Move(knob, new Vector2(on ? Travel : -Travel, 0f), 0.15f);
            Color from = track.color, to = on ? onColor : offColor;
            Tween.Run(track, 0.15f, Ease.OutCubic, t => track.color = Color.Lerp(from, to, t));
            if (notify) Changed?.Invoke(on);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            UIFeedback.Tap();
            Set(!IsOn, true);
        }
    }
}
