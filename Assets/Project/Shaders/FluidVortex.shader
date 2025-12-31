Shader "DECKADENCE/Background/FluidVortex"
{
    Properties
    {
        [Header(Palette)]
        _ColorVoid ("Void Background", Color) = (0.02, 0.01, 0.04, 1)
        _Color1 ("Layer 1 Dark", Color) = (0.12, 0.05, 0.22, 1)
        _Color2 ("Layer 2 Purple", Color) = (0.28, 0.1, 0.42, 1)
        _Color3 ("Layer 3 Cyan", Color) = (0.1, 0.38, 0.52, 1)
        
        [Header(Island Shape)]
        _CellScale ("Cell Scale", Float) = 6.0
        _IslandSize ("Island Size", Range(0.1, 0.9)) = 0.5
        _RaggedAmount ("Ragged Edges", Range(0, 1)) = 0.5
        _RaggedScale ("Ragged Scale", Float) = 12.0
        _EdgeSharpness ("Edge Sharpness", Float) = 30.0
        
        [Header(Fluid Distortion)]
        _FluidStrength ("Fluid Distortion", Range(0, 1)) = 0.4
        
        [Header(Vortex)]
        _SwirlStrength ("Swirl Strength", Float) = 4.0
        _Speed ("Animation Speed", Float) = 0.3
        
        [Header(Layer Separation)]
        _LayerSpread ("Layer Spread", Float) = 0.12
        
        [Header(Tint Filter)]
        _FilterColor ("Overlay Color", Color) = (0,0,0,0)
        
        [Header(Pixelation)]
        _Pixels ("Pixel Density", Float) = 400.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Background" "RenderPipeline" = "UniversalPipeline" }
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 positionHCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _ColorVoid;
                float4 _Color1;
                float4 _Color2;
                float4 _Color3;
                
                float _CellScale;
                float _IslandSize;
                float _RaggedAmount;
                float _RaggedScale;
                float _EdgeSharpness;
                
                float _FluidStrength;
                
                float _SwirlStrength;
                float _Speed;
                
                float _LayerSpread;
                
                float4 _FilterColor;
                float _Pixels;
            CBUFFER_END

            // ==========================================
            // UTILITY
            // ==========================================
            
            float2x2 Rot(float a) 
            {
                float s = sin(a);
                float c = cos(a);
                return float2x2(c, -s, s, c);
            }
            
            // Hash functions
            float Hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }
            
            float2 Hash2(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)),
                           dot(p, float2(269.5, 183.3)));
                return frac(sin(p) * 43758.5453);
            }
            
            // Value noise
            float Noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                
                float a = Hash(i);
                float b = Hash(i + float2(1, 0));
                float c = Hash(i + float2(0, 1));
                float d = Hash(i + float2(1, 1));
                
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }
            
            // FBM for ragged edges
            float FBM(float2 p, float time)
            {
                float value = 0.0;
                float amplitude = 0.5;
                
                for(int i = 0; i < 4; i++)
                {
                    value += amplitude * Noise(p + time * 0.2);
                    amplitude *= 0.5;
                    p *= 2.0;
                }
                
                return value;
            }
            
            // Fluid simulation for distortion - smooth waves like Balatro
            float2 FluidDistort(float2 uv, float time, float strength)
            {
                // Slow, gentle waves - no aggressive spinning
                float wave1 = sin(uv.y * 3.0 + time * 0.3) * cos(uv.x * 2.0 + time * 0.2);
                float wave2 = sin(uv.x * 2.5 - time * 0.25) * cos(uv.y * 1.8 + time * 0.35);
                float wave3 = sin((uv.x + uv.y) * 1.5 + time * 0.15);
                
                // Gentle breathing motion
                float breath = sin(time * 0.4) * 0.3 + 0.7;
                
                float2 offset;
                offset.x = (wave1 * 0.5 + wave3 * 0.3) * strength * breath;
                offset.y = (wave2 * 0.5 + wave3 * 0.3) * strength * breath;
                
                return uv + offset * 0.15;
            }

            // ==========================================
            // VORONOI NOISE - creates cell/island pattern
            // ==========================================
            
            float2 Voronoi(float2 p, float time)
            {
                float2 ip = floor(p);
                float2 fp = frac(p);
                
                float d1 = 8.0;
                float d2 = 8.0;
                
                for(int j = -1; j <= 1; j++)
                {
                    for(int i = -1; i <= 1; i++)
                    {
                        float2 offset = float2(i, j);
                        float2 cellId = ip + offset;
                        
                        float2 randomOffset = Hash2(cellId);
                        randomOffset = 0.5 + 0.4 * sin(time * 0.5 + 6.28318 * randomOffset);
                        
                        float2 cellPoint = offset + randomOffset - fp;
                        float dist = length(cellPoint);
                        
                        if(dist < d1)
                        {
                            d2 = d1;
                            d1 = dist;
                        }
                        else if(dist < d2)
                        {
                            d2 = dist;
                        }
                    }
                }
                
                return float2(d1, d2);
            }
            
            // Get island mask from Voronoi with ragged edges
            float GetIslandMask(float2 uv, float time, float threshold, float sharpness, float raggedAmount, float raggedScale)
            {
                // Add FBM distortion for ragged shapes
                float2 distortedUV = uv;
                float fbm1 = FBM(uv * raggedScale * 0.3, time);
                float fbm2 = FBM(uv * raggedScale * 0.3 + 100.0, time * 1.3);
                distortedUV += float2(fbm1 - 0.5, fbm2 - 0.5) * raggedAmount * 0.4;
                
                float2 vor = Voronoi(distortedUV, time);
                
                float edge = vor.y - vor.x;
                float island = 1.0 - vor.x;
                float pattern = island * 0.7 + edge * 0.3;
                
                // Ragged threshold
                float edgeNoise = FBM(uv * raggedScale, time * 0.7);
                float raggedThreshold = threshold + (edgeNoise - 0.5) * raggedAmount * 0.5;
                
                float mask = smoothstep(raggedThreshold - 1.0/sharpness, raggedThreshold + 1.0/sharpness, pattern);
                
                return mask;
            }

            // ==========================================
            // VERTEX SHADER
            // ==========================================
            
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            // ==========================================
            // FRAGMENT SHADER
            // ==========================================
            
            half4 frag(Varyings IN) : SV_Target
            {
                // 1. UV SETUP
                float aspect = _ScreenParams.x / _ScreenParams.y;
                float2 uv = IN.uv - 0.5;
                uv.x *= aspect;
                
                float p = _Pixels;
                uv = floor(uv * p) / p; 
                
                float time = _Time.y * _Speed;
                float len = length(uv);
                
                // 2. APPLY VORTEX TWIST
                float twist = log(len * 8.0 + 1.0) * _SwirlStrength;
                float2 twistedUV = mul(Rot(twist - time * 0.5), uv);
                
                // 3. APPLY FLUID DISTORTION
                float2 fluidUV = FluidDistort(twistedUV, time, _FluidStrength);
                
                // 4. SCALE FOR CELLS
                float2 cellUV = fluidUV * _CellScale;
                
                // 5. GET ISLAND MASKS FOR EACH LAYER
                float threshold = 1.0 - _IslandSize;
                
                float island1 = GetIslandMask(cellUV, time, threshold, _EdgeSharpness, _RaggedAmount, _RaggedScale);
                float island2 = GetIslandMask(cellUV + 50.0, time + 1.0, threshold + _LayerSpread, _EdgeSharpness, _RaggedAmount, _RaggedScale);
                float island3 = GetIslandMask(cellUV + 100.0, time + 2.0, threshold + _LayerSpread * 2.0, _EdgeSharpness, _RaggedAmount, _RaggedScale);
                
                // 6. COMPOSITE LAYERS
                float4 baseColor = _ColorVoid;
                
                baseColor = lerp(baseColor, _Color1, island1);
                baseColor = lerp(baseColor, _Color2, island2);
                baseColor = lerp(baseColor, _Color3, island3);
                
                // 7. DITHERING
                float dither = frac(sin(dot(IN.uv * 1000.0, float2(12.9898, 78.233))) * 43758.5453);
                baseColor.rgb += (dither - 0.5) * 0.01;
                
                // 8. TINT FILTER
                baseColor.rgb = lerp(baseColor.rgb, _FilterColor.rgb, _FilterColor.a);
                
                return baseColor;
            }
            ENDHLSL
        }
    }
}