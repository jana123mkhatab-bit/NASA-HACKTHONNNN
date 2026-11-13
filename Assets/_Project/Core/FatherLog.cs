using System;
using System.Collections.Generic;
using UnityEngine;

namespace LastSignal.Core
{
    /// <summary>
    /// The father's log. Entries are added automatically when a mission completes
    /// (text from the storyline) and anyone can add more with FatherLog.Add("...").
    /// </summary>
    public static class FatherLog
    {
        public static event Action Changed;

        static readonly List<string> _entries = new List<string>();

        public static IList<string> Entries { get { return _entries; } }

        public static void Add(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            _entries.Add(text);
            if (Changed != null) Changed();
        }

        public static void Clear()
        {
            _entries.Clear();
            if (Changed != null) Changed();
        }

        public static void AddForMission(string missionId)
        {
            Add(TextFor(missionId));
        }

        public static string TextFor(string missionId)
        {
            switch (missionId)
            {
                case MissionIds.M1:
                    return "I've spent months looking at the piece that fell. Today I saw how much of the ship could have kept going.";
                case MissionIds.M2:
                    return "He knew the flight was dangerous. He deserved to know how dangerous. I'm taking both copies with me.";
                case MissionIds.M3:
                    return "Someone put their ship between me and that station. Then they took half my supplies and vanished. Pip found somewhere we can land.";
                default:
                    return null;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _entries.Clear();
            Changed = null;
        }
    }
}
