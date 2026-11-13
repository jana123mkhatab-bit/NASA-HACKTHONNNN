using System;
using UnityEngine;

namespace LastSignal.Ship
{
    /// <summary>
    /// Mass and acceleration of the ship. Pure math so it can be unit tested and shown on screen.
    ///
    ///     mass         = dryMass + supplies x kgPerSupplyPoint + fuel x kgPerFuelPoint
    ///     acceleration = thrust / mass                (Newton's second law, a = F / m)
    ///     fuel burned  = throttle x fuelPerSecondAtFullThrust x dt
    ///
    /// NOTE: every number here is a GAME-BALANCE value chosen for gameplay. They are not NASA data.
    /// </summary>
    public static class ShipMassModel
    {
        [Serializable]
        public class Config
        {
            [Tooltip("Ship mass with no supplies and no fuel, kg. GAME-BALANCE value.")]
            public float dryMass = 8000f;
            [Tooltip("kg per supplies point (supplies go 0 to 100). GAME-BALANCE value.")]
            public float kgPerSupplyPoint = 40f;
            [Tooltip("kg per fuel point (fuel goes 0 to 100). GAME-BALANCE value.")]
            public float kgPerFuelPoint = 60f;
        }

        public static float Mass(Config c, float supplies, float fuel)
        {
            if (c == null) c = new Config();
            float m = c.dryMass + Mathf.Max(0f, supplies) * c.kgPerSupplyPoint + Mathf.Max(0f, fuel) * c.kgPerFuelPoint;
            return Mathf.Max(1f, m);
        }

        /// <summary>a = F / m. Returns 0 for a zero or negative mass.</summary>
        public static float Acceleration(float thrustNewtons, float massKg)
        {
            if (massKg <= 0f) return 0f;
            return thrustNewtons / massKg;
        }

        /// <summary>Fuel points used in dt seconds at the given throttle (0..1, boost may push it above 1).</summary>
        public static float FuelBurn(float throttleAbs, float fuelPerSecondAtFullThrust, float dt)
        {
            return Mathf.Max(0f, throttleAbs) * fuelPerSecondAtFullThrust * Mathf.Max(0f, dt);
        }

        /// <summary>"a = F / m = 120 kN / 17.9 t = 6.7 m/s^2" for the telemetry readout.</summary>
        public static string Explain(float thrustNewtons, float massKg)
        {
            float a = Acceleration(thrustNewtons, massKg);
            return "a = F / m = " + (thrustNewtons / 1000f).ToString("0") + " kN / " + (massKg / 1000f).ToString("0.0") + " t = "
                + a.ToString("0.0") + " m/s²";
        }
    }
}
