Shader "Custom/CharacterBase"
{
    Properties
    {
        [Header(Base Maps)]
        [MainTexture] _MainTex ("MainTex (RGB albedo A occlusion)", 2D) = "white" {}
        [MainColor] _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        [NoScaleOffset] _NmrTex ("NMRTex (RG normal B metallic A roughness)", 2D) = "bump" {}

        [Header(Type Mask)]
        [NoScaleOffset] _TypeMaskTex ("TypeMaskTex (ID)", 2D) = "white" {}
        _TypeMaskThreshold ("TypeMaskThreshold", Range(0.01, 0.5)) = 0.08

        [Header(Type Tints)]
        [HDR] _SkinColorTint ("SkinColorTint", Color) = (1, 1, 1, 1)
        [HDR] _ClothColorTint ("ClothColorTint", Color) = (1, 1, 1, 1)
        [HDR] _StockingsColorTint ("StockingsColor", Color) = (1, 1, 1, 1)

        [Header(Type Material)]
        _SkinMetallicMul ("SkinMetallicMul", Range(0, 2)) = 1
        _SkinRoughnessMul ("SkinRoughnessMul", Range(0, 2)) = 1
        _SkinOcclusionMul ("SkinOcclusionMul", Range(0, 2)) = 1
        _ClothMetallicMul ("ClothMetallicMul", Range(0, 2)) = 1
        _ClothRoughnessMul ("ClothRoughnessMul", Range(0, 2)) = 1
        _ClothOcclusionMul ("ClothOcclusionMul", Range(0, 2)) = 1
        _StockingsMetallicMul ("StockingsMetallic", Range(0, 2)) = 1
        _StockingsRoughnessMul ("StockingsRoughnessMul", Range(0, 2)) = 1
        _StockingsOcclusionMul ("StockingsOcclusionMul", Range(0, 2)) = 1

        [Header(Body Toon Specular)]
        [HDR] _BodySpecColor("Body Spec Color", Color) = (1, 1, 1, 1)
        _BodySpecIntensity("Body Spec Intensity", Range(0, 4)) = 1
        _BodySpecSignalBoost("Body Spec Signal Boost", Range(0.1, 16)) = 4
        _BodySpecThreshold("Body Spec Threshold", Range(0, 1)) = 0.5
        _BodySpecSoftness("Body Spec Softness", Range(0.001, 0.25)) = 0.03
        _BodySpecAAStrength("Body Spec AA Strength", Range(0, 4)) = 1.5

        [Header(PBR Controls)]
        _NormalScale("Normal Scale", Range(0, 2)) = 1
        _MetallicScale("Metallic Scale", Range(0, 1)) = 1
        _RoughnessScale("Roughness Scale", Range(0, 1)) = 1
        _FoldShadowStrength("Fold Shadow Strength", Range(0, 1)) = 1
    
        [Header(Procedural Toon Shadow)]
        [HDR] _ShadowColor("Shadow Color", Color) = (0.72, 0.75, 0.82, 1)
        _ShadowThreshold("Shadow Threshold", Range(0, 1)) = 0.58
        _ShadowSoftness("Shadow Softness", Range(0.001, 0.25)) = 0.04
        _ShadowDetailNormalWeight("Shadow Detail Normal Weight", Range(0, 1)) = 0.25
        _ShadowAAStrength("Shadow AA Strength", Range(0, 4)) = 1.5
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue"      = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
        }

        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            ZWrite On
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex ForwardPassVertex
            #pragma fragment ForwardPassFragment

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            half3 _BaseColor;

            half _TypeMaskThreshold;

            half3 _SkinColorTint;
            half3 _ClothColorTint;
            half3 _StockingsColorTint;

            half _SkinMetallicMul;
            half _SkinRoughnessMul;
            half _SkinOcclusionMul;
            half _ClothMetallicMul;
            half _ClothRoughnessMul;
            half _ClothOcclusionMul;
            half _StockingsMetallicMul;
            half _StockingsRoughnessMul;
            half _StockingsOcclusionMul;

            half3 _BodySpecColor;
            half _BodySpecIntensity;
            half _BodySpecSignalBoost;
            half _BodySpecThreshold;
            half _BodySpecSoftness;
            half _BodySpecAAStrength;

            half _NormalScale;
            half _MetallicScale;
            half _RoughnessScale;
            half _FoldShadowStrength;

            half3 _ShadowColor;
            half _ShadowThreshold;
            half _ShadowSoftness;
            half _ShadowDetailNormalWeight;
            half _ShadowAAStrength;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_NmrTex);
            SAMPLER(sampler_NmrTex);

            TEXTURE2D(_TypeMaskTex);
            SAMPLER(sampler_TypeMaskTex);

            #include "CharacterData.hlsl"
            #include "BodyPBRLighting.hlsl"

            BodyVaryings ForwardPassVertex(BodyAttributes input)
            {
                BodyVaryings output = (BodyVaryings)0;

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS   = normalInputs.normalWS;
                output.tangentWS  = float4(normalInputs.tangentWS.xyz, input.tangentOS.w * GetOddNegativeScale());
                output.uv         = TRANSFORM_TEX(input.uv, _MainTex);
                output.shadowCoord = GetShadowCoord(positionInputs);

                return output;
            }

            half4 ForwardPassFragment(BodyVaryings input) : SV_Target
            {
                BodySurfaceData surfaceData;
                InitializeBodySurfaceData(input.uv, surfaceData);

                BodyInputData inputData;
                InitializeBodyInputData(input, surfaceData.normalTS, inputData);

                // half4 mainTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                // return half4(mainTex.a.xxx, 1.0);
                
                return BodyPBR(inputData, surfaceData);
                
                // return half4(lerp(1.0.xxx, BodyPBR(inputData, surfaceData), surfaceData.typeWeights.r), 1.0);


            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex ShadowCasterVertex
            #pragma fragment ShadowCasterFragment
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct ShadowAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct ShadowVaryings
            {
                float4 positionCS : SV_POSITION;
            };

            half BodyIsDirectionalLight()
            {
                return round(_ShadowBias.z) == 1.0h ? 1.0h : 0.0h;
            }

            float3 BodyApplyShadowBias(float3 positionWS, float3 normalWS, float3 lightDirectionWS)
            {
                float invNdotL = 1.0 - saturate(dot(lightDirectionWS, normalWS));
                float scale = invNdotL * _ShadowBias.y;

                positionWS += lightDirectionWS * _ShadowBias.xxx;
                positionWS += normalWS * scale.xxx;
                return positionWS;
            }

            float4 BodyApplyShadowClamping(float4 positionCS)
            {
            #if UNITY_REVERSED_Z
                float clampedZ = min(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
            #else
                float clampedZ = max(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
            #endif

                positionCS.z = lerp(positionCS.z, clampedZ, BodyIsDirectionalLight());
                return positionCS;
            }

            float4 GetShadowPositionHClipBody(ShadowAttributes input)
            {
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDirectionWS = normalize(_LightPosition - positionWS);
            #else
                float3 lightDirectionWS = _LightDirection;
            #endif

                float3 biasedPositionWS = BodyApplyShadowBias(positionWS, normalWS, lightDirectionWS);
                float4 positionCS = TransformWorldToHClip(biasedPositionWS);
                return BodyApplyShadowClamping(positionCS);
            }

            ShadowVaryings ShadowCasterVertex(ShadowAttributes input)
            {
                ShadowVaryings output = (ShadowVaryings)0;
                output.positionCS = GetShadowPositionHClipBody(input);
                return output;
            }

            half4 ShadowCasterFragment(ShadowVaryings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}