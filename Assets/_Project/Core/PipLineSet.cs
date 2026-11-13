using System;
using System.Collections.Generic;
using UnityEngine;

namespace LastSignal
{
    /// <summary>Pip's lines, per mission, plus hints shown when a mission fails.</summary>
    [CreateAssetMenu(menuName = "LastSignal/PipLineSet", fileName = "PipLines")]
    public class PipLineSet : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public string missionId;
            [TextArea(1, 4)] public string[] intro;
            [TextArea(1, 3)] public string failHint;
        }

        public List<Entry> entries = new List<Entry>();

        public Entry Find(string id) => entries.Find(e => e.missionId == id);
    }
}
