Shader "TRP/Lit"
{
    Properties
    {
        _BaseMap("Texture", 2D) = "white" {}
        _BaseColor("Color", Color) = (0.5, 0.5, 0.5, 1.0)

        [Toggle] _CLIPPING ("Alpha Clipping", Float) = 0
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        [KeywordEnum(On, Clip, Dither, Off)] _Shadows ("Shadows", Float) = 0
        [Toggle] _RECEIVE_SHADOWS ("Receive Shadows", Float) = 1

        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 0
        [Enum(Off, 0, On, 1)] _ZWrite ("Z Write", Float) = 1
    }

    SubShader
    {
        Pass
        {
            Tags
            {
                "LightMode" = "TRPLit"
            }

            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]

            HLSLPROGRAM
            #pragma target 3.5

            #pragma shader_feature _CLIPPING_ON
            #pragma shader_feature _RECEIVE_SHADOWS_ON

            #pragma multi_compile _DIRECTIONAL_PCF2 _DIRECTIONAL_PCF3 _DIRECTIONAL_PCF5 _DIRECTIONAL_PCF7
            #pragma multi_compile _CASCADE_BLEND_HARD _CASCADE_BLEND_SOFT _CASCADE_BLEND_DITHER
            #pragma multi_compile_instancing

            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment

            #include "LitPass.hlsl"
            ENDHLSL
        }

		Pass
		{
			Tags
			{
				"LightMode" = "ShadowCaster"
			}

			ColorMask 0

			HLSLPROGRAM
			#pragma target 3.5

            #pragma shader_feature _SHADOWS_ON _SHADOWS_CLIP _SHADOWS_DITHER _SHADOWS_OFF

			#pragma multi_compile_instancing
			
			#pragma vertex ShadowCasterPassVertex
			#pragma fragment ShadowCasterPassFragment
			
			#include "ShadowCasterPass.hlsl"
			ENDHLSL
		}
    }
}