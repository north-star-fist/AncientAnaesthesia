#if !defined(M4B_DATA_TYPES)
#define M4B_DATA_TYPES

// For shadows routines
#include "AutoLight.cginc"

struct AdvVertexData {
	UNITY_VERTEX_INPUT_INSTANCE_ID
	float4 vertex : POSITION;
	float2 uv : TEXCOORD0;
	float3 normal : NORMAL;
	float4 tangent : TANGENT;
	// Unity puts lightmaps here
	float2 uv1 : TEXCOORD1;
	// Realtime GI lightmap UV
	float2 uv2 : TEXCOORD2;
};

struct AdvInterpolators {
	UNITY_VERTEX_INPUT_INSTANCE_ID
#if defined(LOD_FADE_CROSSFADE)
	UNITY_VPOS_TYPE vpos : VPOS;
#else
	float4 pos : SV_POSITION;
#endif
	float2 uv : TEXCOORD0;	// both UVs packed into one channel
	float3 normal : TEXCOORD1;
	float3 worldPos: TEXTCOORD2;
	UNITY_SHADOW_COORDS(3)
#if defined(BINORMAL_PER_FRAGMENT)
	float4 tangent : TEXCOORD4;
#else
	float4 tangent : TEXCOORD4;
	float3 binormal : TEXCOORD5;
#endif
#if defined(VERTEXLIGHT_ON)
	float3 vertexLightColor : TEXCOORD6;
#elif defined(LIGHTMAP_ON) || defined(DYNAMICLIGHTMAP_ON)
	float4 lightmapUV : TEXCOORD6;
#endif
};

struct AdvInterpolatorsVertex {
	UNITY_VERTEX_INPUT_INSTANCE_ID
	float4 pos : SV_POSITION;
	float2 uv : TEXCOORD0;	// both UVs packed into one channel
	float3 normal : TEXCOORD1;
	float3 worldPos: TEXTCOORD2;
	UNITY_SHADOW_COORDS(3)
#if defined(BINORMAL_PER_FRAGMENT)
		float4 tangent : TEXCOORD4;
#else
		float4 tangent : TEXCOORD4;
	float3 binormal : TEXCOORD5;
#endif
#if defined(VERTEXLIGHT_ON)
	float3 vertexLightColor : TEXCOORD6;
#elif defined(LIGHTMAP_ON) || defined(DYNAMICLIGHTMAP_ON)
	float4 lightmapUV : TEXCOORD6;
#endif
};

struct FragmentOutput {
#if defined(DEFERRED_PASS)
	float4 gBuffer0 : SV_Target0;
	float4 gBuffer1 : SV_Target1;
	float4 gBuffer2 : SV_Target2;
	float4 gBuffer3 : SV_Target3;
#if defined(SHADOWS_SHADOWMASK) && (UNITY_ALLOWED_MRT_COUNT > 4)
	float4 gBuffer4 : SV_Target4;
#endif

#else
	float4 color : SV_Target;
#endif
};

#endif