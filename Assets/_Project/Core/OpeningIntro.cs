using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace LastSignal
{
    /// <summary>
    /// Skippable cinematic opening (about 38 s) that plays before the title in the M1 scene.
    /// Ground: the camera pulls back from the base building and rises into the haze. Space: a runtime set of
    /// Earth, Moon and Sun (the M3Fx materials) and a relay satellite that pulses toward Earth, then goes silent;
    /// its last signal heads out into the dark. Then a fade to the title.
    /// Lines and timing are the open_01..open_08 rows of narration_script.csv (NARRATION.md section 1).
    /// The space set is cinematic, not to scale, and shows no numbers. Any key, click or pad button skips it.
    /// "INTRO: OFF" on the title (PlayerPrefs ls_skip_intro) turns it off.
    /// </summary>
    public class OpeningIntro : MonoBehaviour
    {
        const string SkipKey = "ls_skip_intro";
        static readonly Vector3 SetCenter = new Vector3(0f, 20000f, 0f);   // far above the M1 ground set

        public static bool Running { get; private set; }

        public static bool SkipSetting
        {
            get { try { return PlayerPrefs.GetInt(SkipKey, 0) == 1; } catch { return false; } }
            set { try { PlayerPrefs.SetInt(SkipKey, value ? 1 : 0); PlayerPrefs.Save(); } catch { } }
        }

        /// <summary>False in batch mode, for capture runs, with -lsNoIntro, or when the player turned it off.</summary>
        public static bool ShouldPlay()
        {
            if (Application.isBatchMode || SkipSetting) return false;
            foreach (var a in System.Environment.GetCommandLineArgs())
                if (a == "-lsNoIntro" || a == "-lsCapture") return false;
            return true;
        }

        /// <summary>Start time of each open_ line (and the total at index 8), from the script CSV seconds.</summary>
        public static float[] LineStarts()
        {
            var ids = NarrationPlayer.OpeningIds;
            var starts = new float[ids.Length + 1];
            for (int i = 0; i < ids.Length; i++)
            {
                var r = NarrationPlayer.Find(ids[i]);
                starts[i + 1] = starts[i] + (r != null ? r.seconds : 4.5f);
            }
            return starts;
        }

        public static OpeningIntro Begin(Transform parent, System.Action onDone)
        {
            var go = new GameObject("OpeningIntro");
            go.transform.SetParent(parent, false);
            var intro = go.AddComponent<OpeningIntro>();
            intro._onDone = onDone;
            return intro;
        }

        System.Action _onDone;
        bool _finished;
        float _age;
        ScreenFader _fader;
        Canvas _canvas;
        Text _caption;
        CanvasGroup _captionGroup;

        // ground
        CameraRig _rig;
        Camera _mainCam;
        // space
        GameObject _space;
        Camera _spaceCam;
        Transform _relay, _beacon, _pulse;
        Light _sun;
        Color _sunColor; float _sunIntensity;
        bool _fog;

        void Start()
        {
            Running = true;
            NarrationPlayer.MarkOpeningPlayed();
            var mm = MissionManager.Instance;
            _fader = mm != null ? mm.fader : null;
            _mainCam = Camera.main;
            _rig = _mainCam != null ? _mainCam.GetComponent<CameraRig>() : null;
            BuildOverlay();
            StartCoroutine(Run());
        }

        void Update()
        {
            if (_finished) return;
            _age += Time.unscaledDeltaTime;
            var mm = MissionManager.Instance;
            if (mm != null && mm.Current != MissionIds.Boot) { Finish(false); return; }   // a run was started some other way
            if (_age > 0.4f && AnyInputDown()) { StopAllCoroutines(); StartCoroutine(SkipRoutine()); _finished = true; }
        }

        // ------------------------------------------------------------------ timeline

        IEnumerator Run()
        {
            float[] s = LineStarts();
            float spaceAt = s[3], silentAt = s[6], titleAt = s[7];

            if (_fader != null) yield return _fader.FadeOut(0.01f);
            NarrationPlayer.PlaySequence(NarrationPlayer.OpeningIds);

            // 1. Ground: close on the base building, then pull back and rise (open_01..open_03).
            float t0 = Time.unscaledTime;
            if (_rig != null)
            {
                Vector3 b = FindBuilding();
                _rig.shots.RemoveAll(x => x.name == "intro_close" || x.name == "intro_high");
                _rig.shots.Add(new CameraRig.Shot { name = "intro_close", position = b + new Vector3(-5f, 1.6f, -8f), lookAt = b + new Vector3(0f, 1.2f, 0f), fov = 38f });
                _rig.shots.Add(new CameraRig.Shot { name = "intro_high", position = b + new Vector3(-45f, 85f, -120f), lookAt = b + new Vector3(0f, 0f, 25f), fov = 55f });
                _rig.Snap("intro_close");
                _rig.GoTo("intro_high", Mathf.Max(1f, spaceAt - 0.5f));
            }
            if (_fader != null) yield return _fader.FadeIn(null, null, 1.5f);
            Sfx.Play(Sfx.Id.Whoosh, 0.5f, 0.8f);
            yield return Until(t0 + spaceAt - 0.7f);
            if (_fader != null) yield return _fader.FadeOut(0.6f);

            // 2. Space: Earth close, pull back to Moon and Sun; the relay pulses toward Earth (open_04..open_06).
            BuildSpace();
            Sfx.Play(Sfx.Id.Whoosh, 0.6f, 0.6f);
            if (_fader != null) yield return _fader.FadeIn(null, null, 0.9f);
            float nextPulse = 0f;
            while (Time.unscaledTime < t0 + silentAt)
            {
                float now = Time.unscaledTime - t0;
                Blink(now);
                if (now >= nextPulse) { StartCoroutine(Pulse(_relay.position, SetCenter, 1.3f, false)); nextPulse = now + 1.8f; }
                yield return null;
            }

            // 3. The relay goes silent; its last known signal heads out past the Moon (open_07).
            StartCoroutine(Pulse(_relay.position, _relay.position + (_relay.position - SetCenter).normalized * 2400f + Vector3.up * 300f, 4f, true));
            Sfx.Play(Sfx.Id.Beep, 0.5f, 0.8f);
            yield return Wait(1.2f);
            if (_beacon != null) _beacon.gameObject.SetActive(false);
            Sfx.Play(Sfx.Id.Deny, 0.6f, 0.7f);
            ShowCaption("RELAY R-7  ·  NO CARRIER");
            yield return Wait(1.8f);
            ShowCaption("LAST KNOWN SIGNAL");
            yield return Until(t0 + titleAt);

            // 4. Fade to the title while open_08 plays.
            HideCaption();
            if (_fader != null) yield return _fader.FadeOut(0.9f);
            Cleanup();
            Done();
            if (_fader != null) yield return _fader.FadeIn(null, null, 1.1f);
            Finish(true);
        }

        IEnumerator SkipRoutine()
        {
            if (NarrationPlayer.Instance != null) NarrationPlayer.Instance.Stop();
            Sfx.Play(Sfx.Id.Click);
            HideCaption();
            if (_fader != null) yield return _fader.FadeOut(0.2f);
            Cleanup();
            Done();
            if (_fader != null) yield return _fader.FadeIn(null, null, 0.4f);
            Finish(true);
        }

        void Done() { var cb = _onDone; _onDone = null; cb?.Invoke(); }

        void Finish(bool clean)
        {
            _finished = true;
            if (!clean)   // MissionManager is changing scene and runs its own fades
            {
                StopAllCoroutines();
                if (NarrationPlayer.Instance != null) NarrationPlayer.Instance.Stop();
                Cleanup();
            }
            Running = false;
            Destroy(gameObject);
        }

        void OnDestroy() { Running = false; Cleanup(); if (_canvas != null) Destroy(_canvas.gameObject); }

        static IEnumerator Until(float time) { while (Time.unscaledTime < time) yield return null; }
        static IEnumerator Wait(float seconds) { float end = Time.unscaledTime + seconds; while (Time.unscaledTime < end) yield return null; }

        // ------------------------------------------------------------------ ground

        static Vector3 FindBuilding()
        {
            foreach (var n in new[] { "Hangar A", "Assembly Hall", "Bunker", "Telemetry Tower" })
            {
                var go = GameObject.Find(n);
                if (go != null) return go.transform.position;
            }
            return new Vector3(9f, 1.6f, 9.5f);
        }

        // ------------------------------------------------------------------ space set

        void BuildSpace()
        {
            _space = new GameObject("IntroSpace");
            _space.transform.SetParent(transform, false);

            _fog = RenderSettings.fog;
            RenderSettings.fog = false;
            _sun = RenderSettings.sun;
            if (_sun == null)
                foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None)) if (l.type == LightType.Directional) { _sun = l; break; }
            Vector3 sunDir = new Vector3(0.6f, 0.15f, 0.8f);
            if (_sun != null)
            {
                _sunColor = _sun.color; _sunIntensity = _sun.intensity;
                _sun.color = new Color(1f, 0.97f, 0.92f); _sun.intensity = 1.3f;
                sunDir = -_sun.transform.forward;
            }
            Vector3 flat = new Vector3(sunDir.x, 0f, sunDir.z);
            if (flat.sqrMagnitude < 0.01f) flat = Vector3.forward;
            flat.Normalize();
            Vector3 side = Vector3.Cross(Vector3.up, flat).normalized;
            Vector3 c = SetCenter;

            // Cinematic layout, not to scale. Sun sits along the scene light so Earth's lit side faces it.
            Transform earth = Body("Earth", c, 120f, "earth_surface", new Color(0.2f, 0.4f, 0.8f));
            Shell(earth, 1.012f, "earth_clouds");
            Shell(earth, 1.06f, "earth_atmosphere");
            earth.gameObject.AddComponent<SlowSpin>();
            Body("Moon", c - flat * 250f - side * 350f + Vector3.up * 60f, 32f, "moon_surface", new Color(0.6f, 0.6f, 0.6f));
            Transform sunBody = Body("Sun", c + sunDir.normalized * 1500f, 300f, "sun_surface", new Color(1f, 0.8f, 0.4f));
            Shell(sunBody, 1.35f, "sun_corona");

            _relay = BuildRelay(c + side * 700f - flat * 60f + Vector3.up * 30f, c);

            var camGo = new GameObject("IntroSpaceCam");
            camGo.transform.SetParent(_space.transform, false);
            _spaceCam = camGo.AddComponent<Camera>();
            _spaceCam.clearFlags = CameraClearFlags.SolidColor;
            _spaceCam.backgroundColor = new Color(0.01f, 0.012f, 0.02f);
            _spaceCam.nearClipPlane = 0.5f; _spaceCam.farClipPlane = 12000f;
            _spaceCam.depth = _mainCam != null ? _mainCam.depth + 1 : 10;
            try { _spaceCam.GetUniversalAdditionalCameraData().renderPostProcessing = true; } catch { }
            Stars(camGo.transform);
            if (_mainCam != null) _mainCam.enabled = false;

            var rig = camGo.AddComponent<CameraRig>();
            rig.sway = 1.2f;
            rig.shots.Add(new CameraRig.Shot { name = "close", position = c + side * 230f + Vector3.up * 35f - flat * 40f, lookAt = c, fov = 45f });
            rig.shots.Add(new CameraRig.Shot { name = "wide", position = c + side * 1100f + Vector3.up * 160f, lookAt = c + sunDir.normalized * 450f - side * 80f, fov = 62f });
            rig.Snap("close");
            float[] s = LineStarts();
            rig.GoTo("wide", Mathf.Max(2f, s[6] - s[3]));
        }

        Transform Body(string name, Vector3 pos, float diameter, string mat, Color fallback)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(_space.transform, false);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * diameter;
            var r = go.GetComponent<Renderer>();
            var m = Resources.Load<Material>("M3Fx/" + mat);
            r.sharedMaterial = m != null ? new Material(m) : Lit(fallback, false);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return go.transform;
        }

        void Shell(Transform body, float scale, string mat)
        {
            var m = Resources.Load<Material>("M3Fx/" + mat);
            if (m == null) return;
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = mat;
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(body, false);
            go.transform.localScale = Vector3.one * scale;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = new Material(m);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        Transform BuildRelay(Vector3 pos, Vector3 earth)
        {
            var root = new GameObject("Relay").transform;
            root.SetParent(_space.transform, false);
            root.position = pos;
            root.rotation = Quaternion.LookRotation(pos - earth);
            Color metal = new Color(0.75f, 0.70f, 0.55f), panel = new Color(0.12f, 0.20f, 0.45f);
            Part(PrimitiveType.Cube, root, Vector3.zero, new Vector3(4f, 4f, 5f), Vector3.zero, Lit(metal, false));
            Part(PrimitiveType.Cube, root, new Vector3(-9f, 0f, 0f), new Vector3(12f, 0.2f, 4f), Vector3.zero, Lit(panel, true));
            Part(PrimitiveType.Cube, root, new Vector3(9f, 0f, 0f), new Vector3(12f, 0.2f, 4f), Vector3.zero, Lit(panel, true));
            Part(PrimitiveType.Cylinder, root, new Vector3(0f, 0f, -4f), new Vector3(7f, 0.3f, 7f), new Vector3(90f, 0f, 0f), Lit(new Color(0.9f, 0.9f, 0.9f), false));
            _beacon = Part(PrimitiveType.Sphere, root, new Vector3(0f, 2.6f, 0f), Vector3.one * 1.1f, Vector3.zero, Flat(new Color(1f, 0.3f, 0.2f)));
            root.localScale = Vector3.one * 1.4f;
            return root;
        }

        static Transform Part(PrimitiveType type, Transform parent, Vector3 pos, Vector3 scale, Vector3 euler, Material m)
        {
            var go = GameObject.CreatePrimitive(type);
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.transform.localRotation = Quaternion.Euler(euler);
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go.transform;
        }

        // Primitives ship with the built-in Standard material (magenta under URP in a player): use URP Lit instead.
        static Material Lit(Color c, bool emissive)
        {
            var sh = Shader.Find("Universal Render Pipeline/Lit");
            var m = sh != null ? new Material(sh) : Flat(c);
            m.color = c;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (emissive && m.HasProperty("_EmissionColor")) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c * 0.6f); }
            return m;
        }

        // Sprites/Default is in Always Included Shaders, so it is safe in the Windows player.
        static Material Flat(Color c)
        {
            var sh = Shader.Find("Sprites/Default");
            if (sh == null) sh = Shader.Find("Universal Render Pipeline/Unlit");
            var m = new Material(sh);
            m.color = c;
            return m;
        }

        void Stars(Transform parent)
        {
            var rng = new System.Random(2590);
            const int n = 700;
            var verts = new Vector3[n * 4];
            var cols = new Color[n * 4];
            var tris = new int[n * 6];
            for (int i = 0; i < n; i++)
            {
                Vector3 d = new Vector3((float)rng.NextDouble() * 2f - 1f, (float)rng.NextDouble() * 2f - 1f, (float)rng.NextDouble() * 2f - 1f);
                if (d.sqrMagnitude < 0.01f) d = Vector3.forward;
                d = d.normalized * 9000f;
                float size = 6f + (float)rng.NextDouble() * 14f;
                Vector3 a = Vector3.Cross(d, Mathf.Abs(d.y) < 8000f ? Vector3.up : Vector3.right).normalized * size;
                Vector3 b = Vector3.Cross(d, a).normalized * size;
                verts[i * 4] = d - a - b; verts[i * 4 + 1] = d - a + b; verts[i * 4 + 2] = d + a + b; verts[i * 4 + 3] = d + a - b;
                float k = 0.4f + (float)rng.NextDouble() * 0.6f;
                Color col = new Color(k, k, Mathf.Min(1f, k * 1.1f), 1f);
                for (int j = 0; j < 4; j++) cols[i * 4 + j] = col;
                int v = i * 4, t = i * 6;
                tris[t] = v; tris[t + 1] = v + 1; tris[t + 2] = v + 2; tris[t + 3] = v; tris[t + 4] = v + 2; tris[t + 5] = v + 3;
            }
            var mesh = new Mesh { name = "IntroStars" };
            mesh.vertices = verts; mesh.colors = cols; mesh.triangles = tris;
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 20000f);
            var go = new GameObject("Stars");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            var m = Flat(Color.white);
            m.renderQueue = 1000;   // behind everything
            r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        void Blink(float t)
        {
            if (_beacon != null) _beacon.gameObject.SetActive(Mathf.Repeat(t, 1.2f) < 0.35f);
        }

        IEnumerator Pulse(Vector3 from, Vector3 to, float seconds, bool fade)
        {
            if (_space == null) yield break;
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(go.GetComponent<Collider>());
            go.name = "Pulse";
            go.transform.SetParent(_space.transform, false);
            var r = go.GetComponent<Renderer>();
            var m = Flat(new Color(1f, 0.82f, 0.3f, 1f));
            r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (!fade) Sfx.Play(Sfx.Id.Beep, 0.18f, 1.2f);
            float u = 0f;
            while (u < 1f && go != null)
            {
                u += Time.unscaledDeltaTime / seconds;
                go.transform.position = Vector3.Lerp(from, to, u);
                go.transform.localScale = Vector3.one * (fade ? Mathf.Lerp(5f, 2f, u) : 4f);
                if (fade) m.color = new Color(1f, 0.82f, 0.3f, 1f - u);
                yield return null;
            }
            if (go != null) Destroy(go);
        }

        void Cleanup()
        {
            if (_space != null)
            {
                RenderSettings.fog = _fog;
                if (_sun != null) { _sun.color = _sunColor; _sun.intensity = _sunIntensity; }
                Destroy(_space);
                _space = null;
            }
            if (_mainCam != null) _mainCam.enabled = true;
            if (_rig != null) _rig.Snap("title");
            _rig = null;
        }

        // ------------------------------------------------------------------ overlay

        void BuildOverlay()
        {
            _canvas = UIFactory.CreateCanvas("IntroOverlay", 45, transform);
            var root = (RectTransform)_canvas.transform;
            var hint = UIFactory.Label("SkipHint", root, "PRESS ANY KEY TO SKIP", 16, TextAnchor.LowerRight,
                new Vector2(1, 0), new Vector2(1, 0), new Vector2(-520, 30), new Vector2(-40, 58), Palette.A(Palette.TextDim, 0.8f)).Mono();
            hint.raycastTarget = false;
            var cap = UIFactory.Rect("Caption", root, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-600, -190), new Vector2(600, -110));
            _captionGroup = UIFactory.Group(cap);
            _captionGroup.alpha = 0f; _captionGroup.blocksRaycasts = false;
            _caption = UIFactory.Label("Text", cap, "", 34, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Palette.Warn).Mono();
            _caption.raycastTarget = false;
        }

        void ShowCaption(string text) { if (_caption == null) return; _caption.text = text; _captionGroup.alpha = 1f; }
        void HideCaption() { if (_captionGroup != null) _captionGroup.alpha = 0f; }

        static bool AnyInputDown()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) return true;
            var mouse = Mouse.current;
            if (mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame)) return true;
            var pad = Gamepad.current;
            if (pad != null && (pad.buttonSouth.wasPressedThisFrame || pad.startButton.wasPressedThisFrame || pad.buttonEast.wasPressedThisFrame)) return true;
            return false;
#else
            return Input.anyKeyDown;
#endif
        }
    }
}
