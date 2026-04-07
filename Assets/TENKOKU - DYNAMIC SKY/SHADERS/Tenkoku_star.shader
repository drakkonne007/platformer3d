Shader "TENKOKU/star_shader" {
Properties {
	_TintColor ("Tint Color", Color) = (0.5,0.5,0.5,0.5)
	_MainTex ("Particle Texture", 2D) = "white" {}
	_InvFade ("Soft Particles Factor", Range(0.01,3.0)) = 1.0
}

SubShader {
	Tags { "Queue"="Background+1602" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
	Blend One One
	Cull Off
	ZWrite Off
	
	Pass {
		Name "StarForward"
		Tags { "LightMode" = "UniversalForward" }

		HLSLPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		#pragma target 3.0

		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

		CBUFFER_START(UnityPerMaterial)
			float4 _TintColor;
			float4 _MainTex_ST;
		CBUFFER_END

		TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

		// Globals
		float _tenkokuIsLinear;
		float4 _TenkokuAmbientColor;
		float _Tenkoku_AtmosphereDensity;
		float _Tenkoku_Ambient;

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
			float4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
			half4 col = i.color * 3.0 * _TintColor * tex;
			col.rgb *= col.a;

			half gammaFac = lerp(2.2, 1.0, _tenkokuIsLinear);
			col.rgb *= gammaFac;

			col.rgb = col.rgb * (1.0 - saturate(_Tenkoku_Ambient * 4.0));
			return half4(saturate(col.rgb), 1.0);
		}
		ENDHLSL
	}
}
}
