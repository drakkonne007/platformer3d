Shader "Hidden/UnderwaterDistortion"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BlitTexture ("Blit Texture", 2D) = "white" {}
        _DistortionStrength ("Distortion Strength", Range(0, 0.1)) = 0.02
        _DistortionSpeed ("Distortion Speed", Range(0, 10)) = 1
        _TintColor ("Tint Color", Color) = (0, 0.4, 0.7, 1)
        _Intensity ("Intensity", Range(0, 1)) = 1
        
        [HideInInspector] _WaterVolumeCount ("Volume Count", Int) = 0
        [HideInInspector] _ShowDebugMask ("Show Debug Mask", Int) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float2 uv           : TEXCOORD0;
            };

            // Supporting both
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_BlitTexture); // Unity 6 / Blitter might use this
            SAMPLER(sampler_BlitTexture);

            float _DistortionStrength;
            float _DistortionSpeed;
            float4 _TintColor;
            float _Intensity;
            
            int _WaterVolumeCount;
            int _ShowDebugMask;
            float4x4 _WaterMatrices[8];
            float4x4 _InvVP;

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;

                // 1. Reconstruct World Position accurately
                float depth = SampleSceneDepth(uv);
                
                // Screen to NDC [ -1, 1 ]
                float4 ndc = float4(uv * 2.0 - 1.0, depth, 1.0);
                
                // Unity 6 Blit UV fix: UV can be flipped on some platforms (DirectX)
                #if UNITY_UV_STARTS_AT_TOP
                if (_ProjectionParams.x < 0)
                    ndc.y *= -1.0;
                #endif
                
                // Reconstruct World Position
                float4 worldPos = mul(_InvVP, ndc);
                float3 positionWS = worldPos.xyz / worldPos.w;

                bool pixelUnderwater = false;
                bool cameraUnderwater = false;
                
                // 2. Loop through all active water volumes
                [unroll(8)]
                for (int i = 0; i < _WaterVolumeCount; i++)
                {
                    // Check Pixel Position (the object we are looking at)
                    float3 pixelLS = mul(_WaterMatrices[i], float4(positionWS, 1.0)).xyz;
                    if (all(abs(pixelLS) <= 0.505)) 
                    {
                        pixelUnderwater = true;
                    }

                    // Check Camera Position
                    float3 cameraLS = mul(_WaterMatrices[i], float4(_WorldSpaceCameraPos, 1.0)).xyz;
                    if (all(abs(cameraLS) <= 0.505))
                    {
                        cameraUnderwater = true;
                    }
                }

                // Mask calculation
                float effectMask = (pixelUnderwater || cameraUnderwater) ? 1.0 : 0.0;
                
                // Debug Visualization
                if (_ShowDebugMask > 0)
                {
                    // Green = Inside Volume, Red = Camera Inside, Blue = Depth Found
                    half3 debugCol = half3(0, 0, depth > 0 ? 0.2 : 0);
                    if (pixelUnderwater) debugCol.g = 1.0;
                    if (cameraUnderwater) debugCol.r = 1.0;
                    
                    // IF BLACK -> Return Magenta to prove shader runs
                    if (length(debugCol) < 0.01) return half4(1, 0, 1, 1);
                    
                    return half4(debugCol, 1.0);
                }

                // 3. Apply Distortion
                if (effectMask > 0.5)
                {
                    float wave = _Time.y * _DistortionSpeed;
                    float sinX = sin(uv.y * 20.0 + wave) * _DistortionStrength;
                    float sinY = cos(uv.x * 20.0 + wave) * _DistortionStrength;
                    uv += float2(sinX, sinY);
                }

                // Sample from _BlitTexture (Unity 6 / Blitter)
                half4 color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, uv);
                
                // Fallback for safety if BlitTexture is empty (alpha 0) - though purely heuristic
                // if (color.a == 0 && color.r == 0 && color.g == 0 && color.b == 0)
                //      color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                
                // 4. Apply Tint (Fog-like)
                half4 tintedColor = lerp(color, color * _TintColor, effectMask * _Intensity);
                
                return tintedColor;
            }
            ENDHLSL
        }
    }
}
