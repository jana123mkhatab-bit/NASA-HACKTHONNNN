using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace LastSignal
{
    /// <summary>
    /// Mission Design Mode (free play after the story). Flow: Destination -> Build (Planner + supplies) -> Flight -> Debrief.
    /// RiskModel is the single source of odds: each checkpoint rolls one subsystem with p = 1-(1-r)^n, so the
    /// chance of passing every roll equals RiskModel.ComputeSuccess(plan) (supplies are a separate, deterministic check).
    /// Distances and light times come from Data/Resources/LastSignal CSVs (NASA JPL Horizons / Exoplanet Archive).
    /// Masses, budgets, reliabilities and mission days are GAME-BALANCE values and are labelled so on screen.
    /// </summary>
    public class DesignModeController : MonoBehaviour
    {
        const int SupplyStep = 5, SupplyMax = 120;

        DataTable _dest, _exo, _parts;
        Canvas _canvas;
        RectTransform _win;
        PlannerPanel _planner;
        Dictionary<string, string> _pick;
        PlanConfig _plan;
        int _supplies = 30;
        readonly List<string> _log = new List<string>();
        bool _ok;
        Text _supText, _planText, _vehText;
        List<LaunchCheck.Vehicle> _vehicles = new List<LaunchCheck.Vehicle>();
        int _veh;
        Button _launch;

        void Start()
        {
            _dest = DataTable.Load("destinations");
            _exo = DataTable.Load("exoplanet_trappist1e");
            _parts = DataTable.Load("parts_library");
            _canvas = UIFactory.CreateCanvas("DesignMode", 20);
            _canvas.transform.SetParent(transform, false);
            var pgo = new GameObject("Planner");
            pgo.transform.SetParent(transform, false);
            _planner = pgo.AddComponent<PlannerPanel>();
            _planner.Build();
            _planner.SetVisible(false);
            _planner.PlanApplied += p => { _plan = p; RefreshBuild(); };
            ShowDestinations();
        }

        // ---------------------------------------------------------------- helpers
        RectTransform NewWindow(string title, string kicker)
        {
            if (_win != null) Destroy(_win.gameObject);
            _win = UIFactory.Window("Win", _canvas.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-560, -470), new Vector2(560, 470), title, kicker);
            return _win;
        }

        // Top-left based layout: x0,y0 = top-left, x1,y1 = bottom-right, y measured down from the parent's top.
        static Text L(Transform p, string text, int size, float x0, float y0, float x1, float y1, Color? c = null, TextAnchor a = TextAnchor.UpperLeft) =>
            UIFactory.Label("L", p, text, size, a, new Vector2(0, 1), new Vector2(0, 1), new Vector2(x0, -y1), new Vector2(x1, -y0), c);

        static Button B(Transform p, string text, float x0, float y0, float x1, float y1, UnityEngine.Events.UnityAction a, ButtonStyle s = ButtonStyle.Primary) =>
            UIFactory.Button("B", p, text, new Vector2(0, 1), new Vector2(0, 1), new Vector2(x0, -y1), new Vector2(x1, -y0), a, s);

        static Image Panel(Transform p, float x0, float y0, float x1, float y1)
        {
            var i = UIFactory.Round("Box", p, new Vector2(0, 1), new Vector2(0, 1), new Vector2(x0, -y1), new Vector2(x1, -y0), Palette.Inset, 8);
            i.raycastTarget = false;
            return i;
        }

        static string Dist(float km) => km >= 1e9f ? (km / 1.495978707e8f).ToString("0.00") + " AU" : km.ToString("N0") + " km";
        static string Light(float s)
        {
            if (s < 120f) return s.ToString("0.0") + " s";
            if (s < 7200f) return (s / 60f).ToString("0.0") + " min";
            return (s / 3.15576e7f).ToString("0.0") + " years";
        }
        static float Budget(Dictionary<string, string> d) => DataTable.F(d, "mass_budget_t");
        static float Days(Dictionary<string, string> d) => DataTable.F(d, "mission_days");

        // ---------------------------------------------------------------- 1. destination
        void ShowDestinations()
        {
            var w = NewWindow("MISSION DESIGN MODE", "STEP 1 OF 3  ·  CHOOSE A DESTINATION");
            L(w, "Pick where to send your ship. Distance and signal delay are NASA data; mass budget and mission days are game-balance values.",
                20, 28, 62, 1090, 114, Palette.TextDim);
            float y = 118;
            foreach (var d in _dest.Rows)
            {
                var row = d;
                string file = NasaData.FileFromRef(row["distance_source_row"]);
                if (Resources.Load<TextAsset>("LastSignal/" + file.Replace(".csv", "")) == null) NasaData.ReportFallback(file, "Design Mode destination file missing");
                var bg = Panel(w, 24, y, 1096, y + 196);
                L(bg.transform, row["name"], 30, 18, 6, 330, 44).Bold();
                L(bg.transform, "Distance " + Dist(DataTable.F(row, "distance_from_earth_km")) + "\nSignal delay (one way) " + Light(DataTable.F(row, "one_way_light_time_s")), 17, 18, 44, 700, 90).Mono();
                L(bg.transform, NasaData.Card(file), 14, 18, 88, 880, 194);
                L(bg.transform, "Mass budget " + Budget(row).ToString("0.0") + " t\nMission " + Days(row).ToString("0") + " days\n<color=#8FA6BA>[game balance]</color>", 17, 720, 6, 900, 90).Mono();
                B(bg.transform, "SELECT", 910, 60, 1070, 112, () => { _pick = row; _plan = null; ShowBuild(); });
                y += 202;
            }
        }

        // ---------------------------------------------------------------- 2. build
        void ShowBuild()
        {
            var w = NewWindow("BUILD YOUR SHIP", "STEP 2 OF 3  ·  " + _pick["name"].ToUpper());
            NasaData.Mark(NasaData.FileFromRef(_pick["distance_source_row"]), MissionIds.DesignMode,
                _pick["name"] + ": " + Dist(DataTable.F(_pick, "distance_from_earth_km")) + ", one-way signal " + Light(DataTable.F(_pick, "one_way_light_time_s")));
            _planner.massBudgetTonnes = Budget(_pick);
            _planner.LoadSonsPlan();
            float days = Days(_pick);
            if (_supplies < days) _supplies = Mathf.CeilToInt(days / SupplyStep) * SupplyStep;
            _vehicles = LaunchCheck.VehiclesFor(_pick["id"], _parts);
            _veh = 0;
            _planner.destinationName = _pick["name"];
            _planner.missionDays = days;
            L(w, "Destination: <b>" + _pick["name"] + "</b>  ·  " + Dist(DataTable.F(_pick, "distance_from_earth_km")) + "  ·  signal delay " +
                Light(DataTable.F(_pick, "one_way_light_time_s")) + " one way  <color=#8FA6BA>[" + _pick["distance_source_row"] + "]</color>", 20, 28, 70, 1090, 110);
            L(w, NasaData.Tag(NasaData.FileFromRef(_pick["distance_source_row"])) + "  <color=#8FA6BA>Signal delay and distance are real; mass budget and days are [game balance].</color>", 14, 28, 112, 1090, 150);
            if (_pick["id"] == "trappist1e")
            {
                var e = _exo.Rows.Count > 0 ? _exo.Rows[0] : null;
                L(w, "Target (NASA Exoplanet Archive): radius " + DataTable.F(e, "radius_earth").ToString("0.00") + " Earth, insolation " +
                    DataTable.F(e, "insolation_earth").ToString("0.000") + " Earth, equilibrium temperature " + DataTable.F(e, "eq_temp_k").ToString("0.0") +
                    " K, orbit " + DataTable.F(e, "orbital_period_days").ToString("0.00") + " days.", 18, 28, 152, 1090, 196, Palette.Accent);
                NasaData.Mark("exoplanet_trappist1e.csv", MissionIds.DesignMode);
            }
            var box = Panel(w, 24, 200, 1096, 330);
            L(box.transform, "A. Planner: pick equipment grade and redundant units for five subsystems, inside the mass budget of " + Budget(_pick).ToString("0.0") +
                " t (game balance).", 19, 18, 10, 1050, 60, Palette.TextDim);
            B(box.transform, "OPEN PLANNER", 18, 70, 260, 120, () => _planner.SetVisible(true));
            _planText = L(box.transform, "", 19, 280, 74, 1050, 124);
            var box2 = Panel(w, 24, 350, 1096, 470);
            L(box2.transform, "B. Supplies: food, water and air in days. The trip needs " + days.ToString("0") + " days (game balance). Too few and the crew runs out before arrival.",
                19, 18, 10, 1050, 58, Palette.TextDim);
            B(box2.transform, "-", 18, 66, 78, 110, () => { _supplies = Mathf.Max(0, _supplies - SupplyStep); RefreshBuild(); }, ButtonStyle.Ghost);
            _supText = L(box2.transform, "", 26, 88, 66, 360, 110, null, TextAnchor.MiddleCenter).Bold();
            B(box2.transform, "+", 370, 66, 430, 110, () => { _supplies = Mathf.Min(SupplyMax, _supplies + SupplyStep); RefreshBuild(); }, ButtonStyle.Ghost);
            var vb = Panel(w, 24, 484, 1096, 600);
            _vehText = L(vb.transform, "", 18, 18, 8, 760, 108);
            B(vb.transform, "NEXT VEHICLE", 780, 30, 1056, 80, () => { _veh = (_veh + 1) % Mathf.Max(1, _vehicles.Count); RefreshBuild(); }, ButtonStyle.Ghost).gameObject.SetActive(_vehicles.Count > 1);
            L(w, NasaData.Card("parts_library.csv") + "  <color=#8FA6BA>Payload values are NASA; ship mass is [game balance].</color>", 14, 28, 606, 1090, 680, Palette.TextDim);
            _launch = B(w, "LAUNCH", 840, 690, 1096, 752, TryLaunch, ButtonStyle.Confirm);
            B(w, "BACK", 24, 690, 200, 752, ShowDestinations, ButtonStyle.Ghost);
            RefreshBuild();
        }

        string PartVal(string id)
        {
            var r = _parts.Find("id", id);
            return r == null ? "?" : DataTable.F(r, "value").ToString("N0");
        }

        void RefreshBuild()
        {
            if (_supText == null) return;
            _supText.text = _supplies + " days";
            bool ready = _plan != null;
            var vehicle = _vehicles.Count > 0 ? _vehicles[Mathf.Clamp(_veh, 0, _vehicles.Count - 1)] : null;
            var check = LaunchCheck.Check(_plan, vehicle, _pick != null ? _pick["name"] : "");
            _planner.vehicle = vehicle;
            _planner.suppliesDays = _supplies;
            _planner.RefreshResources();
            if (_vehText != null)
                _vehText.text = "<b>LAUNCH VEHICLE</b>  " + (vehicle != null ? vehicle.name + "  ·  payload " + vehicle.payloadTonnes.ToString("0.0") + " t to " + vehicle.reach : "none") + "\n" +
                    (!ready ? "<color=#FFB938>Apply a plan to check the lift.</color>" : check.ok ? "<color=#4DE38A>" + check.reason + "</color>" : "<color=#FF5A4D>" + check.reason + "</color>");
            if (_planText != null)
                _planText.text = ready ? "Plan applied: success probability <b>" + (RiskModel.ComputeSuccess(_plan) * 100f).ToString("0.0") + "%</b>  ·  mass " +
                    _plan.TotalMass().ToString("0.0") + " t" : "<color=#FFB938>No plan applied yet. Open the Planner and press APPLY PLAN.</color>";
            if (_launch != null) _launch.interactable = ready && check.ok;
        }

        void TryLaunch()
        {
            var vehicle = _vehicles.Count > 0 ? _vehicles[Mathf.Clamp(_veh, 0, _vehicles.Count - 1)] : null;
            var check = LaunchCheck.Check(_plan, vehicle, _pick["name"]);
            if (!check.ok) { Sfx.Play(Sfx.Id.Deny); RefreshBuild(); return; } // blocked: reason is on screen
            StartCoroutine(Fly());
        }

        // ---------------------------------------------------------------- 3. flight
        IEnumerator Fly()
        {
            var w = NewWindow("MISSION IN PROGRESS", "STEP 3 OF 3  ·  " + _pick["name"].ToUpper());
            var body = L(w, "", 21, 28, 80, 1090, 650).Mono();
            _log.Clear();
            _ok = true;
            float days = Days(_pick);
            var order = new[] { ("Launch", "Propulsion"), ("Cruise", "Power"), ("Course correction", "Navigation"), ("Crew systems", "Life support"), ("Link to Earth", "Comms") };
            var rng = new System.Random();
            foreach (var (stage, sys) in order)
            {
                var s = _plan.subsystems.Find(x => x.name == sys);
                float p = 1f - Mathf.Pow(1f - s.reliability, Mathf.Max(1, s.redundancy));
                float roll = (float)rng.NextDouble();
                bool pass = roll < p;
                string extra = sys == "Comms" ? "  (one-way delay " + Light(DataTable.F(_pick, "one_way_light_time_s")) + ")" : "";
                _log.Add((pass ? "<color=#4DE38A>PASS</color> " : "<color=#FF5A4D>FAIL</color> ") + stage + " / " + sys +
                    ": works " + (p * 100f).ToString("0.0") + "%, rolled " + (roll * 100f).ToString("0.0") + extra);
                body.text = string.Join("\n\n", _log);
                Sfx.Play(pass ? Sfx.Id.Confirm : Sfx.Id.Deny, 0.5f);
                yield return new WaitForSeconds(0.8f);
                if (!pass) { _ok = false; break; }
            }
            if (_ok)
            {
                bool enough = _supplies >= days;
                _log.Add((enough ? "<color=#4DE38A>PASS</color> " : "<color=#FF5A4D>FAIL</color> ") + "Supplies: " + _supplies + " days carried, " + days.ToString("0") + " needed");
                body.text = string.Join("\n\n", _log);
                Sfx.Play(enough ? Sfx.Id.Confirm : Sfx.Id.Deny, 0.5f);
                yield return new WaitForSeconds(0.8f);
                _ok = enough;
            }
            ShowDebrief();
        }

        // ---------------------------------------------------------------- 4. debrief
        void ShowDebrief()
        {
            var w = NewWindow(_ok ? "DEBRIEF: MISSION SUCCESS" : "DEBRIEF: MISSION LOST", _pick["name"].ToUpper());
            float planned = RiskModel.ComputeSuccess(_plan);
            L(w, "Planned success " + (planned * 100f).ToString("0.0") + "%  ·  Result: " + (_ok ? "<color=#4DE38A>arrived</color>" : "<color=#FF5A4D>lost</color>") +
                ".  A plan at " + (planned * 100f).ToString("0") + "% still fails about " + (100f - planned * 100f).ToString("0") + " times in 100. One run is not the odds.", 20, 28, 70, 1090, 130);
            L(w, string.Join("\n", _log), 16, 28, 135, 1090, 330).Mono();
            NasaData.Mark("dsn_notes.csv", MissionIds.DesignMode);
            float lt = DataTable.F(_pick, "one_way_light_time_s");
            var sb = new StringBuilder();
            sb.Append("<b>Redundancy.</b> Systems that must all work multiply their chances, so a backup unit raises one link to 1 - (1 - r)^n at the cost of mass. [NASA Systems Engineering Handbook, NASA/SP-2016-6105 Rev2]\n\n");
            sb.Append("<b>Signal delay.</b> Light from " + _pick["name"] + " takes " + Light(lt) + " one way, so a question and its answer take " + Light(lt * 2f) +
                ". Crews must decide alone. [NASA JPL Horizons, retrieved 2026-11-13]\n\n");
            sb.Append("<b>Deep Space Network.</b> Three complexes (Goldstone, Madrid, Canberra), each with a 70 m dish, keep contact as Earth rotates. [NASA Deep Space Network]");
            L(w, sb.ToString(), 18, 28, 345, 1090, 620, Palette.TextDim);
            L(w, NasaData.Card(NasaData.FileFromRef(_pick["distance_source_row"])), 14, 28, 628, 1090, 700);
            L(w, "Game-balance values (not NASA data): reliabilities, part masses, mass budgets, mission days.", 15, 28, 704, 1090, 730, Palette.TextDim);
            B(w, "DESIGN ANOTHER MISSION", 780, 740, 1096, 796, () => { _plan = null; ShowDestinations(); });
        }
    }
}
