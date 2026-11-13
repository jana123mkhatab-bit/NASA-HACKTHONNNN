# The Last Known Signal: setup (Person 1, Sprints S0 to S2)

## 1. Merge this package into the Unity project
Copy the three folders in this package into the project root, next to `Assets` and `ProjectSettings`:

- `Assets/_Project`
- `SourceArt` (the .blend files; Unity ignores it because it is outside `Assets`)
- `tools`

Nothing here touches `ProjectSettings` or `Packages`, and no existing file is overwritten.

## 2. Git LFS (once, before the first commit of art)
Run `tools\setup_git_lfs.bat` from the project root. It tracks fbx, obj, blend, jpg, png, audio and video files.
If the art is already committed without LFS, ask Person 1 before changing history.

## 3. Run the setup menus in Unity 6.6 (in this order)
Open the project and wait for the scripts to compile. Then:

0. **Last Signal > 0 - Install URP and set Unity 6 project settings (run first)**: sets Linear color space and installs the Universal RP package if it is missing. If it installs, wait for Unity to finish compiling.
0b. **Last Signal > 0b - Configure URP asset for maximum visuals**: HDR, 4x MSAA, 4096 shadow map, 4 cascades, soft shadows, HDR color grading and a 64-bit HDR buffer. If the project has no URP asset yet, it creates one and assigns it to every quality level. (This menu appears only after URP is installed.)
1. **Last Signal > 1 - Create folders, scenes and core assets**
2. **Last Signal > 2 - Build space prefabs (Earth, Moon, Sun, skybox)**
3. **Last Signal > 3 - Build mascot prefab (Pip)**
4. **Last Signal > 4 - Add post-processing look to all scenes**: ACES tonemapping, bloom, vignette and colour grading in a global Volume, plus SMAA, HDR cameras and soft sun shadows in every scene.
5. **Last Signal > Check setup** (prints what is missing)

If your project was made from the Unity 6 "Universal 3D" template, URP is already there and menu 0 only fixes the color space.
Every menu is safe to run again. Existing scenes and assets are not overwritten. If you ran menu 2 or 3 before URP was set up, run it again: it switches the materials to the URP Lit shader.
View post-processing in the Game view, not the Scene view.

## 4. Try it
Open `Assets/_Project/Core/Scenes/Boot.unity` and press Play.
Start leads to M1. Every mission scene is a placeholder with three buttons: Set checkpoint, Fail mission, Complete mission.
The flow is Boot, M1, M2, M3, M4, Final (End card), Design Mode.
You can also press Play in any mission scene on its own; the manager is created automatically.

## 5. Run the tests
Window > General > Test Runner > EditMode > Run All. The RiskModel tests check 9.9% (son's plan) and 71.4% (father's plan).

## What you should see
- Boot: title screen, Pip's greeting at the bottom, the mascot on the right, DEV jump buttons (editor only).
- M1: top HUD with four bars, a "Log" button, Pip's M1 hint.
- Set checkpoint lowers Fuel by 10. Fail shows the fail panel; Retry restores the checkpoint (fuel goes back up).
- Complete shows the Debrief card (placeholder text marked TODO); Continue loads the next mission.
- Final complete shows the End card with planned success and retries; the button opens Design Mode.
- Design Mode shows the Risk Gauge slot.

## Not verified yet
This code was written without Unity available, so it has not been compiled or run. Expect to fix small compile errors on first open and report them to Person 1.
The URP setting names in menu 0b can differ between Unity 6 versions. Any setting it cannot find is reported in the Console as skipped; set those by hand on the URP asset.

## Not done (needs the rest of the team)
- S3: merging the real mission scenes from Persons 2 to 6.
- S4: final build.
