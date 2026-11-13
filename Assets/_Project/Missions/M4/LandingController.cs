using System;
using UnityEngine;

namespace LastSignal.M4
{
    /// <summary>
    /// Powered descent: gravity, main engine (Space), RCS translation (WASD, camera relative), fuel, battery power,
    /// site wind gusts and a touchdown check (vertical speed, horizontal speed, ground slope, boulders).
    /// All rates and limits are GAME-BALANCE values, not NASA data.
    /// </summary>
    public class LandingController : MonoBehaviour
    {
        public const float FuelBurnFull = 2.4f;   // % per second at full throttle (game balance)
        public const float RcsAccel = 2.4f;       // m/s^2
        public const float RcsFuel = 0.45f;       // % per second while translating
        public const float ThrustToWeight = 2.3f;
        public const float MaxGroundTiltDeg = 15f;

        public LandingSite Site { get; private set; }
        public float Gravity { get; private set; }
        public float Fuel { get; private set; }
        public float Power { get; private set; }
        public float Throttle { get; private set; }
        public Vector3 Velocity { get; private set; }
        public float Altitude { get; private set; }
        public Vector3 Wind { get; private set; }
        public float PowerDrain { get; private set; }
        public bool Active { get; private set; }
        public float DistanceToTarget => Vector2.Distance(new Vector2(transform.position.x, transform.position.z), Site.xz);

        /// <summary>Leg footprint allowance beyond the ellipse radius (game balance).</summary>
        public const float EllipseTolerance = 3f;
        public bool InsideEllipse => Site != null && DistanceToTarget <= Site.EllipseRadius + EllipseTolerance;

        public event Action<bool, string> Finished;

        Transform _visual, _socket;
        ParticleSystem _plume, _dust;
        Light _engineLight;
        float _windSeed;
        Vector2 _rcs;

        public Transform Dish { get; private set; }

        public static LandingController Create(Transform parent)
        {
            var root = new GameObject("Lander Controller");
            root.transform.SetParent(parent, false);
            var lc = root.AddComponent<LandingController>();
            lc._visual = M4World.BuildLander(out lc._socket, out var dish);
            lc.Dish = dish;
            lc._visual.SetParent(root.transform, false);
            lc._plume = M4Fx.EnginePlume(lc._socket);
            lc._dust = M4Fx.GroundDust(root.transform.parent);
            var lg = new GameObject("Engine Light");
            lg.transform.SetParent(lc._socket, false);
            lg.transform.localPosition = new Vector3(0, -1.2f, 0);
            lc._engineLight = lg.AddComponent<Light>();
            lc._engineLight.type = LightType.Point;
            lc._engineLight.color = new Color(1f, 0.62f, 0.32f);
            lc._engineLight.range = 40f;
            lc._engineLight.intensity = 0f;
            lc._engineLight.shadows = LightShadows.Soft;
            return lc;
        }

        public void Begin(LandingSite site, float gravity, float fuel, float power, Vector3 startPos, Vector3 startVel)
        {
            Site = site; Gravity = gravity; Fuel = fuel; Power = power;
            transform.position = startPos;
            transform.rotation = Quaternion.LookRotation(new Vector3(site.xz.x, 0, site.xz.y) - new Vector3(startPos.x, 0, startPos.z));
            Velocity = startVel;
            Throttle = 0f;
            _windSeed = UnityEngine.Random.value * 100f;
            Active = true;
        }

        /// <summary>Park the lander on the ground (used when resuming on the surface or for the Final).</summary>
        public void Park(Vector3 groundPos, float yaw)
        {
            Active = false;
            transform.position = groundPos;
            transform.rotation = Quaternion.Euler(0, yaw, 0) * Quaternion.FromToRotation(Vector3.up, M4World.GroundNormal(groundPos.x, groundPos.z));
            Throttle = 0f;
            M4Fx.SetRate(_plume, 0f); M4Fx.SetRate(_dust, 0f);
            if (_engineLight != null) _engineLight.intensity = 0f;
            AddBodyCollider();
        }

        void AddBodyCollider()
        {
            if (GetComponent<CapsuleCollider>() != null) return;
            var c = gameObject.AddComponent<CapsuleCollider>();
            c.center = new Vector3(0, 3f, 0); c.radius = 2.2f; c.height = 6f;
        }

        void Update()
        {
            if (!Active) { UpdateFx(0f); return; }
            float dt = Mathf.Min(Time.deltaTime, 0.05f);

            // ---- input
            float want = M4Input.ThrustHeld() && Fuel > 0f ? 1f : 0f;
            Throttle = Mathf.MoveTowards(Throttle, want, dt * 4f);
            _rcs = Fuel > 0f ? M4Input.Move() : Vector2.zero;
            var cam = Camera.main != null ? Camera.main.transform : transform;
            Vector3 fwd = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, fwd);
            Vector3 rcs = (right * _rcs.x + fwd * _rcs.y) * RcsAccel;

            // ---- wind gusts (stronger on exposed, sloped sites)
            float t = Time.time * 0.35f + _windSeed;
            float gust = Mathf.PerlinNoise(t, 1.7f) * 2f - 1f;
            float dirA = Mathf.PerlinNoise(0.3f, t * 0.2f) * Mathf.PI * 2f;
            Wind = new Vector3(Mathf.Cos(dirA), 0f, Mathf.Sin(dirA)) * gust * Site.WindStrength;

            // ---- integrate
            Vector3 accel = new Vector3(0f, -Gravity + Throttle * ThrustToWeight * Gravity, 0f) + rcs + Wind;
            Velocity += accel * dt;
            transform.position += Velocity * dt;

            Fuel = Mathf.Max(0f, Fuel - (Throttle * FuelBurnFull + (_rcs.sqrMagnitude > 0.01f ? RcsFuel : 0f)) * dt);
            PowerDrain = Site.PowerDrainPerSecond + Throttle * 0.3f;
            Power = Mathf.Max(0f, Power - PowerDrain * dt);

            // ---- cosmetic tilt toward the RCS direction
            var tilt = Quaternion.Euler(_rcs.y * 8f, 0f, -_rcs.x * 8f);
            _visual.localRotation = Quaternion.Slerp(_visual.localRotation, tilt, 1f - Mathf.Exp(-4f * dt));

            float ground = M4World.Height(transform.position.x, transform.position.z);
            Altitude = transform.position.y - ground;
            UpdateFx(Throttle);

            // ---- failure checks
            if (Power <= 2f) { Fail("Lost usable power. The batteries drained before touchdown" + (Site.solarExposure < 0.5f ? " in the crater's shadow." : ".")); return; }
            if (new Vector2(transform.position.x, transform.position.z).magnitude > M4World.PlayRadius)
            { Fail("Drifted out of the scanned area."); return; }
            if (Altitude <= 0f) Touchdown(ground);
        }

        void Touchdown(float ground)
        {
            var p = transform.position; p.y = ground; transform.position = p;
            float vs = -Velocity.y;
            float hs = new Vector2(Velocity.x, Velocity.z).magnitude;
            var n = M4World.GroundNormal(p.x, p.z);
            float slope = Vector3.Angle(n, Vector3.up);

            bool boulder = false;
            foreach (var c in Physics.OverlapSphere(p + Vector3.up * 0.6f, 2.6f))
                if (c != null && c.gameObject.name == "Boulder") { boulder = true; break; }

            if (vs > Site.SafeVerticalSpeed)
                Fail($"Touchdown at {vs:0.0} m/s. At site {Site.id} the legs are rated for {Site.SafeVerticalSpeed:0.0} m/s.");
            else if (hs > Site.SafeHorizontalSpeed)
                Fail($"Touched down sliding sideways at {hs:0.0} m/s (limit {Site.SafeHorizontalSpeed:0.0} m/s). The lander tipped.");
            else if (slope > MaxGroundTiltDeg)
                Fail($"Ground slope {slope:0} deg. The lander tipped over (limit {MaxGroundTiltDeg:0} deg).");
            else if (boulder)
                Fail("A leg came down on a boulder. Pick a clearer spot inside the ellipse.");
            else if (!InsideEllipse)
                Fail($"Touched down {DistanceToTarget:0} m from the target, outside the {Site.EllipseRadius:0} m landing ellipse. That ground was never scanned for hazards.");
            else
            {
                Active = false;
                transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0) * Quaternion.FromToRotation(Vector3.up, n);
                _visual.localRotation = Quaternion.identity;
                M4Fx.Puff(_dust, p, 80);
                AddBodyCollider();
                UpdateFx(0f);
                Finished?.Invoke(true, $"Touchdown at {vs:0.0} m/s, {DistanceToTarget:0} m from the target.");
            }
        }

        void Fail(string reason)
        {
            Active = false;
            Throttle = 0f;
            UpdateFx(0f);
            M4Fx.Puff(_dust, transform.position, 160);
            _visual.localRotation = Quaternion.Euler(0f, 0f, 24f); // tipped / crumpled pose
            Finished?.Invoke(false, reason);
        }

        void UpdateFx(float throttle)
        {
            M4Fx.SetRate(_plume, throttle * 260f);
            if (_engineLight != null) _engineLight.intensity = throttle * (7f + Mathf.PerlinNoise(Time.time * 30f, 0f) * 3f);
            if (_dust == null) return;
            float near = Active ? Mathf.Clamp01(1f - Altitude / 30f) : 0f;
            M4Fx.SetRate(_dust, throttle * near * 320f);
            var p = transform.position;
            _dust.transform.position = new Vector3(p.x, M4World.Height(p.x, p.z) + 0.2f, p.z);
        }
    }
}
