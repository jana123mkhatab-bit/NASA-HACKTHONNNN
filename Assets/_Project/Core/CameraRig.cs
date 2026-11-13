using System;
using System.Collections.Generic;
using UnityEngine;

namespace LastSignal
{
    /// <summary>
    /// Staged camera for the mission scenes. It holds a list of named shots (position, look-at, field of
    /// view), glides between them, breathes a little so the frame is never frozen, can follow a moving
    /// object, and can shake. Missions call GoTo("bench"), Follow(sled) and Shake(...).
    /// </summary>
    public class CameraRig : MonoBehaviour
    {
        [Serializable]
        public class Shot
        {
            public string name;
            public Vector3 position;
            public Vector3 lookAt;
            public float fov = 40f;
            [Tooltip("Degrees per second the shot orbits around its look-at point (establishing shots).")]
            public float orbit;
        }

        public List<Shot> shots = new List<Shot>();
        public string startShot;
        public float sway = 0.06f;

        Camera _cam;
        Vector3 _pos, _look, _fromPos, _fromLook;
        float _fov, _fromFov, _t = 1f, _duration = 1f, _orbitAngle;
        Shot _shot;
        Transform _follow;
        Vector3 _followOrigin;
        float _shake, _shakeTime, _shakeDuration;

        public string Current => _shot != null ? _shot.name : "";

        void Awake()
        {
            _cam = GetComponent<Camera>();
            Snap(string.IsNullOrEmpty(startShot) && shots.Count > 0 ? shots[0].name : startShot);
        }

        public Shot Find(string shotName) => shots.Find(s => s.name == shotName);

        public void Snap(string shotName)
        {
            var s = Find(shotName);
            if (s == null) return;
            _shot = s; _t = 1f; _orbitAngle = 0f;
            _pos = s.position; _look = s.lookAt; _fov = s.fov;
            Apply();
        }

        public void GoTo(string shotName, float seconds = 1.4f)
        {
            var s = Find(shotName);
            if (s == null || s == _shot) return;
            _fromPos = _pos; _fromLook = _look; _fromFov = _fov;
            _shot = s; _t = 0f; _duration = Mathf.Max(0.01f, seconds); _orbitAngle = 0f;
        }

        /// <summary>Carry the current shot along with a moving object (null to stop).</summary>
        public void Follow(Transform target)
        {
            _follow = target;
            if (target != null) _followOrigin = target.position;
        }

        public void Shake(float amplitude, float seconds)
        {
            _shake = amplitude; _shakeDuration = Mathf.Max(0.01f, seconds); _shakeTime = 0f;
        }

        void LateUpdate()
        {
            if (_shot == null) return;
            Vector3 targetPos = _shot.position, targetLook = _shot.lookAt;
            if (_shot.orbit != 0f)
            {
                _orbitAngle += _shot.orbit * Time.deltaTime;
                targetPos = targetLook + Quaternion.Euler(0f, _orbitAngle, 0f) * (_shot.position - _shot.lookAt);
            }
            if (_t < 1f)
            {
                _t = Mathf.Min(1f, _t + Time.deltaTime / _duration);
                float k = _t * _t * (3f - 2f * _t);
                _pos = Vector3.Lerp(_fromPos, targetPos, k);
                _look = Vector3.Lerp(_fromLook, targetLook, k);
                _fov = Mathf.Lerp(_fromFov, _shot.fov, k);
            }
            else { _pos = targetPos; _look = targetLook; _fov = _shot.fov; }
            Apply();
        }

        void Apply()
        {
            float t = Time.time;
            Vector3 offset = _follow != null ? _follow.position - _followOrigin : Vector3.zero;
            Vector3 drift = new Vector3(
                (Mathf.PerlinNoise(t * 0.11f, 0.3f) - 0.5f),
                (Mathf.PerlinNoise(0.7f, t * 0.13f) - 0.5f) * 0.6f,
                (Mathf.PerlinNoise(t * 0.09f, 5.1f) - 0.5f)) * (sway * 2f);
            Vector3 shake = Vector3.zero;
            if (_shakeTime < _shakeDuration)
            {
                _shakeTime += Time.deltaTime;
                float fall = 1f - Mathf.Clamp01(_shakeTime / _shakeDuration);
                shake = new Vector3(Mathf.PerlinNoise(t * 31f, 0f) - 0.5f, Mathf.PerlinNoise(0f, t * 37f) - 0.5f, 0f) * (_shake * fall * 2f);
            }
            transform.position = _pos + offset + drift + shake;
            transform.LookAt(_look + offset + shake * 0.5f);
            if (_cam != null) _cam.fieldOfView = _fov;
        }
    }
}
