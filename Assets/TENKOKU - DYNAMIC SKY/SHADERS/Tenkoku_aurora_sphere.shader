Shader "TENKOKU/aurora_sphere"
{
    Properties
    {
        _AuroraTex("Aurora Texture", 2D) = "white" {}
        _AuroraTexNormal("Aurora Normal Texture", 2D) = "bump" {}

        [Space]
        _Altitude4("Altitude 4 (bottom)", Float) = 5500
        _Altitude5("Altitude 5 (top)", Float) = 6000
        _FarDist("Far Distance", Float) = 30000

        [Space]
        aurSpd ("Aurora Speed", Range(0,1)) = 0.15
        aurScale ("Aurora Scale", Range(0,1)) = 0.394
        aurDefScale ("Aurora Deform Scale", Range(0,4)) = 1.53
        aurNormScale ("Aurora Normal Scale", Range(0,1)) = 0.884
        aurRepeat ("Aurora Repeat", Int) = 32
        aurSep ("Aurora Separation", Float) = 0.02
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent"
            "Queue" = "Background+1600"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Front
            ZWrite Off
            Blend One One

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 rayDir : TEXCOORD1;
            };

            TEXTURE2D(_AuroraTex);
            SAMPLER(sampler_AuroraTex);
            TEXTURE2D(_AuroraTexNormal);
            SAMPLER(sampler_AuroraTexNormal);

            CBUFFER_START(UnityPerMaterial)
                float _Altitude4;
                float _Altitude5;
                float _FarDist;
                float aurSpd;
                float aurScale;
                float aurDefScale;
                float aurNormScale;
                int aurRepeat;
                float aurSep;
            CBUFFER_END

            // External globals (usually updated by Tenkoku scripts)
            float _cS;
            float _tenkokuTimer;
            float _tenkokuNoiseTimer;
            float _Tenkoku_AuroraSpd;
            float _Tenkoku_AuroraAmt;

            float UVRandom(float2 uv)
            {
                float f = dot(float2(_tenkokuNoiseTimer, _tenkokuNoiseTimer), uv);
                return frac(43758.5453 * sin(f));
            }

            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;

                VertexPositionInputs positionInputs = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = positionInputs.positionCS;
                
                // Screen-space UV matching original logic: (p.xy / p.w + 1) * 0.5
                float4 p = o.positionCS;
                o.uv = (p.xy / p.w + 1) * 0.5;

                // rayDir calculation
                float3 ray = mul((float3x3)GetObjectToWorldMatrix(), v.positionOS.xyz);
                ray = normalize(ray);
                o.rayDir = -ray;

                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                // Set Base Settings
                float3 ray = -i.rayDir;
                float2 uv = i.uv + _tenkokuTimer;
                float3 wscPos = _WorldSpaceCameraPos;
                wscPos.y = 0;
                
                float dist0 = _Altitude4 / max(0.01, ray.y);
                float dist1 = _Altitude5 / max(0.01, ray.y);
                float offs = UVRandom(uv) * (dist1 - dist0) / 50.0;
                float3 auroraCol = float3(0, 0, 0);
                
                float currentAurSpd = aurSpd * _Tenkoku_AuroraSpd;

                // Early Return
                if (ray.y < 0.01) return half4(0, 0, 0, 0);

                // Build Aurora buffer
                for (int ax = 0; ax < aurRepeat; ax++)
                {
                    float ht = 2.0 - (aurSep * ax);
                    float3 samplePos = (wscPos + ray * (dist0 + offs)) * 1e-5;
                    
                    float2 uvwAN = float2(samplePos.x, samplePos.z) * _cS * ht;
                    uvwAN.xy -= float2(0.0, _Time.x * currentAurSpd);
                    uvwAN.xy *= aurNormScale;
                    float3 aN = SAMPLE_TEXTURE2D_LOD(_AuroraTexNormal, sampler_AuroraTexNormal, uvwAN.xy, 0).rgb;

                    float2 uvwA = float2(samplePos.x, samplePos.z) * _cS * ht;
                    uvwA.x += (aurDefScale * aN.x);
                    uvwA.xy *= aurScale;
                    uvwA.xy += float2(_Time.x * currentAurSpd * 0.1, _Time.x * currentAurSpd);
                    float4 a1 = SAMPLE_TEXTURE2D_LOD(_AuroraTex, sampler_AuroraTex, uvwA, 0);

                    a1.rgb = lerp(a1.rgb, half3(0, 0, 0), saturate((1900.0 / max(0.01, ray.y)) / _FarDist));

                    float aurFac = ((1.0 / max(1.0, (float)aurRepeat)) * (float)ax);
                    auroraCol.rgb += (a1.rgb * aurFac);
                }

                float3 retCol = auroraCol * 2.0 * _Tenkoku_AuroraAmt;
                return half4(retCol, 1.0);
            }
            ENDHLSL
        }
    }
}
