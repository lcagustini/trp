#ifndef TRP_LIGHT
#define TRP_LIGHT

#define MAX_DIRECTIONAL_LIGHT_COUNT 4

#include "../ShaderLibrary/Shadows.hlsl"

CBUFFER_START(_TRPDirectionalLight)
    int _DirectionalLightCount;
    float4 _DirectionalLightColors[MAX_DIRECTIONAL_LIGHT_COUNT];
    float4 _DirectionalLightDirections[MAX_DIRECTIONAL_LIGHT_COUNT];
    float4 _DirectionalLightShadowData[MAX_DIRECTIONAL_LIGHT_COUNT];
CBUFFER_END

struct light {
    float3 color;
    float3 direction;
    float attenuation;
};

int GetDirectionalLightCount () {
    return _DirectionalLightCount;
}

directionalShadowData GetDirectionalShadowData(int lightIndex, shadowData shadowData) {
    directionalShadowData data;
    data.strength = _DirectionalLightShadowData[lightIndex].x * shadowData.strength;
    data.tileIndex = _DirectionalLightShadowData[lightIndex].y + shadowData.cascadeIndex;
    data.normalBias = _DirectionalLightShadowData[lightIndex].z;
    return data;
}

light GetDirectionalLight(int index, surface surfaceWS, shadowData shadowData) {
    light light;
    light.color = _DirectionalLightColors[index].rgb;
    light.direction = _DirectionalLightDirections[index].xyz;
    directionalShadowData dirShadowData = GetDirectionalShadowData(index, shadowData);
    light.attenuation = GetDirectionalShadowAttenuation(dirShadowData, shadowData, surfaceWS);
    return light;
}

#endif