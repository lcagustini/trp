#ifndef TRP_GI
#define TRP_GI

#ifdef LIGHTMAP_ON
    #define GI_ATTRIBUTE_DATA float2 lightMapUV : TEXCOORD1;
    #define GI_VARYINGS_DATA float2 lightMapUV : VAR_LIGHT_MAP_UV;
    #define TRANSFER_GI_DATA(input, output) output.lightMapUV = input.lightMapUV * unity_LightmapST.xy + unity_LightmapST.zw;
    #define GI_FRAGMENT_DATA(input) input.lightMapUV
#else
    #define GI_ATTRIBUTE_DATA
    #define GI_VARYINGS_DATA
    #define TRANSFER_GI_DATA(input, output)
    #define GI_FRAGMENT_DATA(input) 0.0
#endif

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"
#include "../ShaderLibrary/Surface.hlsl"
#include "../ShaderLibrary/UnityInput.hlsl"
#include "../ShaderLibrary/Shadows.hlsl"

TEXTURE2D(unity_Lightmap);
SAMPLER(samplerunity_Lightmap);

TEXTURE2D(unity_ShadowMask);
SAMPLER(samplerunity_ShadowMask);

struct GI {
    float3 diffuse;
    shadowMask shadowMask;
};

float3 SampleLightProbe(surface surfaceWS) {
#ifdef LIGHTMAP_ON
    return 0.0;
#else
    float4 coefficients[7];
    coefficients[0] = unity_SHAr;
    coefficients[1] = unity_SHAg;
    coefficients[2] = unity_SHAb;
    coefficients[3] = unity_SHBr;
    coefficients[4] = unity_SHBg;
    coefficients[5] = unity_SHBb;
    coefficients[6] = unity_SHC;
    return max(0.0, SampleSH9(coefficients, surfaceWS.normal));
#endif
}

float3 SampleLightMap(float2 lightMapUV) {
#ifdef LIGHTMAP_ON
#ifdef UNITY_LIGHTMAP_FULL_HDR
    bool compressed = false;
#else
    bool compressed = true;
#endif
    return SampleSingleLightmap(TEXTURE2D_ARGS(unity_Lightmap, samplerunity_Lightmap), lightMapUV, float4(1.0, 1.0, 0.0, 0.0), compressed, float4(LIGHTMAP_HDR_MULTIPLIER, LIGHTMAP_HDR_EXPONENT, 0.0, 0.0));
#else
    return 0.0;
#endif
}

float4 SampleBakedShadows(float2 lightMapUV) {
#ifdef LIGHTMAP_ON
    return SAMPLE_TEXTURE2D(unity_ShadowMask, samplerunity_ShadowMask, lightMapUV);
#else
    return unity_ProbesOcclusion;
#endif
}

GI GetGI(float2 lightMapUV, surface surfaceWS) {
    GI gi;
    gi.diffuse = SampleLightMap(lightMapUV) + SampleLightProbe(surfaceWS);
#ifdef _SHADOW_MASK_DISTANCE
    gi.shadowMask.always = false;
    gi.shadowMask.distance = true;
    gi.shadowMask.shadows = SampleBakedShadows(lightMapUV);
#elif _SHADOW_MASK_ALWAYS
    gi.shadowMask.always = true;
    gi.shadowMask.distance = false;
    gi.shadowMask.shadows = SampleBakedShadows(lightMapUV);
#else
    gi.shadowMask.always = false;
    gi.shadowMask.distance = false;
    gi.shadowMask.shadows = 1.0;
#endif
    return gi;
}

#endif
