Shader "Custom/SheepVertexShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Cull Front
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
            StructuredBuffer<float3> vertex_animation_buffer;

            float xorshift(int x)
            {
                x ^= x << 13;
                x ^= x >> 17;
                x ^= x << 5;
                return (x & 0xFFFFFFFF) / 4294967296.0f;
            }

            float3x3 create_rotation_matrix(float2 velocity)
            {
                // Normalize the velocity vector in the xz-plane
                float velocityLength = length(velocity); // The length in the xz-plane
                float2 unitVelocity = velocity / velocityLength;

                // Calculate the angle theta (atan2 of the xz components)
                float angle = atan2(unitVelocity.y, unitVelocity.x);

                // Create the rotation matrix around the y-axis
                float3x3 rotationMatrix = float3x3(
                    cos(angle), 0.0f, sin(angle), // First row
                    0.0f, 1.0f, 0.0f, // Second row (no rotation along the y-axis)
                    -sin(angle), 0.0f, cos(angle) // Third row
                );

                return rotationMatrix;
            }

            v2f vert(appdata v, const uint id : SV_InstanceID, uint vertexID : SV_VertexID)
            {
                v2f output;

                Agent agent = agents[id];
                float2 velocity = agent.velocity;
                if (velocity.x == 0 && velocity.y == 0)
                {
                    velocity = float2(1, 0);
                }

                float3 forward = normalize(float3(velocity.x, 0, velocity.y));
                float3 up = float3(0, 1, 0);
                float3 right = normalize(cross(forward, up));
                float3x3 rotationMatrix = float3x3(right, up, forward);
                
                // float3x3 rotationMatrix = create_rotation_matrix(velocity);

                int frame = frac(_Time.x * 40 + xorshift(id)) * 16;
                const int vertices = 1034;
                int index = frame * vertices + vertexID;
                float3 vertex_position = vertex_animation_buffer[index];
                float3 localPosition = mul(rotationMatrix, vertex_position);

                float3 offset = agent.position;
                // float3 worldPosition = TransformObjectToWorldNormal(localPosition) + offset; // for single mesh filter object
                float3 worldPosition = localPosition + offset; // for combined skinned meshes
                output.position = TransformWorldToHClip(worldPosition);

                float3 localNormal = normalize(mul(rotationMatrix, v.normal));
                // float3 worldNormal = TransformObjectToWorldNormal(localNormal); // for single mesh filter object
                float3 worldNormal = localNormal; // for combined skinned meshes

                Light light = GetMainLight();

                float normalDotLight = max(0, dot(worldNormal, normalize(light.direction)));

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