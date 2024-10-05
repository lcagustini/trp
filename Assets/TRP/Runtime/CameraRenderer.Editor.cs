#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Profiling;
using UnityEditor;

public partial class CameraRenderer 
{
    private static readonly ShaderTagId[] legacyShaderTagIds = 
    {
        new("Always"),
        new("ForwardBase"),
        new("PrepassBase"),
        new("Vertex"),
        new("VertexLMRGBM"),
        new("VertexLM")
    };
    
    private static Material errorMaterial;

    partial void PrepareBuffer()
    {
        Profiler.BeginSample("Editor Only");
        buffer.name = camera.name;
        Profiler.EndSample();
    }
    
    partial void PrepareForSceneWindow() 
    {
        if (camera.cameraType == CameraType.SceneView) {
            ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
        }
    }

    partial void DrawUnsupportedShaders() 
    {
        if (errorMaterial == null) errorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));
        
        DrawingSettings drawingSettings = new(legacyShaderTagIds[0], new SortingSettings(camera)) 
        {
            overrideMaterial = errorMaterial
        };
        for (int i = 1; i < legacyShaderTagIds.Length; i++) {
            drawingSettings.SetShaderPassName(i, legacyShaderTagIds[i]);
        }

        FilteringSettings filteringSettings = FilteringSettings.defaultValue;

        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
    }

    partial void DrawGizmosBeforePostProcess()
    {
        if (Handles.ShouldRenderGizmos()) context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
    }
    
    partial void DrawGizmosAfterPostProcess()
    {
        if (Handles.ShouldRenderGizmos()) context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
    }
}
#else
public partial class CameraRenderer 
{
    private const string BUFFER_NAME = "Render Camera";

    partial void PrepareBuffer()
    {
        buffer.name = BUFFER_NAME;
    }
}
#endif
