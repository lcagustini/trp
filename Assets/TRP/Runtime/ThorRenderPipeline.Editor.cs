#if UNITY_EDITOR
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Rendering;
using LightType = UnityEngine.LightType;

public partial class ThorRenderPipeline
{
    private static readonly Lightmapping.RequestLightsDelegate lightsDelegate = (lights, output) =>
    {
        LightDataGI lightData = new();
        for (int i = 0; i < lights.Length; i++)
        {
            Light light = lights[i];

            switch (light.type)
            {
                case LightType.Directional:
                    DirectionalLight directionalLight = new();
                    LightmapperUtils.Extract(light, ref directionalLight);
                    lightData.Init(ref directionalLight);
                    break;
                case LightType.Point:
                    PointLight pointLight = new();
                    LightmapperUtils.Extract(light, ref pointLight);
                    lightData.Init(ref pointLight);
                    break;
                case LightType.Spot:
                    SpotLight spotLight = new();
                    LightmapperUtils.Extract(light, ref spotLight);
                    spotLight.innerConeAngle = light.innerSpotAngle * Mathf.Deg2Rad;
                    spotLight.angularFalloff = AngularFalloffType.AnalyticAndInnerAngle;
                    lightData.Init(ref spotLight);
                    break;
                case LightType.Rectangle:
                    RectangleLight rectangleLight = new();
                    LightmapperUtils.Extract(light, ref rectangleLight);
                    rectangleLight.mode = LightMode.Baked;
                    lightData.Init(ref rectangleLight);
                    break;
                default:
                    lightData.InitNoBake(light.GetInstanceID());
                    break;
            }

            lightData.falloff = FalloffType.InverseSquared;
            output[i] = lightData;
        }
    };
    
    partial void InitializeEditor()
    {
        Lightmapping.SetDelegate(lightsDelegate);
    }
    
    protected override void Dispose(bool disposing) 
    {
        base.Dispose(disposing);

        Lightmapping.ResetDelegate();
    }
}
#endif