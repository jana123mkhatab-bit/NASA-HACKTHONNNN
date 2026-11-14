# The Last Known Signal

NASA Space Apps Challenge 2026, Cairo. Team **Elly**. Challenge: **Space Mission Design Game**.

A Unity game that teaches how real space missions are designed. A relay satellite has gone silent; the player designs and flies the rescue, trading ship mass, redundancy, power and mission days against real launch-vehicle limits, and meets challenges built from real NASA data (solar flares and CMEs, near-Earth asteroids, signal delay, sunlight at distance, the exoplanet TRAPPIST-1 e). Every debrief names its NASA source.

## How to open the working prototype

The game is a ready-to-run Windows build, included in this repository in the [`TheLastKnownSignal_Windows/`](TheLastKnownSignal_Windows/) folder (about 128 MB). Get it either way:

- **Zip download (easiest):** [ADD PUBLIC DOWNLOAD LINK]
- **From this repository:** click **Code > Download ZIP** on GitHub, or `git clone` the repository, then open the `TheLastKnownSignal_Windows` folder.

1. **Unzip the whole download first** (do not run the .exe from inside the zip).
2. Open the `TheLastKnownSignal_Windows` folder and double-click **`The Last Known Signal.exe`** (Windows PC, no install, no login, no account).
3. Press any key to skip the opening scene. Start with the title screen; **DATA & SOURCES** lists every NASA dataset used.

Do not delete the other files and folders next to the .exe (`The Last Known Signal_Data`, `MonoBleedingEdge`, `UnityPlayer.dll`, and so on); the game needs them.

### Controls (flight, mission 3)

| Key | Action |
|---|---|
| W / S | Thrust |
| A / D | Yaw |
| Up / Down | Pitch |
| Q / E | Roll |
| Space / Ctrl | Up / down |
| X | Stabilize |
| B | Brake |
| Shift | Boost |
| F | Fire |
| L | Open the log |

The Planner and mission screens use the mouse.

## Working features, limitations, future plans

- **Working:** story missions M1 to M4 and a final scene, the Planner with live resource box and tutorial, Design Mode with a launch-vehicle lift check, M3 free flight with four NASA-data challenge sites, narration with subtitles, a DATA & SOURCES screen, and 87 automated EditMode tests (86 pass, 1 explicitly skipped).
- **Limitations:** NASA data is a snapshot from 2026-11-13 and 2026-11-14, not live. Reliabilities, masses, budgets, supply days, mission days and event costs are game-balance values, not NASA data, and are labelled on screen. Windows only. A full human play-through and a human re-check of every NASA source link were still pending at submission.
- **Future plans (not built):** live NASA data feeds, more missions and destinations, human-voiced narration, other platforms.

## NASA data

Every dataset is a CSV in `Assets/_Project/Data/Resources/LastSignal/` with a source link and retrieval date; see `Assets/_Project/Data/README.md` and `data_sources.csv`. Sources: JPL Horizons, NASA Exoplanet Archive, NASA DONKI, JPL Close-Approach Data API, NASA Deep Space Network, NASA Space Launch System page, NASA Voyager 1 page, NSSDC planetary fact sheets.

## AI use

AI tools were used to help build the project (Claude Code, GitHub Copilot SDK in VS Code, Piper text-to-speech for the narration audio). Every AI-assisted part is listed in [`AI_LOG.md`](AI_LOG.md).

## Open the source in Unity

Unity 6000.6.9f1 with the Universal Render Pipeline. Open the repository folder in Unity Hub. Opening the source is only needed to edit the game; judges do not need it.
