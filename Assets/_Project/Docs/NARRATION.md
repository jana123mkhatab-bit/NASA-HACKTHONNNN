# NARRATION: The Last Known Signal

Script for voice-over and subtitles. Nothing here is wired into code yet: each row names the event that should play it.

**Rules used**
- Every line has at most 18 words. `seconds` is the on-screen or spoken time, including a short pause.
- A line that states a fact names its NASA source in the text itself. Facts come only from the project's own CSVs and debrief cards.
- Game-balance values (reliabilities, masses, costs) are never spoken as NASA facts. Lines say "in this game" where one is used.
- Data is a snapshot (retrieved 2026-11-13/14), so no line says "live" or "right now".
- All speakers are adults. Nobody under 18 appears or speaks. The son is an adult crew member.
- Speakers: PIP (nervous toy robot), FATHER (father's log), MISSION CONTROL, SON, NARRATOR.
- Lines marked *existing* reuse text already in `PipLines.asset`, `FatherLog.asset` or `FinalCallHome.cs`.

## 1. Cinematic opening (38 s)

Trigger for all: game start, before the title (`Core/OpeningIntro.cs`). One line after another; any key skips; title button INTRO: ON/OFF turns it off. Visuals: pull back from the base building, cut to Earth, Moon and Sun (cinematic, not to scale), relay R-7 pulses then goes silent at open_07, fade to the title for open_08.

| id | speaker | text | seconds | trigger |
|---|---|---|---|---|
| open_01 | NARRATOR | Somewhere past Mars, a ship stopped answering. | 4 | Opening begins, black screen, stars fade in (father's log Day 9: "last heading points past Mars") |
| open_02 | NARRATOR | Then a capsule fell back to Earth. | 4 | Camera pans to Earth from orbit |
| open_03 | FATHER | They found the capsule. I keep thinking he is alive. | 5 | Capsule on the ground; *existing* father's log, Day 1 |
| open_04 | NARRATOR | The ship was his son's. So was the plan. | 5 | Cut to the launch-pad blueprint |
| open_05 | NARRATOR | Nobody checked it. Ten percent. That was his chance. | 5 | Blueprint shows five red subsystems (10% is a game-balance value, see M2) |
| open_06 | PIP | I am Pip. I keep the count so you do not have to. | 5 | Pip appears; *existing* M1 intro line 1 |
| open_07 | NARRATOR | One last known signal. One father with a second chance. | 5 | Signal pulse crosses the star field |
| open_08 | NARRATOR | Fix the plan. Fly the ship. Bring him home. | 5 | Title card "The Last Known Signal" |

## 2. Mission intros and outros

Intro trigger: `MissionEvents.OnMissionStart(id)` after the scene fades in. Outro trigger: `OnMissionComplete(id)` as the debrief card opens.

### M1: What Fell to Earth

| id | speaker | text | seconds | trigger |
|---|---|---|---|---|
| m1_intro_01 | PIP | Build a test frame on the sled. Pick an engine and only the payload this test needs. | 6 | M1 start; *existing* intro line 2, shortened |
| m1_intro_02 | PIP | Lock the clamps, then fire. Watch a = F / m. | 4 | After the first part is placed; *existing* |
| m1_outro_01 | PIP | Heavier frame, slower push: acceleration is thrust over mass. NASA Glenn, Beginner's Guide to Rockets. | 7 | M1 complete, debrief opens |

### M2: Ten Percent

| id | speaker | text | seconds | trigger |
|---|---|---|---|---|
| m2_intro_01 | PIP | Your son's flight assessment is in the records. It was never in his briefing. | 6 | M2 start; *existing* intro line 1 |
| m2_intro_02 | PIP | Open it, then fix the plan in the Mission Planner. | 4 | Assessment opened; *existing*, shortened |
| m2_outro_01 | PIP | In this game, five 63% parts give 10%. Backups raise odds, cost mass. NASA Systems Engineering Handbook. | 8 | M2 complete, debrief opens |
| m2_outro_02 | FATHER | I fixed his plan. Ten percent became seventy. He would have laughed. | 5 | After the debrief; *existing* father's log, Day 20 |

### M3: The Ship That Came Out of Nowhere

| id | speaker | text | seconds | trigger |
|---|---|---|---|---|
| m3_intro_01 | PIP | A relay satellite is in your path. It will be destroyed. | 4 | M3 start; *existing* intro line 1 |
| m3_intro_02 | PIP | Something is coming up behind us. It is not on any chart. | 5 | Unknown craft enters radar; *existing* intro line 2 |
| m3_outro_01 | PIP | One relay is a single point of failure. Moon radio takes 1.4 seconds one way. NASA JPL Horizons. | 8 | M3 complete, debrief opens |
| m3_outro_02 | FATHER | A ship came out of nowhere and took half my supplies. It did not answer. | 6 | After the debrief; *existing* father's log, Day 41 |

### M4: Someone's Been Living Here

| id | speaker | text | seconds | trigger |
|---|---|---|---|---|
| m4_intro_01 | PIP | The scanner found a planet with water. | 3 | M4 start; *existing* intro line 1 |
| m4_intro_02 | PIP | Pick your landing site with care. Slope, rocks and ice all matter. | 5 | Site picker opens; *existing* intro line 2 |
| m4_outro_01 | PIP | The planet uses NASA Exoplanet Archive values: radius, starlight and temperature. | 6 | M4 complete, debrief opens |
| m4_outro_02 | FATHER | The water collector is his. The footprints are fresh. | 5 | After the debrief; *existing* father's log, Day 63 |

## 3. Challenge sites (M3 flight sector)

Trigger: ship enters the site radius and `ChallengeSiteComponent` shows the NASA DATA card. Site order is shuffled by seed, so A to D name the four kinds, not fixed places. The card shows the real CSV values for the row picked; these lines do not repeat numbers, except the solar constant in D.

| id | speaker | text | seconds | trigger |
|---|---|---|---|---|
| site_a_solar | PIP | Solar storm. NASA's DONKI database logs flares and CMEs so crews get warning. Shield or divert? | 7 | Enter site A (`ChallengeKind.SolarStorm`, space_weather_donki CSV) |
| site_b_asteroid | PIP | Asteroid close pass. JPL close-approach data lets NASA track near-Earth objects. Dodge or hold? | 7 | Enter site B (`ChallengeKind.Asteroid`, neo_close_approaches CSV) |
| site_c_blackout | PIP | Comms blackout. Radio travels at light speed; JPL Horizons gives the delay. Wait, or act alone? | 7 | Enter site C (`ChallengeKind.Blackout`, horizons CSV) |
| site_d_power | PIP | Power shortfall. Sunlight fades with distance squared. NASA's Earth fact sheet: 1361 watts per square metre. | 8 | Enter site D (`ChallengeKind.Power`, solar_flux_by_distance CSV) |

Source pages: DONKI (ccmc.gsfc.nasa.gov/DONKI), JPL CAD API (ssd-api.jpl.nasa.gov/cad.api), JPL Horizons (ssd.jpl.nasa.gov/horizons), NASA Earth Fact Sheet (nssdc.gsfc.nasa.gov/planetary/factsheet/earthfact.html). Costs on the choice buttons stay tagged [game balance].

## 4. Finale: Call Home

Trigger: `FinalCallHome` after the M4 debrief. Lines marked *existing* are in `FinalCallHome.cs` or `PipLines.asset` and are kept word for word.

| id | speaker | text | seconds | trigger |
|---|---|---|---|---|
| final_01 | PIP | The survey is ready. Home is listening. | 3 | Final start; *existing* |
| final_02 | MISSION CONTROL | Deep Space Network. Link established. | 3 | Dish swings to Earth; *existing* |
| final_03 | MISSION CONTROL | Survey received. Every byte of it. | 3 | *existing* |
| final_04 | MISSION CONTROL | Coordinates, water readings... and a second flight log. | 4 | *existing* |
| final_05 | FATHER | He's here. He's alive. | 3 | *existing* |
| final_06 | MISSION CONTROL | ...Copy that. Two to bring home. | 3 | *existing* |
| final_07 | SON | Tell Mom I kept the water running. | 3 | *existing* |
| final_08 | MISSION CONTROL | The next ship is already on the build schedule. Your plan is in it. | 4 | *existing*; leads into Design Mode |
| final_09 | NARRATOR | Three NASA Deep Space Network sites keep contact as Earth turns. Source: NASA DSN, JPL. | 6 | Credits begin, camera pulls back |
| final_10 | PIP | Now it is your turn. Design any mission you like. | 4 | End card; *existing* Design Mode intro |

## Source check for a person (before recording)

- The M1 and M2 debrief cards still carry "[person 6: confirm exact page / section]". Open the NASA Glenn and Systems Engineering Handbook pages and confirm before recording m1_outro_01 and m2_outro_01.
- `Debrief_M3.asset` is still a placeholder. The 1.4 s figure in m3_outro_01 comes from horizons_positions_2026-11-13.csv (Moon light time).
- site_d_power: 1361 W/m2 is the NASA Earth Fact Sheet solar irradiance, recorded in solar_flux_by_distance.csv.
- final_09: three DSN complexes come from dsn_notes.csv (https://deepspace.jpl.nasa.gov/dsn/).

## Wiring and TTS import checklist (NarrationPlayer)

Code: `Core/NarrationPlayer.cs`. Script and subtitle text: `Core/Resources/LastSignal/Narration/narration_script.csv` (id, speaker, text, seconds). PIP rows that the Pip box or finale already shows use that exact text, so the voice matches the screen.

1. **File name** = the `id` column, lowercase, exactly: `open_01.wav`, `m3_pip_intercept.wav`, `site_a_solar.wav`, `final_09.wav`. No spaces, no extra suffix.
2. **Folder**: `Assets/_Project/Core/Resources/LastSignal/Narration/` (next to the CSV). Anywhere else it will not load.
3. **Format**: WAV, PCM 16-bit, mono, 44.1 kHz (48 kHz also imports fine; do not use 8-bit or MP3 sources). Normalise to about -3 dBFS peak, trim silence to 0.1-0.2 s at both ends.
4. **Unity import settings** per clip: Load Type = Compressed In Memory, Compression = Vorbis, Quality about 70, Force To Mono on, Preload Audio Data on. (Short lines, so memory is small.)
5. **Length**: keep each clip near the `seconds` column; the subtitle stays up for the clip length plus 0.3 s.
6. **Voices**: one consistent voice per speaker (PIP, FATHER, MISSION CONTROL, SON, NARRATOR). All adult voices; nobody under 18, no cloned real person. Check the TTS licence allows use in a competition submission.
7. **Say the text as written**; do not add words. If you change a line, edit the CSV `text` too (and the Pip box text, where the row is a PIP row), or the audio will stop matching.
8. **Check**: start the game: the opening plays before the title. Press VOICE on the title to set the level. Delete a wav to confirm the subtitle-only fallback.
9. **Not wired to audio** (no row yet): the many M1 and M2 Pip hints written in code, M4 reveal lines. Add a row with the exact text and a wav to cover them.
