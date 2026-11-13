using UnityEngine;

namespace LastSignal.Core
{
    /// <summary>Gentle bob and tilt for the mascot, with an optional spinning halo ring.</summary>
    public class MascotIdle : MonoBehaviour
    {
        public float bobHeight = 0.08f;
        public float bobSpeed = 1.2f;
        public float tiltDegrees = 4f;
        public Transform halo;
        public float haloSpinDegreesPerSecond = 25f;

        Vector3 _start;

        void Start() { _start = transform.localPosition; }

        void Update()
        {
            float t = Time.time * bobSpeed;
            transform.localPosition = _start + Vector3.up * (Mathf.Sin(t) * bobHeight);
            transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 0.5f) * tiltDegrees);
            if (halo != null) halo.Rotate(Vector3.up, haloSpinDegreesPerSecond * Time.deltaTime, Space.World);
        }
    }
}
