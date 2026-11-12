using UnityEngine;

namespace Yx.Game
{
    public class AudioSynth : MonoBehaviour
    {
        AudioSource source;
        AudioClip drum;
        AudioClip breath;
        AudioClip hurt;
        AudioClip gate;
        AudioClip tick;

        public void Init()
        {
            source = gameObject.GetComponent<AudioSource>();
            if (source == null) source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            drum = NoiseTone("drum", 96f, 0.18f, 10f, 0.45f);
            breath = NoiseTone("breath", 420f, 0.12f, 6f, 0.08f);
            hurt = NoiseTone("hurt", 70f, 0.16f, 14f, 0.2f);
            gate = Tone("gate", 520f, 0.08f, 4f);
            tick = Tone("tick", 880f, 0.04f, 8f);
        }

        public void Drum() { Play(drum, 0.55f); }
        public void Breath() { Play(breath, 0.25f); }
        public void Hurt() { Play(hurt, 0.5f); }
        public void Gate() { Play(gate, 0.35f); }
        public void Tick() { Play(tick, 0.2f); }

        void Play(AudioClip clip, float volume)
        {
            if (source == null || clip == null) return;
            source.PlayOneShot(clip, volume);
        }

        static AudioClip Tone(string name, float freq, float dur, float decay)
        {
            int rate = 22050;
            int n = Mathf.Max(1, (int)(rate * dur));
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                data[i] = Mathf.Sin(Mathf.PI * 2f * freq * t) * Mathf.Exp(-decay * t) * 0.4f;
            }
            return Clip(name, rate, data);
        }

        static AudioClip NoiseTone(string name, float freq, float dur, float decay, float noise)
        {
            int rate = 22050;
            int n = Mathf.Max(1, (int)(rate * dur));
            var data = new float[n];
            float seed = 0.17f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                seed = Mathf.Repeat(seed * 37.1f + 0.13f, 1f);
                float nse = (seed - 0.5f) * noise;
                float env = Mathf.Exp(-decay * t);
                data[i] = (Mathf.Sin(Mathf.PI * 2f * freq * t) * 0.55f + nse) * env;
            }
            return Clip(name, rate, data);
        }

        static AudioClip Clip(string name, int rate, float[] data)
        {
            AudioClip clip = AudioClip.Create(name, data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
