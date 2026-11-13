using LastSignal.Ship;
using NUnit.Framework;

namespace LastSignal.Ship.Tests
{
    public class ShipMassModelTests
    {
        static ShipMassModel.Config Config()
        {
            ShipMassModel.Config c = new ShipMassModel.Config();
            c.dryMass = 8000f;
            c.kgPerSupplyPoint = 40f;
            c.kgPerFuelPoint = 60f;
            return c;
        }

        [Test]
        public void MassAddsSuppliesAndFuel()
        {
            Assert.AreEqual(18000f, ShipMassModel.Mass(Config(), 100f, 100f), 0.01f);
            Assert.AreEqual(8000f, ShipMassModel.Mass(Config(), 0f, 0f), 0.01f);
        }

        [Test]
        public void HalfTheSuppliesMakesTheShipLighter()
        {
            float full = ShipMassModel.Mass(Config(), 100f, 80f);
            float half = ShipMassModel.Mass(Config(), 50f, 80f);
            Assert.AreEqual(2000f, full - half, 0.01f);
        }

        [Test]
        public void AccelerationIsThrustOverMass()
        {
            Assert.AreEqual(120000f / 18000f, ShipMassModel.Acceleration(120000f, 18000f), 0.0001f);
            Assert.AreEqual(0f, ShipMassModel.Acceleration(120000f, 0f));
        }

        [Test]
        public void LighterShipAcceleratesHarder()
        {
            float heavy = ShipMassModel.Acceleration(120000f, ShipMassModel.Mass(Config(), 100f, 100f));
            float light = ShipMassModel.Acceleration(120000f, ShipMassModel.Mass(Config(), 50f, 100f));
            Assert.Greater(light, heavy);
        }

        [Test]
        public void FuelBurnScalesWithThrottleAndTime()
        {
            Assert.AreEqual(0.5f, ShipMassModel.FuelBurn(1f, 0.5f, 1f), 0.0001f);
            Assert.AreEqual(0.25f, ShipMassModel.FuelBurn(0.5f, 0.5f, 1f), 0.0001f);
            Assert.AreEqual(0f, ShipMassModel.FuelBurn(-1f, 0.5f, 1f));
        }
    }
}
