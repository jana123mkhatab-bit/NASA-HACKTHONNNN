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
                        "A piece of the capsule came down in the desert. Weigh it. Test the thrust. The serial number is stamped on the fragment.",
                        "Mass fights thrust. Check the numbers before the next test."),
                E("M2", "Your son's plan scores about ten percent. I ran it three times.",
                        "Every subsystem works only 63 times in 100. Five in a row, and the whole ship works one time in ten.",
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

            Card("M1", "What Fell to Earth", "Mass vs. thrust (thrust-to-weight ratio)",
                "NASA Glenn Research Center, Beginner's Guide to Rocketry",
                "A rocket only lifts off when thrust is larger than its weight. Every kilogram you add needs more thrust to carry it.");
            Card("M2", "Ten Percent", "Reliability and redundancy in series systems",
                "NASA Systems Engineering Handbook (NASA/SP-2016-6105)",
                "If every subsystem must work, the success chances multiply. Spare copies (redundancy) raise each subsystem's chance of working at the cost of mass. Reliability numbers in this game are game-balance values, not NASA data.");
            Card("M3", "The Ship That Came Out of Nowhere", "Orbital debris and collision risk",
                "NASA Orbital Debris Program Office (ODPO)",
                "Destroying a satellite creates fragments that stay in orbit and can hit other spacecraft. When resources are short, engineers triage repairs by what keeps the crew alive.");
            Card("M4", "Someone's Been Living Here", "Landing-site selection",
                "NASA Planetary Data System (PDS) and Lunar Reconnaissance Orbiter imagery",
                "Safe landing sites need gentle slopes and few boulders. Water ice makes a site valuable for a long stay.");
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
