using System;
using BilliardGame.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BilliardGame.UI
{
    /// <summary>One timed event banner: icon, title, description, boost chip, real countdown, optional "open crates".</summary>
    public sealed class EventCardView : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private Image accent;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text body;
        [SerializeField] private TMP_Text timer;
        [SerializeField] private TMP_Text boost;
        [SerializeField] private GameObject liveTag;
        [SerializeField] private Button action;
        [SerializeField] private Sprite eventSprite;
        [SerializeField] private Sprite crateSprite;
        [SerializeField] private Color liveColor = new Color(1f, 0.76f, 0.29f);
        [SerializeField] private Color soonColor = new Color(0.42f, 0.45f, 0.54f);

        public EventInfo Event { get; private set; }

        public void Bind(EventInfo info, Action openCrates)
        {
            Event = info;
            bool crate = !string.IsNullOrEmpty(info.crate);
            icon.sprite = crate ? crateSprite : eventSprite;
            accent.color = info.active ? liveColor : soonColor;
            title.text = Loc.Get("event." + info.id, info.id).ToUpper(Loc.Culture);
            body.text = Loc.Get("event." + info.id + ".desc", string.Empty);
            string chip = info.coinMultiplier > 1f ? Loc.Format("event.coin_boost", info.coinMultiplier.ToString("0.#", Loc.Culture))
                : info.xpMultiplier > 1f ? Loc.Format("event.xp_boost", info.xpMultiplier.ToString("0.#", Loc.Culture)) : string.Empty;
            boost.gameObject.SetActive(chip.Length > 0);
            boost.text = chip;
            liveTag.SetActive(info.active);
            action.gameObject.SetActive(crate && info.active);
            action.onClick.RemoveAllListeners();
            action.onClick.AddListener(() => openCrates());
        }

        public void Tick(long startsInMs, long endsInMs)
        {
            timer.text = Event.active ? Loc.Format("event.ends", LiveOpsText.Countdown(endsInMs)) : Loc.Format("event.starts", LiveOpsText.Countdown(startsInMs));
        }
    }
}
