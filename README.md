# THE LAST HORIZON — Unity project (NASA Space Apps 2026)

A free-flight space sandbox with real orbital mechanics: fly the futuristic delta-wing ship **Aurora X-1** between the Sun, Earth and Moon,
descend into the atmosphere, land at a spaceport, step out as an astronaut, and walk into Mission Control. No missions — just the world.

## Run it (3 steps)
1. Install **Unity 6 (6000.0 LTS)**.
2. Open this folder as a project in Unity Hub. Wait for the package import (URP + Input System).
3. Menu **Tools > Last Horizon > Setup Project** (runs automatically the first time), open `Assets/Scenes/Main.unity`, press **Play**.
If the Input System asks to enable the new backend, accept (both old and new input are supported).

Everything (planets, ship, UI, audio, post-processing) is created from code at startup — the scene only needs a camera.

## Controls
| Key | Action |
|---|---|
| Mouse | Pitch / yaw (cursor captured; ESC pauses) |
| W S / A D / Space Ctrl | Thrust fwd-back / strafe / up-down |
| Q E | Roll |
| Shift | Boost (ship) / sprint (astronaut) |
| X | Full stop (flight assist) |
| Z | Flight assist <-> Newtonian (real gravity & momentum) |
| C | Supercruise (far from planets) — W/S change speed |
| , . | Time warp down/up (x1 … x2500) |
| M | System map |
| V | Camera chase / cockpit / far (astronaut: 1st/3rd person) |
| Right mouse | Free look |
| G / L | Landing gear / lights |
| E | Disembark, board ship, enter/leave Mission Control |
| F / Space | Astronaut flashlight / jetpack |
| H or F1 | Help card |

## What is in the world
* **Sun, Earth, Moon** with real NASA textures. Earth orbits the Sun, the Moon orbits Earth (Kepler orbits, eccentricity, inclination, axial tilt, day/night, tidally locked Moon).
  Real-time scale: 1 simulated day = 120 s; the HUD shows the date and time-warp.
* **Gravity** from all three bodies (Newtonian mode), reference-frame velocities, orbit Pe/Ap readout, re-entry and solar heating.
* **Landing**: descend below ~190 km (Earth) / ~130 km (Moon) and the ship transitions to a surface world — Cape Spaceport (Florida), generic land, ocean and ice terrain, or the lunar surface (1.62 m/s² gravity, black sky, Earth in the sky).
  Fly the ship low, land on the pad, press **E** to get out. Climb past 1500 m to return to orbit.
* **Mission Control**: walk through the glowing arch at the spaceport to enter a procedural interior with a live Earth screen and holo-globe.
* **Sun**: not landable — get too close and the ship burns up (it respawns in Earth orbit).

## Honest notes
* Distances are compressed (1 unit = 63.7 km, Earth radius 100 units) so the whole system is explorable in minutes.
* Textures are the supplied 2K maps upscaled to 4K with fine detail added. For truly crisp planets, drop NASA Blue Marble / LRO 8K maps into `Assets/Resources/Textures` using the same file names (`earth_daymap.jpg`, `moon.jpg`, …).
* The code was written without access to the Unity editor. If the console shows a compile error after import, it will be a small API mismatch — send it over and it is a one-line fix.

## Layout
`Assets/Scripts` gameplay · `Assets/Shaders` custom URP shaders · `Assets/Resources/Models` OBJ library + generated ship/astronaut · `Tools/` Python generators for the ship, astronaut and textures.
