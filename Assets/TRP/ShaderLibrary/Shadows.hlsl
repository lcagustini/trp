#ifndef TRP_SHADOWS
#define TRP_SHADOWS

#define MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT 4
#define MAX_CASCADE_COUNT 4

#if _DIRECTIONAL_PCF3
    #define DIRECTIONAL_FILTER_SAMPLES 4
    #define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_3x3
#elif _DIRECTIONAL_PCF5
    #define DIRECTIONAL_FILTER_SAMPLES 9
    #define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_5x5
#elif _DIRECTIONAL_PCF7
    #define DIRECTIONAL_FILTER_SAMPLES 16
    #define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_7x7
#endif

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Shadow/ShadowSamplingTent.hlsl"

TEXTURE2D_SHADOW(_DirectionalShadowAtlas);
#define SHADOW_SAMPLER sampler_linear_clamp_compare
SAMPLER_CMP(SHADOW_SAMPLER);

CBUFFER_START(_TRPShadows)
    int _CascadeCount;
    float4 _CascadeCullingSpheres[MAX_CASCADE_COUNT];
    float4 _CascadeData[MAX_CASCADE_COUNT];
    float4x4 _DirectionalShadowMatrices[MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT * MAX_CASCADE_COUNT];
    float4 _ShadowAtlasSize;
    float4 _ShadowDistanceFade;
CBUFFER_END

struct shadowMask {
    bool always;
    bool distance;
    float4 shadows;
};

struct shadowData {
    int cascadeIndex;
    float cascadeBlend;
    float strength;
    shadowMask shadowMask;
};

struct directionalShadowData {
    float strength;
    int tileIndex;
    float normalBias;
    int shadowMaskChannel;
};

struct otherShadowData {
    float strength;
    int shadowMaskChannel;
};

float FadedShadowStrength(const float distance, const float scale, const float fade) {
    return saturate((1.0 - distance * scale) * fade);
}

shadowData GetShadowData(surface surfaceWS) {
    shadowData data;
    data.cascadeBlend = 1.0;
    data.strength = FadedShadowStrength(surfaceWS.depth, _ShadowDistanceFade.x, _ShadowDistanceFade.y);

    data.shadowMask.always = false;
    data.shadowMask.distance = false;
    data.shadowMask.shadows = 1.0;

    int i;
    for (i = 0; i < _CascadeCount; i++) {
        float4 sphere = _CascadeCullingSpheres[i];
        const float distanceSqr = DistanceSquared(surfaceWS.position, sphere.xyz);
        if (distanceSqr < sphere.w) {
            const float fade = FadedShadowStrength(distanceSqr, _CascadeData[i].x, _ShadowDistanceFade.z);
            if (i == _CascadeCount - 1) {
                data.strength *= fade;
            }
            else {
                data.cascadeBlend = fade;
            }
            break;
        }
    }
    
    if (i == _CascadeCount) {
        data.strength = 0.0;
    }
#ifdef _CASCADE_BLEND_DITHER
    else if (data.cascadeBlend < surfaceWS.dither) {
        i += 1;
    }
#endif
#ifndef  _CASCADE_BLEND_SOFT
    data.cascadeBlend = 1.0;
#endif

    data.cascadeIndex = i;
    return data;
}

float SampleDirectionalShadowAtlas(float3 positionSTS) {
    return SAMPLE_TEXTURE2D_SHADOW(_DirectionalShadowAtlas, SHADOW_SAMPLER, positionSTS);
}

float FilterDirectionalShadow(float3 positionSTS) {
#if defined(DIRECTIONAL_FILTER_SETUP)
    float weights[DIRECTIONAL_FILTER_SAMPLES];
    float2 positions[DIRECTIONAL_FILTER_SAMPLES];
    float4 size = _ShadowAtlasSize.yyxx;
    DIRECTIONAL_FILTER_SETUP(size, positionSTS.xy, weights, positions);
    float shadow = 0;
    for (int i = 0; i < DIRECTIONAL_FILTER_SAMPLES; i++) {
        shadow += weights[i] * SampleDirectionalShadowAtlas(float3(positions[i].xy, positionSTS.z));
    }
    return shadow;
#else
    return SampleDirectionalShadowAtlas(positionSTS);
#endif
}

float GetCascadedShadow(directionalShadowData directionalData, shadowData shadowData, surface surfaceWS) {
    float3 normalBias = surfaceWS.normal * (directionalData.normalBias * _CascadeData[shadowData.cascadeIndex].y);
    float3 positionSTS = mul(_DirectionalShadowMatrices[directionalData.tileIndex], float4(surfaceWS.position + normalBias, 1.0)).xyz;
    float shadow = FilterDirectionalShadow(positionSTS);

    if (shadowData.cascadeBlend < 1.0) {
        normalBias = surfaceWS.normal * (directionalData.normalBias * _CascadeData[shadowData.cascadeIndex + 1].y);
        positionSTS = mul(_DirectionalShadowMatrices[directionalData.tileIndex + 1], float4(surfaceWS.position + normalBias, 1.0)).xyz;
        shadow = lerp(FilterDirectionalShadow(positionSTS), shadow, shadowData.cascadeBlend);
    }
    
    return shadow;
}

float GetBakedShadow(shadowMask mask, int channel) {
    float shadow = 1.0;
    if (mask.always || mask.distance) {
        if (channel >= 0) {
            shadow = mask.shadows[channel];
        }
    }
    return shadow;
}

float GetBakedShadow(shadowMask mask, int channel, float strength) {
    if (mask.always || mask.distance) {
        return lerp(1.0, GetBakedShadow(mask, channel), strength);
    }
    return 1.0;
}

float MixBakedAndRealtimeShadows(shadowData global, float shadow, int shadowMaskChannel, float strength) {
    float baked = GetBakedShadow(global.shadowMask, shadowMaskChannel);
    if (global.shadowMask.always) {
        shadow = lerp(1.0, shadow, global.strength);
        shadow = min(baked, shadow);
        return lerp(1.0, shadow, strength);
    }
    if (global.shadowMask.distance) {
        shadow = lerp(baked, shadow, global.strength);
		return lerp(1.0, shadow, strength);
    }
    return lerp(1.0, shadow, strength * global.strength);
}

float GetDirectionalShadowAttenuation(directionalShadowData directionalData, shadowData shadowData, surface surfaceWS) {
#ifndef _RECEIVE_SHADOWS_ON
    return 1.0;
#endif
    float shadow;
    if (directionalData.strength * shadowData.strength <= 0.0) {
        shadow = GetBakedShadow(shadowData.shadowMask, directionalData.shadowMaskChannel, abs(directionalData.strength));
    }
    else {
        shadow = GetCascadedShadow(directionalData, shadowData, surfaceWS);
        shadow = MixBakedAndRealtimeShadows(shadowData, shadow, directionalData.shadowMaskChannel, directionalData.strength);
    }
    return shadow;
}

float GetOtherShadowAttenuation(otherShadowData other, shadowData global, surface surfaceWS) {
#if !defined(_RECEIVE_SHADOWS)
    return 1.0;
#endif
    float shadow;
    if (other.strength > 0.0) {
        shadow = GetBakedShadow(global.shadowMask, other.shadowMaskChannel, other.strength);
    }
    else {
        shadow = 1.0;
    }
    return shadow;
}
#endif