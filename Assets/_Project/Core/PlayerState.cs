using System;
using UnityEngine;

namespace LastSignal.Core
{
    /// <summary>
    /// Shared player state (part of the contract). Everyone reads and writes it through PlayerState.Current.
    /// PlayerState.Current is a runtime copy, so playing never changes the asset on disk.
    /// </summary>
    [CreateAssetMenu(menuName = "Last Signal/Player State", fileName = "PlayerState")]
    public class PlayerState : ScriptableObject
    {
        [Range(0f, 100f)] public float supplies = 100f;
        [Range(0f, 100f)] public float hull = 100f;
        [Range(0f, 100f)] public float power = 100f;
        [Range(0f, 100f)] public float fuel = 100f;
        public Vector3 heading = Vector3.forward;
        public bool signalPathUp = true;
        [Range(0f, 1f)] public float plannedSuccess = 0f;

        /// <summary>Raised whenever a value changes through the helper methods or NotifyChanged().</summary>
        public event Action Changed;

        public void NotifyChanged()
        {
            if (Changed != null) Changed();
        }

        public void SetSupplies(float value) { supplies = Mathf.Clamp(value, 0f, 100f); NotifyChanged(); }
        public void SetHull(float value) { hull = Mathf.Clamp(value, 0f, 100f); NotifyChanged(); }
        public void SetPower(float value) { power = Mathf.Clamp(value, 0f, 100f); NotifyChanged(); }
        public void SetFuel(float value) { fuel = Mathf.Clamp(value, 0f, 100f); NotifyChanged(); }
        public void SetSignalPath(bool up) { signalPathUp = up; NotifyChanged(); }

        public void SetPlannedSuccess(float value01)
        {
            plannedSuccess = Mathf.Clamp01(value01);
            NotifyChanged();
        }

        public void ResetToDefaults()
        {
            supplies = 100f;
            hull = 100f;
            power = 100f;
            fuel = 100f;
            heading = Vector3.forward;
            signalPathUp = true;
            plannedSuccess = 0f;
            NotifyChanged();
        }

        public string ToSnapshot()
        {
            return JsonUtility.ToJson(this);
        }

        public void RestoreSnapshot(string json)
        {
            if (string.IsNullOrEmpty(json)) return;
            JsonUtility.FromJsonOverwrite(json, this);
            NotifyChanged();
        }

        static PlayerState _current;

        /// <summary>The live player state for this play session.</summary>
        public static PlayerState Current
        {
            get
            {
                if (_current == null)
                {
                    PlayerState template = Resources.Load<PlayerState>("PlayerState");
                    _current = template != null ? Instantiate(template) : CreateInstance<PlayerState>();
                    _current.name = "PlayerState (runtime)";
                    _current.hideFlags = HideFlags.DontSave;
                }
                return _current;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _current = null;
        }
    }
}
