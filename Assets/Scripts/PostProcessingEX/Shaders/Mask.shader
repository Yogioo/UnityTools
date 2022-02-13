Shader "Hidden/Custom/Mask"
{
    HLSLINCLUDE
    
    #include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"
    
    TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
    TEXTURE2D_SAMPLER2D(_MaskTex, sampler_MaskTex);
    float _Blend;
    
    float ConcentricCircle(float2 texcoord){
        // const float pi = 3.1415926535f;
        float2 uv = texcoord * 2.0f - 1.0f;
        
        float angle = (atan2(uv.x,uv.y) / PI + 1.0f) * 0.5f;

        angle *= 50;
        float left = angle - floor(angle);
        float right = ceil(angle) - angle;
        return step(0.1f,left * right);
    }

    float4 Frag(VaryingsDefault i): SV_Target
    {
        float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord);
        // float4 mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, i.texcoord);
        
        float4 maskColor =  color * ConcentricCircle(i.texcoord);
        maskColor = step(0.1,maskColor);
        
        color = lerp(color, maskColor, _Blend);
        //color *= lerp(1, mask, _Blend);
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