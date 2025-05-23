#if !defined(ADV_UTIL)
#define ADV_UTIL

// common shadering stuff from Unity guys
// #include "UnityCG.cginc"	// we include it through UnityStandardBRDF.cginc
// lighting useful stuff from Unity guys
//#include "UnityStandardBRDF.cginc"
// Energy conservation functions etc.
//#include "UnityStandardUtils.cginc"
// Physically-Based Shading. Includes all the stuff above
#include "UnityPBSLighting.cginc"

float3 CreateBinormal(float3 normal, float3 tangent, float binormalSign) {
	return cross(normal, tangent.xyz) * (binormalSign * unity_WorldTransformParams.w);
}

// Gets world space normal of the fragment with applied tangebt space normal (from normal map).
float3 GetTangentSpaceNormal(float3 fragNormal, float3 tangSpaceNormal, float4 tangent, float3 binormalPrecalculated) {
	float3 tangentSpaceNormal = tangSpaceNormal;
#if defined(BINORMAL_PER_FRAGMENT)
	// Fragment (here) tangent space binormal calculation. Less performant because of per-pixel calcs
	float3 binormal = CreateBinormal(fragNormal, tangent.xyz, tangent.w);
#else
	// Vertex tangent space binormal calculation. Most performant because of vertex-only
	float3 binormal = binormalPrecalculated;
#endif

	return normalize(
		tangentSpaceNormal.x * tangent +
		tangentSpaceNormal.y * binormal +
		tangentSpaceNormal.z * fragNormal
	);
}

float3 MixColorsGammaSpace(float portion2, float3 col1, float3 col2) {
	if (portion2 == 0) {
		return col1;
	}
	else if (portion2 == 1) {
		return col2;
	}

	// Gamma space addition
	// see https://discussions.unity.com/t/understanding-srgb-and-gamma-corrected-values-in-the-render-pipeline/766833/2
	// convert linear space colors back to gamma space
	col1 = LinearToGammaSpace(col1);
	col2 = LinearToGammaSpace(col2);
	// combine and convert back to linear space
	return GammaToLinearSpace(col1 * (1 - portion2) + col2 * portion2);
}

float Mix(float4 portions4, float4 val4) {
	float total = portions4.x + portions4.y + portions4.z + portions4.w + 0.0001f;
	float p1 = portions4.x / total;
	float p2 = portions4.y / total;
	float p3 = portions4.z / total;
	float p4 = portions4.w / total;
	return val4.x * p1 + val4.y * p2 + val4.z * p3 + val4.w * p4;
}

float3 Mix(float4 portions4, float3 val1, float3 val2, float3 val3, float3 val4) {
	float total = portions4.x + portions4.y + portions4.z + portions4.w + 0.0001f;
	float p1 = portions4.x / total;
	float p2 = portions4.y / total;
	float p3 = portions4.z / total;
	float p4 = portions4.w / total;
	return val1 * p1 + val2 * p2 + val3 * p3 + val4 * p4;
}

float4 MixColors(float portion2, float4 col1, float4 col2) {
	return float4(MixColorsGammaSpace(portion2, col1.rgb, col2.rgb), lerp(col1.a, col2.a, portion2));
}

float3 MixColors(float portion2, float3 col1, float3 col2) {
	return MixColorsGammaSpace(portion2, col1, col2);
}

float3 MixColors(float4 portions, float3 col1, float3 col2, float3 col3, float3 col4) {
	float p1 = portions.y / (portions.x + portions.y + 0.0001f);
	float3 c1 = MixColorsGammaSpace(p1, col1.rgb, col2.rgb);
	float p2 = portions.w / (portions.z + portions.w + 0.0001f);
	float3 c2 = MixColorsGammaSpace(p2, col3.rgb, col4.rgb);

	float p = (portions.z + portions.w) / (portions.x + portions.y + portions.z + portions.w + 0.0001f);
	return MixColorsGammaSpace(p, c1, c2);
}

float GetChannel(float4 src, int channel) {
	if (channel == 0) {
		return src.x;
	}
	else if (channel == 1) {
		return src.y;
	}
	else if (channel == 2) {
		return src.z;
	}
	else {
		return src.w;
	}
}

float4 MixTexColors(float portion2, sampler2D sampler1, float2 uv1, float4 col1,
	sampler2D sampler2, float2 uv2, float4 col2
) {
	if (portion2 == 0) {
		return tex2D(sampler1, uv1) * col1;
	}
	else if (portion2 == 1) {
		return tex2D(sampler2, uv2) * col2;
	}
	float4 texCol1 = tex2D(sampler1, uv1) * col1;
	float4 texCol2 = tex2D(sampler2, uv2) * col2;
	return MixColors(portion2, texCol1, texCol2);
}

float4 MixTextures(float portion2, sampler2D sampler1, float2 uv1, float4 col1,
	sampler2D sampler2, float2 uv2, float4 col2
) {
	if (portion2 == 0) {
		return tex2D(sampler1, uv1) * col1;
	}
	else if (portion2 == 1) {
		return tex2D(sampler2, uv2) * col2;
	}
	float4 texCol1 = tex2D(sampler1, uv1) * col1;
	float4 texCol2 = tex2D(sampler2, uv2) * col2;
	return lerp(texCol1, texCol2, portion2);
}

// Unity's whiteout normals blending
float3 MixNormalMaps(float portion2, sampler2D normals1, float2 uv1, float scale1,
	sampler2D normals2, float2 uv2, float scale2
) {
	if (portion2 == 0) {
		return UnpackScaleNormal(tex2D(normals1, uv1), scale1);
	}
	else if (portion2 == 1) {
		return UnpackScaleNormal(tex2D(normals2, uv2), scale2);
	}
	float3 normal1 = UnpackScaleNormal(tex2D(normals1, uv1), scale1 * (1 - portion2));
	float3 normal2 = UnpackScaleNormal(tex2D(normals2, uv2), scale2 * portion2);
	return BlendNormals(normal1, normal2);
}

float MixTextureChannels(float portion2, sampler2D sampler1, float2 uv1, float scale1,
	sampler2D sampler2, float2 uv2, float4 scale2, int channel = 0
) {
	if (portion2 == 0) {
		return GetChannel(tex2D(sampler1, uv1), channel) * scale1;
	}
	else if (portion2 == 1) {
		return GetChannel(tex2D(sampler2, uv2), channel) * scale2;
	}
	float c1 = GetChannel(tex2D(sampler1, uv1), channel) * scale1;
	float c2 = GetChannel(tex2D(sampler2, uv2), channel) * scale2;
	return lerp(c1, c2, portion2);
}
#endif