using UnityEngine;

namespace LastSignal.Ship
{
    /// <summary>Grows a flash sphere, fades its light, then destroys it. Created by GreyboxKit.Flash.</summary>
    public class FxFlash : MonoBehaviour
    {
        public float maxScale = 10f;
        public float duration = 0.6f;
        public Light pulse;
        float _t;

        void Update()
        {
            _t += Time.deltaTime;
            float k = Mathf.Clamp01(_t / Mathf.Max(0.01f, duration));
            transform.localScale = Vector3.one * Mathf.Lerp(0.5f, maxScale, Mathf.Sqrt(k));
            if (pulse != null) pulse.intensity = Mathf.Lerp(8f, 0f, k);
            if (k >= 1f) Destroy(gameObject);
        }
    }
}
