// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "MasakaVFX/HueShift"
{
    Properties
    {
        _NoiseRGB ("Noise RGB: 0向左 1向右 偏移", 2D) = "gray" {}
        _MaskMap("Mask Tex: 遮罩黑色不偏移", 2D) = "white" {}
    }
    SubShader
    {
        GrabPass
        {
            "_GrabTexture"
        }

        Tags
        {
            "RenderType"="Transparent" "Queue" = "Transparent"
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

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
                half4 uvgrab : TEXCOORD1;
            };

            sampler2D _NoiseRGB;
            float4 _NoiseRGB_ST;

            sampler2D _GrabTexture;
            float4 _GrabTexture_TexelSize;

            sampler2D _MaskMap;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.uvgrab = ComputeGrabScreenPos(o.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_NoiseRGB, i.uv) * 2. - 1.;
                float uvMask =tex2D(_MaskMap,i.uv);
                half grabRColor = tex2Dproj(_GrabTexture,UNITY_PROJ_COORD(i.uvgrab + col.r * float4(uvMask,0,0,0))).r;
                half grabGColor = tex2Dproj(_GrabTexture,UNITY_PROJ_COORD(i.uvgrab)+ col.g * float4(uvMask,0,0,0)).g;
                half grabBColor = tex2Dproj(_GrabTexture,UNITY_PROJ_COORD(i.uvgrab)+ col.b * float4(uvMask,0,0,0)).b;
                half4 grabColor = half4(grabRColor,grabGColor,grabBColor,1);

                return grabColor;
            }
            ENDCG
        }
    }
}