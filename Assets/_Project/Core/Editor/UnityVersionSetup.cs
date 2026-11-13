using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace LastSignal.EditorTools
{
    /// <summary>
    /// Menu 0: prepares the project for Unity 6 with the Universal Render Pipeline (URP).
    /// Run it first. It never changes anything that is already correct.
    /// </summary>
    public static class UnityVersionSetup
    {
        const string UrpPackage = "com.unity.render-pipelines.universal";

        [MenuItem("Last Signal/0 - Install URP and set Unity 6 project settings (run first)", false, 0)]
        public static void Run()
        {
            Debug.Log("Last Signal: Unity version is " + Application.unityVersion + ".");

            if (PlayerSettings.colorSpace != ColorSpace.Linear)
            {
                PlayerSettings.colorSpace = ColorSpace.Linear;
                Debug.Log("Last Signal: color space set to Linear (needed for correct HDR, bloom and lighting).");
            }

            bool hasUrp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null
                          || System.Type.GetType("UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset, Unity.RenderPipelines.Universal.Runtime") != null;
            if (!hasUrp)
            {
                Debug.Log("Last Signal: installing the Universal RP package. Wait for Unity to finish compiling, then run menu 0b.");
                Client.Add(UrpPackage);
                return;
            }
            Debug.Log("Last Signal: URP is installed. Next, run Last Signal > 0b - Configure URP asset for maximum visuals.");
        }
    }
}
