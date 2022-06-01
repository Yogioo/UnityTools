Shader "Yogi/RenderImage/ScreenBroken" {
	Properties {
		_MainTex ("Main Tex", 2D) = "white" {}
		_BrokenNormalMap("BrokenNormal Map",2D)="bump"{}
		_BrokenScale("BrokenScale",Range(0,1))=1.0
	}
	SubShader {
		
 
		Pass{
			Tags { "LightMode"="ForwardBase" }
 
			CGPROGRAM
 
			#include "UnityCG.cginc"
 
			#pragma vertex vert
			#pragma fragment frag
 
			//这一部分参数的定义要根据Properties
			fixed4 _Color;
			sampler2D _MainTex;
			float4 _MainTex_ST;
			sampler2D _BrokenNormalMap;
			float4 _BrokenNormalMap_ST;
			float _BrokenScale;
 
			struct a2v{
				float4 vertex : POSITION;
				float4 texcoord : TEXCOORD0;
			};
 
			//输出部分要和输入部分对应起来,而输出部分又要由片元着色器里的计算模型来确定
			struct v2f{
				float4 pos : SV_POSITION;
				float4 uv : TEXCOORD0;
			};
 
			v2f vert(a2v v){
				v2f o;
				o.pos=UnityObjectToClipPos(v.vertex);
				
				o.uv.xy=TRANSFORM_TEX(v.texcoord,_MainTex);
				o.uv.zw=TRANSFORM_TEX(v.texcoord, _BrokenNormalMap);
				return o;
			}
 
			fixed4 frag(v2f i) : SV_Target{
 
				fixed4 packedNormal = tex2D(_BrokenNormalMap,i.uv.zw);
				fixed3 tangentNormal;
				tangentNormal=UnpackNormal(packedNormal);
				
				tangentNormal.xy*=_BrokenScale;
				float2 offset = tangentNormal.xy;
				
				fixed3 col=tex2D(_MainTex,i.uv.xy+offset).rgb;
				return fixed4(col,1.0f);
			}
			ENDCG
			}
		
		}
	FallBack "Diffuse"
}
