// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

/// Shadow casting

#if !defined(M4B_SHADOWS_INCLUDED)
#define M4B_SHADOWS_INCLUDED

#include "UnityCG.cginc"
#include "Util.cginc"


float4 _Color;
sampler2D _MainTex;
float4 _MainTex_ST;
float _Cutoff;


// Unity's dither texture
sampler3D _DitherMaskLOD;


#if defined(_RENDERING_CUTOUT)
	#if !defined(_SMOOTHNESS_ALBEDO)
	#define SHADOWS_NEED_UV 1
	#endif
#endif



// Data types
struct AdvShadowVertexData {
	UNITY_VERTEX_INPUT_INSTANCE_ID
	float4 vertex : POSITION;
	float3 normal : NORMAL;
	float2 uv : TEXCOORD0;
};


struct AdvShadowInterpolatorsVertex {
	float4 pos : SV_POSITION;
#if SHADOWS_NEED_UV
	float2 uv : TEXCOORD0;
#endif
#if defined(SHADOWS_CUBE)
	// Cube map shadows for point lights
	float3 lightVec : TEXCOORD1;
#endif
#if defined(_DISTANCE_FADING)
	float3 worldPos : TEXCOORD2;
#endif
};

struct AdvShadowInterpolators {
#if defined(LOD_FADE_CROSSFADE)
	UNITY_VPOS_TYPE vpos : VPOS;
#else
	float4 pos : SV_POSITION;
#endif

#if SHADOWS_NEED_UV
	float2 uv : TEXCOORD0;
#endif
#if defined(SHADOWS_CUBE)
	float3 lightVec : TEXCOORD1;
#endif
#if defined(_DISTANCE_FADING)
	float3 worldPos : TEXCOORD2;
#endif
};

// Aux functions

float GetAlpha(AdvShadowInterpolators i) {
	float alpha = _Color.a;
#if SHADOWS_NEED_UV
	alpha *= tex2D(_MainTex, i.uv.xy).a;
#endif
	return alpha;
}


// Main functions

AdvShadowInterpolatorsVertex AdvShadowVertex(AdvShadowVertexData v) {
	AdvShadowInterpolatorsVertex i;
	UNITY_SETUP_INSTANCE_ID(v);
#if defined(SHADOWS_CUBE)
	i.pos = UnityObjectToClipPos(v.vertex);
	i.lightVec = mul(unity_ObjectToWorld, v.vertex).xyz - _LightPositionRange.xyz;
#else
	// Handling normal bias
	float4 position = UnityClipSpaceShadowCasterPos(v.vertex.xyz, v.normal);
	// Handling shadow bias
	i.pos = UnityApplyLinearShadowBias(position);
#endif
#if SHADOWS_NEED_UV
	i.uv = TRANSFORM_TEX(v.uv, _MainTex);
#endif
#if defined(_DISTANCE_FADING)
	i.worldPos = mul(unity_ObjectToWorld, v.vertex);
#endif
	return i;
}


float4 AdvShadowFragment(AdvShadowInterpolators i) : SV_TARGET{
	#if defined(LOD_FADE_CROSSFADE)
		UnityApplyDitherCrossFade(i.vpos);
	#endif
	float alpha = GetAlpha(i);
	#if defined(_RENDERING_CUTOUT)
		clip(alpha - _Cutoff);
	#endif

#if defined(SHADOWS_CUBE)
		float depth = length(i.lightVec) + unity_LightShadowBias.x;
		depth *= _LightPositionRange.w;
		return UnityEncodeCubeShadowDepth(depth);
#else
	return 0;
#endif
}

#endif