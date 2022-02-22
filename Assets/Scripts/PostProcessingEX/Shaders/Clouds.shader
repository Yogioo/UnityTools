// Reference: https://zhuanlan.zhihu.com/p/248406797
Shader "Hidden/Custom/Clouds"
{
    HLSLINCLUDE
    #include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"

    TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
    TEXTURE2D_SAMPLER2D(_CameraDepthTexture, sampler_CameraDepthTexture);

    float4x4 _InverseProjectionMatrix;
    float4x4 _InverseViewMatrix;
    float3 _boundsMin, _boundsMax;
    float _texScale;
    sampler3D _noiseTex;
    
    // Get World Space By Screen UV and Depth
    float4 GetWorldSpacePosition(float depth, float2 screenUV)
    {
        // screen to view
        float4 view_vector = mul(_InverseProjectionMatrix, float4(screenUV * 2.0 - 1.0, depth, 1.));
        view_vector.xyz /= view_vector.w;
        view_vector.w = 1;

        // view to world
        float4x4 l_matViewInv = _InverseViewMatrix;
        float4 world_vector = mul(l_matViewInv, view_vector);
        return world_vector;
    }

    float CloudRayMatching(float3 ro, float3 rd, float3 boundsMin, float3 boundsMax)
    {
        float sum = 0.0;
        rd *= .3; // step length
        for (int i = 0; i < 256; ++i)
        {
            ro += rd;
            if (ro.x < boundsMax.x && ro.x > boundsMin.x &&
                ro.z < boundsMax.z && ro.z > boundsMin.z &&
                ro.y < boundsMax.y && ro.y > boundsMin.y
            )
            {
                sum += 0.01;
            }
        }
        return sum;
    }

    // Ray Cast Box Get out float2:(Ray Enter Bounds form Camera Distance,Ray Out Bounds form Enter Point Distance)
    // form https://jcgt.org/published/0007/03/04/
    // invRaydir = 1/RayDir
    float2 RayBoxDst(float3 boundsMin, float3 boundsMax,
                     float3 rayOrigin, float3 invRaydir)
    {
        float3 t0 = (boundsMin - rayOrigin) * invRaydir;
        float3 t1 = (boundsMax - rayOrigin) * invRaydir;
        float3 tmin = min(t0, t1);
        float3 tmax = max(t0, t1);

        // enter point
        float dstA = max(max(tmin.x, tmin.y), tmin.z);
        // out point
        float dstB = min(tmax.x, min(tmax.y, tmax.z));

        float dstToBox = max(0, dstA);
        float dstInsideBox = max(0, dstB - dstToBox);

        return float2(dstToBox, dstInsideBox);
    }

    float sampleDensity(float3 rayPos)
    {
        float3 uvw = rayPos * _texScale;
        float4 shapeNoise = tex3D(_noiseTex, uvw);
        return shapeNoise.r;
    }
    


    float4 Frag(VaryingsDefault i) : SV_Target
    {
        _boundsMin = -10;
        _boundsMax = 10;
        // screen Depth
        float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, i.texcoordStereo);
        // world space pos
        float4 worldPos = GetWorldSpacePosition(depth, i.texcoord);
        float3 rayPos = _WorldSpaceCameraPos;
        // camera to pixel pos
        float3 worldViewDir = normalize(worldPos.xyz - rayPos.xyz);
        float depthEyeLinear = length(worldPos.xyz - _WorldSpaceCameraPos);
        float2 rayToBoundsInfo = RayBoxDst(_boundsMin, _boundsMax, rayPos, (1 / worldViewDir));
        float dstToBox = rayToBoundsInfo.x;
        float dstInsideBox = rayToBoundsInfo.y;
        float dstLimit = min(depthEyeLinear - dstToBox, dstInsideBox);

        float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord);

        float3 entryPoint = rayPos + worldViewDir * dstToBox;
        

        if (dstLimit > 0)
        {
            float cloud = CloudRayMatching(rayPos.xyz, worldViewDir, _boundsMin, _boundsMax);
            return color + cloud;
        }
        else
        {
            return color;
        }
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