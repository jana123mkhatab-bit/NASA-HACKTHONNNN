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
| 2026-11-13 | 1 + 2 (visual pass) | Core: Palette (art bible), UISkin, UIFactory, UIButtonFx, Sfx (synthesised placeholder sound), ScreenFader, CameraRig, LightFlicker, restyled HUD / gauge / Pip / title / debrief / fail / end / log screens | Claude Code | Rewrote the presentation layer for a consistent sci-fi look; split SlowSpin and CameraOrbit into their own files | Play-mode capture test (CaptureFlowTests) and manual review of frames |
| 2026-11-13 | 1 + 2 (visual pass) | Core/Editor: StageKit, StageM1 (test range), StageM2 (launch pad), SetupVisuals palette materials + dusk sky, CaptureShots | Claude Code | Built the M1 and M2 3D sets from code | Rendered shots reviewed |
| 2026-11-13 | 2 (Missions 1-2/Planner) | Missions/M1 (M1Parts, M1GroundMission), Missions/M2, UI/Planner/PlannerPanel, Docs/PLANNER_DESIGN.md | Claude Code | Rewrote the M1 sled test, M2 preflight and the Planner trade-off (grade + redundancy vs mass budget) | CaptureFlow test asserts the son's plan reads 9-11%; full play-through by a person still needed |
| 2026-11-13 | 1 | SetupContent debrief and Pip text for M1 and M2 | Claude Code | Rewrote card text to match the design doc titles | Person 6 must confirm the exact NASA source pages |

Fonts: Rajdhani and Share Tech Mono (Google Fonts, SIL Open Font License 1.1); licence files sit next to the fonts in Core/Resources/LastSignal/Fonts.

Reliabilities and mass costs in PlanConfig are game-balance values, not NASA data.
