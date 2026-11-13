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
            if (MissionManager.Instance != null) return;
            var mm = MissionManager.Ensure();
            var root = mm.gameObject;

            UIFactory.EnsureEventSystem();

            mm.hud = Add<HudShell>(root, "HudShell", h => h.Build());
            mm.pipUI = Add<PipDialogueUI>(root, "PipDialogue", p => p.Build());
            Add<FatherLogUI>(root, "FatherLog", l => l.Build());
            mm.debriefUI = Add<DebriefCardUI>(root, "DebriefCard", d => d.Build());
            mm.failUI = Add<FailPanelUI>(root, "FailPanel", f => f.Build());
            mm.endUI = Add<EndCardUI>(root, "EndCard", e => e.Build());

            if (SceneManager.GetActiveScene().name == MissionIds.SceneFor(MissionIds.M1))
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
