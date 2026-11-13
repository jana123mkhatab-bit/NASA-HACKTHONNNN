using UnityEngine;

namespace LastSignal
{
    /// <summary>
    /// Makes a light or a glowing object feel alive: a slow breathing pulse, or a hard on/off blink
    /// for beacons. Put it on a Light, or on any object to blink its children.
    /// </summary>
    public class LightFlicker : MonoBehaviour
    {
        public enum Mode { Breathe, Blink, Noise }
        public Mode mode = Mode.Breathe;
        public float speed = 1f;
        [Range(0f, 1f)] public float depth = 0.25f;
        public float phase;
        public GameObject blinkTarget;

        Light _light;
        float _base;

        void Awake()
        {
            _light = GetComponent<Light>();
            if (_light != null) _base = _light.intensity;
        }

        void Update()
        {
            float t = Time.time * speed + phase;
            float k;
            switch (mode)
            {
                case Mode.Blink: k = (t % 1f) < 0.5f ? 1f : 0f; break;
                case Mode.Noise: k = Mathf.PerlinNoise(t * 9f, phase); break;
                default: k = 0.5f + 0.5f * Mathf.Sin(t * Mathf.PI * 2f); break;
            }
            if (_light != null) _light.intensity = _base * (1f - depth + depth * k);
            if (blinkTarget != null && mode == Mode.Blink) blinkTarget.SetActive(k > 0.5f);
        }
    }
}
