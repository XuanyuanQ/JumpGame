Shader "Custom/PondWater"
{
    Properties
    {
        _WaveNormal_Texture ("Wave Normal Map", 2D) = "bump" {}
        _Cubemap ("Reflection Cubemap", Cube) = "_Skybox" {}
        _MainTex ("Water Texture", 2D) = "white" {}
        _Color ("Water Color", Color) = (0, 0.5, 1, 0.5)
        _WaveSpeed ("Wave Speed", Float) = 1.0
        _WaveAmp ("Wave Amplitude", Float) = 0.1

        [Header(Ripple Settings)]
        _RippleAmplitude ("Ripple Amp", Float) = 0.32
        _RippleFreq ("Ripple Freq", Float) = 18.0
        _RippleSpeed ("Ripple Speed", Float) = 0.08
        _RippleDuration ("Ripple Duration", Float) = 5.2
        
        // 这两个变量由 C# 更新
        _ImpactPos ("Impact Position", Vector) = (0,0,0,0)
        _ImpactTime ("Impact Time", Float) = -100.0

        _WaterMin ("Impact Position", Vector) = (0,0,0,0)
        _WaterMax ("Impact Time", Vector) = (0,0,0,0)
    }

    SubShader
    {
        // 对应 OpenGL 的 glEnable(GL_BLEND)
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha


        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

        struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            // struct v2f {
            //     float2 uv : TEXCOORD0;
            //     float4 vertex : SV_POSITION;
            // };

            struct v2f {
            float4 vertex : SV_POSITION;
            float2 uv : TEXCOORD0;
            float4 normalCoord01 : TEXCOORD3; // xy 存放 coord0, zw 存放 coord1
            float2 normalCoord2 : TEXCOORD4;
            float3 worldPos : TEXCOORD5;
            float3 worldViewDir : TEXCOORD6;
            float3 worldLightDir : TEXCOORD7;
            // TBN 矩阵通常在片元着色器里重组，或者直接把切线空间的方向传过去
            float3x3 TBNWave : TEXCOORD8; 
            float test : TEXCOORD11;
        };


            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _WaveSpeed;
            float _WaveAmp;
            sampler2D _WaveNormal_Texture;
            samplerCUBE _Cubemap;

            // 涟漪参数
            float4 _ImpactPos;
            float _ImpactTime;
            float _RippleAmplitude;
            float _RippleFreq;
            float _RippleSpeed;
            float _RippleDuration;
            float4 _InputCentre[10];

            float3 _WaterMin;
            float3 _WaterMax;


            float waveFun(float time, float A, float f, float p, float k, float2 D, float3 p_pos) {
                // 这里的 D 是方向向量，p_pos 是顶点坐标
                // 加上 0.5 的偏移是为了让波形在 [0, 1] 之间波动，适应幂运算 k
                float a = sin((D.x * p_pos.x + D.y * p_pos.y) * f + time * p) * 0.5 + 0.5;
                return A * pow(a, k);
            }

            // 计算导数的自定义函数（用于后续计算法线）
            float derivativeMain(float time, float A, float f, float p, float k, float2 D, float3 p_pos) {
                // 这里的 k-1.0 必须确保不为负数，否则 pow 函数在 OpenGL/D3D 上可能会报错或返回 NaN
                float wave = waveFun(time, A, f, p, max(0, k - 1.0), D, p_pos);
                return 0.5 * k * f * wave * cos((D.x * p_pos.x + D.y * p_pos.y) * f + time * p);
            }

            // --- 新增：涟漪计算函数 ---
            // 返回 float2: x = 高度偏移, y = 导数(用于法线)
            float calculateRipple(float2 worldXZ, float4 centreData) {
                float startTime = centreData.z;
                if (startTime <= 0.0) return 0.0;

                float age = _Time.y - startTime;
                if (age < 0.0 || age > _RippleDuration) return 0.0;

                float2 waterSize = max(_WaterMax.xz - _WaterMin.xz, float2(0.001, 0.001));
                float maxWaterSize = max(waterSize.x, waterSize.y);
                float2 uv = saturate((worldXZ - _WaterMin.xz) / waterSize);
                float2 centre = saturate((centreData.xy - _WaterMin.xz) / waterSize);

                float2 aspectScale = waterSize / maxWaterSize;
                float dist = length((uv - centre) * aspectScale);
                float radius = age * _RippleSpeed;

                float phase = (dist - radius) * _RippleFreq * 6.2831853;
                float ring = sin(phase);
                float ringWidth = max(0.015, 1.0 / max(_RippleFreq, 1.0));
                float ringMask = exp(-abs(dist - radius) / ringWidth);
                float timeFade = saturate(1.0 - age / max(_RippleDuration, 0.001));
                timeFade *= timeFade;

                float strength = centreData.w <= 0.0 ? 1.0 : centreData.w;
                return ring * ringMask * timeFade * _RippleAmplitude * strength;
            }

            // --- Vertex Shader (顶点着色器) ---
            v2f vert (appdata v)
            {
                v2f o;
                // 1. 拿到原始顶点
                float3 raw = v.vertex.xyz;
                float3 worldPosRaw = mul(unity_ObjectToWorld, v.vertex).xyz;
                float elapsed_time_s = _Time.y/4.0;
                // elapsed_time_s=0;

                float2 ground = raw.xy; 

                // 3. 计算波浪时，强制使用我们定义的“平地”坐标
                float wave1 = waveFun(elapsed_time_s, 0.1, 0.2, 0.5, 2.0, float2(-1.0, 0.0), float3(ground, 0));
                float wave2 = waveFun(elapsed_time_s, 0.05, 0.4, 1.3, 2.0, float2(-0.7, 0.7), float3(ground, 0));
                float combinedWave=0;
                UNITY_LOOP
                for(int n =0; n<10; n++)
                {
                combinedWave += calculateRipple(worldPosRaw.xz, _InputCentre[n]);
                // break;
                }

                // 4. 合成新坐标：高度加在原始的 Z 上
                float totalHeight = (wave1 + wave2)*0.4;
                float3 modifiedPos = float3(raw.x, raw.y, totalHeight); 
                
                // 5. 变换
                o.worldPos = mul(unity_ObjectToWorld, float4(modifiedPos, 1.0)).xyz;
                float3 worldPos = o.worldPos;
               
               worldPos.y=worldPos.y+combinedWave;
                
                
                o.vertex = mul(UNITY_MATRIX_VP, float4(worldPos, 1.0));
                // o.vertex.z = o.vertex.z+combinedWave;
                o.test=combinedWave;
                // o.vertex=v.vertex;
                // o.vertex = UnityObjectToClipPos(float4(modifiedPos, 1.0));
                // o.vertex = UnityObjectToClipPos(float4(raw, 1.0));
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                // o.vertex=v.vertex.xyzw;
                float3 p_pos = v.vertex.xzy;
                // 4. 计算导数并求和（用于计算法线）
                float dx1 = derivativeMain(elapsed_time_s, 0.1, 0.2, 0.5, 2.0, float2(-1.0, 0.0), p_pos) * (-1.0);
                float dx2 = derivativeMain(elapsed_time_s, 0.05, 0.4, 1.3, 2.0, float2(-0.7, 0.7), p_pos) * (-0.7);

                float dz1 = derivativeMain(elapsed_time_s, 0.1, 0.2, 0.5, 2.0, float2(-1.0, 0.0), p_pos) * (0.0);
                float dz2 = derivativeMain(elapsed_time_s, 0.05, 0.4, 1.3, 2.0, float2(-0.7, 0.7), p_pos) * (0.7);

                
                // --- 2. 动态 UV 计算 (类似你的 normalCoord0/1/2) ---
                float2 texScale = float2(8.0, 4.0);
                float normalTime = fmod(elapsed_time_s, 100.0); // mod -> fmod
                float2 normalSpeed = float2(-0.05, 0.0);

                o.normalCoord01.xy = v.uv * texScale * 1.0 + normalTime * normalSpeed;
                o.normalCoord01.zw = v.uv * texScale * 2.0 + normalTime * normalSpeed * 4.0;
                o.normalCoord2 = v.uv * texScale * 4.0 + normalTime * normalSpeed * 8.0;

                float sumDx = dx1 + dx2;
                float sumDz = dz1 + dz2;

                // Unity 中 normal_model_to_world 的等价矩阵是 unity_ObjectToWorld 的逆转置矩阵
                // 但对于法线变换，通常直接用 UnityObjectToWorldNormal 宏更安全
                float3 tangentWave = UnityObjectToWorldNormal(float3(1.0, sumDx, 0.0));
                float3 binormalWave = UnityObjectToWorldNormal(float3(0.0, sumDz, 1.0));
                float3 normalWave = UnityObjectToWorldNormal(float3(-sumDx, 1.0, -sumDz));

                // 组建 TBN 矩阵
                o.TBNWave = float3x3(normalize(tangentWave), normalize(binormalWave), normalize(normalWave));

                // --- 4. 视角和光照向量 (fV, fL) ---
                // 视角向量：摄像机位置 - 顶点世界位置
                o.worldViewDir = _WorldSpaceCameraPos.xyz - worldPos;

                // 光照向量：平行光可以直接用 _WorldSpaceLightPos0.xyz
                // 如果是点光源，则是 _WorldSpaceLightPos0.xyz - worldPos
                o.worldLightDir = _WorldSpaceLightPos0.xyz - worldPos;
                return o;
            }

            // --- Fragment Shader (片元着色器) ---
            fixed4 frag (v2f i,bool facing_front : SV_IsFrontFace) : SV_Target
            {
            // --- 1. 向量归一化 ---
                float3 V = normalize(i.worldViewDir);
                float3 L = normalize(i.worldLightDir);

                // --- 2. 采样并混合 3 层法线贴图 (Normal Mapping) ---
                // Unity 中法线贴图通常是 UnpackNormal，但如果你是原始坐标，则用 *2-1
                float3 n0 = tex2D(_WaveNormal_Texture, i.normalCoord01.xy).xyz * 2.0 - 1.0;
                float3 n1 = tex2D(_WaveNormal_Texture, i.normalCoord01.zw).xyz * 2.0 - 1.0;
                float3 n2 = tex2D(_WaveNormal_Texture, i.normalCoord2).xyz * 2.0 - 1.0;
                
                // 基础贴图 (n3)
                float3 n3 = tex2D(_WaveNormal_Texture, i.uv).xyz * 2.0 - 1.0;

                // 混合三层法线权重
                float3 nBump = normalize(n0 * 0.6 + n1 * 0.3 + n2 * 0.1);
                // 控制整体扰动强度 (mix 对应 lerp)
                nBump = normalize(lerp(float3(0.0, 0.0, 1.0), nBump, 0.4));

                // 将法线从切线空间变换到世界空间 (使用你之前算好的 TBNWave)
                float3 Nwavem = normalize(mul(nBump, i.TBNWave));
                float3 normapMap1 = normalize(mul(n3, i.TBNWave));
                
                // --- 3. 处理双面渲染 (gl_FrontFacing 对应 SV_IsFrontFace) ---
                float eta = 1.0 / 1.33;
                if (!facing_front) {
                    Nwavem = -Nwavem;
                    normapMap1 = -normapMap1;
                    eta = 1.33;
                }

                // --- 4. 光照计算 (Diffuse / Specular) ---
                float3 R_light = normalize(reflect(-L, Nwavem));
                float diffuse = max(dot(Nwavem, L), 0.0);
                float specular = pow(max(dot(R_light, V), 0.0), 10.0);
                
                // --- 5. 颜色与菲涅尔 (Fresnel) ---
                float facing_val = 1.0 - max(0.0, dot(V, Nwavem));
                float3 colorDeep = float3(0.0, 0.0, 0.1);
                float3 colorShallow = float3(0.0, 0.05, 0.05);
                float4 waterColor = float4(lerp(colorDeep, colorShallow, facing_val), 1.0);

                float R0 = 0.02037;
                float fresnel = R0 + (1.0 - R0) * pow(1.0 - max(0.0, dot(V, Nwavem)), 5.0);

                // --- 6. 反射与折射 (Cubemap) ---
                float3 Reflectionwave = reflect(-V, Nwavem);
                // Unity 中采样 Cubemap 使用 texCUBE
                float4 ReflectionColor = texCUBE(_Cubemap, Reflectionwave) * fresnel;

                float3 Refractionwave = refract(-V, Nwavem, eta);
                float4 RefractionColor = texCUBE(_Cubemap, Refractionwave) * (1.0 - fresnel);
                // return waterColor;
                float factor= 1.0 + saturate(abs(i.test) * 3.0) * 0.35;
                return (waterColor+ReflectionColor+RefractionColor)*(factor);
                // float debugHeight = i.worldPos.z; // 假设 Z 是高度
                // return float4(i.test, 0, 0, 1);
                // --- 7. 输出最终颜色 ---
                // return RefractionColor + ReflectionColor + waterColor + (specular * _LightColor0);
            }
            ENDCG
        }
    }
}
