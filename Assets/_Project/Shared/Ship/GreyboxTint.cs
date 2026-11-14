using UnityEngine;

namespace LastSignal.Ship
{
    /// <summary>Gives a greybox renderer its colour at runtime (works with URP Lit and the built-in Standard shader).</summary>
    [DisallowMultipleComponent]
    public class GreyboxTint : MonoBehaviour
    {
        public Color color = Color.grey;
        public bool emissive;
        [Tooltip("Emission strength when emissive is on.")]
        public float emissionBoost = 2.5f;

        bool _applied;
        static Shader _lit;

        void Awake() { Apply(); }

        public void Apply()
        {
            Renderer r = GetComponent<Renderer>();
            if (r == null) return;
            // Primitives ship with the built-in Standard material, which renders magenta under URP in a player build.
            // Start from URP Lit instead (it is always in the build because Resources/M3Fx materials use it).
            if (!_applied && _lit == null) _lit = Shader.Find("Universal Render Pipeline/Lit");
            Material m = _applied ? r.material : (_lit != null ? new Material(_lit) : new Material(r.sharedMaterial));
            r.material = m;
            _applied = true;
            m.color = color;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (emissive)
            {
                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", color * emissionBoost);
            }
            if (emissive || color.a < 1f)
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        /// <summary>Changes the colour later (for blinking lights, damage states).</summary>
        public void SetColor(Color c)
        {
            color = c;
            Apply();
        }
    }
}
