using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastSignal.M3
{
    /// <summary>
    /// Wiring fix: the shipped M3_DeepSpace scene only holds the StubMission (the "Set up M3 scene" menu pointed at a
    /// scene path that does not exist), so M3Director never ran. Same pattern as M4Bootstrap: every time M3_DeepSpace
    /// loads, swap the stub for M3Director and switch off the stub's scenery and orbit camera that would fight the
    /// chase camera. Does nothing if the scene already has an M3Director.
    /// </summary>
    public static class M3Bootstrap
    {
        static readonly string[] StubScenery = { "Earth System", "Moon" };   // M3World builds its own Earth and Moon

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Hook()
        {
            SceneManager.sceneLoaded -= OnLoaded;
            SceneManager.sceneLoaded += OnLoaded;
        }

        static void OnLoaded(Scene s, LoadSceneMode mode)
        {
            if (s.name == MissionIds.SceneFor(MissionIds.M3)) Install(s);
        }

        /// <summary>Makes sure the scene runs M3Director instead of the stub. Returns the director.</summary>
        public static M3Director Install(Scene scene)
        {
            M3Director director = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (director == null) director = root.GetComponentInChildren<M3Director>(true);
                foreach (StubMission stub in root.GetComponentsInChildren<StubMission>(true))
                {
                    stub.enabled = false;
                    if (Application.isPlaying) Object.Destroy(stub.gameObject);
                    else Object.DestroyImmediate(stub.gameObject);
                }
                if (root == null) continue;   // the stub was this root object
                foreach (CameraOrbit orbit in root.GetComponentsInChildren<CameraOrbit>(true)) orbit.enabled = false;
                if (System.Array.IndexOf(StubScenery, root.name) >= 0) root.SetActive(false);
            }
            if (director == null)
            {
                GameObject go = new GameObject("M3_Director");
                SceneManager.MoveGameObjectToScene(go, scene);
                director = go.AddComponent<M3Director>();
            }
            return director;
        }
    }
}
