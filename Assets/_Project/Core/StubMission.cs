using UnityEngine;
using UnityEngine.UI;

namespace LastSignal.Core
{
    /// <summary>
    /// Placeholder mission used by the generated scenes until a team member replaces it.
    /// Shows buttons to set a checkpoint, fail and complete, so the flow can be tested end to end.
    /// Delete this component from your scene when you add the real mission.
    /// </summary>
    public class StubMission : MonoBehaviour
    {
        public string missionId;
        int _checkpointCount;

        void Start()
        {
            if (string.IsNullOrEmpty(missionId)) missionId = MissionIds.IdForScene(gameObject.scene.name);
            Canvas canvas = UIFactory.CreateCanvas("StubMission", 20, transform);

            Text title = UIFactory.CreateText("Title", canvas.transform, "PLACEHOLDER: " + MissionIds.Title(missionId), 48, UIFactory.TextColor, TextAnchor.MiddleCenter);
            UIFactory.SetCenter(title.rectTransform, 0f, 160f, 1500f, 80f);

            Text note = UIFactory.CreateText("Note", canvas.transform, "The real mission replaces this scene content.", 28, UIFactory.Muted, TextAnchor.MiddleCenter);
            UIFactory.SetCenter(note.rectTransform, 0f, 90f, 1500f, 50f);

            Button cp = UIFactory.CreateButton("Checkpoint", canvas.transform, "Set checkpoint", delegate
            {
                _checkpointCount++;
                PlayerState.Current.SetFuel(PlayerState.Current.fuel - 10f);
                MissionManager.Checkpoint("stub_" + _checkpointCount);
            });
            UIFactory.SetCenter(cp.GetComponent<RectTransform>(), -330f, -20f, 300f, 64f);

            Button fail = UIFactory.CreateButton("Fail", canvas.transform, "Fail mission", delegate
            {
                MissionEvents.OnMissionFail(missionId, "Stub failure: pressed the Fail button.");
            });
            UIFactory.SetCenter(fail.GetComponent<RectTransform>(), 0f, -20f, 300f, 64f);

            Button done = UIFactory.CreateButton("Complete", canvas.transform, "Complete mission", delegate
            {
                MissionEvents.OnMissionComplete(missionId);
            });
            UIFactory.SetCenter(done.GetComponent<RectTransform>(), 330f, -20f, 300f, 64f);
        }
    }
}
