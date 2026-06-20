Shader "Custom/testSurfaceShader_URP"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _ChargeAmount ("Charge Amount", Float) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Opaque"
            "Queue"="Geometry"
        }

        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _MainTex_ST;
                half _Glossiness;
                half _Metallic;
                float _ChargeAmount;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                half fogFactor : TEXCOORD3;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float3 localPos = IN.positionOS.xyz;

                float weight = saturate(localPos.y);
                localPos.y -= _ChargeAmount * 0.5 * weight;
                localPos.x += _ChargeAmount * localPos.x * (1.0 - localPos.y) * 0.5;
                localPos.z += _ChargeAmount * localPos.z * (1.0 - localPos.y) * 0.5;

                VertexPositionInputs positionInputs = GetVertexPositionInputs(localPos);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS = positionInputs.positionCS;
                OUT.positionWS = positionInputs.positionWS;
                OUT.normalWS = normalInputs.normalWS;
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.fogFactor = ComputeFogFactor(OUT.positionCS.z);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half4 baseColor = texColor * _Color;

                SurfaceData surfaceData;
                ZERO_INITIALIZE(SurfaceData, surfaceData);

                surfaceData.albedo = baseColor.rgb;
                surfaceData.alpha = baseColor.a;
                surfaceData.metallic = _Metallic;
                surfaceData.smoothness = _Glossiness;
                surfaceData.normalTS = half3(0, 0, 1);
                surfaceData.occlusion = 1.0;
                surfaceData.emission = half3(0, 0, 0);

                InputData inputData;
                ZERO_INITIALIZE(InputData, inputData);

                inputData.positionWS = IN.positionWS;
                inputData.normalWS = normalize(IN.normalWS);
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                inputData.fogCoord = IN.fogFactor;
                inputData.bakedGI = SAMPLE_GI(0, 0, inputData.normalWS);

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.rgb = MixFog(color.rgb, IN.fogFactor);

                return color;
            }

            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}