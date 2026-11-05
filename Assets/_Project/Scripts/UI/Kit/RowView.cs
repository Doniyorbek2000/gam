using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BilliardGame.UI
{
    /// <summary>
    /// One list row (match history, achievement, collection category): icon, title, subtitle, value, optional progress
    /// bar, "done" check and an accent chip. Missing parts are simply left null by the builder.
    /// </summary>
    public sealed class RowView : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private Image chip;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text subtitle;
        [SerializeField] private TMP_Text value;
        [SerializeField] private ProgressBar bar;
        [SerializeField] private GameObject check;
        [SerializeField] private Button button;
        [SerializeField] private string id;

        /// <summary>Static id the builder gave the row (achievement id, item type name).</summary>
        public string Id => id;
        public Button Button => button;
        public Sprite IconSprite => icon != null ? icon.sprite : null;

        public void Configure(string rowId, Image iconImage, Image chipImage, TMP_Text titleText, TMP_Text subtitleText, TMP_Text valueText, ProgressBar progress, GameObject done, Button tap)
        {
            id = rowId;
            icon = iconImage;
            chip = chipImage;
            title = titleText;
            subtitle = subtitleText;
            value = valueText;
            bar = progress;
            check = done;
            button = tap;
        }

        public void Set(string titleText, string subtitleText, string valueText)
        {
            if (title != null) title.text = titleText ?? string.Empty;
            if (subtitle != null) subtitle.text = subtitleText ?? string.Empty;
            if (value != null) value.text = valueText ?? string.Empty;
        }

        public void SetProgress(float value01, bool done)
        {
            if (bar != null) bar.Set(value01, false);
            if (check != null) check.SetActive(done);
        }

        public void SetIcon(Sprite sprite, Color color)
        {
            if (icon == null) return;
            if (sprite != null) icon.sprite = sprite;
            icon.color = color;
        }

        public void SetChip(Color color)
        {
            if (chip != null) chip.color = color;
        }
    }
}
