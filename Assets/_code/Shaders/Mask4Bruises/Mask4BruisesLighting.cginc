#if !defined(M4B_LIGHTING_INCLUDED)
#define M4B_LIGHTING_INCLUDED				

#if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
	#define FOG_ON 1
	#if !defined(FOG_DISTANCE)
		#define FOG_DEPTH 1
	#endif
#endif


// common shadering stuff from Unity guys
#include "UnityCG.cginc"
// we include it through UnityStandardBRDF.cginc
//#include "UnityPBSLighting.cginc"


float4 _Color;

sampler2D _MainTex, _MetalMapTex, _NormalMapTex, _EmissionMapTex;
float4 _MainTex_ST;
float _Smoothness, _Metallic, _NormalScale;
float3 _EmissionColor;
int _SmoothnessSource;
float _Cutoff;

sampler2D _AltMaskTex;
float4 _AltColor, _AltSpecularTint;
sampler2D _AltTex, _AltNormalMapTex;
float4 _AltTex_ST;
float _AltPortion, _AltSmoothness, _AltMetallic, _AltNormalScale;
int _AltSmoothnessSource;

float4 _AltColor2, _AltSpecularTint2;
sampler2D _AltTex2, _AltNormalMapTex2;
float4 _AltTex_ST2;
float _AltPortion2, _AltSmoothness2, _AltMetallic2, _AltNormalScale2;
int _AltSmoothnessSource2;

float4 _AltColor3, _AltSpecularTint3;
sampler2D _AltTex3, _AltNormalMapTex3;
float4 _AltTex_ST3;
float _AltPortion3, _AltSmoothness3, _AltMetallic3, _AltNormalScale3;
int _AltSmoothnessSource3;

float4 _AltColor4, _AltSpecularTint4;
sampler2D _AltTex4, _AltNormalMapTex4;
float4 _AltTex_ST4;
float _AltPortion4, _AltSmoothness4, _AltMetallic4, _AltNormalScale4;
int _AltSmoothnessSource4;



// Including it after properties makes it possible to use them
#include "CommonShading.cginc"


AdvInterpolatorsVertex AdvVertex(AdvVertexData v) {
	AdvInterpolatorsVertex i;
	UNITY_INITIALIZE_OUTPUT(AdvInterpolators, i);

	float2 uvMain = TRANSFORM_TEX(v.uv, _MainTex);
	i.uv = float4(uvMain, uvMain);

	// Clip space position (camera view)
	i.pos = UnityObjectToClipPos(v.vertex);
	// World space position
	i.worldPos = mul(unity_ObjectToWorld, v.vertex);
	// transitioning from object to world space (taking scaling into account)
	i.normal = UnityObjectToWorldNormal(v.normal);
	// Transfers shadow stuff for AutoLight functions
	//TRANSFER_SHADOW(i);
	UNITY_TRANSFER_SHADOW(i, v.uv1);
	
#if defined(BINORMAL_PER_FRAGMENT)
	i.tangent = float4(UnityObjectToWorldDir(v.tangent.xyz), v.tangent.w);
#else
	// Let's add 0 because we use float4 for both precalculated binormal and not
	i.tangent = float4(UnityObjectToWorldDir(v.tangent.xyz), 0);
	i.binormal = CreateBinormal(i.normal, i.tangent, v.tangent.w);
#endif

	
#if defined(LIGHTMAP_ON)
	i.lightmapUV.xy = v.uv1 * unity_LightmapST.xy + unity_LightmapST.zw;
#endif
#if defined(DYNAMICLIGHTMAP_ON)
	i.lightmapUV.zw = v.uv2 * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
#endif
	return i;
}

FragmentOutput AdvFragment(AdvInterpolators i) {

#if defined(LOD_FADE_CROSSFADE)
	UnityApplyDitherCrossFade(i.vpos);
#endif
	
	float4 altMask4 = GetAltPortions(i);
	float altMaskSum = altMask4.x + altMask4.y + altMask4.z + altMask4.w;
	float altMask = altMaskSum / (altMaskSum + 1);

	float smoothness = GetMainSmoothness(i);
	float4 altSmoothness4 = float4(
		GetAltSmoothness(i, 0),
		GetAltSmoothness(i, 1),
		GetAltSmoothness(i, 2),
		GetAltSmoothness(i, 3)
	);
	float altSmoothness = Mix(altMask4, altSmoothness4);
	smoothness = lerp(smoothness, altSmoothness, altMask);

	float3 tsNormal = GetTSNormal(i, _NormalScale);
	float3 altTsNormal1 = GetAltTSNormal(i, 0, altMask4.x);
	float3 altTsNormal2 = GetAltTSNormal(i, 1, altMask4.y);
	float3 altTsNormal3 = GetAltTSNormal(i, 2, altMask4.z);
	float3 altTsNormal4 = GetAltTSNormal(i, 3, altMask4.w);
	float3 altTsNormal12 = BlendNormals(altTsNormal1, altTsNormal2);
	float3 altTsNormal34 = BlendNormals(altTsNormal3, altTsNormal4);
	float3 altTsNormal = BlendNormals(altTsNormal12, altTsNormal34);

	tsNormal = BlendNormals(tsNormal, altTsNormal);

	float3 specTint = 0;
	float oneMinusReflectivity;
	float3 albedo = GetAlbedo(i, altMask4, oneMinusReflectivity, specTint);
	float3 emiss = GetEmission(i);

#if defined(_RENDERING_CUTOUT)
	float alpha = GetAlpha(i);
	clip(alpha - _Cutoff);
#endif

	// tangent space transformation (into world space)
	
#if defined(BINORMAL_PER_FRAGMENT)
	i.normal = GetTangentSpaceNormal(i.normal, tsNormal, i.tangent, 0);
#else
	i.normal = GetTangentSpaceNormal(i.normal, tsNormal, i.tangent, i.binormal);
#endif


	//// Lighting

	// View direction is not actually view direction but direction of light going to viewer
	float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);

	UnityLight light = CreateUnityLight(i, 1);
	float3 vlCol = 0;	
#if defined(VERTEXLIGHT_ON)
	vlCol = i.vertexLightColor;
#endif

	
	UnityIndirect indirectLight = CreateIndirectLight(
		i,
		vlCol,
		viewDir,
		smoothness,
		1
#if defined(DIRLIGHTMAP_COMBINED)
		, i.lightmapUV
#endif
	);
	
	
	float4 noEmissive = UNITY_BRDF_PBS(
		albedo,
		specTint,
		oneMinusReflectivity,
		smoothness,
		i.normal,
		viewDir,
		light,
		indirectLight
	);
	
	float4 finalCol = noEmissive + float4(emiss, noEmissive.a);
	
	FragmentOutput output;
	
#if defined(DEFERRED_PASS)
	#if !defined(UNITY_HDR_ON)
		finalCol.rgb = exp2(-finalCol.rgb);
	#endif

	output.gBuffer0.rgb = albedo;
	output.gBuffer0.a = 1;

	output.gBuffer1.rgb = specTint;
	output.gBuffer1.a = smoothness;

	output.gBuffer2 = float4(i.normal * 0.5 + 0.5, 1);

	output.gBuffer3 = finalCol;

	#if defined(SHADOWS_SHADOWMASK) && (UNITY_ALLOWED_MRT_COUNT > 4)
		float2 shadowUV = 0;
		#if defined(LIGHTMAP_ON)
		shadowUV = i.lightmapUV;
		#endif
		output.gBuffer4 = UnityGetRawBakedOcclusions(shadowUV, i.worldPos.xyz);
	#endif
#else
	output.color = ApplyFog(finalCol, i);
#endif
	return output;
}
#endif	// conditional include end