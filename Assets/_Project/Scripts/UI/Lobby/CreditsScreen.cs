using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BilliardGame.UI
{
    /// <summary>Credits, generated from ArtSource/LICENSES.md into Resources/credits.json (docs/ART_PIPELINE.md §4).</summary>
    public sealed class CreditsScreen : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text body;
        [SerializeField] private Button closeButton;

        private void OnEnable() => closeButton.onClick.AddListener(Close);

        private void OnDisable() => closeButton.onClick.RemoveListener(Close);

        public void Open()
        {
            root.SetActive(true);
            body.text = Build();
        }

        public void Close() => root.SetActive(false);

        public static string Build()
        {
            var asset = Resources.Load<TextAsset>(CreditsData.ResourcePath);
            var text = new StringBuilder("<b>BILLIARDS</b> — mualliflar va litsenziyalar\n\n");
            if (asset == null) return text.Append("credits.json topilmadi").ToString();

            CreditsData data = JsonUtility.FromJson<CreditsData>(asset.text);
            string group = null;
            foreach (CreditsEntry entry in data.entries)
            {
                if (entry.type != group)
                {
                    group = entry.type;
                    text.Append("\n<b><color=#FFC24A>").Append(GroupName(group)).Append("</color></b>\n");
                }

                text.Append("• ").Append(entry.attribution).Append('\n');
            }

            text.Append("\nRahmat: Poly Haven, ambientCG, Quaternius, Google Fonts (CC0 / OFL).\nProtsedural modellar, ovozlar va zal geometriyasi — o'z ishimiz.");
            return text.ToString();
        }

        private static string GroupName(string type)
        {
            switch (type)
            {
                case "avatar": return "Personaj va animatsiyalar";
                case "font": return "Shriftlar";
                case "hdri": return "HDRI";
                case "prop": return "Mebel va bezaklar";
                case "texture": return "Teksturalar";
                default: return type;
            }
        }
    }
}
