Shader "Polytope Studio/ PT_Medieval Weapons Shader PBR"
{
	Properties
	{
		[HDR]_METAL1COLOR("METAL 1 COLOR", Color) = (0.7261481,0.7735849,0.7528313,0)
		_METAL1METALLIC("METAL 1 METALLIC", Range( 0 , 1)) = 0.65
		_METAL1SMOOTHNESS("METAL 1 SMOOTHNESS", Range( 0 , 1)) = 0.7
		[HDR]_METAL2COLOR("METAL 2 COLOR", Color) = (1.678431,1.003922,0.1176471,0)
		_METAL2METALLIC("METAL 2 METALLIC", Range( 0 , 1)) = 0.65
		_METAL2SMOOTHNESS("METAL 2 SMOOTHNESS", Range( 0 , 1)) = 0.7
		[HDR]_METAL3COLOR("METAL 3 COLOR", Color) = (0.597023,0.6237553,0.7395956,0)
		_METAL3METALLIC("METAL 3 METALLIC", Range( 0 , 1)) = 0.65
		_METAL3SMOOTHNESS("METAL 3 SMOOTHNESS", Range( 0 , 1)) = 0.7
		[HDR]_METAL4COLOR("METAL 4 COLOR", Color) = (0.8791043,0.8044721,0.9422547,0)
		_METAL4METALLIC("METAL 4 METALLIC", Range( 0 , 1)) = 0.65
		_METAL4SMOOTNESS("METAL 4 SMOOTNESS", Range( 0 , 1)) = 0.7
		[HDR]_WOOD1COLOR("WOOD 1 COLOR", Color) = (0.1981132,0.08345769,0.06261124,0)
		_WOOD1SMOOTHNESS("WOOD 1 SMOOTHNESS", Range( 0 , 1)) = 0.3
		[HDR]_WOOD2COLOR("WOOD 2 COLOR", Color) = (0.1320755,0.06452555,0.05420079,0)
		_WOOD2SMOOTHNESS("WOOD 2 SMOOTHNESS", Range( 0 , 1)) = 0.3
		[HDR]_WOOD3COLOR("WOOD 3 COLOR", Color) = (0.1037736,0.07509367,0.04650232,0)
		_WOOD3SMOOTHNESS("WOOD 3 SMOOTHNESS", Range( 0 , 1)) = 0.3
		[HDR]_LEATHER1COLOR("LEATHER 1 COLOR", Color) = (0.2924528,0.1296404,0.09242612,1)
		_LEATHER1SMOOTHNESS("LEATHER 1 SMOOTHNESS", Range( 0 , 1)) = 0.3
		[HDR]_LEATHER2COLOR("LEATHER 2 COLOR", Color) = (0.06603771,0.03523636,0.03146137,1)
		_LEATHER2SMOOTHNESS("LEATHER 2 SMOOTHNESS", Range( 0 , 1)) = 0.3
		[HDR]_LEATHER3COLOR("LEATHER 3 COLOR", Color) = (0.1320755,0.03139969,0.02180491,1)
		_LEATHER3SMOOTHNESS("LEATHER 3 SMOOTHNESS", Range( 0 , 1)) = 0.3
		[HDR]_PAINT1COLOR("PAINT 1 COLOR", Color) = (0.5450981,0.6936808,0.6980392,0)
		_PAINT1SMOOTHNESS("PAINT 1 SMOOTHNESS", Range( 0 , 1)) = 1
		[HDR]_PAINT2COLOR("PAINT 2 COLOR", Color) = (0.3649431,0.5566038,0.4386422,0)
		_PAINT2SMOOTHNESS("PAINT 2 SMOOTHNESS", Range( 0 , 1)) = 0
		[HDR]_PAINT3COLOR("PAINT 3 COLOR", Color) = (0.5849056,0.5418971,0.4331613,0)
		_PAINT3SMOOTHNESS("PAINT 3 SMOOTHNESS", Range( 0 , 1)) = 0
		[HDR]_GEMS1COLOR("GEMS 1 COLOR", Color) = (1,0,0,0)
		_GEMS1SMOOTHNESS("GEMS 1 SMOOTHNESS", Range( 0 , 1)) = 0.5
		[HDR]_GEMS2COLOR("GEMS 2 COLOR", Color) = (0,0.3218706,0.5754717,0)
		_GEMS2SMOOTHNESS("GEMS 2 SMOOTHNESS", Range( 0 , 1)) = 0.4
		[HDR]_GEMS3COLOR("GEMS 3 COLOR", Color) = (0,0.4716981,0.1359325,0)
		_GEMS3SMOOTHNESS("GEMS 3 SMOOTHNESS", Range( 0 , 1)) = 0.3
		[HDR]_FEATHERS1COLOR("FEATHERS 1 COLOR", Color) = (0.3301887,0.1241556,0.04516733,0)
		[HDR]_FEATHERS2COLOR("FEATHERS 2 COLOR", Color) = (0.509434,0.4260285,0.1802243,0)
		[HDR]_FEATHERS3COLOR("FEATHERS 3 COLOR", Color) = (0.509434,0.25712,0.25712,0)
		[HDR]_FEATHERS4COLOR("FEATHERS 4 COLOR", Color) = (0.8113208,0.2104842,0.2104842,0)
		[HDR]_FEATHERS5COLOR("FEATHERS 5 COLOR", Color) = (0.4150943,0.2615769,0.1468494,0)
		[HDR]_FEATHERS6COLOR("FEATHERS 6 COLOR", Color) = (0.7924528,0.7444169,0.6391954,0)
		[HDR]_COATOFARMSCOLOR("COAT OF ARMS COLOR", Color) = (1,0,0,0)
		[NoScaleOffset]_COATOFARMSMASK("COAT OF ARMS MASK", 2D) = "black" {}
		_OCCLUSION("OCCLUSION", Range( 0 , 1)) = 0.4139509
		[Toggle]_MetalicOn("Metalic On", Float) = 1
		[Toggle]_SmoothnessOn("Smoothness On", Float) = 1
		[HideInInspector]_TextureSample2("Texture Sample 2", 2D) = "white" {}
		[HideInInspector]_TextureSample9("Texture Sample 9", 2D) = "white" {}
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

			TEXTURE2D(_TextureSample2); SAMPLER(sampler_TextureSample2);
			TEXTURE2D(_TextureSample9); SAMPLER(sampler_TextureSample9);

			CBUFFER_START(UnityPerMaterial)
				float4 _METAL1COLOR, _METAL2COLOR, _METAL3COLOR, _METAL4COLOR;
				float4 _WOOD1COLOR, _WOOD2COLOR, _WOOD3COLOR;
				float4 _LEATHER1COLOR, _LEATHER2COLOR, _LEATHER3COLOR;
				float4 _PAINT1COLOR, _PAINT2COLOR, _PAINT3COLOR;
				float4 _GEMS1COLOR, _GEMS2COLOR, _GEMS3COLOR;
				float4 _FEATHERS1COLOR, _FEATHERS2COLOR, _FEATHERS3COLOR;
				float4 _FEATHERS4COLOR, _FEATHERS5COLOR, _FEATHERS6COLOR;
			CBUFFER_END

			float Hit(float3 mask, float3 id)
			{
				return distance(mask, id) < 0.18 ? 1.0 : 0.0;
			}

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
				float4 baseTex = SAMPLE_TEXTURE2D(_TextureSample2, sampler_TextureSample2, i.uv);
				float3 mask = SAMPLE_TEXTURE2D(_TextureSample9, sampler_TextureSample9, i.uv).rgb;

				float4 col = baseTex * 0.25;
				col = Lay(col, baseTex, _PAINT3COLOR, Hit(mask, float3(1, 0.498, 0.498)));
				col = Lay(col, baseTex, _PAINT2COLOR, Hit(mask, float3(0.498, 0.498, 0.498)));
				col = Lay(col, baseTex, _PAINT1COLOR, Hit(mask, float3(0.498, 0.498, 0)));
				col = Lay(col, baseTex, _FEATHERS6COLOR, Hit(mask, float3(0, 0.498, 0)));
				col = Lay(col, baseTex, _FEATHERS5COLOR, Hit(mask, float3(0, 0, 0)));
				col = Lay(col, baseTex, _FEATHERS4COLOR, Hit(mask, float3(1, 1, 0)));
				col = Lay(col, baseTex, _FEATHERS3COLOR, Hit(mask, float3(0.498, 0, 0)));
				col = Lay(col, baseTex, _FEATHERS2COLOR, Hit(mask, float3(1, 0.498, 0)));
				col = Lay(col, baseTex, _FEATHERS1COLOR, Hit(mask, float3(1, 0, 0)));
				col = Lay(col, baseTex, _WOOD3COLOR, Hit(mask, float3(0, 0, 1)));
				col = Lay(col, baseTex, _WOOD2COLOR, Hit(mask, float3(0, 1, 1)));
				col = Lay(col, baseTex, _WOOD1COLOR, Hit(mask, float3(0, 1, 0)));
				col = Lay(col, baseTex, _LEATHER3COLOR, Hit(mask, float3(1, 0.498, 1)));
				col = Lay(col, baseTex, _LEATHER2COLOR, Hit(mask, float3(1, 0, 1)));
				col = Lay(col, baseTex, _LEATHER1COLOR, Hit(mask, float3(1, 1, 0.498)));
				col = Lay(col, baseTex, _METAL4COLOR, Hit(mask, float3(0.498, 0.498, 1)));
				col = Lay(col, baseTex, _METAL3COLOR, Hit(mask, float3(0, 0.498, 0.498)));
				col = Lay(col, baseTex, _METAL2COLOR, Hit(mask, float3(0, 0, 0.498)));
				col = Lay(col, baseTex, _METAL1COLOR, Hit(mask, float3(0.498, 0, 0.498)));
				col = Lay(col, baseTex, _GEMS3COLOR, Hit(mask, float3(0.498, 1, 1)));
				col = Lay(col, baseTex, _GEMS2COLOR, Hit(mask, float3(0.498, 1, 0.498)));
				col = Lay(col, baseTex, _GEMS1COLOR, Hit(mask, float3(0.498, 0, 1)));

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
