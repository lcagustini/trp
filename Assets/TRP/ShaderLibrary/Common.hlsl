#ifndef TRP_COMMON
#define TRP_COMMON

#if defined(_SHADOW_MASK_DISTANCE) || defined(_SHADOW_MASK_ALWAYS)
#define SHADOWS_SHADOWMASK
#endif

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "UnityInput.hlsl"

#define UNITY_MATRIX_M unity_ObjectToWorld
#define UNITY_MATRIX_I_M unity_WorldToObject
#define UNITY_MATRIX_V unity_MatrixV
#define UNITY_MATRIX_I_V unity_MatrixInvV
#define UNITY_MATRIX_VP unity_MatrixVP
#define UNITY_PREV_MATRIX_M unity_prev_MatrixM
#define UNITY_PREV_MATRIX_I_M unity_prev_MatrixIM
#define UNITY_MATRIX_P glstate_matrix_projection

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"

float DistanceSquared(const float3 pA, const float3 pB) {
    return dot(pA - pB, pA - pB);
}

void ClipLOD(float2 positionCS, float fade) {
#if defined(LOD_FADE_CROSSFADE)
    float dither = InterleavedGradientNoise(positionCS.xy, 0);
    clip(fade + (fade < 0.0 ? dither : -dither));
#endif
}

float Square(const float a) {
    return a * a;
}

#endif