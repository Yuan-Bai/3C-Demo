Shader "Custom/CharacterBang"
{
    Properties
    {
        [Header(Base Maps)]
        [MainTexture] _MainTex ("MainTex (RGB Albedo)", 2D) = "white" {}
        [MainColor] _BaseColor ("Base Color", Color) = (1, 1, 1, 1)

        [NoScaleOffset] _HairUVMap ("Hair UV Map (RG)", 2D) = "gray" {}
        [NoScaleOffset] _SpecMaskTex ("Hair Mask Tex", 2D) = "white" {}
        [NoScaleOffset] _HairNoiseTex ("Hair Noise Tex", 2D) = "gray" {}


        [Header(Spec Args)]
        _SpecCenter ("SpecCenter", Range(0, 1)) = 0.5
        _SpecWidth ("SpecWidth", Range(0, 0.1)) = 0.5
        _SpecBlur ("SpecBlur", Range(0, 0.1)) = 0.02

        _XXX ("XXX", Range(0, 1)) = 0.1
        _YYY ("YYY", Range(0, 1)) = 0.2
    }
    SubShader
    {
        Tags { "LightMode" = "UniversalForward" }

        ZWrite On
        ZTest LEqual
        Cull Back

        Pass
        {
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

            half _SpecCenter;
            half _SpecWidth;
            half _SpecBlur;

            half _XXX;
            half _YYY;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_HairUVMap);
            SAMPLER(sampler_HairUVMap);

            TEXTURE2D(_SpecMaskTex);
            SAMPLER(sampler_SpecMaskTex);

            TEXTURE2D(_HairNoiseTex);
            SAMPLER(sampler_HairNoiseTex);

            half4 HairRamp(half t)
            {
                t = saturate(t);

                // ===== 节点位置 =====
                const half p0 = 0.103h;
                const half p1 = 0.127h;
                const half p2 = 0.141h;
                const half p3 = 0.153h;

                // ===== 颜色 =====
                const half3 c0 = half3(0.0h, 0.0h, 0.0h);
                const half3 c1 = half3(0.1137h, 0.2392h, 0.2745h);
                const half3 c2 = half3(0.4784h, 0.4078h, 0.4471h);
                const half3 c3 = half3(0.0h, 0.0h, 0.0h);

                // ===== 分段颜色插值 =====

                half k01 = saturate((t - p0) / (p1 - p0));
                half k12 = saturate((t - p1) / (p2 - p1));
                half k23 = saturate((t - p2) / (p3 - p2));

                k01 = smoothstep(0.0h, 1.0h, k01);
                k12 = smoothstep(0.0h, 1.0h, k12);
                k23 = smoothstep(0.0h, 1.0h, k23);

                half3 col01 = lerp(c0, c1, k01);
                half3 col12 = lerp(c1, c2, k12);
                half3 col23 = lerp(c2, c3, k23);

                half3 color = c0;

                color = lerp(color, col01, step(p0, t));
                color = lerp(color, col12, step(p1, t));
                color = lerp(color, col23, step(p2, t));
                color = lerp(color, c3,    step(p3, t));

                // ===== Alpha 渐变 =====
                half alpha = 1.0h - saturate((t - p1) / (p2 - p1));

                return half4(color, alpha);
            }

            struct HairAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
            };

            struct HairVaryings
            {
                float4 positionCS  : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float4 tangentWS   : TEXCOORD2;
                float2 uv          : TEXCOORD3;
                float4 shadowCoord : TEXCOORD4;
                float4 positionOS  : TEXCOORD5;
            };

            HairVaryings ForwardPassVertex(HairAttributes input)
            {
                HairVaryings output = (HairVaryings)0;

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.tangentWS = float4(normalInputs.tangentWS.xyz, input.tangentOS.w * GetOddNegativeScale());
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.shadowCoord = GetShadowCoord(positionInputs);
                output.positionOS = input.positionOS;

                return output;
            }

            half4 ForwardPassFragment(HairVaryings input) : SV_Target
            {
                half3 mainTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).rgb;
                half2 hairUvMap = SAMPLE_TEXTURE2D(_HairUVMap, sampler_HairUVMap, input.uv).rg;
                half3 specMaskRgb = SAMPLE_TEXTURE2D(_SpecMaskTex, sampler_SpecMaskTex, input.uv).rgb;
                
                // half3 geometryNormalWS = NormalizeNormalPerPixel(input.normalWS);/
                float3 normalVS = TransformWorldToViewDir(input.normalWS);
                // half3 tangentWS = SafeNormalize(input.tangentWS.xyz);
                // half3 bitangentWS = SafeNormalize(input.tangentWS.w * cross(geometryNormalWS, tangentWS));
                // half3x3 tangentToWorld = half3x3(tangentWS, bitangentWS, geometryNormalWS);
                // half3 noiseuvx = NormalizeNormalPerPixel(TransformWorldToTangent(tangentWS, tangentToWorld));

                half noise =SAMPLE_TEXTURE2D(_HairNoiseTex, sampler_HairNoiseTex, half2(0.4*hairUvMap.r, 0)).r;
                // half temp = (noise - 0.5h) * (-1.140h + 1.0h) + 0.5h - 0.86h + hairUvMap.g;


                half3 viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                Light mainLight = GetMainLight(input.shadowCoord);
                half sceneShadow = saturate(mainLight.shadowAttenuation);
                half3 halfDir = SafeNormalize(viewDirectionWS + mainLight.direction);
                half shift = dot(halfDir, half3(0,1,0));
                half center = _SpecCenter + shift * 0.2 + noise*0.1;
                half temp = (noise - 0.5h) * (-1.04h+1.0h) + 0.5 + hairUvMap.g-1.0 - shift*0.2;
                temp /= 1.4h;

                // half a = center - _SpecWidth;
                // half b = center + _SpecWidth;

                // half mask = smoothstep(a - _SpecBlur, a + _SpecBlur, temp) * (1.0 - smoothstep(b - _SpecBlur, b + _SpecBlur, temp));
                
                // half start = _XXX;
                // half end = _YYY;
                // half3 tipColor1 = half3(60.0h/255.0h, 182.0h/255.0h, 191.0h/255.0h);
                // half3 tipColor2 = half3(154.0h/255.0h, 117.0h/255.0h, 133.0h/255.0h);
                // half k = (temp - start) / (end - start);
                half4 hairSpecColor = HairRamp(temp);
                hairSpecColor = hairSpecColor*specMaskRgb.r;
                hairSpecColor.rgb += mainTex;
                return hairSpecColor;
            }

            ENDHLSL
        }
    }
}
