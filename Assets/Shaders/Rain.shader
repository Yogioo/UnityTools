Shader "Yogi/Rain"
{
    Properties
    {
        _MainTex("_MainTex",2D) = "white" {}
        [NoScaleOffset]_NormalTex("_Nomral",2D) = "bump" {}
        _RainMap ("_RainMap", 2D) = "white" {}
        _WaveNormal ("_WaveNormal", 2D) = "bump" {}
        _WaterNormal ("_WaterNormal", 2D) = "bump" {}
        _WaveWidth("_WaveWidth",float ) = .05
        _WaveSpeed("_WaveSpeed",float ) = 1
        _FlowSpeed("_FlowSpeed",float ) = 1
    }
    SubShader
    {
        Tags {"RenderType" = "Opequa" "Queue" = "Geometry"}
        ZWrite On
        ZTest On
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
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;
                float3 worldNormal: TEXCOORD2;
                float3 objPos : TEXCOORD3;
            };

            sampler2D _RainMap,_MainTex;
            float4 _RainMap_ST,_MainTex_ST;
            float _WaveWidth;
            float _WaveSpeed;
            float _FlowSpeed;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = mul(unity_ObjectToWorld,v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.objPos = mul(unity_ObjectToWorld,float4(0,0,0,1));
                return o;
            }

            float waveLayer(float waveValue, float2 offset){
                float emissive = 1 - frac(_Time.y * _WaveSpeed * offset.x + offset.y);
                float width = _WaveWidth;
                emissive =  smoothstep(emissive-width,emissive,waveValue) - smoothstep(emissive,emissive + width,waveValue);
                emissive *= step(width, waveValue);
                return emissive;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float4 originColor = tex2D(_MainTex, i.uv);

                float4 rainMap = tex2D(_RainMap, i.uv);
                float wave = rainMap.r;

                float waveOne = waveLayer(wave,float2(.9,0.1));

                rainMap = tex2D(_RainMap, i.uv + 0.4f);
                float waveTwo = waveLayer(rainMap.r,float2(.7,0.3));

                float switchValue = sin(_Time.y ) /2. +.5;
                float waveResult = lerp(waveOne,waveTwo, switchValue);

                float2 worlduvXY,worlduvZY;
                worlduvXY = i.worldPos.xy;
                worlduvZY = i.worldPos.zy;

                float2 forwardUV = i.objPos.xy-worlduvXY;
                float2 rightUV = i.objPos.zy-worlduvZY;
                float flowMapFoward = tex2D(_RainMap,forwardUV).y;
                float flowMapRight = tex2D(_RainMap,rightUV).y;
                float rightColor = flowMapRight;
                float forwardColor = flowMapFoward;
                float2 maskUVForward = forwardUV + _Time.y * float2(0,-1) * _FlowSpeed;
                float2 maskUVRight = rightUV + _Time.y * float2(0,-1) * _FlowSpeed;
                float maskForward = abs(i.worldNormal.z) * tex2D(_RainMap,maskUVForward).b; 
                float maskRight =  abs(i.worldNormal.x) * tex2D(_RainMap,maskUVRight).b;
                maskForward = smoothstep(.7,2,maskForward);
                maskRight = smoothstep(.7,2,maskRight);
                float flowResult = (rightColor  * maskRight + forwardColor * maskForward) * 10;

                float4 col = originColor + waveResult * saturate(smoothstep(0.45f,1.,i.worldNormal.y)) + flowResult;

                return col;
            }
            ENDCG
        }
    }
}
