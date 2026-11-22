#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace HarborWatch.Editor
{
    /// <summary>
    /// Explicit local verification and Android development build commands.
    /// Does not change platform settings or ship a release-signed package.
    /// </summary>
    public static class HarborMissionBuildTools
    {
        const string MissionScene = "Assets/Scenes/HarborMission.unity";
        const string CrowdWalk = "Assets/HarborWatch/Resources/CrowdWalk.fbx";
        const string CrowdJog = "Assets/HarborWatch/Resources/CrowdJog.fbx";

        static bool HasPlayableClip(string path)
        {
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                var clip = asset as AnimationClip;
                if (clip && !clip.name.StartsWith("__") && clip.length > 0f)
                    return true;
            }
            return false;
        }

        [MenuItem("Harbor Watch/Verify Phase 3 Mission")]
        public static void VerifyMenu()
        {
            bool valid = ValidateMission();
            EditorUtility.DisplayDialog("Harbor Watch verification",
                valid ? "Scene, imported animation clips, and mission reward checks passed." :
                    "Missing or incorrect project data. Open the Console for details.",
                "OK");
        }

        static bool ValidateMission()
        {
            bool ok = true;
            if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(MissionScene))
            {
                Debug.LogError("Harbor Watch: Mission scene is missing.");
                ok = false;
            }
            if (EditorBuildSettings.scenes.Length == 0 ||
                EditorBuildSettings.scenes[0].path != MissionScene ||
                !EditorBuildSettings.scenes[0].enabled)
            {
                Debug.LogError("Harbor Watch: Mission scene must be first and enabled in Build Profiles.");
                ok = false;
            }
            if (!HasPlayableClip(CrowdWalk) || !HasPlayableClip(CrowdJog))
            {
                Debug.LogError("Harbor Watch: Could not load both original crowd animation clips.");
                ok = false;
            }
            var payout = HarborMissionRules.Settle(3, 3, 3, true);
            if (payout.Total != 576 || payout.Stars != 3)
            {
                Debug.LogError("Harbor Watch: First-run scoring failed the 576-credit check.");
                ok = false;
            }
            if (PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android)
                != "com.medhaneeka.harborwatch")
            {
                Debug.LogError("Harbor Watch: Android application ID is not set.");
                ok = false;
            }
            if (ok) Debug.Log("Harbor Watch Phase 3 verified: all project checks passed.");
            return ok;
        }

        [MenuItem("Harbor Watch/Build Android Development APK")]
        public static void BuildDevelopmentApk()
        {
            if (!ValidateMission())
            {
                EditorUtility.DisplayDialog("Harbor Watch",
                    "Fix the verification errors in the Console before building.", "OK");
                return;
            }
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                EditorUtility.DisplayDialog("Android platform required",
                    "Use File > Build Profiles to switch to Android, then run this command again.",
                    "OK");
                return;
            }

            string destination = EditorUtility.SaveFilePanel(
                "Save Harbor Watch development APK", "",
                "HarborWatch-Development", "apk");
            if (string.IsNullOrEmpty(destination)) return;

            var scenes = new System.Collections.Generic.List<string>();
            foreach (var scene in EditorBuildSettings.scenes)
                if (scene.enabled) scenes.Add(scene.path);
            var options = new BuildPlayerOptions
            {
                scenes = scenes.ToArray(),
                locationPathName = destination,
                target = BuildTarget.Android,
                options = BuildOptions.Development | BuildOptions.AllowDebugging
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result == BuildResult.Succeeded &&
                File.Exists(destination))
            {
                Debug.Log("Harbor Watch development APK: " + destination);
                EditorUtility.RevealInFinder(destination);
            }
            else
            {
                Debug.LogError("Harbor Watch Android build did not succeed. See Build Report/Console.");
            }
        }
    }
}
