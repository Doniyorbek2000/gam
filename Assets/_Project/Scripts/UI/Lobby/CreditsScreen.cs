using System.Text;
using TMPro;
using UnityEngine;

namespace BilliardGame.UI
{
    /// <summary>Credits modal, generated from ArtSource/LICENSES.md into Resources/credits.json (docs/ART_PIPELINE.md §4).</summary>
    public sealed class CreditsScreen : ScreenView
    {
        [SerializeField] private TMP_Text body;

        protected override void OnShown() => body.text = Build();

        public static string Build()
        {
            var asset = Resources.Load<TextAsset>(CreditsData.ResourcePath);
            var text = new StringBuilder();
            if (asset == null) return text.Append(Loc.Get("credits.missing")).ToString();

            CreditsData data = JsonUtility.FromJson<CreditsData>(asset.text);
            string group = null;
            foreach (CreditsEntry entry in data.entries)
            {
                if (entry.type != group)
                {
                    group = entry.type;
                    text.Append("\n<b><color=#FFC24A>").Append(Loc.Get("credits.group." + group, group)).Append("</color></b>\n");
                }

                text.Append("• ").Append(entry.attribution).Append('\n');
            }

            text.Append('\n').Append(Loc.Get("credits.thanks"));
            return text.ToString();
        }
    }
}
