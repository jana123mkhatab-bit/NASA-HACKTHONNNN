# AI_LOG: which parts were AI-assisted

Needed for the NASA Space Apps AI-use disclosure. Add a row whenever AI writes or changes something. Person 6 turns this into the disclosure text.

| Date | Person | Area | Tool | What the AI did | Human check |
|---|---|---|---|---|---|
| 2026-11-13 | 1 (Core) | Assets/_Project/Core: MissionManager, MissionIds, MissionEvents, PlayerState, PlanConfig, RiskModel, DebriefCard, Pip dialogue, Father's log, HUD, Risk Gauge, Debrief/Fail/End/Title UI, StubMission, GameBootstrap | Claude Code | Wrote the code from the Prompt 1 brief | RiskModel and flow tests; Unity play-through (pending) |
| 2026-11-13 | 1 (Core) | Core/Editor: setup menu (content, URP asset, post-processing, SSAO, materials, scenes, build settings) | Claude Code | Wrote the editor setup scripts | Run LastSignal > Setup > Run All (pending) |
| 2026-11-13 | 1 (Core) | Art/Shaders: Earth (day/night, ocean glint), Clouds, Atmosphere | Claude Code | Wrote the HLSL shaders | Visual check in Game view (pending) |
| 2026-11-13 | 1 (Core) | Pip lines, father's log text, debrief card text | Claude Code | Drafted placeholder story and NASA concept text | Person 6 must verify the NASA sources |
| (earlier) | Team | 3D model library (buildings, Earth, Moon, Sun, skybox) and NASA-style textures | Scripts (Node.js generators, Blender) | See Art/CREDITS.md | Person 5 |
| 2026-11-13 | 2 (Missions 1-2/Planner) | Assets/_Project/UI/Planner, Missions/M1, Missions/M2, Docs/PLANNER_DESIGN.md | Copilot SDK in VS Code | Drew the planner flow/wireframes and implemented the reusable planner plus M1/M2 mission interaction flows | Unity compile and play-through (pending) |
| 2026-11-13 | 2 (Missions 1-2/Planner) | Core/GameBootstrap, Core/Editor/SetupScenes | Copilot SDK in VS Code | Fixed repeat-play bootstrap/title initialization and added a scoped M1/M2 scene rebuild command using the imported celestial/building models | Unity editor play-through (pending) |
| 2026-11-13 | 4 (M4/Final) | Missions/M4 (scanner + CSV loader, site picker, landing controller, procedural planet world, materials, FX, camera rig, walker, son's log, reveal fallback, survey packet), Missions/Final (call home, walk, story end card), M4_DESIGN.md | Claude Code | Wrote the code, diagram, wireframes and fallback exoplanet CSV from the Prompt brief | Terrain slopes and landing fuel/battery checked with a Python port; Unity compile and play-through (pending) |
| 2026-11-13 | 4 (M4/Final) | Core/Resources/LastSignal/Debrief_M4 + Debrief_Final (text only) | Claude Code | Drafted the M4 and Final debrief text (Exoplanet Archive, Mars 2020 TRN, DSN) and the reveal/call-home dialogue | Person 6 must verify NASA sources; person 1 informed of the edit |
| 2026-11-13 | 4 (M4/Final) | Missions/M4, Missions/Final (audit pass) | Claude Code | Whole-project read-through; fixed in M4/Final: reveal staging (son could get stuck on the hull), Pip box over cutscenes and panels, wrong Pip lines after Retry, landing-ellipse rule + HUD warning, CSV BOM and Archive multi-row handling, shader fallback for builds | CSV loader and landing balance checked with Python ports; Unity compile and play-through still pending (no Unity editor on this machine) |

Reliabilities and mass costs in PlanConfig are game-balance values, not NASA data.
