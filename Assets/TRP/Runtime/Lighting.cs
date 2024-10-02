using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class Lighting 
{
    private const string BUFFER_NAME = "Lighting";

    private const int MAX_DIR_LIGHT_COUNT = 4;
    private const int MAX_OTHER_LIGHT_COUNT = 64;

    private static readonly int dirLightCountId = Shader.PropertyToID("_DirectionalLightCount");
    private static readonly int dirLightColorsId = Shader.PropertyToID("_DirectionalLightColors");
    private static readonly int dirLightDirectionsId = Shader.PropertyToID("_DirectionalLightDirections");
    private static readonly int dirLightShadowDataId = Shader.PropertyToID("_DirectionalLightShadowData");

    private static readonly Vector4[] dirLightColors = new Vector4[MAX_DIR_LIGHT_COUNT];
    private static readonly Vector4[] dirLightDirections = new Vector4[MAX_DIR_LIGHT_COUNT];
    private static readonly Vector4[] dirLightShadowData = new Vector4[MAX_DIR_LIGHT_COUNT];
    
    private static readonly int otherLightCountId = Shader.PropertyToID("_OtherLightCount");
    private static readonly int otherLightColorsId = Shader.PropertyToID("_OtherLightColors");
    private static readonly int otherLightPositionsId = Shader.PropertyToID("_OtherLightPositions");
    private static readonly int otherLightDirectionsId = Shader.PropertyToID("_OtherLightDirections");
    private static readonly int otherLightSpotAnglesId = Shader.PropertyToID("_OtherLightSpotAngles");
    private static readonly int otherLightShadowDataId = Shader.PropertyToID("_OtherLightShadowData");

    private static readonly Vector4[] otherLightColors = new Vector4[MAX_OTHER_LIGHT_COUNT];
    private static readonly Vector4[] otherLightPositions = new Vector4[MAX_OTHER_LIGHT_COUNT];
    private static readonly Vector4[] otherLightDirections = new Vector4[MAX_OTHER_LIGHT_COUNT];
    private static readonly Vector4[] otherLightSpotAngles = new Vector4[MAX_OTHER_LIGHT_COUNT];
    private static readonly Vector4[] otherLightShadowData = new Vector4[MAX_OTHER_LIGHT_COUNT];

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
        int otherLightCount = 0;
        for (int i = 0; i < visibleLights.Length; i++)
        {
            VisibleLight visibleLight = visibleLights[i];
            switch (visibleLight.lightType)
            {
                case LightType.Directional:
                    if (dirLightCount >= MAX_DIR_LIGHT_COUNT) continue;
                    SetupDirectionalLight(dirLightCount++, i, visibleLight);
                    break;
                case LightType.Point:
                    if (otherLightCount >= MAX_OTHER_LIGHT_COUNT) continue;
                    SetupPointLight(otherLightCount++, i, visibleLight);
                    break;
                case LightType.Spot:
                    if (otherLightCount >= MAX_OTHER_LIGHT_COUNT) continue;
                    SetupSpotLight(otherLightCount++, i, visibleLight);
                    break;
            }
        }

        buffer.SetGlobalInt(dirLightCountId, dirLightCount);
        if (dirLightCount > 0)
        {
            buffer.SetGlobalVectorArray(dirLightColorsId, dirLightColors);
            buffer.SetGlobalVectorArray(dirLightDirectionsId, dirLightDirections);
            buffer.SetGlobalVectorArray(dirLightShadowDataId, dirLightShadowData);
        }
        
        buffer.SetGlobalInt(otherLightCountId, otherLightCount);
        if (otherLightCount > 0) 
        {
            buffer.SetGlobalVectorArray(otherLightColorsId, otherLightColors);
            buffer.SetGlobalVectorArray(otherLightPositionsId, otherLightPositions);
            buffer.SetGlobalVectorArray(otherLightDirectionsId, otherLightDirections);
            buffer.SetGlobalVectorArray(otherLightSpotAnglesId, otherLightSpotAngles);
            buffer.SetGlobalVectorArray(otherLightShadowDataId, otherLightShadowData);
        }
    }
    
    private void SetupDirectionalLight(int index, int visibleIndex, VisibleLight visibleLight) 
    {
        dirLightColors[index] = visibleLight.finalColor;
        dirLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);
        dirLightShadowData[index] = shadows.ReserveDirectionalShadows(visibleLight.light, visibleIndex);
    }
    
    private void SetupPointLight(int index, int visibleIndex, VisibleLight visibleLight) 
    {
        otherLightColors[index] = visibleLight.finalColor;
        otherLightSpotAngles[index] = new Vector4(0f, 1f);
        
        Vector4 position = visibleLight.localToWorldMatrix.GetColumn(3);
        position.w = 1f / Mathf.Max(visibleLight.range * visibleLight.range, 0.00001f);
        otherLightPositions[index] = position;
        
        Light light = visibleLight.light;
        otherLightShadowData[index] = shadows.ReserveOtherShadows(light, visibleIndex);
    }
    
    private void SetupSpotLight(int index, int visibleIndex, VisibleLight visibleLight) 
    {
        otherLightColors[index] = visibleLight.finalColor;
        otherLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);
        
        Vector4 position = visibleLight.localToWorldMatrix.GetColumn(3);
        position.w = 1f / Mathf.Max(visibleLight.range * visibleLight.range, 0.00001f);
        otherLightPositions[index] = position;
        
        Light light = visibleLight.light;
        otherLightShadowData[index] = shadows.ReserveOtherShadows(light, visibleIndex);
        
        float innerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * light.innerSpotAngle);
        float outerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * visibleLight.spotAngle);
        float angleRangeInv = 1f / Mathf.Max(innerCos - outerCos, 0.001f);
        otherLightSpotAngles[index] = new Vector4(angleRangeInv, -outerCos * angleRangeInv);
    }
    
    public void Cleanup()
    {
        shadows.Cleanup();
    }
}