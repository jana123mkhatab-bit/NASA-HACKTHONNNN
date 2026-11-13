# The Last Known Signal (Unity 6, URP)

NASA Space Apps Challenge Cairo 2026, target challenge: **Space Mission Design Game**.
Every mission follows PLAN (Mission Planner with a live Success Probability gauge) -> RUN -> DEBRIEF (a card naming the real NASA concept and source).

Story (year 2590): a survey pilot vanishes on a risky flight and his father joins the build team of NASA's next ship.
M1 What Fell to Earth, M2 Ten Percent, M3 The Ship That Came Out of Nowhere, M4 Someone's Been Living Here, then the call home and free-play Mission Design Mode.

## Start here
1. Open the project in Unity (6000.3 or newer, URP). Wait for scripts to compile.
2. Follow `README_SETUP.md`: run the `Last Signal` menus 0, 0b, 1, 2, 3, 4, then `Check setup`.
3. Open `Assets/_Project/Core/Scenes/Boot.unity` and press Play.
4. `Window > General > Test Runner > EditMode > Run All` (RiskModel: 9.9% and 71.4%).

Team contract and Core API: `Assets/_Project/Core/README_CORE_API.md`. AI use is logged in `AI_LOG.md`.
Git LFS: run `tools\setup_git_lfs.bat` once.

## Legacy sandbox
The earlier "Last Horizon" free-flight sandbox (`Assets/Scenes/Main.unity`, `Assets/Scripts`) is kept untouched for reuse of its models and shaders.
It is not part of the mission flow, and the mission UI stays hidden in that scene.
