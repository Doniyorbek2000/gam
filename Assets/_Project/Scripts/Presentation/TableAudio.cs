using BilliardGame.Physics;
using UnityEngine;

namespace BilliardGame.Presentation
{
    /// <summary>
    /// Plays ball, cushion, pocket and cue sounds as playback reaches each simulation event.
    /// Loudness follows impact speed; pan follows the ball's position along the table.
    /// </summary>
    public sealed class TableAudio : MonoBehaviour
    {
        [SerializeField] private TablePlayback playback;
        [SerializeField] private AudioClip ballHit;
        [SerializeField] private AudioClip cushionHit;
        [SerializeField] private AudioClip pocketDrop;
        [SerializeField] private AudioClip cueStrike;
        [Tooltip("Simultaneous sounds; a break produces dozens of clicks in the first half second.")]
        [SerializeField] private int voices = 12;
        [SerializeField] private float ballFullVolumeSpeed = 3f;
        [SerializeField] private float cushionFullVolumeSpeed = 2.5f;
        [SerializeField] private float pocketFullVolumeSpeed = 2f;
        [Tooltip("Resting contacts report tiny approach speeds; they would only add chatter.")]
        [SerializeField] private float minAudibleSpeed = 0.03f;
        [SerializeField] private float panPerMetre = 0.35f;

        /// <summary>Settings ▸ Sound ▸ Effects.</summary>
        public static float EffectsVolume { get; set; } = 1f;

        private AudioSource[] sources;
        private int nextVoice;

        public void PlayCueStrike(float power01) => Play(cueStrike, Mathf.Lerp(0.3f, 1f, Mathf.Clamp01(power01)), 0f, 1f);

        private void Awake()
        {
            sources = new AudioSource[Mathf.Max(1, voices)];
            for (int i = 0; i < sources.Length; i++)
            {
                AudioSource source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                sources[i] = source;
            }
        }

        private void OnEnable() => playback.EventPlayed += OnEventPlayed;

        private void OnDisable() => playback.EventPlayed -= OnEventPlayed;

        private void OnEventPlayed(SimulationEvent e)
        {
            // A ball creeping into a pocket still drops audibly; only contacts are gated.
            if (e.Type != SimulationEventType.BallPocketed && e.Speed < minAudibleSpeed) return;

            float pan = playback.GetView(e.BallId).transform.position.x * panPerMetre;
            float pitch = 1f + Random.Range(-0.04f, 0.04f);
            switch (e.Type)
            {
                case SimulationEventType.BallCollision:
                    Play(ballHit, Loudness(e.Speed / ballFullVolumeSpeed), pan, pitch);
                    break;
                case SimulationEventType.CushionCollision:
                    Play(cushionHit, Loudness(e.Speed / cushionFullVolumeSpeed), pan, pitch);
                    break;
                case SimulationEventType.BallPocketed:
                    Play(pocketDrop, Mathf.Lerp(0.5f, 1f, (float)(e.Speed / pocketFullVolumeSpeed)), pan, pitch);
                    break;
            }
        }

        // Perceptual curve: soft touches stay audible, hard hits do not clip.
        private static float Loudness(double ratio) => Mathf.Pow(Mathf.Clamp01((float)ratio), 0.6f);

        private void Play(AudioClip clip, float volume, float pan, float pitch)
        {
            if (clip == null) return;
            AudioSource voice = sources[nextVoice];
            nextVoice = (nextVoice + 1) % sources.Length;
            voice.clip = clip;
            voice.volume = volume * EffectsVolume;
            voice.panStereo = Mathf.Clamp(pan, -1f, 1f);
            voice.pitch = pitch;
            voice.Play();
        }
    }
}
