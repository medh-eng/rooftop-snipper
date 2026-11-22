# Harbor Watch pedestrian review

Open the extracted unity/HarborWatch folder in Unity Hub with Editor 6000.3.28f1. Open Assets/Scenes/WalkReview.unity and press Play. This scene is the first enabled scene in the project's build settings. Select Android and Build and Run to test the same scene on a phone.

The commuter follows four sides of a paved square. Watch the support shoe against a paving seam as the opposite leg swings. At each corner the person slows, pauses, turns in short steps and sets off again. The bottom controls switch between rooftop and close views, change playback speed, pause or resume, and replay the walk. At a corner, check the shoes and the weight shift through the hips from both angles.

The original 3,552-vertex character and 14-bone rig are authored in tools/harbor-watch/author_walker.py. The single baked FBX is included under Assets/HarborWatch/Resources. The authoring script requires Blender 4.3; playing the project requires only Unity. Rebuild the FBX with Blender in background mode using the authoring script as its Python script.

[Watch the source-animation preview](walk-preview.gif). This preview was rendered from the re-imported FBX. The exported route and planted contacts were checked in Blender. Unity Editor playback, APK installation, device frame rate and artistic approval remain to be checked on the target hardware.

The next milestone after walking approval is six pedestrians with different routes, speeds and starting strides in Port Meridian.
