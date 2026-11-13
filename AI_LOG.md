# AI_LOG: which parts were AI-assisted

Needed for the NASA Space Apps AI-use disclosure. Add a row whenever AI writes or changes something. Person 6 turns this into the disclosure text.

| Date | Person | Area | Tool | What the AI did | Human check |
|---|---|---|---|---|---|
| 2026-11-13 | 1 (Core) | Assets/_Project/Core: MissionManager, MissionIds, MissionEvents, PlayerState, PlanConfig, RiskModel, Pip, FatherLog, HUD, Risk Gauge, Debrief/End/Fail/Title UI, StubMission | Claude | Wrote the code from the Prompt 1 brief | RiskModel tests, Unity play-through (pending) |
| 2026-11-13 | 1 (Core) | Assets/_Project/Core/Editor: setup menus (folders, scenes, prefabs, URP, post-processing) | Claude | Wrote the editor setup scripts | Run menus 0 to 5 in Unity (pending) |
| 2026-11-13 | 1 (Core) | Review pass: first-scene EventSystem, early OnMissionComplete, non-mission scenes, folder names, LFS rules | Claude | Fixed gaps found in review | Pending |
| 2026-11-13 | 3 (Flight, M3) | Assets/_Project/Shared/Ship: ShipController (a = F / m, PlayerState drain, M4 landing hooks), ShipInput, ShipMassModel, LaunchSequence, ChaseCamera, ShipTelemetryHud, ShipFactory, prefab menu, tests | Claude | Wrote the code, SHIP_INTERFACE.md and the design doc from the person 3 brief | Compiled outside Unity (0 errors), 5/5 unit tests pass; Unity play-through pending |
| 2026-11-13 | 3 (Flight, M3) | Assets/_Project/Missions/M3: M3Director, relay strike, unknown craft AI, tumble recovery, triage, scanner, reroute planner, Signal Path HUD + CSV parser, scene/debrief menus, tests, sample CSV | Claude | Wrote the code, wireframes and debrief draft text | Compiled outside Unity (0 errors), 14/14 unit tests pass; Unity play-through pending; person 6 to verify debrief sources |
| (earlier) | Team | Mascot Pip (SourceArt/mascot_2.blend), space scene models and textures | Blender / scripts | See Art/CREDITS.md | Person 5 |

Reliabilities and mass costs in PlanConfig are game-balance values, not NASA data.
