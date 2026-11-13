using System;
using System.Collections.Generic;
using UnityEngine;

namespace LastSignal
{
    /// <summary>End-of-mission card naming the real NASA concept and data source.</summary>
    [CreateAssetMenu(menuName = "LastSignal/DebriefCard", fileName = "DebriefCard")]
    public class DebriefCard : ScriptableObject
    {
        public string title;
        public string concept;
        public string nasaSource;
        [TextArea(3, 10)] public string body;
    }
}
