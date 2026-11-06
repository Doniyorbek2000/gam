using System;
using System.Collections;
using System.Collections.Generic;
using BilliardGame.Avatar.Rules;
using BilliardGame.Content;
using BilliardGame.Physics;
using BilliardGame.Presentation;
using UnityEngine;
using UnityEngine.AI;

namespace BilliardGame.Avatar
{
    /// <summary>
    /// One player in the hall (docs/GDD.md §8.1): walks round the table (NavMesh, or <see cref="TablePath"/> when none is
    /// baked) at the walk clip's own speed so the feet do not slide, bends into the address pose with the bridge hand on
    /// the cloth and the grip hand on the cue, sits, drinks and reacts. Purely visual: nothing here reaches the shot.
    /// </summary>
    public sealed class MatchCharacter : MonoBehaviour
    {
        private enum Mode { Idle, Walking, Address, Seated, Busy }

        [SerializeField] private CharacterRig rig;
        [SerializeField] private NavMeshAgent agent;
        [SerializeField] private float turnSpeed = 360f;
        [Tooltip("Grip hand distance from the cue tip, metres along the stick.")]
        [SerializeField] private float gripFromTip = 1.12f;
        [Tooltip("Bridge hand distance behind the cue ball.")]
        [SerializeField] private float bridgeBehindBall = 0.24f;
        [Tooltip("How far ahead of the hips the bridge hand reaches when bent over (horizontal).")]
        [SerializeField] private float bridgeReach = 0.62f;

        private readonly List<Vec2> route = new List<Vec2>();
        private Mode mode;
        private int waypoint;
        private Vector3 goal, faceDirection;
        private Action arrived;
        private Coroutine routine;
        private Transform cueStick;
        private Vector3 bridgePoint;
        private CharacterSpot seat;
        private float floorY;
        private float nextDrink;
        private Vector2 tableHalf;
        private bool hasNavMesh;

        public bool IsWalking => mode == Mode.Walking;
        public bool IsSeated => mode == Mode.Seated;
        public bool IsAddressing => mode == Mode.Address;
        /// <summary>Standing with nothing to do: free to take the address pose.</summary>
        public bool IsIdle => mode == Mode.Idle;
        public Vector3 Goal => goal;
        public CharacterRig Rig => rig;

        public void Configure(CharacterRig characterRig, NavMeshAgent navAgent)
        {
            rig = characterRig;
            agent = navAgent;
        }

        public void Setup(float floorHeight, Vector2 tableOuterHalf)
        {
            floorY = floorHeight;
            tableHalf = tableOuterHalf;
            // The agent is enabled only while walking, so it never fights the seat or address placement.
            hasNavMesh = agent != null && NavMesh.SamplePosition(new Vector3(0f, floorY, tableOuterHalf.y + 0.8f), out _, 1.5f, NavMesh.AllAreas);
            if (agent != null)
            {
                agent.enabled = false;
                agent.updateRotation = true;
                agent.speed = rig.WalkClipSpeed;
                agent.angularSpeed = turnSpeed;
                agent.acceleration = 8f;
                agent.stoppingDistance = 0.05f;
                agent.autoBraking = true;
            }
        }

        /// <summary>Teleports (match start); sits straight away when given a seat.</summary>
        public void Place(Vector3 position, Quaternion rotation, CharacterSpot sitOn)
        {
            Stop();
            transform.SetPositionAndRotation(position, rotation);
            goal = position;
            seat = sitOn;
            if (sitOn != null)
            {
                transform.SetPositionAndRotation(sitOn.transform.position, sitOn.transform.rotation);
                rig.SetSeated(true);
                mode = Mode.Seated;
                nextDrink = Time.time + UnityEngine.Random.Range(4f, 8f);
            }
            else
            {
                rig.SetSeated(false);
                mode = Mode.Idle;
            }
        }

        /// <summary>Walks to <paramref name="position"/> (stands up first if seated), then faces <paramref name="face"/>.</summary>
        public void WalkTo(Vector3 position, Vector3 face, Action onArrived)
        {
            position.y = floorY;
            goal = position;
            faceDirection = face;
            arrived = onArrived;
            cueStick = null;
            rig.SetAddress(false);
            rig.SetArms(0f, 0f);
            Run(WalkRoutine());
        }

        /// <summary>Address pose on <paramref name="stick"/> (pivot at the tip, butt along +Z), bridge behind the cue ball.</summary>
        public void Address(Transform stick)
        {
            if (mode != Mode.Idle && mode != Mode.Address) return;
            StopRoutine();
            cueStick = stick;
            mode = Mode.Address;
            rig.SetAddress(true);
            rig.SetArms(1f, 1f);
        }

        /// <summary>Address with no visible cue (the opponent's turn online): hands rest towards <paramref name="cueBall"/>.</summary>
        public void AddressWithoutCue(Vector3 cueBall)
        {
            if (mode != Mode.Idle && mode != Mode.Address) return;
            StopRoutine();
            cueStick = null;
            bridgePoint = cueBall;
            mode = Mode.Address;
            rig.SetAddress(true);
            rig.SetArms(1f, 0f);
        }

        /// <summary>Stands up out of the address pose and watches <paramref name="target"/>.</summary>
        public void Rise()
        {
            if (mode != Mode.Address) return;
            cueStick = null;
            rig.SetAddress(false);
            rig.SetArms(0f, 0f);
            mode = Mode.Idle;
        }

        public void SitOn(CharacterSpot spot)
        {
            if (mode == Mode.Seated && seat == spot) return;
            seat = spot;
            if (spot == null)
            {
                Rise();
                return;
            }

            Vector3 approach = spot.transform.position + spot.transform.forward * 0.55f;
            WalkTo(approach, -spot.transform.forward, () => Run(SitRoutine(spot)));
        }

        /// <summary>Optional chalking before the address; any input (aim drag, shoot) cuts it short.</summary>
        public void Chalk(Func<bool> interrupt, Action done)
        {
            Run(ChalkRoutine(interrupt, done));
        }

        public void LookAt(Vector3 point, float weight) => rig.LookAt(point, weight);

        public void React(Reaction reaction)
        {
            switch (reaction)
            {
                case Reaction.Clap:
                    if (mode != Mode.Walking) Run(ClapRoutine());
                    break;
                case Reaction.Disappointed:
                    if (mode != Mode.Seated) rig.Sad();
                    break;
                case Reaction.Celebrate:
                    if (mode != Mode.Seated) rig.Celebrate();
                    break;
            }
        }

        private void Update()
        {
            if (mode == Mode.Address) UpdateAddress();
            if (mode == Mode.Seated && seat != null && seat.Drink != null && Time.time >= nextDrink) Run(DrinkRoutine(seat.Drink));
            if (mode != Mode.Walking) rig.SetSpeed(0f);
        }

        private void UpdateAddress()
        {
            Vector3 forward;
            if (cueStick != null)
            {
                // Stick axis points from the tip back to the butt (+Z); the player looks down it towards the tip.
                Vector3 tip = cueStick.position;
                Vector3 axis = cueStick.forward;
                forward = Flat(-axis);
                // Bridge behind the ball, or as far forward as the arm reaches when the ball is out of reach.
                Vector3 axisFlat = Flat(axis);
                float hipsAlong = Vector3.Dot(transform.position - tip, axisFlat);
                float along = Mathf.Clamp(hipsAlong - bridgeReach, bridgeBehindBall, Mathf.Max(bridgeBehindBall, hipsAlong));
                Vector3 bridge = tip + axis * (along / Mathf.Max(Vector3.Dot(axis, axisFlat), 0.5f));
                bridge.y = Mathf.Max(bridge.y - 0.035f, 0.01f);
                Vector3 grip = tip + axis * gripFromTip;
                rig.PlaceLeft(bridge, Quaternion.LookRotation(forward, Vector3.up), bridge + Right(forward) * -0.25f + Vector3.up * 0.2f);
                rig.PlaceRight(grip, Quaternion.LookRotation(Vector3.down, forward), grip + Vector3.up * 0.35f - forward * 0.15f);
                rig.LookAt(tip - axis * 0.3f, 1f);
            }
            else
            {
                forward = Flat(bridgePoint - transform.position);
                Vector3 bridge = bridgePoint - forward * bridgeBehindBall;
                bridge.y = 0.01f;
                rig.PlaceLeft(bridge, Quaternion.LookRotation(forward, Vector3.up), bridge + Right(forward) * -0.25f + Vector3.up * 0.2f);
                rig.LookAt(bridgePoint, 1f);
            }

            if (forward.sqrMagnitude > 1e-4f)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(forward), turnSpeed * Time.deltaTime);
            }
        }

        private IEnumerator WalkRoutine()
        {
            if (mode == Mode.Seated)
            {
                mode = Mode.Busy;
                rig.SetSeated(false);
                yield return new WaitForSeconds(1.0f);
                // The sitting clips have no root motion: step off the seat to its front edge.
                if (seat != null) yield return Slide(seat.transform.position + seat.transform.forward * 0.4f, transform.rotation, 0.25f);
            }

            mode = Mode.Walking;
            if (hasNavMesh && NavMesh.SamplePosition(transform.position, out NavMeshHit start, 1.2f, NavMesh.AllAreas))
            {
                agent.enabled = true;
                agent.Warp(start.position);
                agent.SetDestination(goal);
                // The path and remainingDistance only settle after the agent's next update.
                yield return null;
                float giveUp = Time.time + 20f;
                while ((agent.pathPending || agent.remainingDistance > agent.stoppingDistance + 0.02f) && Time.time < giveUp)
                {
                    rig.SetSpeed(agent.velocity.magnitude);
                    yield return null;
                }

                agent.enabled = false;
            }
            else
            {
                TablePath.Route(TableSpace.ToTable(transform.position), TableSpace.ToTable(goal), tableHalf.x, tableHalf.y, route);
                foreach (Vec2 point in route)
                {
                    Vector3 target = TableSpace.ToWorld(point, floorY);
                    while (FlatDistance(target, transform.position) > 0.03f)
                    {
                        Vector3 step = Flat(target - transform.position);
                        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(step), turnSpeed * Time.deltaTime);
                        transform.position = Vector3.MoveTowards(transform.position, target, rig.WalkClipSpeed * Time.deltaTime);
                        rig.SetSpeed(rig.WalkClipSpeed);
                        yield return null;
                    }
                }
            }

            rig.SetSpeed(0f);
            Vector3 face = Flat(faceDirection);
            while (face.sqrMagnitude > 1e-4f && Quaternion.Angle(transform.rotation, Quaternion.LookRotation(face)) > 2f)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(face), turnSpeed * Time.deltaTime);
                yield return null;
            }

            mode = Mode.Idle;
            routine = null;
            Action done = arrived;
            arrived = null;
            done?.Invoke();
        }

        private IEnumerator SitRoutine(CharacterSpot spot)
        {
            mode = Mode.Busy;
            rig.SetSeated(true);
            yield return Slide(spot.transform.position, spot.transform.rotation, 0.9f);
            mode = Mode.Seated;
            nextDrink = Time.time + UnityEngine.Random.Range(3f, 6f);
            routine = null;
        }

        private IEnumerator ChalkRoutine(Func<bool> interrupt, Action done)
        {
            mode = Mode.Busy;
            rig.ShowChalk(true);
            Transform head = rig.Head;
            float end = Time.time + 1.4f;
            while (Time.time < end && (interrupt == null || !interrupt()))
            {
                // Left hand holds the cue tip up in front of the chest; the right hand twists the chalk on it.
                Vector3 front = transform.position + transform.forward * 0.32f + Vector3.up * (head.position.y - transform.position.y - 0.3f);
                float t = Time.time * 9f;
                rig.PlaceLeft(front + Right(transform.forward) * -0.05f - Vector3.up * 0.15f, Quaternion.LookRotation(transform.forward), front + Right(transform.forward) * -0.4f);
                rig.PlaceRight(front + Vector3.up * 0.05f + new Vector3(Mathf.Cos(t), 0f, Mathf.Sin(t)) * 0.015f, Quaternion.LookRotation(Vector3.down, transform.forward), front + Right(transform.forward) * 0.4f);
                rig.SetArms(1f, 1f);
                rig.LookAt(front, 0.8f);
                yield return null;
            }

            rig.ShowChalk(false);
            rig.SetArms(0f, 0f);
            mode = Mode.Idle;
            routine = null;
            done?.Invoke();
        }

        private IEnumerator DrinkRoutine(Transform glass)
        {
            mode = Mode.Busy;
            Transform home = glass.parent;
            Vector3 homePosition = glass.localPosition;
            Quaternion homeRotation = glass.localRotation;
            Vector3 right = Right(transform.forward);

            yield return Reach(glass.position, 0.6f);
            glass.SetParent(rig.RightTarget, true);
            Vector3 mouth = rig.Head.position + transform.forward * 0.12f - Vector3.up * 0.08f;
            yield return MoveRight(mouth, 0.7f, right);
            rig.LookAt(mouth + transform.forward, 0.3f);
            yield return new WaitForSeconds(1.1f);
            yield return MoveRight(home.TransformPoint(homePosition), 0.7f, right);
            glass.SetParent(home, false);
            glass.SetLocalPositionAndRotation(homePosition, homeRotation);
            rig.SetArms(0f, 0f);
            yield return new WaitForSeconds(0.4f);

            mode = Mode.Seated;
            nextDrink = Time.time + UnityEngine.Random.Range(7f, 12f);
            routine = null;
        }

        private IEnumerator ClapRoutine()
        {
            Mode previous = mode == Mode.Busy ? Mode.Idle : mode;
            mode = Mode.Busy;
            Transform head = rig.Head;
            Vector3 right = Right(transform.forward);
            float end = Time.time + 1.6f;
            while (Time.time < end)
            {
                Vector3 centre = transform.position + transform.forward * 0.3f + Vector3.up * (head.position.y - transform.position.y - 0.35f);
                float open = 0.06f + 0.06f * Mathf.Abs(Mathf.Sin(Time.time * 14f));
                rig.PlaceLeft(centre - right * open, Quaternion.LookRotation(transform.forward, right), centre - right * 0.4f - Vector3.up * 0.2f);
                rig.PlaceRight(centre + right * open, Quaternion.LookRotation(transform.forward, -right), centre + right * 0.4f - Vector3.up * 0.2f);
                rig.SetArms(1f, 1f);
                yield return null;
            }

            rig.SetArms(0f, 0f);
            mode = previous;
            routine = null;
        }

        private IEnumerator Reach(Vector3 target, float seconds)
        {
            rig.PlaceRight(target, Quaternion.LookRotation(transform.forward), target + Right(transform.forward) * 0.3f - Vector3.up * 0.1f);
            rig.SetArms(0f, 1f);
            yield return new WaitForSeconds(seconds);
        }

        private IEnumerator MoveRight(Vector3 to, float seconds, Vector3 right)
        {
            Vector3 from = rig.RightTarget.position;
            for (float t = 0f; t < 1f; t += Time.deltaTime / seconds)
            {
                Vector3 p = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t));
                rig.PlaceRight(p, Quaternion.LookRotation(transform.forward), p + right * 0.3f - Vector3.up * 0.2f);
                yield return null;
            }
        }

        private IEnumerator Slide(Vector3 position, Quaternion rotation, float seconds)
        {
            Vector3 fromPosition = transform.position;
            Quaternion fromRotation = transform.rotation;
            for (float t = 0f; t < 1f; t += Time.deltaTime / seconds)
            {
                float s = Mathf.SmoothStep(0f, 1f, t);
                transform.SetPositionAndRotation(Vector3.Lerp(fromPosition, position, s), Quaternion.Slerp(fromRotation, rotation, s));
                yield return null;
            }

            transform.SetPositionAndRotation(position, rotation);
        }

        private void Run(IEnumerator next)
        {
            StopRoutine();
            routine = StartCoroutine(next);
        }

        private void StopRoutine()
        {
            if (routine == null) return;
            StopCoroutine(routine);
            routine = null;
            rig.ShowChalk(false);
            if (agent != null) agent.enabled = false;
        }

        private void Stop()
        {
            StopRoutine();
            arrived = null;
            cueStick = null;
            rig.SetAddress(false);
            rig.SetArms(0f, 0f);
        }

        private static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude > 1e-8f ? v.normalized : Vector3.zero;
        }

        private static float FlatDistance(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));

        private static Vector3 Right(Vector3 forward) => Vector3.Cross(Vector3.up, forward).normalized;
    }
}
