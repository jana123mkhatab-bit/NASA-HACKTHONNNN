using System.Collections.Generic;
using System.IO;
using LastSignal.M3;
using LastSignal.Ship.EditorTools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastSignal.M3.EditorTools
{
    /// <summary>
    /// Menus for person 3:
    ///   Last Signal > M3 - Set up M3_DeepSpace scene   (removes the StubMission, adds M3Director, wires ship + CSV)
    ///   Last Signal > M3 - Create M3 Debrief card      (writes Missions/M3/Debrief_M3.asset, optionally installs it)
    /// Safe to run again.
    /// </summary>
    public static class M3SceneBuilder
    {
        const string ScenePath = "Assets/_Project/Core/Scenes/M3_DeepSpace.unity";
        const string DataCsvPath = "Assets/_Project/Data/M3_SignalPath.csv";
        const string SampleCsvPath = "Assets/_Project/Missions/M3/Resources/M3_SignalPath_Sample.csv";
        const string CardPath = "Assets/_Project/Missions/M3/Debrief_M3.asset";
        const string CoreCardPath = "Assets/_Project/Core/Resources/Debrief/M3.asset";

        [MenuItem("Last Signal/M3 - Set up M3_DeepSpace scene")]
        public static void SetupScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            GameObject shipPrefab = ShipPrefabBuilder.EnsurePrefab();

            Scene scene;
            bool isNew = !File.Exists(ScenePath);
            if (isNew)
            {
                Debug.LogWarning("M3: " + ScenePath + " did not exist (run Last Signal > 1 first). Creating it.");
                scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            }
            else
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            M3Director director = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (StubMission stub in root.GetComponentsInChildren<StubMission>(true))
                {
                    if (stub.gameObject.GetComponents<Component>().Length <= 2) Object.DestroyImmediate(stub.gameObject);
                    else Object.DestroyImmediate(stub);
                }
            }
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (director == null) director = root.GetComponentInChildren<M3Director>(true);
            }
            if (director == null) director = new GameObject("M3_Director").AddComponent<M3Director>();

            director.shipPrefab = shipPrefab;
            director.signalPathCsv = FindSignalCsv();
            EditorUtility.SetDirty(director);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            if (isNew) AddToBuildSettings(ScenePath);

            string csvName = director.signalPathCsv != null ? AssetDatabase.GetAssetPath(director.signalPathCsv) : "none";
            Debug.Log("M3: scene ready. Ship = " + AssetDatabase.GetAssetPath(shipPrefab) + ", Signal CSV = " + csvName + ". Press Play.");
        }

        static TextAsset FindSignalCsv()
        {
            TextAsset csv = AssetDatabase.LoadAssetAtPath<TextAsset>(DataCsvPath);
            if (csv != null) return csv;
            if (AssetDatabase.IsValidFolder("Assets/_Project/Data"))
            {
                foreach (string guid in AssetDatabase.FindAssets("t:TextAsset", new[] { "Assets/_Project/Data" }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!path.EndsWith(".csv")) continue;
                    TextAsset t = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                    if (t != null && t.text.Contains(SignalPathData.ShipRelay) && t.text.Contains(SignalPathData.RelayEarth)) return t;
                }
            }
            Debug.LogWarning("M3: no Horizons CSV in Assets/_Project/Data yet (expected " + DataCsvPath + "). Using the SAMPLE file.");
            return AssetDatabase.LoadAssetAtPath<TextAsset>(SampleCsvPath);
        }

        static void AddToBuildSettings(string path)
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (EditorBuildSettingsScene s in scenes)
            {
                if (s.path == path) return;
            }
            scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        // ---------------------------------------------------------------- debrief

        [MenuItem("Last Signal/M3 - Create M3 Debrief card")]
        public static void CreateDebriefCard()
        {
            DebriefCard card = AssetDatabase.LoadAssetAtPath<DebriefCard>(CardPath);
            if (card == null)
            {
                card = ScriptableObject.CreateInstance<DebriefCard>();
                AssetDatabase.CreateAsset(card, CardPath);
            }
            Fill(card);
            EditorUtility.SetDirty(card);
            AssetDatabase.SaveAssets();
            Debug.Log("M3: wrote " + CardPath + ". Person 6 verifies the wording and sources.");

            DebriefCard core = AssetDatabase.LoadAssetAtPath<DebriefCard>(CoreCardPath);
            if (core == null)
            {
                Debug.Log("M3: " + CoreCardPath + " does not exist yet (person 1's menu 1 creates it). Run this menu again after that.");
                return;
            }
            bool placeholder = core.concept == null || core.concept.StartsWith("TODO");
            string question = placeholder
                ? "Copy the M3 debrief text into Core's slot (" + CoreCardPath + ")?\n\nThat file is in person 1's folder; tell person 1 and person 6."
                : "Core's M3 card already has non-placeholder text. Overwrite it with person 3's version?\n\nTell person 1 and person 6.";
            if (EditorUtility.DisplayDialog("M3 Debrief card", question, "Copy", "Cancel"))
            {
                Fill(core);
                EditorUtility.SetDirty(core);
                AssetDatabase.SaveAssets();
                Debug.Log("M3: copied the debrief text into " + CoreCardPath);
            }
        }

        static void Fill(DebriefCard c)
        {
            c.missionId = MissionIds.M3;
            c.title = "Debrief: " + MissionIds.Title(MissionIds.M3);
            c.concept = "Single points of failure in deep-space communication. If every message must pass through one relay, losing that relay "
                + "cuts you off completely. And because radio travels at the speed of light, the farther away you are, the older every message is.";
            c.nasaSource = "NASA Deep Space Network (DSN): https://www.nasa.gov/communicating-with-missions/dsn/\n"
                + "NASA/JPL Horizons System (distances and one-way light time on the Signal Path HUD): https://ssd.jpl.nasa.gov/horizons/\n"
                + "(Person 6: verify the links, the Voyager 2 / DSS-43 example, and the wording.)";
            c.body = "Your path home ran ship -> relay -> Earth. When the relay was destroyed, repairing your own antenna did not help: nobody was left to hear it.\n\n"
                + "Real missions design this risk out with redundancy. The DSN has three antenna complexes (Goldstone, Madrid, Canberra) about 120 degrees apart, "
                + "so a spacecraft is always in view of one. Mars rovers can send data through several orbiters instead of just one.\n\n"
                + "A real single point of failure: Voyager 2 can only be commanded from one dish, DSS-43 in Canberra. While it was upgraded in 2020, "
                + "Voyager 2 kept sending data but could not receive commands for months.\n\n"
                + "One-way light time = distance / speed of light. The delays you saw come from JPL Horizons distances. "
                + "Thrust, mass, power and supply numbers in this mission are game-balance values.";
        }
    }
}
