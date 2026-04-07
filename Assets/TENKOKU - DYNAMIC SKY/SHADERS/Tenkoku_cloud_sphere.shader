Shader "TENKOKU/cloud_sphere"
{
    Properties
    {
        _overBright("OverBright", Range(0.0,4.0)) = 0.0
        _SampleCount0("Sample Count (min)", Float) = 30
        _SampleCount1("Sample Count (max)", Float) = 90
        _SampleCountL("Sample Count (light)", Int) = 16

        _NoiseTex1("Noise Volume", 3D) = ""{}
        _NoiseTex2("Noise Volume", 3D) = ""{}
        _CloudTex1("Cloud Texture", 2D) = ""{}

        _NoiseFreq1("Frequency 1", Float) = 3.1
        _NoiseFreq2("Frequency 2", Float) = 35.1
        _NoiseAmp1("Amplitude 1", Float) = 5
        _NoiseAmp2("Amplitude 2", Float) = 1
        _NoiseBias("Bias", Float) = -0.2
        _NoiseBias2("Bias 2", Float) = -0.2
        _NoiseBias3("Bias 3", Float) = -0.2

        _Scroll1("Scroll Speed 1", Vector) = (0.01, 0.08, 0.06, 0)
        _Scroll2("Scroll Speed 2", Vector) = (0.01, 0.05, 0.03, 0)

        _Altitude0("Altitude (bottom)", Float) = 1500
        _Altitude1("Altitude (top)", Float) = 3500
        _Altitude2("Altitude 2 (bottom)", Float) = 5500
        _Altitude3("Altitude 2 (top)", Float) = 6000
        _Altitude4("Altitude 4 (bottom)", Float) = 5500
        _Altitude5("Altitude 5 (top)", Float) = 6000
        _FarDist("Far Distance", Float) = 30000

        _Scatter("Scattering Coeff", Float) = 0.008
        _HGCoeff("Henyey-Greenstein", Float) = 0.5
        _Extinct("Extinction Coeff", Float) = 0.01

        _Edge("Edge", Range(0.0,1.0)) = 0.0
        _Darkness("Darkness", Range(0.0,1.0)) = 1.0

        _SunSize ("Sun Size", Range(0,1)) = 0.04
        _AtmosphereThickness ("Atmoshpere Thickness", Range(0,5)) = 1.0
        _SkyTint ("Sky Tint", Color) = (.5, .5, .5, 1)
        _GroundColor ("Ground", Color) = (.369, .349, .341, 1)
        _Exposure("Exposure", Range(0, 8)) = 1.3
    }

    SubShader
    {
        Tags {"Queue"="Background+1605" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}
        Cull Front
        ZWrite Off
        Offset 1,80000
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes {
                float4 positionOS : POSITION;
            };

            struct Varyings {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 rayDir : TEXCOORD1;
                float4 screenPos : TEXCOORD2;
            };

            TEXTURE3D(_NoiseTex1); SAMPLER(sampler_NoiseTex1);
            TEXTURE3D(_NoiseTex2); SAMPLER(sampler_NoiseTex2);
            TEXTURE2D(_CloudTex1); SAMPLER(sampler_CloudTex1);
            TEXTURE2D(_Tenkoku_SkyBox); SAMPLER(sampler_Tenkoku_SkyBox);
            TEXTURE2D(_Tenkoku_SkyTex); SAMPLER(sampler_Tenkoku_SkyTex);

            CBUFFER_START(UnityPerMaterial)
                float _SampleCount0;
                float _SampleCount1;
                int _SampleCountL;
                float _NoiseFreq1;
                float _NoiseFreq2;
                float _NoiseAmp1;
                float _NoiseAmp2;
                float _NoiseBias;
                float _NoiseBias2;
                float _NoiseBias3;
                float3 _Scroll1;
                float3 _Scroll2;
                float _Altitude0;
                float _Altitude1;
                float _Altitude2;
                float _Altitude3;
                float _Altitude4;
                float _Altitude5;
                float _FarDist;
                float _Scatter;
                float _HGCoeff;
                float _Extinct;
                float _Edge;
                float _Darkness;
                float _overBright;
            CBUFFER_END

            // Globals
            float4 Tenkoku_Vec_SunFwd;
            float4 Tenkoku_Vec_MoonFwd;
            float4 Tenkoku_Vec_LightningFwd;
            float Tenkoku_LightningLightIntensity;
            float4 Tenkoku_LightningColor;
            float4 _TenkokuSunColor;
            float4 Tenkoku_MoonLightColor;
            float4 _TenkokuCloudColor;
            float4 _TenkokuCloudAmbientColor;
            float _Tenkoku_Ambient;
            float _Tenkoku_AmbientGI;
            float4 _Tenkoku_overcastColor;
            float _Tenkoku_overcastAmt;
            float _cS;
            float _tenkokuTimer;
            float _tenkokuNoiseTimer;
            float _Tenkoku_UseElek;
            float _humid;

            float UVRandom(float2 uv){
                float f = dot(float2(_tenkokuNoiseTimer, _tenkokuNoiseTimer), uv);
                return frac(43758.5453 * sin(f));
            }

            Varyings vert(Attributes v){
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                float4 p = o.positionCS;
                o.uv = (p.xy / p.w + 1.0) * 0.5;
                float3 ray = mul((float3x3)GetObjectToWorldMatrix(), v.positionOS.xyz);
                o.rayDir = -normalize(ray);
                o.screenPos = ComputeScreenPos(p);
                return o;
            }

            float SampleNoise(float3 uvw){
                const float baseFreq = 1e-5;
                float4 uvw1 = float4(uvw * _NoiseFreq1 * baseFreq, 0) * _cS;
                float4 uvw2 = float4(uvw * _NoiseFreq2 * baseFreq, 0) * _cS;
                uvw1.xyz += _Scroll1.xyz * _tenkokuTimer;
                uvw2.xyz += _Scroll2.xyz * _tenkokuTimer;
                float n1 = SAMPLE_TEXTURE3D_LOD(_NoiseTex1, sampler_NoiseTex1, uvw1.xyz, 0).a;
                float n2 = SAMPLE_TEXTURE3D_LOD(_NoiseTex2, sampler_NoiseTex2, uvw2.xyz, 0).a;
                float n = n1 * _NoiseAmp1 + n2 * _NoiseAmp2;
                n = saturate(n + lerp(-0.4, 2.15, _NoiseBias));
                float y = uvw.y - _Altitude0;
                float h = _Altitude1 - _Altitude0;
                n *= smoothstep(0, h * 0.1, y);
                n *= smoothstep(0, h * 0.4, h - y);
                return n;
            }

            float SampleNoise2(float3 uvw){
                const float baseFreq = 1e-5;
                float3 scroll = float3(1, 0, 0.25) * _tenkokuTimer * 1.4;
                float4 uvw0 = float4(uvw * lerp(0.2, 1.5, _NoiseBias2) * baseFreq, 0) * _cS;
                uvw0.xyz += _Scroll1.xyz * scroll;
                float n0 = SAMPLE_TEXTURE3D_LOD(_NoiseTex1, sampler_NoiseTex1, uvw0.xyz * float3(1, 4, 1), 0).a;
                float n1 = SAMPLE_TEXTURE3D_LOD(_NoiseTex2, sampler_NoiseTex2, (uvw * lerp(0.1, 5, _NoiseBias2) * baseFreq * _cS) + _Scroll1.xyz * scroll, 0).a;
                float n = n1 * _NoiseAmp1 - n0 * 0.8;
                n = saturate(n + lerp(0.3, 2.8, _NoiseBias2));
                float y = uvw.y - _Altitude2;
                float h = _Altitude3 - _Altitude2;
                n *= smoothstep(0, h * 0.1, y);
                n *= smoothstep(0, h * 0.4, h - y);
                return n;
            }

            float SampleNoise3(float3 uvw){
                const float baseFreq = 1e-5;
                float3 scroll = float3(0.9, 0, 0.15) * _tenkokuTimer * 0.25;
                float2 uv0 = (uvw.xz * 3.0 * baseFreq * _cS) + _Scroll1.xz * scroll.xz;
                float n0 = SAMPLE_TEXTURE2D_LOD(_CloudTex1, sampler_CloudTex1, uv0, 0).b;
                float n1 = SAMPLE_TEXTURE2D_LOD(_CloudTex1, sampler_CloudTex1, (uvw.xz * 2.0 * baseFreq * _cS) + _Scroll1.xz * scroll.xz, 0).g;
                float n = n1 - n0 * 1.2;
                return saturate(n + lerp(-0.1, 1.25, _NoiseBias3));
            }

            float BeerPowder(float depth){
                return exp(-_Extinct * depth) * (1.0 - exp(-_Extinct * 0.75 * depth));
            }

            float Trace(float3 pos, float rand){
                float3 light = Tenkoku_Vec_SunFwd.xyz;
                float stride = (_Altitude1 - pos.y) / max(0.01, light.y * _SampleCountL);
                pos += light * stride * rand;
                float depth = 0;
                for (int s = 0; s < _SampleCountL; s++) {
                    depth += SampleNoise(pos) * stride;
                    pos += light * stride;
                }
                return BeerPowder(max(0, depth));
            }

            float TraceDown(float3 pos, float rand){
                float3 up = float3(0, 1, 0);
                float stride = (_Altitude1 - pos.y) / max(0.01, up.y * _SampleCountL);
                pos += up * stride * rand;
                float depth = 0;
                for (int s = 0; s < _SampleCountL; s++) {
                    depth += SampleNoise(pos) * stride;
                    pos += up * stride;
                }
                return BeerPowder(max(0, depth));
            }

            half4 frag(Varyings i) : SV_Target {
                float3 ray = -i.rayDir;
                float2 screenUV = i.screenPos.xy / max(0.001, i.screenPos.w);
                float3 sky = SAMPLE_TEXTURE2D(_Tenkoku_SkyBox, sampler_Tenkoku_SkyBox, screenUV).rgb * 0.8;
                if (_Tenkoku_UseElek == 0.0) sky = SAMPLE_TEXTURE2D(_Tenkoku_SkyTex, sampler_Tenkoku_SkyTex, screenUV).rgb * 0.8;

                if (ray.y < 0.01) return half4(sky, 0);

                float3 wscPos = _WorldSpaceCameraPos; wscPos.y = 0;
                float dist0 = _Altitude0 / ray.y;
                float dist1 = _Altitude1 / ray.y;
                int samples = (int)lerp(_SampleCount1, _SampleCount0, ray.y);
                if (_Tenkoku_overcastAmt > 0.275) samples = 4;
                
                float stride = (dist1 - dist0) / max(1, samples);
                float offs = UVRandom(i.uv) * stride;
                float3 pos = wscPos + ray * (dist0 + offs);
                
                float accumDepth = 0;
                float3 accCol = lerp(sky, float3(0,0,0), _Darkness);
                float underCloud = 0;
                float hg = 0.5 * (1.0 - _HGCoeff*_HGCoeff) / pow(1.0 + _HGCoeff*_HGCoeff - 2.0*_HGCoeff*dot(ray, Tenkoku_Vec_SunFwd.xyz), 1.5);

                if (_NoiseBias > 0) {
                    for (int ss = 0; ss < samples; ss++) {
                        float n = SampleNoise(pos);
                        if (n > _Edge) {
                            float d = n * stride;
                            float scatter = d * _Scatter * hg * Trace(pos, UVRandom(i.uv + (float)ss));
                            accCol += _TenkokuCloudColor.rgb * scatter * exp(-_Extinct * accumDepth);
                            underCloud += d * _Scatter * hg * TraceDown(pos, UVRandom(i.uv + (float)ss)) * exp(-_Extinct * accumDepth);
                            accumDepth += d;
                        }
                        pos += ray * stride;
                    }
                }

                float finalAlpha = saturate(accumDepth * 0.1); // Simplified alpha
                float3 retCol = lerp(sky, accCol, finalAlpha);
                
                // Add overcast and lighting tints
                retCol = lerp(retCol, retCol * _Tenkoku_overcastColor.rgb, _Tenkoku_overcastAmt);
                retCol *= 1.25;

                return half4(retCol, 1.0 - finalAlpha);
            }
            ENDHLSL
        }
    }
}
