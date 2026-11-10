using System.Collections.Generic;
using BilliardGame.Meta;
using UnityEngine;

namespace BilliardGame.UI
{
    /// <summary>
    /// Timed events (ROADMAP M3.2): running and upcoming windows from the server's config/events with real countdowns
    /// (docs/ECONOMY.md §4.2: no fake timers). A crate event links to the crate screen.
    /// </summary>
    public sealed class EventsScreen : LiveOpsScreen
    {
        [SerializeField] private RectTransform list;
        [SerializeField] private EventCardView cardTemplate;
        [SerializeField] private GameObject noneLabel;
        [SerializeField] private ScreenView crates;

        private readonly List<EventCardView> cards = new List<EventCardView>();

        protected override void Awake()
        {
            base.Awake();
            cardTemplate.gameObject.SetActive(false);
        }

        protected override void Render()
        {
            EventInfo[] events = LiveOps.State.events;
            while (cards.Count < events.Length) cards.Add(Instantiate(cardTemplate, list));
            for (int i = 0; i < cards.Count; i++)
            {
                bool used = i < events.Length;
                cards[i].gameObject.SetActive(used);
                if (used) cards[i].Bind(events[i], () => ScreenRouter.Instance.Open(crates));
            }

            noneLabel.SetActive(events.Length == 0);
        }

        protected override void Tick()
        {
            foreach (EventCardView card in cards)
            {
                if (card.gameObject.activeSelf) card.Tick(LiveOps.Remaining(card.Event.startsInMs), LiveOps.Remaining(card.Event.endsInMs));
            }
        }
    }
}
