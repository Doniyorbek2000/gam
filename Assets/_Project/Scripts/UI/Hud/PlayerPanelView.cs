using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BilliardGame.UI
{
    /// <summary>Match HUD player panel: avatar + frame, shot-timer ring, name, rank, group pips and the sticker bubble.</summary>
    public sealed class PlayerPanelView : MonoBehaviour
    {
        private const float BubbleSeconds = 2.5f;

        [SerializeField] private TMP_Text playerName;
        [SerializeField] private TMP_Text rank;
        [SerializeField] private TMP_Text group;
        [SerializeField] private Image frame;
        [SerializeField] private Image timerRing;
        [SerializeField] private GameObject activeGlow;
        [SerializeField] private Image[] pips = new Image[0];
        [SerializeField] private RectTransform bubble;
        [SerializeField] private Image bubbleSticker;
        [SerializeField] private CanvasGroup group01;

        private float bubbleUntil;

        public void Configure(TMP_Text nameText, TMP_Text rankText, TMP_Text groupText, Image frameImage, Image ring, GameObject glow, Image[] groupPips,
            RectTransform stickerBubble, Image sticker, CanvasGroup canvasGroup)
        {
            playerName = nameText;
            rank = rankText;
            group = groupText;
            frame = frameImage;
            timerRing = ring;
            activeGlow = glow;
            pips = groupPips;
            bubble = stickerBubble;
            bubbleSticker = sticker;
            group01 = canvasGroup;
        }

        public void Set(string displayName, string rankText, string groupText, int remaining, Color pipColor, bool active)
        {
            playerName.text = displayName;
            rank.text = rankText;
            group.text = groupText;
            for (int i = 0; i < pips.Length; i++)
            {
                pips[i].gameObject.SetActive(remaining >= 0);
                pips[i].color = i < remaining ? pipColor : new Color(1f, 1f, 1f, 0.12f);
            }

            activeGlow.SetActive(active);
            group01.alpha = active ? 1f : 0.7f;
        }

        public void SetFrame(Sprite sprite)
        {
            if (sprite != null) frame.sprite = sprite;
        }

        /// <summary>Shot clock ring: 1 = full; negative hides the countdown (hot-seat has no clock).</summary>
        public void SetTimer(float fraction, Color color)
        {
            timerRing.gameObject.SetActive(fraction >= 0f);
            timerRing.fillAmount = Mathf.Clamp01(fraction);
            timerRing.color = color;
        }

        public void ShowSticker(Sprite sticker)
        {
            if (sticker == null) return;
            bubbleSticker.sprite = sticker;
            bubble.gameObject.SetActive(true);
            bubble.localScale = Vector3.zero;
            Tween.Scale(bubble, Vector3.one, 0.3f, Ease.OutBack);
            bubbleUntil = Time.unscaledTime + BubbleSeconds;
        }

        private void Update()
        {
            if (!bubble.gameObject.activeSelf || Time.unscaledTime < bubbleUntil) return;
            bubbleUntil = float.MaxValue;
            Tween.Scale(bubble, Vector3.zero, 0.2f, Ease.InOutCubic, () => bubble.gameObject.SetActive(false));
        }
    }
}
