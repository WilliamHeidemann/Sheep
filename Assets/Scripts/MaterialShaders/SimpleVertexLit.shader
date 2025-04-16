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

                // matrix
                float4x4 b = unity_ObjectToWorld;
                matrix a = b * unity_WorldToObject;

                Agent agent = agents[id];
                float2 velocity = agent.velocity;
                if (velocity.x == 0 && velocity.y == 0)
                {
                    velocity = float2(1, 0);
                }

                float3 forward = normalize(float3(velocity.x, 0, velocity.y));
                float3 up = float3(0, 1, 0);
                float3 right = cross(up, forward);
                // up = cross(forward, right); if velocity.y is not always zero
                float3x3 rotationMatrix = float3x3(right, up, forward);
                float3 localPosition = mul(rotationMatrix, v.vertex.xyz);

                float3 offset = agent.position;
                // float3 worldPosition = TransformObjectToWorldNormal(localPosition) + offset; // for single mesh filter object
                float3 worldPosition = localPosition + offset; // for combined skinned meshes
                output.position = TransformWorldToHClip(worldPosition);

                float3 localNormal = normalize(mul(v.normal, rotationMatrix));
                // float3 worldNormal = TransformObjectToWorldNormal(localNormal); // for single mesh filter object
                float3 worldNormal = localNormal; // for combined skinned meshes

                Light light = GetMainLight();

                float normalDotLight = max(0, dot(worldNormal, -normalize(light.direction)));

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