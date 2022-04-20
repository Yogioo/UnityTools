Shader "Masaka/LayeredParallax"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
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
                float3 tangent : TANGENT;
                float3 normal: NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 viewDirTangentSpace : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            uint _Count;
            float _Height;

            v2f vert(appdata v)
            {
                v2f o;
                float4 camObjPos = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1.0));
                float3 viewdir = v.vertex.xyz - camObjPos.xyz;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                // float tangentSign = v.tangent.w * unity_WorldTransformParams.w;
                float3 bitangent = cross(v.tangent,v.normal);
                float3x3 tangentMatrix = float3x3(v.tangent, bitangent, v.normal);
                o.viewDirTangentSpace = mul(tangentMatrix, viewdir);

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = 0; //tex2D(_MainTex, i.uv);

                // Reference: https://halisavakis.com/my-take-on-shaders-parallax-effect-part-ii/
                for (int index = 0; index < _Count; index++)
                {
                    float ratio = (float)index / _Count;
                    float2 uvOffset = normalize(i.viewDirTangentSpace) * lerp(0,_Height, ratio) * lerp(1, 0, ratio);
                    col += tex2D(_MainTex, i.uv + uvOffset);
                }
                col /= _Count;

                // return i.viewDirTangentSpace.xyzx;
                return col;
            }
            ENDCG
        }
    }
}