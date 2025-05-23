// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/Mask4Bruises"
{

	Properties{
		_Color("Tint", Color) = (1, 1, 1, 1)
		_MainTex("Main Texture", 2D) = "white" {}
		_Cutoff("Alpha Cutoff", Range(0, 1)) = 0.5
		_Smoothness("Smoothness", Range(0, 1)) = 0.5
		_SmoothnessSource("Smoothness Source", Integer) = 0	// 0 - uniform, 1 - MainTex alpha, 2 - MetallicTex alpha

		[Gamma] _Metallic("Metallic", Range(0, 1)) = 0	// We use Unity function for metallic stuff so need to convert to Gamma space
		[NoScaleOffset] _MetalMapTex("Metallic Map", 2D) = "white" {}

		[NoScaleOffset] _NormalMapTex("Normals", 2D) = "bump" {}
		_NormalScale("Normals Scale", float) = 1
		[NoScaleOffset] _EmissionMapTex("Emission Map", 2D) = "black" {}
		_EmissionColor("Emission Color", Color) = (0, 0, 0)

		[NoScaleOffset] _AltMaskTex("Alternative Blend Mask", 2D) = "white" {}	// Has _ST of the main texture

		// Alternative texturing masked by _AltMaskTex channels
		// R
		_AltPortion("Alternative Portion", Range(0, 1)) = 0.5
		_AltColor("Alternative Tint", Color) = (1, 1, 1, 1)
		_AltTex("Alternative Texture", 2D) = "white" {}
		_AltSmoothness("Alternative Smoothness", Range(0, 1)) = 0.5
		_AltSmoothnessSource("Alternative Smoothness Source", Integer) = 0	// 0 - uniform, 1 - MainTex alpha, 2 - MetallicTex alpha
		[Gamma] _AltMetallic("Alternative Metallic", Range(0, 1)) = 0
		[NoScaleOffset] _AltNormalMapTex("Alternative Normals", 2D) = "bump" {}
		_AltNormalScale("Alternative Normals Scale", float) = 1

		// G
		_AltPortion2("Alternative 2 Portion", Range(0, 1)) = 0.5
		_AltColor2("Alternative 2 Tint", Color) = (1, 1, 1, 1)
		_AltTex2("Alternative 2 Texture", 2D) = "white" {}
		_AltSmoothness2("Alternative 2 Smoothness", Range(0, 1)) = 0.5
		_AltSmoothnessSource2("Alternative 2 Smoothness Source", Integer) = 0	// 0 - uniform, 1 - MainTex alpha, 2 - MetallicTex alpha
		[Gamma] _AltMetallic2("Alternative 2 Metallic", Range(0, 1)) = 0
		[NoScaleOffset] _AltNormalMapTex2("Alternative 2 Normals", 2D) = "bump" {}
		_AltNormalScale2("Alternative 2 Normals Scale", float) = 1

		// B
		_AltPortion3("Alternative 3 Portion", Range(0, 1)) = 0.5
		_AltColor3("Alternative 3 Tint", Color) = (1, 1, 1, 1)
		_AltTex3("Alternative 3 Texture", 2D) = "white" {}
		_AltSmoothness3("Alternative 3 Smoothness", Range(0, 1)) = 0.5
		_AltSmoothnessSource3("Alternative 3 Smoothness Source", Integer) = 0	// 0 - uniform, 1 - MainTex alpha, 2 - MetallicTex alpha
		[Gamma] _AltMetallic3("Alternative 3 Metallic", Range(0, 1)) = 0
		[NoScaleOffset] _AltNormalMapTex3("Alternative 3 Normals", 2D) = "bump" {}
		_AltNormalScale3("Alternative 3 Normals Scale", float) = 1

		// A
		_AltPortion4("Alternative 4 Portion", Range(0, 1)) = 0.5
		_AltColor4("Alternative 4 Tint", Color) = (1, 1, 1, 1)
		_AltTex4("Alternative 4 Texture", 2D) = "white" {}
		_AltSmoothness4("Alternative 4 Smoothness", Range(0, 1)) = 0.5
		_AltSmoothnessSource4("Alternative 4 Smoothness Source", Integer) = 0	// 0 - uniform, 1 - MainTex alpha, 2 - MetallicTex alpha
		[Gamma] _AltMetallic4("Alternative 4 Metallic", Range(0, 1)) = 0
		[NoScaleOffset] _AltNormalMapTex4("Alternative 4 Normals", 2D) = "bump" {}
		_AltNormalScale4("Alternative 4 Normals Scale", float) = 1


		// Culling. 0 - off, 1 - culling face (back is visible), 2 - culling back (face is visible)
		[HideInInspector] _Cull("Culling Mode", Integer) = 2
		[HideInInspector] _SrcBlend("_SrcBlend", Integer) = 1
		[HideInInspector] _DstBlend("_DstBlend", Integer) = 0
		[HideInInspector] _ZWrite("_ZWrite", Integer) = 1
	}

	CustomEditor "Sergei.Safonov.Shaders.Mask4BruisesShaderEditor"

	CGINCLUDE
	// Common stuff for all passes project wide
	#define BINORMAL_PER_FRAGMENT
	#define FOG_DISTANCE

	ENDCG

	SubShader{

		Pass {
			// First pass (the main directional light only)
			Tags {
				"LightMode" = "ForwardBase"
			}
			Blend [_SrcBlend] [_DstBlend]
			ZWrite [_ZWrite]
			Cull [_Cull]

			CGPROGRAM
				// For Physically-Based Shading better to enable 3.0
				#pragma target 3.0
				// to handle vertex lighting, lightmapping, shadows etc.
				#pragma multi_compile_fwdbase
				// Forward rendering Fog support
				#pragma multi_compile_fog
				// LOD crossffading support
				#pragma multi_compile _ LOD_FADE_CROSSFADE
				#pragma multi_compile_instancing
				#pragma instancing_options lodfade

				// Shader configuration
				#pragma shader_feature _ _RENDERING_CUTOUT
			
				// Emission is just added to color in base pass and that's it. No additional passes needed
				#pragma shader_feature _EMISSION_MAP

				#pragma vertex AdvVertex
				#pragma fragment AdvFragment

				// For including spherical harmonics into indirect diffuse lighting
				#define FORWARD_BASE_PASS

				#include "Mask4BruisesLighting.cginc"

			ENDCG
		}
		
		Pass {
			// Additional pass (other light sources)
			Tags {
				"LightMode" = "ForwardAdd"
			}
			Blend [_SrcBlend] One

			// No need to wright to Z-buffer. It's filled already after the firs pass
			ZWrite Off

			CGPROGRAM
			// For Physically-Based Shading better to enable 3.0
			#pragma target 3.0
			// Handling shadows from all types of light in additive pass
			#pragma multi_compile_fwdadd_fullshadows
			// Forward rendering Fog support
			#pragma multi_compile_fog
			// LOD crossffading support
			#pragma multi_compile _ LOD_FADE_CROSSFADE
			#pragma multi_compile_instancing
			#pragma instancing_options lodfade

			// Shader configuration
			#pragma shader_feature _RENDERING_CUTOUT

			#pragma vertex AdvVertex
			#pragma fragment AdvFragment

			#include "Mask4BruisesLighting.cginc"

			ENDCG
		}
		
		Pass {
			Tags {
				"LightMode" = "Deferred"
			}

			CGPROGRAM

				#pragma target 3.0
				#pragma exclude_renderers nomrt

				// Supports HDR, lightmaps etc
				#pragma multi_compile_prepassfinal
				// LOD crossffading support
				#pragma multi_compile _ LOD_FADE_CROSSFADE
				#pragma multi_compile_instancing
				#pragma instancing_options lodfade

				// Shader configuration
				#pragma shader_feature _ _RENDERING_CUTOUT

				// Emission is just added to color in base pass and that's it. No additional passes needed
				#pragma shader_feature _EMISSION_MAP

				#pragma vertex AdvVertex
				#pragma fragment AdvFragment

				#define DEFERRED_PASS

				#include "Mask4BruisesLighting.cginc"

			ENDCG
		}

		Pass {
			Tags {
				"LightMode" = "ShadowCaster"
			}

			CGPROGRAM

			#pragma target 3.0
			// For SHADOW_DEPTH and SHADOW_CUBE
			#pragma multi_compile_shadowcaster
			// LOD crossffading support
			#pragma multi_compile _ LOD_FADE_CROSSFADE
			// instancing handling
			#pragma multi_compile_instancing
			#pragma instancing_options lodfade

			// For cuttin out hikes into shadows
			#pragma shader_feature _RENDERING_CUTOUT

			#pragma vertex AdvShadowVertex
			#pragma fragment AdvShadowFragment

			#include "Shadows.cginc"

			ENDCG
		}

		Pass{
			// Unity lightmapping support. While baking Unity uses it to see what color a thing has
			Tags {
				"LightMode" = "Meta"
			}

			Cull Off

			CGPROGRAM

			// Shader configuration
			#pragma shader_feature _ _RENDERING_CUTOUT

			#pragma shader_feature _EMISSION_MAP

			#pragma vertex AdvLightmappingVertex
			#pragma fragment AdvLightmappingFragment

			#include "Mask4BruisesLightmapping.cginc"

			ENDCG
		}
	}
}
