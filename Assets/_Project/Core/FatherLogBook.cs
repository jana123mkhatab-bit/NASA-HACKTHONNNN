using System;
using System.Collections.Generic;
using UnityEngine;

namespace LastSignal
{
    /// <summary>The father's log entries, unlocked per mission.</summary>
    [CreateAssetMenu(menuName = "LastSignal/FatherLogBook", fileName = "FatherLog")]
    public class FatherLogBook : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public string unlockedAfter; // mission id; "" = always
            public string heading;
            [TextArea(2, 8)] public string text;
        }

        public List<Entry> entries = new List<Entry>();
    }
}
