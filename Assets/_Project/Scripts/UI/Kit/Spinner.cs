using UnityEngine;

namespace BilliardGame.UI
{
    /// <summary>Loading spinner: steady rotation on unscaled time.</summary>
    public sealed class Spinner : MonoBehaviour
    {
        [SerializeField] private float degreesPerSecond = 300f;

        private void Update() => transform.Rotate(0f, 0f, -degreesPerSecond * Time.unscaledDeltaTime);
    }
}
