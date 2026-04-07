Shader "TENKOKU/fx_Particle_Lightning" {
Properties {
	_TintColor ("Tint Color", Color) = (0.5,0.5,0.5,0.5)
	_MainTex ("Particle Texture", 2D) = "white" {}
	_LightFac ("LightFactor", Range(0.0,1.0)) = 1.0
	_LightningFac ("LightningFactor", Range(0.0,1.0)) = 1.0
}

SubShader {
	Tags { "Queue"="Overlay" "IgnoreProjector"="True" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
	Blend One One
	ColorMask RGBA
	Cull Off Lighting Off ZWrite Off

	Pass {
		Name "LightningForward"
		Tags { "LightMode" = "UniversalForward" }

		HLSLPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		#pragma target 3.0

		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

		CBUFFER_START(UnityPerMaterial)
			float4 _TintColor;
			float4 _MainTex_ST;
			float _LightFac, _LightningFac;
		CBUFFER_END

		TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

		// Globals
		float Tenkoku_LightningIntensity;
		float Tenkoku_LightningLightIntensity;

		struct Attributes {
			float4 positionOS : POSITION;
			float4 color : COLOR;
			float2 uv : TEXCOORD0;
		};

		struct Varyings {
			float4 positionCS : SV_POSITION;
			float4 color : COLOR;
			float2 uv : TEXCOORD0;
		};

		Varyings vert (Attributes v) {
			Varyings o;
			o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
			o.color = v.color;
			o.uv = v.uv * _MainTex_ST.xy + _MainTex_ST.zw;
			return o;
		}

		half4 frag (Varyings i) : SV_Target {
			half4 col = i.color * _TintColor * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
			col.rgb = col.rgb * _TintColor.rgb * Tenkoku_LightningLightIntensity * lerp(0.5, 10.0, col.b);
			col.a = 1.0;
			return col;
		}
		ENDHLSL
	}
}
}
