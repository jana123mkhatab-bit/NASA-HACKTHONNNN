# Data: every NASA number in the game

All files are in `Resources/LastSignal/` so code can load them with `DataTable.Load("<name>")`.
Every row has a `source_url` and `retrieved` column. Retrieved 2026-11-13. Values marked GAME-BALANCE are not NASA data.

| File | What | Source | Used by |
|---|---|---|---|
| parts_library.csv | SLS Block 1/1B/2 mass, thrust, payload; RS-25 and RL10 thrust; Voyager 1 mass and RTG power. Original unit kept, SI value derived (lb x 0.45359237, lbf x 4.448222) | https://www.nasa.gov/reference/space-launch-system/ and https://science.nasa.gov/mission/voyager/voyager-1/ | Design Mode build screen (reference line); persons 2, 3 parts text |
| horizons_positions_2026-11-13.csv | Sun-centred ecliptic J2000 state vectors of Earth, Moon, Mars, Jupiter at 2026-11-13 00:00 TDB, plus distance and light time from Earth | JPL Horizons API https://ssd.jpl.nasa.gov/api/horizons.api (exact query in `source_url` column) | destinations.csv, M3 comms delay, debrief |
| exoplanet_trappist1e.csv | TRAPPIST-1 e: radius 0.92 Earth, insolation 0.646 Earth, equilibrium temperature 249.7 K, period 6.101 d, distance 12.43 pc | NASA Exoplanet Archive TAP, table `pscomppars` (query in file) https://exoplanetarchive.ipac.caltech.edu | M4 habitability card, Design Mode |
| dsn_notes.csv | Facts for the comms panel: 3 complexes, 70 m / 34 m dishes, live data every 5 s, Doppler tracking, Voyager 1 one light-day | https://deepspace.jpl.nasa.gov/dsn/ , https://www.nasa.gov/directorates/somd/space-communications-navigation-program/what-is-the-deep-space-network/ | Comms panel, debrief |
| destinations.csv | Design Mode destinations. Distance and light time come from the Horizons / Exoplanet rows named in `distance_source_row`; `mass_budget_t` and `mission_days` are GAME-BALANCE | as above | DesignModeController |

## Derived values
- Light time to TRAPPIST-1 e: 12.43 pc x 3.26156 ly/pc = 40.5 years, shown as years.
- Moon distance is Earth-to-Moon at that instant (405,339 km), not the mean distance.

## Not NASA data
Reliabilities (0.63 / 0.90), subsystem masses, mass budgets, supply days and mission days are game balance.
The Design Mode screens label them.

## Still to verify by a person (S3)
Open each `source_url` in a logged-out browser and confirm the number matches. The SLS page was read on 2026-11-13 by the assistant; a human must repeat that check.
