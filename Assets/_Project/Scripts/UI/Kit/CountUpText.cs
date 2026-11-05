using TMPro;
using UnityEngine;

namespace BilliardGame.UI
{
    /// <summary>Number that counts to its new value (balances, rewards) instead of jumping.</summary>
    [RequireComponent(typeof(TMP_Text))]
    public sealed class CountUpText : MonoBehaviour
    {
        [SerializeField] private string prefix;
        [SerializeField] private float duration = 0.6f;

        private TMP_Text label;
        private long shown;
        private bool initialised;

        public void Configure(string textPrefix, float seconds)
        {
            prefix = textPrefix;
            duration = seconds;
        }

        public void Set(long value, bool animate = true)
        {
            if (!initialised || !animate || !isActiveAndEnabled || value == shown)
            {
                initialised = true;
                Tween.Kill(this);
                Show(value);
                return;
            }

            long from = shown;
            Tween.Run(this, duration, Ease.OutCubic, t => Show((long)Mathf.Round(Mathf.Lerp(from, value, t))));
            Tween.Scale(transform, Vector3.one * 1.12f, duration * 0.3f, Ease.OutCubic, () => Tween.Scale(transform, Vector3.one, duration * 0.4f, Ease.OutBack));
        }

        /// <summary>Count from <paramref name="from"/> (result screen rewards start at 0).</summary>
        public void CountFrom(long from, long value)
        {
            initialised = true;
            Show(from);
            Set(value);
        }

        private void Show(long value)
        {
            shown = value;
            if (label == null) label = GetComponent<TMP_Text>();
            label.text = prefix + Loc.Number(value);
        }
    }
}
