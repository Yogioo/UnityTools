Shader "Hidden/Custom/Mask"
{
    HLSLINCLUDE
    
    #include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"
    
    TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
    TEXTURE2D_SAMPLER2D(_MaskTex, sampler_MaskTex);
    float _Blend;

    float4 Frag(VaryingsDefault i): SV_Target
    {
        float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord);
        float4 mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, i.texcoord);
        
        color += lerp(color, mask, _Blend);
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