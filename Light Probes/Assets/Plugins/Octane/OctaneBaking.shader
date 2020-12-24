Shader "Octane/Bake Result"
{
	Properties
	{
		[PerRendererData]
		_BakeTex ("Texture", 2D) = "white" {}
		[PerRendererData]
		_BakeUvChannel ("Channel", int) = 1
	}
	
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float2 uv1 : TEXCOORD1;
				float2 uv2 : TEXCOORD2;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float2 uv1 : TEXCOORD1;
				float2 uv2 : TEXCOORD2;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
			};

			sampler2D _BakeTex;
			float4 _BakeTex_ST;
			int _BakeUvChannel;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _BakeTex);
				o.uv1 = TRANSFORM_TEX(v.uv1, _BakeTex);
				o.uv2 = TRANSFORM_TEX(v.uv2, _BakeTex);
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = tex2D(_BakeTex, i.uv);
				if(_BakeUvChannel == 1)
					col = tex2D(_BakeTex, i.uv1);
				if(_BakeUvChannel == 2)
					col = tex2D(_BakeTex, i.uv2);
				// apply fog
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
		}
	}
}
