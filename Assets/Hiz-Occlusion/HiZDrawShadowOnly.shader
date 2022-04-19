// Upgrade NOTE: replaced 'UNITY_INSTANCE_ID' with 'UNITY_VERTEX_INPUT_INSTANCE_ID'

Shader "Yogi/HiZDrawShadowOnly"
{
    SubShader
    {
        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }

            ZWrite On ZTest LEqual Cull Back

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster
            #pragma shader_feature __ HiZCull
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:setup
            #include "UnityCG.cginc"

            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            StructuredBuffer<float4x4> localToWorldBuffer;
            #endif
            void setup()
            {
                #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                    unity_ObjectToWorld = localToWorldBuffer[unity_InstanceID];
                #endif
            }

            struct a2v
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                V2F_SHADOW_CASTER;
            };


            v2f vert(appdata_base v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                v2f o;
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
}