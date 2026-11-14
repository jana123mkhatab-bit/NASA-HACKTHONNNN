using System;
using System.Collections.Generic;
using UnityEngine;

namespace LastSignal.Ship
{
    public enum ShipMode { Space, Atmosphere, Landed }

    /// <summary>What happened when the ship touched the ground (M4 landing hook).</summary>
    public struct TouchdownInfo
    {
        public float impactSpeed;
        /// <summary>1 = perfectly upright, 0 = on its side, -1 = upside down.</summary>
        public float uprightDot;
        public bool safe;
        public Vector3 point;
        public Collider ground;
    }

    /// <summary>
    /// The one ship used in M3 and M4. Rigidbody controller where acceleration = thrust / current mass,
    /// and the mass follows PlayerState (supplies and fuel). Fuel and supplies drain through PlayerState.
    /// See SHIP_INTERFACE.md for the agreed API.
    ///
    /// NOTE: thrust, turn rates, consumption and damage numbers are GAME-BALANCE values, not NASA data.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [DisallowMultipleComponent]
    public class ShipController : MonoBehaviour
    {
        /// <summary>The most recently enabled ship.</summary>
        public static ShipController Active { get; private set; }

        [Header("Engines (GAME-BALANCE values)")]
        [Tooltip("Main engine thrust along the nose, newtons.")]
        public float maxThrust = 120000f;
        [Tooltip("Reverse thrust as a fraction of main thrust.")]
        [Range(0f, 1f)] public float reverseFraction = 0.5f;
        [Tooltip("Lift (landing) thrusters along the ship's up axis, newtons.")]
        public float liftThrust = 90000f;
        public float boostMultiplier = 1.6f;
        [Tooltip("Angular acceleration from the RCS, rad/s^2.")]
        public float turnAcceleration = 1.4f;
        public float maxAngularSpeed = 6f;

        [Header("Mass model (GAME-BALANCE values)")]
        public ShipMassModel.Config massConfig = new ShipMassModel.Config();

        [Header("Consumption (GAME-BALANCE values)")]
        [Tooltip("Fuel points per second at full main thrust.")]
        public float fuelPerSecondAtFullThrust = 0.5f;
        [Tooltip("Fuel points per second while the stabilizer fires.")]
        public float stabilizerFuelPerSecond = 0.3f;
        [Tooltip("Supplies points per second eaten by the crew.")]
        public float suppliesPerSecond = 0.08f;

        [Header("Free flight (GAME-BALANCE values, 0 = off; M3 sets these)")]
        [Tooltip("Power points per second drained while boosting. 0 = boost is free (M4).")]
        public float powerPerSecondBoost = 0f;
        [Tooltip("Cruise speed cap, m/s (boost raises it by boostMultiplier). 0 = no cap (M4).")]
        public float maxSpeed = 0f;
        [Tooltip("How fast speed above the cap bleeds off, per second.")]
        public float overSpeedBleed = 0.8f;

        [Header("Flight assist")]
        [Tooltip("Linear damping applied when assist is on and there is no thrust input (arcade feel).")]
        public float assistLinearDamping = 0.15f;
        public float assistAngularDamping = 2.5f;
        [Tooltip("Angular damping while the stabilizer key is held.")]
        public float stabilizerAngularDamping = 6f;

        [Header("Atmosphere and landing (M4)")]
        public float planetGravity = 3.7f;
        public float atmosphereLinearDamping = 0.4f;
        public float maxSafeTouchdownSpeed = 3f;
        [Range(-1f, 1f)] public float minUprightDot = 0.8f;
        public float landedSettleTime = 0.6f;
        public LayerMask groundMask = ~0;
        public float groundProbeDistance = 500f;

        [Header("Collision damage (GAME-BALANCE values)")]
        public bool applyCollisionDamage = true;
        public float harmlessImpactSpeed = 4f;
        public float hullDamagePerMps = 2.5f;

        [Header("Visual hooks (optional)")]
        public Transform[] engineGlows;
        public Light engineLight;
        public GameObject gearRoot;

        // ---------------------------------------------------------------- runtime switches
        [NonSerialized] public bool ControlEnabled = true;
        [NonSerialized] public bool FlightAssist = true;
        [NonSerialized] public bool MainEngineOnline = true;
        [NonSerialized] public bool StabilizerOnline = true;
        [NonSerialized] public float RcsAuthority = 1f;
        [NonSerialized] public float ThrustMultiplier = 1f;
        [NonSerialized] public bool DrainSupplies = true;
        [NonSerialized] public float SuppliesDrainMultiplier = 1f;
        /// <summary>Autopilot: when set, its frame is used instead of the player's input.</summary>
        [NonSerialized] public Func<ShipInputFrame> InputOverride;

        // ---------------------------------------------------------------- state (read only)
        public ShipMode Mode { get; private set; }
        public Rigidbody Body { get; private set; }
        public float CurrentMass { get; private set; }
        public float CurrentThrust { get; private set; }
        public float LastAcceleration { get; private set; }
        public ShipInputFrame LastInput { get; private set; }
        public bool IsGrounded { get; private set; }
        public bool IsHeld { get; private set; }
        public bool GearDown { get; private set; }
        public float AltitudeAboveGround { get; private set; }
        public Vector3 Velocity { get { return Body != null ? Body.linearVelocity : Vector3.zero; } }
        public float Speed { get { return Velocity.magnitude; } }
        public float VerticalSpeed { get { return Velocity.y; } }
        /// <summary>Spin in ship axes: x = pitch rate (+ nose up), y = yaw rate (+ right), z = roll rate (+ right), rad/s.</summary>
        public Vector3 LocalSpin
        {
            get
            {
                if (Body == null) return Vector3.zero;
                Vector3 w = transform.InverseTransformDirection(Body.angularVelocity);
                return new Vector3(-w.x, w.y, -w.z);
            }
        }

        // ---------------------------------------------------------------- events
        public event Action<TouchdownInfo> Touchdown;
        public event Action Landed;
        public event Action Liftoff;
        public event Action FuelEmpty;
        public event Action HullDepleted;
        public event Action<Collision> Collided;
        public event Action<bool> GearChanged;

        float _stillTimer;
        bool _fuelEmptyRaised;
        bool _hullRaised;
        readonly HashSet<Collider> _groundContacts = new HashSet<Collider>();

        void Awake()
        {
            Body = GetComponent<Rigidbody>();
            Body.useGravity = false;
            Body.interpolation = RigidbodyInterpolation.Interpolate;
            Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            Body.maxAngularVelocity = maxAngularSpeed;
            Mode = ShipMode.Space;
            UpdateMass();
            if (gearRoot != null) GearDown = gearRoot.activeSelf;
        }

        void OnEnable() { Active = this; }

        void OnDisable()
        {
            if (Active == this) Active = null;
        }

        // ---------------------------------------------------------------- public API

        public void SetMode(ShipMode mode)
        {
            if (Mode == mode) return;
            ShipMode old = Mode;
            Mode = mode;
            if (old == ShipMode.Landed && mode == ShipMode.Atmosphere && Liftoff != null) Liftoff();
        }

        public void SetGear(bool down)
        {
            GearDown = down;
            if (gearRoot != null) gearRoot.SetActive(down);
            if (GearChanged != null) GearChanged(down);
        }

        /// <summary>Freezes the ship in place (docking clamp, launch pad, cutscene). Velocity is cleared.</summary>
        public void Hold(bool hold)
        {
            IsHeld = hold;
            if (hold)
            {
                if (!Body.isKinematic)
                {
                    Body.linearVelocity = Vector3.zero;
                    Body.angularVelocity = Vector3.zero;
                }
                Body.isKinematic = true;
            }
            else
            {
                Body.isKinematic = false;
            }
        }

        public void Teleport(Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angularVelocity)
        {
            bool wasKinematic = Body.isKinematic;
            Body.isKinematic = true;
            transform.SetPositionAndRotation(position, rotation);
            Body.position = position;
            Body.rotation = rotation;
            Body.isKinematic = wasKinematic;
            if (!Body.isKinematic)
            {
                Body.linearVelocity = velocity;
                Body.angularVelocity = angularVelocity;
            }
        }

        /// <summary>A scripted hit (M3 ram). Changes velocity and spin directly and removes hull.</summary>
        public void ApplyImpact(Vector3 velocityChange, Vector3 angularVelocityChange, float hullDamage)
        {
            if (Body.isKinematic) Hold(false);
            Body.AddForce(velocityChange, ForceMode.VelocityChange);
            Body.AddTorque(angularVelocityChange, ForceMode.VelocityChange);
            if (hullDamage > 0f) DamageHull(hullDamage);
        }

        public void DamageHull(float amount)
        {
            PlayerState s = PlayerState.Current;
            s.SetHull(s.hull - amount);
            if (s.hull <= 0f && !_hullRaised)
            {
                _hullRaised = true;
                if (HullDepleted != null) HullDepleted();
            }
        }

        // ---------------------------------------------------------------- simulation

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            UpdateMass();
            ProbeGround();

            if (IsHeld)
            {
                CurrentThrust = 0f;
                LastAcceleration = 0f;
                LastInput = new ShipInputFrame();
                UpdateVisuals(0f);
                return;
            }

            ShipInputFrame input = ReadInput();
            LastInput = input;
            PlayerState state = PlayerState.Current;
            bool hasFuel = state.fuel > 0f;

            // ---- main engine and lift thrusters: a = F / m
            bool boosting = input.boost && (powerPerSecondBoost <= 0f || state.power > 0f);
            float boost = boosting ? boostMultiplier : 1f;
            float main = 0f;
            if (MainEngineOnline && hasFuel)
            {
                main = input.thrust >= 0f ? input.thrust : input.thrust * reverseFraction;
                main *= maxThrust * boost * ThrustMultiplier;
            }
            float lift = hasFuel ? input.lift * liftThrust * boost * ThrustMultiplier : 0f;

            Vector3 thrustForce = transform.forward * main + transform.up * lift;
            CurrentThrust = thrustForce.magnitude;
            Vector3 acceleration = thrustForce / CurrentMass;           // a = F / m
            LastAcceleration = acceleration.magnitude;
            Body.AddForce(acceleration, ForceMode.Acceleration);

            // ---- brake: the engines fire against the velocity (reverse thrust limit), never past a full stop
            float brakeThrottle = 0f;
            if (input.brake && MainEngineOnline && hasFuel && !Body.isKinematic)
            {
                float maxBrake = maxThrust * reverseFraction * ThrustMultiplier / CurrentMass;   // a = F / m
                Vector3 brakeAcc = FlightModel.BrakeAcceleration(Body.linearVelocity, maxBrake, dt);
                Body.AddForce(brakeAcc, ForceMode.Acceleration);
                brakeThrottle = brakeAcc.magnitude * CurrentMass / maxThrust;
                LastAcceleration = Mathf.Max(LastAcceleration, brakeAcc.magnitude);
            }

            // ---- soft speed cap (free flight only)
            if (maxSpeed > 0f && !Body.isKinematic)
            {
                Body.linearVelocity = FlightModel.SoftCapSpeed(Body.linearVelocity, maxSpeed * boost, overSpeedBleed, dt);
            }

            // ---- gravity (atmosphere / landed only; deep space has none)
            if (Mode != ShipMode.Space)
            {
                Body.AddForce(Vector3.down * planetGravity, ForceMode.Acceleration);
            }

            // ---- rotation from the RCS
            Vector3 torque = new Vector3(-input.pitch, input.yaw, -input.roll) * turnAcceleration * RcsAuthority;
            Body.AddRelativeTorque(torque, ForceMode.Acceleration);

            // ---- damping: flight assist and stabilizer
            bool stabilizing = input.stabilize && StabilizerOnline && hasFuel;
            float linDamp = 0f;
            if (Mode != ShipMode.Space) linDamp = atmosphereLinearDamping;
            else if (FlightAssist && Mathf.Abs(input.thrust) < 0.01f && Mathf.Abs(input.lift) < 0.01f) linDamp = assistLinearDamping;
            Body.linearDamping = linDamp;

            float angDamp = 0f;
            if (stabilizing) angDamp = stabilizerAngularDamping;
            else if (FlightAssist && StabilizerOnline) angDamp = assistAngularDamping;
            Body.angularDamping = angDamp;

            // ---- consumption through PlayerState
            float burn = ShipMassModel.FuelBurn(Mathf.Abs(main) / maxThrust + Mathf.Abs(lift) / maxThrust + brakeThrottle, fuelPerSecondAtFullThrust, dt);
            if (boosting && powerPerSecondBoost > 0f && (Mathf.Abs(main) > 0f || Mathf.Abs(lift) > 0f))
            {
                state.SetPower(state.power - FlightModel.PowerDrain(true, powerPerSecondBoost, dt));
            }
            if (stabilizing) burn += stabilizerFuelPerSecond * dt;
            if (burn > 0f && hasFuel)
            {
                state.SetFuel(state.fuel - burn);
                if (state.fuel <= 0f && !_fuelEmptyRaised)
                {
                    _fuelEmptyRaised = true;
                    if (FuelEmpty != null) FuelEmpty();
                }
            }
            if (DrainSupplies && suppliesPerSecond > 0f && state.supplies > 0f)
            {
                state.SetSupplies(state.supplies - suppliesPerSecond * SuppliesDrainMultiplier * dt);
            }

            UpdateLanded(dt);
            UpdateVisuals(Mathf.Clamp01(Mathf.Abs(main) / maxThrust + Mathf.Abs(lift) / liftThrust * 0.5f));
        }

        ShipInputFrame ReadInput()
        {
            if (InputOverride != null) return InputOverride();
            if (!ControlEnabled) return new ShipInputFrame();
            return ShipInput.Read();
        }

        void UpdateMass()
        {
            PlayerState s = PlayerState.Current;
            CurrentMass = ShipMassModel.Mass(massConfig, s.supplies, s.fuel);
            if (Body != null) Body.mass = CurrentMass;   // so collisions also feel the mass
        }

        void ProbeGround()
        {
            RaycastHit hit;
            Vector3 origin = transform.position;
            if (Physics.Raycast(origin, Vector3.down, out hit, groundProbeDistance, groundMask, QueryTriggerInteraction.Ignore)
                && hit.rigidbody != Body)
            {
                AltitudeAboveGround = hit.distance;
            }
            else
            {
                AltitudeAboveGround = float.PositiveInfinity;
            }
        }

        void UpdateLanded(float dt)
        {
            _groundContacts.RemoveWhere(c => c == null || !c.enabled);
            IsGrounded = _groundContacts.Count > 0;
            if (Mode == ShipMode.Space) return;

            if (Mode == ShipMode.Landed)
            {
                if (LastInput.lift > 0.1f) SetMode(ShipMode.Atmosphere);
                return;
            }
            if (IsGrounded && Speed < 0.5f)
            {
                _stillTimer += dt;
                if (_stillTimer >= landedSettleTime)
                {
                    SetMode(ShipMode.Landed);
                    if (Landed != null) Landed();
                }
            }
            else
            {
                _stillTimer = 0f;
            }
        }

        void UpdateVisuals(float throttle01)
        {
            if (engineGlows != null)
            {
                for (int i = 0; i < engineGlows.Length; i++)
                {
                    if (engineGlows[i] == null) continue;
                    float len = 0.2f + throttle01 * 1.6f;
                    Vector3 s = engineGlows[i].localScale;
                    engineGlows[i].localScale = new Vector3(s.x, s.y, len);
                }
            }
            if (engineLight != null) engineLight.intensity = throttle01 * 6f;
        }

        // ---------------------------------------------------------------- collisions

        void OnCollisionEnter(Collision collision)
        {
            if (Collided != null) Collided(collision);

            float speed = collision.relativeVelocity.magnitude;
            bool fromBelow = false;
            Vector3 point = transform.position;
            if (collision.contactCount > 0)
            {
                ContactPoint c = collision.GetContact(0);
                point = c.point;
                fromBelow = c.normal.y > 0.6f;
            }

            if (fromBelow && Mode != ShipMode.Space)
            {
                _groundContacts.Add(collision.collider);
                TouchdownInfo info = new TouchdownInfo();
                info.impactSpeed = speed;
                info.uprightDot = Vector3.Dot(transform.up, Vector3.up);
                info.safe = speed <= maxSafeTouchdownSpeed && info.uprightDot >= minUprightDot;
                info.point = point;
                info.ground = collision.collider;
                if (Touchdown != null) Touchdown(info);
                if (info.safe) return;
            }

            if (applyCollisionDamage && speed > harmlessImpactSpeed && collision.collider.GetComponentInParent<ShipNoAutoDamage>() == null)
            {
                DamageHull((speed - harmlessImpactSpeed) * hullDamagePerMps);
            }
        }

        void OnCollisionExit(Collision collision)
        {
            _groundContacts.Remove(collision.collider);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Active = null;
        }
    }
}
