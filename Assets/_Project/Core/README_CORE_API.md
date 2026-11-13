# Core API (the shared contract)

Namespace `LastSignal.Core`. Only Person 1 edits `Assets/_Project/Core`. Ask before changing anything here.

## Mission flow
```csharp
MissionEvents.OnMissionStart("M1");               // optional: the manager already does this
MissionEvents.OnMissionComplete("M1");            // shows Debrief, then loads the next mission
MissionEvents.OnMissionFail("M1", "Hull broke");  // shows the fail panel, Pip speaks the hint
MissionManager.Checkpoint("after_launch");        // Retry returns here
MissionManager.Instance.RecordPlan("M2", plan);   // End card shows planned success
```
Ids: `MissionIds.M1, M2, M3, M4, Final, DesignMode` (Boot is the title). Scenes: Boot, M1_Ground, M2_Preflight, M3_DeepSpace, M4_Planet, Final, DesignMode.

## Player state
`PlayerState.Current` has supplies, hull, power, fuel (0 to 100), heading, signalPathUp, plannedSuccess. Use `SetFuel(...)` etc. so the HUD updates.

## Risk
```csharp
float p = RiskModel.ComputeSuccess(plan);   // product over subsystems of 1 - (1 - r)^n
```
Reliabilities and mass costs are game-balance values, not NASA figures.

## UI helpers
`Pip.Say("...")`, `Pip.SayKey("hint.M1")`, `FatherLog.Add("...")`, `HudShell.Instance.ShowGauge(true)`, `HudShell.Instance.Gauge.SetPlan(plan)`.
Build UI with `UIFactory` (code-built uGUI, no prefabs needed).

## Replacing a placeholder scene
Open the mission scene, delete the `StubMission` object, add your mission. Keep the scene name.
