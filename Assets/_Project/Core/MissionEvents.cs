using System;
using UnityEngine;

namespace LastSignal.Core
{
    /// <summary>
    /// Shared mission events (part of the contract).
    ///
    /// To finish or fail a mission from any scene, call:
    ///     MissionEvents.OnMissionComplete("M1");
    ///     MissionEvents.OnMissionFail("M1", "Frame was overweight");
    ///
    /// The MissionManager listens and takes care of the Debrief card, retry and the next scene.
    /// Subscribe to MissionStarted / MissionCompleted / MissionFailed if you need to react.
    /// </summary>
    public static class MissionEvents
    {
        public static event Action<string> MissionStarted;
        public static event Action<string> MissionCompleted;
        public static event Action<string, string> MissionFailed;

        /// <summary>Raised by the MissionManager when a mission scene begins. Missions normally do not call this.</summary>
        public static void OnMissionStart(string id)
        {
            if (MissionStarted != null) MissionStarted(id);
        }

        public static void OnMissionComplete(string id)
        {
            if (MissionCompleted != null) MissionCompleted(id);
        }

        public static void OnMissionFail(string id, string reason)
        {
            if (MissionFailed != null) MissionFailed(id, reason);
        }

        // Keeps events clean when "Enter Play Mode" options disable domain reload.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            MissionStarted = null;
            MissionCompleted = null;
            MissionFailed = null;
        }
    }
}
