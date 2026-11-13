using System;
using UnityEngine;

namespace LastSignal.M4
{
    /// <summary>
    /// A candidate landing site with orbital scan data. All difficulty numbers are GAME-BALANCE values, not NASA data.
    /// Distance to the beacon is measured from the real positions in the generated terrain.
    /// </summary>
    [Serializable]
    public class LandingSite
    {
        public string id;              // "A", "B", "C"
        public string name;            // "VALLEY"
        public string blurb;
        public Vector2 xz;             // world position of the site centre
        [Range(0, 1)] public float waterSignal;
        public float slopeDeg;
        [Range(0, 1)] public float solarExposure;
        public float beaconDistance;   // filled by M4World from real positions
        public bool boulders;

        // ---- how the site changes the landing (game balance) ----
        public float SafeVerticalSpeed => Mathf.Lerp(3.0f, 1.7f, Mathf.InverseLerp(3f, 14f, slopeDeg)) * (boulders ? 0.85f : 1f);
        public float SafeHorizontalSpeed => Mathf.Lerp(1.6f, 0.9f, Mathf.InverseLerp(3f, 14f, slopeDeg));
        public float WindStrength => Mathf.Lerp(0.25f, 2.4f, Mathf.InverseLerp(3f, 14f, slopeDeg)); // m/s^2 gust peak
        public float PowerDrainPerSecond => 0.25f + (1f - solarExposure) * 1.7f;                   // % per second
        public float EllipseRadius => boulders ? 11f : Mathf.Lerp(26f, 10f, Mathf.InverseLerp(3f, 14f, slopeDeg));

        /// <summary>0 = easy, 1 = very hard. Shown as LOW / MEDIUM / HIGH risk.</summary>
        public float Risk
        {
            get
            {
                float r = Mathf.InverseLerp(3f, 16f, slopeDeg) * 0.4f + (1f - solarExposure) * 0.3f
                        + (boulders ? 0.15f : 0f) + Mathf.InverseLerp(100f, 500f, beaconDistance) * 0.15f;
                return Mathf.Clamp01(r);
            }
        }

        public string RiskLabel => Risk < 0.3f ? "LOW" : Risk < 0.55f ? "MEDIUM" : "HIGH";

        public static LandingSite[] Defaults() => new[]
        {
            new LandingSite { id = "A", name = "VALLEY", xz = new Vector2(38f, -112f), waterSignal = 0.92f, slopeDeg = 3f, solarExposure = 0.85f,
                blurb = "Flat valley floor. Strong water signal near a faint beacon." },
            new LandingSite { id = "B", name = "RIDGE", xz = new Vector2(230f, 170f), waterSignal = 0.30f, slopeDeg = 14f, solarExposure = 0.97f,
                blurb = "High ridge in full sun. Steep ground and strong gusts." },
            new LandingSite { id = "C", name = "CRATER", xz = new Vector2(-210f, -170f), waterSignal = 0.62f, slopeDeg = 8f, solarExposure = 0.28f, boulders = true,
                blurb = "Crater floor in deep shadow. Ice likely, boulders everywhere." },
        };
    }

    /// <summary>
    /// Survives the scene reload that MissionManager.RetryFromCheckpoint does, so a failed landing restarts
    /// at the approach (same site) instead of the orbit scan.
    /// </summary>
    public static class M4Checkpoint
    {
        public static bool atApproach;
        public static int siteIndex;
        public static int retriesWhenSaved;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Clear();
            MissionEvents.OnMissionStart -= OnStart;
            MissionEvents.OnMissionStart += OnStart;
        }

        static void OnStart(string id)
        {
            // Any earlier mission starting means a new attempt at the whole run: forget the approach.
            if (id == MissionIds.M1 || id == MissionIds.M2 || id == MissionIds.M3) Clear();
        }

        public static void SaveApproach(int site)
        {
            atApproach = true;
            siteIndex = site;
            retriesWhenSaved = MissionManager.Instance != null ? MissionManager.Instance.Retries : 0;
        }

        /// <summary>True when this scene load is a Retry after a failed landing.</summary>
        public static bool ShouldResume()
            => atApproach && MissionManager.Instance != null && MissionManager.Instance.Retries > retriesWhenSaved;

        public static void Clear() { atApproach = false; siteIndex = 0; retriesWhenSaved = 0; }
    }

    /// <summary>Public hooks for other team members. Static events, subscribe from anywhere.</summary>
    public static class M4Hooks
    {
        /// <summary>Phase names: Orbit, SitePicker, Descent, Landed, Walk, Reveal, Survey, Transmitted, CallHome, WalkTogether, End.</summary>
        public static event Action<string> OnPhaseChanged;
        public static event Action<bool, string> OnLanded;
        public static event Action OnSurveyTransmitted;

        /// <summary>
        /// Person 5: subscribe, play your reveal cinematic, then call the callback exactly once.
        /// If nobody subscribes, M4 plays its built-in fallback (camera push-in + subtitles).
        /// </summary>
        public static event Action<Action> RevealCinematic;

        internal static void Phase(string p) => OnPhaseChanged?.Invoke(p);
        internal static void Landed(bool ok, string reason) => OnLanded?.Invoke(ok, reason);
        internal static void Transmitted() => OnSurveyTransmitted?.Invoke();

        internal static bool TryExternalReveal(Action onDone)
        {
            if (RevealCinematic == null) return false;
            bool called = false;
            RevealCinematic(() => { if (called) return; called = true; onDone?.Invoke(); });
            return true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { OnPhaseChanged = null; OnLanded = null; OnSurveyTransmitted = null; RevealCinematic = null; }
    }
}
