using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace LastSignal.Tests
{
    /// <summary>
    /// Plays the real flow (title, M1 bench, M2, Planner) and saves full frames WITH the UI to Captures/.
    /// Explicit: run it by name from the Test Runner, or in batch with -testFilter CaptureFlow.
    /// Any error logged during the flow fails the test.
    /// </summary>
    [Explicit, Category("Capture")]
    public class CaptureFlowTests
    {
        const int W = 1920, H = 1080;

        [UnityTest]
        public IEnumerator CaptureFlow()
        {
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/M1_Ground.unity");
            yield return new EnterPlayMode();
            yield return Wait(2f);
            Capture("ui_title");

            MissionManager.Instance.BeginRun();
            yield return Wait(1.0f);
            Capture("ui_m1_titlecard");
            yield return Wait(4.5f);
            Capture("ui_m1_bench");

            MissionManager.Instance.GoTo(MissionIds.M2);
            yield return Wait(5.5f);
            Capture("ui_m2");

            // Mission scripts live in Assembly-CSharp, which test assemblies cannot reference: use reflection.
            var m2Type = System.Type.GetType("LastSignal.M2PreflightMission, Assembly-CSharp");
            var m2 = m2Type != null ? Object.FindFirstObjectByType(m2Type) : null;
            Assert.IsNotNull(m2, "M2 mission controller missing");
            m2Type.GetMethod("OpenSonsAssessment", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(m2, null);
            yield return Wait(1.5f);
            Capture("ui_planner_son");
            var plannerType = System.Type.GetType("LastSignal.PlannerPanel, Assembly-CSharp");
            var planner = Object.FindFirstObjectByType(plannerType);
            float p = (float)plannerType.GetProperty("Probability").GetValue(planner);
            Assert.That(p, Is.InRange(0.09f, 0.11f), "son's plan should score about 10%");

            yield return new ExitPlayMode();
        }

        static IEnumerator Wait(float seconds)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < end) yield return null;
        }

        static void Capture(string name)
        {
            var cam = Camera.main;
            Assert.IsNotNull(cam, "no main camera");
            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.DefaultHDR);
            cam.targetTexture = rt;

            // Overlay canvases never reach a camera texture, so move them in front of the camera for the shot.
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (var c in canvases)
                if (c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay)
                { c.renderMode = RenderMode.ScreenSpaceCamera; c.worldCamera = cam; c.planeDistance = cam.nearClipPlane + 0.05f; }
            Canvas.ForceUpdateCanvases();
            cam.Render();
            cam.Render();

            var ldr = RenderTexture.GetTemporary(W, H, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Graphics.Blit(rt, ldr);
            var old = RenderTexture.active;
            RenderTexture.active = ldr;
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            tex.Apply();
            RenderTexture.active = old;

            foreach (var c in canvases)
                if (c != null && c.renderMode == RenderMode.ScreenSpaceCamera && c.worldCamera == cam)
                { c.renderMode = RenderMode.ScreenSpaceOverlay; c.worldCamera = null; }
            cam.targetTexture = null;

            string dir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Captures");
            Directory.CreateDirectory(dir);
            File.WriteAllBytes(Path.Combine(dir, name + ".png"), tex.EncodeToPNG());
            RenderTexture.ReleaseTemporary(ldr);
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
        }
    }
}
