using System.Collections.Generic;
using BilliardGame.Presentation;
using UnityEngine;
using UnityEngine.Rendering;

namespace BilliardGame.Avatar
{
    /// <summary>
    /// Keeps the players out of the way of the shot (ROADMAP M1.6 item 3): a character between the camera and the
    /// cue ball or table centre is drawn screen-door faded; one the camera is inside or nearly touching (1st person)
    /// only casts its shadow. Runs after the camera brain so it tests this frame's view.
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public sealed class CharacterOcclusion : MonoBehaviour
    {
        private enum Look { Normal, Faded, Hidden }

        [SerializeField] private MatchCharacterDirector cast;
        [SerializeField] private CueController cue;
        [SerializeField] private Camera view;
        [SerializeField] private Shader ditherShader;
        [Range(0f, 1f)] [SerializeField] private float fadedVisibility = 0.35f;
        [Tooltip("Body capsule (hips to head) radius used for the line-of-sight test.")]
        [SerializeField] private float bodyRadius = 0.22f;
        [SerializeField] private float hideDistance = 0.35f;

        private sealed class Entry
        {
            public MatchCharacter Character;
            public Renderer[] Renderers;
            public Transform Hips;
            public Transform Head;
            public Material[][] Original;
            public Material[][] Faded;
            public Look Look;
        }

        private readonly Entry[] entries = new Entry[2];
        private readonly Dictionary<Material, Material> fadedCache = new Dictionary<Material, Material>();

        private void LateUpdate()
        {
            for (int seat = 0; seat < entries.Length; seat++)
            {
                MatchCharacter character = cast.Player(seat);
                Entry entry = Track(seat, character);
                if (entry == null) continue;
                Apply(entry, Decide(entry));
            }
        }

        private Entry Track(int seat, MatchCharacter character)
        {
            Entry entry = entries[seat];
            if (entry != null && entry.Character == character) return entry;
            if (character == null)
            {
                entries[seat] = null;
                return null;
            }

            Renderer[] renderers = character.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            Animator animator = character.GetComponentInChildren<Animator>();
            entry = new Entry
            {
                Character = character, Renderers = renderers, Original = new Material[renderers.Length][], Faded = new Material[renderers.Length][],
                Hips = animator != null ? animator.GetBoneTransform(HumanBodyBones.Hips) : null,
                Head = animator != null ? animator.GetBoneTransform(HumanBodyBones.Head) : null,
            };
            for (int i = 0; i < renderers.Length; i++) entry.Original[i] = renderers[i].sharedMaterials;
            entries[seat] = entry;
            return entry;
        }

        // Skinned-mesh bounds are far too loose (rest-pose boxes), so the body is a hips-to-head capsule.
        private Look Decide(Entry entry)
        {
            if (entry.Hips == null || entry.Head == null) return Look.Normal;
            Vector3 hips = entry.Hips.position;
            Vector3 head = entry.Head.position + (entry.Head.position - hips).normalized * 0.12f;
            Vector3 eye = view.transform.position;
            if (SegmentDistance(eye, eye, hips, head) < hideDistance) return Look.Hidden;
            bool blocks = SegmentDistance(eye, cue.CueBallView.position, hips, head) < bodyRadius || SegmentDistance(eye, Vector3.zero, hips, head) < bodyRadius;
            return blocks ? Look.Faded : Look.Normal;
        }

        /// <summary>Closest distance between segments p1-q1 and p2-q2 (Ericson, Real-Time Collision Detection 5.1.9).</summary>
        private static float SegmentDistance(Vector3 p1, Vector3 q1, Vector3 p2, Vector3 q2)
        {
            Vector3 d1 = q1 - p1, d2 = q2 - p2, r = p1 - p2;
            float a = Vector3.Dot(d1, d1), e = Vector3.Dot(d2, d2), f = Vector3.Dot(d2, r);
            float s, t;
            if (a <= 1e-8f)
            {
                s = 0f;
                t = e > 1e-8f ? Mathf.Clamp01(f / e) : 0f;
            }
            else
            {
                float c = Vector3.Dot(d1, r);
                if (e <= 1e-8f)
                {
                    t = 0f;
                    s = Mathf.Clamp01(-c / a);
                }
                else
                {
                    float b = Vector3.Dot(d1, d2), denom = a * e - b * b;
                    s = denom > 1e-8f ? Mathf.Clamp01((b * f - c * e) / denom) : 0f;
                    t = (b * s + f) / e;
                    if (t < 0f) { t = 0f; s = Mathf.Clamp01(-c / a); }
                    else if (t > 1f) { t = 1f; s = Mathf.Clamp01((b - c) / a); }
                }
            }

            return Vector3.Distance(p1 + d1 * s, p2 + d2 * t);
        }

        private void Apply(Entry entry, Look look)
        {
            if (entry.Look == look) return;
            entry.Look = look;
            for (int i = 0; i < entry.Renderers.Length; i++)
            {
                Renderer r = entry.Renderers[i];
                r.shadowCastingMode = look == Look.Hidden ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On;
                r.sharedMaterials = look == Look.Faded ? FadedSet(entry, i) : entry.Original[i];
            }
        }

        private Material[] FadedSet(Entry entry, int index)
        {
            if (entry.Faded[index] != null) return entry.Faded[index];
            Material[] source = entry.Original[index];
            var faded = new Material[source.Length];
            for (int i = 0; i < source.Length; i++) faded[i] = Faded(source[i]);
            entry.Faded[index] = faded;
            return faded;
        }

        private Material Faded(Material source)
        {
            if (source == null || ditherShader == null) return source;
            if (fadedCache.TryGetValue(source, out Material faded)) return faded;
            faded = new Material(ditherShader) { name = source.name + " (faded)" };
            if (source.HasProperty("_BaseMap")) faded.SetTexture("_BaseMap", source.GetTexture("_BaseMap"));
            if (source.HasProperty("_BaseColor")) faded.SetColor("_BaseColor", source.GetColor("_BaseColor"));
            faded.SetFloat("_Visibility", fadedVisibility);
            fadedCache[source] = faded;
            return faded;
        }

        private void OnDestroy()
        {
            foreach (Material m in fadedCache.Values) Destroy(m);
        }
    }
}
