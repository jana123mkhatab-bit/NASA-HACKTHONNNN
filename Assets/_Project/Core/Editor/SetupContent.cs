using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LastSignal.EditorTools
{
    /// <summary>Creates the ScriptableObject data: PlayerState, Pip lines, father's log, debrief cards.</summary>
    public static class SetupContent
    {
        public const string ResDir = "Assets/_Project/Core/Resources/LastSignal";

        public static void Run()
        {
            Directory.CreateDirectory(ResDir);

            var ps = Load<PlayerState>("PlayerState");
            ps.ResetToDefaults();
            ps.plannedSuccess = RiskModel.ComputeSuccess(PlanConfig.SonsOriginalPlan());
            EditorUtility.SetDirty(ps);

            var pip = Load<PipLineSet>("PipLines");
            pip.entries = new List<PipLineSet.Entry>
            {
                E("M1", "I am Pip. I keep the count so you do not have to.",
                        "Build a test frame on the sled. Pick an engine and only the payload this test needs, lock the clamps, then fire. Watch a = F / m.",
                        "Mass fights thrust. Check the numbers before the next test."),
                E("M2", "Your son's flight assessment is in the records. It was never in his briefing.",
                        "Open it, then fix the plan in the Mission Planner. Every check runs against the plan you apply.",
                        "One copy of each part is not enough. Add a spare where the failures hurt most."),
                E("M3", "A relay satellite is in your path. It will be destroyed.",
                        "Something is coming up behind us. It is not on any chart.",
                        "You cannot fix everything. Repair what keeps you alive first."),
                E("M4", "The scanner found a planet with water.",
                        "Pick your landing site with care. Slope, rocks and ice all matter.",
                        "Look at the slope and the rocks before you pick a site. Water is nearby."),
                E("Final", "The survey is ready. Home is listening.",
                        "Open the channel. Say what you found.",
                        "Raise the antenna and align it before you transmit."),
                E("DesignMode", "Now it is your turn. Design any mission you like.",
                        "Pick subsystems, add redundancy, and watch the success probability move.",
                        "Start with the cheapest change that raises the gauge the most."),
            };
            EditorUtility.SetDirty(pip);

            var log = Load<FatherLogBook>("FatherLog");
            log.entries = new List<FatherLogBook.Entry>
            {
                L("", "Day 1", "They found the capsule. I keep thinking he is alive."),
                L("M1", "Day 9", "The serial number matched. It was his ship. The last heading points past Mars."),
                L("M2", "Day 20", "I fixed his plan. Ten percent became seventy. He would have laughed."),
                L("M3", "Day 41", "A ship came out of nowhere and took half my supplies. It did not answer my hails."),
                L("M4", "Day 63", "The water collector is his. The footprints are fresh."),
            };
            EditorUtility.SetDirty(log);

            Card("M1", "Why mass is the enemy of every mission", "Newton's second law: acceleration = thrust / mass",
                "NASA Glenn Research Center, Beginner's Guide to Rockets (Newton's laws of motion) [person 6: confirm exact page]",
                "The same engine pushes a heavier frame more slowly. Every kilogram you add needs more thrust, and on a real rocket more propellant, " +
                "which is more mass again (the rocket equation). Engineers fight for every kilogram. Part masses and thrusts in M1 are game-balance values.");
            Card("M2", "Redundancy, and why risk numbers must reach the crew", "Reliability of series systems and redundancy",
                "NASA Systems Engineering Handbook, NASA/SP-2016-6105 Rev2 [person 6: confirm section]",
                "If every subsystem must work, their chances multiply: five parts at 63% give about 10%. A backup unit raises one subsystem from r to 1 - (1 - r)^n, " +
                "at the cost of mass. Real programs also make sure the crew sees the risk number. Reliabilities in this game are game-balance values, not NASA data.");
            Card("M3", "Single points of failure in deep-space communication", "Relay networks and light delay",
                "NASA Deep Space Network (deepspace.jpl.nasa.gov/dsn); JPL Horizons, retrieved 2026-11-13",
                "A signal from Mars takes about 11 minutes to reach Earth, and about 44 minutes from Jupiter. You cannot ask Earth what to do, so the ship must decide alone. " +
                "NASA's Deep Space Network uses three sites, in California, Spain and Australia, so one of them can always hear the ship as Earth turns. " +
                "If your ship has only one radio, that radio is a single point of failure. A backup unit is what keeps the link alive.");
            Card("M4", "How scientists judge habitability from a few numbers", "Habitability from radius, insolation and temperature",
                "NASA Exoplanet Archive, table pscomppars, planet TRAPPIST-1 e, retrieved 2026-11-13",
                "Four numbers give a first guess about a planet. TRAPPIST-1 e has a radius of 0.92 Earths and gets 0.646 times Earth's sunlight. " +
                "Its equilibrium temperature is 249.7 K, and it orbits every 6.1 days. Those values are in the range where liquid water could exist, " +
                "but they are not proof. It depends on the atmosphere. Scientists call this habitable-zone reasoning, not a promise of life.");
            Card("Final", "Call Home", "Deep-space communications",
                "NASA Deep Space Network (JPL)",
                "Signals take time to travel, and large antennas on Earth must point exactly at the spacecraft to hear it.");

            AssetDatabase.SaveAssets();
        }

        static T Load<T>(string name) where T : ScriptableObject
        {
            string path = ResDir + "/" + name + ".asset";
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a == null) { a = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(a, path); }
            return a;
        }

        static PipLineSet.Entry E(string id, params string[] lines)
        {
            return new PipLineSet.Entry
            {
                missionId = id,
                intro = new[] { lines[0], lines[1] },
                failHint = lines[2],
            };
        }

        static FatherLogBook.Entry L(string after, string heading, string text) =>
            new FatherLogBook.Entry { unlockedAfter = after, heading = heading, text = text };

        static void Card(string id, string title, string concept, string source, string body)
        {
            var c = Load<DebriefCard>("Debrief_" + id);
            c.title = title; c.concept = concept; c.nasaSource = source; c.body = body;
            EditorUtility.SetDirty(c);
        }
    }
}
