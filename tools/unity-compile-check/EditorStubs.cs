using System;
namespace UnityEditor
{
    [AttributeUsage(AttributeTargets.Method)] public sealed class MenuItemAttribute : Attribute { public MenuItemAttribute(string p) { } public MenuItemAttribute(string p, bool v) { } public MenuItemAttribute(string p, bool v, int pr) { } }
    public static class EditorUtility { public static bool DisplayDialog(string a, string b, string c) => true; public static bool DisplayDialog(string a, string b, string c, string d) => true; public static string SaveFilePanel(string a, string b, string c, string d) => ""; public static void RevealInFinder(string p) { } }
    public static class AssetDatabase { public static T LoadAssetAtPath<T>(string p) where T : UnityEngine.Object => null; public static UnityEngine.Object[] LoadAllAssetsAtPath(string p) => new UnityEngine.Object[0]; public static void Refresh() { } public static void SaveAssets() { } }
    public class SceneAsset : UnityEngine.Object { }
    public enum BuildTarget { Android, StandaloneWindows64 }
    public enum BuildTargetGroup { Android }
    [Flags] public enum BuildOptions { None = 0, Development = 1, AllowDebugging = 2 }
    public static class EditorUserBuildSettings { public static BuildTarget activeBuildTarget; }
    public class EditorBuildSettingsScene { public bool enabled; public string path; public EditorBuildSettingsScene(string p, bool e) { path = p; enabled = e; } }
    public static class EditorBuildSettings { public static EditorBuildSettingsScene[] scenes; }
    public struct BuildPlayerOptions { public string[] scenes; public string locationPathName; public BuildTarget target; public BuildOptions options; }
    public static class BuildPipeline { public static Build.Reporting.BuildReport BuildPlayer(BuildPlayerOptions o) => null; }
    public enum ModelImporterGenerateAnimations { None, GenerateAnimations, InOriginalRoots, InNodes, InRoot }
    public enum ModelImporterAnimationCompression { Off, KeyframeReduction, Optimal }
    public class MonoScript : UnityEngine.Object { public static MonoScript FromMonoBehaviour(UnityEngine.MonoBehaviour b) => null; public System.Type GetClass() => null; }
    public class ModelImporter : AssetImporter { public bool importAnimation, importCameras, importLights, importBlendShapes, importVisibility; public ModelImporterGenerateAnimations generateAnimations; public ModelImporterAnimationCompression animationCompression; public ModelImporterAnimationType animationType; public ModelImporterClipAnimation[] clipAnimations, defaultClipAnimations; public bool isReadable; public float globalScale; }
    public enum ModelImporterAnimationType { None, Legacy, Generic, Human }
    public class ModelImporterClipAnimation { public bool loopTime; public string name; public WrapMode wrapMode; }
    public enum WrapMode { Loop }
    public class AssetImporter : UnityEngine.Object { public string assetPath; public void SaveAndReimport() { } public static AssetImporter GetAtPath(string p) => null; }
    public class AssetPostprocessor { public AssetImporter assetImporter; public string assetPath; }
    public static class PlayerSettings { public static string GetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget t) => ""; public static string productName, companyName; }
}
namespace UnityEditor.Build { public struct NamedBuildTarget { public static NamedBuildTarget Android; } }
namespace UnityEditor.Build.Reporting
{
    public enum BuildResult { Unknown, Succeeded, Failed, Cancelled }
    public class BuildSummary { public BuildResult result; }
    public class BuildReport { public BuildSummary summary; }
}
