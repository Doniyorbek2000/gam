using UnityEngine;
using UnityEngine.EventSystems;

namespace BilliardGame.UI
{
    /// <summary>Cue-ball face: tap or drag to pick where the tip strikes. Spin is in the unit disc (x right, y top).</summary>
    public sealed class SpinPad : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        [SerializeField] private RectTransform area;
        [SerializeField] private RectTransform dot;
        [SerializeField] private RectTransform previewArea;
        [SerializeField] private RectTransform previewDot;

        public Vector2 Spin { get; private set; }

        public void OnPointerDown(PointerEventData eventData) => SetFrom(eventData);

        public void OnDrag(PointerEventData eventData) => SetFrom(eventData);

        public void ResetSpin() => Apply(Vector2.zero);

        private void SetFrom(PointerEventData eventData)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(area, eventData.position, eventData.pressEventCamera, out Vector2 local))
            {
                Apply(local / (area.rect.width * 0.5f));
            }
        }

        private void Apply(Vector2 spin)
        {
            Spin = Vector2.ClampMagnitude(spin, 1f);
            dot.anchoredPosition = Spin * (area.rect.width * 0.5f - dot.rect.width * 0.5f);
            if (previewArea != null && previewDot != null) previewDot.anchoredPosition = Spin * (previewArea.rect.width * 0.5f - previewDot.rect.width * 0.5f);
        }
    }
}
