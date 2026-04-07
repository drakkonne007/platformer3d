Shader "Hidden/TenkokuBlur" {
	Properties { _MainTex ("", any) = "" {} }
	
	SubShader {
		Tags { "RenderPipeline" = "UniversalPipeline" }
		
		Pass {
			ZTest Always Cull Off ZWrite Off

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			struct Attributes {
				float4 positionOS : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct Varyings {
				float4 positionCS : SV_POSITION;
				float2 uv : TEXCOORD0;
				float2 taps[4] : TEXCOORD1; 
			};

			TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
			float4 _MainTex_TexelSize;
			float4 _BlurOffsets;
			float _Tenkoku_UseElek;

			Varyings vert(Attributes v) {
				Varyings o; 
				o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
				o.uv = v.uv - _BlurOffsets.xy * _MainTex_TexelSize.xy;
				o.taps[0] = o.uv + _MainTex_TexelSize.xy * _BlurOffsets.xy;
				o.taps[1] = o.uv - _MainTex_TexelSize.xy * _BlurOffsets.xy;
				o.taps[2] = o.uv + _MainTex_TexelSize.xy * _BlurOffsets.xy * float2(1, -1);
				o.taps[3] = o.uv - _MainTex_TexelSize.xy * _BlurOffsets.xy * float2(1, -1);
				return o;
			}

			half4 frag(Varyings i) : SV_Target {
				half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.taps[0]);
				color += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.taps[1]);
				color += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.taps[2]);
				color += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.taps[3]); 

				half grayFac = min(min(color.r, color.g), color.b);
				if (_Tenkoku_UseElek == 1.0) {
					color.rgb = lerp(color.rgb, half3(grayFac, grayFac, grayFac), 0.2);
				}

				return color * 0.25;
			}
			ENDHLSL
		}
	}
	Fallback off
}
