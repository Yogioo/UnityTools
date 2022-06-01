Shader "Yogi/RenderImage/ScreenLerpMask" {
	Properties {
		_MainTex ("Main Tex", 2D) = "white" {}
		_BlendMap("BlendMap Map",2D)="black"{}
		_BlendScale("BlendScale",Range(0,1))=1.0
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
			sampler2D _BlendMap;
			float4 _BlendMap_ST;
			float _BlendScale;
 
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
				o.uv.zw=TRANSFORM_TEX(v.texcoord, _BlendMap);
				return o;
			}
 
			fixed4 frag(v2f i) : SV_Target{
 
				fixed4 blendMap = tex2D(_BlendMap,i.uv.zw).rgba;
				fixed4 col=tex2D(_MainTex,i.uv.xy).rgba;

				// Overlay
				//(col > 0.5) * (1 - (1-2*(col-0.5)) * (1-blendMap)) + (col <= 0.5) * ((2*col) * blendMap);
				// Lighten
				//max(col,blendMap);
				// Hard Light
					fixed4 target = col * blendMap;
				col = lerp(col, target, _BlendScale);
				return fixed4(col.rgb,1.0f);
			}
			ENDCG
			}
		
		}
	FallBack "Diffuse"
}
