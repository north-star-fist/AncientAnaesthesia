#if !defined(CREATE_UNITY_LIGHT)
#define CREATE_UNITY_LIGHT

#include "Util.cginc"

// Supporting Subtractive lighting mode
#if defined(LIGHTMAP_ON) && defined(SHADOWS_SCREEN)
	#if defined(LIGHTMAP_SHADOW_MIXING) && !defined(SHADOWS_SHADOWMASK)
		#define SUBTRACTIVE_LIGHTING 1
	#endif
#endif

// Computes vertex light colors (the first pass only needed), and puts it back into input data
void ComputeVertexLightColor(inout AdvInterpolators i) {
#if defined(VERTEXLIGHT_ON)
	i.vertexLightColor = Shade4PointLights(
		unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
		unity_LightColor[0].rgb, unity_LightColor[1].rgb,
		unity_LightColor[2].rgb, unity_LightColor[3].rgb,
		unity_4LightAtten0, i.worldPos, i.normal
	);
#endif
}

float3 BoxProjection(
	float3 direction, float3 position,
	float4 cubemapPosition, float3 boxMin, float3 boxMax
) {
	UNITY_BRANCH
	if (cubemapPosition.w > 0) {
		float3 factors = ((direction > 0 ? boxMax : boxMin) - position) / direction;
		float scalar = min(min(factors.x, factors.y), factors.z);
		direction = direction * scalar + (position - cubemapPosition);
	}
	return direction;
}

// Fading directional shadows for lightmapped mode
float FadeShadows(AdvInterpolators i, float attenuation) {
#if HANDLE_SHADOWS_BLENDING_IN_GI
	// UNITY_LIGHT_ATTENUATION doesn't fade shadows for us.
	float viewZ = dot(_WorldSpaceCameraPos - i.worldPos, UNITY_MATRIX_V[2].xyz);
	float shadowFadeDistance = UnityComputeShadowFadeDistance(i.worldPos, viewZ);
	float shadowFade = UnityComputeShadowFade(shadowFadeDistance);
	float bakedAttenuation = UnitySampleBakedOcclusion(i.lightmapUV, i.worldPos);
	//attenuation = saturate(attenuation + shadowFade);
	UnityMixRealtimeAndBakedShadows(attenuation, bakedAttenuation, shadowFade);

#endif
	return attenuation;
}

void ApplySubtractiveLighting(AdvInterpolators i, inout UnityIndirect indirectLight) {
#if SUBTRACTIVE_LIGHTING
	UNITY_LIGHT_ATTENUATION(attenuation, i, i.worldPos.xyz);
	attenuation = FadeShadows(i, attenuation);

	float ndotl = saturate(dot(i.normal, _WorldSpaceLightPos0.xyz));
	float3 shadowedLightEstimate = ndotl * (1 - attenuation) * _LightColor0.rgb;
	float3 subtractedLight = indirectLight.diffuse - shadowedLightEstimate
	// The color should not be darker then shadow color that is set in Unity Lighting settings
	subtractedLight = max(subtractedLight, unity_ShadowColor.rgb);
	// Hndling shadow intensity
	subtractedLight = lerp(subtractedLight, indirectLight.diffuse, _LightShadowData.x);
	// We don't want to make shadows lighter than the baked light
	indirectLight.diffuse = min(subtractedLight, indirectLight.diffuse);
#endif
}

// Here we use PBS that Unity provides for us
UnityLight CreateUnityLight(AdvInterpolators i, float ambientOcclusion) {
	UnityLight light;
	
#if defined(DEFERRED_PASS) || SUBTRACTIVE_LIGHTING
	// Switching off dynamic light
	light.dir = float3(0, 1, 0);
	light.color = 0;
#else
	// In Unity (maybe in shaders generally) direction is - direction to source (not to target)
	// So directio of light is actually direction FROM light TO camera

#if defined(POINT) || defined(POINT_COOKIE) || defined(SPOT)
	// Point lights
	light.dir = normalize(_WorldSpaceLightPos0.xyz - i.worldPos);
#else
	// it's directional light
	light.dir = _WorldSpaceLightPos0.xyz;
#endif

	// Unity's macros from AutoLight.cginc. IT works for all types of light (handling shadows also)
	UNITY_LIGHT_ATTENUATION(attenuation, i, i.worldPos);
	attenuation = FadeShadows(i, attenuation);	// Fixing fading for lightmaps
	attenuation *= ambientOcclusion;
	light.color = _LightColor0.rgb *attenuation;
	light.ndotl = DotClamped(i.normal, light.dir);
#endif
	return light;
}

UnityIndirect CreateIndirectLight(
	AdvInterpolators i,
	float3 vertexLightColor,
	float3 viewDir,
	float smoothness,
	float ambientOcclusion,
	float2 lightmapUV = 0
) {
	UnityIndirect indirectLight;
	indirectLight.diffuse = 0;
	indirectLight.specular = 0;
	
	// Diffuse reflections
// Adding vertex lighting to indirect diffuse
#if defined(VERTEXLIGHT_ON)
	indirectLight.diffuse += vertexLightColor;
#endif
// Adding spherical harmonics to indirect diffuse
#if defined(FORWARD_BASE_PASS) || defined(DEFERRED_PASS)
#if defined(LIGHTMAP_ON)
	// no spherical harmonics needed when we have lightmaps baked
	indirectLight.diffuse = DecodeLightmap(	UNITY_SAMPLE_TEX2D(unity_Lightmap, lightmapUV));

#if defined(DIRLIGHTMAP_COMBINED)
	// We have light direction also. We are using Unity macro for reading texture via custom sampler
	// because directional maps and intensity maps use the same sampler
	float4 lightmapDirection = UNITY_SAMPLE_TEX2D_SAMPLER(
		unity_LightmapInd, unity_Lightmap, lightmapUV
	);
	// Knowing light direction we can tune the final diffuse properly
	indirectLight.diffuse = DecodeDirectionalLightmap(
		indirectLight.diffuse, lightmapDirection, i.normal
	);
#endif
	ApplySubtractiveLighting(i, indirectLight);
#endif
#if defined(DYNAMICLIGHTMAP_ON)
	float3 dynamicLightDiffuse = DecodeRealtimeLightmap(
		UNITY_SAMPLE_TEX2D(unity_DynamicLightmap, i.lightmapUV.zw)
	);

#if defined(DIRLIGHTMAP_COMBINED)
	float4 dynamicLightmapDirection = UNITY_SAMPLE_TEX2D_SAMPLER(
		unity_DynamicDirectionality, unity_DynamicLightmap,
		i.lightmapUV.zw
	);
	indirectLight.diffuse += DecodeDirectionalLightmap(
		dynamicLightDiffuse, dynamicLightmapDirection, i.normal
	);
#else
	indirectLight.diffuse += dynamicLightDiffuse;
#endif
#endif

#if !defined(LIGHTMAP_ON) && !defined(DYNAMICLIGHTMAP_ON)
	// Our spherical harmonics (and hence light probes)
	#if UNITY_LIGHT_PROBE_PROXY_VOLUME
		// Ok our project has LPPV switched on
		if (unity_ProbeVolumeParams.x == 1) {
			// And our object uses it. So we need some additional calculations (that Unity provides for this)
			indirectLight.diffuse = SHEvalLinearL0L1_SampleProbeVolume(float4(i.normal, 1), i.worldPos);
			indirectLight.diffuse = max(0, indirectLight.diffuse);
		#if defined(UNITY_COLORSPACE_GAMMA)
			indirectLight.diffuse =	LinearToGammaSpace(indirectLight.diffuse);
		#endif
		} else {
			// Ok we have an ordinary situation here
			indirectLight.diffuse += max(0, ShadeSH9(float4(i.normal, 1)));
		}
	#else
		indirectLight.diffuse += max(0, ShadeSH9(float4(i.normal, 1)));
	#endif
#endif

	// Specular reflections
	float3 reflectionDir = reflect(-viewDir, i.normal);

	// Decoding HDR because skybox (and other refl cubes) is in HDR format taking glossiness into account
	Unity_GlossyEnvironmentData envData;
	envData.roughness = 1 - smoothness;
	envData.reflUVW = BoxProjection(
		reflectionDir, i.worldPos,
		unity_SpecCube0_ProbePosition,
		unity_SpecCube0_BoxMin, unity_SpecCube0_BoxMax
	);
	float3 probe0 = Unity_GlossyEnvironment(
		UNITY_PASS_TEXCUBE(unity_SpecCube0), unity_SpecCube0_HDR, envData
	);
	envData.reflUVW = BoxProjection(
		reflectionDir, i.worldPos,
		unity_SpecCube1_ProbePosition,
		unity_SpecCube1_BoxMin, unity_SpecCube1_BoxMax
	);
#if UNITY_SPECCUBE_BLENDING
	float interpolator = unity_SpecCube0_BoxMin.w;
	UNITY_BRANCH
	if (interpolator < 0.99999) {
		float3 probe1 = Unity_GlossyEnvironment(
			UNITY_PASS_TEXCUBE_SAMPLER(unity_SpecCube1, unity_SpecCube0),
			unity_SpecCube0_HDR, envData
		);
		indirectLight.specular = lerp(probe1, probe0, interpolator);
	}
	else {
		indirectLight.specular = probe0;
	}
#else
	indirectLight.specular = probe0;
#endif

	// TODO: Check that this stuff is in it's place and works
#if defined(DEFERRED_PASS) && UNITY_ENABLE_REFLECTION_BUFFERS
	indirectLight.specular = 0;
#endif

#endif

	indirectLight.diffuse *= ambientOcclusion;
	indirectLight.specular *= ambientOcclusion;

	return indirectLight;
}

#endif