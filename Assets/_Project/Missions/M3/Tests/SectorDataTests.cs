using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace LastSignal.M3.Tests
{
    /// <summary>The sector layout and NASA sites must come from the real CSVs in Data/Resources/LastSignal.</summary>
    public class SectorDataTests
    {
        [Test]
        public void MoonOffset_MatchesHorizonsDistance()
        {
            CsvTable h = CsvTable.Load(SectorLayout.HorizonsCsv);
            Vector3 off;
            Assert.IsTrue(SectorLayout.TryMoonOffset(h, out off), "Horizons CSV missing Earth/Moon rows");
            double listed;
            Assert.IsTrue(h.TryGetDouble("Moon", "dist_from_earth_km", out listed));
            double km = off.magnitude * SectorLayout.KmPerUnit;
            Assert.AreEqual(listed, km, listed * 0.001, "Moon placement must agree with the CSV distance within 0.1%");
        }

        [Test]
        public void SunDirection_IsUnit_AndRadiiLoad()
        {
            CsvTable h = CsvTable.Load(SectorLayout.HorizonsCsv);
            Vector3 dir;
            double au;
            Assert.IsTrue(SectorLayout.TrySunDirection(h, out dir, out au));
            Assert.AreEqual(1f, dir.magnitude, 1e-4f);
            Assert.Greater(au, 1.4e8);

            CsvTable r = CsvTable.Load(SectorLayout.RadiiCsv);
            double earth, moon, sun;
            Assert.IsTrue(SectorLayout.TryRadiusKm(r, "Earth", out earth));
            Assert.IsTrue(SectorLayout.TryRadiusKm(r, "Moon", out moon));
            Assert.IsTrue(SectorLayout.TryRadiusKm(r, "Sun", out sun));
            Assert.AreEqual(6371.0, earth, 1e-6);
            Assert.AreEqual(1737.4, moon, 1e-6);
            Assert.AreEqual(695700.0, sun, 1e-6);
        }

        [Test]
        public void FourSites_EachHasSourceWhyAndRealValues()
        {
            List<ChallengeSite> sites = ChallengeSite.LoadAll();
            Assert.AreEqual(4, sites.Count);
            foreach (ChallengeSite s in sites)
            {
                StringAssert.StartsWith("https://", s.sourceUrl, s.id);
                Assert.IsFalse(string.IsNullOrEmpty(s.why), s.id + " needs a why-NASA-cares line");
                StringAssert.Contains("[game balance]", s.balanceNote, s.id);
                Assert.Greater(s.values.Count, 0, s.id);
                foreach (KeyValuePair<string, string> v in s.values)
                    Assert.IsFalse(string.IsNullOrEmpty(v.Value), s.id + ": '" + v.Key + "' not found in " + s.dataFile + ".csv");
                StringAssert.Contains(s.sourceUrl, FreeFlight.CardText(s));
            }
        }

        [Test]
        public void Resolve_HandlesRowKeysWithSpaces()
        {
            CsvTable t = CsvTable.Parse("pl_name,radius_earth\nTRAPPIST-1 e,0.92\n");
            Assert.AreEqual("0.92", ChallengeSite.Resolve(t, "TRAPPIST-1 e.radius_earth"));
            Assert.IsNull(ChallengeSite.Resolve(t, "nope.radius_earth"));
        }
    }
}
