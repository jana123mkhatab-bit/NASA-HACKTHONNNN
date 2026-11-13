using UnityEngine;

namespace LastSignal
{
    /// <summary>Cinematic camera drift: orbits a target at a fixed radius and height, always looking at it.</summary>
    public class CameraOrbit : MonoBehaviour
    {
        public Transform target;
        public float radius = 40f;
        public float height = 8f;
        public float degreesPerSecond = 3f;
        public float startAngle = 0f;
        float _angle;

        void OnEnable() { _angle = startAngle; Apply(); }

        void LateUpdate() { _angle += degreesPerSecond * Time.deltaTime; Apply(); }

        void Apply()
        {
            Vector3 c = target != null ? target.position : Vector3.zero;
            float a = _angle * Mathf.Deg2Rad;
            transform.position = c + new Vector3(Mathf.Sin(a) * radius, height, Mathf.Cos(a) * radius);
            transform.LookAt(c + Vector3.up * (height * 0.25f));
        }
    }
}
