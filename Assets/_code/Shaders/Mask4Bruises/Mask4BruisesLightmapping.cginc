#if !defined(M4B_LIGHTMAPPING_INCLUDED)
#define M4B_LIGHTMAPPING_INCLUDED

#include "UnityPBSLighting.cginc"
// Some Unity functions useful for lightmapping
#include "UnityMetaPass.cginc"
#include "Util.cginc"

float4 _Color;
sampler2D _MainTex, _MetalMapTex, _EmissionMapTex;
float4 _MainTex_ST;
float _Smoothness, _Metallic, _NormalScale;
float3 _EmissionColor;
int _SmoothnessSource;
float _Cutoff;

float4 _AltColor, _AltSpecularTint;
sampler2D _AltMaskTex, _AltTex, _AltSpecMapTex, _AltMetalMapTex;
float4 _AltTex_ST;
float _AltPortion, _AltSmoothness, _AltMetallic, _AltNormalScale;
int _AltSmoothnessSource;

float4 _AltColor2, _AltSpecularTint2;
sampler2D _AltMaskTex2, _AltTex2, _AltSpecMapTex2, _AltMetalMapTex2;
float4 _AltTex_ST2;
float _AltPortion2, _AltSmoothness2, _AltMetallic2, _AltNormalScale2;
int _AltSmoothnessSource2;

float4 _AltColor3, _AltSpecularTint3;
sampler2D _AltMaskTex3, _AltTex3, _AltSpecMapTex3, _AltMetalMapTex3;
float4 _AltTex_ST3;
float _AltPortion3, _AltSmoothness3, _AltMetallic3, _AltNormalScale3;
int _AltSmoothnessSource3;

float4 _AltColor4, _AltSpecularTint4;
sampler2D _AltMaskTex4, _AltTex4, _AltSpecMapTex4, _AltMetalMapTex4;
float4 _AltTex_ST4;
float _AltPortion4, _AltSmoothness4, _AltMetallic4, _AltNormalScale4;
int _AltSmoothnessSource4;


struct VertexData {
	float4 vertex : POSITION;
	float2 uv : TEXCOORD0;
	// Unity puts lightmaps here
	float2 uv1 : TEXCOORD1;
	// Realtime GI lightmap UV
	float2 uv2 : TEXCOORD2;
};

struct AdvInterpolators {
	float4 pos : SV_POSITION;
	float2 uv : TEXCOORD0;
#if defined(DYNAMICLIGHTMAP_ON)
	float2 dynamicLightmapUV : TEXCOORD6;
#endif
};

float GetMainMetallic(float2 uv = 0) {
	return tex2D(_MetalMapTex, uv).r * _Metallic;
}

float GetAltMetallic(float2 uv = 0, int channel = 0) {
	if (channel == 0) {
		return tex2D(_AltMetalMapTex, uv).r * _AltMetallic;
	}
	else if (channel == 1) {
		return tex2D(_AltMetalMapTex2, uv).r * _AltMetallic2;
	}
	else if (channel == 2) {
		return tex2D(_AltMetalMapTex3, uv).r * _AltMetallic3;
	}
	else if (channel == 3) {
		return tex2D(_AltMetalMapTex4, uv).r * _AltMetallic4;
	}
	return 0;
}

float3 GetDiffuseAndSpecular(
	float3 plainColor,
	inout float3 specularTint,
	float metallic,
	inout float oneMinusReflectivity
) {
	/// Energy conservation for reflections
	return DiffuseAndSpecularFromMetallic(
		plainColor.rgb, metallic, specularTint, oneMinusReflectivity
	);
}

float3 SampleMainEmissionTex(AdvInterpolators i) {
#if defined(_EMISSION_MAP)
	return tex2D(_EmissionMapTex, i.uv);
#else
	return 0;
#endif
}

float3 GetMainEmission(AdvInterpolators i, float3 emissionTint) {
#if defined(_EMISSION_MAP)
	return SampleMainEmissionTex(i)* emissionTint;
#else
	return emissionTint;
#endif
}

float3 GetEmission(AdvInterpolators i, float altMask = 0) {
	return GetMainEmission(i, _EmissionColor);
}



// Albedo
float3 GetMainAlbedo(AdvInterpolators i, inout float oneMinusReflectivity, inout float3 specTint) {
	float metallic = GetMainMetallic(i.uv);

	float4 plainColor = tex2D(_MainTex, i.uv) * _Color;
	plainColor = float4(plainColor.rgb, plainColor.a);
	// Specularity
	return GetDiffuseAndSpecular(plainColor.rgb, specTint, metallic, oneMinusReflectivity);
}

float3 GetAltAlbedo(
	AdvInterpolators i,
	inout float altOneMinusReflectivity,
	inout float3 altSpecTint,
	int channel
) {
	float altMetallic = GetAltMetallic(i.uv, channel);

	float3 altColor = 1;

	float3 color = _AltColor;
	if (channel == 0) {
		color = _AltColor;
	}
	else if (channel == 1) {
		color = _AltColor2;
	}
	else if (channel == 2) {
		color = _AltColor3;
	}
	else if (channel == 3) {
		color = _AltColor4;
	}
	sampler2D tex = _AltTex;
	if (channel == 0) {
		tex = _AltTex;
	}
	else if (channel == 1) {
		tex = _AltTex2;
	}
	else if (channel == 2) {
		tex = _AltTex3;
	}
	else if (channel == 3) {
		tex = _AltTex4;
	}

	altColor = color.rgb;
	altColor *= tex2D(tex, i.uv);
	// Applying Specularity
	return GetDiffuseAndSpecular(altColor, altSpecTint, altMetallic, altOneMinusReflectivity);
}

float3 GetAlbedo(
	AdvInterpolators i,
	float4 altMask,
	inout float oneMinusReflectivity,
	inout float3 specTint
) {
	float3 altSpecTint1 = 0;
	float3 altSpecTint2 = 0;
	float3 altSpecTint3 = 0;
	float3 altSpecTint4 = 0;
	float altOneMinusReflectivity1;
	float altOneMinusReflectivity2;
	float altOneMinusReflectivity3;
	float altOneMinusReflectivity4;

	float3 mainAlbedo = GetMainAlbedo(i, oneMinusReflectivity, specTint);

	float3 altAlbedo1 = GetAltAlbedo(i, altOneMinusReflectivity1, altSpecTint1, 0);
	float3 altAlbedo2 = GetAltAlbedo(i, altOneMinusReflectivity2, altSpecTint2, 1);
	float3 altAlbedo3 = GetAltAlbedo(i, altOneMinusReflectivity3, altSpecTint3, 2);
	float3 altAlbedo4 = GetAltAlbedo(i, altOneMinusReflectivity4, altSpecTint4, 3);

	float3 altAlbedo = MixColors(altMask, altAlbedo1, altAlbedo2, altAlbedo3, altAlbedo4);
	float altOneMinusReflectivity = Mix(
		altMask,
		float4(altOneMinusReflectivity1, altOneMinusReflectivity2, altOneMinusReflectivity3, altOneMinusReflectivity4)
	);
	float3 altSpecTint = Mix(altMask, altSpecTint1, altSpecTint2, altSpecTint3, altSpecTint4);

	float altTotal = altMask.x + altMask.y + altMask.z + altMask.w;
	float altPart = altTotal / altTotal + 1;

	oneMinusReflectivity = saturate(lerp(oneMinusReflectivity, altOneMinusReflectivity, altPart));
	specTint = lerp(specTint, altSpecTint, altPart);

	return MixColors(altPart, mainAlbedo, altAlbedo);
}



AdvInterpolators AdvLightmappingVertex(VertexData v) {
	AdvInterpolators i;
	// We are rendering lightmap texture not a camera, so
	// we have to use the lightmap coordinates instead of the vertex position
	i.pos = UnityMetaVertexPosition(
		v.vertex, v.uv1, v.uv2, unity_LightmapST, unity_DynamicLightmapST
	);

	i.uv = TRANSFORM_TEX(v.uv, _MainTex);

#if defined(DYNAMICLIGHTMAP_ON)
	float2 dynamicLightmapUV : TEXCOORD7;
#endif
	return i;
}

float4 AdvLightmappingFragment(AdvInterpolators i) : SV_TARGET{
	// Stuff from UnityMetaPass.cginc
	UnityMetaInput surfaceData;
	float3 specTint;
	float oneMinusReflectivity;

	float altMask = 0;
	float4 altMask4 = tex2D(_AltMaskTex, i.uv) * float4(_AltPortion, _AltPortion2, _AltPortion3, _AltPortion4);

	surfaceData.Albedo = GetAlbedo(i, altMask4, oneMinusReflectivity, surfaceData.SpecularColor);
	surfaceData.Emission = GetEmission(i);
	return UnityMetaFragment(surfaceData);
}

#endif