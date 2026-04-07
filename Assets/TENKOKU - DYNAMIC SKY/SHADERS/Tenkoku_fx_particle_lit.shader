Shader "TENKOKU/fx_Particle_Lit" {
Properties {
	_TintColor ("Tint Color", Color) = (0.5,0.5,0.5,0.5)
	_MainTex ("Particle Texture", 2D) = "white" {}
	_NightFac ("Night Factor", Range(0.0,1.0)) = 0.1
	_LightFac ("Light Factor", Range(0.0,1.0)) = 1.0
	_LightningFac ("Lightning Factor", Range(0.0,1.0)) = 1.0
	_InvFade ("Soft Particles Factor", Range(0.01,3.0)) = 1.0
	_OverBright ("Overbright", Range(0.0,5.0)) = 1.0
}

SubShader {
	Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
	Blend SrcAlpha OneMinusSrcAlpha
	ColorMask RGB
	Cull Off Lighting Off ZWrite Off

	Pass {
		Name "LitParticleForward"
		Tags { "LightMode" = "UniversalForward" }

		HLSLPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		#pragma target 3.0

		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

		CBUFFER_START(UnityPerMaterial)
			float4 _TintColor;
			float4 _MainTex_ST;
			float _LightFac, _LightningFac;
			float _NightFac;
			float _OverBright;
			float _InvFade;
		CBUFFER_END

		TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
		TEXTURE2D(_Tenkoku_SkyTex); SAMPLER(sampler_Tenkoku_SkyTex);

		// Globals
		float _Tenkoku_Ambient;
		float _Tenkoku_AmbientGI;
		float4 _Tenkoku_overcastColor;
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
            float4 screenPos : TEXCOORD1;
		};

		Varyings vert (Attributes v) {
			Varyings o;
			o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
			o.color = v.color;
			o.uv = v.uv * _MainTex_ST.xy + _MainTex_ST.zw;
            o.screenPos = ComputeScreenPos(o.positionCS);
			return o;
		}

		half4 frag (Varyings i) : SV_Target {
            float2 screenUV = i.screenPos.xy / max(0.001, i.screenPos.w);
            float rawDepth = SampleSceneDepth(screenUV);
            float sceneZ = LinearEyeDepth(rawDepth, _ZBufferParams);
            float partZ = i.screenPos.z; // This might need adjustment for perspective
            float fade = saturate(_InvFade * (sceneZ - i.positionCS.w)); // Standard soft particle approach using clip w as distance

			half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
			half4 col = i.color * _TintColor * tex;
			col.a *= fade * 3.0;

			// Lit logic
			col.rgb *= lerp(1.0, _Tenkoku_AmbientGI, _LightFac);
			col.rgb = lerp(col.rgb, SAMPLE_TEXTURE2D(_Tenkoku_SkyTex, sampler_Tenkoku_SkyTex, screenUV).rgb, 0.04);
			col.rgb *= lerp(1.0, 0.35, _Tenkoku_overcastColor.a) * _TintColor.rgb;

			// Lightning and night lightening
			col.rgb = max(col.rgb, (tex.rgb * _NightFac).rgb) + Tenkoku_LightningLightIntensity * 2.0 * _LightningFac;
			col.rgb *= _OverBright;

			return col;
		}
		ENDHLSL
	}
}
}
