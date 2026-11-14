using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace LastSignal
{
    /// <summary>
    /// Safety net for UI clicks. The EventSystem normally uses the legacy StandaloneInputModule. If the new
    /// Input System sees a mouse press that legacy Input does not (legacy backend off or dead), this swaps the
    /// EventSystem over to InputSystemUIInputModule with default actions so buttons keep working.
    /// </summary>
    public sealed class ClickDebug : MonoBehaviour
    {
        bool _switched;
        string _last = "no click seen yet";
        int _clicks;

        void OnGUI()
        {
            var es = EventSystem.current;
            string mod = es != null && es.currentInputModule != null ? es.currentInputModule.GetType().Name : "NONE";
            GUI.color = Color.yellow;
            GUI.Label(new Rect(10, 10, 1200, 120), "[UI debug] module=" + mod + " mouse=" + (Mouse.current != null) + " clicks=" + _clicks + " | " + _last);
        }
        static readonly List<RaycastResult> Hits = new List<RaycastResult>();

        void Awake() { Debug.Log("[UI] click watchdog alive. Mouse.current=" + (Mouse.current != null)); }

        void LogHits(Vector2 pos, bool legacySees)
        {
            _clicks++;
            var es = EventSystem.current;
            var sb = new StringBuilder("[UI] click at " + pos + " legacySees=" + legacySees);
            if (es == null) { _last = sb.Append(" EventSystem.current=NULL").ToString(); return; }
            sb.Append(" module=").Append(es.currentInputModule != null ? es.currentInputModule.GetType().Name : "NONE")
              .Append(" timeScale=").Append(Time.timeScale).Append(" hits: ");
            Hits.Clear();
            es.RaycastAll(new PointerEventData(es) { position = pos }, Hits);
            if (Hits.Count == 0) sb.Append("NOTHING");
            for (int i = 0; i < Hits.Count && i < 6; i++)
            {
                var c = Hits[i].gameObject.GetComponentInParent<Canvas>();
                sb.Append(i == 0 ? "" : " > ").Append(Hits[i].gameObject.name).Append(" [").Append(c != null ? c.rootCanvas.name : "?").Append("]");
            }
            _last = sb.ToString();
            Debug.Log(_last);
        }

        void Update()
        {
            var m = Mouse.current;
            if (m == null || !m.leftButton.wasPressedThisFrame) return;
            if (_switched) { LogHits(m.position.ReadValue(), false); return; }

            bool legacySees;
            try { legacySees = Input.GetMouseButtonDown(0); }
            catch (System.InvalidOperationException) { legacySees = false; }
            LogHits(m.position.ReadValue(), legacySees);
            if (legacySees) return; // legacy path works, nothing to do

            _switched = true;
            var go = gameObject;
            var legacy = go.GetComponent<StandaloneInputModule>();
            if (legacy != null) legacy.enabled = false;
            var ui = go.GetComponent<InputSystemUIInputModule>();
            if (ui == null) ui = go.AddComponent<InputSystemUIInputModule>();
            ui.AssignDefaultActions();
            ui.enabled = true;
            Debug.LogWarning("[UI] Legacy Input is not receiving mouse clicks; switched EventSystem to InputSystemUIInputModule.");
        }
    }
}
