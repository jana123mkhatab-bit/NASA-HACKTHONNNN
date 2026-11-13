using UnityEngine;
using UnityEngine.UI;

namespace LastSignal
{
    public class M2PreflightMission : MonoBehaviour
    {
        Canvas _canvas;
        Text _status;
        bool _lifeSupport, _comms, _navigation, _emergency;
        PlannerPanel _planner;

        void Start()
        {
            var stub = FindFirstObjectByType<StubMission>();
            if (stub != null) Destroy(stub.gameObject);
            _canvas = UIFactory.CreateCanvas("M2 Preflight", 20);
            _canvas.transform.SetParent(transform, false);
            var root = (RectTransform)_canvas.transform;
            var panel = UIFactory.Box("Checks", root, new Vector2(0, .5f), new Vector2(0, .5f),
                new Vector2(40, -350), new Vector2(930, 390), UIFactory.Panel);
            UIFactory.Label("Title", panel, "M2 · PREFLIGHT SYSTEM CHECKS", 30, TextAnchor.UpperLeft,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(28, -65), new Vector2(-28, -20), UIFactory.Accent);
            UIFactory.Button("LifeSupport", panel, "Check life support", new Vector2(0, 1), new Vector2(.5f, 1),
                new Vector2(28, -145), new Vector2(-10, -92), () => { _lifeSupport = true; Refresh(); });
            UIFactory.Button("Comms", panel, "Check backup comms", new Vector2(.5f, 1), new Vector2(1, 1),
                new Vector2(10, -145), new Vector2(-28, -92), () => { _comms = true; Refresh(); });
            UIFactory.Button("Navigation", panel, "Check navigation", new Vector2(0, 1), new Vector2(.5f, 1),
                new Vector2(28, -220), new Vector2(-10, -167), () => { _navigation = true; Refresh(); });
            UIFactory.Button("Emergency", panel, "Run emergency radio cut", new Vector2(.5f, 1), new Vector2(1, 1),
                new Vector2(10, -220), new Vector2(-28, -167), () => { _emergency = true; Refresh(); });
            _status = UIFactory.Label("Status", panel, "", 21, TextAnchor.UpperLeft,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(28, -315), new Vector2(-28, -245));
            UIFactory.Button("Preflight", panel, "RUN PREFLIGHT", new Vector2(0, 0), new Vector2(1, 0),
                new Vector2(28, 28), new Vector2(-28, 90), Preflight);

            var plan = UIFactory.Box("Plan", root, new Vector2(1, .5f), new Vector2(1, .5f),
                new Vector2(-720, -350), new Vector2(-40, 390), UIFactory.Panel);
            UIFactory.Label("PlanTitle", plan, "MISSION PLAN", 26, TextAnchor.UpperLeft,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(24, -60), new Vector2(-24, -18), UIFactory.Accent);
            UIFactory.Button("OpenPlanner", plan, "Open planner", new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(24, -145), new Vector2(-24, -90), OpenPlanner);
            UIFactory.Button("LoadSon", plan, "Load son's configuration", new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(24, -215), new Vector2(-24, -160), LoadSon);
            Refresh();
        }

        void Refresh()
        {
            if (_status == null) return;
            _status.text = "Life support: " + (_lifeSupport ? "PASS" : "PENDING") +
                "\nBackup comms: " + (_comms ? "PASS" : "PENDING") +
                "\nNavigation: " + (_navigation ? "PASS" : "PENDING") +
                "\nRadio cut: " + (_emergency ? "PASS · sealed supplies retained" : "PENDING");
        }

        void OpenPlanner()
        {
            if (_planner == null)
            {
                var prefab = Resources.Load<GameObject>("LastSignal/PlannerPanel");
                var go = prefab != null ? Instantiate(prefab) : new GameObject("Planner");
                _planner = go.GetComponent<PlannerPanel>();
                if (_planner == null) _planner = go.AddComponent<PlannerPanel>();
                _planner.Build();
                _planner.PlanApplied += plan => Refresh();
            }
            _planner.SetVisible(true);
        }

        void LoadSon()
        {
            OpenPlanner();
            _planner.LoadSonsPlan();
        }

        void Preflight()
        {
            if (!_lifeSupport || !_comms || !_navigation || !_emergency)
            {
                MissionEvents.RaiseFail(MissionIds.M2, "Complete every system check and run the emergency radio cut.");
                return;
            }
            if (MissionManager.Instance != null && MissionManager.Instance.PlannedSuccess < .70f)
            {
                MissionEvents.RaiseFail(MissionIds.M2, "The plan is below the 70% preflight threshold. Add redundancy or improve reliability.");
                return;
            }
            MissionEvents.RaiseComplete(MissionIds.M2);
        }
    }
}
