using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastSignal.EditorTools
{
    /// <summary>
    /// Renders every CameraRig shot of a scene to PNG (3D only; overlay UI is not included).
    /// Menu: LastSignal > Capture > Current scene shots. Batch: -executeMethod LastSignal.EditorTools.CaptureShots.CaptureM1M2
    /// Output folder: Captures/ next to Assets (ignored by git). Useful for the video storyboard and visual checks.
    /// </summary>
    public static class CaptureShots
    {
        const int W = 1920, H = 1080;

        [MenuItem("LastSignal/Capture/Current scene shots")]
        public static void CaptureCurrent() => CaptureScene(null);

        public static void CaptureM1M2()
        {
            CaptureScene("Assets/_Project/Scenes/M1_Ground.unity");
            CaptureScene("Assets/_Project/Scenes/M2_Preflight.unity");
        }

        static void CaptureScene(string path)
        {
            if (path != null) EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            string scene = EditorSceneManager.GetActiveScene().name;
            var cam = Camera.main;
            if (cam == null) { Debug.LogError("[Capture] No main camera in " + scene); return; }
            var rig = cam.GetComponent<CameraRig>();
            string dir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Captures");
            Directory.CreateDirectory(dir);

            // Optional: show some modules so the frame is not empty (scene state is not saved).
            var mods = GameObject.Find("Sled/Modules");
            if (mods != null)
                foreach (Transform m in mods.transform)
                    if (m.name == "Mod_E35" || m.name == "Mod_REC" || m.name == "Mod_TEL") m.gameObject.SetActive(true);

            if (rig == null || rig.shots.Count == 0) { Render(cam, Path.Combine(dir, scene + ".png")); return; }
            foreach (var s in rig.shots)
            {
                cam.transform.position = s.position;
                cam.transform.LookAt(s.lookAt);
                cam.fieldOfView = s.fov;
                Render(cam, Path.Combine(dir, scene + "_" + s.name + ".png"));
            }
        }

        static void Render(Camera cam, string file)
        {
            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.DefaultHDR) { antiAliasing = 1 };
            var prev = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render(); // warm-up frame: the first render after load can come back without shaders
            cam.Render();
            var ldr = RenderTexture.GetTemporary(W, H, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Graphics.Blit(rt, ldr);
            var old = RenderTexture.active;
            RenderTexture.active = ldr;
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            tex.Apply();
            RenderTexture.active = old;
            File.WriteAllBytes(file, tex.EncodeToPNG());
            cam.targetTexture = prev;
            RenderTexture.ReleaseTemporary(ldr);
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
            Debug.Log("[Capture] " + file);
        }
    }
}
