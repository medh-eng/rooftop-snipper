using UnityEditor;

namespace HarborWatch.Editor
{
    // Legacy import keeps the original bone paths, including both planted feet.
    // The isolated review scene samples this authored clip at a user-controlled time.
    public sealed class WalkerImporter : AssetPostprocessor
    {
        void OnPreprocessModel()
        {
            if (assetPath != "Assets/HarborWatch/Resources/HarborWalker.fbx") return;
            var importer = (ModelImporter)assetImporter;
            importer.animationType = ModelImporterAnimationType.Legacy;
            importer.generateAnimations = ModelImporterGenerateAnimations.GenerateAnimations;
            importer.importAnimation = true;
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            importer.importCameras = false;
            importer.importLights = false;
        }
    }
}
