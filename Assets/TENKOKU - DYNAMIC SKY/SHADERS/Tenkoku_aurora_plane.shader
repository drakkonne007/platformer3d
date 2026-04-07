Shader "TENKOKU/aurora_plane" {
	Properties {
		_Height ("Height", float) = 1.0
		_aurSpeed ("Aurora Speed", Range(0.0, 1.0)) = 0.25
		_aurLatSpeed ("Aurora Lateral Speed", Range(0.0, 1.0)) = 0.1
		_aurDir ("Aurora Direction", Range(-1.0, 1.0)) = -1.0
		_distAmt ("Distortion Amount", Range(0.0, 1.0)) = 0.1
		_overallAlpha ("Overall Alpha", Range(0.0, 1.0)) = 1.0
		_aurTint1a ("Aurora Tint1a", Color) = (1.0, 1.0, 1.0, 1.0)
		_aurTint1b ("Aurora Tint1b", Color) = (1.0, 1.0, 1.0, 1.0)
		_aurTint2a ("Aurora Tint2a", Color) = (1.0, 1.0, 1.0, 1.0)
		_aurTint2b ("Aurora Tint2b", Color) = (1.0, 1.0, 1.0, 1.0)
		_aurTint3a ("Aurora Tint3a", Color) = (1.0, 1.0, 1.0, 1.0)
		_aurTint3b ("Aurora Tint3b", Color) = (1.0, 1.0, 1.0, 1.0)
		_MainTex ("Clouds A", 2D) = "white" {}
		_DistortTex ("Normal Distortion)", 2D) = "white" {}
		_BlendTex ("Blend", 2D) = "white" {}
	}

	SubShader {
		Tags { "Queue"="Transparent-1" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
		Blend One One
		Cull Off
		ZWrite Off

		HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

		CBUFFER_START(UnityPerMaterial)
			float _Height;
			float _aurSpeed;
			float _aurLatSpeed;
			float _aurDir;
			float _overallAlpha;
			float _distAmt;
			float4 _aurTint1a, _aurTint1b;
			float4 _aurTint2a, _aurTint2b;
			float4 _aurTint3a, _aurTint3b;
		CBUFFER_END

		TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
		TEXTURE2D(_DistortTex); SAMPLER(sampler_DistortTex);
		TEXTURE2D(_BlendTex); SAMPLER(sampler_BlendTex);

		// Globals
		float _Tenkoku_AuroraAmt;
		float _Tenkoku_AuroraSpd;

		struct Attributes {
			float4 positionOS : POSITION;
			float2 uv : TEXCOORD0;
			float3 normalOS : NORMAL;
		};

		struct Varyings {
			float4 positionCS : SV_POSITION;
			float2 uv : TEXCOORD0;
		};

		Varyings vert_aurora(Attributes v, float offsetMult) {
			Varyings o;
			float3 pos = v.positionOS.xyz;
			pos -= v.normalOS * (offsetMult * _Height);
			o.positionCS = TransformObjectToHClip(pos);
			o.uv = v.uv;
			return o;
		}

		half4 frag_aurora(Varyings i, float4 tintA, float4 tintB, float channelIdx, float alphaMult) {
			float2 distortUV = i.uv + float2(_Time.y * 0.05, _Time.y * 0.01);
			float3 distort = UnpackNormal(SAMPLE_TEXTURE2D(_DistortTex, sampler_DistortTex, distortUV));
			
			float2 mainUV = i.uv + (distort.xz * _distAmt);
			mainUV += float2(0, _aurDir) * (_Time.y * _aurSpeed * _Tenkoku_AuroraSpd * 0.1);
			mainUV += float2(-_Time.y * _aurLatSpeed * _Tenkoku_AuroraSpd * 0.1, 0);

			half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, mainUV);
			half edgeBlend = SAMPLE_TEXTURE2D(_BlendTex, sampler_BlendTex, i.uv).r;
			half colMorph = SAMPLE_TEXTURE2D(_BlendTex, sampler_BlendTex, i.uv * 0.25 + float2(0, _Time.y * 0.25)).a;

			half3 color = lerp(tintB.rgb, tintA.rgb, colMorph);
			float chan = (channelIdx == 0) ? c.r : (channelIdx == 1 ? c.g : c.b);
			float alpha = chan * tintA.a * edgeBlend * alphaMult * _Tenkoku_AuroraAmt;

			return half4(color * alpha * 2.0 * _overallAlpha, alpha * _overallAlpha);
		}
		ENDHLSL

		// Consolidated some passes to keep it manageable in URP while maintaining layering
		Pass { Name "AuroraTop1" HLSLPROGRAM #pragma vertex vert #pragma fragment frag
			Varyings vert(Attributes v) { return vert_aurora(v, 17.5); }
			half4 frag(Varyings i) : SV_Target { return frag_aurora(i, _aurTint3a, _aurTint3b, 2, 0.2); }
		ENDHLSL }

		Pass { Name "AuroraTop2" HLSLPROGRAM #pragma vertex vert #pragma fragment frag
			Varyings vert(Attributes v) { return vert_aurora(v, 14.5); }
			half4 frag(Varyings i) : SV_Target { return frag_aurora(i, _aurTint3a, _aurTint3b, 2, 0.3); }
		ENDHLSL }

		Pass { Name "AuroraMid1" HLSLPROGRAM #pragma vertex vert #pragma fragment frag
			Varyings vert(Attributes v) { return vert_aurora(v, 11.5); }
			half4 frag(Varyings i) : SV_Target { return frag_aurora(i, _aurTint2a, _aurTint2b, 1, 0.2); }
		ENDHLSL }

		Pass { Name "AuroraMid2" HLSLPROGRAM #pragma vertex vert #pragma fragment frag
			Varyings vert(Attributes v) { return vert_aurora(v, 7.5); }
			half4 frag(Varyings i) : SV_Target { return frag_aurora(i, _aurTint2a, _aurTint2b, 1, 0.3); }
		ENDHLSL }

		Pass { Name "AuroraBot1" HLSLPROGRAM #pragma vertex vert #pragma fragment frag
			Varyings vert(Attributes v) { return vert_aurora(v, 2.5); }
			half4 frag(Varyings i) : SV_Target { return frag_aurora(i, _aurTint1a, _aurTint1b, 0, 0.2); }
		ENDHLSL }

		Pass { Name "AuroraBot2" HLSLPROGRAM #pragma vertex vert #pragma fragment frag
			Varyings vert(Attributes v) { return vert_aurora(v, 0.0); }
			half4 frag(Varyings i) : SV_Target { return frag_aurora(i, _aurTint1a, _aurTint1b, 0, 0.7); }
		ENDHLSL }
	}
}
