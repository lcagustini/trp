using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "TRP/TRP Pipeline")]
public class ThorRenderPipelineAsset : RenderPipelineAsset
{
    [SerializeField] private bool useDynamicBatching = true;
    [SerializeField] private bool useGPUInstancing = true;
    [SerializeField] private bool useSRPBatcher = true;

    [SerializeField] private ShadowSettings shadows;
    
    protected override RenderPipeline CreatePipeline()
    {
        return new ThorRenderPipeline(useDynamicBatching, useGPUInstancing, useSRPBatcher, shadows);
    }
}
