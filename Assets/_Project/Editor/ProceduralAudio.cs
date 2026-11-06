using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BilliardGame.Editor
{
    /// <summary>
    /// Synthesises the table sounds (modal sine partials + filtered noise) and saves them as 16-bit WAV assets,
    /// so the project carries no third-party audio licences.
    /// </summary>
    internal static class ProceduralAudio
    {
        public const string Folder = "Assets/_Project/Audio";
        private const int SampleRate = 44100;

        /// <summary>Phenolic ball on ball: a bright, very short clack.</summary>
        public static AudioClip BallHit()
        {
            float[] s = Buffer(0.09f);
            AddPartial(s, 2650f, 0.010f, 1.0f);
            AddPartial(s, 4180f, 0.007f, 0.6f);
            AddPartial(s, 6320f, 0.004f, 0.35f);
            AddNoise(s, 0.002f, 0.5f, 0.9f, seed: 1);
            return Save("BallHit", s);
        }

        /// <summary>Ball into rubber cushion: a dull, low thud.</summary>
        public static AudioClip CushionHit()
        {
            float[] s = Buffer(0.22f);
            AddPartial(s, 120f, 0.045f, 1.0f, attack: 0.003f);
            AddPartial(s, 190f, 0.030f, 0.6f, attack: 0.003f);
            AddPartial(s, 560f, 0.008f, 0.25f);
            AddNoise(s, 0.025f, 0.35f, 0.08f, seed: 2);
            return Save("CushionHit", s);
        }

        /// <summary>Ball dropping into a pocket: thump, a few rattles against the liner, then a short roll.</summary>
        public static AudioClip PocketDrop()
        {
            float[] s = Buffer(0.7f);
            AddPartial(s, 85f, 0.06f, 1.0f, attack: 0.004f);
            AddPartial(s, 140f, 0.04f, 0.5f, attack: 0.004f);
            var random = new System.Random(3);
            float when = 0.07f;
            float level = 0.45f;
            for (int i = 0; i < 4; i++)
            {
                float pitch = 1300f + (float)random.NextDouble() * 700f;
                AddPartial(s, pitch, 0.006f, level, start: when);
                AddPartial(s, pitch * 1.58f, 0.004f, level * 0.5f, start: when);
                when += 0.06f + (float)random.NextDouble() * 0.07f;
                level *= 0.6f;
            }

            AddNoise(s, 0.18f, 0.12f, 0.03f, seed: 4, start: 0.05f);
            return Save("PocketDrop", s);
        }

        /// <summary>Leather tip on the cue ball: a soft knock with a little wood body.</summary>
        public static AudioClip CueStrike()
        {
            float[] s = Buffer(0.12f);
            AddPartial(s, 950f, 0.008f, 0.7f);
            AddPartial(s, 1650f, 0.005f, 0.4f);
            AddPartial(s, 230f, 0.025f, 0.6f, attack: 0.001f);
            AddNoise(s, 0.006f, 0.6f, 0.25f, seed: 5);
            return Save("CueStrike", s);
        }

        /// <summary>Hall background (M1.6.3): room tone, a murmur of far voices and the odd cup clink; seamless 12 s loop.</summary>
        public static AudioClip HallAmbience()
        {
            const float seconds = 12f, fade = 1.5f;
            float[] s = Buffer(seconds + fade);
            var random = new System.Random(1606);
            AddBrownNoise(s, 0.5f, random);
            for (int voice = 0; voice < 7; voice++) AddMurmur(s, 0.35f, random);
            for (int i = 0; i < 5; i++)
            {
                float at = (float)random.NextDouble() * seconds;
                AddPartial(s, 2600f + (float)random.NextDouble() * 900f, 0.06f, 0.05f, 0.0005f, at);
                AddPartial(s, 4100f + (float)random.NextDouble() * 700f, 0.04f, 0.03f, 0.0005f, at);
            }

            return Save("HallAmbience", Loop(s, fade), AudioClipLoadType.CompressedInMemory);
        }

        /// <summary>Low lounge pad (M1.6.3): four soft chords, 4 s each, seamless 16 s loop. Original, no licence.</summary>
        public static AudioClip HallMusic()
        {
            const float chord = 4f, fade = 1.2f;
            float[][] chords =
            {
                new[] { 110f, 220f, 261.63f, 329.63f },   // Am
                new[] { 87.31f, 174.61f, 220f, 261.63f }, // F
                new[] { 130.81f, 196f, 261.63f, 329.63f }, // C
                new[] { 98f, 196f, 246.94f, 293.66f },    // G
            };
            float[] s = Buffer(chord * chords.Length + fade);
            for (int c = 0; c <= chords.Length; c++)
            {
                float[] notes = chords[c % chords.Length];
                for (int n = 0; n < notes.Length; n++) AddPad(s, notes[n], c * chord, chord + 0.8f, n == 0 ? 0.5f : 0.22f);
            }

            return Save("HallMusic", Loop(s, fade), AudioClipLoadType.CompressedInMemory);
        }

        private static void AddBrownNoise(float[] samples, float amplitude, System.Random random)
        {
            float brown = 0f, low = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                brown = brown * 0.998f + ((float)random.NextDouble() * 2f - 1f) * 0.04f;
                low += (brown - low) * 0.05f;
                samples[i] += low * amplitude;
            }
        }

        // One far voice: band-passed noise gated by syllables (3-5 Hz) inside phrases that come and go.
        private static void AddMurmur(float[] samples, float amplitude, System.Random random)
        {
            float centre = 250f + (float)random.NextDouble() * 500f;
            float w = 2f * Mathf.PI * centre / SampleRate, r = 0.985f;
            float a1 = 2f * r * Mathf.Cos(w), a2 = r * r, y1 = 0f, y2 = 0f;
            float rate = 3f + (float)random.NextDouble() * 2f, phase = (float)random.NextDouble() * 6.283f;
            float gate = 0f, gateTarget = 0f, nextPhrase = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / SampleRate;
                if (t >= nextPhrase)
                {
                    gateTarget = random.NextDouble() < 0.6 ? 1f : 0f;
                    nextPhrase = t + 1f + (float)random.NextDouble() * 2.5f;
                }

                gate += (gateTarget - gate) * 0.0004f;
                float x = (float)random.NextDouble() * 2f - 1f;
                float y = (1f - r) * x + a1 * y1 - a2 * y2;
                y2 = y1;
                y1 = y;
                float syllable = Mathf.Sin(2f * Mathf.PI * rate * t + phase + Mathf.Sin(t * 1.7f + phase));
                samples[i] += y * amplitude * gate * syllable * syllable * 40f;
            }
        }

        // Soft pad note: sine plus a quiet octave, raised-cosine swell so chords overlap without clicks.
        private static void AddPad(float[] samples, float frequency, float start, float length, float amplitude)
        {
            int from = Mathf.RoundToInt(start * SampleRate), count = Mathf.RoundToInt(length * SampleRate);
            for (int i = 0; i < count && from + i < samples.Length; i++)
            {
                float t = (float)i / SampleRate;
                float envelope = 0.5f - 0.5f * Mathf.Cos(2f * Mathf.PI * i / count);
                float wobble = 1f + 0.002f * Mathf.Sin(2f * Mathf.PI * 0.3f * t);
                float phase = 2f * Mathf.PI * frequency * wobble * t;
                samples[from + i] += amplitude * envelope * (Mathf.Sin(phase) + 0.2f * Mathf.Sin(2f * phase));
            }
        }

        /// <summary>Cross-fades the tail (last <paramref name="fade"/> s) into the head so the clip loops seamlessly.</summary>
        private static float[] Loop(float[] samples, float fade)
        {
            int f = Mathf.RoundToInt(fade * SampleRate), length = samples.Length - f;
            var loop = new float[length];
            Array.Copy(samples, loop, length);
            for (int i = 0; i < f; i++)
            {
                float k = (float)i / f;
                loop[i] = samples[i] * k + samples[length + i] * (1f - k);
            }

            return loop;
        }

        private static float[] Buffer(float seconds) => new float[Mathf.CeilToInt(seconds * SampleRate)];

        /// <summary>Exponentially decaying sine (one vibration mode) with a short linear attack against clicks.</summary>
        private static void AddPartial(float[] samples, float frequency, float decay, float amplitude, float attack = 0.0005f, float start = 0f)
        {
            int first = Mathf.RoundToInt(start * SampleRate);
            for (int i = first; i < samples.Length; i++)
            {
                float t = (i - first) / (float)SampleRate;
                float envelope = Mathf.Exp(-t / decay) * Mathf.Min(1f, t / attack);
                if (envelope < 1e-4f && t > attack) break;
                samples[i] += amplitude * envelope * Mathf.Sin(2f * Mathf.PI * frequency * t);
            }
        }

        /// <summary>Decaying white noise through a one-pole low-pass; `brightness` 0..1 is the filter coefficient.</summary>
        private static void AddNoise(float[] samples, float decay, float amplitude, float brightness, int seed, float start = 0f)
        {
            var random = new System.Random(seed);
            int first = Mathf.RoundToInt(start * SampleRate);
            float filtered = 0f;
            for (int i = first; i < samples.Length; i++)
            {
                float t = (i - first) / (float)SampleRate;
                float white = (float)(random.NextDouble() * 2.0 - 1.0);
                filtered += brightness * (white - filtered);
                samples[i] += amplitude * Mathf.Exp(-t / decay) * filtered;
            }
        }

        private static AudioClip Save(string name, float[] samples, AudioClipLoadType loadType = AudioClipLoadType.DecompressOnLoad)
        {
            float peak = 0f;
            foreach (float v in samples) peak = Mathf.Max(peak, Mathf.Abs(v));
            float gain = peak > 0f ? 0.9f / peak : 0f;

            BallMaterialFactory.EnsureFolder(Folder);
            string path = $"{Folder}/{name}.wav";
            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (var writer = new BinaryWriter(stream))
            {
                WriteWavHeader(writer, samples.Length);
                foreach (float v in samples) writer.Write((short)Mathf.Clamp(Mathf.RoundToInt(v * gain * short.MaxValue), short.MinValue, short.MaxValue));
            }

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (AudioImporter)AssetImporter.GetAtPath(path);
            importer.forceToMono = true;
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = loadType;   // tiny hit clips decompress (no decode latency); long loops stay compressed
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path) ?? throw new InvalidOperationException("Audio import failed: " + path);
        }

        private static void WriteWavHeader(BinaryWriter writer, int sampleCount)
        {
            const short channels = 1;
            const short bitsPerSample = 16;
            int dataBytes = sampleCount * channels * bitsPerSample / 8;
            writer.Write(new[] { 'R', 'I', 'F', 'F' });
            writer.Write(36 + dataBytes);
            writer.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
            writer.Write(16);                 // PCM fmt chunk size
            writer.Write((short)1);           // PCM
            writer.Write(channels);
            writer.Write(SampleRate);
            writer.Write(SampleRate * channels * bitsPerSample / 8);
            writer.Write((short)(channels * bitsPerSample / 8));
            writer.Write(bitsPerSample);
            writer.Write(new[] { 'd', 'a', 't', 'a' });
            writer.Write(dataBytes);
        }
    }
}
