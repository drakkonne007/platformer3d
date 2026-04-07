Shader "TENKOKU/suneclipse_shader" {
 Properties 
 {
  _TintColor ("Tint Color", Color) = (0.5,0.5,0.5,0.5)
  _MainTex ("Base (RGB)", 2D) = "white" {}
 }

 SubShader {
	Tags { "Queue"="Background+1603" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
	Blend One One
	Cull Back
	ZWrite Off
	Offset 1,996000

 	Stencil {
		Ref 2
		Comp Greater
		Pass Replace 
		Fail Keep
		ZFail Replace
	}
	
	Pass {
        Name "SunEclipseForward"
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
		float _Tenkoku_EclipseFactor;
		float _Tenkoku_TotalEclipseFactor;

		struct Attributes {
			float4 positionOS : POSITION;
			float2 uv : TEXCOORD0;
		};

		struct Varyings {
			float4 positionCS : SV_POSITION;
			float2 uv : TEXCOORD0;
		};

		Varyings vert (Attributes v) {
			Varyings o;
			o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
			o.uv = v.uv * _MainTex_ST.xy + _MainTex_ST.zw;
			return o;
		}

		half4 frag (Varyings i) : SV_Target {
			half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
			tex.rgb = tex.rgb * _TintColor.rgb * 2.0 * saturate(lerp(1.0, -16.0, saturate(_Tenkoku_TotalEclipseFactor * 10.0)));
			return half4(tex.rgb, 1.0);
		}
		ENDHLSL 
	}
 } 	
}