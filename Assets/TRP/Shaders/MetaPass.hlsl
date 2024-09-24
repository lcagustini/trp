#ifndef TRP_UNLIT_PASS
#define TRP_UNLIT_PASS

#include "../ShaderLibrary/Surface.hlsl"

struct vertexAttrib
{
    float3 positionOS : POSITION;
    float2 baseUV : TEXCOORD0;
    float2 lightMapUV : TEXCOORD1;
};

struct fragVaryings {
    float4 positionCS : SV_POSITION;
    float2 baseUV : VAR_BASE_UV;
};

fragVaryings MetaPassVertex(vertexAttrib input) {
    input.positionOS.xy = input.lightMapUV * unity_LightmapST.xy + unity_LightmapST.zw;
    input.positionOS.z = input.positionOS.z > 0.0 ? FLT_MIN : 0.0;
    
    fragVaryings output;
    output.positionCS = TransformWorldToHClip(input.positionOS);
    output.baseUV = TransformBaseUV(input.baseUV);
    return output;
}

float4 MetaPassFragment(fragVaryings input) : SV_TARGET {
    float4 base = GetBase(input.baseUV);

    surface surface;
    surface.position = 0;
    surface.normal = 0;
    surface.color = base.rgb;
    surface.alpha = 0;
    surface.depth = 0;
    surface.dither = 0;
    
    float4 meta = 0.0;
    if (unity_MetaFragmentControl.x) {
        meta = float4(surface.color, 1.0);
        meta.rgb = min(PositivePow(meta.rgb, unity_OneOverOutputBoost), unity_MaxOutputValue);
    }
    else if (unity_MetaFragmentControl.y) {
        meta = float4(GetEmission(input.baseUV), 1.0);
    }
    return meta;
}

#endif