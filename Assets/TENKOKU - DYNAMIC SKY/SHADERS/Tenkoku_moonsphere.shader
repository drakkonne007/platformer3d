Shader "TENKOKU/moonsphere_shader" {
    Properties 
    {
        _PrimaryTint("Primary Tint", Color) = (1,1,1,1)
        _Color ("Main Color", Color) = (1,1,1,1)
        _AmbientTint ("Ambient Tint", Color) = (1,1,1,1)
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _BRDFTex ("BRDF", 2D) = "white" {}
        _overBright ("OverBright", float) = 1.0
        _dispStrength ("Displace Amount", Range(0.0,3.0)) = 1.0
        _GlowColor ("Glow Color", Color) = (0.5,0.5,0.5,0.5)
    }
    
    SubShader 
    {
        Tags {"Queue"="Background+1605" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}
        Cull Back
        ZWrite Off
        Blend One Zero // The original used surface shader default which is usually Opaque for Background queue, but here it's meant to be a solid sphere.
        
        Offset 1,993000

        Pass {
            Name "MoonForward"
            Tags { "LightMode" = "UniversalForward" }

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			CBUFFER_START(UnityPerMaterial)
				float4 _PrimaryTint;
				float4 _AmbientTint;
				float4 _Color;
                float _overBright;
			CBUFFER_END

			TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
			TEXTURE2D(_Tenkoku_SkyBox); SAMPLER(sampler_Tenkoku_SkyBox);

			// Globals
			float4 Tenkoku_MoonLightColor;
			float4 Tenkoku_MoonHorizColor;
			float4 Tenkoku_Vec_SunFwd;
			float4 _Tenkoku_overcastColor;
			float _Tenkoku_Ambient;
			float Tenkoku_MoonHFac;
			float _tenkokuIsLinear;

			struct Attributes {
				float4 positionOS : POSITION;
				float2 uv : TEXCOORD0;
				float3 normalOS : NORMAL;
			};

			struct Varyings {
				float4 positionCS : SV_POSITION;
				float2 uv : TEXCOORD0;
				float4 screenPos : TEXCOORD1;
                float3 normalWS : TEXCOORD3;
			};

			Varyings vert (Attributes v) {
				Varyings o;
				o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
				o.uv = v.uv;
				o.screenPos = ComputeScreenPos(o.positionCS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
				return o;
			}

			half4 frag (Varyings i) : SV_Target {
				float2 screenUV = i.screenPos.xy / max(0.001, i.screenPos.w);
				float4 skySample = SAMPLE_TEXTURE2D(_Tenkoku_SkyBox, sampler_Tenkoku_SkyBox, screenUV);

				float4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
				tex.rgb *= clamp(lerp(-0.25, 3.0, tex.r), 0.0, 3.0);

				float3 albedo = tex.rgb;
				float3 mTCol = lerp(float3(1,1,1), Tenkoku_MoonHorizColor.rgb * 2.0, Tenkoku_MoonHorizColor.a);
				albedo = albedo * _PrimaryTint.rgb * lerp(mTCol, float3(1,1,1), max(Tenkoku_MoonHFac, _Tenkoku_Ambient));

				// Extend Brightness Range
				albedo.r = lerp(0.0, 2.5, albedo.r);
				albedo.g = lerp(0.0, 2.1, albedo.g);
				albedo.b = lerp(0.0, 2.0, albedo.b);

				if (_tenkokuIsLinear == 0.0) {
					albedo = saturate(albedo * 0.4646);
				}

                // Phase lighting from Sun
                float3 normalWS = normalize(i.normalWS);
                float dotPhase = saturate(dot(normalWS, normalize(Tenkoku_Vec_SunFwd.xyz)) * 2.0);
                
                float3 lightCol = lerp(skySample.rgb, albedo * max(0.5, Tenkoku_MoonLightColor.rgb) * lerp(2.0, 5.0, _AmbientTint.rgb), dotPhase);
                lightCol = lerp(lightCol, skySample.rgb, min(0.8, _AmbientTint.r));
                lightCol = max(skySample.rgb, lightCol);

                float alpha = saturate(lerp(1.0, -3.0, _Tenkoku_overcastColor.a));

				return half4(lightCol, alpha);
			}
			ENDHLSL
        }
    }
}