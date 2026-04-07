Shader "Hidden/Tenkoku_VelocityBuffer"
{
	SubShader
	{
		Tags { "RenderPipeline" = "UniversalPipeline" }
		
		HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

		TEXTURE2D(_VelocityTex); SAMPLER(sampler_VelocityTex);
		float4 _VelocityTex_TexelSize;
		float4 _Corner;
		float4x4 _CurrV;
		float4x4 _CurrVP;
		float4x4 _CurrM;
		float4x4 _PrevVP;
		float4x4 _PrevM;

		struct Attributes
		{
			float4 positionOS : POSITION;
			float2 uv : TEXCOORD0;
		};

		struct Varyings
		{
			float4 positionCS : SV_POSITION;
			float2 uv : TEXCOORD0;
			float2 vs_ray : TEXCOORD1;
		};

		Varyings vert_blit(Attributes v)
		{
			Varyings o;
			o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
			o.uv = v.uv;
			o.vs_ray = (2.0 * v.uv - 1.0) * _Corner.xy + _Corner.zw;
			return o;
		}

		half4 frag_prepass(Varyings i) : SV_Target
		{
			float depth = SampleSceneDepth(i.uv);
			float vs_dist = LinearEyeDepth(depth, _ZBufferParams);
			float3 vs_pos = float3(i.vs_ray, 1.0) * vs_dist;
			float4 ws_pos = mul(UNITY_MATRIX_I_V, float4(vs_pos, 1.0));

			float4 rp_cs_pos = mul(_PrevVP, ws_pos);
			float2 rp_ss_ndc = rp_cs_pos.xy / rp_cs_pos.w;
			float2 rp_ss_txc = 0.5 * rp_ss_ndc + 0.5;

			float2 ss_vel = i.uv - rp_ss_txc;
			return half4(ss_vel, 0, 0);
		}

		half4 frag_tilemax(Varyings i) : SV_Target
		{
			#if defined(TILESIZE_10)
				const int support = 10;
			#elif defined(TILESIZE_20)
				const int support = 20;
			#elif defined(TILESIZE_40)
				const int support = 40;
			#else
				const int support = 2;
			#endif

			const float2 step = _VelocityTex_TexelSize.xy;
			const float2 base = i.uv + (0.5 - 0.5 * support) * step;
			
			float2 mv = 0.0;
			float rmv = 0.0;

			for (int x = 0; x < support; x++)
			{
				for (int y = 0; y < support; y++)
				{
					float2 v = SAMPLE_TEXTURE2D(_VelocityTex, sampler_VelocityTex, base + float2(y * step.x, x * step.y)).xy;
					float rv = dot(v, v);
					if (rv > rmv)
					{
						mv = v;
						rmv = rv;
					}
				}
			 support; }
			return half4(mv, 0, 0);
		}

		half4 frag_neighbormax(Varyings i) : SV_Target
		{
			const float2 step = _VelocityTex_TexelSize.xy;
			float2 mv = 0.0;
			float dmv = 0.0;

			for (int x = -1; x <= 1; x++)
			{
				for (int y = -1; y <= 1; y++)
				{
					float2 v = SAMPLE_TEXTURE2D(_VelocityTex, sampler_VelocityTex, i.uv + float2(y * step.x, x * step.y)).xy;
					float dv = dot(v, v);
					if (dv > dmv)
					{
						mv = v;
						dmv = dv;
					}
				}
			}
			return half4(mv, 0, 0);
		}

		struct VaryingsGeom
		{
			float4 positionCS : SV_POSITION;
			float4 ss_pos : TEXCOORD0;
			float3 cs_xy_curr : TEXCOORD1;
			float3 cs_xy_prev : TEXCOORD2;
		};

		VaryingsGeom vert_geom(Attributes v, float4 ws_pos_curr, float4 ws_pos_prev)
		{
			VaryingsGeom o;
			o.positionCS = mul(_CurrVP, mul(_CurrM, ws_pos_curr));
			o.ss_pos = ComputeScreenPos(o.positionCS);
			
			float4 vs_pos_curr = mul(_CurrV, mul(_CurrM, ws_pos_curr));
			o.ss_pos.z = -vs_pos_curr.z;
			
			o.cs_xy_curr = o.positionCS.xyw;
			o.cs_xy_prev = mul(_PrevVP, mul(_PrevM, ws_pos_prev)).xyw;

			#if UNITY_UV_STARTS_AT_TOP
				o.cs_xy_curr.y = -o.cs_xy_curr.y;
				o.cs_xy_prev.y = -o.cs_xy_prev.y;
			#endif
			return o;
		}

		half4 frag_geom(VaryingsGeom i) : SV_Target
		{
			float2 ss_uv = i.ss_pos.xy / i.ss_pos.w;
			float scene_z = SampleSceneDepth(ss_uv);
			float scene_d = LinearEyeDepth(scene_z, _ZBufferParams);
			const float occlusion_bias = 0.03;

			clip(scene_d - i.ss_pos.z + occlusion_bias);

			float2 ndc_curr = i.cs_xy_curr.xy / i.cs_xy_curr.z;
			float2 ndc_prev = i.cs_xy_prev.xy / i.cs_xy_prev.z;

			return half4(0.5 * (ndc_curr - ndc_prev), 0, 0);
		}
		ENDHLSL

		Pass
		{
			Name "Prepass"
			ZTest Always Cull Off ZWrite Off
			HLSLPROGRAM
			#pragma vertex vert_blit
			#pragma fragment frag_prepass
			ENDHLSL
		}

		Pass
		{
			Name "Vertices"
			ZTest LEqual Cull Back ZWrite On
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag_geom
			struct AttributesV { float4 positionOS : POSITION; };
			VaryingsGeom vert(AttributesV v) { return vert_geom(v, v.positionOS, v.positionOS); }
			ENDHLSL
		}

		Pass
		{
			Name "TileMax"
			ZTest Always Cull Off ZWrite Off
			HLSLPROGRAM
			#pragma vertex vert_blit
			#pragma fragment frag_tilemax
			#pragma multi_compile __ TILESIZE_10 TILESIZE_20 TILESIZE_40
			ENDHLSL
		}

		Pass
		{
			Name "NeighborMax"
			ZTest Always Cull Off ZWrite Off
			HLSLPROGRAM
			#pragma vertex vert_blit
			#pragma fragment frag_neighbormax
			ENDHLSL
		}
	}
}
