using System;
using System.Collections.Generic;
using UnityEngine;

namespace LastSignal
{
    /// <summary>Shared run state. Create via Assets/Create/LastSignal/PlayerState or let MissionManager make one.</summary>
    [CreateAssetMenu(menuName = "LastSignal/PlayerState", fileName = "PlayerState")]
    public class PlayerState : ScriptableObject
    {
        public static PlayerState Current { get; private set; }
        public event Action Changed;

        public float supplies = 100f, hull = 100f, power = 100f, fuel = 100f;
        public Vector3 heading;
        public bool signalPathUp;
        [Range(0f, 1f)] public float plannedSuccess;

        [Serializable]
        public struct Snapshot
        {
            public float supplies, hull, power, fuel;
            public Vector3 heading;
            public bool signalPathUp;
            public float plannedSuccess;
        }

        public void ResetToDefaults()
        {
            supplies = hull = power = fuel = 100f;
            heading = Vector3.zero;
            signalPathUp = false;
            plannedSuccess = 0f;
        }

        public void SetHull(float value) => hull = Mathf.Clamp(value, 0f, 100f);
        public void SetFuel(float value) => fuel = Mathf.Clamp(value, 0f, 100f);
        public void SetSupplies(float value) => supplies = Mathf.Clamp(value, 0f, 100f);
        public void SetPower(float value) => power = Mathf.Clamp(value, 0f, 100f);
        public void SetSignalPath(bool value) => signalPathUp = value;
        public void SetPlannedSuccess(float value) { plannedSuccess = Mathf.Clamp01(value); NotifyChanged(); }
        public void NotifyChanged() { Changed?.Invoke(); }

        public static void SetCurrent(PlayerState state) { Current = state; }

        public Snapshot Capture() => new Snapshot
        {
            supplies = supplies, hull = hull, power = power, fuel = fuel,
            heading = heading, signalPathUp = signalPathUp, plannedSuccess = plannedSuccess
        };

        public void Restore(Snapshot s)
        {
            supplies = s.supplies; hull = s.hull; power = s.power; fuel = s.fuel;
            heading = s.heading; signalPathUp = s.signalPathUp; plannedSuccess = s.plannedSuccess;
        }
    }
}
