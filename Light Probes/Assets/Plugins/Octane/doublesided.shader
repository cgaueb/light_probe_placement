Shader "Hidden/Octane/DoubleSided"
{
	Properties
	{
		_Color ("Main Color", Color) = (0.7,0.7,0.7,0.5)
		_MainTex ("Base (RGB)", 2D) = "white" {}
	}
	SubShader
	{    
		//UsePass "Self-Illumin/VertexLit/BASE"
		//UsePass "Bumped Diffuse/PPL"

		// Ambient pass
		Pass
		{
			Blend SrcAlpha OneMinusSrcAlpha
			Name "BASE"
			Tags { "LightMode"="Always" "Queue" = "Transparent" "RenderType"="Transparent" }
			Color [_PPLAmbient]
			SetTexture [_BumpMap]
			{
				constantColor [_Color]//constantColor (1,1,1)
				combine constant lerp (texture) previous
			}
			SetTexture [_MainTex]
			{
				constantColor [_Color]
				Combine texture * previous DOUBLE, texture*constant
			}
		}

		// Vertex lights
		Pass
		{
			Blend SrcAlpha OneMinusSrcAlpha
			Name "BASE"
			Tags { "LightMode"="Vertex" "Queue" = "Transparent" "RenderType"="Transparent" }
			Material
			{
				Diffuse [_Color]
				Emission [_PPLAmbient]
				Shininess [_Shininess]
				Specular [_SpecColor]
			}
			SeparateSpecular On
			Lighting On
			Cull Off
			SetTexture [_BumpMap]
			{
				constantColor [_Color]
				combine constant lerp (texture) previous
			}
			SetTexture [_MainTex]
			{
				Combine texture * previous DOUBLE, texture*primary
			}
		}
	}
	FallBack "Diffuse", 0.5
}