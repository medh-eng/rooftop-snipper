using UnityEngine;

namespace HarborWatch
{
    /// <summary>
    /// Small synthesized sound set, generated at start-up so the project ships
    /// no extra audio files. Matches the browser prototype's WebAudio cues.
    /// </summary>
    public sealed class FindTheOneAudio
    {
        const int Rate = 22050;
        public AudioClip Beat, Lock, Shot, Good, Bad, Horn, Ping, Glint, Tick, Clue, Block, Select, Rewind, Forward;

        public FindTheOneAudio()
        {
            Beat = Make("Heartbeat", .4f, (t, i) => Env(t, .14f) * Sine(58, t) * .9f + Env(t - .16f, .16f) * Sine(52, t) * .7f);
            Lock = Make("Lock", .1f, (t, i) => Env(t, .08f) * Sine(1500, t) * .4f);
            Shot = Make("Pulse", .6f, (t, i) => Env(t, .35f) * Noise(i) * .7f + Env(t, .5f) * Saw(220 * (1 - t * .9f), t) * .25f);
            Good = Make("Tagged", .8f, (t, i) => Arp(t, new[] { 523f, 659f, 784f, 1046f }, .09f, .35f) * .35f);
            Bad = Make("Wrong", .6f, (t, i) => Env(t, .45f) * Saw(160 * (1 - t * .5f), t) * .35f);
            Horn = Make("Ferry horn", 1.8f, (t, i) => Env(t, 1.6f, .05f) * (Saw(110, t) * .3f + Saw(165, t) * .18f));
            Ping = Make("Cell ping", .65f, (t, i) => Env(t, .25f) * Sine(1800 - t * 1800, t) * .3f + Env(t - .3f, .25f) * Sine(1800 - (t - .3f) * 1800, t) * .25f);
            Glint = Make("Glint", .3f, (t, i) => Env(t, .25f) * Sine(3200, t) * .3f);
            Tick = Make("Tick", .04f, (t, i) => Env(t, .03f) * Square(1000, t) * .15f);
            Clue = Make("Intel", .3f, (t, i) => Env(t, .12f) * Sine(880, t) * .35f + Env(t - .08f, .18f) * Sine(1320, t) * .3f);
            Block = Make("Blocked", .2f, (t, i) => Env(t, .15f) * Square(300 - t * 600, t) * .2f);
            Rewind = Make("Echo rewind", .5f, (t, i) => Env(t, .5f) * Saw(900 - t * 1300, t) * .12f + Env(t, .45f) * Noise(i) * .1f);
            Forward = Make("Echo forward", .35f, (t, i) => Env(t, .35f) * Saw(300 + t * 900, t) * .1f);
            Select = Make("Select", .07f, (t, i) => Env(t, .06f) * Square(620, t) * .12f);
        }

        delegate float Wave(float t, int i);

        static AudioClip Make(string name, float seconds, Wave wave)
        {
            int n = Mathf.CeilToInt(seconds * Rate);
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(wave(i / (float)Rate, i), -1f, 1f);
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static float Env(float t, float length, float attack = .01f)
        {
            if (t < 0f || t > length) return 0f;
            return Mathf.Min(1f, t / attack) * Mathf.Exp(-5f * t / length);
        }
        static float Sine(float f, float t) { return Mathf.Sin(2f * Mathf.PI * f * t); }
        static float Square(float f, float t) { return Mathf.Sign(Sine(f, t)); }
        static float Saw(float f, float t) { float x = f * t; return 2f * (x - Mathf.Floor(x + .5f)); }
        static uint noiseState = 22222;
        static float Noise(int i) { noiseState = noiseState * 1664525u + 1013904223u; return (noiseState >> 9) / 4194304f - 1f; }
        static float Arp(float t, float[] notes, float gap, float length)
        {
            float sum = 0f;
            for (int k = 0; k < notes.Length; k++) sum += Env(t - k * gap, length) * Sine(notes[k], t);
            return sum;
        }
    }
}
