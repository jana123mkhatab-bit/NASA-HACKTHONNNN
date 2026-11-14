using System;
using UnityEngine;
using UnityEngine.UI;

namespace LastSignal
{
    /// <summary>
    /// Pip's 5-step first-time tour of the Planner: mass budget, equipment grade, redundancy, success gauge, APPLY/LAUNCH.
    /// Skippable, replayable (the "?" button), remembered in PlayerPrefs. It only draws a card and outlines; it never
    /// captures clicks outside its own buttons, and any error closes it so the planner keeps working.
    /// Percentages quoted are game-balance values, not NASA data.
    /// </summary>
    public class PlannerTutorial : MonoBehaviour
    {
        public const string SeenKey = "ls_planner_tutorial_seen";

        static readonly string[] Lines =
        {
            "This is the mass budget. Every part weighs something and the rocket can only lift so much. Stay under the line.",
            "Tested equipment fails less (90% vs 63% per unit, game balance) but weighs more. Untested is light and risky.",
            "A second unit is a backup: if one fails, the spare works on. But a spare is more hardware, and every tonne costs fuel to lift.",
            "This gauge is your success chance. Every subsystem must work, so the odds multiply, and one weak link drags it down.",
            "APPLY PLAN locks in your design. Then LAUNCH flies it. Too heavy for the rocket? Launch stays locked and I tell you why.",
        };

        PlannerPanel _owner;
        Canvas _canvas;
        Text _text, _step;
        Button _next;
        Image _frame;
        int _i;
        bool _active;

        /// <summary>Show the tour unless already seen (force = replay).</summary>
        public void Begin(PlannerPanel owner, bool force)
        {
            try
            {
                if (_active) { if (force) { _i = 0; ShowStep(); } return; }
                if (!force && Seen()) return;
                _owner = owner;
                if (_canvas == null) Build();
                _i = 0;
                _active = true;
                _canvas.enabled = true;
                ShowStep();
            }
            catch (Exception e) { Debug.LogWarning("[PlannerTutorial] " + e.Message); Close(false); }
        }

        public static bool Seen()
        {
            try { return PlayerPrefs.GetInt(SeenKey, 0) == 1; } catch { return true; }
        }

        static void MarkSeen()
        {
            try { PlayerPrefs.SetInt(SeenKey, 1); PlayerPrefs.Save(); } catch { }
        }

        void Build()
        {
            _canvas = UIFactory.CreateCanvas("PlannerTutorial", 45);
            _canvas.transform.SetParent(transform, false);
            var root = (RectTransform)_canvas.transform;
            _frame = UIFactory.Stroke("Highlight", root, Palette.Warn, 8, 3f);
            var card = UIFactory.Window("PipCard", root, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-460, 6), new Vector2(460, 118));
            // Pip: ceramic head, dark visor, two cyan eyes (same look as the dialogue box).
            var head = UIFactory.Round("Head", card, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(22, -30), new Vector2(92, 36), Palette.Ceramic, 16);
            var visor = UIFactory.Round("Visor", head.rectTransform, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(8, -12), new Vector2(-8, 14), Palette.Ink, 10);
            foreach (var x in new[] { -13f, 13f })
                UIFactory.Round("Eye", visor.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x - 6, -6), new Vector2(x + 6, 6), Palette.Accent, 6);
            UIFactory.Label("Name", card, "PIP", 14, TextAnchor.LowerCenter, new Vector2(0, 0), new Vector2(0, 0), new Vector2(18, 6), new Vector2(98, 26), Palette.Accent).Mono();
            _text = UIFactory.Label("Line", card, "", 20, TextAnchor.MiddleLeft, Vector2.zero, Vector2.one, new Vector2(112, 40), new Vector2(-16, -8));
            _step = UIFactory.Label("Step", card, "", 13, TextAnchor.LowerLeft, new Vector2(0, 0), new Vector2(0, 0), new Vector2(112, 10), new Vector2(300, 32), Palette.TextDim).Mono();
            UIFactory.Button("Skip", card, "SKIP", new Vector2(1, 0), new Vector2(1, 0), new Vector2(-250, 8), new Vector2(-136, 38), () => Close(true), ButtonStyle.Ghost);
            _next = UIFactory.Button("Next", card, "NEXT", new Vector2(1, 0), new Vector2(1, 0), new Vector2(-126, 8), new Vector2(-14, 38), Advance);
        }

        void Advance()
        {
            try
            {
                Sfx.Play(Sfx.Id.Click);
                if (_i >= Lines.Length - 1) { Close(true); return; }
                _i++;
                ShowStep();
            }
            catch (Exception e) { Debug.LogWarning("[PlannerTutorial] " + e.Message); Close(false); }
        }

        void ShowStep()
        {
            _text.text = Lines[_i];
            _step.text = "STEP " + (_i + 1) + " OF " + Lines.Length;
            UIFactory.SetLabel(_next, _i >= Lines.Length - 1 ? "GOT IT" : "NEXT");
            PlaceFrame();
        }

        void Close(bool seen)
        {
            _active = false;
            if (_canvas != null) _canvas.enabled = false;
            if (seen) MarkSeen();
        }

        void PlaceFrame()
        {
            var targets = _owner.TutorialTargets(_i);
            var root = (RectTransform)_canvas.transform;
            Vector2 lo = new Vector2(float.MaxValue, float.MaxValue), hi = new Vector2(float.MinValue, float.MinValue);
            var c = new Vector3[4];
            foreach (var t in targets)
            {
                if (t == null) continue;
                t.GetWorldCorners(c);
                foreach (var w in c)
                {
                    var l = root.InverseTransformPoint(w);
                    lo = Vector2.Min(lo, l); hi = Vector2.Max(hi, l);
                }
            }
            if (lo.x > hi.x) { _frame.enabled = false; return; }
            _frame.enabled = true;
            var rt = _frame.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.offsetMin = lo - new Vector2(8, 8);
            rt.offsetMax = hi + new Vector2(8, 8);
        }

        void Update()
        {
            if (!_active) return;
            try
            {
                if (_owner == null || !_owner.IsVisible) { Close(false); return; } // planner closed: pause, show again next time
                PlaceFrame();
                var col = Palette.Warn; col.a = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 6f);
                _frame.color = col;
            }
            catch (Exception e) { Debug.LogWarning("[PlannerTutorial] " + e.Message); Close(false); }
        }
    }
}
