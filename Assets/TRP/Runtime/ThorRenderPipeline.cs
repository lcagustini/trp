using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public partial class ThorRenderPipeline : RenderPipeline
{
    private readonly CameraRenderer renderer = new();

    private readonly bool useDynamicBatching;
    private readonly bool useGPUInstancing;

    private readonly ShadowSettings shadowSettings;
    
    public ThorRenderPipeline(bool useDynamicBatching, bool useGPUInstancing, bool useSRPBatcher, ShadowSettings shadowSettings)
    {
        this.useDynamicBatching = useDynamicBatching;
        this.useGPUInstancing = useGPUInstancing;
        this.shadowSettings = shadowSettings;
        GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;
        GraphicsSettings.lightsUseLinearIntensity = true;

        InitializeEditor();
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
    }
    
    protected override void Render(ScriptableRenderContext context, List<Camera> cameras)
    {
        foreach (Camera camera in cameras)
        {
            renderer.Render(context, camera, useDynamicBatching, useGPUInstancing, shadowSettings);
        }
    }
    
    partial void InitializeEditor();
}