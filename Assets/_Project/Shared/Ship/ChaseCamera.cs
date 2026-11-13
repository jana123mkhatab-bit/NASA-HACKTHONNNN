using UnityEngine;

namespace LastSignal.Ship
{
    /// <summary>
    /// Third-person chase camera for the ship. SetStable(true) stops it from turning with the ship,
    /// so a tumbling ship is visible instead of the whole world spinning.
    /// </summary>
    public class ChaseCamera : MonoBehaviour
    {
        public static ChaseCamera Instance { get; private set; }

        public Transform target;
        public Vector3 offset = new Vector3(0f, 4.5f, -20f);
        public float lookAhead = 30f;
        public float positionSharpness = 8f;
        public float rotationSharpness = 6f;

        bool _stable;
        Quaternion _stableBasis = Quaternion.identity;
        float _shake;
        float _shakeTime;

        public static ChaseCamera Attach(Camera cam, Transform target)
        {
            if (cam == null) return null;
            ChaseCamera cc = cam.GetComponent<ChaseCamera>();
            if (cc == null) cc = cam.gameObject.AddComponent<ChaseCamera>();
            cc.target = target;
            cc.SnapNow();
            return cc;
        }

        void OnEnable() { Instance = this; }

        void OnDisable()
        {
            if (Instance == this) Instance = null;
        }

        public void SetStable(bool stable)
        {
            if (stable && !_stable && target != null)
            {
                // Keep the current view direction but level it, so the camera does not inherit any roll.
                Vector3 fwd = transform.forward;
                _stableBasis = Quaternion.LookRotation(fwd, Vector3.up);
            }
            _stable = stable;
        }

        public void Shake(float amount, float seconds)
        {
            if (amount >= _shake || _shakeTime <= 0f)
            {
                _shake = amount;
                _shakeTime = seconds;
            }
        }

        public void SnapNow()
        {
            if (target == null) return;
            Quaternion basis = Basis();
            transform.position = target.position + basis * offset;
            transform.rotation = Quaternion.LookRotation(target.position + basis * Vector3.forward * lookAhead - transform.position, basis * Vector3.up);
        }

        Quaternion Basis()
        {
            return _stable ? _stableBasis : target.rotation;
        }

        void LateUpdate()
        {
            if (target == null) return;
            float dt = Time.deltaTime;
            Quaternion basis = Basis();

            Vector3 desired = target.position + basis * offset;
            transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-positionSharpness * dt));

            Vector3 lookPoint = target.position + basis * Vector3.forward * lookAhead;
            Quaternion look = Quaternion.LookRotation(lookPoint - transform.position, basis * Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, 1f - Mathf.Exp(-rotationSharpness * dt));

            if (_shakeTime > 0f)
            {
                _shakeTime -= dt;
                transform.position += Random.insideUnitSphere * _shake;
                if (_shakeTime <= 0f) _shake = 0f;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Instance = null;
        }
    }
}
