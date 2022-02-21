Shader "Yogi/Raymarching"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
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
            Cull Off
            ZWrite On
            ZTest On
            
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
                float3 worldPos : TEXCOORD1;
                float3 objCenterWorldPos : TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.objCenterWorldPos = mul(unity_ObjectToWorld, float4(0, 0, 0, 1));
                return o;
            }

            float opSmoothUnion(float d1, float d2, float k)
            {
                float h = clamp(0.5 + 0.5 * (d2 - d1) / k, 0.0, 1.0);
                return lerp(d2, d1, h) - k * h * (1.0 - h);
            }

            float sdf(float3 p)
            {
                float s0 = length(p) - 0.5f;
                float s1 = length(p - .5) - 0.5f;

                return opSmoothUnion(s0, s1, .5);
            }

            float3 GetNormal(float3 p)
            {
                float2 min = float2(1e-2, 0);
                float3 normal = sdf(p) - float3(
                    sdf(p - min.xyy),
                    sdf(p - min.yxy),
                    sdf(p - min.yyx)
                );
                return normalize(normal);
            }

            #define MAX_DIS 1000
            #define STEP_COUNT 150
            #define SURF_DIST 1e-3

            float Raymarch(float3 ro, float3 rd)
            {
                const float maxDistance = 1000;
                float dO = 0;
                float dS;
                for (int i = 0; i < STEP_COUNT; ++i)
                {
                    float3 p = ro + rd * dO;
                    dS = sdf(p);
                    dO += dS;
                    if (dS < SURF_DIST || dS > maxDistance)
                        break;
                }
                return dO;
            }

            float4 Triplanar(sampler2D tex, float3 worldPos, float3 worldNormal)
            {
                //calculate UV coordinates for three projections
                float2 uv_front = worldPos.xy;
                float2 uv_side = worldPos.zy;
                float2 uv_top = worldPos.xz;

                //read texture at uv position of the three projections
                float4 col_front = tex2D(tex, uv_front);
                float4 col_side = tex2D(tex, uv_side);
                float4 col_top = tex2D(tex, uv_top);

                //generate weights from world normals
                float3 weights = worldNormal;
                //show texture on both sides of the object (positive and negative)
                weights = abs(weights);
                //make the transition sharper
                // weights = pow(weights, 1);
                //make it so the sum of all components is 1
                weights = weights / (weights.x + weights.y + weights.z);

                //combine weights with projected colors
                col_front *= weights.z;
                col_side *= weights.x;
                col_top *= weights.y;

                //combine the projected colors
                float4 col = col_front + col_side + col_top;
                return col;
            }


            fixed4 frag(v2f i) : SV_Target
            {
                float3 ro = _WorldSpaceCameraPos;
                float3 rd = normalize(i.worldPos - ro);
                float d = Raymarch(ro - i.objCenterWorldPos, rd);
                fixed4 col = tex2D(_MainTex, i.uv);

                if (d >= MAX_DIS)
                {
                    discard;
                }

                float3 worldPos = ro + d * rd;
                float3 normal = GetNormal(ro + d * rd - i.objCenterWorldPos).rgbr;
                float3 lightDir = normalize(float3(.5, .2, .78) - i.worldPos);
                // return max(0, dot(lightDir, normal)).rrrr + normal.rgbr;
                
                return Triplanar(_MainTex,worldPos,normal.xyz);
            }
            ENDCG
        }
    }
}