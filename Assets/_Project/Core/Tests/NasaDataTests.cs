using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LastSignal.Tests
{
    /// <summary>Guards the NASA backing: every listed dataset has a source_url, a date, a why line and a file in Resources.</summary>
    public class NasaDataTests
    {
        [SetUp] public void Reset() { NasaData.ResetRun(); }

        [Test]
        public void EverySource_HasUrlDateWhy_AndFileInResources()
        {
            Assert.GreaterOrEqual(NasaData.All.Count, 10);
            foreach (NasaSource s in NasaData.All)
            {
                StringAssert.StartsWith("http", s.sourceUrl, s.file);
                Assert.IsNotEmpty(s.retrieved, s.file);
                Assert.IsNotEmpty(s.why, s.file);
                Assert.IsNotEmpty(s.usedBy, s.file);
                Assert.IsNotNull(Resources.Load<TextAsset>("LastSignal/" + s.file.Replace(".csv", "")), "missing file " + s.file);
            }
        }

        [Test]
        public void Tag_SaysNasaData_AndNotLive()
        {
            string t = NasaData.Card("horizons_positions_2026-11-13.csv");
            StringAssert.Contains("NASA DATA", t);
            StringAssert.Contains("not live", t);
            StringAssert.Contains("Why NASA cares", t);
            StringAssert.Contains("https://ssd.jpl.nasa.gov/horizons/", t);
        }

        [Test]
        public void FileFromRef_HandlesRowRefs()
        {
            Assert.AreEqual("horizons_positions_2026-11-13.csv", NasaData.FileFromRef("horizons_positions_2026-11-13.csv#Moon"));
            Assert.AreEqual("exoplanet_trappist1e.csv", NasaData.FileFromRef("exoplanet_trappist1e.csv (sy_dist 12.43 pc; km and s derived)"));
        }

        [Test]
        public void M1Debrief_ShowsRealSlsAcceleration_FromCsv()
        {
            string b = NasaData.DebriefBlock(MissionIds.M1);
            StringAssert.Contains("NASA DATA", b);
            StringAssert.Contains("m/s2", b);
            StringAssert.Contains("15.0", b);   // 39,144,350 N / 2,608,156 kg
        }

        [Test]
        public void M2Debrief_SaysPlainlyNoCsvDrivesIt()
        {
            StringAssert.Contains("[game balance]", NasaData.DebriefBlock(MissionIds.M2));
        }

        [Test]
        public void Fallback_IsRecorded_AndShownInDataAndSources()
        {
            NasaData.ReportFallback("dsn_notes.csv", "test reason");
            Assert.IsTrue(NasaData.HasFallback("dsn_notes.csv"));
            StringAssert.Contains("FALLBACK USED", DataSourcesScreen.Describe());
            StringAssert.Contains("test reason", NasaData.PlaythroughList());
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("FALLBACK"));
        }

        [Test]
        public void PlaythroughList_NamesMarkedDatasets()
        {
            NasaData.Mark("exoplanet_trappist1e.csv", MissionIds.M4, "x");
            string l = NasaData.PlaythroughList();
            StringAssert.Contains("TRAPPIST-1 e", l);
            StringAssert.Contains("2026-11-13", l);
        }

        [Test]
        public void DataAndSources_StatesSnapshot_AndListsEveryDataset()
        {
            string d = DataSourcesScreen.Describe();
            foreach (NasaSource s in NasaData.All) StringAssert.Contains(s.file, d);
            StringAssert.Contains("not live", NasaData.SnapshotLine.ToLower());
            StringAssert.Contains("2026-11-13", NasaData.SnapshotLine);
        }

        [Test]
        public void M4_SyncedExoplanetCsv_IsTheRealNasaRow_NotTheFallback()
        {
            TextAsset real = Resources.Load<TextAsset>("LastSignal/M4/exoplanets");
            Assert.IsNotNull(real, "M4 would fall back");
            StringAssert.Contains("TRAPPIST-1 e", real.text);
            StringAssert.Contains("exoplanetarchive.ipac.caltech.edu", real.text);
            StringAssert.DoesNotContain("Kepler-442 b", real.text);
        }
    }
}
