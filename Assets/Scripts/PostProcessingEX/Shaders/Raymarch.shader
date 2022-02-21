Shader "Hidden/Custom/Raymarch"
{
    HLSLINCLUDE
    #include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"

    TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
    TEXTURE2D_SAMPLER2D(_MaskTex, sampler_MaskTex);
    TEXTURE2D_SAMPLER2D(_CameraDepthTexture, sampler_CameraDepthTexture);

    float4x4 _ViewToWorld;

    float sdSphere(float3 p, float s)
    {
        return length(p) - s;
    }

    float sdBox(float3 p, float3 b)
    {
        float3 q = abs(p) - b;
        return length(max(q, 0.0)) + min(max(q.x, max(q.y, q.z)), 0.0);
    }

    float opSmoothUnion(float d1, float d2, float k)
    {
        float h = clamp(0.5 + 0.5 * (d2 - d1) / k, 0.0, 1.0);
        return lerp(d2, d1, h) - k * h * (1.0 - h);
    }

    float hash(float n)
    {
        return frac(sin(n) * 43728.1453);
    }

    float noise(float3 x)
    {
        float3 p = floor(x);
        float3 f = frac(x);

        f = f * f * (3.0 - 2.0 * f);
        float n = p.x + p.y * 55.0 + p.z * 101.0;

        return lerp(
            lerp(
                lerp(hash(n), hash(n + 1.0), f.x),
                lerp(hash(n + 55.0), hash(n + 56.0), f.x),
                f.y),
            lerp(
                lerp(hash(n + 101.0), hash(n + 102.0), f.x),
                lerp(hash(n + 156.0), hash(n + 157.0), f.x),
                f.y),
            f.z);
    }

    float sdf(float3 p)
    {
        float sphereCount = 3;
        float ret = 10000;
        for (float i = 0; i < sphereCount; ++i)
        {
            float3 offset;
            float dif = (i*2 / sphereCount - 0.5) * _Time.y;

            offset.x = (noise(float3(.1,dif,.5))-.5) * 5;
            offset.y = noise(float3(dif,.2,.2)) *2;
            offset.z = (noise(float3(.7,0,dif))-.5) * 3;
            float rad = noise(dif) + 0.5;
            float s = sdSphere(p - offset, rad);
            ret = opSmoothUnion(ret, s, .5);
        }

        for (float i = 0; i < sphereCount; ++i)
        {
            float3 offset;
            float dif = (i*2 / sphereCount - 0.5) * _Time.y;

            offset.x = (noise(float3(.1,dif,.5))-.5) * 5;
            offset.y = noise(float3(dif,.2,.2)) *2;
            offset.z = (noise(float3(.7,0,dif))-.5) * 3;
            float rad = noise(dif) + 0.5;
            float s = sdBox(p - offset, rad * float3(sin(_Time.y)+0.5,1,1));
            ret = opSmoothUnion(ret, s, .5);
        }
        
        return ret; //opSmoothUnion(s0, s1, .5);
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
    #define STEP_COUNT 200
    #define SURF_DIST 1e-3

    float Raymarch(float3 ro, float3 rd, float maxDis)
    {
        float dO = 0;
        float dS;
        for (int i = 0; i < STEP_COUNT; ++i)
        {
            float3 p = ro + rd * dO;
            dS = sdf(p);
            dO += dS;
            if (dS < SURF_DIST || dS > maxDis)
                break;
        }
        return dO;
    }

    float4 Triplanar(Texture2D<float4> tex, SamplerState samplerState, float3 worldPos, float3 worldNormal)
    {
        //calculate UV coordinates for three projections
        float2 uv_front = worldPos.xy;
        float2 uv_side = worldPos.zy;
        float2 uv_top = worldPos.xz;

        //read texture at uv position of the three projections
        float4 col_front = SAMPLE_TEXTURE2D(tex, samplerState, uv_front);
        float4 col_side = SAMPLE_TEXTURE2D(tex, samplerState, uv_side);
        float4 col_top = SAMPLE_TEXTURE2D(tex, samplerState, uv_top);

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

    float4 Frag(VaryingsDefault i): SV_Target
    {
        float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord);
        float depth = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, i.texcoord).r;
        depth = Linear01Depth(depth);
        if (depth < 1)
        {
            //view Depth
            float z = depth * _ProjectionParams.z;
            float maxDis = min(z,MAX_DIS);
            // convert to world space position
            float2 p11_22 = float2(unity_CameraProjection._11, unity_CameraProjection._22);
            float3 viewPos = float3((i.texcoord * 2 - 1) / p11_22, -1) * z;
            float3 worldPos = mul(_ViewToWorld, float4(viewPos, 1)).xyz;

            float3 ro = _WorldSpaceCameraPos;
            float3 rd = normalize(worldPos - ro);
            float d = Raymarch(ro, rd, maxDis);


            if (d >= maxDis)
            {
                return color;
            }
            float3 realWorldPos = ro + d * rd;
            float3 normal = GetNormal(ro + d * rd).rgb;
            return Triplanar(_MaskTex, sampler_MaskTex, realWorldPos, normal.xyz);
        }
        return color;
    }
    ENDHLSL

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma vertex VertDefault
            #pragma fragment Frag
            ENDHLSL

        }
    }
}