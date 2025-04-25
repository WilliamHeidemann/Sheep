Shader "Custom/TiltShiftShader"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _BlurAmount ("Blur Amount", Float) = 2.0
        _FocusPosition ("Focus Y Position", Range(0,1)) = 0.5
        _FocusRange ("Focus Range", Range(0,1)) = 0.2
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }

        Pass
        {
            Name "TiltShift"
            ZTest Always Cull Off ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

            float _BlurAmount;
            float _FocusPosition;
            float _FocusRange;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = v.vertex;
                o.uv = v.uv;
                return o;
            }

            float2 shiftUV(float2 uv)
            {
                // Center around middle Y
                float y = uv.y - 0.5;

                // Scale down the vertical based on distance from center
                float shift = y * abs(y) * 0.3; // tweak the 0.3 for effect strength

                uv.y -= shift;
                return uv;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 warpedUV = shiftUV(i.uv);

                float blur = saturate(abs(warpedUV.y - _FocusPosition) / _FocusRange);
                blur = smoothstep(0, 1, blur);

                int samples = 7;
                float2 dir = float2(_MainTex_TexelSize.x, 0);

                float3 col = tex2D(_MainTex, warpedUV).rgb * 0.2;
                for (int n = 1; n <= samples; ++n)
                {
                    float offset = n * _BlurAmount * blur * 0.5;
                    col += tex2D(_MainTex, warpedUV + dir * offset).rgb * 0.1;
                    col += tex2D(_MainTex, warpedUV - dir * offset).rgb * 0.1;
                }

                return float4(col, 1.0);
            }
            ENDCG
        }
    }
}