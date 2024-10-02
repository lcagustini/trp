#ifndef TRP_LIGHT
#define TRP_LIGHT

#define MAX_DIRECTIONAL_LIGHT_COUNT 4
#define MAX_OTHER_LIGHT_COUNT 64

#include "../ShaderLibrary/Shadows.hlsl"

CBUFFER_START(_TRPLight)
    int _DirectionalLightCount;
    float4 _DirectionalLightColors[MAX_DIRECTIONAL_LIGHT_COUNT];
    float4 _DirectionalLightDirections[MAX_DIRECTIONAL_LIGHT_COUNT];
    float4 _DirectionalLightShadowData[MAX_DIRECTIONAL_LIGHT_COUNT];

    int _OtherLightCount;
    float4 _OtherLightColors[MAX_OTHER_LIGHT_COUNT];
    float4 _OtherLightPositions[MAX_OTHER_LIGHT_COUNT];
    float4 _OtherLightDirections[MAX_OTHER_LIGHT_COUNT];
    float4 _OtherLightSpotAngles[MAX_OTHER_LIGHT_COUNT];
    float4 _OtherLightShadowData[MAX_OTHER_LIGHT_COUNT];
CBUFFER_END

struct light {
    float3 color;
    float3 direction;
    float attenuation;
};

int GetDirectionalLightCount() {
    return _DirectionalLightCount;
}

int GetOtherLightCount() {
    return _OtherLightCount;
}

directionalShadowData GetDirectionalShadowData(int lightIndex, shadowData shadowData) {
    directionalShadowData data;
    data.strength = _DirectionalLightShadowData[lightIndex].x;
    data.tileIndex = _DirectionalLightShadowData[lightIndex].y + shadowData.cascadeIndex;
    data.normalBias = _DirectionalLightShadowData[lightIndex].z;
    data.shadowMaskChannel = _DirectionalLightShadowData[lightIndex].w;
    return data;
}

otherShadowData GetOtherShadowData(int lightIndex) {
    otherShadowData data;
    data.strength = _OtherLightShadowData[lightIndex].x;
    data.tileIndex = _OtherLightShadowData[lightIndex].y;
    data.shadowMaskChannel = _OtherLightShadowData[lightIndex].w;
    data.isPoint = _OtherLightShadowData[lightIndex].z == 1.0;
    data.lightPositionWS = 0.0;
    data.lightDirectionWS = 0.0;
    data.spotDirectionWS = 0.0;
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

light GetOtherLight(int index, surface surfaceWS, shadowData shadowData) {
    light light;

    light.color = _OtherLightColors[index].rgb;

    float3 position = _OtherLightPositions[index].xyz;
    float3 ray = position - surfaceWS.position;
    light.direction = normalize(ray);
    
    float3 spotDirection = _OtherLightDirections[index].xyz;
    float4 spotAngles = _OtherLightSpotAngles[index];
    float spotAttenuation = Square(saturate(dot(spotDirection, light.direction) * spotAngles.x + spotAngles.y));

    otherShadowData otherShadowData = GetOtherShadowData(index);
    otherShadowData.lightPositionWS = position;
    otherShadowData.lightDirectionWS = light.direction;
    otherShadowData.spotDirectionWS = spotDirection;
    
    float distanceSqr = max(dot(ray, ray), 0.00001);
    float rangeAttenuation = Square(saturate(1.0 - Square(distanceSqr * _OtherLightPositions[index].w)));
    light.attenuation = GetOtherShadowAttenuation(otherShadowData, shadowData, surfaceWS) * spotAttenuation * rangeAttenuation / distanceSqr;
    
    return light;
}

#endif