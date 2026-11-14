# Data: every NASA number in the game

All files are in `Resources/LastSignal/` so code can load them with `DataTable.Load("<name>")`.
Every row has a `source_url` and `retrieved` column. Retrieved 2026-11-13. Values marked GAME-BALANCE are not NASA data.

| File | What | Source | Used by |
|---|---|---|---|
| parts_library.csv | SLS Block 1/1B/2 mass, thrust, payload; RS-25 and RL10 thrust; Voyager 1 mass and RTG power. Original unit kept, SI value derived (lb x 0.45359237, lbf x 4.448222) | https://www.nasa.gov/reference/space-launch-system/ and https://science.nasa.gov/mission/voyager/voyager-1/ | Design Mode build screen (reference line); persons 2, 3 parts text |
| horizons_positions_2026-11-13.csv | Sun-centred ecliptic J2000 state vectors of Earth, Moon, Mars, Jupiter at 2026-11-13 00:00 TDB, plus distance and light time from Earth | JPL Horizons API https://ssd.jpl.nasa.gov/api/horizons.api (exact query in `source_url` column) | destinations.csv, M3 comms delay, debrief |
| space_weather_donki_2026-10-01_to_2026-11-10.csv | 3 strongest solar flares (M6.7, M6.4, M3.5) and 3 fastest CMEs (1048, 974, 973 km/s) from the archived DONKI list, 1 Oct to 10 Nov 2026, with a why-NASA-cares line | NASA DONKI https://ccmc.gsfc.nasa.gov/DONKI/ (query URLs in file; the api.nasa.gov/DONKI/FLR gateway returned 404 on 2026-11-14, CME worked there, so the official CCMC host was used for both) | Space-weather events (snapshot) |
| neo_close_approaches_2026-11-13.csv | 5 nearest close approaches 13 Nov 2026 to 31 Jan 2027 (date, nominal distance, relative speed, H) | JPL SBDB Close Approach Data API https://ssd-api.jpl.nasa.gov/cad.api (query in file) | Asteroid flyby events |
| solar_flux_by_distance.csv | Sunlight power per m2 at Earth, Moon, Mars, Jupiter on 2026-11-13 (DERIVED) | S0 from https://nssdc.gsfc.nasa.gov/planetary/factsheet/earthfact.html, distances from horizons_positions_2026-11-13.csv | Power/solar-panel trade-offs |
| exoplanet_trappist1e.csv | TRAPPIST-1 e: radius 0.92 Earth, insolation 0.646 Earth, equilibrium temperature 249.7 K, period 6.101 d, distance 12.43 pc | NASA Exoplanet Archive TAP, table `pscomppars` (query in file) https://exoplanetarchive.ipac.caltech.edu | M4 habitability card, Design Mode |
| dsn_notes.csv | Facts for the comms panel: 3 complexes, 70 m / 34 m dishes, live data every 5 s, Doppler tracking, Voyager 1 one light-day | https://deepspace.jpl.nasa.gov/dsn/ , https://www.nasa.gov/directorates/somd/space-communications-navigation-program/what-is-the-deep-space-network/ | Comms panel, debrief |
| destinations.csv | Design Mode destinations. Distance and light time come from the Horizons / Exoplanet rows named in `distance_source_row`; `mass_budget_t` and `mission_days` are GAME-BALANCE | as above | DesignModeController |

## Derived values
- Light time to TRAPPIST-1 e: 12.43 pc x 3.26156 ly/pc = 40.5 years, shown as years.
- Moon distance is Earth-to-Moon at that instant (405,339 km), not the mean distance.

- Solar flux (DERIVED, not measured): S = S0 / d^2 (inverse-square law). S0 = 1361.0 W/m2 at 1 AU (NASA Earth fact sheet), d = `dist_from_sun_au` from the Horizons CSV. Results: Earth 1389.3, Moon 1395.0, Mars 527.1, Jupiter 48.1 W/m2. The fact sheets list orbit-mean values (Mars 586.2, Jupiter 50.26) which differ because Mars and Jupiter are not at their mean distance on 2026-11-13; both are in the CSV.
- Asteroid distance in km = AU x 149,597,870.7 (derived). The distance is the nominal value; JPL also gives min/max uncertainty.
- Light time and DSN facts: unchanged, see horizons_positions (light_time_from_earth_s: Mars 665.1 s, Jupiter 2652.0 s) and dsn_notes.csv.
- All three new files are a snapshot retrieved 2026-11-14, never live. The "why NASA cares" lines are plain-language summaries, not NASA quotes.

## Not NASA data
Reliabilities (0.63 / 0.90), subsystem masses, mass budgets, supply days and mission days are game balance.
The Design Mode screens label them.

## Still to verify by a person (S3)
Open each `source_url` in a logged-out browser and confirm the number matches. The SLS page was read on 2026-11-13 by the assistant; a human must repeat that check.

## Added 2026-11-14 (NASA backing pass)
- `data_sources.csv`: registry of every dataset (file, source_url, retrieved, kind NASA or DERIVED, used_by, why_nasa_cares). It feeds the NASA DATA tag, the Data & Sources screen and the end card.
- `m3_signal_path_2026-11-13.csv`: M3 signal path. relay_earth = Moon row of the Horizons file (405,339 km, 1.4 s). ship_relay = DERIVED distance between the Mars and Moon rows (199,648,559 km); light time = km / 299,792.458. Mars and Moon are story proxies for ship and relay. This replaces the old SAMPLE file, which M3 had been showing.
