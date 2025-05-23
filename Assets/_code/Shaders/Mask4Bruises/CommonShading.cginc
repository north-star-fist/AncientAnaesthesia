#if !defined(M4B_COMMON_SHADING)
#define M4B_COMMON_SHADING

// major code is here

// Light attenuation by range and intensity
#include "AutoLight.cginc"
// My shared data types
#include "Mask4BruisesDataTypes.cginc"
// My shared Specular and Metallic stuff
#include "CreateUnityLight.cginc"
#include "Util.cginc"

float4 GetAltPortions(AdvInterpolators i) {
	float4 ap4 = float4(_AltPortion, _AltPortion2, _AltPortion3, _AltPortion4);
	float4 mask4 = tex2D(_AltMaskTex, i.uv);
	float4 portions = ap4 * mask4;
	float sum = portions.x + portions.y + portions.z + portions.w;
	if (sum > 1) {
		return portions / sum;
	}
	return portions;
}

float3 SampleMainEmissionTex(AdvInterpolators i) {
#if defined(_EMISSION_MAP)
	return tex2D(_EmissionMapTex, i.uv);
#else
	return 0;
#endif
}


float3 GetEmission(AdvInterpolators i) {
#if defined(FORWARD_BASE_PASS) || defined(DEFERRED_PASS)
#if defined(_EMISSION_MAP)
	return SampleMainEmissionTex(i) * _EmissionColor;
#else
	return _EmissionColor;
#endif
#else
	return 0;
#endif
}

//  Gets transparency factor
float GetAlpha(AdvInterpolators i) {
	float a = _Color.a;
#if defined(_RENDERING_CUTOUT)
	if (_SmoothnessSource != 1) {
		// When we get smoothness from main tex alpha channel we assume that it's not transparency
		a *= tex2D(_MainTex, i.uv).a;
	}
#endif
	return a;
}


float3 GetDiffuseAndSpecular(
	float3 plainColor,
	inout float3 specularTint,
	float metallic,
	inout float oneMinusReflectivity
) {
	return DiffuseAndSpecularFromMetallic(
		plainColor.rgb, metallic, specularTint, oneMinusReflectivity
	);
}


float GetMainMetallic(float2 uv = 0) {
	return tex2D(_MetalMapTex, uv).r * _Metallic;
}

float GetAltMetallic(float2 uv = 0, int channel = 0) {
	if (channel == 0) {
		return tex2D(_MetalMapTex, uv).r * _AltMetallic;
	}
	else if (channel == 1) {
		return tex2D(_MetalMapTex, uv).r * _AltMetallic2;
	}
	else if (channel == 2) {
		return tex2D(_MetalMapTex, uv).r * _AltMetallic3;
	}
	else if (channel == 3) {
		return tex2D(_MetalMapTex, uv).r * _AltMetallic4;
	}
	return 0;
}

float GetMainSmoothnessFromTexture(AdvInterpolators i) {
	float s = 1;
	if (_SmoothnessSource == 1) {
		// From color texture
		s *= tex2D(_MainTex, i.uv).a;
	}
	else if (_SmoothnessSource == 2) {
		sampler2D smoothTex = _MetalMapTex;
		s *= tex2D(smoothTex, i.uv).a;
	}
	return s;
}

float GetAltSmoothnessFromTexture(AdvInterpolators i, int channel) {
	float s = 1;
	int smoothSource = 0;
	if (channel == 0) {
		smoothSource = _AltSmoothnessSource;
	} else if (channel == 1) {
		smoothSource = _AltSmoothnessSource2;
	} else if (channel == 2) {
		smoothSource = _AltSmoothnessSource3;
	} else if (channel == 3) {
		smoothSource = _AltSmoothnessSource4;
	}
	if (smoothSource == 1) {
		// From color texture
		if (channel == 0) {
			s *= tex2D(_AltTex, i.uv).a;
		}
		else if (channel == 1) {
			s *= tex2D(_AltTex2, i.uv).a;
		}
		else if (channel == 2) {
			s *= tex2D(_AltTex3, i.uv).a;
		}
		else if (channel == 3) {
			s *= tex2D(_AltTex4, i.uv).a;
		}
	}
	else if (smoothSource == 2) {
		// From metal/spec texture
		s *= tex2D(_MetalMapTex, i.uv).a;
	}
	return s;
}


float GetMainSmoothness(AdvInterpolators i) {
	float s = _Smoothness;
	if (_SmoothnessSource > 0) {
		// From color texture
		s *= GetMainSmoothnessFromTexture(i);
	}
	return s;
}

float GetAltSmoothness(AdvInterpolators i, int channel) {
	float s = GetMainSmoothness(i);
	s = _AltSmoothness;
	if (channel == 0) {
		s = _AltSmoothness;
	} else if (channel == 1) {
		s = _AltSmoothness2;
	}
	else if (channel == 2) {
		s = _AltSmoothness3;
	}
	else if (channel == 3) {
		s = _AltSmoothness4;
	}
	float fromTex = 1;
	int smoothSource = 0;
	if (channel == 0) {
		smoothSource = _AltSmoothnessSource;
	}
	else if (channel == 1) {
		smoothSource = _AltSmoothnessSource2;
	}
	else if (channel == 2) {
		smoothSource = _AltSmoothnessSource3;
	}
	else if (channel == 3) {
		smoothSource = _AltSmoothnessSource4;
	}
	if (smoothSource > 0) {
		// Getting it from textures
		fromTex = GetAltSmoothnessFromTexture(i, channel);
	}
	s *= fromTex;
	return s;
}

float3 GetTSNormal(AdvInterpolators i, float normalScale) {
	float3 tsNormal = float3(0, 0, 1);
	tsNormal = UnpackScaleNormal(tex2D(_NormalMapTex, i.uv), normalScale);
	return tsNormal;
}

float3 GetAltTSNormal(AdvInterpolators i, int channel, float portion) {
	float3 aN = float3(0, 0, 1);
	float scale = _AltNormalScale;
	sampler2D text = _AltNormalMapTex;
	if (channel == 0) {
		scale = _AltNormalScale;
		text = _AltNormalMapTex;
	}
	else if (channel == 1) {
		scale = _AltNormalScale2;
		text = _AltNormalMapTex2;
	}else if (channel == 2) {
		scale = _AltNormalScale3;
		text = _AltNormalMapTex3;
	}
	else if (channel == 3) {
		scale = _AltNormalScale4;
		text = _AltNormalMapTex4;
	}
	return UnpackScaleNormal(tex2D(text, i.uv), scale * portion);
}



float3 GetMainAlbedo(AdvInterpolators i, inout float oneMinusReflectivity, inout float3 specTint) {
	float metallic= GetMainMetallic(i.uv);
	float4 plainColor = tex2D(_MainTex, i.uv) * _Color;
	// Specularity
	return GetDiffuseAndSpecular(plainColor.rgb, specTint, metallic, oneMinusReflectivity);
}

float3 GetAltAlbedo(
	AdvInterpolators i,
	inout float altOneMinusReflectivity,
	inout float3 altSpecTint,
	int channel
) {
	float3 altColor = 0;

	float altMetallic = 0;
	altMetallic = GetAltMetallic(i.uv, channel);

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

	altColor = color.rgb;
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

	float altPart = altMask.x + altMask.y + altMask.z + altMask.w;

	oneMinusReflectivity = saturate(lerp(oneMinusReflectivity, altOneMinusReflectivity, altPart));
	specTint = lerp(specTint, altSpecTint, altPart);

	return MixColors(altPart, mainAlbedo, altAlbedo);
}

float4 ApplyFog(float4 color, AdvInterpolators i) {
#if FOG_ON
	float viewDistance;
#if FOG_DEPTH
	// Clip space depth
	viewDistance = UNITY_Z_0_FAR_FROM_CLIPSPACE(i.pos.z);
#else
	viewDistance = length(_WorldSpaceCameraPos - i.worldPos);
#endif
	
	// Unity calclulates fog factor depending on scene lighting settings and puts it to unityFogFactor
	UNITY_CALC_FOG_FACTOR_RAW(viewDistance);
	float3 fogColor = 0;
#if defined(FORWARD_BASE_PASS)
	fogColor = unity_FogColor.rgb;
#endif
	color.rgb = lerp(fogColor, color.rgb, saturate(unityFogFactor));
#endif
	return color;
}

#endif