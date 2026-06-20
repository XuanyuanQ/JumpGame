Shader "Custom/PhyllotaxisBloom"
{
    Properties
    {
        _Speed ("Growth Speed", Range(0.1, 2.0)) = 0.25
        _BaseColor ("Base Color", Color) = (0.15, 0.15, 0.4, 1.0)
        _Brightness ("Brightness", Range(0.5, 3.0)) = 1.5
    }
    SubShader
    {
        // 开启透明混合，这样没撞击到花朵的地方就是透明的
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off // 双面渲染，防止进到方块内部就看不见了

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1; // 传递像素的世界坐标
            };

            float _Speed;
            float4 _BaseColor;
            float _Brightness;

            #define PI 3.14159265359

            // --- 基础数学工具 ---
            float2 hash2(float2 p) {
                return frac(sin(float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)))) * 43758.5453);
            }

            float voronoi(float2 x) {
                float2 cell = floor(x);
                float d = 1e12;
                for(int j=-1; j<=1; j++)
                for(int i=-1; i<=1; i++) {
                    float2 offset = float2(float(i), float(j));
                    float2 pos = hash2(cell + offset);
                    float2 r = cell + offset + pos;
                    d = min(d, length(x - r));
                }
                return d;
            }

            void pR(inout float2 p, float a) {
                float s = sin(a); float c = cos(a);
                p = mul(p, float2x2(c, s, -s, c));
            }

            float smin(float a, float b, float k) {
                float f = clamp(0.5 + 0.5 * ((a - b) / k), 0.0, 1.0);
                return (1.0 - f) * a + f * b - f * (1.0 - f) * k;
            }

            float smax(float a, float b, float k) { return -smin(-a, -b, k); }

            struct Model {
                float d; float2 uv; float2 cell; float wedge; float llen;
            };

            // --- 核心建模算法 ---
            Model leaf(float3 p, float3 cellData) {
                float2 cell = cellData.xy;
                float cellTime = max(cellData.z, 0.0);
                
                pR(p.xz, -cell.x);
                pR(p.zy, cell.y);
                float3 pp = p;

                float len = pow(max(cellTime * 3.0 - 0.2, 0.0), 0.33);
                float llen = len;

                Model m; m.d = 1e12; m.uv = 0; m.cell = 0; m.wedge = 0; m.llen = 0;

                if (cellTime > 0.0) {
                    float ins = 0.25;
                    p.z += ins;
                    float3 n = normalize(float3(1, 0, 0.35));
                    float wedge = -dot(p, n);
                    wedge = max(wedge, dot(p, n * float3(1, 1, -1)));
                    wedge = smax(wedge, p.z - len * 1.12 - ins, len);
                    p.z -= ins;

                    float curve = smoothstep(0.0, 0.2, cellTime);
                    len *= lerp(1.5, 0.65, curve);
                    pR(p.zy, -lerp(0.2, 0.7, curve));
                    
                    float d2 = abs(length(p - float3(0, len, 0)) - len) - 0.05;
                    m.d = smax(d2, wedge, 0.05);
                    m.uv = pp.xz/llen; m.cell = cell; m.wedge = wedge; m.llen = llen;
                }
                return m;
            }

            float3 calcCellData(float2 cell, float2 offset, float stretch, float t) {
                float maxBloomOffset = PI / 2.0;
                float sz = maxBloomOffset + PI / 2.0;
                float2 cc = float2(5.0, 8.0);
                float aa = atan2(cc.x, cc.y);
                float scale = (PI * 2.0) / sqrt(dot(cc, cc));
                float2x2 mRot = float2x2(cos(aa), -sin(aa), sin(aa), cos(aa));
                float2x2 trans = mRot * (1.0 / scale);
                trans = mul(trans, float2x2(1, 0, 0, stretch));
                float det = trans[0][0]*trans[1][1] - trans[0][1]*trans[1][0];
                float2x2 transI = float2x2(trans[1][1], -trans[0][1], -trans[1][0], trans[0][0]) * (1.0 / det);

                cell = mul(trans, cell);
                cell = round(cell) + offset;
                float2 cellRaw = mul(transI, cell);
                float y = cellRaw.y * (stretch / sz);
                float cellAppearTime = (0.25 - y) / (0.25 - 1.0);
                return float3(cellRaw.x, cellRaw.y - maxBloomOffset, t - cellAppearTime);
            }

            Model map(float3 p, float time) {
                pR(p.xy, time * -PI);
                float side = sign(p.y);
                p.y = abs(p.y); p.z *= side;
                float t = sin((time + 0.5 * side) * PI - PI/2.0) * 0.5 + 0.5;
                float stretch = lerp(0.25, 1.0, t);
                float2 polar = float2(atan2(p.x, p.z), atan2(p.y, length(p.xz)) + (PI/2.0));
                
                Model res; res.d = 1e12;
                for(int m=-1; m<=1; m++)
                for(int n=-1; n<=1; n++) {
                    Model o = leaf(p, calcCellData(polar, float2(m, n), stretch, t));
                    if (o.d < res.d) res = o;
                }
                return res;
            }

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                // 核心：计算本地空间射线
                float3 worldCamPos = _WorldSpaceCameraPos;
                float3 worldRayDir = normalize(i.worldPos - worldCamPos);
                
                float3 ro = mul(unity_WorldToObject, float4(worldCamPos, 1.0)).xyz;
                float3 rd = normalize(mul((float3x3)unity_WorldToObject, worldRayDir));

                float time = frac(_Time.y * _Speed);
                float d = 0;
                Model m;
                float3 p = ro;

                // Raymarching
                bool hit = false;
                for(int j=0; j<64; j++) {
                    m = map(p, time);
                    if (m.d < 0.001) { hit = true; break; }
                    d += m.d;
                    p = ro + rd * d;
                    if (d > 10.0) break;
                }

                if (!hit) return float4(0, 0, 0, 0); // 没撞到返回透明

                // 着色
                float3 col = _BaseColor.rgb;
                float v = voronoi(m.uv * 10.0);
                col *= (1.0 - v * 0.5) * _Brightness;
                col *= (1.0 - d * 0.2); // 简单的深度阴影

                return float4(col, 1.0);
            }
            ENDCG
        }
    }
}