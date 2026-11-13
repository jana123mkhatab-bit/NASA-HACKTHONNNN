using UnityEngine;

namespace LastSignal.M4
{
    /// <summary>Pulses an emissive renderer (via MaterialPropertyBlock, materials stay shared) and an optional light.</summary>
    public class M4Blink : MonoBehaviour
    {
        public Light target;
        public float period = 2f;
        public float phase;
        [Range(0, 1)] public float minIntensity = 0.2f;
        public bool sharp;

        Renderer _r;
        MaterialPropertyBlock _mpb;
        Color _baseEmission;
        float _baseLight;
        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        void Start()
        {
            _r = GetComponent<Renderer>();
            if (_r != null && _r.sharedMaterial != null && _r.sharedMaterial.HasProperty(EmissionId))
                _baseEmission = _r.sharedMaterial.GetColor(EmissionId);
            _mpb = new MaterialPropertyBlock();
            if (target != null) _baseLight = target.intensity;
        }

        void Update()
        {
            float t = (Time.time / Mathf.Max(0.05f, period) + phase) % 1f;
            float k = sharp ? (t < 0.18f ? 1f : 0f) : 0.5f + 0.5f * Mathf.Cos(t * Mathf.PI * 2f);
            k = Mathf.Lerp(minIntensity, 1f, k);
            if (_r != null)
            {
                _r.GetPropertyBlock(_mpb);
                _mpb.SetColor(EmissionId, _baseEmission * k);
                _r.SetPropertyBlock(_mpb);
            }
            if (target != null) target.intensity = _baseLight * k;
        }
    }
}
