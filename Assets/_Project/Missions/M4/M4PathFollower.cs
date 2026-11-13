using UnityEngine;

namespace LastSignal.M4
{
    /// <summary>Moves along a polyline at constant speed and loops (the light pulse running along the cable).</summary>
    public class M4PathFollower : MonoBehaviour
    {
        Vector3[] _path;
        float[] _cum;
        float _speed, _d;

        public void Init(Vector3[] path, float speed)
        {
            _path = path; _speed = speed;
            _cum = new float[path.Length];
            for (int i = 1; i < path.Length; i++) _cum[i] = _cum[i - 1] + Vector3.Distance(path[i - 1], path[i]);
        }

        public float Length => _cum != null && _cum.Length > 0 ? _cum[_cum.Length - 1] : 0f;

        public static Vector3 Sample(Vector3[] path, float[] cum, float d)
        {
            int i = 1;
            while (i < path.Length - 1 && cum[i] < d) i++;
            float seg = Mathf.Max(0.0001f, cum[i] - cum[i - 1]);
            return Vector3.Lerp(path[i - 1], path[i], Mathf.Clamp01((d - cum[i - 1]) / seg));
        }

        void Update()
        {
            if (_path == null || _path.Length < 2) return;
            _d += _speed * Time.deltaTime;
            if (_d > Length + 6f) _d = 0f; // short pause at the ship end
            transform.position = Sample(_path, _cum, Mathf.Min(_d, Length)) + Vector3.up * 0.05f;
        }
    }
}
