using UnityEngine;
using UnityEngine.UI;

namespace LastSignal
{
    /// <summary>
    /// Title screen shown over the first scene. It carries the hook of the whole game: the son's launch
    /// panel, where the Success Probability reads "??%" because the number never reached him.
    /// Begin starts a new run at M1.
    /// </summary>
    public class TitleScreenUI : MonoBehaviour
    {
        Canvas _canvas;
        CanvasGroup _group;
        float _fade = -1f;

        public void Build()
        {
            _canvas = UIFactory.CreateCanvas("Title", 70);
            _canvas.transform.SetParent(transform, false);
            var root = (RectTransform)_canvas.transform;
            _group = UIFactory.Group(_canvas);

            // Darken the scene behind: heavy on the left where the text sits, open on the right.
            UIFactory.Box("Shade", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Palette.A(Palette.Shadow, 0.42f));
            var left = UIFactory.Rect("ShadeLeft", root, Vector2.zero, new Vector2(0.75f, 1), Vector2.zero, Vector2.zero);
            var li = left.gameObject.AddComponent<Image>();
            li.sprite = UISkin.FadeRight(); li.color = Palette.A(Palette.Ink, 0.92f); li.raycastTarget = false;

            UIFactory.Label("Kicker", root, "NASA SPACE APPS CHALLENGE  ·  CAIRO 2026  ·  SPACE MISSION DESIGN GAME", 17, TextAnchor.LowerLeft,
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(120, 252), new Vector2(1100, 280), Palette.Accent).Mono();
            UIFactory.Label("Name1", root, "THE LAST", 124, TextAnchor.LowerLeft,
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(114, 126), new Vector2(1100, 256)).Bold();
            UIFactory.Label("Name2", root, "KNOWN SIGNAL", 124, TextAnchor.LowerLeft,
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(114, 16), new Vector2(1200, 146), Palette.Accent).Bold();
            var rule = UIFactory.Box("Rule", root, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(120, 2), new Vector2(520, 5), Palette.Warn);
            rule.GetComponent<Image>().raycastTarget = false;
            UIFactory.Label("Sub", root,
                "Year 2590. A survey pilot vanished on a flight nobody should have approved.\nHis father joins the build team of the next ship.", 28, TextAnchor.UpperLeft,
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(120, -96), new Vector2(980, -14), Palette.Text);
            UIFactory.Label("Pitch", root, "Design the mission. See its true odds. Live with the consequences.", 22, TextAnchor.UpperLeft,
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(120, -146), new Vector2(980, -108), Palette.TextDim);

            UIFactory.Button("Begin", root, "BEGIN MISSION 1", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(120, -262), new Vector2(470, -192), OnBegin);
            UIFactory.Button("DataSources", root, "DATA & SOURCES", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(490, -262), new Vector2(790, -192), OnData, ButtonStyle.Ghost);
            var voice = UIFactory.Button("Voice", root, VoiceText(), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(810, -262), new Vector2(1060, -192), OnVoice, ButtonStyle.Ghost);
            _voiceText = voice.GetComponentInChildren<Text>();
            var intro = UIFactory.Button("Intro", root, IntroText(), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(1080, -262), new Vector2(1300, -192), OnIntro, ButtonStyle.Ghost);
            _introText = intro.GetComponentInChildren<Text>();
            UIFactory.Label("Keys", root, "Mouse to play  ·  SPACE advances Pip  ·  L opens the father's log  ·  F11 fullscreen", 16, TextAnchor.UpperLeft,
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(120, -306), new Vector2(980, -278), Palette.TextDim).Mono();

            // The son's launch panel.
            var card = UIFactory.Window("LaunchPanel", root, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-560, -250), new Vector2(-120, 250),
                "LAUNCH PANEL", "SURVEY FLIGHT 041");
            var gauge = RiskGaugeWidget.Create(card, new Vector2(70, -78), new Vector2(300, 300));
            gauge.showBackground = false;
            gauge.Build();
            gauge.SetUnknown();
            UIFactory.Label("Quote", card, "\"Dad, I'll call you when I get there.\"", 25, TextAnchor.MiddleCenter,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(24, 64), new Vector2(-24, 112), Palette.Text);
            UIFactory.Label("Note", card, "The pilot never saw this number.", 17, TextAnchor.MiddleCenter,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(24, 26), new Vector2(-24, 58), Palette.Warn).Mono();

            UIFactory.Label("Credit", root,
                "Uses NASA open data (a snapshot, not live): see DATA & SOURCES. Reliability and mass values are game-balance values, not NASA data.", 16, TextAnchor.LowerLeft,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(120, 34), new Vector2(-120, 60), Palette.A(Palette.TextDim, 0.85f)).Mono();
        }

        DataSourcesScreen _data;
        Text _voiceText;

        static string VoiceText() => NarrationPlayer.Volume <= 0.001f ? "VOICE: OFF (subtitles)" : "VOICE: " + Mathf.RoundToInt(NarrationPlayer.Volume * 100f) + "%";

        // Cycle narration volume: 100, 75, 50, 25, off. Subtitles always stay on.
        void OnVoice()
        {
            float v = NarrationPlayer.Volume;
            NarrationPlayer.Volume = v <= 0.001f ? 1f : Mathf.Max(0f, Mathf.Round(v * 4f) / 4f - 0.25f);
            if (_voiceText != null) _voiceText.text = VoiceText();
            Sfx.Play(Sfx.Id.Click);
        }

        Text _introText;

        static string IntroText() => OpeningIntro.SkipSetting ? "INTRO: OFF" : "INTRO: ON";

        // "Skip intro" setting: OFF means the opening is not shown next time the game starts.
        void OnIntro()
        {
            OpeningIntro.SkipSetting = !OpeningIntro.SkipSetting;
            if (_introText != null) _introText.text = IntroText();
            Sfx.Play(Sfx.Id.Click);
        }

        // The opening plays once, before the title is first shown. Any key skips it.
        void Start()
        {
            NarrationPlayer.MarkOpeningPlayed();
            if (!OpeningIntro.ShouldPlay() || _canvas == null || !_canvas.enabled) return;
            _canvas.enabled = false;
            OpeningIntro.Begin(transform, () =>
            {
                var mm = MissionManager.Instance;
                if (mm == null || mm.Current == MissionIds.Boot) Show();
            });
        }

        void OnData()
        {
            if (_data == null)
            {
                _data = new GameObject("DataSources").AddComponent<DataSourcesScreen>();
                _data.transform.SetParent(transform, false);
                _data.Build();
            }
            Sfx.Play(Sfx.Id.Confirm);
            _data.Show();
        }

        void OnBegin()
        {
            if (_fade >= 0f) return;
            Sfx.Play(Sfx.Id.Confirm);
            _group.interactable = false;
            _fade = 0f;
            MissionManager.Instance.BeginRun();
        }

        void Update()
        {
            // However the run was started, the title never covers a mission.
            var mm = MissionManager.Instance;
            if (_fade < 0f && _canvas != null && _canvas.enabled && mm != null && mm.Current != MissionIds.Boot) _fade = 0f;
            if (_fade < 0f || _canvas == null || !_canvas.enabled) return;
            _fade += Time.unscaledDeltaTime / 0.35f;
            _group.alpha = 1f - Mathf.Clamp01(_fade);
            if (_fade >= 1f) { _canvas.enabled = false; _fade = -1f; }
        }

        public void Show()
        {
            _fade = -1f;
            _group.alpha = 1f; _group.interactable = true;
            _canvas.enabled = true;
        }
    }
}
