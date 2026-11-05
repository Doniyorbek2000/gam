using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BilliardGame.UI
{
    /// <summary>Back stack of open screens over the lobby. Android back (Escape) closes the top one.</summary>
    public sealed class ScreenRouter : MonoBehaviour
    {
        [SerializeField] private ScreenView lobbyChrome;

        private readonly List<ScreenView> stack = new List<ScreenView>();

        public static ScreenRouter Instance { get; private set; }

        public ScreenView Top => stack.Count > 0 ? stack[stack.Count - 1] : null;
        public int Depth => stack.Count;

        /// <summary>Raised when the stack changes; the lobby stage uses it to reset its 3D view.</summary>
        public event Action StackChanged;

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Open(ScreenView view)
        {
            if (view == null) return;
            stack.Remove(view);
            stack.Add(view);
            view.Show();
            RefreshChrome();
        }

        public void Close(ScreenView view)
        {
            if (view == null || !stack.Remove(view)) return;
            view.Hide();
            RefreshChrome();
        }

        public bool IsOpen(ScreenView view) => stack.Contains(view);

        /// <summary>Closes everything (match start, returning from a match).</summary>
        public void CloseAll(bool immediate)
        {
            // Copy first: a screen's OnHidden may close other screens.
            var open = new List<ScreenView>(stack);
            stack.Clear();
            for (int i = open.Count - 1; i >= 0; i--)
            {
                if (immediate) open[i].HideImmediate();
                else open[i].Hide();
            }

            RefreshChrome();
        }

        private void RefreshChrome()
        {
            bool covered = false;
            foreach (ScreenView view in stack) covered |= view.HidesLobby;
            if (lobbyChrome != null)
            {
                if (covered && lobbyChrome.IsOpen) lobbyChrome.Hide();
                else if (!covered && !lobbyChrome.IsOpen && lobbyChrome.transform.parent.gameObject.activeInHierarchy) lobbyChrome.Show();
            }

            StackChanged?.Invoke();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame) return;
            ScreenView top = Top;
            if (top != null) top.OnBack();
        }
    }
}
