using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastSignal.M3.Tests
{
    /// <summary>Guards the M3 wiring: the real M3_DeepSpace scene must end up running M3Director, not the stub.</summary>
    public class M3WiringTests
    {
        const string ScenePath = "Assets/_Project/Scenes/M3_DeepSpace.unity";

        [Test]
        public void M3Scene_IsInBuildSettings_AndMatchesMissionId()
        {
            Assert.AreEqual("M3_DeepSpace", MissionIds.SceneFor(MissionIds.M3));
            Assert.IsTrue(EditorBuildSettings.scenes.Any(s => s.enabled && s.path == ScenePath), ScenePath + " missing from Build Settings");
        }

        [Test]
        public void Bootstrap_ReplacesStubWithDirector_InRealScene()
        {
            if (SceneManager.GetSceneByPath(ScenePath).isLoaded)
                Assert.Ignore("M3_DeepSpace is open in the Editor; close it to run this test without touching your scene.");

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                M3Director director = M3Bootstrap.Install(scene);

                Assert.IsNotNull(director);
                Assert.AreEqual(scene, director.gameObject.scene, "director must live in the M3 scene so it unloads with it");
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    Assert.IsEmpty(root.GetComponentsInChildren<StubMission>(true), "StubMission still present on " + root.name);
                    foreach (CameraOrbit orbit in root.GetComponentsInChildren<CameraOrbit>(true))
                        Assert.IsFalse(orbit.enabled, "CameraOrbit would fight ChaseCamera on " + orbit.name);
                }
                Assert.AreSame(director, M3Bootstrap.Install(scene), "second install must not add a second director");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);   // discard: the scene file is never saved
            }
        }
    }
}
