using UnityEngine;
using UnityEngine.UI;

namespace LastSignal
{
    public class M1GroundMission : MonoBehaviour
    {
        Canvas _canvas;
        Text _readout;
        bool _propulsion;
        bool _payload;
        bool _serialFound;

        void Start()
        {
            var stub = FindFirstObjectByType<StubMission>();
            if (stub != null) Destroy(stub.gameObject);
            _canvas = UIFactory.CreateCanvas("M1 Ground Test", 20);
            _canvas.transform.SetParent(transform, false);
            var root = (RectTransform)_canvas.transform;
            var left = UIFactory.Box("TestFrame", root, new Vector2(0, .5f), new Vector2(0, .5f),
                new Vector2(40, -350), new Vector2(930, 390), UIFactory.Panel);
            UIFactory.Label("Title", left, "M1 · TEST FRAME BENCH", 30, TextAnchor.UpperLeft,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(28, -65), new Vector2(-28, -20), UIFactory.Accent);
            UIFactory.Button("Propulsion", left, "Snap propulsion module", new Vector2(0, 1), new Vector2(.5f, 1),
                new Vector2(28, -150), new Vector2(-10, -92), () => { _propulsion = !_propulsion; Refresh(); });
            UIFactory.Button("Payload", left, "Snap payload module", new Vector2(.5f, 1), new Vector2(1, 1),
                new Vector2(10, -150), new Vector2(-28, -92), () => { _payload = !_payload; Refresh(); });
            _readout = UIFactory.Label("Readout", left, "", 24, TextAnchor.UpperLeft,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(28, -250), new Vector2(-28, -165));
            UIFactory.Button("TestFire", left, "TEST FIRE", new Vector2(0, 0), new Vector2(.5f, 0),
                new Vector2(28, 28), new Vector2(-10, 90), TestFire);
            UIFactory.Button("Planner", left, "Open mission planner", new Vector2(.5f, 0), new Vector2(1, 0),
                new Vector2(10, 28), new Vector2(-28, 90), OpenPlanner);

            var parts = UIFactory.Box("PartsDiagram", root, new Vector2(1, .5f), new Vector2(1, .5f),
                new Vector2(-720, -350), new Vector2(-40, 390), UIFactory.Panel);
            UIFactory.Label("PartsTitle", parts, "PARTS DIAGRAM · RECOVERY ARCHIVE", 24, TextAnchor.UpperLeft,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(24, -60), new Vector2(-24, -18), UIFactory.Accent);
            UIFactory.Button("Serial", parts, "Inspect serial number", new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(24, -145), new Vector2(-24, -90), FindSerial);
            UIFactory.Label("Archive", parts, "The ground-test archive stores the last recorded heading after a valid test fire.\n\nSerial: --\nHeading: --", 22, TextAnchor.UpperLeft,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(24, -260), new Vector2(-24, -155));
            Refresh();
        }

        void Refresh()
        {
            if (_readout == null) return;
            float mass = (_propulsion ? 8f : 0f) + (_payload ? 3f : 0f);
            float acceleration = _propulsion && mass > 0f ? 120f / mass : 0f;
            _readout.text = "Propulsion: " + (_propulsion ? "SECURED" : "not installed") +
                "\nPayload: " + (_payload ? "SECURED" : "not installed") +
                "\nMass: " + mass.ToString("0.0") + " / 12.0 kg" +
                "\nAcceleration: " + acceleration.ToString("0.00") + " m/s2";
        }

        void TestFire()
        {
            float mass = (_propulsion ? 8f : 0f) + (_payload ? 3f : 0f);
            if (!_propulsion || !_payload || mass > 12f)
                MissionEvents.RaiseFail(MissionIds.M1, "The test frame is overweight or a module is not secured.");
            else
            {
                if (MissionManager.Instance != null) MissionManager.Instance.state.heading = new Vector3(.42f, .18f, .89f);
                MissionEvents.RaiseComplete(MissionIds.M1);
            }
        }

        void FindSerial()
        {
            _serialFound = true;
            UIFactory.Label("Found", _canvas.transform, "Serial: LK-2590-041\nLast heading: 24° 11' 08\" / 81° 42' 19\"", 22,
                TextAnchor.UpperLeft, new Vector2(1, .5f), new Vector2(1, .5f), new Vector2(-690, -300), new Vector2(-70, -225));
        }

        void OpenPlanner()
        {
            var prefab = Resources.Load<GameObject>("LastSignal/PlannerPanel");
            var go = prefab != null ? Instantiate(prefab) : new GameObject("Planner");
            var planner = go.GetComponent<PlannerPanel>();
            if (planner == null) planner = go.AddComponent<PlannerPanel>();
            planner.Build();
        }
    }
}
