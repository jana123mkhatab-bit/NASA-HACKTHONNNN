using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastSignal.Ship
{
    /// <summary>
    /// Floating origin: when the focus (the ship) gets further than `threshold` from (0,0,0), every root object in
    /// its scene (and the main camera) is moved back by that amount, so the ship always flies near the origin and
    /// large sector distances do not jitter. The total shift is kept in doubles: use ToSector / ToWorld for any
    /// position that must survive a shift (checkpoints, fixed sector coordinates).
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public class FloatingOrigin : MonoBehaviour
    {
        public Transform focus;
        [Tooltip("Recentre when the focus is this far from the world origin (metres).")]
        public float threshold = 2000f;

        static double _ox, _oy, _oz;

        /// <summary>Raised after a shift with the amount everything moved by (world positions changed by -shift).</summary>
        public static event Action<Vector3> Shifted;

        /// <summary>Total distance the world has been shifted (sector position = world position + Offset).</summary>
        public static Vector3 Offset { get { return new Vector3((float)_ox, (float)_oy, (float)_oz); } }

        public static Vector3 ToSector(Vector3 world)
        {
            return new Vector3((float)(world.x + _ox), (float)(world.y + _oy), (float)(world.z + _oz));
        }

        public static Vector3 ToWorld(Vector3 sector)
        {
            return new Vector3((float)(sector.x - _ox), (float)(sector.y - _oy), (float)(sector.z - _oz));
        }

        public static void ResetOffset()
        {
            _ox = _oy = _oz = 0d;
        }

        /// <summary>Records a shift without moving objects (used by Recenter, and by tests).</summary>
        public static void AddShift(Vector3 shift)
        {
            _ox += shift.x;
            _oy += shift.y;
            _oz += shift.z;
        }

        void Awake()
        {
            ResetOffset();   // a fresh scene starts at sector origin
        }

        void FixedUpdate()
        {
            if (focus == null) return;
            if (FlightModel.ShouldRecenter(focus.position, threshold)) Recenter(focus.position);
        }

        /// <summary>Moves every root object of the focus scene (and the main camera) by -shift.</summary>
        public void Recenter(Vector3 shift)
        {
            Scene scene = focus != null ? focus.gameObject.scene : gameObject.scene;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++) roots[i].transform.position -= shift;

            Camera cam = Camera.main;
            if (cam != null && cam.gameObject.scene != scene && cam.transform.parent == null) cam.transform.position -= shift;

            Physics.SyncTransforms();
            AddShift(shift);
            if (Shifted != null) Shifted(shift);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            ResetOffset();
            Shifted = null;
        }
    }
}
