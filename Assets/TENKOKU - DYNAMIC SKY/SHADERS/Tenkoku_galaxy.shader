Shader "TENKOKU/galaxy_shader" {
Properties {
	_SIntensity ("Star Intensity", Range(0.0,1.0)) = 1.0
	_GIntensity ("Galaxy Intensity", Range(0.0,1.0)) = 1.0
	_Color ("Main Color", Color) = (1,1,1,1)
	_GTex ("Galaxy Tex", 2D) = "white" {}
	_STex ("Star Detail Tex", 2D) = "white" {} 
	_CubeTex ("Cube Tex", Cube) = "white" {}
	_perturbation ("Perturbation", Range(0.0,1.0)) = 1.0
}

SubShader {
	Tags { "Queue"="Background+1601" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
	Blend One One
	Cull Front
	ZWrite Off
	
	Offset 1,996000
	
	Pass {
		Name "GalaxyForward"
		Tags { "LightMode" = "UniversalForward" }

		HLSLPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		#pragma target 3.0

		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

		CBUFFER_START(UnityPerMaterial)
			float _GIntensity;
			float _SIntensity;
			float4 _Color;
			float _useCube;
			float _useGlxy;
		CBUFFER_END

		TEXTURE2D(_GTex); SAMPLER(sampler_GTex);
		TEXTURECUBE(_CubeTex); SAMPLER(sampler_CubeTex);

		// Globals
		float _tenkokuIsLinear;
		float4 _TenkokuAmbientColor;
		float _Tenkoku_AtmosphereDensity;

		struct Attributes {
			float4 positionOS : POSITION;
			float3 uv : TEXCOORD0;
			float3 normalOS : NORMAL;
		};

		struct Varyings {
			float4 positionCS : SV_POSITION;
			float3 uv : TEXCOORD0;
			float3 normalWS : TEXCOORD1;
		};

		Varyings vert (Attributes v)
		{
			Varyings o;
			o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
			o.uv = v.uv;
			o.uv.y = 1.0 - o.uv.y;
			o.normalWS = TransformObjectToWorldNormal(v.normalOS);
			return o;
		}

		half4 frag (Varyings i) : SV_Target
		{
			half3 col = half3(0,0,0);
			
			// galaxy 2D spheremap
			if (_useCube == 0.0 && _useGlxy <= 1.0){
				half3 gtex = SAMPLE_TEXTURE2D(_GTex, sampler_GTex, i.uv.xy).rgb;
				col = lerp(half3(0,0,0), gtex * _GIntensity, _Color.a);
			}

			// galaxy cubemap
			if (_useCube == 1.0 && _useGlxy <= 1.0){
				half3 gCtex = SAMPLE_TEXTURECUBE(_CubeTex, sampler_CubeTex, i.normalWS).rgb;
				col = lerp(half3(0,0,0), gCtex * _GIntensity, _Color.a);
			}

			// gamma
			half gammaFac = lerp(2.4, 1.0, _tenkokuIsLinear);
			col *= gammaFac;

			// final masking
			float mask = saturate(1.0 - _TenkokuAmbientColor.r);
			mask -= lerp(0.0, 1.0, _Tenkoku_AtmosphereDensity * 0.25);
			mask = saturate(mask);
			
			return half4(col * mask, 1.0);
		}
		ENDHLSL
	}
}
}