#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace HarborWatch.Editor
{
    /// <summary>
    /// Optional Phase 4 verification and Android test build.
    /// Builds only HarborDrone.unity without changing the normal mission scene list.
    /// </summary>
    public static class HarborDroneBuildTools
    {
        const string DroneScene = "Assets/Scenes/HarborDrone.unity";
        const string CrowdScene = "Assets/Scenes/HarborCrowd.unity";
        const string DroneController = "Assets/HarborWatch/HarborDroneReview.cs";
        const string CrowdWalk = "Assets/HarborWatch/Resources/CrowdWalk.fbx";

        static bool Verify()
        {
            bool valid = true;
            if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(DroneScene) ||
                !AssetDatabase.LoadAssetAtPath<SceneAsset>(CrowdScene))
            {
                Debug.LogError("Harbor Drone: Review scene or original crowd scene is missing.");
                valid = false;
            }
            if (!AssetDatabase.LoadAssetAtPath<MonoScript>(DroneController))
            {
                Debug.LogError("Harbor Drone: Missing aerial camera controller.");
                valid = false;
            }
            bool foundClip = false;
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(CrowdWalk))
            {
                var clip = asset as AnimationClip;
                if (clip && !clip.name.StartsWith("__") && clip.length > 0f)
                    foundClip = true;
            }
            if (!foundClip)
            {
                Debug.LogError("Harbor Drone: CrowdWalk FBX has no playable clip.");
                valid = false;
            }

            // Simulate a camera descending over the observer building:
            // the constrained flight path must never be inside its roof.
            Vector3 start = new Vector3(-5.1f, 8.5f, -3.3f);
            Vector3 proposed = new Vector3(-7.4f, 5.1f, -5f);
            Vector3 resolved = HarborDroneFlight.Advance(start, proposed-start);
            if (HarborDroneFlight.InsideRoof(resolved) ||
                HarborDroneFlight.Constrain(resolved) != resolved)
            {
                Debug.LogError("Harbor Drone: Aerial bounds check failed.");
                valid = false;
            }
            if (PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android)
                != "com.medhaneeka.harborwatch")
            {
                Debug.LogError("Harbor Drone: Android application ID is missing.");
                valid = false;
            }
            if (valid) Debug.Log("Harbor Drone Phase 4 source/asset checks passed.");
            return valid;
        }

        [MenuItem("Harbor Watch/Verify Phase 4 Drone")]
        public static void VerifyMenu()
        {
            bool valid = Verify();
            EditorUtility.DisplayDialog("Harbor Drone",
                valid ? "Drone scene, crowd asset and basic bounds are present." :
                    "Verification failed. Read the first Console error.", "OK");
        }

        [MenuItem("Harbor Watch/Build Drone Review APK")]
        public static void BuildDroneReviewApk()
        {
            if (!Verify())
            {
                EditorUtility.DisplayDialog("Harbor Drone", "Fix Console errors first.", "OK");
                return;
            }
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                EditorUtility.DisplayDialog("Android required",
                    "Switch to Android in File > Build Profiles and retry.", "OK");
                return;
            }

            string destination = EditorUtility.SaveFilePanel(
                "Save the standalone Harbor Drone review APK", "",
                "HarborDrone-Development", "apk");
            if (string.IsNullOrEmpty(destination)) return;

            var options = new BuildPlayerOptions
            {
                scenes = new[] { DroneScene },
                locationPathName = destination,
                target = BuildTarget.Android,
                options = BuildOptions.Development | BuildOptions.AllowDebugging
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result == BuildResult.Succeeded &&
                File.Exists(destination))
            {
                Debug.Log("Harbor Drone review APK: " + destination);
                EditorUtility.RevealInFinder(destination);
            }
            else Debug.LogError("Harbor Drone Android build failed. Check the Console.");
        }
    }
}
