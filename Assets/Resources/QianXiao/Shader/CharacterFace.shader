Shader "Custom/CharacterFace"
{
    Properties
    {
        _BaseMap ("BaseMap", 2D) = "white" {}
        _FaceAmbientStrength ("FaceAmbientStrength", Range(0.08, 0.2)) = 0.08
        _SDFMap ("SDFMap", 2D) = "white" {}
        _FaceShadowColor ("FaceShadowColor", Color) = (0.55,0.60,0.72,1)
        _FaceShadowSmooth ("FaceShadowSmooth", Range(0, 1)) = 0.4

    }
    SubShader
    {
        Tags 
        {       
            "RenderType"="Opaque"  // 不透明材质
            "RenderPipeline"="UniversalPipeline"  // 表示这个 SubShader 只在 URP 下使用
            "Queue"="Geometry"  // 不透明物体的渲染队列
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }  // 该 Pass 在 URP 前向渲染路径中执行

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            
            // 导入 URP 已经封装好的基础工具函数、矩阵、宏定义、坐标变换函数等。
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // 引入 URP 的 lighting 库，以获取主光
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION; // 从模型顶点数据中读取模型空间坐标
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0; // 模型的第一套UV坐标，用来在纹理上定位采样位置
            };

            struct Varyings
            {
                float4 positionCS    : SV_POSITION; // 顶点变换到裁剪空间后输出给光栅化阶段的位置
                float3 positionWS    :TEXCOORD0;
                float2 uv            : TEXCOORD1;
                float3 normalWS      : TEXCOORD2;
                float3 faceRightWS   : TEXCOORD3;
                float3 faceForwardWS : TEXCOORD4;
            };

            TEXTURE2D(_BaseMap); // 在HLSL中声明这是一张2D纹理，后续才能采样
            SAMPLER(sampler_BaseMap); // 声明与_BaseMap配套的采样器，决定纹理如何被采样
            TEXTURE2D(_RampAtlas);
            SAMPLER(sampler_RampAtlas);
            TEXTURE2D(_BaseRamp);
            SAMPLER(sampler_BaseRamp);
            TEXTURE2D(_SDFMap);
            SAMPLER(sampler_SDFMap);

            CBUFFER_START(UnityPerMaterial) // 声明当前材质使用的常量缓冲区，Unity会把材质参数传给GPU
                float4 _BaseMap_ST; // Unity为纹理自动提供的缩放和偏移参数，xy是缩放，zw是偏移
                half4 _FaceShadowColor;
                float _FaceAmbientStrength;
                float _FaceShadowSmooth;
            CBUFFER_END

            float4 _RampAtlas_TexelSize; // Unity为纹理自动提供
            float4 _BaseRamp_TexelSize; // Unity为纹理自动提供

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz); // 将模型空间坐标变换到裁剪空间，函数内部会补齐次分量w=1
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap); // 使用_BaseMap_ST对UV做缩放和偏移
                OUT.faceRightWS = normalize(TransformObjectToWorldDir(float3(1,0,0)));
                OUT.faceForwardWS = normalize(TransformObjectToWorldDir(float3(0,0,1)));
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                Light mainLight = GetMainLight();

                float3 forward = -unity_ObjectToWorld._m02_m12_m22; // 该角色脸模型forward实际是脸后方，故取反获得脸前方的forward
                float3 right = unity_ObjectToWorld._m00_m10_m20;
                
                float3 lightDirXZ = normalize(float3(mainLight.direction.x, 0, mainLight.direction.z));
                float3 forwardXZ = normalize(float3(forward.x, 0, forward.z));
                float3 rightXZ = normalize(float3(right.x, 0, right.z));

                float lightAtten = 1.0 - (dot(lightDirXZ, forwardXZ) * 0.5 + 0.5);
                
                float side = dot(lightDirXZ, rightXZ);
                float2 sdfUV = IN.uv;
                if (side > 0.0) // 在右边需要翻转
                {
                    sdfUV.x = 1.0 - sdfUV.x;
                }

                float4 sdf = SAMPLE_TEXTURE2D(_SDFMap, sampler_SDFMap, sdfUV);
                float threshold = 0.6; // 通过观察获得，r到a并不是刚好中间就变

                float faceMask = smoothstep(lightAtten - _FaceShadowSmooth, lightAtten, sdf.b);

                float attenSegment;

                if (lightAtten > threshold)
                {
                    attenSegment = (lightAtten - threshold)/(1-threshold); // 重新映射到0-1
                    faceMask += step(attenSegment, sdf.a);
                }
                else
                {
                    attenSegment = lightAtten/threshold;
                    faceMask += step(attenSegment, sdf.r);
                }

                half4 baseTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                half3 baseColor = baseTex.rgb;

                half3 darkColor = baseColor * _FaceShadowColor.rgb;
                half3 litColor = baseColor * mainLight.color * 0.5;

                half3 ambient = baseColor * half3(1, 1, 1) * _FaceAmbientStrength;

                half3 faceLitColor = lerp(darkColor, litColor, faceMask);
                
                faceLitColor += ambient;

                return half4(faceLitColor, baseTex.a);
            }

            ENDHLSL
        }
    }
}
