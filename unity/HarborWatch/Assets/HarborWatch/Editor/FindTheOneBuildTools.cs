#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using HarborWatch.Core;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace HarborWatch.Editor
{
    /// <summary>
    /// Find the One: verification and an Android development build that starts
    /// in FindTheOne.unity. Leaves the existing Harbor Watch build order alone.
    /// </summary>
    public static class FindTheOneBuildTools
    {
        const string Scene = "Assets/Scenes/FindTheOne.unity";

        [MenuItem("Harbor Watch/Find The One/Verify")]
        public static void VerifyMenu()
        {
            bool ok = Validate();
            EditorUtility.DisplayDialog("Find the One verification",
                ok ? "Scene, crowd clips and case engine checks passed.\nOpen Assets/Scenes/FindTheOne and press Play."
                   : "Something is missing. Open the Console for details.", "OK");
        }

        static bool Validate()
        {
            bool ok = true;
            if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(Scene))
            {
                Debug.LogError("Find the One: " + Scene + " is missing.");
                ok = false;
            }
            foreach (var clip in new[] { "Assets/HarborWatch/Resources/CrowdWalk.fbx", "Assets/HarborWatch/Resources/CrowdJog.fbx" })
                if (AssetDatabase.LoadAllAssetsAtPath(clip).Length == 0)
                {
                    Debug.LogError("Find the One: missing " + clip);
                    ok = false;
                }
            // Every campaign case must have exactly one courier.
            for (int c = 0; c < CaseEngine.Cases.Length; c++)
                for (uint seed = 1; seed <= 50; seed++)
                {
                    var k = CaseEngine.Generate(seed, CaseEngine.Cases[c]);
                    if (CaseEngine.Candidates(k, k.Clues.Length).Count != 1)
                    {
                        Debug.LogError("Find the One: case " + (c + 1) + " seed " + seed + " is ambiguous.");
                        ok = false;
                    }
                }
            // Parity spot-check with the browser build (see game/vectors.json).
            if (Mathf.RoundToInt((float)(new CaseRng(12345).Next() * 1e9)) / 1000 != 979728)
            {
                Debug.LogError("Find the One: PRNG no longer matches the browser build.");
                ok = false;
            }
            if (ok) Debug.Log("Find the One verified. Run the EditMode tests (Window > General > Test Runner) for full parity checks.");
            return ok;
        }

        [MenuItem("Harbor Watch/Find The One/Build Android Development APK")]
        public static void BuildApk()
        {
            if (!Validate()) { EditorUtility.DisplayDialog("Find the One", "Fix the Console errors first.", "OK"); return; }
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                EditorUtility.DisplayDialog("Android platform required",
                    "Use File > Build Profiles to switch to Android, then run this command again.", "OK");
                return;
            }
            string destination = EditorUtility.SaveFilePanel("Save Find the One development APK", "", "RedSignal-FindTheOne-Development", "apk");
            if (string.IsNullOrEmpty(destination)) return;
            // FindTheOne first, then the other enabled scenes (they stay reachable for reviews).
            var scenes = new List<string> { Scene };
            foreach (var s in EditorBuildSettings.scenes) if (s.enabled && s.path != Scene) scenes.Add(s.path);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes.ToArray(),
                locationPathName = destination,
                target = BuildTarget.Android,
                options = BuildOptions.Development | BuildOptions.AllowDebugging
            });
            if (report.summary.result == BuildResult.Succeeded && File.Exists(destination))
            {
                Debug.Log("Find the One APK: " + destination);
                EditorUtility.RevealInFinder(destination);
            }
            else Debug.LogError("Find the One Android build did not succeed. See the Build Report / Console.");
        }
    }
}
#endif
