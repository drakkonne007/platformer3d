Shader "Hidden/TenkokuSunShafts" {
	Properties {
		_MainTex ("Base", 2D) = "" {}
		_ColorBuffer ("Color", 2D) = "" {}
		_Skybox ("Skybox", 2D) = "" {}
	}
	
	SubShader {
		Tags { "RenderPipeline" = "UniversalPipeline" }
		ZTest Always Cull Off ZWrite Off

		HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

		struct Attributes {
			float4 positionOS : POSITION;
			float2 uv : TEXCOORD0;
		};

		struct Varyings {
			float4 positionCS : SV_POSITION;
			float2 uv : TEXCOORD0;
		};

		struct VaryingsRadial {
			float4 positionCS : SV_POSITION;
			float2 uv : TEXCOORD0;
			float2 blurVector : TEXCOORD1;
		};

		TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
		TEXTURE2D(_ColorBuffer); SAMPLER(sampler_ColorBuffer);
		TEXTURE2D(_Skybox); SAMPLER(sampler_Skybox);

		float4 _SunThreshold;
		float4 _ColorBlock;
		float4 _SunColor;
		float4 _TintColor;
		float4 _BlurRadius4;
		float4 _SunPosition;
		float4 _MainTex_TexelSize;	
		float4 _Tenkoku_overcastColor;

		#define SAMPLES_FLOAT 5.0
		#define SAMPLES_INT 5

		Varyings vert(Attributes v) {
			Varyings o;
			o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
			o.uv = v.uv;
			return o;
		}

		half4 fragScreen(Varyings i) : SV_Target { 
			half4 colorA = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
			half4 colorB = SAMPLE_TEXTURE2D(_ColorBuffer, sampler_ColorBuffer, i.uv);
			half4 depthMask = saturate(colorB * lerp(_SunColor, _SunColor * _TintColor, _TintColor.a));
			return 1.0 - (1.0 - colorA) * (1.0 - depthMask);
		}

		VaryingsRadial vert_radial(Attributes v) {
			VaryingsRadial o;
			o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
			o.uv = v.uv;
			o.blurVector = (_SunPosition.xy - v.uv) * _BlurRadius4.xy;	
			return o; 
		}
		
		half4 frag_radial(VaryingsRadial i) : SV_Target {	
			half4 color = half4(0,0,0,0);
			float2 uv = i.uv;
			for(int j = 0; j < SAMPLES_INT; j++) {	
				color += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
				uv += i.blurVector; 	
			}
			return color / SAMPLES_FLOAT;
		}	
		
		half TransformColor (half4 val) {
			return dot(max(val.rgb - _ColorBlock.rgb, half3(0,0,0)), half3(1,1,1));
		}
		
		half4 frag_depth (Varyings i) : SV_Target {
			float depthSample = SampleSceneDepth(i.uv);
			float d01 = Linear01Depth(depthSample, _ZBufferParams);
			half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
			
			half2 vec = _SunPosition.xy - i.uv;
			half dist = saturate(_SunPosition.w - length(vec));		
			
			half4 outColor = 0;
			if (d01 > 0.99) {
				outColor = TransformColor(tex) * dist;
			}
			return outColor;
		}
		ENDHLSL

		Pass {
			Name "Screen"
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment fragScreen
			ENDHLSL
		}
		
		Pass {
			Name "Radial"
			HLSLPROGRAM
			#pragma vertex vert_radial
			#pragma fragment frag_radial
			ENDHLSL
		}
		
		Pass {
			Name "Depth"
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag_depth
			ENDHLSL
		}
	}
	Fallback off
}
