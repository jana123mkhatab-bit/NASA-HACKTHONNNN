using UnityEngine;

namespace LastSignal.M3
{
    /// <summary>
    /// Scene data that must survive a Retry reload. PlayerState is restored by the MissionManager;
    /// this keeps the ship's pose so the encounter restarts exactly at the impact (or at triage).
    /// </summary>
    public static class M3Checkpoint
    {
        public const string Impact = "m3_impact";
        public const string Triage = "m3_triage";

        public static bool hasImpact;
        public static Vector3 impactPosition;
        public static Quaternion impactRotation = Quaternion.identity;
        public static Vector3 impactVelocity;
        public static Vector3 impactSpin;
        public static Vector3 strangerOffset;

        public static bool hasTriage;
        public static Vector3 triagePosition;
        public static Quaternion triageRotation = Quaternion.identity;

        public static void SaveImpact(Vector3 pos, Quaternion rot, Vector3 vel, Vector3 spin, Vector3 strangerOff)
        {
            hasImpact = true;
            impactPosition = pos;
            impactRotation = rot;
            impactVelocity = vel;
            impactSpin = spin;
            strangerOffset = strangerOff;
        }

        public static void SaveTriage(Vector3 pos, Quaternion rot)
        {
            hasTriage = true;
            triagePosition = pos;
            triageRotation = rot;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Clear()
        {
            hasImpact = false;
            hasTriage = false;
        }
    }
}
