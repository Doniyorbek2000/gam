using UnityEngine;
using UnityEngine.UI;

namespace BilliardGame.UI
{
    /// <summary>Horizontal bar: a filled image that eases to its new value.</summary>
    public sealed class ProgressBar : MonoBehaviour
    {
        [SerializeField] private Image fill;

        public float Value { get; private set; }

        public void Configure(Image fillImage) => fill = fillImage;

        public void Set(float value01, bool animate = true)
        {
            float from = fill.fillAmount;
            Value = Mathf.Clamp01(value01);
            float to = Value;
            if (!animate)
            {
                Tween.Kill(fill);
                fill.fillAmount = to;
                return;
            }

            Tween.Run(fill, 0.45f, Ease.OutCubic, t => fill.fillAmount = Mathf.Lerp(from, to, t));
        }

        public void SetColor(Color color) => fill.color = color;
    }
}
