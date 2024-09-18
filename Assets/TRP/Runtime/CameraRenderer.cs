using UnityEngine;
using UnityEngine.Rendering;

public partial class CameraRenderer 
{
    private static readonly ShaderTagId unlitShaderTagId = new("SRPDefaultUnlit");
    private static readonly ShaderTagId litShaderTagId = new("TRPLit");
    
    private ScriptableRenderContext context;
    private Camera camera;
    
    private CullingResults cullingResults;

    private readonly Lighting lighting = new();
    private readonly CommandBuffer buffer = new();

    public void Render(ScriptableRenderContext context, Camera camera, bool useDynamicBatching, bool useGPUInstancing) 
    {
        this.context = context;
        this.camera = camera;
        
        PrepareBuffer();
        PrepareForSceneWindow();
        
        if (!Cull()) return;

        Setup();
        lighting.Setup(context, cullingResults);
        DrawVisibleGeometry(useDynamicBatching, useGPUInstancing);
        DrawUnsupportedShaders();
        DrawGizmos();
        Submit();
    }

    private bool Cull ()
    {
        if (camera.TryGetCullingParameters(out ScriptableCullingParameters p)) 
        {
            cullingResults = context.Cull(ref p);
            return true;
        }
        return false;
    }

    private void Setup()
    {
        context.SetupCameraProperties(camera);
        
        CameraClearFlags flags = camera.clearFlags;
        buffer.ClearRenderTarget(flags <= CameraClearFlags.Depth, flags <= CameraClearFlags.Color, flags == CameraClearFlags.Color ? camera.backgroundColor.linear : Color.clear);

        buffer.BeginSample(buffer.name);
        ExecuteBuffer();
    }

    private void DrawVisibleGeometry(bool useDynamicBatching, bool useGPUInstancing) 
    {
        SortingSettings sortingSettings = new(camera) 
        {
            criteria = SortingCriteria.CommonOpaque
        };

        DrawingSettings drawingSettings = new(unlitShaderTagId, sortingSettings)
        {
            enableInstancing = useGPUInstancing,
            enableDynamicBatching = useDynamicBatching,
        };
        drawingSettings.SetShaderPassName(1, litShaderTagId);

        FilteringSettings filteringSettings = new(RenderQueueRange.opaque);

        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
        
        context.DrawSkybox(camera);
        
        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        drawingSettings.sortingSettings = sortingSettings;
        filteringSettings.renderQueueRange = RenderQueueRange.transparent;

        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
    }

    partial void PrepareBuffer();
    partial void PrepareForSceneWindow();
    
    partial void DrawUnsupportedShaders();
    partial void DrawGizmos();
    
    private void Submit() 
    {
        buffer.EndSample(buffer.name);
        ExecuteBuffer();

        context.Submit();
    }
    
    private void ExecuteBuffer() 
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }
}