Shader "Masaka/LayeredParallax_WorldSpace"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _WorldScale("World Scale",float) = .1
        _Count ("Count",Range(1,100)) = 5
        _Height("Height",float) = 2
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

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
                float3 viewDirWorldSpace : TEXCOORD1;
                float3 worldPos: TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            uint _Count;
            float _Height;
            float _WorldScale;

            v2f vert(appdata v)
            {
                v2f o;
                float4 camObjPos = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1.0));
                float3 viewdir = v.vertex.xyz - camObjPos.xyz;

                o.worldPos = mul(UNITY_MATRIX_M, v.vertex);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.viewDirWorldSpace = viewdir;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = 0;

                // Reference: https://halisavakis.com/my-take-on-shaders-parallax-effect-part-ii/
                for (int index = 0; index < _Count; index++)
                {
                    float ratio = (float)index / _Count;
                    float2 uvOffset = normalize(i.viewDirWorldSpace).xz * lerp(0, _Height, ratio) * lerp(1, 0, ratio);
                    col += tex2D(_MainTex, (i.worldPos.xz + uvOffset) * _WorldScale);
                }
                col /= _Count;

                // return i.viewDirWorldSpace.xyzx;
                return col;
            }
            ENDCG
        }
    }
}