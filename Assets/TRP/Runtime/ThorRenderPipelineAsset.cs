using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "TRP/TRP Pipeline")]
public class ThorRenderPipelineAsset : RenderPipelineAsset
{
    public enum ColorLUTResolution
    {
        _16 = 16,
        _32 = 32,
        _64 = 64
    }

    [SerializeField] private bool useDynamicBatching = true;
    [SerializeField] private bool useGPUInstancing = true;
    [SerializeField] private bool useSRPBatcher = true;
    [SerializeField] private bool allowHDR = true;

    [SerializeField] private ShadowSettings shadows;

    [SerializeField] private PostProcessSettings postProcessSettings;
    
    [SerializeField] private ColorLUTResolution colorLUTResolution = ColorLUTResolution._32;

    protected override RenderPipeline CreatePipeline()
    {
        return new ThorRenderPipeline(allowHDR, useDynamicBatching, useGPUInstancing, useSRPBatcher, shadows, postProcessSettings, (int)colorLUTResolution);
    }
}