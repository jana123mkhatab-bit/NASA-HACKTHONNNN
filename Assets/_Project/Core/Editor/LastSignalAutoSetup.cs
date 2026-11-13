using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastSignal.EditorTools
{
    /// <summary>
    /// First open after the merge: builds the folders, the seven scenes and the core assets (menu 1),
    /// adds them to Build Settings with Boot first, and opens Boot. Does nothing once Boot.unity exists.
    /// </summary>
    [InitializeOnLoad]
    public static class LastSignalAutoSetup
    {
        const string BootPath = "Assets/_Project/Core/Scenes/Boot.unity";

        static LastSignalAutoSetup()
        {
            EditorApplication.delayCall += Run;
        }

        static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            if (File.Exists(BootPath)) return;
            Debug.Log("Last Signal: first open, creating scenes and core assets automatically.");
            LastSignalSetup.CreateFoldersScenesAndAssets();
            if (File.Exists(BootPath)) EditorSceneManager.OpenScene(BootPath);
        }
    }
}
