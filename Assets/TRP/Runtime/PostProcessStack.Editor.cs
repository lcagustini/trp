#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public partial class PostProcessStack
{
    partial void ApplySceneViewState()
    {
        if (camera.cameraType == CameraType.SceneView && !SceneView.currentDrawingSceneView.sceneViewState.showImageEffects)
        {
            settings = null;
        }
    }
}
#endif