using UnityEngine;

namespace LastSignal.M4
{
    /// <summary>
    /// One camera, several behaviours with smooth blends between them:
    /// Orbit (the scene's CameraOrbit, used for the scan), Chase (lander), Follow (astronaut, right-mouse orbit),
    /// Shot (cinematic dolly with its own FOV) and an additive Perlin shake.
    /// </summary>
    public class M4CameraRig : MonoBehaviour
    {
        public enum Mode { Orbit, Chase, Follow, Shot }

        public Mode CurrentMode { get; private set; } = Mode.Orbit;
        public Camera Cam { get; private set; }

        CameraOrbit _orbit;
        Transform _target;
        Vector3 _posVel;
        float _fov = 50f, _fovVel;

        // follow / chase
        float _yaw, _pitch = 14f, _distance = 6f, _height = 1.7f;

        // shot
        Vector3 _shotFrom, _shotTo, _lookOffset, _blendStartPos;
        Quaternion _blendStartRot;
        Transform _lookAt;
        float _shotT, _shotDur, _blend;

        // shake
        float _shakeAmp, _shakeTime, _shakeDur;

        public static M4CameraRig Attach()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            var rig = cam.GetComponent<M4CameraRig>();
            if (rig == null) rig = cam.gameObject.AddComponent<M4CameraRig>();
            rig.Cam = cam;
            rig._orbit = cam.GetComponent<CameraOrbit>();
            rig._fov = cam.fieldOfView;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = Mathf.Max(cam.farClipPlane, 3000f);
            return rig;
        }

        public void Orbit()
        {
            CurrentMode = Mode.Orbit;
            if (_orbit != null) _orbit.enabled = true;
            _fov = 50f;
        }

        public void Chase(Transform target, float yawDeg)
        {
            SetMode(Mode.Chase);
            _target = target; _yaw = yawDeg; _pitch = 16f; _distance = 30f; _height = 4f; _fov = 55f;
        }

        public void Follow(Transform target, float yawDeg)
        {
            SetMode(Mode.Follow);
            _target = target; _yaw = yawDeg; _pitch = 12f; _distance = 5.5f; _height = 1.7f; _fov = 58f;
        }

        /// <summary>Cinematic dolly from -> to, looking at lookAt (+offset). Blends in from wherever the camera is.</summary>
        public void Shot(Vector3 from, Vector3 to, Transform lookAt, Vector3 lookOffset, float duration, float fov, float blendIn = 1.4f)
        {
            SetMode(Mode.Shot);
            _shotFrom = from; _shotTo = to; _lookAt = lookAt; _lookOffset = lookOffset;
            _shotT = 0f; _shotDur = Mathf.Max(0.1f, duration); _fov = fov;
            _blend = Mathf.Max(0.01f, blendIn);
            _blendStartPos = transform.position; _blendStartRot = transform.rotation;
        }

        public void Shake(float amplitude, float duration)
        {
            _shakeAmp = Mathf.Max(_shakeAmp, amplitude); _shakeDur = duration; _shakeTime = duration;
        }

        public float Yaw => _yaw;

        void SetMode(Mode m)
        {
            CurrentMode = m;
            if (_orbit != null) _orbit.enabled = false;
            _posVel = Vector3.zero;
        }

        void LateUpdate()
        {
            if (Cam == null) return;
            float dt = Time.deltaTime;
            Cam.fieldOfView = Mathf.SmoothDamp(Cam.fieldOfView, _fov, ref _fovVel, 0.6f);
            if (CurrentMode == Mode.Orbit) return;

            Vector3 desired; Vector3 look;
            float follow = 0.25f;
            switch (CurrentMode)
            {
                case Mode.Chase:
                    if (_target == null) return;
                    desired = _target.position + Quaternion.Euler(_pitch, _yaw, 0f) * new Vector3(0, 0, -_distance) + Vector3.up * _height;
                    look = _target.position + Vector3.down * 6f;
                    follow = 0.35f;
                    break;
                case Mode.Follow:
                    if (_target == null) return;
                    var d = M4Input.LookDelta();
                    _yaw += d.x * 2.2f; _pitch = Mathf.Clamp(_pitch - d.y * 1.6f, -5f, 55f);
                    desired = _target.position + Vector3.up * _height + Quaternion.Euler(_pitch, _yaw, 0f) * new Vector3(0.6f, 0f, -_distance);
                    look = _target.position + Vector3.up * 1.5f;
                    follow = 0.12f;
                    break;
                default: // Shot
                    _shotT += dt;
                    float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_shotT / _shotDur));
                    Vector3 p = Vector3.Lerp(_shotFrom, _shotTo, k);
                    look = (_lookAt != null ? _lookAt.position : _shotTo + Vector3.forward) + _lookOffset;
                    float b = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_shotT / _blend));
                    transform.position = Vector3.Lerp(_blendStartPos, p, b) + ShakeOffset(dt);
                    transform.rotation = Quaternion.Slerp(_blendStartRot, Quaternion.LookRotation(look - p), b);
                    return;
            }

            float ground = M4World.Height(desired.x, desired.z) + 0.6f;
            if (desired.y < ground) desired.y = ground;
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref _posVel, follow) + ShakeOffset(dt);
            var rot = Quaternion.LookRotation(look - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, 1f - Mathf.Exp(-8f * dt));
        }

        Vector3 ShakeOffset(float dt)
        {
            if (_shakeTime <= 0f) return Vector3.zero;
            _shakeTime -= dt;
            float a = _shakeAmp * Mathf.Clamp01(_shakeTime / Mathf.Max(0.01f, _shakeDur));
            if (_shakeTime <= 0f) _shakeAmp = 0f;
            float t = Time.time * 22f;
            return new Vector3(Mathf.PerlinNoise(t, 0.3f) - 0.5f, Mathf.PerlinNoise(0.7f, t) - 0.5f, Mathf.PerlinNoise(t, t * 0.5f) - 0.5f) * 2f * a;
        }
    }
}
