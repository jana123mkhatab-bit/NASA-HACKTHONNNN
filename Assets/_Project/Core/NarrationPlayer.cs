using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace LastSignal
{
    /// <summary>
    /// Plays Resources/LastSignal/Narration/&lt;id&gt;.wav with synced subtitles. The text, speaker and fallback
    /// duration come from Resources/LastSignal/Narration/narration_script.csv. A missing clip is not an error:
    /// the subtitle shows for the row's seconds instead. Volume (0..1) is saved in PlayerPrefs.
    /// Audio is never generated here; the clips are recorded or text-to-speech files added by a person.
    /// </summary>
    public class NarrationPlayer : MonoBehaviour
    {
        public const string ScriptPath = "LastSignal/Narration/narration_script";
        public const string ClipFolder = "LastSignal/Narration/";
        const string VolumeKey = "ls_narration_volume";

        public class Row { public string id, speaker, text; public float seconds; }

        public static NarrationPlayer Instance { get; private set; }

        static float _volume = -1f;
        public static float Volume
        {
            get
            {
                if (_volume < 0f) _volume = Mathf.Clamp01(Safe(() => PlayerPrefs.GetFloat(VolumeKey, 0.8f), 0.8f));
                return _volume;
            }
            set
            {
                _volume = Mathf.Clamp01(value);
                Safe(() => { PlayerPrefs.SetFloat(VolumeKey, _volume); return 0; }, 0);
                if (Instance != null && Instance._src != null) Instance._src.volume = _volume;
            }
        }

        static T Safe<T>(System.Func<T> f, T fallback) { try { return f(); } catch { return fallback; } }

        // ---------- script table ----------
        static List<Row> _rows;
        static readonly Dictionary<string, Row> ById = new Dictionary<string, Row>();
        static readonly Dictionary<string, Row> ByText = new Dictionary<string, Row>();

        public static IReadOnlyList<Row> Rows { get { Load(); return _rows; } }

        public static Row Find(string id) { Load(); Row r; return id != null && ById.TryGetValue(id, out r) ? r : null; }
        public static Row FindByText(string text) { Load(); Row r; return !string.IsNullOrEmpty(text) && ByText.TryGetValue(Norm(text), out r) ? r : null; }
        static string Norm(string s) => s.Trim().Replace("’", "'");

        static void Load()
        {
            if (_rows != null) return;
            _rows = new List<Row>();
            ById.Clear(); ByText.Clear();
            var ta = Resources.Load<TextAsset>(ScriptPath);
            if (ta == null) return;
            _rows = Parse(ta.text);
            foreach (var r in _rows) { ById[r.id] = r; if (!ByText.ContainsKey(Norm(r.text))) ByText[Norm(r.text)] = r; }
        }

        /// <summary>Tests only: reload the script next time it is used.</summary>
        public static void ResetCache() { _rows = null; }

        /// <summary>Parses id,speaker,text,seconds. Text may be quoted and contain commas and "" quotes.</summary>
        public static List<Row> Parse(string csv)
        {
            var rows = new List<Row>();
            var cells = new List<string>();
            var sb = new StringBuilder();
            bool quoted = false, first = true;
            for (int i = 0; i <= csv.Length; i++)
            {
                char c = i < csv.Length ? csv[i] : '\n';
                if (quoted)
                {
                    if (c == '"') { if (i + 1 < csv.Length && csv[i + 1] == '"') { sb.Append('"'); i++; } else quoted = false; }
                    else sb.Append(c);
                    continue;
                }
                if (c == '"') quoted = true;
                else if (c == ',') { cells.Add(sb.ToString()); sb.Length = 0; }
                else if (c == '\n' || c == '\r')
                {
                    if (sb.Length > 0 || cells.Count > 0)
                    {
                        cells.Add(sb.ToString()); sb.Length = 0;
                        if (first) first = false;
                        else if (cells.Count >= 4 && cells[0].Trim().Length > 0)
                        {
                            float s; float.TryParse(cells[3], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out s);
                            rows.Add(new Row { id = cells[0].Trim(), speaker = cells[1].Trim(), text = cells[2].Trim(), seconds = s > 0f ? s : 4f });
                        }
                        cells.Clear();
                    }
                }
                else sb.Append(c);
            }
            return rows;
        }

        // ---------- hooks ----------
        public static readonly string[] OpeningIds = { "open_01", "open_02", "open_03", "open_04", "open_05", "open_06", "open_07", "open_08" };
        static bool _openingDone;

        /// <summary>The opening intro plays (or skips) the open_ lines itself, so M1 start must not repeat them.</summary>
        public static void MarkOpeningPlayed() { _openingDone = true; }

        public bool Busy { get; private set; }
        public string CurrentId { get; private set; }
        public string CurrentSubtitle => _sub != null ? _sub.text : "";
        public bool LastClipMissing { get; private set; }

        AudioSource _src;
        Canvas _canvas;
        Text _sub;
        readonly Queue<string> _queue = new Queue<string>();
        Coroutine _routine;

        public static NarrationPlayer Ensure()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("NarrationPlayer");
            var mm = MissionManager.Instance;
            if (mm != null) go.transform.SetParent(mm.transform, false); else DontDestroyOnLoad(go);
            var p = go.AddComponent<NarrationPlayer>();
            p.Build();
            return p;
        }

        void Awake() { if (Instance == null) Instance = this; }

        public void Build()
        {
            Instance = this;
            _src = gameObject.AddComponent<AudioSource>();
            _src.playOnAwake = false; _src.spatialBlend = 0f; _src.volume = Volume;
            _canvas = UIFactory.CreateCanvas("NarrationSubtitles", 40);
            _canvas.transform.SetParent(transform, false);
            var root = (RectTransform)_canvas.transform;
            var bar = UIFactory.Box("Bar", root, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-520, 184), new Vector2(520, 244), Palette.A(Palette.Ink, 0.72f));
            bar.GetComponent<Image>().raycastTarget = false;
            _sub = UIFactory.Label("Subtitle", bar, "", 24, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(14, 4), new Vector2(-14, -4));
            _sub.supportRichText = true;
            _sub.raycastTarget = false;
            _canvas.enabled = false;
        }

        void OnEnable()
        {
            MissionEvents.OnMissionStart += HandleStart;
            MissionEvents.OnMissionComplete += HandleComplete;
        }

        void OnDisable()
        {
            MissionEvents.OnMissionStart -= HandleStart;
            MissionEvents.OnMissionComplete -= HandleComplete;
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        void HandleStart(string id)
        {
            var mm = MissionManager.Instance;
            if (id == MissionIds.M1 && !_openingDone && (mm == null || mm.Retries == 0))
            {
                _openingDone = true;
                PlaySequence(OpeningIds);
            }
        }

        void HandleComplete(string id)
        {
            if (id == MissionIds.Final) { PlaySequence("final_09", "final_10"); return; }
            string prefix = id.ToLowerInvariant() + "_outro_";
            var ids = new List<string>();
            foreach (var r in Rows) if (r.id.StartsWith(prefix)) ids.Add(r.id);
            if (ids.Count > 0) PlaySequence(ids.ToArray());
        }

        // ---------- public API ----------

        /// <summary>Play one row (clip + subtitle). Queued behind whatever is speaking.</summary>
        public static void Play(string id) { if (Instance != null) Instance.Enqueue(id); }

        public static void PlaySequence(params string[] ids) { if (Instance != null) foreach (var i in ids) Instance.Enqueue(i); }

        /// <summary>
        /// For lines another system already shows as text (Pip's box, the finale subtitles): play only the audio of
        /// the script row with exactly this text, no second subtitle. Skipped while a narration is speaking.
        /// </summary>
        public static void PlayMatching(string text)
        {
            var p = Instance;
            if (p == null || p.Busy) return;
            var row = FindByText(text);
            if (row == null) return;
            var clip = Resources.Load<AudioClip>(ClipFolder + row.id);
            if (clip != null && Volume > 0f) { p._src.Stop(); p._src.volume = Volume; p._src.clip = clip; p._src.Play(); }
        }

        /// <summary>Length in seconds of the clip for the row with exactly this text (0 if none), so timed scenes can wait for the voice.</summary>
        public static float ClipLength(string text)
        {
            var row = FindByText(text);
            var clip = row != null ? Resources.Load<AudioClip>(ClipFolder + row.id) : null;
            return clip != null && Volume > 0f ? clip.length : 0f;
        }

        public void Stop()
        {
            _queue.Clear();
            if (_routine != null) StopCoroutine(_routine);
            _routine = null;
            if (_src != null) _src.Stop();
            Busy = false; CurrentId = null;
            if (_canvas != null) _canvas.enabled = false;
        }

        void Enqueue(string id)
        {
            if (Find(id) == null) { Debug.LogWarning("[Narration] no script row for id '" + id + "'"); return; }
            _queue.Enqueue(id);
            if (_routine == null) _routine = StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            while (_queue.Count > 0)
            {
                var row = Find(_queue.Dequeue());
                if (row == null) continue;
                Busy = true; CurrentId = row.id;
                var clip = Resources.Load<AudioClip>(ClipFolder + row.id);
                LastClipMissing = clip == null;
                float dur = clip != null ? clip.length + 0.3f : row.seconds;
                yield return null; // let a Pip box that opens on the same frame claim PIP lines first
                var mm = MissionManager.Instance;
                bool pipBoxShowsIt = row.speaker == "PIP" && mm != null && mm.pipUI != null && mm.pipUI.IsPlaying;
                if (clip != null && Volume > 0f) { _src.volume = Volume; _src.clip = clip; _src.Play(); }
                if (!pipBoxShowsIt)
                {
                    _sub.text = Subtitle(row);
                    _canvas.enabled = true;
                }
                for (float t = 0f; t < dur; t += Time.unscaledDeltaTime) yield return null;
                _canvas.enabled = false;
            }
            Busy = false; CurrentId = null; _routine = null;
        }

        public static string Subtitle(Row r)
        {
            return r.speaker == "NARRATOR" || string.IsNullOrEmpty(r.speaker)
                ? "<i>" + r.text + "</i>"
                : "<color=#FFD24D><b>" + r.speaker + "</b></color>   " + r.text;
        }
    }
}
