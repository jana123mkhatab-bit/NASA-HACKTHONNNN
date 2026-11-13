using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastSignal
{
    /// <summary>
    /// Runs automatically in every scene (even one opened alone in the Editor) and makes sure the
    /// MissionManager, HUD, Pip, log, debrief, fail and end screens exist exactly once.
    /// </summary>
    public static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            var scene = SceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(scene.path))
            {
                SceneManager.LoadScene(MissionIds.SceneFor(MissionIds.M1));
                return;
            }

            var mm = MissionManager.Ensure();
            var root = mm.gameObject;

            UIFactory.EnsureEventSystem();

            if (mm.hud == null) mm.hud = Add<HudShell>(root, "HudShell", h => h.Build());
            if (mm.pipUI == null) mm.pipUI = Add<PipDialogueUI>(root, "PipDialogue", p => p.Build());
            if (root.GetComponentInChildren<FatherLogUI>() == null) Add<FatherLogUI>(root, "FatherLog", l => l.Build());
            if (mm.debriefUI == null) mm.debriefUI = Add<DebriefCardUI>(root, "DebriefCard", d => d.Build());
            if (mm.failUI == null) mm.failUI = Add<FailPanelUI>(root, "FailPanel", f => f.Build());
            if (mm.endUI == null) mm.endUI = Add<EndCardUI>(root, "EndCard", e => e.Build());
            if (mm.fader == null) mm.fader = Add<ScreenFader>(root, "Fader", f => f.Build());
            Sfx.Ambience(true);

            if (scene.name == MissionIds.SceneFor(MissionIds.M1) &&
                mm.Current == MissionIds.Boot && root.GetComponentInChildren<TitleScreenUI>() == null)
                Add<TitleScreenUI>(root, "Title", t => t.Build());
        }

        static T Add<T>(GameObject parent, string name, System.Action<T> build) where T : Component
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var c = go.AddComponent<T>();
            build(c);
            return c;
        }
    }
}
