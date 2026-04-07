Shader "Hidden/TenkokuDepth" {
Properties {
   _MainTex ("", 2D) = "white" {}
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

        struct Attributes {
            float4 positionOS : POSITION;
            float2 uv : TEXCOORD0;
        };

        struct Varyings {
            float4 positionCS : SV_POSITION;
            float2 uv : TEXCOORD0;
        };

        TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

        Varyings vert (Attributes v)
        {
            Varyings o;
            o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
            o.uv = v.uv;
            return o;
        }

        half4 frag (Varyings i) : SV_Target {
            half4 origColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
            float rawDepth = SampleSceneDepth(i.uv);
            float dpth = LinearEyeDepth(rawDepth, _ZBufferParams);
            
            half4 retValue;
            retValue.rgb = origColor.rgb;
            retValue.a = saturate(dpth / 500.0);
            retValue.a = max(retValue.r, max(retValue.g, retValue.b));

            return retValue;
        }
        ENDHLSL
    }
}
Fallback off
}
