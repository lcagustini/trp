#ifndef TRP_SHADOW_CASTER_PASS
#define TRP_SHADOW_CASTER_PASS

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

fragVaryings ShadowCasterPassVertex(vertexAttrib input) {
    UNITY_SETUP_INSTANCE_ID(input);

    fragVaryings output;
    UNITY_TRANSFER_INSTANCE_ID(input, output);

#ifdef _SHADOWS_OFF
    output.positionCS = float4(0, 0, 2, 1);
#else
    float3 positionWS = TransformObjectToWorld(input.positionOS);
    output.positionCS = TransformWorldToHClip(positionWS);

    // Shadow Pancaking
#if UNITY_REVERSED_Z
    output.positionCS.z = min(output.positionCS.z, output.positionCS.w * UNITY_NEAR_CLIP_VALUE);
#else
    output.positionCS.z = max(output.positionCS.z, output.positionCS.w * UNITY_NEAR_CLIP_VALUE);
#endif
    
    output.baseUV = TransformBaseUV(input.baseUV);
#endif

    return output;
}

void ShadowCasterPassFragment(fragVaryings input) {
    UNITY_SETUP_INSTANCE_ID(input);

    float4 base = GetBase(input.baseUV);

#ifdef _SHADOWS_CLIP
    clip(base.a - GetCutoff(input.baseUV));
#elif _SHADOWS_DITHER
    float dither = InterleavedGradientNoise(input.positionCS.xy, 0);
    clip(base.a - dither);
#endif
}

#endif