using UnityEngine;
using UnityEngine.UI;

namespace LastSignal.M3
{
    /// <summary>
    /// Signal Path HUD (top left): SHIP -> RELAY -> EARTH with the one-way light delay on each link.
    /// Delays come from the Horizons CSV (SignalPathData), never from code. When PlayerState.signalPathUp
    /// goes false the chain breaks: the links turn red and dashed and the relay is crossed out.
    /// </summary>
    public class SignalPathHud : MonoBehaviour
    {
        const int Segments = 12;
        const float LinkWidth = 150f;

        SignalPathData _data;
        Image[] _linkA;
        Image[] _linkB;
        Text _delayA;
        Text _delayB;
        Text _relayNode;
        Image _relayBox;
        Text _summary;
        Text _source;
        Text _breakA;
        Text _breakB;
        bool _shownUp = true;
        bool _first = true;

        public static SignalPathHud Create(Transform parent, SignalPathData data)
        {
            GameObject go = new GameObject("SignalPathHud");
            go.transform.SetParent(parent, false);
            SignalPathHud hud = go.AddComponent<SignalPathHud>();
            hud._data = data;
            hud.Build();
            return hud;
        }

        void Build()
        {
            Canvas canvas = UIFactory.CreateCanvas("SignalPath", 35, transform);
            Image panel = M3Ui.Box("Panel", canvas.transform, UIFactory.PanelColor, new Vector2(0f, 1f), new Vector2(24f, -108f), new Vector2(640f, 150f));
            Transform p = panel.transform;

            M3Ui.Label("Title", p, "SIGNAL PATH", 18, UIFactory.Muted, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(14f, -8f), new Vector2(200f, 24f));
            _source = M3Ui.Label("Source", p, "", 16, UIFactory.Muted, TextAnchor.UpperRight, new Vector2(1f, 1f), new Vector2(-14f, -10f), new Vector2(420f, 22f));

            float y = -62f;
            Node(p, "SHIP", 14f, y, out _);
            _relayNode = Node(p, "RELAY", 14f + 90f + LinkWidth, y, out _relayBox);
            Node(p, "EARTH", 14f + 2f * (90f + LinkWidth), y, out _);

            _linkA = Link(p, 14f + 90f, y);
            _linkB = Link(p, 14f + 90f + LinkWidth + 90f, y);
            _delayA = M3Ui.Label("DelayA", p, "", 18, UIFactory.TextColor, TextAnchor.LowerCenter, new Vector2(0f, 1f), new Vector2(14f + 90f, y + 30f), new Vector2(LinkWidth, 24f));
            _delayB = M3Ui.Label("DelayB", p, "", 18, UIFactory.TextColor, TextAnchor.LowerCenter, new Vector2(0f, 1f), new Vector2(14f + 180f + LinkWidth, y + 30f), new Vector2(LinkWidth, 24f));
            _breakA = M3Ui.Label("BreakA", p, "X", 30, UIFactory.Bad, TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(14f + 90f, y - 2f), new Vector2(LinkWidth, 36f));
            _breakB = M3Ui.Label("BreakB", p, "X", 30, UIFactory.Bad, TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(14f + 180f + LinkWidth, y - 2f), new Vector2(LinkWidth, 36f));

            _summary = M3Ui.Label("Summary", p, "", 18, UIFactory.TextColor, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(14f, -112f), new Vector2(612f, 34f));

            if (_data == null || !_data.IsValid)
            {
                _source.text = "NO SIGNAL DATA (CSV missing or broken)";
                _source.color = UIFactory.Bad;
                _delayA.text = "?";
                _delayB.text = "?";
                if (_data != null && _data.errors.Count > 0) Debug.LogWarning("M3 Signal Path CSV: " + string.Join(" ", _data.errors.ToArray()));
            }
            else
            {
                _source.text = (_data.isSample ? "SAMPLE DATA, not Horizons yet" : "src: JPL Horizons") + " (" + _data.fileName + ")";
                _source.color = _data.isSample ? UIFactory.Warn : UIFactory.Muted;
                _delayA.text = SignalPathData.FormatDelay(_data.Get(SignalPathData.ShipRelay).oneWaySeconds);
                _delayB.text = SignalPathData.FormatDelay(_data.Get(SignalPathData.RelayEarth).oneWaySeconds);
            }
        }

        Text Node(Transform parent, string name, float x, float y, out Image box)
        {
            box = M3Ui.Box(name + "_Box", parent, new Color(0.10f, 0.30f, 0.32f, 1f), new Vector2(0f, 1f), new Vector2(x, y), new Vector2(90f, 34f));
            Text t = M3Ui.Label(name, box.transform, name, 18, Color.white, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(90f, 34f));
            return t;
        }

        Image[] Link(Transform parent, float x, float y)
        {
            Image[] segs = new Image[Segments];
            float step = LinkWidth / Segments;
            for (int i = 0; i < Segments; i++)
            {
                segs[i] = M3Ui.Box("Seg", parent, UIFactory.Accent, new Vector2(0f, 1f), new Vector2(x + i * step, y - 15f), new Vector2(step, 4f));
            }
            return segs;
        }

        void Update()
        {
            bool up = PlayerState.Current.signalPathUp;
            if (up == _shownUp && !_first) return;
            _first = false;
            _shownUp = up;
            Apply(up);
        }

        void Apply(bool up)
        {
            float step = LinkWidth / Segments;
            Color c = up ? UIFactory.Accent : UIFactory.Bad;
            Image[][] links = { _linkA, _linkB };
            for (int l = 0; l < links.Length; l++)
            {
                for (int i = 0; i < links[l].Length; i++)
                {
                    links[l][i].color = c;
                    links[l][i].rectTransform.sizeDelta = new Vector2(up ? step : step * 0.5f, up ? 4f : 3f);
                }
            }
            _breakA.gameObject.SetActive(!up);
            _breakB.gameObject.SetActive(!up);
            _relayBox.color = up ? new Color(0.10f, 0.30f, 0.32f, 1f) : new Color(0.45f, 0.08f, 0.08f, 1f);
            _relayNode.text = up ? "RELAY" : "RELAY X";
            _delayA.color = up ? UIFactory.TextColor : UIFactory.Muted;
            _delayB.color = up ? UIFactory.TextColor : UIFactory.Muted;

            if (!up)
            {
                _summary.text = "NO PATH HOME: the relay was a single point of failure.";
                _summary.color = UIFactory.Bad;
            }
            else if (_data != null && _data.IsValid)
            {
                double oneWay = _data.ShipToEarthSeconds;
                _summary.text = "Ship -> Earth one way " + SignalPathData.FormatDelay(oneWay) + "   |   round trip " + SignalPathData.FormatDelay(oneWay * 2.0);
                _summary.color = UIFactory.TextColor;
            }
            else
            {
                _summary.text = "Light delay unknown.";
                _summary.color = UIFactory.Warn;
            }
        }
    }
}
