using System;
using System.Collections.Generic;
using UnityEngine;

namespace BilliardGame.UI
{
    public enum Ease
    {
        Linear,
        OutCubic,
        InOutCubic,
        OutBack,
        OutQuint,
    }

    /// <summary>
    /// Tiny tween runner for UI motion (screen transitions, modals, count-ups, pulses). Own code instead of a package: we need
    /// five easings on unscaled time, and every tween is keyed by owner so a new tween on the same key replaces the old one.
    /// </summary>
    public sealed class Tween : MonoBehaviour
    {
        private struct Job
        {
            public object Key;
            public float Start;
            public float Duration;
            public Ease Ease;
            public Action<float> Step;
            public Action Done;
        }

        private static Tween runner;
        private readonly List<Job> jobs = new List<Job>(32);

        /// <summary>Global switch from Settings ▸ "reduce motion" style options: tweens jump to their end.</summary>
        public static bool Instant { get; set; }

        private static Tween Runner
        {
            get
            {
                if (runner != null) return runner;
                var go = new GameObject("[Tween]") { hideFlags = HideFlags.HideInHierarchy };
                DontDestroyOnLoad(go);
                runner = go.AddComponent<Tween>();
                return runner;
            }
        }

        /// <summary>Runs <paramref name="step"/>(eased 0→1) over <paramref name="duration"/> s; replaces any tween with the same key.</summary>
        public static void Run(object key, float duration, Ease ease, Action<float> step, Action done = null)
        {
            if (!Application.isPlaying || Instant || duration <= 0f)
            {
                Kill(key);
                step(1f);
                done?.Invoke();
                return;
            }

            Tween t = Runner;
            t.Remove(key);
            t.jobs.Add(new Job { Key = key, Start = Time.unscaledTime, Duration = duration, Ease = ease, Step = step, Done = done });
            step(0f);
        }

        public static void Kill(object key)
        {
            if (runner != null) runner.Remove(key);
        }

        public static float Evaluate(Ease ease, float t)
        {
            t = Mathf.Clamp01(t);
            switch (ease)
            {
                case Ease.OutCubic: return 1f - Mathf.Pow(1f - t, 3f);
                case Ease.InOutCubic: return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
                case Ease.OutQuint: return 1f - Mathf.Pow(1f - t, 5f);
                case Ease.OutBack:
                    const float c1 = 1.70158f, c3 = c1 + 1f;
                    return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
                default: return t;
            }
        }

        // Common UI helpers.
        public static void Fade(CanvasGroup group, float to, float duration, Action done = null)
        {
            float from = group.alpha;
            Run(group, duration, Ease.OutCubic, t => group.alpha = Mathf.LerpUnclamped(from, to, t), done);
        }

        public static void Scale(Transform target, Vector3 to, float duration, Ease ease = Ease.OutCubic, Action done = null)
        {
            Vector3 from = target.localScale;
            Run(target, duration, ease, t => target.localScale = Vector3.LerpUnclamped(from, to, t), done);
        }

        public static void Move(RectTransform target, Vector2 to, float duration, Ease ease = Ease.OutCubic, Action done = null)
        {
            Vector2 from = target.anchoredPosition;
            Run((target, "move"), duration, ease, t => target.anchoredPosition = Vector2.LerpUnclamped(from, to, t), done);
        }

        private void Remove(object key)
        {
            for (int i = jobs.Count - 1; i >= 0; i--)
            {
                if (Equals(jobs[i].Key, key)) jobs.RemoveAt(i);
            }
        }

        private void Update()
        {
            float now = Time.unscaledTime;
            for (int i = jobs.Count - 1; i >= 0; i--)
            {
                if (i >= jobs.Count) continue;
                Job job = jobs[i];
                float t = (now - job.Start) / job.Duration;
                bool finished = t >= 1f;
                try
                {
                    // A destroyed UI object throws on access; drop its tween instead of spamming every frame.
                    job.Step(Evaluate(job.Ease, finished ? 1f : t));
                }
                catch (MissingReferenceException)
                {
                    jobs.RemoveAt(i);
                    continue;
                }

                if (!finished) continue;
                jobs.RemoveAt(i);
                job.Done?.Invoke();
            }
        }
    }
}
