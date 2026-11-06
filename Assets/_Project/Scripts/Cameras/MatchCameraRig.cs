using BilliardGame.Avatar.Rules;
using BilliardGame.Gameplay;
using BilliardGame.Physics;
using BilliardGame.Presentation;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BilliardGame.Cameras
{
    /// <summary>
    /// Match camera modes (ROADMAP M1.6 item 4): Classic top view, 3rd person over the shoulder, 1st person on the cue
    /// line, Cinematic (3rd-person aim, then follows the balls and zooms on pots; a tap skips to the top view).
    /// Three Cinemachine cameras, the brain blends between them. It only reads the match; nothing here reaches physics.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class MatchCameraRig : MonoBehaviour
    {
        private const int Live = 20;
        private const float AimLookAhead = 0.9f;
        private const float PotZoomSeconds = 1.3f;
        private const float MovingEpsilon = 0.0004f;

        [SerializeField] private GameController game;
        [SerializeField] private CueController cue;
        [SerializeField] private TablePlayback playback;
        [SerializeField] private CinemachineCamera classic;
        [SerializeField] private CinemachineCamera aim;
        [SerializeField] private CinemachineCamera follow;
        [SerializeField] private float aimSharpness = 10f;
        [SerializeField] private float followSharpness = 3f;
        [Tooltip("Follow camera offset from the balls it watches (shooter's side, raised).")]
        [SerializeField] private Vector3 followOffset = new Vector3(0f, 1.25f, -1.35f);
        [SerializeField] private Vector3 potOffset = new Vector3(0f, 0.42f, -0.5f);

        private readonly Vector3[] lastBall = new Vector3[EightBallSet.BallCount];
        private MatchCameraMode mode;
        private bool skipped;
        private bool aimPlaced;
        private float zoomUntil;
        private Vector3 zoomPoint;
        private Vector3 focus;

        public MatchCameraMode Mode
        {
            get => mode;
            set
            {
                mode = value;
                cue.SwipeAim = CameraRules.AimsFromBehind(mode);
                aimPlaced = false;
            }
        }

        public CinemachineCamera Active { get; private set; }

        private void OnEnable()
        {
            game.ShotStruck += OnShotStruck;
            playback.EventPlayed += OnEventPlayed;
            Mode = mode;
        }

        private void OnDisable()
        {
            game.ShotStruck -= OnShotStruck;
            playback.EventPlayed -= OnEventPlayed;
        }

        private void OnShotStruck()
        {
            skipped = false;
            zoomUntil = 0f;
            focus = cue.CueBallView.position;
            SampleBalls();
        }

        private void OnEventPlayed(SimulationEvent e)
        {
            if (e.Type != SimulationEventType.BallPocketed || mode != MatchCameraMode.Cinematic) return;
            zoomPoint = playback.GetView(e.BallId).transform.position;
            zoomUntil = Time.time + PotZoomSeconds;
        }

        private void LateUpdate()
        {
            if (game.State == null) return;
            bool playing = game.IsShotInProgress && !cue.IsStroking;
            if (playing && mode == MatchCameraMode.Cinematic && !skipped && Pointer.current != null && Pointer.current.press.wasPressedThisFrame) skipped = true;

            CinemachineCamera next;
            if (playing) next = mode == MatchCameraMode.Cinematic && !skipped ? follow : classic;
            else next = CameraRules.AimsFromBehind(mode) ? aim : classic;

            if (next == aim) PlaceAim();
            else aimPlaced = false;
            if (next == follow) PlaceFollow();
            Activate(next);
        }

        private void PlaceAim()
        {
            Vec2 ball = cue.IsActive ? cue.CuePosition : game.State.GetBall(EightBallSet.CueBall).Position;
            Vec2 direction = cue.IsActive ? cue.AimDirection : (ball.Length > 0.05 ? (-ball).Normalized : Vec2.UnitX);
            Vector3 eye = TableSpace.ToWorld(CameraRules.Behind(mode, ball, direction), (float)CameraRules.Height(mode));
            Vector3 target = TableSpace.ToWorld(ball + direction * AimLookAhead, 0f);
            Quaternion look = Quaternion.LookRotation(target - eye, Vector3.up);
            float k = aimPlaced ? 1f - Mathf.Exp(-aimSharpness * Time.deltaTime) : 1f;
            Transform t = aim.transform;
            t.SetPositionAndRotation(Vector3.Lerp(t.position, eye, k), Quaternion.Slerp(t.rotation, look, k));
            aimPlaced = true;
        }

        private void PlaceFollow()
        {
            Vector3 goal;
            Vector3 offset;
            if (Time.time < zoomUntil)
            {
                goal = zoomPoint;
                offset = potOffset;
            }
            else
            {
                goal = MovingCentre();
                offset = followOffset;
            }

            float k = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
            focus = Vector3.Lerp(focus, goal, k);
            Transform t = follow.transform;
            Vector3 eye = Vector3.Lerp(t.position, focus + offset, k);
            t.SetPositionAndRotation(eye, Quaternion.LookRotation(focus - eye, Vector3.up));
        }

        /// <summary>Centre of the balls that moved since last frame; the cue ball when everything rests.</summary>
        private Vector3 MovingCentre()
        {
            Vector3 sum = Vector3.zero;
            int moving = 0;
            for (int id = 0; id < lastBall.Length; id++)
            {
                Transform view = playback.GetView(id).transform;
                Vector3 p = view.position;
                if (view.gameObject.activeInHierarchy && (p - lastBall[id]).sqrMagnitude > MovingEpsilon * MovingEpsilon)
                {
                    sum += p;
                    moving++;
                }

                lastBall[id] = p;
            }

            return moving > 0 ? sum / moving : cue.CueBallView.position;
        }

        private void SampleBalls()
        {
            for (int id = 0; id < lastBall.Length; id++) lastBall[id] = playback.GetView(id).transform.position;
        }

        private void Activate(CinemachineCamera next)
        {
            if (Active == next) return;
            if (next == follow) follow.transform.SetPositionAndRotation(Active != null ? Active.transform.position : next.transform.position, Active != null ? Active.transform.rotation : next.transform.rotation);
            Active = next;
            classic.Priority = next == classic ? Live : 0;
            aim.Priority = next == aim ? Live : 0;
            follow.Priority = next == follow ? Live : 0;
        }
    }
}
