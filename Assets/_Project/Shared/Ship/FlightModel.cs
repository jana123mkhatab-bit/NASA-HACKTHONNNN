using UnityEngine;

namespace LastSignal.Ship
{
    /// <summary>
    /// Pure free-flight math (brake, speed cap, soft sector bounds, floating-origin trigger, stranded check),
    /// kept out of MonoBehaviours so it can be unit tested. Does not touch ShipMassModel.
    ///
    /// NOTE: every rate and limit passed in is a GAME-BALANCE value, not NASA data.
    /// </summary>
    public static class FlightModel
    {
        /// <summary>
        /// Acceleration that slows the ship down along its velocity, at most maxAccel,
        /// and never more than needed to reach zero in dt (so braking never reverses the ship).
        /// </summary>
        public static Vector3 BrakeAcceleration(Vector3 velocity, float maxAccel, float dt)
        {
            float speed = velocity.magnitude;
            if (speed < 1e-4f || maxAccel <= 0f || dt <= 0f) return Vector3.zero;
            float a = Mathf.Min(maxAccel, speed / dt);
            return -velocity / speed * a;
        }

        /// <summary>
        /// Soft speed limit: speed above the cap bleeds off exponentially (rate per second), so releasing boost
        /// slows the ship smoothly instead of snapping. Below the cap the velocity is unchanged. cap &lt;= 0 means no cap.
        /// </summary>
        public static Vector3 SoftCapSpeed(Vector3 velocity, float cap, float rate, float dt)
        {
            if (cap <= 0f) return velocity;
            float speed = velocity.magnitude;
            if (speed <= cap) return velocity;
            float k = Mathf.Clamp01(rate * Mathf.Max(0f, dt));
            float next = speed - (speed - cap) * k;
            return velocity / speed * next;
        }

        /// <summary>0 inside the soft radius, rising smoothly to 1 at the hard radius and staying 1 beyond.</summary>
        public static float BoundsFactor(float distanceFromCenter, float softRadius, float hardRadius)
        {
            if (distanceFromCenter <= softRadius) return 0f;
            float t = Mathf.Clamp01((distanceFromCenter - softRadius) / Mathf.Max(1f, hardRadius - softRadius));
            return t * t * (3f - 2f * t);
        }

        /// <summary>Acceleration pulling a ship back toward the sector centre once it passes the soft radius (no hard wall).</summary>
        public static Vector3 BoundsAcceleration(Vector3 position, Vector3 center, float softRadius, float hardRadius, float strength)
        {
            Vector3 d = position - center;
            float r = d.magnitude;
            if (r < 1e-3f) return Vector3.zero;
            float f = BoundsFactor(r, softRadius, hardRadius);
            if (f <= 0f) return Vector3.zero;
            return -d / r * strength * f;
        }

        /// <summary>True when the focus is far enough from the world origin that the floating origin should recentre.</summary>
        public static bool ShouldRecenter(Vector3 focusPosition, float threshold)
        {
            return threshold > 0f && focusPosition.sqrMagnitude > threshold * threshold;
        }

        /// <summary>The ship cannot thrust or run its systems: it drifts and must be rescued or the mission retried.</summary>
        public static bool IsStranded(float fuel, float power)
        {
            return fuel <= 0f || power <= 0f;
        }

        /// <summary>Power points used in dt seconds while boosting.</summary>
        public static float PowerDrain(bool boosting, float perSecond, float dt)
        {
            return boosting ? Mathf.Max(0f, perSecond) * Mathf.Max(0f, dt) : 0f;
        }

        /// <summary>Seconds to cover a distance at the closing speed; infinity if not closing.</summary>
        public static float Eta(float distance, float closingSpeed)
        {
            if (closingSpeed <= 0.5f) return float.PositiveInfinity;
            return Mathf.Max(0f, distance) / closingSpeed;
        }

        /// <summary>Yaw (+ right) and pitch (+ up) in degrees to point the nose at a target, given the direction in ship axes.</summary>
        public static Vector2 SteerAngles(Vector3 localDirection)
        {
            float yaw = Mathf.Atan2(localDirection.x, localDirection.z) * Mathf.Rad2Deg;
            float flat = Mathf.Sqrt(localDirection.x * localDirection.x + localDirection.z * localDirection.z);
            float pitch = Mathf.Atan2(localDirection.y, flat) * Mathf.Rad2Deg;
            return new Vector2(yaw, pitch);
        }
    }
}
