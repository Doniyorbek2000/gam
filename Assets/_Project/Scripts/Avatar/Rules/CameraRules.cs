using BilliardGame.Physics;

namespace BilliardGame.Avatar.Rules
{
    /// <summary>Match camera modes (docs/GDD.md §8.1, ROADMAP M1.6 item 4). Purely visual.</summary>
    public enum MatchCameraMode
    {
        Classic = 0,
        ThirdPerson = 1,
        FirstPerson = 2,
        Cinematic = 3,
    }

    /// <summary>Camera geometry in table coordinates; no UnityEngine, so it is EditMode-testable.</summary>
    public static class CameraRules
    {
        public const int ModeCount = 4;

        /// <summary>Over-the-shoulder: behind the cue ball along the cue, above the shooter's head line.</summary>
        public const double ThirdBack = 1.05;
        public const double ThirdHeight = 0.55;

        /// <summary>Eye on the cue line: low and close, looking down the shot.</summary>
        public const double FirstBack = 0.6;
        public const double FirstHeight = 0.2;

        public static MatchCameraMode Next(MatchCameraMode mode) => (MatchCameraMode)(((int)mode + 1) % ModeCount);

        /// <summary>Aiming from behind the ball (and swiping to turn the cue) everywhere but the top view.</summary>
        public static bool AimsFromBehind(MatchCameraMode mode) => mode != MatchCameraMode.Classic;

        public static double Height(MatchCameraMode mode) => mode == MatchCameraMode.FirstPerson ? FirstHeight : ThirdHeight;

        /// <summary>
        /// Camera ground point behind <paramref name="cueBall"/>, opposite <paramref name="aim"/>. Even from a rail ball
        /// this stays within ~1.1 m of the table, short of the halls' seats and walls (so no deoccluder is needed).
        /// </summary>
        public static Vec2 Behind(MatchCameraMode mode, Vec2 cueBall, Vec2 aim)
        {
            Vec2 forward = aim.Normalized;
            if (forward == Vec2.Zero) forward = Vec2.UnitX;
            return cueBall - forward * (mode == MatchCameraMode.FirstPerson ? FirstBack : ThirdBack);
        }
    }
}
