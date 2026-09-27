Shader "Polytope Studio/ PT_Medieval Armors Shader PBR"
{
	Properties
	{
		[HDR] _SKINCOLOR("SKIN COLOR", Color) = (2.02193, 1.0081, 0.6199315, 0)
		_SKINSMOOTHNESS("SKIN SMOOTHNESS", Range(0, 1)) = 0.3
		[HDR] _EYESCOLOR("EYES COLOR", Color) = (0.0734529, 0.1320755, 0.05046281, 1)
		_EYESSMOOTHNESS("EYES SMOOTHNESS", Range(0, 1)) = 0.7
		[HDR] _HAIRCOLOR("HAIR COLOR", Color) = (0.5943396, 0.3518379, 0.1093361, 0)
		_HAIRSMOOTHNESS("HAIR SMOOTHNESS", Range(0, 1)) = 0.1
		[HDR] _SCLERACOLOR("SCLERA COLOR", Color) = (0.9056604, 0.8159487, 0.8159487, 0)
		_SCLERASMOOTHNESS("SCLERA SMOOTHNESS", Range(0, 1)) = 0.5
		[HDR] _LIPSCOLOR("LIPS COLOR", Color) = (0.8301887, 0.3185886, 0.2780349, 0)
		_LIPSSMOOTHNESS("LIPS SMOOTHNESS", Range(0, 1)) = 0.4
		[HDR] _SCARSCOLOR("SCARS COLOR", Color) = (0.8490566, 0.5037117, 0.3884835, 0)
		_SCARSSMOOTHNESS("SCARS SMOOTHNESS", Range(0, 1)) = 0.3
		[HDR] _METAL1COLOR("METAL 1 COLOR", Color) = (2, 0.682353, 0.1960784, 0)
		_METAL1METALLIC("METAL 1 METALLIC", Range(0, 1)) = 0.65
		_METAL1SMOOTHNESS("METAL 1 SMOOTHNESS", Range(0, 1)) = 0.7
		[HDR] _METAL2COLOR("METAL 2 COLOR", Color) = (0.4674706, 0.4677705, 0.5188679, 0)
		_METAL2METALLIC("METAL 2 METALLIC", Range(0, 1)) = 0.65
		_METAL2SMOOTHNESS("METAL 2 SMOOTHNESS", Range(0, 1)) = 0.7
		[HDR] _METAL3COLOR("METAL 3 COLOR", Color) = (0.4383232, 0.4383232, 0.4716981, 0)
		_METAL3METALLIC("METAL 3 METALLIC", Range(0, 1)) = 0.65
		_METAL3SMOOTHNESS("METAL 3 SMOOTHNESS", Range(0, 1)) = 0.7
		[HDR] _LEATHER1COLOR("LEATHER 1 COLOR", Color) = (0.4811321, 0.2041155, 0.08851016, 1)
		_LEATHER1SMOOTHNESS("LEATHER 1 SMOOTHNESS", Range(0, 1)) = 0.3
		[HDR] _LEATHER2COLOR("LEATHER 2 COLOR", Color) = (0.4245283, 0.190437, 0.09011215, 1)
		_LEATHER2SMOOTHNESS("LEATHER 2 SMOOTHNESS", Range(0, 1)) = 0.3
		[HDR] _LEATHER3COLOR("LEATHER 3 COLOR", Color) = (0.1698113, 0.04637412, 0.02963688, 1)
		_LEATHER3SMOOTHNESS("LEATHER 3 SMOOTHNESS", Range(0, 1)) = 0.3
		[HDR] _CLOTH1COLOR("CLOTH 1 COLOR", Color) = (0.1465379, 0.282117, 0.3490566, 0)
		[HDR] _CLOTH2COLOR("CLOTH 2 COLOR", Color) = (1, 0, 0, 0)
		[HDR] _CLOTH3COLOR("CLOTH 3 COLOR", Color) = (0.8773585, 0.6337318, 0.3434941, 0)
		[HDR] _GEMS1COLOR("GEMS 1 COLOR", Color) = (0.3773585, 0, 0.06650025, 0)
		_GEMS1SMOOTHNESS("GEMS 1 SMOOTHNESS", Range(0, 1)) = 1
		[HDR] _GEMS2COLOR("GEMS 2 COLOR", Color) = (0.2023368, 0, 0.4339623, 0)
		_GEMS2SMOOTHNESS("GEMS 2 SMOOTHNESS", Range(0, 1)) = 0
		[HDR] _GEMS3COLOR("GEMS 3 COLOR", Color) = (0, 0.1132075, 0.01206957, 0)
		_GEMS3SMOOTHNESS("GEMS 3 SMOOTHNESS", Range(0, 1)) = 0
		[HDR] _FEATHERS1COLOR("FEATHERS 1 COLOR", Color) = (0.7735849, 0.492613, 0.492613, 0)
		[HDR] _FEATHERS2COLOR("FEATHERS 2 COLOR", Color) = (0.6792453, 0, 0, 0)
		[HDR] _FEATHERS3COLOR("FEATHERS 3 COLOR", Color) = (0, 0.1793142, 0.7264151, 0)
		[HDR] _CUSTOM1COLOR("CUSTOM 1 COLOR", Color) = (0.0734529, 0.1320755, 0.05046281, 1)
		_CUSTOM1SMOOTHNESS("CUSTOM 1 SMOOTHNESS", Range(0, 1)) = 0.7
		[HDR] _CUSTOM2COLOR("CUSTOM 2 COLOR", Color) = (0.5943396, 0.3518379, 0.1093361, 0)
		_CUSTOM2SMOOTHNESS("CUSTOM 2 SMOOTHNESS", Range(0, 1)) = 0.1
		[HDR] _CUSTOM3COLOR("CUSTOM 3 COLOR", Color) = (2.02193, 1.0081, 0.6199315, 0)
		_CUSTOM3SMOOTHNESS("CUSTOM 3 SMOOTHNESS", Range(0, 1)) = 0.3
		[HideInInspector] _Texture0("Texture 0", 2D) = "white" {}
		[HideInInspector] _Texture8("Texture 0", 2D) = "white" {}
		[HideInInspector] _Texture1("Texture 1", 2D) = "white" {}
		[HideInInspector] _Texture6("Texture 6", 2D) = "white" {}
		[HideInInspector] _Texture3("Texture 3", 2D) = "white" {}
		[HideInInspector] _Texture5("Texture 5", 2D) = "white" {}
		[HideInInspector][HDR] _Texture2("Texture 2", 2D) = "white" {}
		[HideInInspector] _Texture4("Texture 4", 2D) = "white" {}
		[HideInInspector] _Texture7("Texture 7", 2D) = "white" {}
		[HDR] _COATOFARMSCOLOR("COAT OF ARMS COLOR", Color) = (1, 0, 0, 0)
		[NoScaleOffset] _COATOFARMSMASK("COAT OF ARMS MASK", 2D) = "black" {}
		_OCCLUSION("OCCLUSION", Range(0, 1)) = 0.5
		[Toggle] _MetalicOn("Metalic On", Float) = 1
		[Toggle] _SmoothnessOn("Smoothness On", Float) = 1
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
				float2 uv : TEXCOORD0;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 normalWS : TEXCOORD1;
			};

			TEXTURE2D(_Texture0); SAMPLER(sampler_Texture0);
			TEXTURE2D(_Texture1); SAMPLER(sampler_Texture1);
			TEXTURE2D(_Texture2); SAMPLER(sampler_Texture2);
			TEXTURE2D(_Texture3); SAMPLER(sampler_Texture3);
			TEXTURE2D(_Texture4); SAMPLER(sampler_Texture4);
			TEXTURE2D(_Texture5); SAMPLER(sampler_Texture5);
			TEXTURE2D(_Texture6); SAMPLER(sampler_Texture6);
			TEXTURE2D(_Texture7); SAMPLER(sampler_Texture7);
			TEXTURE2D(_Texture8); SAMPLER(sampler_Texture8);

			CBUFFER_START(UnityPerMaterial)
				float4 _SKINCOLOR, _EYESCOLOR, _HAIRCOLOR, _SCLERACOLOR, _LIPSCOLOR, _SCARSCOLOR;
				float4 _METAL1COLOR, _METAL2COLOR, _METAL3COLOR;
				float4 _LEATHER1COLOR, _LEATHER2COLOR, _LEATHER3COLOR;
				float4 _CLOTH1COLOR, _CLOTH2COLOR, _CLOTH3COLOR;
				float4 _GEMS1COLOR, _GEMS2COLOR, _GEMS3COLOR;
				float4 _FEATHERS1COLOR, _FEATHERS2COLOR, _FEATHERS3COLOR;
				float4 _CUSTOM1COLOR, _CUSTOM2COLOR, _CUSTOM3COLOR;
			CBUFFER_END

			// Polytope ID maps: a channel near 0 (cyan/magenta/yellow squares) marks that region.
			float Id(float c) { return c < 0.2 ? 1.0 : 0.0; }

			float4 Lay(float4 cur, float4 baseTex, float4 tint, float m)
			{
				return lerp(cur, baseTex * tint, m);
			}

			Varyings vert(Attributes v)
			{
				Varyings o;
				o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
				o.normalWS = TransformObjectToWorldNormal(v.normalOS);
				o.uv = v.uv;
				return o;
			}

			half4 frag(Varyings i) : SV_Target
			{
				float4 baseTex = SAMPLE_TEXTURE2D(_Texture2, sampler_Texture2, i.uv);
				float4 gems = SAMPLE_TEXTURE2D(_Texture7, sampler_Texture7, i.uv);
				float4 feathers = SAMPLE_TEXTURE2D(_Texture4, sampler_Texture4, i.uv);
				float4 cloth = SAMPLE_TEXTURE2D(_Texture5, sampler_Texture5, i.uv);
				float4 leather = SAMPLE_TEXTURE2D(_Texture3, sampler_Texture3, i.uv);
				float4 metal = SAMPLE_TEXTURE2D(_Texture6, sampler_Texture6, i.uv);
				float4 face = SAMPLE_TEXTURE2D(_Texture1, sampler_Texture1, i.uv);
				float4 skin = SAMPLE_TEXTURE2D(_Texture0, sampler_Texture0, i.uv);
				float4 custom = SAMPLE_TEXTURE2D(_Texture8, sampler_Texture8, i.uv);

				float4 col = baseTex * 0.2;
				col = Lay(col, baseTex, _GEMS3COLOR, Id(gems.b));
				col = Lay(col, baseTex, _GEMS2COLOR, Id(gems.g));
				col = Lay(col, baseTex, _GEMS1COLOR, Id(gems.r));
				col = Lay(col, baseTex, _FEATHERS3COLOR, Id(feathers.b));
				col = Lay(col, baseTex, _FEATHERS2COLOR, Id(feathers.g));
				col = Lay(col, baseTex, _FEATHERS1COLOR, Id(feathers.r));
				col = Lay(col, baseTex, _CLOTH3COLOR, Id(cloth.b));
				col = Lay(col, baseTex, _CLOTH2COLOR, Id(cloth.g));
				col = Lay(col, baseTex, _CLOTH1COLOR, Id(cloth.r));
				col = Lay(col, baseTex, _LEATHER3COLOR, Id(leather.b));
				col = Lay(col, baseTex, _LEATHER2COLOR, Id(leather.g));
				col = Lay(col, baseTex, _LEATHER1COLOR, Id(leather.r));
				col = Lay(col, baseTex, _METAL3COLOR, Id(metal.b));
				col = Lay(col, baseTex, _METAL2COLOR, Id(metal.g));
				col = Lay(col, baseTex, _METAL1COLOR, Id(metal.r));
				col = Lay(col, baseTex, _SCARSCOLOR, Id(face.b));
				col = Lay(col, baseTex, _LIPSCOLOR, Id(face.g));
				col = Lay(col, baseTex, _SCLERACOLOR, Id(face.r));
				col = Lay(col, baseTex, _EYESCOLOR, Id(skin.b));
				col = Lay(col, baseTex, _HAIRCOLOR, Id(skin.g));
				col = Lay(col, baseTex, _SKINCOLOR, Id(skin.r));
				col = Lay(col, baseTex, _CUSTOM1COLOR, Id(custom.b));
				col = Lay(col, baseTex, _CUSTOM2COLOR, Id(custom.g));
				col = Lay(col, baseTex, _CUSTOM3COLOR, Id(custom.r));

				Light mainLight = GetMainLight();
				float ndotl = saturate(dot(normalize(i.normalWS), mainLight.direction));
				float3 lit = col.rgb * (ndotl * 0.7 + 0.35) * mainLight.color;
				return half4(lit, 1);
			}
			ENDHLSL
		}
	}
	FallBack Off
}
