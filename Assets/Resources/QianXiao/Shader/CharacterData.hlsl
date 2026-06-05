static const half3 kBodyTypeMaskSkin = half3(1.0h, 0.1019608h, 0.0h);
static const half3 kBodyTypeMaskCloth = half3(0.0h, 0.4h, 0.0h);
static const half3 kBodyTypeMaskStockings = half3(0.7960785h, 0.4h, 0.0h);

struct BodyAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS   : NORMAL;
    float4 tangentOS  : TANGENT;
    float2 uv         : TEXCOORD0;
};

struct BodyVaryings
{
    float4 positionCS  : SV_POSITION;
    float3 positionWS  : TEXCOORD0;
    float3 normalWS    : TEXCOORD1;
    float4 tangentWS   : TEXCOORD2;
    float2 uv          : TEXCOORD3;
    float4 shadowCoord : TEXCOORD4;

};

struct BodySurfaceData
{
    half3 albedo;
    half metallic;
    half roughness;
    half occlusion;
    half smoothness;

    float3 normalTS;
    half3 typeWeights;
};

struct BodyInputData
{
    float3 positionWS;
    float4 positionCS;

    half3 geometryNormalWS;
    half3 normalWS;
    half3 viewDirectionWS;
    float4 shadowCoord;
};

half3 BodySampleTypeWeights(float2 uv)
{
    // The exported type mask is color-coded, not an ID texture.
    // We convert RGB to three soft weights so borders stay stable even under filtering.
    half3 typeMask = SAMPLE_TEXTURE2D(_TypeMaskTex, sampler_TypeMaskTex, uv).rgb;
    half threshold = max(_TypeMaskThreshold, 0.0001h);

    half3 weights;
    weights.x = saturate(1.0h - distance(typeMask, kBodyTypeMaskSkin) / threshold);
    weights.y = saturate(1.0h - distance(typeMask, kBodyTypeMaskCloth) / threshold);
    weights.z = saturate(1.0h - distance(typeMask, kBodyTypeMaskStockings) / threshold);

    half weightSum = weights.x + weights.y + weights.z;
    if (weightSum > 0.0001h)
    {
        weights /= weightSum;
    }
    else
    {
        weights = half3(0.0h, 0.0h, 0.0h);
    }

    return weights;
}

half BodyResolveTypedScalar(half3 typeWeights, half skinValue, half clothValue, half stockingsValue)
{
    half typedWeight = saturate(typeWeights.x + typeWeights.y + typeWeights.z);
    half defaultWeight = 1.0h - typedWeight;

    return defaultWeight
        + typeWeights.x * skinValue
        + typeWeights.y * clothValue
        + typeWeights.z * stockingsValue;
}

half3 BodyResolveTypedColor(half3 typeWeights, half3 skinValue, half3 clothValue, half3 stockingsValue)
{
    half typedWeight = saturate(typeWeights.x + typeWeights.y + typeWeights.z);
    half defaultWeight = 1.0h - typedWeight;

    return defaultWeight.xxx
        + typeWeights.x * skinValue
        + typeWeights.y * clothValue
        + typeWeights.z * stockingsValue;
}

half3 BodyUnpackNormalRG(half2 packedRG, half normalScale)
{
    // 重写映射
    half2 normalXY = packedRG * 2.0h - 1.0h;
    normalXY *= normalScale;

    // 切线空间的法向量是归一化了的，故可得z*z=1-x*x-y*y
    half normalZ = sqrt(saturate(1.0h - dot(normalXY, normalXY)));
    return half3(normalXY, normalZ);
}

half3x3 BuildBodyTangentToWorld(half3 normalWS, half4 tangentWS)
{
    // tangentWS.w stores handedness.
    // It tells us whether the bitangent has to be flipped.
    half3 bitangentWS = tangentWS.w * cross(normalWS, tangentWS.xyz);
    return half3x3(tangentWS.xyz, bitangentWS, normalWS);
}

void InitializeBodySurfaceData(float2 uv, out BodySurfaceData surfaceData)
{
    half4 mainSample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
    half4 nmrSample = SAMPLE_TEXTURE2D(_NmrTex, sampler_NmrTex, uv);
    
    half3 typeWeights = BodySampleTypeWeights(uv);
    half3 typeColorTint = BodyResolveTypedColor(typeWeights, _SkinColorTint.rgb, _ClothColorTint.rgb, _StockingsColorTint.rgb);
    half metallicMul = BodyResolveTypedScalar(typeWeights, _SkinMetallicMul, _ClothMetallicMul, _StockingsMetallicMul);
    half roughnessMul = BodyResolveTypedScalar(typeWeights, _SkinRoughnessMul, _ClothRoughnessMul, _StockingsRoughnessMul);
    half occlusionMul = BodyResolveTypedScalar(typeWeights, _SkinOcclusionMul, _ClothOcclusionMul, _StockingsOcclusionMul);

    surfaceData.albedo = mainSample.rgb * _BaseColor.rgb * typeColorTint;
    surfaceData.normalTS = BodyUnpackNormalRG(nmrSample.rg, _NormalScale);
    surfaceData.typeWeights = typeWeights;
    surfaceData.metallic = saturate(nmrSample.b * _MetallicScale * metallicMul);
    surfaceData.roughness = saturate(nmrSample.a * _RoughnessScale * roughnessMul);
    surfaceData.occlusion = saturate(lerp(1.0h, mainSample.a, _FoldShadowStrength) * occlusionMul);
    surfaceData.smoothness = saturate(1.0h - surfaceData.roughness);
}

void InitializeBodyInputData(BodyVaryings input, half3 normalTS, out BodyInputData inputData)
{
    inputData.positionWS = input.positionWS;
    inputData.positionCS = input.positionCS;
    inputData.geometryNormalWS = NormalizeNormalPerPixel(input.normalWS);

    // 将切线空间的法向量变为世界空间
    half3x3 tangentToWorld = BuildBodyTangentToWorld(input.normalWS, input.tangentWS);
    half3 normalWS = NormalizeNormalPerPixel(TransformTangentToWorld(normalTS, tangentToWorld));

    inputData.normalWS = normalWS;
    inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
    // inputData.bakedGI = BodySampleSHPixel(input.vertexSH, normalWS);
    inputData.shadowCoord = input.shadowCoord;
    // inputData.fogFactor = input.fogFactor;
}