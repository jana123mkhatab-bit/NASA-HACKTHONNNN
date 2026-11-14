using LastSignal.Ship;
using NUnit.Framework;
using UnityEngine;

namespace LastSignal.Ship.Tests
{
    public class FlightModelTests
    {
        [Test]
        public void Brake_OpposesVelocity_AtMostMaxAccel()
        {
            Vector3 a = FlightModel.BrakeAcceleration(new Vector3(0f, 0f, 100f), 5f, 0.02f);
            Assert.AreEqual(5f, a.magnitude, 1e-4f);
            Assert.Less(Vector3.Dot(a, Vector3.forward), 0f);
        }

        [Test]
        public void Brake_NeverReversesTheShip()
        {
            Vector3 v = new Vector3(0.05f, 0f, 0f);
            float dt = 0.02f;
            Vector3 a = FlightModel.BrakeAcceleration(v, 50f, dt);
            Vector3 after = v + a * dt;
            Assert.AreEqual(0f, after.magnitude, 1e-5f);
        }

        [Test]
        public void Brake_StoppedShip_NoForce()
        {
            Assert.AreEqual(Vector3.zero, FlightModel.BrakeAcceleration(Vector3.zero, 10f, 0.02f));
        }

        [Test]
        public void Brake_StopsShipInExpectedTime()
        {
            // v = 100 m/s, a = 10 m/s^2 -> about 10 s of braking.
            Vector3 v = new Vector3(0f, 0f, 100f);
            float dt = 0.02f, t = 0f;
            while (v.magnitude > 0f && t < 60f)
            {
                v += FlightModel.BrakeAcceleration(v, 10f, dt) * dt;
                t += dt;
            }
            Assert.AreEqual(10f, t, 0.05f);
        }

        [Test]
        public void SoftCap_BelowCap_Unchanged_AboveCap_Bleeds_NeverBelowCap()
        {
            Vector3 slow = new Vector3(0f, 0f, 300f);
            Assert.AreEqual(slow, FlightModel.SoftCapSpeed(slow, 400f, 0.8f, 0.02f));

            Vector3 fast = new Vector3(0f, 0f, 640f);
            Vector3 v = fast;
            for (int i = 0; i < 1000; i++) v = FlightModel.SoftCapSpeed(v, 400f, 0.8f, 0.02f);
            Assert.Less(v.magnitude, 401f);
            Assert.GreaterOrEqual(v.magnitude, 400f);
            Assert.AreEqual(1f, Vector3.Dot(fast.normalized, v.normalized), 1e-5f, "direction kept");

            Assert.AreEqual(fast, FlightModel.SoftCapSpeed(fast, 0f, 0.8f, 0.02f), "cap 0 = no cap");
        }

        [Test]
        public void Bounds_ZeroInside_PullsTowardCentreOutside_NoHardWall()
        {
            Vector3 c = Vector3.zero;
            Assert.AreEqual(Vector3.zero, FlightModel.BoundsAcceleration(new Vector3(29000f, 0f, 0f), c, 30000f, 36000f, 60f));

            Vector3 a = FlightModel.BoundsAcceleration(new Vector3(33000f, 0f, 0f), c, 30000f, 36000f, 60f);
            Assert.Less(a.x, 0f);
            Assert.Less(a.magnitude, 60f, "between soft and hard radius the pull is partial");

            Vector3 far = FlightModel.BoundsAcceleration(new Vector3(0f, 50000f, 0f), c, 30000f, 36000f, 60f);
            Assert.AreEqual(60f, far.magnitude, 1e-3f, "beyond the hard radius the pull is capped, never infinite");
            Assert.Less(far.y, 0f);
        }

        [Test]
        public void BoundsFactor_IsMonotonic()
        {
            float prev = 0f;
            for (float r = 25000f; r <= 40000f; r += 500f)
            {
                float f = FlightModel.BoundsFactor(r, 30000f, 36000f);
                Assert.GreaterOrEqual(f, prev);
                Assert.That(f, Is.InRange(0f, 1f));
                prev = f;
            }
        }

        [Test]
        public void Bounds_TurnsADriftingShipBack()
        {
            // Unpowered ship drifting outward at the boost speed must come back without hitting a wall.
            Vector3 p = new Vector3(30000f, 0f, 0f);
            Vector3 v = new Vector3(640f, 0f, 0f);
            float dt = 0.02f, maxR = 0f;
            for (int i = 0; i < 6000 && v.x > 0f; i++)
            {
                v += FlightModel.BoundsAcceleration(p, Vector3.zero, 30000f, 36000f, 60f) * dt;
                p += v * dt;
                maxR = Mathf.Max(maxR, p.magnitude);
            }
            Assert.LessOrEqual(v.x, 0f, "ship turned back");
            Assert.Less(maxR, 45000f);
        }

        [Test]
        public void Recenter_OnlyBeyondThreshold()
        {
            Assert.IsFalse(FlightModel.ShouldRecenter(new Vector3(1999f, 0f, 0f), 2000f));
            Assert.IsTrue(FlightModel.ShouldRecenter(new Vector3(1500f, 1500f, 0f), 2000f));
            Assert.IsFalse(FlightModel.ShouldRecenter(new Vector3(1e6f, 0f, 0f), 0f), "threshold 0 = off");
        }

        [Test]
        public void FloatingOrigin_SectorWorldRoundTrip()
        {
            FloatingOrigin.ResetOffset();
            Vector3 sector = new Vector3(21000f, 2500f, 4000f);
            Assert.AreEqual(sector, FloatingOrigin.ToWorld(sector));
            FloatingOrigin.AddShift(new Vector3(20000f, 0f, 3000f));
            Vector3 world = FloatingOrigin.ToWorld(sector);
            Assert.AreEqual(new Vector3(1000f, 2500f, 1000f), world);
            Assert.AreEqual(sector, FloatingOrigin.ToSector(world));
            FloatingOrigin.ResetOffset();
        }

        [Test]
        public void Stranded_WhenFuelOrPowerEmpty()
        {
            Assert.IsFalse(FlightModel.IsStranded(10f, 10f));
            Assert.IsTrue(FlightModel.IsStranded(0f, 50f));
            Assert.IsTrue(FlightModel.IsStranded(50f, 0f));
        }

        [Test]
        public void PowerDrain_OnlyWhileBoosting()
        {
            Assert.AreEqual(0f, FlightModel.PowerDrain(false, 2f, 1f));
            Assert.AreEqual(2f, FlightModel.PowerDrain(true, 2f, 1f), 1e-5f);
        }

        [Test]
        public void Eta_And_SteerAngles()
        {
            Assert.AreEqual(10f, FlightModel.Eta(1000f, 100f), 1e-4f);
            Assert.IsTrue(float.IsPositiveInfinity(FlightModel.Eta(1000f, -5f)));
            Vector2 right = FlightModel.SteerAngles(new Vector3(1f, 0f, 1f));
            Assert.AreEqual(45f, right.x, 1e-3f);
            Assert.AreEqual(0f, right.y, 1e-3f);
            Vector2 up = FlightModel.SteerAngles(new Vector3(0f, 1f, 1f));
            Assert.AreEqual(45f, up.y, 1e-3f);
        }

        [Test]
        public void ShipMassModel_Unchanged_ByFreeFlight()
        {
            // Free flight must not change the mass model: a = F / m with the default config.
            ShipMassModel.Config c = new ShipMassModel.Config();
            Assert.AreEqual(18000f, ShipMassModel.Mass(c, 100f, 100f), 0.01f);
            Assert.AreEqual(0.5f * 0.02f, ShipMassModel.FuelBurn(1f, 0.5f, 0.02f), 1e-6f);
        }
    }
}
