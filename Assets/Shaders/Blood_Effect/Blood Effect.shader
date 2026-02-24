Shader "Particles/Blood Effect URP"
{
	Properties
	{
		[Header (Color Controls)]
		[HDR] _BaseColor ("Base Color Mult", Color) = (1,1,1,1)
		_LightStr ("Lighting Strength", float) = 0.85
		_AlphaMin ("Alpha Clip Min", Range (-0.01, 1.01)) = 0.1
		_AlphaSoft ("Alpha Clip Softness", Range (0,1)) = 0.022
		_EdgeDarken ("Edge Darkening", float) = 1.0
		_ProcMask ("Procedural Mask Strength", float) = 1.0

		[Header (Mask Controls)]
		_MainTex ("Mask Texture", 2D) = "white" {}
		_MaskStr ("Mask Strength", float) = 0.7
		_Columns ("Flipbook Columns", Int) = 1
		_Rows ("Flipbook Rows", Int) = 1
		_ChannelMask ("Channel Mask", Vector) = (1,0,0,0)
		[Toggle] _FlipU("Flip U Randomly", float) = 0
		[Toggle] _FlipV("Flip V Randomly", float) = 0

		[Header (Noise Controls)]
		_NoiseTex ("Noise Texture", 2D) = "white" {}
		_NoiseAlphaStr ("Noise Strength", float) = 0.8
		_ChannelMask2 ("Channel Mask",Vector) = (1,0,0,0)
		_Randomize ("Randomize Noise", float) = 1.0

		// Hidden properties that were used but not exposed in the original shader
		[HideInInspector] _WarpTex ("Warp Texture", 2D) = "grey" {}
		[HideInInspector] _WarpStr ("Warp Strength", float) = 0.0
		[HideInInspector] _NoiseColorStr("Noise Color Strength", float) = 0.0

		[Header (Vertex Physics)]
		_FallOffset ("Gravity Offset", range(-1,0)) = -1.0
		_FallRandomness ("Gravity Randomness", float) = 0.25
	}
	
	SubShader
	{
		Tags 
		{
			"IgnoreProjector"="True"
			"Queue"="Transparent"
			"RenderType"="Transparent"
			"RenderPipeline"="UniversalPipeline"
		}

		Blend SrcAlpha OneMinusSrcAlpha
		ZWrite Off

		Pass 
		{
			Name "UniversalForward"
			Tags { "LightMode"="UniversalForward" }

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog
			#pragma target 3.0

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

			CBUFFER_START(UnityPerMaterial)
				half4 _BaseColor;
				half _LightStr;
				half _AlphaMin;
				half _AlphaSoft;
				half _EdgeDarken;
				half _ProcMask;

				float4 _MainTex_ST;
				half _MaskStr;
				half _Columns;
				half _Rows;
				half4 _ChannelMask;
				half _FlipU;
				half _FlipV;

				float4 _NoiseTex_ST;
				half _NoiseAlphaStr;
				half _NoiseColorStr;
				half4 _ChannelMask2;
				half _Randomize;

				float4 _WarpTex_ST;
				half _WarpStr;

				half _FallOffset;
				half _FallRandomness;
			CBUFFER_END

			sampler2D _MainTex;
			sampler2D _NoiseTex;
			sampler2D _WarpTex;

			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float4 texcoord0 : TEXCOORD0; // Z is Random, W is Lifetime
				float3 texcoord1 : TEXCOORD1; // X is Pan Offset, Y is UV Warp Strength, Z is Gravity
				float4 color : COLOR;
			};

			struct v2f
			{
				float4 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float4 color : COLOR;
				float4 vertLight : TEXCOORD3;
				float3 customData : TEXCOORD4; 
				float fogCoord : TEXCOORD5;
				half3 normal : NORMAL;
			};

			v2f vert (appdata v)
			{
				v2f o = (v2f)0;

				float lifetime = v.texcoord0.w;
				lifetime = lifetime * lifetime + (_FallOffset + ((v.texcoord0.z - 0.5) * _FallRandomness)) * lifetime;
				float4 fallPos = lifetime * float4(0, v.texcoord1.z, 0, 0);

				float2 UVflip = round(frac(float2(v.texcoord0.z * 13, v.texcoord0.z * 8))); 
				UVflip = UVflip * 2 - 1; 
				UVflip = lerp(1, UVflip, float2(_FlipU, _FlipV));
				
				float3 worldPos = TransformObjectToWorld(v.vertex.xyz) + fallPos.xyz;
				o.vertex = TransformWorldToHClip(worldPos);
				
				o.color = v.color;
				o.color.a *= o.color.a;
				o.color.a += _AlphaMin;
				
				o.normal = TransformObjectToWorldNormal(v.normal);
				o.customData = float3(v.texcoord1.xy, v.texcoord0.z);

				o.fogCoord = ComputeFogFactor(o.vertex.z);

				o.uv.xy = v.texcoord0.xy * _MainTex_ST.xy * UVflip + _MainTex_ST.zw;
				o.uv.zw = o.uv.xy * half2(_Columns, _Rows) + v.texcoord0.z * half2(3,8) * _Randomize;
				
				// Vertex Lighting Approx
				float3 shade = SampleSH(o.normal);
				shade = max(shade, float3(0.15, 0.15, 0.15));
				o.vertLight.xyz = lerp(float3(1,1,1), shade, _LightStr);

				return o;
			}

			half4 frag (v2f i) : SV_Target
			{	
				// UV Warp
				float4 uvWarp = tex2D(_WarpTex, i.uv.zw * _WarpTex_ST.xy + _WarpTex_ST.zw * (i.customData.x + 1) + (float2(5,8) * i.customData.z) );
				float2 warp = (uvWarp.xy * 2) - 1;
				warp *= _WarpStr * i.customData.y;

				// Mask
				half4 mask = tex2D(_MainTex, i.uv.xy + warp);
				mask = saturate(lerp(1, mask, _MaskStr));

				// Edge Mask (prevents spill)
				half2 tempUV = frac(i.uv.xy * half2(_Columns, _Rows)) - 0.5;
				tempUV *= tempUV * 4;
				half edgeMask = saturate(tempUV.x + tempUV.y);
				edgeMask *= edgeMask;
				edgeMask = 1 - edgeMask;
				edgeMask = lerp(1.0, edgeMask, _ProcMask);

				mask *= edgeMask;
				half4 col = max(0.001, i.color);
				col.a = saturate(dot(mask, _ChannelMask));

				// Noise
				half4 noise4 = tex2D(_NoiseTex, i.uv.zw * _NoiseTex_ST.xy + _NoiseTex_ST.zw * i.customData.x + warp);
				half noise = dot(noise4, _ChannelMask2);
				noise = saturate(lerp(1, noise, _NoiseAlphaStr));

				// Alpha Clip
				col.a *= noise;
				half preClipAlpha = col.a;
				half clippedAlpha = saturate((preClipAlpha * i.color.a - _AlphaMin) / max(0.001, _AlphaSoft));
				col.a = clippedAlpha;

				// Lighting
				float3 baseLighting = i.vertLight.xyz;

				// Edge Find
				half edge = 1 - saturate(preClipAlpha * clippedAlpha);
				edge *= edge;
				edge = 1 - edge;
				edge = edge + lerp(0, noise - 0.5, _NoiseColorStr);
				
				// Edge Darken
				edge = saturate(lerp(0.71, edge * edge, _EdgeDarken));

				// Edge Alpha
				col.a *= saturate(lerp(1.25, _BaseColor.a , edge));
				col.xyz *= lerp(min(col.xyz * col.xyz * col.xyz * 0.3, 1.0), 0.71, edge);  

				col.xyz *= max(0, baseLighting * _BaseColor.xyz);

				col.rgb = MixFog(col.rgb, i.fogCoord);
				return col;
			}
			ENDHLSL
		}
	}
	FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
