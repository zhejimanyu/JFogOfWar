Shader "Custom/JFogOfWar" 
{
	Properties 
	{
		[NoScaleOffset]_MainTex("Fog Texture", 2D) = "white" {}
		_UnExplored("Unexplored Color", Color) = (0.05, 0.05, 0.05, 0.05)
		_Explored("Explored Color", Color) = (0.35, 0.35, 0.35, 0.35)
		_BlendFactor("Blend Factor", range(0,1)) = 0
	}
	SubShader 
	{
			Tags{"Queue" = "Transparent+100" "IgnoreProjector" = "True" "RenderType" = "Transparent" }

			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite Off
			ZTest Off
			Cull Back

			CGINCLUDE

			#include "UnityCG.cginc"

			sampler2D _MainTex;
			uniform half4 _UnExplored;
			uniform half4 _Explored;
			half _BlendFactor;

			struct v2f
			{
				half4 pos : SV_POSITION;
				half2 uv : TEXCOORD0;
			};

			v2f vert(appdata_base v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = v.texcoord;
				return o;
			}

			fixed4 frag(v2f i) :SV_Target
			{
				half4 texColor = tex2D(_MainTex,1-i.uv);
				half fogValue = lerp(texColor.b,texColor.r,_BlendFactor);
				half4 c = lerp(_UnExplored,_Explored,fogValue);
				c.a = (1-fogValue) * c.a;//越是可见的部分越是透明（不显示遮罩网格的颜色）
				return c;
			}

			ENDCG

			Pass 
			{
				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag	
				#pragma fragmentoption ARB_precision_hint_fastest 
				ENDCG
			}
		}

		FallBack Off
}
