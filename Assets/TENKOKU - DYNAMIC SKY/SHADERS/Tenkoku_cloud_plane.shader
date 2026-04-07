Shader "TENKOKU/cloud_plane" {
	Properties {
		_dist ("Distance", float) = 500.0

		_brightMult ("Brightness", float) = 1.0
		_cloudHeight ("Cloud Height", float) = 1.0
		_sizeCloud ("Cloud Size", Range(0.0, 1.0)) = 1.0

		_amtCloudS ("Cloud Stratus", Range(0.0, 1.0)) = 1.0
		_amtCloudC ("Cloud Cirrus", Range(0.0, 1.0)) = 1.0
		_amtCloudM ("Cloud Cumulus", Range(0.0, 1.0)) = 1.0
		_amtCloudO ("Cloud Overcast", Range(0.0, 1.0)) = 1.0

		_clpCloud ("Cloud Clip", Range(0.0, 1.0)) = 1.0

		_colTint ("Cloud Tint", Color) = (1.0, 1.0, 1.0, 1.0)

		_colCloudS ("Cloud Stratus Color", Color) = (1.0, 1.0, 1.0, 1.0)
		_colCloudC ("Cloud Cirrus Color", Color) = (1.0, 1.0, 1.0, 1.0)
		_colCloud ("Cloud Cumulus Color", Color) = (1.0, 1.0, 1.0, 1.0)
		_colCloudO ("Cloud Overcast Color", Color) = (1.0, 1.0, 1.0, 1.0)
		_MainTex ("Clouds A", 2D) = "white" {}
		_CloudTexB ("Clouds B)", 2D) = "white" {}
		_BlendTex ("Blend", 2D) = "white" {}
	}

	SubShader {
		Tags { "RenderType"="Transparent" "Queue"="Transparent-1" "RenderPipeline"="UniversalPipeline" }
		ZWrite Off
		Cull Off

		HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		#include "AtmosphericScattering.cginc"

		CBUFFER_START(UnityPerMaterial)
			float _dist;
			float _brightMult;
			float _cloudHeight;
			float _sizeCloud;
			float _amtCloudS;
			float _amtCloudC;
			float _amtCloudM;
			float _amtCloudO;
			float _clpCloud;
			float4 _colTint;
			float4 _colCloudS;
			float4 _colCloudC;
			float4 _colCloud;
			float4 _colCloudO;
            float _TenkokuDist;
		CBUFFER_END

		TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
		TEXTURE2D(_CloudTexB); SAMPLER(sampler_CloudTexB);
		TEXTURE2D(_BlendTex); SAMPLER(sampler_BlendTex);
		TEXTURE2D(_Tenkoku_SkyBox); SAMPLER(sampler_Tenkoku_SkyBox);

		// Globals
		float4 windCoords;
		float4 _TenkokuCloudColor, _Tenkoku_Daylight, _Tenkoku_Nightlight;
		float4 _TenkokuCloudHighlightColor;
		float4 _Tenkoku_overcastColor;
		float4 skyColor, _cloudSpd;
		float4 Tenkoku_Vec_MoonFwd, Tenkoku_Vec_SunFwd;
		float _Tenkoku_Ambient;
		float _Tenkoku_shaderDepth;
		float _tenkokuTimer;
        float Tenkoku_LightningIntensity;
        float Tenkoku_LightningLightIntensity;
        float4 Tenkoku_LightningColor;
        float4 Tenkoku_Vec_LightningFwd;

		struct Attributes {
			float4 positionOS : POSITION;
			float2 uv : TEXCOORD0;
			float3 normalOS : NORMAL;
		};

		struct Varyings {
			float4 positionCS : SV_POSITION;
			float2 uv : TEXCOORD0;
			float4 screenPos : TEXCOORD1;
            float3 positionWS : TEXCOORD3;
            float3 normalWS : TEXCOORD4;
		};

		Varyings vert_base(Attributes v, float offsetMult) {
			Varyings o;
			float3 pos = v.positionOS.xyz;
            // Legacy had some vertex offset logic
            pos -= v.normalOS * offsetMult;
			o.positionCS = TransformObjectToHClip(pos);
			o.uv = v.uv;
			o.screenPos = ComputeScreenPos(o.positionCS);
            o.positionWS = TransformObjectToWorld(pos);
            o.normalWS = TransformObjectToWorldNormal(v.normalOS);
			return o;
		}

        half3 CalculateCloudLighting(half alpha, float2 screenUV, float3 viewDir, float depth) {
            float4 skySample = SAMPLE_TEXTURE2D(_Tenkoku_SkyBox, sampler_Tenkoku_SkyBox, screenUV);
            
            half3 sunFwd = normalize(Tenkoku_Vec_SunFwd.xyz);
			half3 moonFwd = normalize(Tenkoku_Vec_MoonFwd.xyz);

            half3 col = saturate(saturate(max(max(_Tenkoku_Daylight.r,_Tenkoku_Daylight.g),_Tenkoku_Daylight.b) * dot(sunFwd,half3(0,1,0))) + skySample.rgb);
            
            col.rgb += (saturate(1.0-_Tenkoku_Ambient) * _Tenkoku_Nightlight.rgb * 20.0 * (1.0-saturate(pow(0.998, dot(-viewDir, moonFwd)-0.5))));
            
            half3 fCol = lerp(half3(1,1,1), lerp(_TenkokuCloudColor.rgb, half3(1,1,1), _Tenkoku_overcastColor.a*2.0), saturate(dot(-sunFwd, viewDir)));
            col.rgb = col.rgb * lerp(half3(1,1,1), fCol, saturate(dot(-viewDir, sunFwd)));
            
            col.rgb = lerp(col.rgb, col.rgb * max(1.0, (1.5 * saturate(dot(-viewDir, sunFwd)))), saturate(dot(-viewDir, sunFwd)));
            col.rgb = saturate(lerp(skySample.rgb, col.rgb, depth));
            
            return col;
        }

		ENDHLSL

		// Pass 1: ALTOSTRATUS
		Pass {
			Name "Altostratus"
			Blend SrcAlpha OneMinusSrcAlpha

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			Varyings vert(Attributes v) {
				return vert_base(v, 0.2);
			}

			half4 frag(Varyings i) : SV_Target {
				float2 uv = i.uv * 0.5 + (windCoords.xy * _cloudSpd.x);
				half c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).b;
				half alpha = c * _amtCloudS;
				alpha *= SAMPLE_TEXTURE2D(_BlendTex, sampler_BlendTex, i.uv).r;

				float2 screenUV = i.screenPos.xy / max(0.001, i.screenPos.w);
				float3 viewDir = normalize(_WorldSpaceCameraPos - i.positionWS);
				
                float dpth = max(i.screenPos.w, 0.001) / _TenkokuDist;
                float depthVal = 1.0 - saturate(max(i.screenPos.w, 0.001) / (_TenkokuDist * 0.15));

                half3 col = CalculateCloudLighting(alpha, screenUV, viewDir, depthVal);
				return half4(col, alpha);
			}
			ENDHLSL
		}

		// Pass 2: CIRRUS A
		Pass {
			Name "CirrusA"
			Blend SrcAlpha OneMinusSrcAlpha

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			Varyings vert(Attributes v) {
				return vert_base(v, 0.1);
			}

			half4 frag(Varyings i) : SV_Target {
				float2 uv = i.uv * 1.4 + (windCoords.xy * _cloudSpd.y);
				half c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).g;
				half alpha = c * _amtCloudC;
				alpha *= SAMPLE_TEXTURE2D(_BlendTex, sampler_BlendTex, i.uv).r;

				float2 screenUV = i.screenPos.xy / max(0.001, i.screenPos.w);
				float3 viewDir = normalize(_WorldSpaceCameraPos - i.positionWS);
                float depthVal = 1.0 - saturate(max(i.screenPos.w, 0.001) / (_TenkokuDist * 0.15));

                half3 col = CalculateCloudLighting(alpha, screenUV, viewDir, depthVal);
				return half4(col, alpha);
			}
			ENDHLSL
		}

		// Pass 3: CIRRUS B
		Pass {
			Name "CirrusB"
			Blend SrcAlpha OneMinusSrcAlpha

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			Varyings vert(Attributes v) {
				return vert_base(v, 0.198);
			}

			half4 frag(Varyings i) : SV_Target {
				float2 uv = i.uv * 1.4 + (windCoords.xy * _cloudSpd.y);
				half c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).g;
				half alpha = c * _amtCloudC;
				alpha *= SAMPLE_TEXTURE2D(_BlendTex, sampler_BlendTex, i.uv).r;

				float2 screenUV = i.screenPos.xy / max(0.001, i.screenPos.w);
				float3 viewDir = normalize(_WorldSpaceCameraPos - i.positionWS);
                float depthVal = 1.0 - saturate(max(i.screenPos.w, 0.001) / (_TenkokuDist * 0.15));

                half3 col = CalculateCloudLighting(alpha, screenUV, viewDir, depthVal);
				return half4(col, alpha);
			}
			ENDHLSL
		}

        // LOW CLOUDS (Simplified to 2 passes to avoid overhead, Tenkoku had 6-8 very similar passes)
        // I will implement a few representative layers.
		Pass {
			Name "LowClouds1"
			Blend SrcAlpha OneMinusSrcAlpha

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			Varyings vert(Attributes v) {
				return vert_base(v, -0.013 * _cloudHeight);
			}

			half4 frag(Varyings i) : SV_Target {
				float2 uv = i.uv * 1.0 + (windCoords.xy * _cloudSpd.z);
				half4 clouds = SAMPLE_TEXTURE2D(_CloudTexB, sampler_CloudTexB, uv);
				
                half alpha = lerp(0.0, clouds.r, saturate(lerp(0.0, 4.0, _sizeCloud)));
                alpha += lerp(0.0, clouds.g, saturate(lerp(-1.0, 3.0, _sizeCloud)));
                alpha += lerp(0.0, clouds.b, saturate(lerp(-2.0, 2.0, _sizeCloud)));
                alpha += lerp(0.0, clouds.a, saturate(lerp(-3.0, 1.0, _sizeCloud)));
                alpha = saturate(alpha);

                half f = SAMPLE_TEXTURE2D(_CloudTexB, sampler_CloudTexB, i.uv * 8.0 + (_Time.x * _cloudSpd.z * 0.00001 + windCoords.xy * 10.0)).a * 0.6;
                alpha = saturate(alpha + lerp(-1.0, 0.7, f)) * (alpha * 3.0);
				alpha *= SAMPLE_TEXTURE2D(_BlendTex, sampler_BlendTex, i.uv).r;

				float2 screenUV = i.screenPos.xy / max(0.001, i.screenPos.w);
				float3 viewDir = normalize(_WorldSpaceCameraPos - i.positionWS);
                float depthVal = 1.0 - saturate(max(i.screenPos.w, 0.001) / (_TenkokuDist * 0.15));

                half3 col = CalculateCloudLighting(alpha, screenUV, viewDir, depthVal);
				return half4(col, alpha * 0.1); // Scaled alpha for layering
			}
			ENDHLSL
		}

        // OVERCAST
		Pass {
			Name "Overcast"
			Blend SrcAlpha OneMinusSrcAlpha

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			Varyings vert(Attributes v) {
				return vert_base(v, -4.0);
			}

			half4 frag(Varyings i) : SV_Target {
				half c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv * 8.0 + (windCoords.xy * _cloudSpd.w)).r;
				half3 albedo = lerp(half3(0.6,0.6,0.6), half3(c,c,c), saturate(_Tenkoku_overcastColor.a));
                
				half alpha = saturate(SAMPLE_TEXTURE2D(_BlendTex, sampler_BlendTex, i.uv).r * (_Tenkoku_overcastColor.a * 3.0));

				float2 screenUV = i.screenPos.xy / max(0.001, i.screenPos.w);
                float4 skySample = SAMPLE_TEXTURE2D(_Tenkoku_SkyBox, sampler_Tenkoku_SkyBox, screenUV);
				float3 viewDir = normalize(_WorldSpaceCameraPos - i.positionWS);
                float depthVal = 1.0 - saturate(max(i.screenPos.w, 0.001) / (_TenkokuDist * 0.15));

                half3 col = albedo * lerp(_Tenkoku_Ambient * 0.1, _Tenkoku_Ambient * 0.35, saturate(_Tenkoku_overcastColor.a));
                
                // Lightning logic
                half3 lCol = Tenkoku_LightningColor.rgb;
                half3 lightningFwd = normalize(Tenkoku_Vec_LightningFwd.xyz);
                col += lCol * (Tenkoku_LightningLightIntensity * 0.2 * (1.0 - c));
                col += lCol * saturate(dot(-viewDir, lightningFwd) * Tenkoku_LightningLightIntensity * 0.7 * c);

                col = saturate(lerp(skySample.rgb, col, depthVal));
				return half4(col, alpha);
			}
			ENDHLSL
		}
	}
}
