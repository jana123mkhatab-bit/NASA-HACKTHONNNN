using System;
using LastSignal.Ship;
using UnityEngine;

namespace LastSignal.M3
{
    /// <summary>
    /// The unknown craft, a scripted AI: it warns, intercepts (always faster than the player), rams,
    /// holds off while the father regains control, docks, and leaves.
    /// It moves by script (no physics) so the encounter plays out the same every time.
    /// </summary>
    public class StrangerCraft : MonoBehaviour
    {
        public enum State { Hidden, Warning, Intercept, Hold, Docking, Docked, Leaving, Gone }

        [Header("Intercept (GAME-BALANCE values)")]
        public float minInterceptSpeed = 70f;
        public float speedOverTarget = 45f;
        public float acceleration = 35f;
        public float ramDistance = 13f;

        public State Current { get; private set; }

        ShipController _target;
        Action _onEvent;
        Vector3 _holdOffset;
        Vector3 _velocity;
        float _speed;
        float _timer;
        Vector3 _leaveDir;
        Transform _blinker;

        /// <summary>Docked position above the ship's docking port.</summary>
        static readonly Vector3 DockedLocal = ShipFactory.DockingPortLocal + new Vector3(0f, 2.2f, 0f);

        public static StrangerCraft Build(Transform parent)
        {
            GameObject go = new GameObject("UnknownCraft");
            go.transform.SetParent(parent, false);
            StrangerCraft s = go.AddComponent<StrangerCraft>();
            s.BuildParts();
            s.Current = State.Hidden;
            go.SetActive(false);
            return s;
        }

        void BuildParts()
        {
            Color dark = new Color(0.16f, 0.15f, 0.17f);
            Color red = new Color(0.9f, 0.15f, 0.1f);
            Transform v = new GameObject("Visual").transform;
            v.SetParent(transform, false);
            GreyboxKit.Part(PrimitiveType.Cube, "Hull", v, Vector3.zero, new Vector3(3.5f, 1.6f, 9f), dark);
            GreyboxKit.Part(PrimitiveType.Cube, "Prow", v, new Vector3(0f, 0f, 5.2f), new Vector3(2.2f, 1.0f, 2.5f), new Vector3(0f, 45f, 0f), dark);
            GreyboxKit.Part(PrimitiveType.Cube, "WingL", v, new Vector3(-3.6f, -0.2f, -1.5f), new Vector3(4f, 0.25f, 4.5f), new Vector3(0f, 0f, 12f), dark);
            GreyboxKit.Part(PrimitiveType.Cube, "WingR", v, new Vector3(3.6f, -0.2f, -1.5f), new Vector3(4f, 0.25f, 4.5f), new Vector3(0f, 0f, -12f), dark);
            GreyboxKit.Part(PrimitiveType.Cube, "Clamp", v, new Vector3(0f, -1.1f, 0f), new Vector3(1.6f, 0.6f, 1.6f), red * 0.7f);
            GreyboxKit.Part(PrimitiveType.Sphere, "Engine", v, new Vector3(0f, 0f, -4.8f), new Vector3(1.6f, 1.2f, 0.4f), new Color(1f, 0.35f, 0.15f), true);
            _blinker = GreyboxKit.Part(PrimitiveType.Sphere, "Blinker", v, new Vector3(0f, 1.1f, 1f), Vector3.one * 0.6f, red, true).transform;
            gameObject.AddComponent<ShipNoAutoDamage>();
        }

        // ---------------------------------------------------------------- commands

        /// <summary>Appears out of nowhere at a world position, facing the target, and warns.</summary>
        public void Appear(Vector3 position, ShipController target)
        {
            _target = target;
            gameObject.SetActive(true);
            transform.position = position;
            transform.rotation = Quaternion.LookRotation(target.transform.position - position);
            GreyboxKit.Flash(position, new Color(0.9f, 0.3f, 0.2f), 25f, 0.6f);
            Current = State.Warning;
        }

        public void BeginIntercept(Action onRam)
        {
            _onEvent = onRam;
            _speed = 20f;
            Current = State.Intercept;
            _timer = 0f;
        }

        /// <summary>Stays at a world-space offset from the ship (after the ram).</summary>
        public void HoldAt(ShipController target, Vector3 worldOffset)
        {
            _target = target;
            gameObject.SetActive(true);
            _holdOffset = worldOffset;
            transform.position = target.transform.position + worldOffset;
            Current = State.Hold;
        }

        public void Dock(Action onDocked)
        {
            _onEvent = onDocked;
            _timer = 0f;
            Current = State.Docking;
        }

        public void Leave(Vector3 direction, Action onGone)
        {
            _onEvent = onGone;
            _leaveDir = direction.normalized;
            _speed = 10f;
            _timer = 0f;
            Current = State.Leaving;
        }

        public float DistanceToTarget
        {
            get { return _target != null ? Vector3.Distance(transform.position, _target.transform.position) : float.PositiveInfinity; }
        }

        // ---------------------------------------------------------------- behaviour

        void Update()
        {
            float dt = Time.deltaTime;
            _timer += dt;
            if (_blinker != null) _blinker.gameObject.SetActive(Mathf.Repeat(Time.time, 0.5f) < 0.25f);
            if (_target == null) return;
            Transform t = _target.transform;

            switch (Current)
            {
                case State.Warning:
                    FaceTowards(t.position, dt * 2f);
                    break;

                case State.Intercept:
                {
                    // Pursuit with lead. Always faster than the player, and gets faster the longer it takes.
                    float cap = Mathf.Max(minInterceptSpeed, _target.Speed + speedOverTarget) + _timer * 4f;
                    _speed = Mathf.MoveTowards(_speed, cap, acceleration * dt);
                    float dist = Vector3.Distance(transform.position, t.position);
                    float lead = Mathf.Clamp(dist / Mathf.Max(_speed, 1f), 0f, 2.5f);
                    Vector3 aim = t.position + _target.Velocity * lead;
                    Vector3 dir = (aim - transform.position).normalized;
                    _velocity = dir * _speed;
                    transform.position += _velocity * dt;
                    FaceTowards(transform.position + dir, dt * 6f);
                    if (Vector3.Distance(transform.position, t.position) <= ramDistance)
                    {
                        Current = State.Hold;
                        Action cb = _onEvent;
                        _onEvent = null;
                        if (cb != null) cb();
                    }
                    break;
                }

                case State.Hold:
                    transform.position = Vector3.Lerp(transform.position, t.position + _holdOffset, 1f - Mathf.Exp(-1.5f * dt));
                    FaceTowards(t.position, dt * 2f);
                    break;

                case State.Docking:
                {
                    Vector3 dockPos = t.TransformPoint(DockedLocal);
                    Vector3 hover = t.TransformPoint(DockedLocal + new Vector3(0f, 12f, 0f));
                    float k = Mathf.Clamp01(_timer / 4f);
                    Vector3 goal = k < 0.6f ? hover : dockPos;
                    float sharp = k < 0.6f ? 1.8f : 4f;
                    transform.position = Vector3.Lerp(transform.position, goal, 1f - Mathf.Exp(-sharp * dt));
                    transform.rotation = Quaternion.Slerp(transform.rotation, t.rotation, 1f - Mathf.Exp(-3f * dt));
                    if (k >= 1f)
                    {
                        Current = State.Docked;
                        Action cb = _onEvent;
                        _onEvent = null;
                        if (cb != null) cb();
                    }
                    break;
                }

                case State.Leaving:
                {
                    _speed += 60f * dt;
                    transform.position += _leaveDir * _speed * dt;
                    FaceTowards(transform.position + _leaveDir, dt * 3f);
                    if (_timer > 4f)
                    {
                        Current = State.Gone;
                        gameObject.SetActive(false);
                        Action cb = _onEvent;
                        _onEvent = null;
                        if (cb != null) cb();
                    }
                    break;
                }
            }
        }

        void LateUpdate()
        {
            // Stick to the ship's interpolated pose while docked.
            if (Current == State.Docked && _target != null)
            {
                transform.SetPositionAndRotation(_target.transform.TransformPoint(DockedLocal), _target.transform.rotation);
            }
        }

        void FaceTowards(Vector3 point, float k)
        {
            Vector3 d = point - transform.position;
            if (d.sqrMagnitude < 0.001f) return;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), Mathf.Clamp01(k));
        }
    }
}
