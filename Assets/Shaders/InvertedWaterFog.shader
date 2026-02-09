Shader "Custom/InvertedWaterFog"
{
    Properties
    {
        _FogColor ("Fog Color", Color) = (0, 0.5, 1, 1)
        _FogDensity ("Fog Density", Range(0, 5)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+1" "RenderPipeline" = "UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "WaterFog"
            Tags { "LightMode" = "UniversalForward" }
            
            // Render back faces to see the "volume"
            Cull Front
            // We want to blend the fog over the background
            Blend SrcAlpha OneMinusSrcAlpha
            // We need to read depth, but not write to it (transparent)
            ZWrite Off
            ZTest Always

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 positionSS : TEXCOORD0; // Screen Space
                float3 positionWS : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _FogColor;
                float _FogDensity;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.positionSS = ComputeScreenPos(output.positionCS);
                return output;
            }

            // Ray-Box Intersection in Object Space
            // Returns (tNear, tFar)
            // Box is assumed to be -0.5 to 0.5
            float2 RayBoxIntersect(float3 ro, float3 rd)
            {
                float3 boxMin = float3(-0.5, -0.5, -0.5);
                float3 boxMax = float3(0.5, 0.5, 0.5);

                float3 tMin = (boxMin - ro) / rd;
                float3 tMax = (boxMax - ro) / rd;

                float3 t1 = min(tMin, tMax);
                float3 t2 = max(tMin, tMax);

                float tNear = max(max(t1.x, t1.y), t1.z);
                float tFar = min(min(t2.x, t2.y), t2.z);

                return float2(tNear, tFar);
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.positionSS.xy / input.positionSS.w;

                // 1. Get Scene Depth (Opaque Geometry)
                float sceneDepthRaw = SampleSceneDepth(uv);
                float3 scenePosWS = ComputeWorldSpacePosition(uv, sceneDepthRaw, UNITY_MATRIX_I_VP);
                
                // 2. Get Camera Position and View Direction
                float3 camPos = GetCameraPositionWS();
                float3 viewDir = normalize(input.positionWS - camPos);
                
                // 3. Transform to Object Space for easy Box Intersection
                // We use the inverse of the Model Matrix associated with this renderer
                float3 ro = mul(GetWorldToObjectMatrix(), float4(camPos, 1.0)).xyz;
                float3 rd = normalize(mul((float3x3)GetWorldToObjectMatrix(), viewDir));
                
                // 4. Intersect with Unit Box (-0.5 to 0.5)
                // The mesh in the prefab is likely a standard Cube, so bounds are -0.5 to 0.5
                float2 t = RayBoxIntersect(ro, rd);
                float tNear = t.x;
                // float tFar = t.y; // We don't strictly need tFar if we assume we are hitting the back face (which is tFar-ish)

                // 5. Determine Entry Point
                // If Camera is inside box, tNear will be < 0. Entry is camera position (distance 0 along ray).
                // If Camera is outside, Entry is tNear.
                float distToEntry = max(0.0, tNear);

                // 6. Determine End Point (Object Space Distance)
                // We need to convert Scene Depth distance to Object Space distance scale to match 't'
                // Or easier: Convert tNear back to World Space distance?
                // Let's stick to World Space for Distance calculation to ensure uniform fog density.

                // Re-calculate Entry Point in World Space
                float3 entryPosWS = camPos + viewDir * (distToEntry * length(mul((float3x3)GetObjectToWorldMatrix(), rd))); 
                // Note: The scale factor is needed because 't' is in Object Space units. 
                // Better way: If we are effectively rendering the back face, 'input.positionWS' is close to tFar.
                
                // Simplified Logic:
                // Start Point: Max(CameraPos, BoxIntersectionEnter)
                // End Point: Min(BackFacePos, SceneDepthPos)
                
                // World Space distances
                float distCamToScene = distance(camPos, scenePosWS);
                float distCamToBackFace = distance(camPos, input.positionWS);
                float distCamToEntry = 0;

                // Check if camera is inside simple Box (Object Space)
                // AABB Check: ro vs -0.5..0.5
                bool isInside = all(abs(ro) <= 0.5);

                if (!isInside)
                {
                    // If outside, we need robust intersection
                    // We calculated tNear (Object Space).
                    // We need World Distance for tNear.
                    // Scale factor approximation? 
                    // Let's compute entryPosWS exactly.
                    float3 intersectionOS = ro + rd * tNear;
                    entryPosWS = mul(GetObjectToWorldMatrix(), float4(intersectionOS, 1.0)).xyz;
                    distCamToEntry = distance(camPos, entryPosWS);
                }

                float validDist = min(distCamToScene, distCamToBackFace);
                float fogDistance = max(0, validDist - distCamToEntry);

                // 7. Calculate Fog
                float fogFactor = 1.0 - exp(-fogDistance * _FogDensity);

                return half4(_FogColor.rgb, fogFactor * _FogColor.a);
            }
            ENDHLSL
        }
    }
}
