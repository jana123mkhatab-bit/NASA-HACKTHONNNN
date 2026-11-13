using System;
using System.Collections.Generic;
using UnityEngine;

namespace LastSignal.Core
{
    [Serializable]
    public class PipLine
    {
        public string key;
        [TextArea(1, 3)] public string text;
    }

    /// <summary>Optional ScriptableObject that overrides Pip's default lines. Resources/PipLines.asset.</summary>
    [CreateAssetMenu(menuName = "Last Signal/Pip Lines", fileName = "PipLines")]
    public class PipLines : ScriptableObject
    {
        public List<PipLine> lines = new List<PipLine>();
    }

    /// <summary>
    /// Pip, the nervous little AI guide. Anyone can call:
    ///     Pip.Say("Um, I think the frame is too heavy.");
    ///     Pip.SayKey("hint.M1");
    /// Lines come from Resources/PipLines.asset when it exists, otherwise from the built-in defaults.
    /// A hint is spoken automatically whenever a mission fails.
    /// </summary>
    public static class Pip
    {
        public static event Action<string> Spoke;

        static Dictionary<string, string> _cache;

        public static void Say(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            if (Spoke != null) Spoke(text);
        }

        public static void SayKey(string key)
        {
            Say(Lookup(key));
        }

        public static void Hint(string missionId)
        {
            Say(Lookup("hint." + missionId));
        }

        public static string Lookup(string key)
        {
            if (_cache == null) BuildCache();
            string text;
            if (_cache.TryGetValue(key, out text)) return text;
            return "Um... I do not have a line for '" + key + "' yet.";
        }

        /// <summary>The built-in lines. Also used by the editor menu to create the PipLines asset.</summary>
        public static List<PipLine> DefaultLines()
        {
            List<PipLine> list = new List<PipLine>();
            list.Add(new PipLine { key = "welcome", text = "Um, hello! I am Pip. I will help you with the controls. Please do not break anything." });
            list.Add(new PipLine { key = "hint.M1", text = "Check the mass budget first. Same thrust, more mass, less acceleration." });
            list.Add(new PipLine { key = "hint.M2", text = "Look at the weakest system. A backup costs mass, but one failure can end the mission." });
            list.Add(new PipLine { key = "hint.M3", text = "Keep the ship stable first. Then power the scanner. Please be careful out there." });
            list.Add(new PipLine { key = "hint.M4", text = "Read the scan data before you pick a site. Water and slope both matter." });
            list.Add(new PipLine { key = "hint.Final", text = "Take your time. Send the survey when you are ready." });
            list.Add(new PipLine { key = "hint.DesignMode", text = "Watch the gauge. If the number drops, a system is too weak." });
            return list;
        }

        static void BuildCache()
        {
            _cache = new Dictionary<string, string>();
            List<PipLine> defaults = DefaultLines();
            for (int i = 0; i < defaults.Count; i++) _cache[defaults[i].key] = defaults[i].text;

            PipLines asset = Resources.Load<PipLines>("PipLines");
            if (asset != null)
            {
                for (int i = 0; i < asset.lines.Count; i++)
                {
                    if (!string.IsNullOrEmpty(asset.lines[i].key)) _cache[asset.lines[i].key] = asset.lines[i].text;
                }
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Spoke = null;
            _cache = null;
        }
    }
}
