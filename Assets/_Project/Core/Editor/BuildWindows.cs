using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace LastSignal.EditorTools
{
    /// <summary>
    /// Windows player build for the CLI:
    ///   Unity.exe -batchmode -quit -projectPath . -executeMethod LastSignal.EditorTools.BuildWindows.Build -logFile Builds/build.log
    /// Builds every enabled scene in Build Settings to Builds/Windows. Exits with code 1 if the build fails.
    /// </summary>
    public static class BuildWindows
    {
        public const string OutputPath = "Builds/Windows/The Last Known Signal.exe";

        [MenuItem("Last Signal/Build/Windows Player")]
        public static void Build()
        {
            string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            BuildPlayerOptions o = new BuildPlayerOptions();
            o.scenes = scenes;
            o.locationPathName = OutputPath;
            o.target = BuildTarget.StandaloneWindows64;
            o.targetGroup = BuildTargetGroup.Standalone;
            o.options = BuildOptions.None;
            BuildReport report = BuildPipeline.BuildPlayer(o);
            BuildSummary sum = report.summary;
            Debug.Log("[BuildWindows] " + sum.result + "  scenes=" + scenes.Length + "  size=" + sum.totalSize + " bytes  errors=" + sum.totalErrors
                + "  time=" + sum.totalTime + "  output=" + OutputPath);
            if (Application.isBatchMode) EditorApplication.Exit(sum.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
