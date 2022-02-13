Shader "Hidden/Custom/RadialBlur"
{
    HLSLINCLUDE
    
    #include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"
    
    TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
    float4 _MainTex_TexelSize;
    float _Blend;
    float _StepLength;
    float _StepCount;

    // https://www.shadertoy.com/view/4sfGRn
    float4 Frag(VaryingsDefault i): SV_Target
    {
        float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord);
        float2 dir = i.texcoord * 2.0f - 1.0f;
        dir *= -1;
        dir *= _MainTex_TexelSize.xy;

        
        float w = 1.0f;
        for(int index = 1; index < _StepCount; index++)
        {
            color += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord + index * dir * _Blend) * w;
            w *= 0.99f;

        }
        color = color / _StepCount;

        // color = step(0.01f,color);

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