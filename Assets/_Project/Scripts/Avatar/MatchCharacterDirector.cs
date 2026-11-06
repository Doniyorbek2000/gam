using BilliardGame.Avatar.Rules;
using BilliardGame.Content;
using BilliardGame.Gameplay;
using BilliardGame.Physics;
using BilliardGame.Presentation;
using UnityEngine;

namespace BilliardGame.Avatar
{
    /// <summary>
    /// Puts both players in the hall for a match (docs/GDD.md §8.1) and drives them from the match's own state:
    /// the shooter walks to the stand point behind the cue ball, may chalk, addresses the cue and strokes with it;
    /// the other player sits, drinks and watches; both react to the verdict. It only listens to
    /// <see cref="GameController"/> — no shot, rule or timer ever waits for it, online included.
    /// </summary>
    public sealed class MatchCharacterDirector : MonoBehaviour
    {
        [SerializeField] private GameController game;
        [SerializeField] private CueController cue;
        [SerializeField] private float floorY = -0.8f;
        [Tooltip("Half size of the table body (rails included), table X/Y.")]
        [SerializeField] private Vector2 tableOuterHalf = new Vector2(1.47f, 0.83f);
        [Range(0f, 1f)] [SerializeField] private float chalkChance = 0.35f;
        [SerializeField] private float riseDelay = 0.55f;

        private readonly MatchCharacter[] players = new MatchCharacter[2];
        private readonly CharacterSpot[] seats = new CharacterSpot[2];
        private int shooter = -1;
        private ShotOutcome seenOutcome;
        private bool chalkedThisTurn;
        private float riseAt = float.NaN;
        private bool shotWatch;

        public MatchCharacter Player(int seat) => players[seat];
        public bool IsCast => players[0] != null;

        private void OnEnable()
        {
            game.StateChanged += OnStateChanged;
            game.ShotStruck += OnShotStruck;
        }

        private void OnDisable()
        {
            game.StateChanged -= OnStateChanged;
            game.ShotStruck -= OnShotStruck;
        }

        /// <summary>Spawns the two characters in the loaded hall, both seated until the first turn starts.</summary>
        public void Begin(CharacterDefinition first, CharacterDefinition second)
        {
            End();
            FindSeats();
            players[0] = Spawn(first, 0);
            players[1] = Spawn(second, 1);
            shooter = -1;
            seenOutcome = null;
            OnStateChanged();
        }

        public void End()
        {
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i] != null) Destroy(players[i].gameObject);
                players[i] = null;
            }

            shooter = -1;
            riseAt = float.NaN;
        }

        private MatchCharacter Spawn(CharacterDefinition definition, int seat)
        {
            if (definition == null || definition.Prefab == null) return null;
            GameObject instance = Instantiate(definition.Prefab, transform);
            instance.name = $"Player {seat + 1} ({definition.DisplayName})";
            var character = instance.GetComponent<MatchCharacter>();
            if (character == null)
            {
                Debug.LogError($"[Characters] {definition.Id} prefab has no MatchCharacter; rebuild characters.");
                Destroy(instance);
                return null;
            }

            character.Setup(floorY, tableOuterHalf);
            character.Place(seats[seat].transform.position, seats[seat].transform.rotation, seats[seat]);
            return character;
        }

        private void FindSeats()
        {
            seats[0] = seats[1] = null;
            foreach (CharacterSpot spot in FindObjectsByType<CharacterSpot>())
            {
                if (spot.Kind == CharacterSpotKind.Seat && spot.Index >= 0 && spot.Index < 2 && seats[spot.Index] == null) seats[spot.Index] = spot;
            }

            for (int i = 0; i < 2; i++)
            {
                if (seats[i] != null) continue;
                // Hall without seats: stand-in spots either side of the table, facing it.
                var fallback = new GameObject($"Fallback Seat {i}").AddComponent<CharacterSpot>();
                fallback.transform.SetParent(transform, false);
                float side = i == 0 ? -1f : 1f;
                fallback.transform.SetPositionAndRotation(new Vector3(side * (tableOuterHalf.x + 1.2f), floorY, tableOuterHalf.y + 0.6f),
                    Quaternion.LookRotation(new Vector3(-side, 0f, -0.6f)));
                fallback.Configure(CharacterSpotKind.Seat, i, null);
                seats[i] = fallback;
            }
        }

        private void OnStateChanged()
        {
            if (!IsCast) return;
            EightBallState state = game.State;
            if (state == null) return;

            if (game.LastOutcome != null && !ReferenceEquals(game.LastOutcome, seenOutcome))
            {
                seenOutcome = game.LastOutcome;
                if (shooter >= 0)
                {
                    Get(shooter)?.React(Reactions.ForShooter(seenOutcome, shooter));
                    Get(1 - shooter)?.React(Reactions.ForWatcher(seenOutcome));
                }
            }

            if (game.IsShotInProgress || state.IsGameOver) return;
            shotWatch = false;
            if (state.CurrentPlayer != shooter)
            {
                shooter = state.CurrentPlayer;
                chalkedThisTurn = false;
                Get(1 - shooter)?.SitOn(seats[1 - shooter]);
                Get(shooter)?.Rise();
            }

            ApproachShot(force: true);
        }

        private void OnShotStruck()
        {
            if (!IsCast || shooter < 0) return;
            riseAt = Time.time + riseDelay;
            shotWatch = true;
        }

        private void Update()
        {
            if (!IsCast || shooter < 0 || game.State == null) return;
            MatchCharacter active = Get(shooter);
            if (active == null) return;

            if (!float.IsNaN(riseAt) && Time.time >= riseAt)
            {
                riseAt = float.NaN;
                active.Rise();
            }

            if (shotWatch)
            {
                Vector3 ball = cue.CueBallView.position;
                active.LookAt(ball, 0.8f);
                Get(1 - shooter)?.LookAt(ball, 0.6f);
                return;
            }

            if (!game.IsShotInProgress) ApproachShot(force: false);
        }

        /// <summary>Keeps the shooter at the stand point for the current aim; re-walks once the aim moved far enough.</summary>
        private void ApproachShot(bool force)
        {
            MatchCharacter active = Get(shooter);
            if (active == null || active.IsWalking && !force) return;

            bool localAim = cue.IsActive;
            Vec2 ball = localAim ? cue.CuePosition : game.State.GetBall(EightBallSet.CueBall).Position;
            Vec2 aim = localAim ? cue.AimDirection : (ball.Length > 0.05 ? (-ball).Normalized : Vec2.UnitX);
            Vec2 stand = StancePlanner.StandPoint(ball, aim, tableOuterHalf.x, tableOuterHalf.y);
            Vector3 target = TableSpace.ToWorld(stand, floorY);
            Vector3 face = TableSpace.ToWorld(aim, 0f);

            if (localAim && cue.IsDragging) return;     // wait for the finger to settle, then walk once
            bool moved = StancePlanner.NeedsReposition(TableSpace.ToTable(active.Goal), stand);
            if (!force && !moved)
            {
                if (active.IsIdle) Address(active, ball);
                return;
            }

            if (!moved && !active.IsIdle && !active.IsSeated) return;   // already there, or busy (standing up, chalking)
            active.WalkTo(target, face, () => OnArrived(active, ball));
        }

        private void OnArrived(MatchCharacter active, Vec2 ball)
        {
            if (!chalkedThisTurn && Random.value < chalkChance)
            {
                chalkedThisTurn = true;
                active.Chalk(() => cue.IsDragging || cue.IsStroking || game.IsShotInProgress, () => Address(active, ball));
                return;
            }

            chalkedThisTurn = true;
            Address(active, ball);
        }

        private void Address(MatchCharacter active, Vec2 ball)
        {
            if (cue.IsActive) active.Address(cue.CueStick);
            else active.AddressWithoutCue(TableSpace.ToWorld(ball, 0f));
        }

        private MatchCharacter Get(int seat) => seat >= 0 && seat < players.Length ? players[seat] : null;
    }
}
