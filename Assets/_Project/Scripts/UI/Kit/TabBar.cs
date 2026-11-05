using System;
using UnityEngine;
using UnityEngine.UI;

namespace BilliardGame.UI
{
    /// <summary>Row (or column) of tab buttons; exactly one is selected.</summary>
    public sealed class TabBar : MonoBehaviour
    {
        [SerializeField] private Button[] tabs = new Button[0];

        public int Selected { get; private set; } = -1;
        public int Count => tabs.Length;
        public event Action<int> Changed;

        public void Configure(Button[] buttons) => tabs = buttons;

        private void Awake()
        {
            for (int i = 0; i < tabs.Length; i++)
            {
                int index = i;
                tabs[i].onClick.AddListener(() => Select(index, true));
            }
        }

        public Button Tab(int index) => tabs[index];

        public void Select(int index, bool notify)
        {
            Selected = Mathf.Clamp(index, 0, tabs.Length - 1);
            for (int i = 0; i < tabs.Length; i++)
            {
                var fx = tabs[i].GetComponent<UIButtonFx>();
                if (fx != null) fx.SetSelected(i == Selected);
            }

            if (notify) Changed?.Invoke(Selected);
        }
    }
}
