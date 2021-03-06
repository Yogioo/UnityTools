Shader "Unlit/Transparent_Shadow"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ClipValue("ClipValue",float) = .1
        [HDR]_Color("Color",Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Transparent" "Queue"="Transparent" 
        }
        Pass
        {
            ZWrite off
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // #pragma multi_compile_fwdbase_fullshadows

            #include "UnityCG.cginc"
            #include "AutoLight.cginc"
            #include "Shadows.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 pos : SV_POSITION;
                // SHADOW_COORDS(1)
                float4 worldPos : TEXCOORD2;
                float4 screenPos: TEXCOORD3;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _AlphaScale;
            float _ClipValue;
            float4 _Color;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                // TRANSFER_SHADOW(o);
                o.worldPos = mul(unity_ObjectToWorld,v.vertex);
                o.screenPos = ComputeScreenPos(o.pos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                half shadow = GetSunShadowsAttenuation_PCF5x5(i.worldPos,i.screenPos.z,0).x;
                col.rgb = col.rgb * _Color * shadow;
                return col;
            }
            ENDCG
        }
//
//        Pass
//        {
//            Tags
//            {
//                "LightMode"="ShadowCaster"
//            }
//            Offset 1, 1
//            Cull Off
//            CGPROGRAM
//            #pragma vertex vert
//            #pragma fragment frag
//            #include "UnityCG.cginc"
//
//            struct app_data
//            {
//                float4 vertex : POSITION;
//            };
//
//            struct v2f
//            {
//                V2F_SHADOW_CASTER;
//            };
//
//            v2f vert(app_data v)
//            {
//                v2f o;
//                TRANSFER_SHADOW_CASTER(o)
//                return o;
//            }
//
//            float4 frag(v2f i) : COLOR
//            {
//                SHADOW_CASTER_FRAGMENT(i)
//            }
//            ENDCG
//        }
    }
}