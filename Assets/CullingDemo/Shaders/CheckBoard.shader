Shader "Unlit/CheckBoard"
{
    Properties
    {
        // _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        // LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            fixed4 _Color;

            #include "UnityCG.cginc"

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


            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed checker(float2 uv)
            {
                float2 repeatUV = uv * 100;
                float2 c = floor(repeatUV) / 2;
                float checker = frac(c.x + c.y) * 1.0;
                return checker;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed col = checker(i.uv);
                return col;
            }
            ENDCG
        }
    }
}
