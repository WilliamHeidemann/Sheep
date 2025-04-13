Shader "Custom/SimpleVertexLit"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalRenderPipeline"
        }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                float4 color : COLOR;
            };

            struct Agent
            {
                float3 position;
                float2 velocity;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
            CBUFFER_END

            StructuredBuffer<Agent> agents;

            v2f vert(appdata v, const uint id : SV_InstanceID)
            {
                v2f output;

                float3 offset = agents[id].position;
                // float3 worldPosition = TransformObjectToWorldNormal(v.vertex.xyz) + offset; // for single mesh filter object
                float3 worldPosition = v.vertex.xyz + offset; // for combined skinned meshes
                output.position = TransformWorldToHClip(worldPosition);

                // float3 worldNormal = TransformObjectToWorldNormal(v.normal); // for single mesh filter object
                float3 worldNormal = v.normal; // for combined skinned meshes

                Light light = GetMainLight();

                float normalDotLight = max(0, dot(worldNormal, light.direction));

                output.color = _Color * normalDotLight;

                return output;
            }

            half4 frag(v2f input) : SV_Target
            {
                return input.color;
            }
            ENDHLSL
        }
    }
}