#ifndef TRP_LIGHT
#define TRP_LIGHT

#define MAX_DIRECTIONAL_LIGHT_COUNT 4

CBUFFER_START(_TRPDirectionalLight)
    int _DirectionalLightCount;
    float4 _DirectionalLightColors[MAX_DIRECTIONAL_LIGHT_COUNT];
    float4 _DirectionalLightDirections[MAX_DIRECTIONAL_LIGHT_COUNT];
CBUFFER_END

struct light {
    float3 color;
    float3 direction;
};

int GetDirectionalLightCount () {
    return _DirectionalLightCount;
}

light GetDirectionalLight(int index) {
    light light;
    light.color = _DirectionalLightColors[index].rgb;
    light.direction = _DirectionalLightDirections[index].xyz;
    return light;
}

#endif