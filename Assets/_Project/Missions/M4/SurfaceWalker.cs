using System.Collections.Generic;
using UnityEngine;

namespace LastSignal.M4
{
    /// <summary>
    /// Astronaut on foot. Player mode: WASD camera-relative, Shift to run, F headlamp.
    /// Follow mode (no input): walks beside another walker (the son in the Final). Procedural walk cycle.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class SurfaceWalker : MonoBehaviour
    {
        public float walkSpeed = 3.0f, runSpeed = 5.2f, gravity = 3.7f;
        public bool inputEnabled = true;
        public Transform followTarget;
        public Vector3 followOffset = new Vector3(1.6f, 0f, -0.4f);

        public M4Interactable Nearby { get; private set; }
        public float Speed01 { get; private set; }

        CharacterController _cc;
        Transform _legL, _legR, _armL, _armR, _visual;
        Light _lamp;
        float _vy, _cycle;
        ParticleSystem _puffs;
        static readonly List<M4Interactable> Interactables = new List<M4Interactable>();

        public static void Register(M4Interactable i) { if (i != null && !Interactables.Contains(i)) Interactables.Add(i); }
        public static void ClearInteractables() => Interactables.Clear();

        public static SurfaceWalker Spawn(string name, bool son, Vector3 pos, float yaw, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0, yaw, 0);
            var cc = go.AddComponent<CharacterController>();
            cc.height = 1.85f; cc.radius = 0.35f; cc.center = new Vector3(0, 0.95f, 0); cc.stepOffset = 0.45f; cc.slopeLimit = 50f;
            var w = go.AddComponent<SurfaceWalker>();
            w._cc = cc;
            w._visual = M4World.BuildAstronaut(son ? "Son (visual)" : "Father (visual)", son);
            w._visual.SetParent(go.transform, false);
            w._legL = w._visual.Find("Leg L"); w._legR = w._visual.Find("Leg R");
            w._armL = w._visual.Find("Arm L"); w._armR = w._visual.Find("Arm R");

            var lampGo = new GameObject("Headlamp");
            lampGo.transform.SetParent(w._visual, false);
            lampGo.transform.localPosition = new Vector3(0.18f, 1.72f, 0.2f);
            lampGo.transform.localRotation = Quaternion.Euler(14f, 0, 0);
            w._lamp = lampGo.AddComponent<Light>();
            w._lamp.type = LightType.Spot; w._lamp.spotAngle = 55f; w._lamp.innerSpotAngle = 25f;
            w._lamp.range = 22f; w._lamp.intensity = son ? 3f : 5f;
            w._lamp.color = son ? new Color(1f, 0.8f, 0.6f) : new Color(0.9f, 0.95f, 1f);
            w._lamp.shadows = son ? LightShadows.None : LightShadows.Soft;

            w._puffs = M4Fx.GroundDust(go.transform);
            var main = w._puffs.main;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.45f);
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
            var sh = w._puffs.shape; sh.radius = 0.2f;
            return w;
        }

        void Awake() { _cc = GetComponent<CharacterController>(); }

        void Update()
        {
            float dt = Time.deltaTime;
            Vector3 move = Vector3.zero;
            bool run = false;

            if (followTarget != null)
            {
                Vector3 goal = followTarget.position + followTarget.rotation * followOffset;
                Vector3 to = goal - transform.position; to.y = 0f;
                if (to.magnitude > 0.6f) move = to.normalized * Mathf.Clamp01(to.magnitude / 2f);
                run = to.magnitude > 5f;
            }
            else if (inputEnabled)
            {
                var m = M4Input.Move();
                var cam = Camera.main != null ? Camera.main.transform : transform;
                Vector3 fwd = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
                Vector3 right = Vector3.Cross(Vector3.up, fwd);
                move = right * m.x + fwd * m.y;
                run = M4Input.RunHeld();
                if (M4Input.LampDown() && _lamp != null) _lamp.enabled = !_lamp.enabled;
            }

            float speed = run ? runSpeed : walkSpeed;
            Vector3 horiz = move * speed;
            if (_cc.isGrounded && _vy < 0f) _vy = -1f;
            _vy -= gravity * dt;
            if (_cc.enabled) _cc.Move((horiz + Vector3.up * _vy) * dt);

            if (move.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(move), 1f - Mathf.Exp(-10f * dt));

            Speed01 = Mathf.MoveTowards(Speed01, move.magnitude * (run ? 1f : 0.6f), dt * 4f);
            Animate(dt);
            FindNearby();
        }

        void Animate(float dt)
        {
            float prev = _cycle;
            _cycle += dt * Mathf.Lerp(0f, 9f, Speed01);
            float s = Mathf.Sin(_cycle) * 32f * Speed01;
            if (_legL != null) _legL.localRotation = Quaternion.Euler(s, 0, 0);
            if (_legR != null) _legR.localRotation = Quaternion.Euler(-s, 0, 0);
            if (_armL != null) _armL.localRotation = Quaternion.Euler(-s * 0.7f, 0, 8f);
            if (_armR != null) _armR.localRotation = Quaternion.Euler(s * 0.7f, 0, -8f);
            if (_visual != null) _visual.localPosition = new Vector3(0, Mathf.Abs(Mathf.Sin(_cycle)) * 0.06f * Speed01, 0);
            // a dust puff every footfall
            if (Speed01 > 0.2f && Mathf.Floor(prev / Mathf.PI) != Mathf.Floor(_cycle / Mathf.PI))
                M4Fx.Puff(_puffs, transform.position + transform.forward * 0.2f, 6);
        }

        void FindNearby()
        {
            Nearby = null;
            if (followTarget != null) return;
            float best = float.MaxValue;
            for (int i = Interactables.Count - 1; i >= 0; i--)
            {
                var it = Interactables[i];
                if (it == null) { Interactables.RemoveAt(i); continue; }
                if (!it.available || !it.isActiveAndEnabled) continue;
                float d = Vector3.Distance(transform.position, it.transform.position);
                if (d < it.radius && d < best) { best = d; Nearby = it; }
            }
        }

        public void SetLamp(bool on) { if (_lamp != null) _lamp.enabled = on; }

        /// <summary>Teleport safely (the CharacterController overrides transform moves while enabled).</summary>
        public void Teleport(Vector3 pos, float yaw)
        {
            _cc.enabled = false;
            transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
            _cc.enabled = true;
            _vy = 0f;
        }
    }
}
