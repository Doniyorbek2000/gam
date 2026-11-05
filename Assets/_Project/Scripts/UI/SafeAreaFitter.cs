using UnityEngine;

namespace BilliardGame.UI
{
    /// <summary>Keeps HUD children out of notches and rounded corners by matching Screen.safeArea.</summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private Rect applied;
        private Vector2Int appliedScreen;

        private void Update()
        {
            Rect safe = Screen.safeArea;
            var screen = new Vector2Int(Screen.width, Screen.height);
            if (safe == applied && screen == appliedScreen) return;
            if (screen.x <= 0 || screen.y <= 0) return;

            var rect = (RectTransform)transform;
            rect.anchorMin = new Vector2(safe.xMin / screen.x, safe.yMin / screen.y);
            rect.anchorMax = new Vector2(safe.xMax / screen.x, safe.yMax / screen.y);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            applied = safe;
            appliedScreen = screen;
        }
    }
}
