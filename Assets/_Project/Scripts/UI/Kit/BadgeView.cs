using TMPro;
using UnityEngine;

namespace BilliardGame.UI
{
    /// <summary>Red notification dot (docs/UI_UX.md §1): hidden at 0, a plain dot for "something new", or a count.</summary>
    public sealed class BadgeView : MonoBehaviour
    {
        [SerializeField] private TMP_Text count;

        public void Configure(TMP_Text label) => count = label;

        public void Set(int value, bool showNumber = true)
        {
            gameObject.SetActive(value > 0);
            if (count == null) return;
            count.gameObject.SetActive(showNumber);
            count.text = value > 99 ? "99+" : value.ToString();
        }
    }
}
