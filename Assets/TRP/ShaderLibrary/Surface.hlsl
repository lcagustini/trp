#ifndef TRP_SURFACE
#define TRP_SURFACE

struct surface {
    float3 position;
    float3 normal;
    float3 color;
    float alpha;
    float depth;
    float dither;
};

#endif