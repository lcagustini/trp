using UnityEngine;
using UnityEngine.Rendering;

public partial class CameraRenderer
{
    private static readonly CameraSettings defaultCameraSettings = new();
    
    private static readonly ShaderTagId unlitShaderTagId = new("SRPDefaultUnlit");
    private static readonly ShaderTagId litShaderTagId = new("TRPLit");

    private static readonly int frameBufferId = Shader.PropertyToID("_CameraFrameBuffer");

    private ScriptableRenderContext context;
    private Camera camera;
    
    private bool useHDR;

    private CullingResults cullingResults;

    private readonly Lighting lighting = new();
    private readonly PostProcessStack postProcessStack = new();
    private readonly CommandBuffer buffer = new();

    public void Render(ScriptableRenderContext context, Camera camera, bool allowHDR, bool useDynamicBatching, bool useGPUInstancing, ShadowSettings shadowSettings, PostProcessSettings postProcessSettings, int colorLUTResolution)
    {
        this.context = context;
        this.camera = camera;
        
        TRPCamera trpCamera = camera.GetComponent<TRPCamera>();
        CameraSettings cameraSettings = trpCamera != null ? trpCamera.Settings : defaultCameraSettings;
        if (cameraSettings.OverridesPostProcess) postProcessSettings = cameraSettings.postProcessSettingsOverride;

        PrepareBuffer();
        PrepareForSceneWindow();

        if (!Cull(shadowSettings.maxDistance)) return;
        
        useHDR = allowHDR && camera.allowHDR;

        lighting.Setup(context, cullingResults, shadowSettings);
        postProcessStack.Setup(context, camera, postProcessSettings, useHDR, colorLUTResolution, cameraSettings.finalBlendMode);
        Setup();

        DrawVisibleGeometry(useDynamicBatching, useGPUInstancing);
        DrawUnsupportedShaders();
        DrawGizmosBeforePostProcess();
        if (postProcessStack.IsActive) postProcessStack.Render(frameBufferId);
        DrawGizmosAfterPostProcess();

        Cleanup();

        Submit();
    }

    private bool Cull(float maxShadowDistance)
    {
        if (camera.TryGetCullingParameters(out ScriptableCullingParameters p))
        {
            p.shadowDistance = Mathf.Min(maxShadowDistance, camera.farClipPlane);
            cullingResults = context.Cull(ref p);
            return true;
        }

        return false;
    }

    private void Setup()
    {
        context.SetupCameraProperties(camera);

        CameraClearFlags flags = camera.clearFlags;

        if (postProcessStack.IsActive)
        {
            if (flags > CameraClearFlags.Color) flags = CameraClearFlags.Color;
            buffer.GetTemporaryRT(frameBufferId, camera.pixelWidth, camera.pixelHeight, 32, FilterMode.Bilinear, useHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default);
            buffer.SetRenderTarget(frameBufferId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        }

        buffer.ClearRenderTarget(flags <= CameraClearFlags.Depth, flags <= CameraClearFlags.Color, flags == CameraClearFlags.Color ? camera.backgroundColor.linear : Color.clear);

        buffer.BeginSample(buffer.name);
        ExecuteBuffer();
    }

    private void Cleanup()
    {
        lighting.Cleanup();
        if (postProcessStack.IsActive)
        {
            buffer.ReleaseTemporaryRT(frameBufferId);
        }
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
            perObjectData = PerObjectData.Lightmaps | PerObjectData.LightProbe | PerObjectData.ShadowMask | PerObjectData.OcclusionProbe
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
    partial void DrawGizmosBeforePostProcess();
    partial void DrawGizmosAfterPostProcess();

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