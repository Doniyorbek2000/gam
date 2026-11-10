using System;
using BilliardGame.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BilliardGame.UI
{
    /// <summary>One mission: title, reward line, progress bar and count, Claim button and (daily) the free reroll.</summary>
    public sealed class MissionRowView : MonoBehaviour
    {
        [SerializeField] private RowView row;
        [SerializeField] private Button claim;
        [SerializeField] private TMP_Text claimLabel;
        [SerializeField] private Button reroll;
        [SerializeField] private Color activeChip = new Color(1f, 0.76f, 0.29f);
        [SerializeField] private Color readyChip = new Color(0.18f, 0.8f, 0.44f);
        [SerializeField] private Color doneChip = new Color(0.42f, 0.45f, 0.54f);

        public void Bind(MissionInfo mission, string title, bool canReroll, Action onClaim, Action onReroll)
        {
            int shown = Math.Min(mission.progress, mission.target);
            row.Set(title, Loc.Format("missions.reward", Loc.Number(mission.coins), Loc.Number(mission.xp)), shown + "/" + mission.target);
            row.SetProgress(mission.target > 0 ? (float)shown / mission.target : 1f, mission.claimed);
            row.SetChip(mission.claimed ? doneChip : mission.Complete ? readyChip : activeChip);
            claim.gameObject.SetActive(!mission.claimed);
            claim.interactable = mission.Complete;
            claim.GetComponent<CanvasGroup>().alpha = mission.Complete ? 1f : 0.45f;
            claimLabel.text = Loc.Get(mission.Complete ? "missions.claim" : "missions.in_progress").ToUpper(Loc.Culture);
            claim.onClick.RemoveAllListeners();
            claim.onClick.AddListener(() => onClaim());
            reroll.gameObject.SetActive(canReroll);
            reroll.onClick.RemoveAllListeners();
            reroll.onClick.AddListener(() => onReroll());
        }
    }
}
