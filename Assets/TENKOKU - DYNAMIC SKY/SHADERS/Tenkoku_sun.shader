Shader "TENKOKU/sun_shader" {
	Properties {
		_TintColor ("Tint Color", Color) = (0.5,0.5,0.5,0.5)
		_CoronaColor ("Corona Color", Color) = (0.5,0.5,0.5,0.5)
		_MainTex ("BRDF", 2D) = "white" {}
		_overBright ("OverBright", float) = 1.0
		_dispStrength ("Displace Amount", Range(0.0,10.0)) = 1.0
	}

	SubShader {
		Tags { "Queue"="Transparent+1604" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "PreviewType"="Billboard" }
		Blend One One
		Cull Off
		ZWrite Off
        ZTest LEqual

		Pass {
            Name "SunForward"
            Tags { "LightMode" = "UniversalForward" }

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			CBUFFER_START(UnityPerMaterial)
				float4 _TintColor;
				float4 _CoronaColor;
				float _dispStrength;
				float _overBright;
                float4 _TenkokuSunColor;
			CBUFFER_END

			TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

			// Globals
			float4 _Tenkoku_overcastColor;
			float _Tenkoku_AmbientGI;
			float _Tenkoku_Ambient;
			float _Tenkoku_EclipseFactor;

			struct Attributes {
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
				float4 color : COLOR;
			};

			struct Varyings {
				float4 positionCS : SV_POSITION;
				float3 normalWS : TEXCOORD0;
				float3 viewDirWS : TEXCOORD1;
				float4 positionWS : TEXCOORD3;
			};

			Varyings vert (Attributes v) {
				Varyings o;
				float3 pos = v.positionOS.xyz;
				// vertex offset from original shader: v.vertex.xyz += (v.normal * 0.75);
				pos += v.normalOS * 0.75;
				
				o.positionCS = TransformObjectToHClip(pos);
				o.normalWS = TransformObjectToWorldNormal(v.normalOS);
				o.positionWS = float4(TransformObjectToWorld(pos), 1.0);
				o.viewDirWS = normalize(_WorldSpaceCameraPos - o.positionWS.xyz);
				return o;
			}

			half4 frag (Varyings i) : SV_Target {
				float3 normal = normalize(i.normalWS);
				float3 viewDir = normalize(i.viewDirWS);
                
                // In the original shader, lightDir was used in LightingRamp but not explicitly passed from surf.
                // In sky shaders, lightDir is usually the Sun vector.
                // However, the sun shader is usually a billboard or sphere at the Sun position.
                // Let's use the view direction relative to normal for the "ramp" effect.
				float NdotE = dot(normal, viewDir);
                
                // Original legacy logic for Sun lighting model:
                // float diff = (NdotL * 0.5) + 0.5;
                // float2 brdfUV = float2(NdotE * 1.0, diff);
                // For a sun disc, NdotL is usually 1 (facing camera) or we use NdotE for the ramp.
                float2 brdfUV = float2(NdotE, 0.5);
                float3 BRDF = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, brdfUV).rgb;

                float4 c = float4(_TenkokuSunColor.rgb, 1.0);
                c.a = saturate(BRDF.b);
                c.a *= _overBright;
                c.a = lerp(1.0, c.a * dot(-viewDir, normal), _CoronaColor.a);
                c.rgb *= c.a;

                c.rgb = _TintColor.rgb;

                half sSize = saturate(1.0 - saturate(_Tenkoku_overcastColor.a * 3.0));
                sSize *= saturate(lerp(-1.0, 1.0, _Tenkoku_Ambient));

                float alpha = 0;
                float dotVN = dot(viewDir, -normal);
                alpha += saturate(lerp(-0.5, 1.0, dotVN)) * 0.05 * sSize * _Tenkoku_EclipseFactor;
                alpha += saturate(lerp(-1.0, 1.0, dotVN)) * 0.05 * sSize * _Tenkoku_EclipseFactor;
                alpha += saturate(lerp(-2.0, 1.0, dotVN)) * 0.1 * sSize * _Tenkoku_EclipseFactor;
                alpha += saturate(lerp(-3.0, 1.0, dotVN)) * 0.1 * sSize * _Tenkoku_EclipseFactor;
                alpha += saturate(lerp(-6.0, 1.0, dotVN)) * 0.1 * sSize * _Tenkoku_EclipseFactor;
                alpha += saturate(lerp(-2.0, 1.0, dotVN)) * sSize;

                c.rgb = lerp(_CoronaColor.rgb, c.rgb * _TenkokuSunColor.rgb, alpha);
                c.rgb = lerp(c.rgb, c.rgb * _TintColor.rgb, _TintColor.a);
                c.rgb += (_overBright * saturate(lerp(0.0, 1.0, dotVN)) * saturate(lerp(0.0, 4.0, _Tenkoku_AmbientGI)));

                alpha = saturate(alpha - saturate(_Tenkoku_overcastColor.a * 3.0));
                alpha *= saturate(lerp(0.0, 4.0, _Tenkoku_Ambient));

				return half4(c.rgb, alpha);
			}
			ENDHLSL
		}
	}
}
