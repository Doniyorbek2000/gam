using UnityEngine;
using UnityEngine.UI;

namespace BilliardGame.UI
{
    /// <summary>Coin / Gem balance pill with a "+" that opens the shop (docs/UI_UX.md §1).</summary>
    public sealed class CurrencyPill : MonoBehaviour
    {
        [SerializeField] private CountUpText value;
        [SerializeField] private Button plus;

        public Button Plus => plus;

        public void Configure(CountUpText amount, Button add)
        {
            value = amount;
            plus = add;
        }

        public void Set(long amount, bool animate = true) => value.Set(amount, animate);
    }
}
