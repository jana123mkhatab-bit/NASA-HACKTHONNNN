using NUnit.Framework;
using UnityEngine;

namespace LastSignal.M3.Tests
{
    public class M3FxTests
    {
        [Test]
        public void LightTime_IsDistanceOverC()
        {
            // 1 sector unit = KmPerUnit km; 299,792.458 km is one light second.
            float units = 299792.458f / SectorLayout.KmPerUnit;
            Assert.AreEqual(1f, FxMath.LightTimeSeconds(units), 1e-3f);
        }

        [Test]
        public void Shadow_BehindEarth_IsFull_SunSide_IsNone()
        {
            Vector3 sun = Vector3.forward;
            Vector3 earth = new Vector3(0f, 0f, 1000f);
            Assert.AreEqual(0f, FxMath.ShadowFactor(Vector3.zero, -sun, earth, 250f), 1e-4f, "Earth is behind the ship, away from the Sun side");
            Assert.AreEqual(1f, FxMath.ShadowFactor(Vector3.zero, sun, earth, 250f), 1e-4f, "Earth sits between ship and Sun");
            Assert.AreEqual(0f, FxMath.ShadowFactor(new Vector3(400f, 0f, 0f), sun, earth, 250f), 1e-4f, "well off the shadow axis");
            float edge = FxMath.ShadowFactor(new Vector3(250f, 0f, 0f), sun, earth, 250f);
            Assert.Greater(edge, 0f); Assert.Less(edge, 1f);
        }
    }
}
