using System;
using System.Collections.Generic;
using BilliardGame.Physics;
using UnityEngine;

namespace BilliardGame.Presentation
{
    /// <summary>Replays simulation frames on the ball views, interpolating between fixed steps at render rate.</summary>
    public sealed class TablePlayback : MonoBehaviour
    {
        [SerializeField] private BallView[] balls = Array.Empty<BallView>();
        [SerializeField] private float playbackSpeed = 1f;

        private SimulationResult result;
        private double time;
        private int nextEvent;
        private Action onFinished;

        public bool IsPlaying => result != null;

        /// <summary>Raised when playback reaches a simulation event's time (audio, effects).</summary>
        public event Action<SimulationEvent> EventPlayed;

        /// <summary>Views indexed by ball id.</summary>
        public void Bind(BallView[] views) => balls = views;

        public BallView GetView(int ballId) => balls[ballId];

        public void Snap(IReadOnlyList<BallState> states)
        {
            for (int i = 0; i < states.Count; i++)
            {
                BallState s = states[i];
                balls[s.Id].SetPose(s.Position, s.IsPocketed);
            }
        }

        public void Play(SimulationResult shot, Action finished)
        {
            result = shot ?? throw new ArgumentNullException(nameof(shot));
            onFinished = finished;
            time = 0.0;
            nextEvent = 0;
        }

        private void Update()
        {
            if (result == null) return;

            IReadOnlyList<SimulationFrame> frames = result.Frames;
            time += Time.deltaTime * playbackSpeed;
            double step = frames.Count > 1 ? frames[1].Time - frames[0].Time : 1.0;
            double cursor = time / step;
            int index = (int)cursor;
            RaiseEventsUpTo(time);

            if (index >= frames.Count - 1)
            {
                Finish();
                return;
            }

            IReadOnlyList<BallState> from = frames[index].Balls;
            IReadOnlyList<BallState> to = frames[index + 1].Balls;
            double alpha = cursor - index;
            for (int i = 0; i < from.Count; i++)
            {
                BallState a = from[i];
                BallState b = to[i];
                balls[a.Id].SetPose(a.Position + (b.Position - a.Position) * alpha, b.IsPocketed);
            }
        }

        private void RaiseEventsUpTo(double playbackTime)
        {
            IReadOnlyList<SimulationEvent> events = result.Events;
            while (nextEvent < events.Count && events[nextEvent].Time <= playbackTime)
            {
                EventPlayed?.Invoke(events[nextEvent++]);
            }
        }

        private void Finish()
        {
            RaiseEventsUpTo(double.MaxValue);
            Snap(result.FinalFrame.Balls);
            Action done = onFinished;
            result = null;
            onFinished = null;
            done?.Invoke();
        }
    }
}
