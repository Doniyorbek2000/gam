using System;

namespace BilliardGame.Presentation
{
    /// <summary>
    /// The visible cue stroke: from the slider's pullback the cue accelerates forward, touches the cue ball and follows
    /// through. The shot (ShotParams) is fixed when the player presses Shoot; physics starts on the contact frame, so the
    /// stroke never changes the result, only when the balls start moving.
    /// </summary>
    public readonly struct StrokeTimeline
    {
        /// <summary>Pullback (0..1 of the cue's max pullback) where the tip touches the ball; negative = past the rest gap.</summary>
        public readonly float ContactPull;
        public readonly float StartPull;
        public readonly float Duration;

        /// <summary>Follow-through past the contact point, in pullback units.</summary>
        public const float FollowThrough = 0.25f;
        public const float SoftDuration = 0.3f;
        public const float HardDuration = 0.12f;

        public StrokeTimeline(float startPull01, float power01, float contactPull)
        {
            StartPull = Math.Max(startPull01, contactPull + 0.05f);
            ContactPull = contactPull;
            Duration = SoftDuration + (HardDuration - SoftDuration) * Clamp01(power01);
        }

        private float EndPull => ContactPull - FollowThrough;

        /// <summary>Pullback at <paramref name="time"/> seconds: accelerating forward (u²), like a real stroke.</summary>
        public float PullAt(float time)
        {
            float u = Clamp01(time / Duration);
            return StartPull - (StartPull - EndPull) * u * u;
        }

        /// <summary>Seconds from the start of the stroke to the tip touching the ball.</summary>
        public float ContactTime => Duration * (float)Math.Sqrt((StartPull - ContactPull) / (StartPull - EndPull));

        public bool IsFinished(float time) => time >= Duration;

        private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
    }
}
