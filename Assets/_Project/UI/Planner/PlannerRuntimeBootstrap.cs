using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastSignal
{
    public static class PlannerRuntimeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AttachMissionController()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "M1_Ground" && Object.FindFirstObjectByType<M1GroundMission>() == null)
                new GameObject("M1 Ground Mission").AddComponent<M1GroundMission>();
            if (scene.name == "M2_Preflight" && Object.FindFirstObjectByType<M2PreflightMission>() == null)
                new GameObject("M2 Preflight Mission").AddComponent<M2PreflightMission>();
            if (scene.name == "DesignMode" && Object.FindFirstObjectByType<DesignModeController>() == null)
                new GameObject("Design Mode").AddComponent<DesignModeController>();
        }
    }
}
