using System;
using UnityEngine;
using UnityEngine.UI;

namespace LastSignal.Core
{
    /// <summary>Title screen in the Boot scene: Start button, plus debug jump buttons in editor/debug builds.</summary>
    public class TitleScreenUI : MonoBehaviour
    {
        GameObject _root;

        public static TitleScreenUI Create(Transform parent, Action onStart, Action<string> onJump)
        {
            GameObject go = new GameObject("TitleScreenUI");
            go.transform.SetParent(parent, false);
            TitleScreenUI ui = go.AddComponent<TitleScreenUI>();
            ui.Build(onStart, onJump);
            return ui;
        }

        void Build(Action onStart, Action<string> onJump)
        {
            Canvas canvas = UIFactory.CreateCanvas("Title", 70, transform);
            _root = canvas.gameObject;

            Image back = UIFactory.CreatePanel("Back", canvas.transform, new Color(0.02f, 0.03f, 0.07f, 0.55f));
            UIFactory.SetStretch(back.rectTransform, 0f, 0f, 0f, 0f);

            Text title = UIFactory.CreateText("Title", canvas.transform, "THE LAST KNOWN SIGNAL", 84, UIFactory.TextColor, TextAnchor.MiddleCenter);
            UIFactory.SetCenter(title.rectTransform, 0f, 220f, 1600f, 120f);

            Text sub = UIFactory.CreateText("Sub", canvas.transform, "Your father's last signal is still out there.", 32, UIFactory.Muted, TextAnchor.MiddleCenter);
            UIFactory.SetCenter(sub.rectTransform, 0f, 130f, 1400f, 50f);

            Button start = UIFactory.CreateButton("Start", canvas.transform, "Start", delegate { if (onStart != null) onStart(); });
            UIFactory.SetCenter(start.GetComponent<RectTransform>(), 0f, 0f, 320f, 70f);

            if (Application.isEditor || Debug.isDebugBuild)
            {
                string[] ids = { MissionIds.M1, MissionIds.M2, MissionIds.M3, MissionIds.M4, MissionIds.Final, MissionIds.DesignMode };
                for (int i = 0; i < ids.Length; i++)
                {
                    string id = ids[i];
                    Button b = UIFactory.CreateButton("Jump_" + id, canvas.transform, "DEV: " + id, delegate { if (onJump != null) onJump(id); });
                    UIFactory.SetCenter(b.GetComponent<RectTransform>(), -500f + i * 200f, -300f, 180f, 50f);
                }
            }
        }

        public void SetVisible(bool v) { if (_root != null) _root.SetActive(v); }
    }
}
