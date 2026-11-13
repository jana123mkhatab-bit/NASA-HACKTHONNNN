# AI_LOG: which parts were AI-assisted

Needed for the NASA Space Apps AI-use disclosure. Add a row whenever AI writes or changes something. Person 6 turns this into the disclosure text.

| Date | Person | Area | Tool | What the AI did | Human check |
|---|---|---|---|---|---|
| 2026-11-13 | 1 (Core) | Assets/_Project/Core: MissionManager, MissionIds, MissionEvents, PlayerState, PlanConfig, RiskModel, Pip, FatherLog, HUD, Risk Gauge, Debrief/End/Fail/Title UI, StubMission | Claude | Wrote the code from the Prompt 1 brief | RiskModel tests, Unity play-through (pending) |
| 2026-11-13 | 1 (Core) | Assets/_Project/Core/Editor: setup menus (folders, scenes, prefabs, URP, post-processing) | Claude | Wrote the editor setup scripts | Run menus 0 to 5 in Unity (pending) |
| 2026-11-13 | 1 (Core) | Review pass: first-scene EventSystem, early OnMissionComplete, non-mission scenes, folder names, LFS rules | Claude | Fixed gaps found in review | Pending |
| (earlier) | Team | Mascot Pip (SourceArt/mascot_2.blend), space scene models and textures | Blender / scripts | See Art/CREDITS.md | Person 5 |

Reliabilities and mass costs in PlanConfig are game-balance values, not NASA data.
