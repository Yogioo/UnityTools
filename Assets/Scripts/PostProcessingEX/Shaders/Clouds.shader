// Reference: https://zhuanlan.zhihu.com/p/248406797
Shader "Hidden/Custom/Clouds"
{
    HLSLINCLUDE
    #include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"

    TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
    TEXTURE2D_SAMPLER2D(_CameraDepthTexture, sampler_CameraDepthTexture);

    float _rayStep;


    float4x4 _InverseProjectionMatrix;
    float4x4 _InverseViewMatrix;
    float3 _boundsMin, _boundsMax;

    sampler2D _WeatherMap;
    sampler3D _ShapeTex;
    float _ShapeTiling;
    sampler3D _ShapeDetailTex;
    float _ShapeDetailTiling;
    float3 _ShapeNoiseWeights;
    float _DensityOffset;
    float _DensityMultiplier;
    float _DetailWeights;
    float _DetailNoiseWeight;
    float4 _xy_Speed_zw_Warp;
    sampler2D _MaskNoise;

    float3 _WorldSpaceLightPos0;
    float _lightAbsorptionTowardSun;
    float _lightAbsorptionThroughCloud;

    float3 _ColA, _ColB;
    float _ColorOffsetA, _ColorOffsetB;
    float3 _LightColor0;
    float _DarknessThreshold;

    float4 _phaseParams;

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

    float Remap(float original_value, float original_min, float original_max, float new_Min, float new_Max)
    {
        float zeroToOne = (original_value - original_min) / (original_max - original_min);
        float newRange = new_Max - new_Min;
        return new_Min + zeroToOne * newRange;
    }

    float SampleWeather(float3 pos, float2 uv, float moveSpeed)
    {
        float heightGradient;
        float3 size = _boundsMax - _boundsMin;

        // Get Weather Map
        float4 weather = tex2D(_WeatherMap, uv + float2(moveSpeed,0.));
        // Soft Down Side
        float gMin = Remap(weather.x, 0, 1, .1, .6);
        // Fade by Height
        float heightPercent = (pos.y - _boundsMin.y) / size.y;
        heightGradient = saturate(Remap(heightPercent, 0.0, gMin, 0, 1))
            * saturate(Remap(heightPercent, 0.0, weather.r, 1.0, 0.0));

        // Edge of the attenuation
        const float containerEdgeFadeDst = 10;
        float2 minA = _boundsMax.xz - pos.xz;
        float2 minB = pos.xz - _boundsMin.xz;
        float2 minDis = min(minA, minB);
        float2 dstFromEdge = min(containerEdgeFadeDst, minDis);
        // 0~containerEdgeFadeDst remap to 0~1
        float edgeWeight = min(dstFromEdge.x, dstFromEdge.y) / containerEdgeFadeDst;

        // mix
        heightGradient *= edgeWeight;

        return heightGradient;
    }



    float SampleDensity(float3 rayPos)
    {
        float3 size = _boundsMax - _boundsMin;
        float3 boundsCentre = (_boundsMax + _boundsMin) * .5f;
        float2 uv = (size.xz * 0.5f + (rayPos.xz - boundsCentre.xz)) / max(size.x, size.z);
        
        float speedShape = _Time.y * _xy_Speed_zw_Warp.x;
        float speedDetail = _Time.y * _xy_Speed_zw_Warp.y;
        float4 maskNoise = tex2Dlod(_MaskNoise,float4(uv + float2(speedShape * .5, 0),0,0));
        float3 uvwShape = rayPos * _ShapeTiling + float3(speedShape, speedShape * .2, 0.);
        float3 uvwDetail = rayPos * _ShapeDetailTiling + float3(speedDetail, speedDetail * .2, 0.);

        float4 shapeNoise = tex3Dlod(_ShapeTex, float4(uvwShape.xyz + (maskNoise * _xy_Speed_zw_Warp.z * 0.1), 0.));
        float4 detailNoise = tex3Dlod(_ShapeDetailTex, float4(uvwDetail.xyz * _xy_Speed_zw_Warp.w, 0.));

        float4 normalizedShapeWeights = normalize(float4(_ShapeNoiseWeights.xyz, 1.));
        float weather = SampleWeather(rayPos,uv, speedShape * .4);
        float shapeFBM = dot(shapeNoise, normalizedShapeWeights) * weather;
        float baseShapeDensity = shapeFBM + _DensityOffset * .01f;

        if (baseShapeDensity > 0)
        {
            // Shape Detail 
            float detailFBM = pow(detailNoise.r, _DetailWeights);
            float oneMinusShape = 1 - baseShapeDensity;
            float detailErodeWeight = oneMinusShape * oneMinusShape * oneMinusShape;
            float cloudDensity = baseShapeDensity - detailFBM * detailErodeWeight * _DetailNoiseWeight;

            return saturate(cloudDensity * _DensityMultiplier);
        }
        return 0;
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

    float3 LightMarch(float3 position)
    {
        // it is already normalized 
        float3 dirToLight = _WorldSpaceLightPos0.xyz;
        // light calc bounds
        float3 dstInsideBox = RayBoxDst(_boundsMin, _boundsMax, position, 1 / dirToLight).y;
        float stepSize = dstInsideBox / 10;
        float totalDensity = 0;
        for (int step = 0; step < 8; step++)
        {
            position += dirToLight * stepSize;
            totalDensity += max(0, SampleDensity(position));
        }
        float transmittance = exp(-totalDensity * _lightAbsorptionTowardSun);
        // remap to three sections of color
        // most bright => light color
        // mid bright => Color A
        // dark => Color B
        // It's Trick Not Physics
        float3 cloudColor = lerp(_ColA, _LightColor0, saturate(transmittance * _ColorOffsetA));
        cloudColor = lerp(_ColB, cloudColor, saturate(pow(transmittance * _ColorOffsetB, 3)));
        return _DarknessThreshold + transmittance * cloudColor * (1 - _DarknessThreshold);
    }

    float hg(float a, float g)
    {
        float g2 = g * g;
        return (1 - g2) / (4. * PI * pow(1 + g2 - 2 * g * a, 1.5));
    }

    float phase(float a)
    {
        float blend = .5;
        float hgBlend = lerp(hg(a, _phaseParams.x), hg(a, - _phaseParams.y), blend);
        float ret = _phaseParams.z + hgBlend * _phaseParams.w;
        return ret;
    }


    float4 Frag(VaryingsDefault i) : SV_Target
    {
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

        // sun light scattering
        float cosAngle = dot(worldViewDir, _WorldSpaceLightPos0.xyz);
        float3 phaseValue = phase(cosAngle);


        float3 entryPoint = rayPos + worldViewDir * dstToBox;

        float dstTravelled = 0;

        float sumDensity = 1;
        float stepSize = _rayStep;
        float3 lightEnergy = 0;
        const float sizeLoop = 512;
        [loop]
        for (int j = 0; j < sizeLoop; j++)
        {
            if (dstTravelled < dstLimit)
            {
                rayPos = entryPoint + (worldViewDir * dstTravelled);
                float density = SampleDensity(rayPos);
                if (density > 0)
                {
                    float3 lightTransmittance = LightMarch(rayPos);
                    lightEnergy += density * stepSize * sumDensity * lightTransmittance * phaseValue;
                    sumDensity *= exp(-density * stepSize * _lightAbsorptionThroughCloud);

                    if (sumDensity < 0.01)
                        break;
                }
            }
            dstTravelled += stepSize;
        }
        float4 cloud = float4(lightEnergy, sumDensity);
        color.rgb *= cloud.a;
        color.rgb += cloud.rgb;

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