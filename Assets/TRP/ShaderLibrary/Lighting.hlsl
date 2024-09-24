#ifndef TRP_LIGHTING
#define TRP_LIGHTING

#include "../ShaderLibrary/GI.hlsl"
#include "../ShaderLibrary/Surface.hlsl"
#include "../ShaderLibrary/Light.hlsl"

float3 IncomingLight(surface surface, light light) {
    return saturate(dot(surface.normal, light.direction)) * light.color * light.attenuation;
}

float3 GetLighting(surface surface, light light) {
    return IncomingLight(surface, light) * surface.color;
}

float3 GetLighting(surface surfaceWS, GI gi) {
    shadowData shadowData = GetShadowData(surfaceWS);
    float3 color = gi.diffuse;
    shadowData.shadowMask = gi.shadowMask;
    for (int i = 0; i < GetDirectionalLightCount(); i++) {
        color += GetLighting(surfaceWS, GetDirectionalLight(i, surfaceWS, shadowData));
    }
    return color;
}

#endif