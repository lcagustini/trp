using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/Thor Render Pipeline")]
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
