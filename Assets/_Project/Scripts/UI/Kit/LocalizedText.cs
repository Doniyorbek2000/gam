using TMPro;
using UnityEngine;

namespace BilliardGame.UI
{
    /// <summary>Static label: shows <see cref="Key"/> in the current language and follows language changes.</summary>
    [RequireComponent(typeof(TMP_Text))]
    public sealed class LocalizedText : MonoBehaviour
    {
        [SerializeField] private string key;
        [SerializeField] private bool upperCase;

        private TMP_Text label;

        public string Key
        {
            get => key;
            set
            {
                key = value;
                Refresh();
            }
        }

        private void OnEnable()
        {
            Loc.Changed += Refresh;
            Refresh();
        }

        private void OnDisable() => Loc.Changed -= Refresh;

        public void Refresh()
        {
            if (!Application.isPlaying) return;
            if (label == null) label = GetComponent<TMP_Text>();
            string text = Loc.Get(key);
            label.text = upperCase ? text.ToUpper(Loc.Culture) : text;
        }

        public void Configure(string textKey, bool upper)
        {
            key = textKey;
            upperCase = upper;
        }
    }
}
