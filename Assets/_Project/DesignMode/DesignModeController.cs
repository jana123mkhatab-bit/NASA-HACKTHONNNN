using System.Collections;
using UnityEngine;

namespace LastSignal
{
    /// <summary>
    /// Design Mode (end of the game): the mission planner stays open so the player can design any mission,
    /// add redundancy and watch the success probability move (Pip's Design Mode intro says exactly this).
    /// It reuses PlannerPanel as described in Docs/PLANNER_DESIGN.md ("DM -. future reuse .-> P") and adds no new rules.
    /// Reliabilities and masses are GAME-BALANCE values, not NASA data.
    /// </summary>
    public class DesignModeController : MonoBehaviour
    {
        PlannerPanel _planner;

        void Start()
        {
            var stub = FindFirstObjectByType<StubMission>();
            if (stub != null) Destroy(stub.gameObject);

            var prefab = Resources.Load<GameObject>("LastSignal/PlannerPanel");
            var go = prefab != null ? Instantiate(prefab) : new GameObject("Planner");
            go.transform.SetParent(transform, false);
            _planner = go.GetComponent<PlannerPanel>();
            if (_planner == null) _planner = go.AddComponent<PlannerPanel>();
            _planner.Build();
            _planner.PlanApplied += OnPlanApplied;
            _planner.SetVisible(true);
        }

        void OnDestroy()
        {
            if (_planner != null) _planner.PlanApplied -= OnPlanApplied;
        }

        // PlannerPanel hides itself after Apply (M1 and M2 want that). In Design Mode the planner is the whole
        // screen, so show it again once the Apply click has finished.
        void OnPlanApplied(PlanConfig plan) => StartCoroutine(Reopen());

        IEnumerator Reopen()
        {
            yield return null;
            if (_planner != null) _planner.SetVisible(true);
        }
    }
}
