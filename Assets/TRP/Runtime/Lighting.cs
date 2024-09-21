using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class Lighting 
{
    private const string BUFFER_NAME = "Lighting";

    const int maxDirLightCount = 4;

    private static readonly int dirLightCountId = Shader.PropertyToID("_DirectionalLightCount");
    private static readonly int dirLightColorsId = Shader.PropertyToID("_DirectionalLightColors");
    private static readonly int dirLightDirectionsId = Shader.PropertyToID("_DirectionalLightDirections");
    private static readonly int dirLightShadowDataId = Shader.PropertyToID("_DirectionalLightShadowData");

    private static readonly Vector4[] dirLightColors = new Vector4[maxDirLightCount];
    private static readonly Vector4[] dirLightDirections = new Vector4[maxDirLightCount];
    private static readonly Vector4[] dirLightShadowData = new Vector4[maxDirLightCount];

    private readonly Shadows shadows = new();

    private CullingResults cullingResults;

    private readonly CommandBuffer buffer = new()
    {
        name = BUFFER_NAME
    };
	
    public void Setup(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings shadowSettings)
    {
        this.cullingResults = cullingResults;
        
        buffer.BeginSample(BUFFER_NAME);
        shadows.Setup(context, cullingResults, shadowSettings);
        SetupLights();
        shadows.Render();
        buffer.EndSample(BUFFER_NAME);
        
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }
	
    private void SetupLights() 
    { 
        NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;
        int dirLightCount = 0;
        foreach (VisibleLight visibleLight in visibleLights)
        {
            if (visibleLight.lightType != LightType.Directional) continue;

            SetupDirectionalLight(dirLightCount++, visibleLight);
            if (dirLightCount >= maxDirLightCount) break;
        }

        buffer.SetGlobalInt(dirLightCountId, dirLightCount);
        buffer.SetGlobalVectorArray(dirLightColorsId, dirLightColors);
        buffer.SetGlobalVectorArray(dirLightDirectionsId, dirLightDirections);
        buffer.SetGlobalVectorArray(dirLightShadowDataId, dirLightShadowData);
    }
    
    private void SetupDirectionalLight(int index, VisibleLight visibleLight) 
    {
        dirLightColors[index] = visibleLight.finalColor;
        dirLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);
        dirLightShadowData[index] = shadows.ReserveDirectionalShadows(visibleLight.light, index);
    }
    
    public void Cleanup()
    {
        shadows.Cleanup();
    }
}