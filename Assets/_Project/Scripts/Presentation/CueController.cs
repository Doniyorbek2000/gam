using System;
using System.Collections.Generic;
using BilliardGame.Gameplay;
using BilliardGame.Physics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace BilliardGame.Presentation
{
    /// <summary>
    /// Aiming with any pointer (mouse or touch): drag anywhere on the table to swing the cue around the cue ball, drag the cue ball to place it
    /// when in hand. Draws the aim line, ghost ball, object/cue paths and the cue stick.
    /// </summary>
    public sealed class CueController : MonoBehaviour
    {
        private enum DragMode { None, Aim, Place }

        [SerializeField] private Camera viewCamera;
        [SerializeField] private BallView cueBall;
        [SerializeField] private LineRenderer aimLine;
        [SerializeField] private LineRenderer objectLine;
        [SerializeField] private LineRenderer cueLine;
        [SerializeField] private Transform ghostBall;
        [SerializeField] private Transform cueStick;
        [SerializeField] private float objectLineLength = 0.3f;
        [SerializeField] private float placeGrabRadius = 0.12f;
        [SerializeField] private float maxPullback = 0.2f;
        [SerializeField] private float cueElevation = 5f;

        private readonly List<RaycastResult> uiHits = new List<RaycastResult>();
        private PointerEventData pointerData;
        private EventSystem pointerDataOwner;

        private IReadOnlyList<BallState> balls = Array.Empty<BallState>();
        private TableGeometry table;
        private PhysicsConfig config;
        private Func<Vec2, bool> canPlace;
        private bool isActive;
        private bool ballInHand;
        private DragMode drag;
        private float pullback01;
        private double dragAngle;
        private bool hasDragAngle;

        /// <summary>Below this distance from the cue ball the sweep angle is too noisy to steer with.</summary>
        private const double MinDragRadius = 0.03;

        public Vec2 AimDirection { get; private set; } = Vec2.UnitX;
        public Vec2 CuePosition { get; private set; }

        public void Begin(IReadOnlyList<BallState> tableBalls, Vec2 cuePosition, bool inHand, TableGeometry geometry, PhysicsConfig physics, Func<Vec2, bool> placementCheck)
        {
            balls = tableBalls;
            CuePosition = cuePosition;
            ballInHand = inHand;
            table = geometry;
            config = physics;
            canPlace = placementCheck;
            isActive = true;
            drag = DragMode.None;
            cueBall.SetPose(cuePosition, false);
            SetVisualsVisible(true);
        }

        public void End()
        {
            isActive = false;
            drag = DragMode.None;
            SetVisualsVisible(false);
        }

        public void SetPullback(float power01) => pullback01 = Mathf.Clamp01(power01);

        /// <summary>Fine aim adjustment, counter-clockwise seen from above.</summary>
        public void RotateAim(float degrees)
        {
            if (!isActive) return;
            AimDirection = Vec2.FromAngle(AimDirection.Angle + degrees * Mathf.Deg2Rad);
        }

        private void Update()
        {
            if (!isActive) return;
            HandlePointer();
            UpdateVisuals();
        }

        private void HandlePointer()
        {
            Pointer pointer = Pointer.current;
            if (pointer == null) return;

            Vector2 screen = pointer.position.ReadValue();
            if (pointer.press.wasPressedThisFrame)
            {
                drag = DragMode.None;
                if (IsOverUi(screen) || !TryTablePoint(screen, out Vec2 down)) return;
                drag = ballInHand && Vec2.Distance(down, CuePosition) < placeGrabRadius ? DragMode.Place : DragMode.Aim;
                hasDragAngle = false;
            }

            if (drag != DragMode.None && pointer.press.isPressed && TryTablePoint(screen, out Vec2 point))
            {
                if (drag == DragMode.Place)
                {
                    if (canPlace(point))
                    {
                        CuePosition = point;
                        cueBall.SetPose(point, false);
                    }
                }
                else
                {
                    // Relative: the cue turns by the finger's sweep around the cue ball, so the finger never has
                    // to cover the target and the aim does not jump on touch-down.
                    Vec2 toPoint = point - CuePosition;
                    if (toPoint.LengthSquared < MinDragRadius * MinDragRadius)
                    {
                        hasDragAngle = false;
                    }
                    else
                    {
                        double angle = toPoint.Angle;
                        if (hasDragAngle)
                        {
                            double delta = Math.IEEERemainder(angle - dragAngle, 2.0 * Math.PI);
                            AimDirection = Vec2.FromAngle(AimDirection.Angle + delta);
                        }

                        dragAngle = angle;
                        hasDragAngle = true;
                    }
                }
            }

            if (pointer.press.wasReleasedThisFrame) drag = DragMode.None;
        }

        private void UpdateVisuals()
        {
            float r = (float)config.BallRadius;
            AimPrediction aim = AimPredictor.Predict(CuePosition, AimDirection, balls, EightBallSet.CueBall, table, config);
            Vector3 cue = TableSpace.ToWorld(CuePosition, r);
            Vector3 ghost = TableSpace.ToWorld(aim.GhostPosition, r);
            Vector3 dir = TableSpace.ToWorld(AimDirection, 0f);

            aimLine.SetPosition(0, cue);
            aimLine.SetPosition(1, ghost);
            ghostBall.position = ghost;

            objectLine.enabled = aim.HitsBall;
            cueLine.enabled = aim.HitsBall && aim.CueBallDirection != Vec2.Zero;
            if (aim.HitsBall)
            {
                Vector3 objectDir = TableSpace.ToWorld(aim.ObjectBallDirection, 0f);
                Vector3 objectCentre = ghost + objectDir * (2f * r);
                objectLine.SetPosition(0, objectCentre);
                objectLine.SetPosition(1, objectCentre + objectDir * objectLineLength);
                Vector3 cueDir = TableSpace.ToWorld(aim.CueBallDirection, 0f);
                cueLine.SetPosition(0, ghost);
                cueLine.SetPosition(1, ghost + cueDir * (objectLineLength * 0.6f));
            }

            float halfLength = cueStick.localScale.y;
            Vector3 tip = cue - dir * (r * 1.5f + pullback01 * maxPullback);
            // Butt raised like a real stroke so the stick clears the rails.
            Vector3 axis = (dir - Vector3.up * Mathf.Tan(cueElevation * Mathf.Deg2Rad)).normalized;
            cueStick.SetPositionAndRotation(tip - axis * halfLength, Quaternion.FromToRotation(Vector3.up, axis));
        }

        private void SetVisualsVisible(bool visible)
        {
            aimLine.enabled = visible;
            objectLine.enabled = visible;
            cueLine.enabled = visible;
            ghostBall.gameObject.SetActive(visible);
            cueStick.gameObject.SetActive(visible);
        }

        private bool IsOverUi(Vector2 screen)
        {
            EventSystem events = EventSystem.current;
            if (events == null) return false;
            if (pointerData == null || pointerDataOwner != events)
            {
                pointerData = new PointerEventData(events);
                pointerDataOwner = events;
            }

            pointerData.position = screen;
            uiHits.Clear();
            events.RaycastAll(pointerData, uiHits);
            return uiHits.Count > 0;
        }

        private bool TryTablePoint(Vector2 screen, out Vec2 point)
        {
            Ray ray = viewCamera.ScreenPointToRay(screen);
            float height = (float)config.BallRadius;
            if (ray.direction.y > -1e-4f)
            {
                point = Vec2.Zero;
                return false;
            }

            Vector3 hit = ray.origin + ray.direction * ((height - ray.origin.y) / ray.direction.y);
            point = TableSpace.ToTable(hit);
            return true;
        }
    }
}
