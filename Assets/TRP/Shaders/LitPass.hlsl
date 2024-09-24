#ifndef TRP_LIT_PASS
#define TRP_LIT_PASS

#include "../ShaderLibrary/Lighting.hlsl"

struct vertexAttrib
{
    float3 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float2 baseUV : TEXCOORD0;
    GI_ATTRIBUTE_DATA
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct fragVaryings {
    float4 positionCS : SV_POSITION;
    float3 positionWS : VAR_POSITION;
    float3 normalWS : VAR_NORMAL;
    float2 baseUV : VAR_BASE_UV;
    GI_VARYINGS_DATA
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

fragVaryings LitPassVertex(vertexAttrib input) {
    UNITY_SETUP_INSTANCE_ID(input);

    fragVaryings output;
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    TRANSFER_GI_DATA(input, output);

    float3 positionWS = TransformObjectToWorld(input.positionOS);
    output.positionWS = positionWS;
    output.positionCS = TransformWorldToHClip(positionWS);
    
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);

    output.baseUV = TransformBaseUV(input.baseUV);

    return output;
}

float4 LitPassFragment(fragVaryings input) : SV_TARGET {
    UNITY_SETUP_INSTANCE_ID(input);

    float4 base = GetBase(input.baseUV);

    ClipLOD(input.positionCS.xy, unity_LODFade.x);

#if _CLIPPING_ON
    clip(base.a - GetCutoff(input.baseUV));
#endif

    surface surface;
    surface.position = input.positionWS;
    surface.normal = normalize(input.normalWS);
    surface.color = base.rgb;
    surface.alpha = base.a;
    surface.depth = -TransformWorldToView(input.positionWS).z;
    surface.dither = InterleavedGradientNoise(input.positionCS.xy, 0);

    const GI gi = GetGI(GI_FRAGMENT_DATA(input), surface);
    float3 color = GetLighting(surface, gi);
    color += GetEmission(input.baseUV);
    return float4(color, surface.alpha);
}

#endif