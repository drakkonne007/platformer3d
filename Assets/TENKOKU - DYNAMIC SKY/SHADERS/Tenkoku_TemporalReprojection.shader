Shader "Hidden/Tenkoku_TemporalReprojection"
{
	Properties
	{
		_MainTex ("Base (RGB)", 2D) = "white" {}
	}

	SubShader
	{
		Tags { "RenderPipeline" = "UniversalPipeline" }
		ZTest Always Cull Off ZWrite Off
		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

			TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
			float4 _MainTex_TexelSize;
			TEXTURE2D(_PrevTex); SAMPLER(sampler_PrevTex);
			TEXTURE2D(_VelocityBuffer); SAMPLER(sampler_VelocityBuffer);
			
			float4 _Corner;
			float4 _Jitter;
			float4x4 _PrevVP;
			float _FeedbackMin;
			float _FeedbackMax;

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

			Varyings vert(Attributes v)
			{
				Varyings o;
				o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
				o.uv = v.uv;
				o.vs_ray = (2.0 * v.uv - 1.0) * _Corner.xy;
				return o;
			}

			float PDsrand(float2 n)
			{
				return frac(sin(dot(n.xy, float2(12.9898, 78.233))) * 43758.5453) * 2.0 - 1.0;
			}

			float4 PDsrand4(float2 n)
			{
				return frac(sin(dot(n.xy, float2(12.9898, 78.233))) * float4(43758.5453, 28001.8384, 50849.4141, 12996.89)) * 2.0 - 1.0;
			}

			struct f2rt
			{
				float4 buffer : SV_Target0;
				float4 screen : SV_Target1;
			};

			f2rt frag(Varyings i)
			{
				f2rt o;
				float2 uv = i.uv;
				float depth = SampleSceneDepth(uv);
				float dp01 = Linear01Depth(depth, _ZBufferParams);

				if (dp01 < 1.0)
				{
					float4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
					o.screen = col;
					o.buffer = col;
				}
				else
				{
					float2 ss_vel = SAMPLE_TEXTURE2D(_VelocityBuffer, sampler_VelocityBuffer, uv).xy;
					float4 texel0 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
					float4 texel1 = SAMPLE_TEXTURE2D(_PrevTex, sampler_PrevTex, uv - ss_vel);

					float2 du = float2(_MainTex_TexelSize.x, 0);
					float2 dv = float2(0, _MainTex_TexelSize.y);

					float4 ctl = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - dv - du);
					float4 ctc = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - dv);
					float4 ctr = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - dv + du);
					float4 cml = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - du);
					float4 cmc = texel0;
					float4 cmr = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + du);
					float4 cbl = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + dv - du);
					float4 cbc = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + dv);
					float4 cbr = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + dv + du);

					float4 cmin = min(ctl, min(ctc, min(ctr, min(cml, min(cmc, min(cmr, min(cbl, min(cbc, cbr))))))));
					float4 cmax = max(ctl, max(ctc, max(ctr, max(cml, max(cmc, max(cmr, max(cbl, max(cbc, cbr))))))));

					texel1 = clamp(texel1, cmin, cmax);

					float lum0 = Luminance(texel0.rgb);
					float lum1 = Luminance(texel1.rgb);
					float unbiased_diff = abs(lum0 - lum1) / max(lum0, max(lum1, 0.2));
					float k_feedback = lerp(_FeedbackMin, _FeedbackMax, (1.0 - unbiased_diff) * (1.0 - unbiased_diff));

					float4 color_temporal = lerp(texel0, texel1, k_feedback);
					float4 noise = PDsrand4(uv + _SinTime.x + 0.6959) / 510.0;
					
					o.buffer = saturate(color_temporal + noise);
					o.screen = o.buffer;
				}
				return o;
			}
			ENDHLSL
		}
	}
	Fallback off
}
