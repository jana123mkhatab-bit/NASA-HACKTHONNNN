using UnityEngine;

namespace LastSignal.Core
{
    /// <summary>Makes sure the MissionManager exists no matter which scene you press Play in.</summary>
    public static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            MissionManager.EnsureExists();
        }
    }
}
