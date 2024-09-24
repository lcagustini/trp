#ifndef TRP_UNLIT_PASS
#define TRP_UNLIT_PASS

struct vertexAttrib
{
    float3 positionOS : POSITION;
    float2 baseUV : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct fragVaryings {
    float4 positionCS : SV_POSITION;
    float2 baseUV : VAR_BASE_UV;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

fragVaryings UnlitPassVertex(vertexAttrib input) {
    UNITY_SETUP_INSTANCE_ID(input);

    fragVaryings output;
    UNITY_TRANSFER_INSTANCE_ID(input, output);

    float3 positionWS = TransformObjectToWorld(input.positionOS);
    output.positionCS = TransformWorldToHClip(positionWS);

    output.baseUV = TransformBaseUV(input.baseUV);

    return output;
}

float4 UnlitPassFragment(fragVaryings input) : SV_TARGET {
    UNITY_SETUP_INSTANCE_ID(input);

    float4 base = GetBase(input.baseUV);
#if _CLIPPING_ON
    clip(base.a - GetCutoff(input.baseUV));
#endif
    return base;
}

#endif