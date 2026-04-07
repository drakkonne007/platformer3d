Shader "TENKOKU/Tenkoku_sky_elek_Scatter"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_ZTest ("ZTest", Float) = 0
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" }
		LOD 100

		HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		#include "AtmosphericScattering.cginc"

		TEXTURE2D(_LightShaft1); SAMPLER(sampler_LightShaft1);
		
		float _DistanceScale;

		struct Attributes
		{
			float4 positionOS : POSITION;
            float2 uv : TEXCOORD0;
		};

		struct Varyings
		{
			float4 positionCS : SV_POSITION;
			float2 uv : TEXCOORD0;
		};
		               
		ENDHLSL
            
		Pass
		{
            Name "ParticleDensityLUT"
			ZTest Off
			Cull Off
			ZWrite Off
			Blend Off

			HLSLPROGRAM
            #pragma vertex vertQuad
            #pragma fragment particleDensityLUT
            #pragma target 3.0

            Varyings vertQuad(Attributes v)
            {
                Varyings o = (Varyings)0;
                o.positionCS = float4(v.positionOS.xyz, 1.0);
                o.uv = v.uv;
                return o;
            }

			float4 particleDensityLUT(Varyings i) : SV_Target
			{
                float cosAngle = i.uv.x * 2.0 - 1.0;
                float sinAngle = sqrt(saturate(1 - cosAngle * cosAngle));
                float startHeight = lerp(0.0, _AtmosphereHeight, i.uv.y);

                float3 rayStart = float3(0, startHeight, 0);
                float3 rayDir = float3(sinAngle, cosAngle, 0);
                
				return float4(PrecomputeParticleDensity(rayStart, rayDir), 0, 1);
			}

			ENDHLSL
		}
	}
}
