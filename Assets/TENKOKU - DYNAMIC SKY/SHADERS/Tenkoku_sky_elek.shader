Shader "TENKOKU/Tenkoku_Sky_Elek"
{
	Properties {
		_NightColor ("Night Color", Color) = (1,1,1,1)
	}

	SubShader
	{
		Tags{ "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" "RenderPipeline" = "UniversalPipeline" }
		Cull Off ZWrite Off

		Pass
		{
            Name "SkyForward"
            Tags { "LightMode" = "UniversalForward" }

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "AtmosphericScattering.cginc"

			float3 _TenkokuCameraPos;
			float4 Tenkoku_Vec_SunFwd;
			float4 Tenkoku_Vec_MoonFwd;
			float _Tenkoku_Ambient;
			float _Tenkoku_AmbientGI;
			float4 tenkoku_globalTintColor;
			float4 tenkoku_globalSkyColor;
			float _Tenkoku_SkyBright;

			float4 _NightColor;
			float _Tenkoku_NightBright;
			float4 _Tenkoku_overcastColor;
			float _Tenkoku_overcastAmt;
			float _Tenkoku_MnMieAmt;
			float _Tenkoku_MnIntensity;
			float _tenkokuIsLinear;

			struct Attributes
			{
				float4 positionOS : POSITION;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float3 positionOS : TEXCOORD0;	
			};
			
			Varyings vert (Attributes v)
			{
				Varyings o;
				o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
				o.positionOS = v.positionOS.xyz;
				return o;
			}

			half4 frag (Varyings i) : SV_Target
			{
				float3 lightVec = Tenkoku_Vec_SunFwd.xyz;
				float3 rayStart = _TenkokuCameraPos;
				float3 rayDir = normalize(mul((float3x3)GetObjectToWorldMatrix(), i.positionOS));
				float3 lightDir = -lightVec.xyz;
				float3 planetCenter = float3(0, -_PlanetRadius, 0);

				float rayLength = 100000.0;

				float4 extinction;
				float4 inscattering = IntegrateInscattering(rayStart, rayDir, rayLength, planetCenter, 1.0, lightDir, 16.0, extinction);
				
				// NaN check
				if (any(isnan(inscattering.rgb))) inscattering.rgb = 0;
			
				// Tenkoku Ambient Scatter
				float3 ambColor = float3(0.06, 0.069, 0.067) * 0.7 * _Tenkoku_Ambient;
				inscattering.rgb = max(ambColor, inscattering.rgb);

				// Tenkoku Eclipse Darkening
				float eclFac = saturate(lerp(0.1, 1.0, _Tenkoku_EclipseFactor));
				float3 eclipseScattering = inscattering.rgb;
				float moonDiscFac = saturate(dot(rayDir, -lightDir) - 0.25);
				float horizFac = saturate(saturate(dot(rayDir, half3(0, 1, 0)) + 0.15) * (1.0 - moonDiscFac));
				eclipseScattering.rgb = lerp(half3(1.9, 1.0 + horizFac, 0), inscattering.rgb, horizFac + moonDiscFac + 0.25);
				eclipseScattering.rgb *= lerp(0.04, 0.001, moonDiscFac);
				inscattering.rgb = lerp(eclipseScattering.rgb, inscattering.rgb, eclFac);

				// Tenkoku Final Tinting
				inscattering.rgb = inscattering.rgb * (_Tenkoku_SkyBright * 0.1);
				inscattering.rgb = lerp(inscattering.rgb, inscattering.rgb * tenkoku_globalSkyColor.rgb, tenkoku_globalSkyColor.a);
				inscattering.rgb = lerp(inscattering.rgb, inscattering.rgb * tenkoku_globalTintColor.rgb, tenkoku_globalTintColor.a);

				// Moon Mie
				float mmS = lerp(0.0, 0.0075, _Tenkoku_MnMieAmt);
				float dotMoon = dot(rayDir, normalize(Tenkoku_Vec_MoonFwd.xyz)) + mmS;
				float3 moonMie = (dotMoon - 0.9995) * 1.0;
				moonMie += (dotMoon - 0.999) * 1.0;
				moonMie += (dotMoon - 0.997) * 1.0;
				moonMie += (dotMoon - 0.990) * 0.75;
				moonMie += (dotMoon - 0.97) * 0.5;
				inscattering.rgb += saturate(moonMie * half3(0.2, 0.28, 0.4) * saturate(lerp(1.5, 1.0, _Tenkoku_MnMieAmt))) * max(_Tenkoku_MnIntensity, 0.01);

				// Night Brightening
				half3 nBright = half3(1.0, 1.0, 1.0);
				#ifdef UNITY_COLORSPACE_GAMMA
					nBright = half3(0.027, 0.02, 0.025);
				#endif
				inscattering.rgb = max(inscattering.rgb, _NightColor.rgb * _Tenkoku_NightBright * nBright);

				// Night Horizon Brightening
				horizFac = saturate(lerp(0.0, 2.0, dot(half3(0, -1, 0), normalize(rayDir.xyz) - lerp(0.2, 0.45, _Tenkoku_NightBright))));
				inscattering.rgb = max(inscattering.rgb, inscattering.rgb + half3(0.07, 0.06, 0.06) * horizFac * saturate(lerp(0.0, 0.25, _Tenkoku_NightBright)));

				// Overcast Color
				inscattering.rgb = lerp(inscattering.rgb, max(max(inscattering.r, inscattering.g), inscattering.b) * 0.1, saturate(_Tenkoku_overcastAmt * 3.0));

				// Gamma Shift (URP is usually Linear, but we respect the flag)
				if (_tenkokuIsLinear == 0.0) {
					inscattering.rgb = inscattering.rgb * 2.2;
				}

				return half4(inscattering.xyz, 1.0);
			}
			ENDHLSL
		}
	}
}
