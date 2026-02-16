Shader "Custom/UnderwaterEffect"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
        _TintColor ("Tint Color", Color) = (0, 0.5, 1, 0.5)
        _Intensity ("Intensity", Range(0, 1)) = 1.0
        _DebugMode ("Debug Mode (0=Off, 1=Depth)", Int) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            Name "UnderwaterTint"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            // Unity 6 Blitter texture
            TEXTURE2D(_BlitTexture);
            SAMPLER(sampler_BlitTexture);

            float4 _TintColor;
            float _Intensity;
            int _DebugMode; // 0=None, 1=Depth, 2=WorldPos

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // --- DEBUG: FORCE ALIVE ---
                if (_DebugMode == 2) return half4(1, 0, 1, 1); // MAGENTA = Shader Alive

                // Robust sampling
                half4 color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, input.uv);
                
                // If BlitTexture is Black (ignore alpha), try MainTex
                if (length(color.rgb) < 0.001)
                {
                    color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                    
                    // DEBUG: If BOTH are black, return RED
                    if (length(color.rgb) < 0.001)
                    {
                        return half4(1, 0, 0, 1); // RED = Input Textures are Black
                    }
                }

                // --- DEBUG: DEPTH ---
                if (_DebugMode == 1)
                {
                     float depth = SampleSceneDepth(input.uv);
                     float linearDepth = Linear01Depth(depth, _ZBufferParams);
                     return half4(linearDepth, linearDepth, linearDepth, 1);
                }

                // Simple Tint
                half4 tinted = color * _TintColor;
                return lerp(color, tinted, _Intensity);
            }
            ENDHLSL
        }
    }
}
