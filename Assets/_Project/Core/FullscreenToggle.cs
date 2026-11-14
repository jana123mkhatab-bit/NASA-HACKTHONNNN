using UnityEngine;
using UnityEngine.InputSystem;

namespace LastSignal
{
    /// <summary>
    /// F11 toggles between borderless fullscreen and a window, in every scene.
    /// (Alt+Enter also works; Unity handles it because allowFullscreenSwitch is on.)
    /// </summary>
    public class FullscreenToggle : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Create()
        {
            if (FindAnyObjectByType<FullscreenToggle>() != null) return;
            var go = new GameObject("FullscreenToggle");
            DontDestroyOnLoad(go);
            go.AddComponent<FullscreenToggle>();
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || !kb.f11Key.wasPressedThisFrame) return;

            if (Screen.fullScreenMode == FullScreenMode.Windowed)
            {
                var r = Screen.currentResolution;
                Screen.SetResolution(r.width, r.height, FullScreenMode.FullScreenWindow);
            }
            else
            {
                int w = Mathf.RoundToInt(Screen.currentResolution.width * 0.75f);
                int h = Mathf.RoundToInt(Screen.currentResolution.height * 0.75f);
                Screen.SetResolution(w, h, FullScreenMode.Windowed);
            }
        }
    }
}
