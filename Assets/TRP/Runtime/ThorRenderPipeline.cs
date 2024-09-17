using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class ThorRenderPipeline : RenderPipeline
{
    private readonly CameraRenderer renderer = new();

    private readonly bool useDynamicBatching;
    private readonly bool useGPUInstancing;
    
    public ThorRenderPipeline(bool useDynamicBatching, bool useGPUInstancing, bool useSRPBatcher)
    {
        this.useDynamicBatching = useDynamicBatching;
        this.useGPUInstancing = useGPUInstancing;
        GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
    }
    
    protected override void Render(ScriptableRenderContext context, List<Camera> cameras)
    {
        foreach (Camera camera in cameras)
        {
            renderer.Render(context, camera, useDynamicBatching, useGPUInstancing);
        }
    }
}