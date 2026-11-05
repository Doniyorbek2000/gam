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

        private static AudioClip Save(string name, float[] samples)
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
            settings.loadType = AudioClipLoadType.DecompressOnLoad;   // tiny clips: no decode latency on hit
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
