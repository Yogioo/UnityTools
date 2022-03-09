// Upgrade NOTE: replaced 'UNITY_INSTANCE_ID' with 'UNITY_VERTEX_INPUT_INSTANCE_ID'

Shader "Yogi/HiZDraw"
{
    Properties
    {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
    }
    SubShader
    {
        Pass
        {

            Tags
            {
                "LightMode"="ForwardBase"
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:setup
            #pragma shader_feature __ HiZCull

            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"
            #include "AutoLight.cginc"

            sampler2D _MainTex;

            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            StructuredBuffer<float4x4> localToWorldBuffer;
            #endif
            void setup()
            {
                #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                    unity_ObjectToWorld = localToWorldBuffer[unity_InstanceID];
                #endif
            }

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv_MainTex : TEXCOORD0;
                float3 ambient : TEXCOORD1;
                float3 diffuse : TEXCOORD2;
                float3 color : TEXCOORD3;
                SHADOW_COORDS(4)
            };

            v2f vert(appdata_full v)
            {
                UNITY_SETUP_INSTANCE_ID(v);

                float3 worldPosition = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1.0f));
                // float3 worldPosition = ;
                float3 worldNormal = v.normal;


                float3 ndotl = saturate(dot(worldNormal, _WorldSpaceLightPos0.xyz));
                float3 ambient = ShadeSH9(float4(worldNormal, 1.0f));
                float3 diffuse = (ndotl * _LightColor0.rgb);
                float3 color = v.color;

                v2f o;
                o.pos = mul(UNITY_MATRIX_VP, float4(worldPosition, 1.0f));
                o.uv_MainTex = v.texcoord;
                o.ambient = ambient;
                o.diffuse = diffuse;
                o.color = color;
                TRANSFER_SHADOW(o)
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed shadow = SHADOW_ATTENUATION(i);
                fixed4 albedo = tex2D(_MainTex, i.uv_MainTex);
                float3 lighting = i.diffuse * shadow + i.ambient;
                fixed4 output = fixed4(albedo.rgb * i.color * lighting, albedo.w);
                UNITY_APPLY_FOG(i.fogCoord, output);
                return output;
            }
            ENDCG
        }

        // Pass to render object as a shadow caster
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