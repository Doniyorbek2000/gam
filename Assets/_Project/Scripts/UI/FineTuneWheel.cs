using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BilliardGame.UI
{
    /// <summary>Vertical knurled wheel: a full-height drag turns the aim by only a few degrees, for precise cuts.</summary>
    public sealed class FineTuneWheel : MonoBehaviour, IInitializePotentialDragHandler, IDragHandler
    {
        [SerializeField] private RawImage ridges;
        [SerializeField] private float degreesPerWheelHeight = 4f;

        /// <summary>Aim change in degrees, counter-clockwise seen from above.</summary>
        public event Action<float> Rotated;

        public void OnInitializePotentialDrag(PointerEventData eventData) => eventData.useDragThreshold = false;

        public void OnDrag(PointerEventData eventData)
        {
            float wheelPixels = ridges.rectTransform.rect.height * ridges.canvas.scaleFactor;
            if (wheelPixels <= 0f) return;

            float turns = eventData.delta.y / wheelPixels;
            Rect uv = ridges.uvRect;
            uv.y -= turns * uv.height;   // ridges follow the finger
            ridges.uvRect = uv;
            Rotated?.Invoke(-turns * degreesPerWheelHeight);
        }
    }
}
