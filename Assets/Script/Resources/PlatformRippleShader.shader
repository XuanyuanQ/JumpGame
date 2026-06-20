Shader "Custom/PlatformRippleURP"
{
    Properties
    {
        _Color ("Base Color", Color) = (1,1,1,1)
        _RippleColor ("Ripple Color", Color) = (1,1,1,1)
        _RippleCenterOS ("Ripple Center OS", Vector) = (0,0,0,0)
        _RippleStartTime ("Ripple Start Time", Float) = -100
        _RippleDuration ("Ripple Duration", Float) = 0.85
        _RippleMaxRadius ("Ripple Max Radius", Float) = 0.55
        _RippleWidth ("Ripple Width", Float) = 0.16
        _RippleStrength ("Ripple Strength", Float) = 0.75
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Opaque"
            "Queue"="Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _RippleColor;
                float4 _RippleCenterOS;
                float _RippleStartTime;
                float _RippleDuration;
                float _RippleMaxRadius;
                float _RippleWidth;
                float _RippleStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionOS = IN.positionOS.xyz;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half3 normalWS = normalize(IN.normalWS);
                Light mainLight = GetMainLight();
                half lightTerm = saturate(dot(normalWS, mainLight.direction)) * 0.45h + 0.55h;
                half3 baseColor = _Color.rgb * lightTerm;

                float age = _Time.y - _RippleStartTime;
                float active = step(0.0, age) * step(age, _RippleDuration);
                float topMask = smoothstep(0.45, 0.82, normalWS.y);

                float2 delta = IN.positionOS.xz - _RippleCenterOS.xz;
                float dist = length(delta);
                float progress = saturate(age / max(_RippleDuration, 0.001));
                float radius = progress * _RippleMaxRadius;
                float fade = active * pow(1.0 - progress, 1.35);

                float ring = exp(-pow(abs(dist - radius) / max(_RippleWidth, 0.001), 2.0));
                float softFill = exp(-dist / max(radius + _RippleWidth, 0.001)) * 0.16;
                float ripple = (ring + softFill) * fade * topMask * _RippleStrength;

                half3 color = baseColor + _RippleColor.rgb * ripple;
                return half4(color, _Color.a);
            }
            ENDHLSL
        }
    }
}
