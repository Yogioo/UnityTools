Shader "Hidden/Custom/ScanningWave"
{
    HLSLINCLUDE

    #include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"

    TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
    TEXTURE2D_SAMPLER2D(_CameraDepthTexture, sampler_CameraDepthTexture);
    TEXTURE2D_SAMPLER2D(_CameraDepthNormalsTexture, sampler_CameraDepthNormalsTexture);
    TEXTURE2D_SAMPLER2D(_FontTex, sampler_FontTex);
    
    TEXTURE2D_SAMPLER2D(_ScanTex, sampler_ScanTex);

    
    // 注: 这里需要摄像机Camera.depthTextureMode = DepthTextureMode.DepthNormals;
    // 否则不显示

    float4x4 _ViewToWorld;
    
    float3 _ScanningCenter;
    
    float _Blend;
    
    float3 _WaveColor;
    
    float4 Triplanar(Texture2D<float4> tex, float3 worldPos, float3 worldNormal)
    {
        //calculate UV coordinates for three projections
        float2 uv_front = worldPos.xy;
        float2 uv_side = worldPos.zy ;
        float2 uv_top = worldPos.xz;
        
        //read texture at uv position of the three projections
        float4 col_front = SAMPLE_TEXTURE2D(tex,sampler_FontTex, uv_front);
        float4 col_side = SAMPLE_TEXTURE2D(tex,sampler_FontTex, uv_side);
        float4 col_top = SAMPLE_TEXTURE2D(tex,sampler_FontTex, uv_top);

        //generate weights from world normals
        float3 weights = worldNormal;
        //show texture on both sides of the object (positive and negative)
        weights = abs(weights);
        //make the transition sharper
        //weights = pow(weights, _Sharpness);
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

    float4 Frag(VaryingsDefault i) : SV_Target
    {
        float4 _TextColor = float4(.2,.2,5,1);
        float _TextScale = 1;
        
        float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord);
        float depth = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, i.texcoord);
        depth = Linear01Depth(depth);
        float4 depthNormals = SAMPLE_TEXTURE2D(_CameraDepthNormalsTexture, sampler_CameraDepthNormalsTexture, i.texcoord);
        float3 normal;
        // DecodeDepthNormal(depthNormals,depth,normal);
        normal = DecodeViewNormalStereo(depthNormals);

        if(depth < 1)
        {
            float3 worldNormal = mul(_ViewToWorld, float4(normal,0)).xyz;

            // view depth
            float z =  depth * _ProjectionParams.z;
            // convert to world space position
            float2 p11_22 = float2(unity_CameraProjection._11, unity_CameraProjection._22);
            float3 viewPos= float3((i.texcoord * 2 - 1) / p11_22, -1) * z;
            float3 worldPos = mul(_ViewToWorld, float4(viewPos, 1)).xyz;


            //Triplanar
            float front = Triplanar(_FontTex, worldPos * _TextScale, worldNormal).r;
            
            float scan = SAMPLE_TEXTURE2D(_ScanTex,sampler_ScanTex,(worldPos.xz - _ScanningCenter.xz) * (_Blend) + 0.5);
            
            color.rgb += scan  * front * _WaveColor.rgb;
            //color.rgb += scan;
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