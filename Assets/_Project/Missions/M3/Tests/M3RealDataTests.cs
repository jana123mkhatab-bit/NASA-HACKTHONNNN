using NUnit.Framework;
using UnityEngine;

namespace LastSignal.M3.Tests
{
    /// <summary>M3 must read the real Horizons-based CSV rows, not the sample.</summary>
    public class M3RealDataTests
    {
        [Test]
        public void SignalPath_RealFile_IsNotSample_AndMatchesHorizonsRow()
        {
            TextAsset a = Resources.Load<TextAsset>("LastSignal/m3_signal_path_2026-11-13");
            Assert.IsNotNull(a);
            SignalPathData d = SignalPathData.Parse(a.text, a.name);
            Assert.IsTrue(d.IsValid);
            Assert.IsFalse(d.isSample, "real file must not say SAMPLE");
            CsvTable h = CsvTable.Load("LastSignal/horizons_positions_2026-11-13");
            double moonKm, moonS;
            Assert.IsTrue(h.TryGetDouble("Moon", "dist_from_earth_km", out moonKm));
            Assert.IsTrue(h.TryGetDouble("Moon", "light_time_from_earth_s", out moonS));
            Assert.AreEqual(moonKm, d.Get(SignalPathData.RelayEarth).distanceKm, 1.0);
            Assert.AreEqual(moonS, d.Get(SignalPathData.RelayEarth).oneWaySeconds, 0.05);
        }

        [Test]
        public void SectorLayout_ReadsRealHorizonsAndRadii()
        {
            CsvTable h = CsvTable.Load(SectorLayout.HorizonsCsv), r = CsvTable.Load(SectorLayout.RadiiCsv);
            Vector3 off; Vector3 sun; double au; double radius;
            Assert.IsTrue(SectorLayout.TryMoonOffset(h, out off));
            Assert.IsTrue(SectorLayout.TrySunDirection(h, out sun, out au));
            Assert.IsTrue(SectorLayout.TryRadiusKm(r, "Earth", out radius));
        }
    }
}
