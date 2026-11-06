using UnityEngine;

namespace BilliardGame.Content
{
    public enum CharacterSpotKind { Seat, CueRest, Npc }

    /// <summary>
    /// Marker placed in a venue by its builder: where a waiting player sits (forward = facing the table), where the
    /// spare cue leans, or where a background NPC stands. Index = player seat (0/1) for seats.
    /// </summary>
    public sealed class CharacterSpot : MonoBehaviour
    {
        [SerializeField] private CharacterSpotKind kind;
        [SerializeField] private int index;
        [Tooltip("Optional drink prop on the side table next to a seat.")]
        [SerializeField] private Transform drink;

        public CharacterSpotKind Kind => kind;
        public int Index => index;
        public Transform Drink => drink;

        public void Configure(CharacterSpotKind spotKind, int spotIndex, Transform drinkProp)
        {
            kind = spotKind;
            index = spotIndex;
            drink = drinkProp;
        }
    }
}
