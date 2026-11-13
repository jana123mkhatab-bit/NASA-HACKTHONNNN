using UnityEngine;

namespace LastSignal
{
    /// <summary>Slow rotation for planets, clouds, the sun corona, beacons. Degrees per second per axis.</summary>
    public class SlowSpin : MonoBehaviour
    {
        public Vector3 degreesPerSecond = new Vector3(0, 2f, 0);
        void Update() { transform.Rotate(degreesPerSecond * Time.deltaTime, Space.Self); }
    }
}
