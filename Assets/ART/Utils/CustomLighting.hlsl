#ifndef CUSTOM_LIGHTING_INCLUDED
#define CUSTOM_LIGHTING_INCLUDED


#ifndef SHADERGRAPH_PREVIEW
	#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
	#if (SHADERPASS != SHADERPASS_FORWARD)
		#undef REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR
	#endif
#endif

//#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

struct CustomLightingData {
	// Surface
	float3 albedo;
	float smoothness;

	// Position and Direction
	float3 worldPosition;
	float3 worldNormal;
	float3 viewDirectionWS;
	float4 shadowCoord;
	float ambientOcclusion;

	// Baked GI
	float3 bakedGI;
};

//Light GetCustomLocalLight(CustomLightingData d) {
//	Light light = GetMainLight(d.shadowCoord, d.worldPosition, 1);
//	
//	return light;
//}

float GetSmoothnessPower(float rawSmoothness) {
	return exp2(10 * rawSmoothness + 1);
}

#ifndef SHADERGRAPH_PREVIEW
float3 CustomGlobalIllumination(CustomLightingData d) {
	float3 indirectDiffuse = d.albedo * d.bakedGI * d.ambientOcclusion;

	float3 reflectVector = reflect(-d.viewDirectionWS, d.worldNormal);

	float fresnel = Pow4(1 - saturate(dot(d.viewDirectionWS, d.worldNormal)));

	float3 indirectSpecular = GlossyEnvironmentReflection(reflectVector,
		RoughnessToPerceptualRoughness(1 - d.smoothness),
		d.ambientOcclusion) * fresnel;

	return indirectDiffuse * indirectSpecular;
}

float3 CustomLightingHandling(CustomLightingData d, Light light) {
	
	float3 radiance = light.color * (light.distanceAttenuation * light.shadowAttenuation);

	float diffuse = saturate(dot(d.worldNormal, light.direction));

	float specularDot = saturate(dot(d.worldNormal, normalize(light.direction + d.viewDirectionWS)));
	float specular = pow(specularDot, GetSmoothnessPower(d.smoothness)) * diffuse;

	float3 color = d.albedo * radiance * (diffuse + specular);

	return color;
}
#endif

float3 CalculateCustomLighting(CustomLightingData d) {
#ifdef SHADERGRAPH_PREVIEW

	float3 lightDir = float3(0.5, 0.5, 0);
	float intensity = saturate(dot(d.worldNormal, lightDir)) +
		pow(saturate(dot(d.worldNormal, normalize(d.viewDirectionWS + lightDir))), GetSmoothnessPower(d.smoothness));
	return d.albedo * intensity;
#else
	Light mainLight = GetMainLight(d.shadowCoord, d.worldPosition, 1);

	MixRealtimeAndBakedGI(mainLight, d.worldNormal, d.bakedGI);
	float3 color = CustomGlobalIllumination(d);

	color += CustomLightingHandling(d, mainLight);

#ifdef _ADDITIONAL_LIGHTS

	uint numAdditionalLights = GetAdditionalLightsCount();
	for (uint i = 0; i < numAdditionalLights; i++) {
		Light light = GetAdditionalLight(i, d.worldPosition, 1);
		color += CustomLightingHandling(d, light);
	}
#endif

	return color;
#endif
}

void CalculateCustomLighting_float(
	float3 Albedo, float3 Normal,
	float3 ViewDirection, float Smoothness,
	float3 Position, float AmbientOcclusion,
	float2 LightmapUV,
	out float3 Color)
{
	CustomLightingData d;

	d.albedo = Albedo;
	d.worldNormal = Normal;
	d.viewDirectionWS = ViewDirection;
	d.smoothness = Smoothness;
	d.worldPosition = Position;
	d.ambientOcclusion = AmbientOcclusion;

#ifdef SHADERGRAPH_PREVIEW
	d.shadowCoord = 0;
	d.bakedGI = 0;
#else

	float4 posCS = TransformWorldToHClip(Position);
	#if SHADOWS_SCREEN
	d.shadowCoord = ComputeScreenPos(posCS);
	#else
	d.shadowCoord = TransformWorldToShadowCoord(Position);
	#endif
	
	// BAKED GI
	float3 lightmapUV;
	OUTPUT_LIGHTMAP_UV(LightmapUV, unity_LightmapST, lightmapUV);

	float3 vertexSH;
	OUTPUT_SH(Normal, vertexSH);

	d.bakedGI = SAMPLE_GI(lightmapUV, vertexSH, Normal);

#endif
	Color = CalculateCustomLighting(d);
}

#endif

//#if defined(SHADERGRAPH_PREVIEW)
//#else
//#pragma multi_compile _ _MAIN_LIGHT_SHADOWS
//#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
//#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
//#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
//#pragma multi_compile_fragment _ _SHADOWS_SOFT
//#pragma multi_compile _ SHADOWS_SHADOWMASK
//#pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSIONC:\Users\Obrutsky\Desktop\Projects\___MJRPG_Systems\test.hlsl
//#pragma multi_compile _ LIGHTMAP_ON
//#pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
//#endif
//
////Light _GetLocalLight(float3 localPos)
////{
////    
////}
////
////void _LocalLight_float(float3 WorldPos, float3 localLightPos, out float3 Direction, out float3 Color, out float ShadowAtten)
////{
////#if defined(SHADERGRAPH_PREVIEW)
////    Direction = float3(0.5, 0.5, 0);
////    Color = 1;
////    ShadowAtten = 1;
////#else
////    float4 shadowCoord = TransformWorldToShadowCoord(WorldPos);
////    
////
////    Light mainLight = GetMainLight(shadowCoord); // GetLocalLight(localLightPos);
////    Direction = mainLight.direction;
////    Color = mainLight.color;
////
////#if !defined(_MAIN_LIGHT_SHADOWS) || defined(_RECEIVE_SHADOWS_OFF)
////    ShadowAtten = 1.0h;
////#else
////    ShadowSamplingData shadowSamplingData = GetMainLightShadowSamplingData();
////    float shadowStrength = GetMainLightShadowStrength();
////    ShadowAtten = SampleShadowmap(shadowCoord, TEXTURE2D_ARGS(_MainLightShadowmapTexture,
////        sampler_MainLightShadowmapTexture),
////        shadowSamplingData, shadowStrength, false);
////#endif
////#endif
////}
//
//void MainLight_float(float3 WorldPos, out float3 Direction, out float3 Color, out float ShadowAtten)
//{
//#if defined(SHADERGRAPH_PREVIEW)
//    Direction = float3(0.5, 0.5, 0);
//    Color = 1;
//    ShadowAtten = 1;
//#else
//	float4 shadowCoord = TransformWorldToShadowCoord(WorldPos);
//
//    Light mainLight = GetMainLight(shadowCoord);
//    Direction = mainLight.direction;
//    Color = mainLight.color;
//
//	#if !defined(_MAIN_LIGHT_SHADOWS) || defined(_RECEIVE_SHADOWS_OFF)
//		ShadowAtten = 1.0h;
//    #else
//	    ShadowSamplingData shadowSamplingData = GetMainLightShadowSamplingData();
//	    float shadowStrength = GetMainLightShadowStrength();
//	    ShadowAtten = SampleShadowmap(shadowCoord, TEXTURE2D_ARGS(_MainLightShadowmapTexture,
//	    sampler_MainLightShadowmapTexture),
//	    shadowSamplingData, shadowStrength, false);
//    #endif
//#endif
//}
//
//
//void DirectSpecular_float(float Smoothness, float3 Direction, float3 WorldNormal, float3 WorldView, out float3 Out)
//{
//    float4 White = 1;
//
//#if defined(SHADERGRAPH_PREVIEW)
//    Out = 0;
//#else
//    Smoothness = exp2(10 * Smoothness + 1);
//    WorldNormal = normalize(WorldNormal);
//    WorldView = SafeNormalize(WorldView);
//    Out = LightingSpecular(White, Direction, WorldNormal, WorldView, White, Smoothness);
//#endif
//}
//
//void AdditionalLights_float(float Smoothness, float3 WorldPosition, float3 WorldNormal, float3 WorldView, out float3 Diffuse, out float3 Specular)
//{
//    float3 diffuseColor = 0;
//    float3 specularColor = 0;
//    float4 White = 1;
//
//#if !defined(SHADERGRAPH_PREVIEW)
//    Smoothness = exp2(10 * Smoothness + 1);
//    WorldNormal = normalize(WorldNormal);
//    WorldView = SafeNormalize(WorldView);
//    int pixelLightCount = GetAdditionalLightsCount();
//    for (int i = 0; i < pixelLightCount; ++i)
//    {
//        Light light = GetAdditionalLight(i, WorldPosition);
//        half3 attenuatedLightColor = light.color * (light.distanceAttenuation * light.shadowAttenuation);
//        diffuseColor += LightingLambert(attenuatedLightColor, light.direction, WorldNormal);
//        specularColor += LightingSpecular(attenuatedLightColor, light.direction, WorldNormal, WorldView, White, Smoothness);
//    }
//#endif
//
//    Diffuse = diffuseColor;
//    Specular = specularColor;
//}
