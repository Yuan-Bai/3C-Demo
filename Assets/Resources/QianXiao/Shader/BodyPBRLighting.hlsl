

static const half4 kBodyDielectricSpec = half4(0.04h, 0.04h, 0.04h, 0.96h);

struct BodyBRDFData
{
    half3 albedo;
    half3 diffuse;
    half3 specular;
    half reflectivity;
    half perceptualRoughness;
    half roughness;
    half roughness2;
    half grazingTerm;
    half normalizationTerm;
    half roughness2MinusOne;
};

void InitializeBodyBRDFData(BodySurfaceData surfaceData, out BodyBRDFData brdfData)
{
    half oneMinusReflectivity = kBodyDielectricSpec.a - surfaceData.metallic * kBodyDielectricSpec.a;
    half reflectivity = 1.0h - oneMinusReflectivity;

    brdfData.albedo = surfaceData.albedo;
    brdfData.diffuse = surfaceData.albedo * oneMinusReflectivity;
    brdfData.specular = lerp(kBodyDielectricSpec.rgb, surfaceData.albedo, surfaceData.metallic);
    brdfData.reflectivity = reflectivity;

    brdfData.perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surfaceData.smoothness);
    brdfData.roughness = max(PerceptualRoughnessToRoughness(brdfData.perceptualRoughness), HALF_MIN_SQRT);
    brdfData.roughness2 = max(brdfData.roughness * brdfData.roughness, HALF_MIN);
    brdfData.grazingTerm = saturate(surfaceData.smoothness + reflectivity);
    brdfData.normalizationTerm = brdfData.roughness * 4.0h + 2.0h;
    brdfData.roughness2MinusOne = brdfData.roughness2 - 1.0h;

}

// 计算直接光漫反射
half3 EvaluateBodyDirectDiffuse(BodyBRDFData brdfData, BodyInputData inputData, Light light, half occlusion)
{
    // 人物自身阴影区域计算，使用模型法线计算更平滑
    half ndotlShadow  = saturate(dot(inputData.geometryNormalWS, light.direction));
    half halfLambert = ndotlShadow  * 0.5h + 0.5h;
    half aaWidth = fwidth(halfLambert) * _ShadowAAStrength;
    half shadowWidth = max(_ShadowSoftness, aaWidth);
    half bodyShadow = smoothstep(_ShadowThreshold - shadowWidth, _ShadowThreshold + shadowWidth, halfLambert);
    // bodyShadow += occlusion;

    // // 场景阴影 （目前效果不好，放弃添加）
    // half shadowAttenuation = saturate(light.shadowAttenuation);
    // aaWidth = fwidth(shadowAttenuation) * _ShadowAAStrength;
    // shadowWidth = max(_ShadowSoftness, aaWidth);
    // half sceneShadow = smoothstep(0.5h - shadowWidth, 0.5h + shadowWidth, shadowAttenuation);

    half3 lightTint = lerp(_ShadowColor.rgb * 0.6, float3(1.0, 1.0, 1.0), bodyShadow) + occlusion;
    half3 toonRadiance = light.color * light.distanceAttenuation * lightTint;
    return brdfData.diffuse * toonRadiance;
}

// 计算直接光高光
half3 EvaluateBodyDirectSpecular(BodyBRDFData brdfData, Light light, half3 normalWS, half3 viewDirectionWS)
{
    float3 halfDir = SafeNormalize(light.direction + viewDirectionWS);
    // 计算高光使用贴图法线，细节更多
    half ndotlLight = saturate(dot(normalWS, light.direction));
    float ndoth = saturate(dot(normalWS, halfDir));
    half ldoth = saturate(dot(light.direction, halfDir));

    float d = ndoth * ndoth * brdfData.roughness2MinusOne + 1.00001f;
    half ldoth2 = ldoth * ldoth;

    half specularTerm = brdfData.roughness2 / ((d * d) * max(0.1h, ldoth2) * brdfData.normalizationTerm);
    
    #if REAL_IS_HALF
        specularTerm = specularTerm - HALF_MIN;
        specularTerm = clamp(specularTerm, 0.0h, 1000.0h);
    #endif

    specularTerm = max(specularTerm, 0.0h) * _BodySpecSignalBoost;
    half specularSignal = specularTerm / (1.0h + specularTerm);

    half aaWidth = fwidth(specularSignal) * _BodySpecAAStrength;
    half specularWidth = max(_BodySpecSoftness, aaWidth);
    half toonSpecularMask = smoothstep(_BodySpecThreshold - specularWidth, _BodySpecThreshold + specularWidth, specularSignal);
    
    half3 specularTint = brdfData.specular * _BodySpecColor.rgb;
    return specularTint * (_BodySpecIntensity * toonSpecularMask);
}

half4 BodyPBR(BodyInputData inputData, BodySurfaceData surfaceData)
{
    BodyBRDFData brdfData;
    InitializeBodyBRDFData(surfaceData, brdfData);

    Light mainLight = GetMainLight(inputData.shadowCoord);



    half3 color = EvaluateBodyDirectDiffuse(brdfData, inputData, mainLight, surfaceData.occlusion);
    color += EvaluateBodyDirectSpecular(brdfData, mainLight, inputData.normalWS, inputData.viewDirectionWS);
    return half4(color, 1.0);
}
