using UnityEngine;
using UnityEngine.UI;

namespace BilliardGame.UI
{
    /// <summary>Idle "breathing" of a call-to-action (PLAY, new crate) plus an optional glow alpha pulse.</summary>
    public sealed class PulseFx : MonoBehaviour
    {
        [SerializeField] private float scale = 0.04f;
        [SerializeField] private float period = 1.6f;
        [SerializeField] private Graphic glow;
        [SerializeField] private float glowMin = 0.2f;
        [SerializeField] private float glowMax = 0.85f;

        public void Configure(float amplitude, float seconds, Graphic glowGraphic)
        {
            scale = amplitude;
            period = seconds;
            glow = glowGraphic;
        }

        private void Update()
        {
            if (Tween.Instant) return;
            float wave = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f / period);
            transform.localScale = Vector3.one * (1f + scale * wave);
            if (glow == null) return;
            Color c = glow.color;
            c.a = Mathf.Lerp(glowMin, glowMax, wave);
            glow.color = c;
        }

        private void OnDisable() => transform.localScale = Vector3.one;
    }
}
