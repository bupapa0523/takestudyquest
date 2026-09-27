Shader "Study/VertexColorLit"
{
	Properties
	{
		_BaseColor("Color", Color) = (1, 1, 1, 1)
	}
	SubShader
	{
		Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
		Pass
		{
			Name "ForwardLit"
			Tags { "LightMode" = "UniversalForward" }
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
				float4 color : COLOR;
			};
			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float4 color : COLOR;
				float3 normalWS : TEXCOORD0;
			};

			CBUFFER_START(UnityPerMaterial)
				float4 _BaseColor;
			CBUFFER_END

			Varyings vert(Attributes v)
			{
				Varyings o;
				o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
				o.normalWS = TransformObjectToWorldNormal(v.normalOS);
				o.color = v.color * _BaseColor;
				return o;
			}

			half4 frag(Varyings i) : SV_Target
			{
				Light mainLight = GetMainLight();
				float ndotl = saturate(dot(normalize(i.normalWS), mainLight.direction));
				float3 lit = i.color.rgb * (ndotl * 0.7 + 0.35) * mainLight.color;
				return half4(lit, 1);
			}
			ENDHLSL
		}
	}
	FallBack Off
}
