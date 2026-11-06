using System.Collections.Generic;
using BilliardGame.Avatar.Rules;
using BilliardGame.Content;
using BilliardGame.Physics;
using BilliardGame.Presentation;
using UnityEngine;
using UnityEngine.Rendering;

namespace BilliardGame.Avatar
{
    /// <summary>
    /// Hall life (ROADMAP M1.6 item 5): background NPCs on the venue's Npc/NpcSeat spots (count from graphics quality,
    /// see <see cref="HallRules"/>), drawn at a forced low mesh LOD with no shadows and colour-varied outfits, watching
    /// the cue ball and now and then clapping a pot; plus the hall's ambience and low music loops. Purely visual/audio:
    /// nothing here is read by the match.
    /// </summary>
    public sealed class HallLife : MonoBehaviour
    {
        private const int NpcMeshLod = 2;
        private const float ClapChance = 0.3f;

        [SerializeField] private CueController cue;
        [SerializeField] private TablePlayback playback;
        [SerializeField] private float floorY = -0.8f;
        [SerializeField] private Vector2 tableOuterHalf = new Vector2(1.47f, 0.83f);
        [SerializeField] private AudioSource ambience;
        [SerializeField] private AudioSource music;
        [Range(0f, 1f)] [SerializeField] private float ambienceLevel = 0.35f;
        [Range(0f, 1f)] [SerializeField] private float musicLevel = 0.18f;

        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly Color[] Tints =
        {
            new Color(0.55f, 0.62f, 0.75f), new Color(0.78f, 0.58f, 0.45f), new Color(0.5f, 0.68f, 0.52f),
            new Color(0.82f, 0.78f, 0.7f), new Color(0.62f, 0.5f, 0.68f), new Color(0.42f, 0.44f, 0.48f),
        };

        private readonly List<MatchCharacter> npcs = new List<MatchCharacter>();
        private readonly List<bool> seated = new List<bool>();
        private MaterialPropertyBlock block;

        /// <summary>Set from settings: effects volume scales the ambience, music volume the music.</summary>
        public static float AmbienceVolume { get; set; } = 1f;
        public static float MusicVolume { get; set; } = 1f;

        public int Count => npcs.Count;

        private void OnEnable() => playback.EventPlayed += OnEventPlayed;

        private void OnDisable() => playback.EventPlayed -= OnEventPlayed;

        /// <summary>Fills the loaded hall's NPC spots (lowest index first) and starts its sound.</summary>
        public void Begin(IReadOnlyList<CharacterDefinition> pool, int qualityLevel)
        {
            End();
            List<CharacterSpot> spots = FindSpots();
            int count = pool == null || pool.Count == 0 ? 0 : HallRules.NpcCount(qualityLevel, spots.Count);
            for (int i = 0; i < count; i++) Spawn(pool[i % pool.Count], spots[i], i);
            Play(ambience, ambienceLevel * AmbienceVolume);
            Play(music, musicLevel * MusicVolume);
        }

        public void End()
        {
            foreach (MatchCharacter npc in npcs)
            {
                if (npc != null) Destroy(npc.gameObject);
            }

            npcs.Clear();
            seated.Clear();
            if (ambience != null) ambience.Stop();
            if (music != null) music.Stop();
        }

        private static List<CharacterSpot> FindSpots()
        {
            var spots = new List<CharacterSpot>();
            foreach (CharacterSpot spot in FindObjectsByType<CharacterSpot>())
            {
                if (spot.Kind == CharacterSpotKind.Npc || spot.Kind == CharacterSpotKind.NpcSeat) spots.Add(spot);
            }

            spots.Sort((a, b) => a.Index.CompareTo(b.Index));
            return spots;
        }

        private void Spawn(CharacterDefinition definition, CharacterSpot spot, int number)
        {
            if (definition == null || definition.Prefab == null) return;
            GameObject instance = Instantiate(definition.Prefab, transform);
            instance.name = $"NPC {number} ({definition.DisplayName})";
            var character = instance.GetComponent<MatchCharacter>();
            if (character == null)
            {
                Destroy(instance);
                return;
            }

            bool sits = spot.Kind == CharacterSpotKind.NpcSeat;
            character.Setup(floorY, tableOuterHalf);
            character.Place(spot.transform.position, spot.transform.rotation, sits ? spot : null);
            Dress(instance, Tints[number % Tints.Length]);
            Animator animator = instance.GetComponentInChildren<Animator>();
            if (animator != null)
            {
                animator.cullingMode = AnimatorCullingMode.CullCompletely;
                animator.Update(Random.Range(0f, 3f));   // desync idle loops
            }

            npcs.Add(character);
            seated.Add(sits);
        }

        // Cheap crowd: low mesh LOD, no shadows, outfit and hair tinted so repeated models read as different people.
        private void Dress(GameObject instance, Color tint)
        {
            block = block ?? new MaterialPropertyBlock();
            foreach (SkinnedMeshRenderer r in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (r.name == "Eyebrows")
                {
                    r.enabled = false;   // invisible from across the hall; one draw less per NPC
                    continue;
                }

                r.forceMeshLod = NpcMeshLod;
                r.shadowCastingMode = ShadowCastingMode.Off;
                Material[] materials = r.sharedMaterials;
                for (int m = 0; m < materials.Length; m++)
                {
                    Material material = materials[m];
                    if (material == null || !material.HasProperty(BaseColor)) continue;
                    string n = material.name;
                    if (!n.Contains("Outfit") && !n.Contains("Hair")) continue;
                    r.GetPropertyBlock(block, m);
                    block.SetColor(BaseColor, material.GetColor(BaseColor) * tint);
                    r.SetPropertyBlock(block, m);
                }
            }
        }

        private void Update()
        {
            // Follows the settings sliders mid-match.
            if (ambience != null) ambience.volume = ambienceLevel * AmbienceVolume;
            if (music != null) music.volume = musicLevel * MusicVolume;
            if (npcs.Count == 0 || cue == null || cue.CueBallView == null) return;
            Vector3 ball = cue.CueBallView.position;
            for (int i = 0; i < npcs.Count; i++)
            {
                if (npcs[i] != null) npcs[i].LookAt(ball, seated[i] ? 0.5f : 0.8f);
            }
        }

        private void OnEventPlayed(SimulationEvent e)
        {
            if (e.Type != SimulationEventType.BallPocketed) return;
            for (int i = 0; i < npcs.Count; i++)
            {
                if (npcs[i] != null && Random.value < ClapChance) npcs[i].React(Reaction.Clap);
            }
        }

        private static void Play(AudioSource source, float volume)
        {
            if (source == null || source.clip == null) return;
            source.loop = true;
            source.volume = volume;
            source.time = Random.Range(0f, source.clip.length);
            source.Play();
        }
    }
}
