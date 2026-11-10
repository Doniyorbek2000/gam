using System;
using BilliardGame.Content;
using BilliardGame.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BilliardGame.UI
{
    /// <summary>One Season Pass level: number plate, free reward on top, premium reward below.</summary>
    public sealed class PassColumnView : MonoBehaviour
    {
        [SerializeField] private TMP_Text levelLabel;
        [SerializeField] private Image plate;
        [SerializeField] private RewardTileView free;
        [SerializeField] private RewardTileView premium;
        [SerializeField] private Color reachedPlate = new Color(1f, 0.76f, 0.29f);
        [SerializeField] private Color lockedPlate = new Color(0.12f, 0.15f, 0.22f);

        public int Level { get; private set; }

        public void Bind(PassLevelInfo info, PassInfo pass, SkinCatalog catalog, Action<int, bool> onTap)
        {
            Level = info.level;
            bool reached = info.level <= pass.level;
            levelLabel.text = info.level.ToString(Loc.Culture);
            plate.color = reached ? reachedPlate : lockedPlate;
            levelLabel.color = reached ? new Color(0.1f, 0.07f, 0.02f) : Color.white;
            free.Bind(info.free, catalog, !reached, info.freeClaimed, reached && !info.freeClaimed);
            premium.Bind(info.premium, catalog, !reached || !pass.premium, info.premiumClaimed, reached && pass.premium && !info.premiumClaimed);
            free.Button.onClick.RemoveAllListeners();
            free.Button.onClick.AddListener(() => onTap(info.level, false));
            premium.Button.onClick.RemoveAllListeners();
            premium.Button.onClick.AddListener(() => onTap(info.level, true));
        }
    }
}
