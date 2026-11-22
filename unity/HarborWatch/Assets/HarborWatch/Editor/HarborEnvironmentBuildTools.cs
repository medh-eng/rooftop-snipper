#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace HarborWatch.Editor
{
    public static class HarborEnvironmentBuildTools
    {
        [MenuItem("Harbor Watch/Verify Phase 5 Living Harbor")]
        public static void VerifyMenu()
        {
            bool ok = true;
            foreach (string scene in new[] {
                "Assets/Scenes/HarborCrowd.unity",
                "Assets/Scenes/HarborMission.unity",
                "Assets/Scenes/HarborDrone.unity"
            })
            {
                if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(scene))
                {
                    Debug.LogError("Living Harbor: Missing scene " + scene);
                    ok = false;
                }
            }
            if (!AssetDatabase.LoadAssetAtPath<MonoScript>(
                "Assets/HarborWatch/HarborEnvironment.cs"))
            {
                Debug.LogError("Living Harbor: Missing environment script.");
                ok = false;
            }
            if (!AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/HarborWatch/Resources/ReviewSurface.mat"))
            {
                Debug.LogError("Living Harbor: Missing source material.");
                ok = false;
            }
            float dock = HarborEnvironment.DockX;
            float sea = HarborEnvironment.SeaX;
            if (Mathf.Abs(HarborEnvironment.FerryLocalX(0f) - dock) > .001f ||
                Mathf.Abs(HarborEnvironment.FerryLocalX(20.5f) - (dock + sea) * .5f) > .001f ||
                Mathf.Abs(HarborEnvironment.FerryLocalX(31f) - sea) > .001f ||
                Mathf.Abs(HarborEnvironment.FerryLocalX(64f) - dock) > .001f)
            {
                Debug.LogError("Living Harbor: Ferry animation arithmetic failed.");
                ok = false;
            }
            if (ok) Debug.Log("Living Harbor asset checks passed; runtime/Android test pending.");
            EditorUtility.DisplayDialog("Living Harbor Phase 5",
                ok ? "Source assets and ferry cycle verified. Test all scenes in Play Mode." :
                "Preflight failed. Check Console errors.", "OK");
        }
    }
}
