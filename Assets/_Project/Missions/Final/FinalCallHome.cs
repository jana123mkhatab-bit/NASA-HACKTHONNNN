using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using LastSignal.M4;

namespace LastSignal.Finale
{
    /// <summary>
    /// Final "Call Home" (runs in M4_Planet after the M4 debrief):
    /// sunset call to Earth over the Deep Space Network -> walk with the son to the water collector
    /// -> crane-up end shot -> story end card -> RaiseComplete("Final") (Core end card, then Design Mode).
    /// </summary>
    public class FinalCallHome : MonoBehaviour
    {
        M4Mission _m;
        bool _walking, _ending;
        Canvas _card;

        public void Begin(M4Mission m)
        {
            _m = m;
            StartCoroutine(CallHome());
        }

        IEnumerator CallHome()
        {
            M4Hooks.Phase("CallHome");
            var mm = MissionManager.Instance;
            if (mm != null && mm.hud != null) mm.hud.SetVisible(false);
            _m.Father.inputEnabled = false;
            _m.UI.SetObjective(null);

            // Core plays Pip's Final intro right after raising Start("Final"). Let it finish over the open landscape
            // (at most 12 s), then close Pip's box so it never sits on top of the letterbox subtitles.
            yield return null;
            for (float w = 0f; w < 12f && mm != null && mm.pipUI != null && mm.pipUI.IsPlaying; w += Time.deltaTime) yield return null;
            M4Mission.SilencePip();
            yield return StartCoroutine(_m.UI.Letterbox(true));
            StartCoroutine(Sunset(10f));

            // Wide: father and son beside the lander as its dish swings toward home.
            var f = _m.Father.transform;
            Vector3 lander = _m.Lander != null ? _m.Lander.transform.position : f.position + f.forward * 10f;
            Vector3 mid = (f.position + lander) * 0.5f;
            Vector3 toSun = -_m.World.SunLight.transform.forward; toSun.y = 0; toSun.Normalize();
            Vector3 side = Vector3.Cross(Vector3.up, toSun);
            // Camera looks roughly toward the setting sun: silhouettes and rim light.
            Rig.Shot(mid - toSun * 22f + side * 6f + Vector3.up * 3f, mid - toSun * 16f + side * 4f + Vector3.up * 2.4f, f, Vector3.up * 1.6f, 22f, 40f, 2f);
            _m.PostFx.Cinematic(true, 18f);

            string[,] lines =
            {
                { "", "Deep Space Network. Link established." },
                { "MISSION CONTROL", "Survey received. Every byte of it." },
                { "MISSION CONTROL", "Coordinates, water readings... and a second flight log." },
                { "FATHER", "He's here. He's alive." },
                { "MISSION CONTROL", "...Copy that. Two to bring home." },
                { "SON", "Tell Mom I kept the water running." },
                { "MISSION CONTROL", "The next ship is already on the build schedule. Your plan is in it." },
            };
            for (int i = 0; i < lines.GetLength(0); i++)
            {
                _m.UI.Subtitle(lines[i, 0], lines[i, 1]);
                if (i == 0 && _m.Lander != null && _m.Lander.Dish != null) StartCoroutine(SwingDish(_m.Lander.Dish));
                yield return new WaitForSeconds(lines[i, 1].Length < 30 ? 2.8f : 3.8f);
            }
            _m.UI.Subtitle("", "");
            _m.PostFx.Cinematic(false);
            yield return StartCoroutine(_m.UI.Letterbox(false));
            if (mm != null && mm.hud != null) mm.hud.SetVisible(true);

            // Walk together to the collector.
            M4Hooks.Phase("WalkTogether");
            Rig.Follow(f, f.eulerAngles.y);
            _m.Father.inputEnabled = true;
            _m.Son.followOffset = new Vector3(1.6f, 0f, -0.2f);
            _m.Son.followTarget = f;
            _m.UI.SetObjective("Walk with your son to the water collector", _m.World.Collector.position, f);
            Pip("Last walk of the mission. Take your time.");
            _walking = true;
        }

        M4CameraRig Rig => _m.Rig;

        static void Pip(params string[] lines)
        {
            var mm = MissionManager.Instance;
            if (mm != null && mm.pipUI != null) mm.pipUI.PlayLines(lines);
        }

        IEnumerator SwingDish(Transform dish)
        {
            Quaternion a = dish.localRotation, b = Quaternion.Euler(-25f, 140f, 0f);
            for (float t = 0; t < 3f; t += Time.deltaTime) { dish.localRotation = Quaternion.Slerp(a, b, Mathf.SmoothStep(0, 1, t / 3f)); yield return null; }
        }

        /// <summary>Sun sinks toward the horizon, light warms, fog and grade shift toward dusk.</summary>
        IEnumerator Sunset(float dur)
        {
            var sun = _m.World.SunLight;
            Quaternion from = sun.transform.rotation;
            Vector3 dir = -sun.transform.forward;
            Vector3 low = new Vector3(dir.x, 0.07f, dir.z).normalized;
            Quaternion to = Quaternion.LookRotation(-low);
            Color c0 = sun.color, c1 = new Color(1f, 0.55f, 0.3f);
            float i0 = sun.intensity, i1 = sun.intensity * 0.8f;
            Color f0 = RenderSettings.fogColor, f1 = new Color(0.38f, 0.2f, 0.14f);
            var sunBody = GameObject.Find("Sun System");
            for (float t = 0; t < dur; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0, 1, t / dur);
                sun.transform.rotation = Quaternion.Slerp(from, to, k);
                sun.color = Color.Lerp(c0, c1, k);
                sun.intensity = Mathf.Lerp(i0, i1, k);
                RenderSettings.fogColor = Color.Lerp(f0, f1, k);
                _m.PostFx.Dusk(k);
                if (sunBody != null) sunBody.transform.position = -sun.transform.forward * 850f;
                yield return null;
            }
        }

        void Update()
        {
            if (!_walking || _ending || _m == null || _m.Father == null) return;
            Vector3 d = _m.World.Collector.position - _m.Father.transform.position; d.y = 0;
            if (d.magnitude < 7f) StartCoroutine(EndShot());
        }

        IEnumerator EndShot()
        {
            _ending = true;
            _walking = false;
            M4Hooks.Phase("End");
            _m.Father.inputEnabled = false;
            _m.UI.SetObjective(null);
            var mm = MissionManager.Instance;
            if (mm != null && mm.hud != null) mm.hud.SetVisible(false);
            M4Mission.SilencePip();
            yield return StartCoroutine(_m.UI.Letterbox(true));

            // Crane up and back from the two figures at the collector to the whole valley under the stars.
            var c = _m.World.Collector;
            Vector3 toFather = _m.Father.transform.position - c.position; toFather.y = 0; toFather.Normalize();
            Vector3 start = c.position + toFather * 10f + Vector3.up * 2f;
            Vector3 end = c.position + toFather * 45f + Vector3.up * 30f + Vector3.Cross(Vector3.up, toFather) * 15f;
            Rig.Shot(start, end, c, Vector3.up * 3f, 11f, 42f, 1.5f);
            _m.UI.Subtitle("SON", "I named the valley after you, you know.");
            yield return new WaitForSeconds(3.5f);
            _m.UI.Subtitle("FATHER", "Let's go home.");
            yield return new WaitForSeconds(4.5f);
            _m.UI.Subtitle("", "");
            yield return StartCoroutine(_m.UI.Fade(1f, 2.5f));
            ShowStoryCard();
        }

        void ShowStoryCard()
        {
            _card = UIFactory.CreateCanvas("Final Story Card", 36);
            _card.transform.SetParent(transform, false);
            var root = (RectTransform)_card.transform;
            UIFactory.Box("Black", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.black);
            UIFactory.Label("Title", root, "THE LAST KNOWN SIGNAL", 64, TextAnchor.MiddleCenter, new Vector2(0.1f, 0.6f), new Vector2(0.9f, 0.72f), Vector2.zero, Vector2.zero, UIFactory.Accent);
            UIFactory.Label("Body", root,
                "Year 2590. The survey reached Earth.\nOne father. One son. One ship.\nComing home.", 34, TextAnchor.UpperCenter,
                new Vector2(0.15f, 0.36f), new Vector2(0.85f, 0.58f), Vector2.zero, Vector2.zero);
            string planet = _m.Planet != null ? _m.Planet.name : "an exoplanet";
            UIFactory.Label("Credit", root,
                $"The planet in this story is based on {planet}. Values from the NASA Exoplanet Archive.\nLanding limits, fuel and power are game-balance values, not NASA data.", 20,
                TextAnchor.UpperCenter, new Vector2(0.15f, 0.22f), new Vector2(0.85f, 0.32f), Vector2.zero, Vector2.zero, new Color(0.6f, 0.7f, 0.8f));
            UIFactory.Button("Continue", root, "Continue", new Vector2(0.42f, 0.08f), new Vector2(0.58f, 0.15f), Vector2.zero, Vector2.zero, OnContinue);
            var cg = _card.gameObject.AddComponent<CanvasGroup>();
            StartCoroutine(FadeIn(cg));
        }

        IEnumerator FadeIn(CanvasGroup cg)
        {
            cg.alpha = 0f;
            for (float t = 0; t < 2f; t += Time.deltaTime) { cg.alpha = t / 2f; yield return null; }
            cg.alpha = 1f;
        }

        void OnContinue()
        {
            if (_card != null) Destroy(_card.gameObject);
            StartCoroutine(_m.UI.Fade(0f, 0.5f));
            var mm = MissionManager.Instance;
            if (mm != null && mm.hud != null) mm.hud.SetVisible(true);
            MissionEvents.RaiseComplete(MissionIds.Final); // Core end card (planned vs actual), then Design Mode
        }
    }
}
