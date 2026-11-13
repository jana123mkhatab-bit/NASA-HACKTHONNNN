using System.Collections.Generic;
using UnityEngine;

namespace LastSignal
{
    /// <summary>
    /// Placeholder sound, synthesised in code so the game is never silent and ships no audio files.
    /// Person 5 can replace any clip by dropping an AudioClip named after the Id into
    /// Resources/LastSignal/Audio (for example "Click"); a file there wins over the synthesised one.
    /// </summary>
    public static class Sfx
    {
        public enum Id { Hover, Click, Confirm, Deny, Snap, Lock, Type, Beep, Success, Fail, Whoosh, Engine, Ambience }

        const int Rate = 44100;
        static readonly Dictionary<Id, AudioClip> Clips = new Dictionary<Id, AudioClip>();
        static GameObject _host;
        static AudioSource[] _pool;
        static AudioSource _ambience;
        static int _next;

        public static float Volume = 0.8f;

        static void EnsureHost()
        {
            if (_host != null) return;
            _host = new GameObject("Sfx");
            Object.DontDestroyOnLoad(_host);
            _pool = new AudioSource[6];
            for (int i = 0; i < _pool.Length; i++)
            {
                _pool[i] = _host.AddComponent<AudioSource>();
                _pool[i].playOnAwake = false;
                _pool[i].spatialBlend = 0f;
            }
            _ambience = _host.AddComponent<AudioSource>();
            _ambience.playOnAwake = false; _ambience.loop = true; _ambience.spatialBlend = 0f;
        }

        public static void Play(Id id, float volume = 1f, float pitch = 1f)
        {
            if (!Application.isPlaying) return;
            EnsureHost();
            var src = _pool[_next];
            _next = (_next + 1) % _pool.Length;
            src.pitch = pitch;
            src.PlayOneShot(Clip(id), volume * Volume);
        }

        /// <summary>Start a looping sound on its own source. Stop it with <see cref="StopLoop"/>.</summary>
        public static AudioSource Loop(Id id, float volume)
        {
            if (!Application.isPlaying) return null;
            EnsureHost();
            var src = _host.AddComponent<AudioSource>();
            src.clip = Clip(id); src.loop = true; src.volume = volume * Volume; src.spatialBlend = 0f;
            src.Play();
            return src;
        }

        public static void StopLoop(AudioSource src) { if (src != null) Object.Destroy(src); }

        /// <summary>Quiet room tone under the whole game.</summary>
        public static void Ambience(bool on, float volume = 0.22f)
        {
            if (!Application.isPlaying) return;
            EnsureHost();
            if (!on) { _ambience.Stop(); return; }
            if (_ambience.isPlaying) { _ambience.volume = volume * Volume; return; }
            _ambience.clip = Clip(Id.Ambience);
            _ambience.volume = volume * Volume;
            _ambience.Play();
        }

        static AudioClip Clip(Id id)
        {
            if (Clips.TryGetValue(id, out var c) && c != null) return c;
            c = Resources.Load<AudioClip>("LastSignal/Audio/" + id);
            if (c == null) c = Synth(id);
            Clips[id] = c;
            return c;
        }

        // ------------------------------------------------------------------ synthesis

        static AudioClip Make(string name, float seconds, System.Func<float, float, float> sample)
        {
            int n = Mathf.Max(1, Mathf.RoundToInt(seconds * Rate));
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                data[i] = Mathf.Clamp(sample(t, t / seconds), -1f, 1f);
            }
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static float Sin(float hz, float t) => Mathf.Sin(2f * Mathf.PI * hz * t);
        static float Decay(float u, float power) => Mathf.Pow(1f - u, power);

        static AudioClip Synth(Id id)
        {
            var rng = new System.Random(1234 + (int)id);
            float Noise() => (float)(rng.NextDouble() * 2.0 - 1.0);
            float low = 0f; // one-pole low-pass state shared by the noise voices below

            switch (id)
            {
                case Id.Hover:
                    return Make("Hover", 0.035f, (t, u) => Sin(1500f, t) * Decay(u, 2f) * 0.10f);
                case Id.Click:
                    return Make("Click", 0.07f, (t, u) => (Sin(880f, t) + 0.4f * Sin(1760f, t)) * Decay(u, 3f) * 0.22f);
                case Id.Confirm:
                    return Make("Confirm", 0.22f, (t, u) => Sin(u < 0.45f ? 660f : 990f, t) * Decay(u, 1.5f) * 0.22f);
                case Id.Deny:
                    return Make("Deny", 0.18f, (t, u) => Mathf.Sign(Sin(150f, t)) * Decay(u, 1.2f) * 0.10f);
                case Id.Snap:
                    return Make("Snap", 0.20f, (t, u) =>
                    {
                        low += (Noise() - low) * 0.25f;
                        return low * Decay(u, 6f) * 0.7f + Sin(95f - 40f * u, t) * Decay(u, 2.5f) * 0.45f;
                    });
                case Id.Lock:
                    return Make("Lock", 0.16f, (t, u) =>
                    {
                        low += (Noise() - low) * 0.5f;
                        float tick = u < 0.25f ? low * 0.5f : 0f;
                        return tick + Sin(240f, t) * Decay(u, 4f) * 0.25f + Sin(1250f, t) * Decay(u, 8f) * 0.12f;
                    });
                case Id.Type:
                    return Make("Type", 0.018f, (t, u) => Sin(2100f, t) * Decay(u, 2f) * 0.06f);
                case Id.Beep:
                    return Make("Beep", 0.16f, (t, u) => Sin(1040f, t) * Mathf.Min(1f, u * 20f) * Mathf.Min(1f, (1f - u) * 12f) * 0.18f);
                case Id.Success:
                    return Make("Success", 0.75f, (t, u) =>
                    {
                        float hz = u < 0.22f ? 523.25f : u < 0.44f ? 659.25f : 783.99f;
                        float local = u < 0.22f ? u / 0.22f : u < 0.44f ? (u - 0.22f) / 0.22f : (u - 0.44f) / 0.56f;
                        return (Sin(hz, t) + 0.3f * Sin(hz * 2f, t)) * Decay(local, 1.4f) * 0.2f;
                    });
                case Id.Fail:
                    return Make("Fail", 0.6f, (t, u) => (Sin(196f - 70f * u, t) + 0.5f * Mathf.Sign(Sin(98f - 35f * u, t))) * Decay(u, 1.2f) * 0.16f);
                case Id.Whoosh:
                    return Make("Whoosh", 0.5f, (t, u) =>
                    {
                        low += (Noise() - low) * (0.03f + 0.25f * Mathf.Sin(u * Mathf.PI));
                        return low * Mathf.Sin(u * Mathf.PI) * 0.6f;
                    });
                case Id.Engine:
                    return Make("Engine", 2.0f, (t, u) =>
                    {
                        low += (Noise() - low) * 0.06f;
                        float rumble = Sin(48f, t) * 0.25f + Sin(71f, t) * 0.15f;
                        // 2.0 s is a whole number of cycles for both tones, so the loop is seamless.
                        return low * 1.6f + rumble;
                    });
                default: // Ambience
                    return Make("Ambience", 4.0f, (t, u) =>
                    {
                        low += (Noise() - low) * 0.012f;
                        return low * 0.8f + Sin(55f, t) * 0.035f + Sin(110f, t) * 0.012f;
                    });
            }
        }
    }
}
