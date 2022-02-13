Shader "Hidden/Custom/EdgeDetection"
{
    HLSLINCLUDE
    
    #include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"
    
    TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
    TEXTURE2D_SAMPLER2D(_CameraDepthTexture, sampler_CameraDepthTexture);
    
    float4 _MainTex_TexelSize;
    float _Blend;
    
    float getLuminance(float3 color)
    {
            return dot(color.rgb, float3(0.2126729, 0.7151522, 0.0721750));
    }
    
    float4 Frag(VaryingsDefault i): SV_Target
    {
            float x, y;
            x = _MainTex_TexelSize.x;
            y = _MainTex_TexelSize.y;
        
            float4 color, top, down, left, right;
            float t, d, l, r;
        
            color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord);
            top = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord + float2(0, y));
            down = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord + float2(0, -y));
            left = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord + float2(-x, 0));
            right = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord + float2(x, 0));
        
            float a= ddx(x);
        
            t = getLuminance(top);
            d = getLuminance(down);
            l = getLuminance(left);
            r = getLuminance(right);
        
            float edge = abs(t - d) + abs(l - r);
        
            color = lerp(color, edge.rrrr, _Blend);
        
        
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