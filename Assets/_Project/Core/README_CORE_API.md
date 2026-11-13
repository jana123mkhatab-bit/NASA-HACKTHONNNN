# Core API (Person 1): what everyone else needs

## Finish your mission
```csharp
using LastSignal;
MissionEvents.RaiseComplete("M1");          // opens the Debrief card, then loads the next scene
MissionEvents.RaiseFail("M1", "Hull broke"); // opens the Fail panel with Pip's hint; Retry reloads from the checkpoint
```
Only `MissionManager` loads scenes. Never call `SceneManager.LoadScene` yourself.

## State
`MissionManager.Instance.state` is the `PlayerState` (supplies, hull, power, fuel, heading, signalPathUp, plannedSuccess).
`MissionManager.Instance.SaveCheckpoint()` saves a mid-mission checkpoint.
`MissionManager.Instance.SetPlannedSuccess(p)` after the planner computes `RiskModel.ComputeSuccess(plan)`.

## Reusable widgets
- Risk Gauge: drop `Assets/_Project/UI/RiskGauge.prefab` on a Canvas, or `RiskGaugeWidget.Create(parent, pos, size)`, then `SetPlan(plan)` or `SetValue(0..1)`.
- Pip lines, father's log and debrief text are ScriptableObjects in `Core/Resources/LastSignal/`.

## Scenes
Open your own scene and press Play. `GameBootstrap` adds the manager, HUD and screens automatically.
Delete the `StubMission` object in your scene when the real mission works.

## Balance
Reliabilities and mass costs are GAME-BALANCE values, not NASA data.
