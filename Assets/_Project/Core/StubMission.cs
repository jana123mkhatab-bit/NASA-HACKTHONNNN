using UnityEngine;
using UnityEngine.UI;

namespace LastSignal
{
    /// <summary>
    /// Placeholder mission: two buttons that raise Complete or Fail for whatever mission is current.
    /// Owners delete this component from their scene once the real mission exists.
    /// </summary>
    public class StubMission : MonoBehaviour
    {
        Canvas _canvas;
        Text _label;

        void Start()
        {
            _canvas = UIFactory.CreateCanvas("StubMission", 5);
            _canvas.transform.SetParent(transform, false);
            var root = (RectTransform)_canvas.transform;
            var p = UIFactory.Box("Panel", root, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-460, -230), new Vector2(-20, -20), UIFactory.Panel);
            _label = UIFactory.Label("Label", p, "", 24, TextAnchor.UpperLeft,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(14, -80), new Vector2(-14, -10));
            UIFactory.Button("Complete", p, "Complete mission", new Vector2(0, 0), new Vector2(0.5f, 0), new Vector2(10, 12), new Vector2(-5, 70),
                () => MissionEvents.RaiseComplete(CurrentId()));
            UIFactory.Button("Fail", p, "Fail mission", new Vector2(0.5f, 0), new Vector2(1, 0), new Vector2(5, 12), new Vector2(-10, 70),
                () => MissionEvents.RaiseFail(CurrentId(), "Stub failure for testing the retry flow."));
        }

        string CurrentId()
        {
            var m = MissionManager.Instance;
            return m != null && m.Current != MissionIds.Boot ? m.Current : MissionIds.M1;
        }

        void Update()
        {
            if (_label == null) return;
            var m = MissionManager.Instance;
            bool title = m == null || m.Current == MissionIds.Boot && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == MissionIds.SceneFor(MissionIds.M1);
            _canvas.enabled = !title;
            _label.text = "STUB MISSION  " + CurrentId() + "\n" + MissionIds.TitleFor(CurrentId());
        }
    }
}
