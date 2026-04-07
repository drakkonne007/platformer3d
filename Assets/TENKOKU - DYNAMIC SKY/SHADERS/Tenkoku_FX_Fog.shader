Shader "TENKOKU/TenkokuFog" {
Properties {
	_MainTex ("Base (RGB)", 2D) = "black" {}
	_SkyTex ("Sky Texture", 2D) = "black" {}
}

SubShader {
	Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
	ZTest Always Cull Off ZWrite Off

	HLSLINCLUDE
	#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

	TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
	TEXTURE2D(_SkyTex); SAMPLER(sampler_SkyTex);
	TEXTURE2D(_Tenkoku_SkyBox); SAMPLER(sampler_Tenkoku_SkyBox);
	TEXTURE2D(_Tenkoku_SkyTex); SAMPLER(sampler_Tenkoku_SkyTex);
    TEXTURE2D(_HeatDistortText); SAMPLER(sampler_HeatDistortText);

	float4 _MainTex_TexelSize;
	float4x4 _FrustumCornersWS;
	float4 _CameraWS;
	half4 _Tenkoku_FogColor;
	float _fogSkybox;
	float _fogHorizon;
	float _Tenkoku_FogStart;
	float _Tenkoku_FogEnd;
	float _camDistance;
	float _Tenkoku_Ambient;
	float _Tenkoku_FogDensity;
	float4 _Tenkoku_overcastColor;
	float _tenkokufogFull;
	float4 Tenkoku_Vec_SunFwd;
	float4 Tenkoku_Vec_LightningFwd;
	float4 Tenkoku_LightningColor;
	float Tenkoku_LightningLightIntensity;
	float _Tenkoku_FogObscurance;
	float _Tenkoku_UseElek;
    float _Tenkoku_AtmosphereDensity;
    float _Tenkoku_HeatDistortAmt;
    float _HeatDistortSpeed;
    float _HeatDistortScale;
    float _HeatDistortDist;
    float _tenkoku_rainbowFac1, _tenkoku_rainbowFac2;
    float _tenkokuTimer;
    float4 _DistanceParams;
    float4 _HeightParams;
    float4 _SceneFogParams;
    int4 _SceneFogMode;

	struct Attributes {
		float4 positionOS : POSITION;
		float2 uv : TEXCOORD0;
	};

	struct Varyings {
		float4 positionCS : SV_POSITION;
		float2 uv : TEXCOORD0;
		float4 interpolatedRay : TEXCOORD1;
	};

	Varyings vert (Attributes v) {
		Varyings o;
		o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
		o.uv = v.uv;

		// Original shader used vertex.z to index frustum corners
		int index = (int)v.positionOS.z;
		o.interpolatedRay = _FrustumCornersWS[index];

		return o;
	}

	float ComputeDistance (float3 camDir, float dpth) {
		if (_SceneFogMode.y == 1)
			return length(camDir);
		else
			return dpth;
	}

	float ComputeHalfSpace (float3 wsDir) {
		float FH = _HeightParams.x;
		float3 C = _CameraWS.xyz;
		float3 V = wsDir;
		float3 P = C + V;
		float k = _HeightParams.z;
		float FdotP = P.y - FH;
		float FdotV = V.y;
		float FdotC = _HeightParams.y;
		float c1 = k * (FdotP + FdotC);
		float c2 = (1.0 - 2.0 * k) * FdotP;
		float g = min(c2, 0.0);
		g = -length(V * _HeightParams.w) * (c1 - g * g / abs(FdotV + 1.0e-5f));
		return g;
	}

	half4 frag (Varyings i) : SV_Target {
		float rawDepth = SampleSceneDepth(i.uv);
		float dpth = Linear01Depth(rawDepth, _ZBufferParams);
		float3 wsDir = dpth * i.interpolatedRay.xyz;

		// Calculate scene fog parameters
		float diff = _Tenkoku_FogEnd - _Tenkoku_FogStart;
		float invDiff = abs(diff) > 0.0001 ? 1.0 / diff : 0.0;
		float sceneFogZ = -invDiff;
		float sceneFogW = _Tenkoku_FogEnd * invDiff;

		float dist = _DistanceParams.z + ComputeDistance(wsDir, dpth) + ComputeHalfSpace(wsDir);
		float fogFac = saturate(max(0.0, dist) * sceneFogZ + sceneFogW);

		// Heat Distortion
		float heatDistFac = saturate(max(0.0, dist) * sceneFogZ + sceneFogW);
		heatDistFac += saturate(wsDir.y / 2000.0);
		heatDistFac = saturate(lerp(1.0, 0.0 - _HeatDistortDist, heatDistFac));

		float2 distortUV = i.uv * _HeatDistortScale + float2(0.0, -_Time.y * _HeatDistortSpeed);
		float2 distort = UnpackNormal(SAMPLE_TEXTURE2D(_HeatDistortText, sampler_HeatDistortText, distortUV)).xy;
		float2 uv = i.uv + distort * (_Tenkoku_HeatDistortAmt * heatDistFac);

		// Recalculate with distortion
		rawDepth = SampleSceneDepth(uv);
		dpth = Linear01Depth(rawDepth, _ZBufferParams);
		wsDir = dpth * i.interpolatedRay.xyz;
		dist = _DistanceParams.z + ComputeDistance(wsDir, dpth) + ComputeHalfSpace(wsDir);
		fogFac = saturate(max(0.0, dist) * sceneFogZ + sceneFogW);

		half4 sceneColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);

		if (dpth >= 1.0) {
			fogFac = (_fogSkybox == 1.0) ? 1.0 : 0.0;
		}

		fogFac = saturate(fogFac + saturate(1.0 - _Tenkoku_AtmosphereDensity * 2.0));

		// Horizon Fog
		float diff2 = _tenkokufogFull - 10.0;
		float invDiff2 = abs(diff2) > 0.0001 ? 1.0 / diff2 : 0.0;
		half fogFac3 = saturate(max(0.0, dist) * -invDiff2 + (_tenkokufogFull * invDiff2));
		if (_fogHorizon == 1.0) {
			fogFac *= saturate((wsDir.y / min(_tenkokufogFull, 250.0)) + fogFac3);
		}

		half colFac = (dpth >= 1.0) ? 0.0 : 1.0;
		half4 skyColor = SAMPLE_TEXTURE2D(_Tenkoku_SkyTex, sampler_Tenkoku_SkyTex, uv);
		half4 skyBox = SAMPLE_TEXTURE2D(_Tenkoku_SkyBox, sampler_Tenkoku_SkyBox, uv);

		skyColor = lerp(skyColor, skyColor * _Tenkoku_FogColor, _Tenkoku_FogColor.a * colFac);
		half4 fCol = lerp(skyColor, sceneColor, fogFac);

		if (dpth < 1.0) {
			half tcM1 = lerp(0.98, 0.4, saturate(_Tenkoku_overcastColor.a));
			fCol *= tcM1;
            fCol.rgb = lerp(fCol.rgb, fCol.rgb * 0.65, saturate(_Tenkoku_overcastColor.a * 4.0));
            fCol.rgb = lerp(fCol.rgb, skyBox.rgb, (1.0 - fogFac) * _Tenkoku_FogObscurance);
            fCol = lerp(sceneColor, fCol, _Tenkoku_FogDensity);

            // Lightning
            float lVec = saturate(dot(normalize(Tenkoku_Vec_LightningFwd.xyz), normalize(i.interpolatedRay.xyz))) - 0.1;
            fCol.rgb = lerp(fCol.rgb, Tenkoku_LightningColor.rgb, (1.0 - fogFac) * 2.6 * lVec * saturate(lerp(1.0, 0.2, rawDepth)) * Tenkoku_LightningLightIntensity * 0.2);
		} else {
            if (_Tenkoku_UseElek == 1.0) fCol = sceneColor;
            fCol = lerp(fCol, sceneColor, saturate(lerp(-0.1, 1.0, (dot(float3(0,1,0), normalize(i.interpolatedRay.xyz)) * 0.0005))));
        }

        // Rainbows logic (simplified conversion)
        float rFac = saturate(dot(fCol.rgb, half3(0.4, 0.8, 0.5)));
        float rVec = saturate(dot(normalize(Tenkoku_Vec_SunFwd.xyz), normalize(-i.interpolatedRay.xyz))) - 0.1;
        if (_tenkoku_rainbowFac1 > 0) {
            half3 rainbow = half3(saturate(rVec * 12.0), saturate((rVec - 0.2) * 12.0), saturate((rVec - 1.0) * 12.0));
            fCol.rgb = lerp(fCol.rgb, fCol.rgb + rainbow, _tenkoku_rainbowFac1 * rFac);
        }

		return fCol;
	}
	ENDHLSL

	Pass {
		HLSLPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		ENDHLSL
	}
}
}
