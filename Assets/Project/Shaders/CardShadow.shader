Shader "RoyalLeech/UI/CardShadow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        [Header(Shadow Settings)]
        [HideInInspector] _ShadowColor ("Shadow Color", Color) = (0, 0, 0, 0.5)
        
        [Header(Tear Settings)]
        _TearDepth ("Max Tear Depth", Range(0, 0.4)) = 0.15
        _TearSeed ("Random Seed", Float) = 0
        
        [Header(Teeth Settings)]
        _TeethCount ("Max Teeth Per Edge", Range(1, 8)) = 3
        _TeethMinWidth ("Min Width", Range(0.02, 0.2)) = 0.05
        _TeethMaxWidth ("Max Width", Range(0.05, 0.4)) = 0.15
        
        [Header(Animation)]
        _AnimSpeed ("Frames Per Second", Float) = 1.0
        
        [Header(Per Edge Intensity)]
        _TopTear ("Top Edge", Range(0, 1)) = 0.5
        _BottomTear ("Bottom Edge", Range(0, 1)) = 0.5
        _LeftTear ("Left Edge", Range(0, 1)) = 0.5
        _RightTear ("Right Edge", Range(0, 1)) = 0.5
        
        [Header(Corner Cut)]
        _CornerCutMin ("Corner Cut Min", Range(0, 0.2)) = 0.05
        _CornerCutMax ("Corner Cut Max", Range(0, 0.3)) = 0.12
        
        [Header(Stencil)]
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
    }
    
    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent" 
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
            "RenderPipeline"="UniversalPipeline"
        }
        
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        
        Pass
        {
            Name "CardShadow"
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1; // Shadow flag: x=1 means shadow vertex
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float isShadow : TEXCOORD1;
                float4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float4 _ShadowColor;
                float _TearDepth;
                float _TearSeed;
                float _TeethCount;
                float _TeethMinWidth;
                float _TeethMaxWidth;
                float _AnimSpeed;
                float _TopTear;
                float _BottomTear;
                float _LeftTear;
                float _RightTear;
                float _CornerCutMin;
                float _CornerCutMax;
            CBUFFER_END
            
            // Hash functions
            float Hash(float n)
            {
                return frac(sin(n * 127.1) * 43758.5453);
            }
            
            float Hash2(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }
            
            // Check if point is inside a triangle notch coming from edge
            float IsInsideToothNotch(float edgePos, float distFromEdge, float toothIndex, float seed,
                                      float minWidth, float maxWidth, float maxDepth)
            {
                // Random parameters for this tooth
                float r1 = Hash(toothIndex + seed);
                float r2 = Hash(toothIndex + seed + 100.0);
                float r3 = Hash(toothIndex + seed + 200.0);
                float r4 = Hash(toothIndex + seed + 300.0);
                float r5 = Hash(toothIndex + seed + 400.0);
                
                // Should this tooth exist? (~60% chance)
                if (r1 < 0.4) return 0.0;
                
                // Tooth center position along edge
                float toothCenter = (toothIndex + 0.5 + (r2 - 0.5) * 0.6) / _TeethCount;
                
                // Random width and depth
                float toothWidth = lerp(minWidth, maxWidth, r3);
                float toothDepth = maxDepth * (0.4 + r4 * 0.6);
                
                // Asymmetry - tip offset from center
                float tipOffset = (r5 - 0.5) * 0.6 * toothWidth;
                
                float baseLeft = toothCenter - toothWidth * 0.5;
                float baseRight = toothCenter + toothWidth * 0.5;
                float tipX = toothCenter + tipOffset;
                float tipY = toothDepth;
                
                if (edgePos < baseLeft || edgePos > baseRight) return 0.0;
                
                float maxDepthAtPos;
                
                if (edgePos < tipX)
                {
                    float t = (edgePos - baseLeft) / max(tipX - baseLeft, 0.001);
                    maxDepthAtPos = t * tipY;
                }
                else
                {
                    float t = (edgePos - tipX) / max(baseRight - tipX, 0.001);
                    maxDepthAtPos = (1.0 - t) * tipY;
                }
                
                return step(distFromEdge, maxDepthAtPos);
            }
            
            // Calculate total tear for an edge
            float CalculateEdgeTear(float edgePos, float distFromEdge, float seed, float intensity)
            {
                if (intensity < 0.01) return 0.0;
                
                float result = 0.0;
                
                for (int i = 0; i < 8; i++)
                {
                    if (float(i) >= _TeethCount) break;
                    
                    result = max(result, IsInsideToothNotch(
                        edgePos, 
                        distFromEdge, 
                        float(i), 
                        seed,
                        _TeethMinWidth,
                        _TeethMaxWidth,
                        _TearDepth * intensity
                    ));
                }
                
                return result;
            }
            
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color * _Color;
                OUT.isShadow = IN.uv1.x;
                
                return OUT;
            }
            
            half4 frag(Varyings IN) : SV_Target
            {
                // Stepped time for animation
                float steppedTime = floor(_Time.y * _AnimSpeed);
                float seed = _TearSeed + steppedTime * 17.31;
                
                // Sample sprite
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                
                // Distance from each edge
                float distTop = 1.0 - IN.uv.y;
                float distBottom = IN.uv.y;
                float distLeft = IN.uv.x;
                float distRight = 1.0 - IN.uv.x;
                
                // Check each edge for tears
                float tearTop = CalculateEdgeTear(IN.uv.x, distTop, seed, _TopTear);
                float tearBottom = CalculateEdgeTear(IN.uv.x, distBottom, seed + 1000.0, _BottomTear);
                float tearLeft = CalculateEdgeTear(IN.uv.y, distLeft, seed + 2000.0, _LeftTear);
                float tearRight = CalculateEdgeTear(IN.uv.y, distRight, seed + 3000.0, _RightTear);
                
                // Combine tears
                float shouldCut = max(max(tearTop, tearBottom), max(tearLeft, tearRight));
                
                // Corner cut
                float cornerMask = 1.0;
                
                float tlCut = lerp(_CornerCutMin, _CornerCutMax, Hash(seed + 500.0));
                float trCut = lerp(_CornerCutMin, _CornerCutMax, Hash(seed + 600.0));
                float blCut = lerp(_CornerCutMin, _CornerCutMax, Hash(seed + 700.0));
                float brCut = lerp(_CornerCutMin, _CornerCutMax, Hash(seed + 800.0));
                
                cornerMask *= step(tlCut, distLeft + distTop);
                cornerMask *= step(trCut, distRight + distTop);
                cornerMask *= step(blCut, distLeft + distBottom);
                cornerMask *= step(brCut, distRight + distBottom);
                
                // Calculate final alpha with tears and corners
                float finalAlpha = texColor.a * (1.0 - shouldCut) * cornerMask;
                
                // Shadow or main rendering
                if (IN.isShadow > 0.5)
                {
                    // Shadow vertex: use shadow color, apply tear mask
                    half4 result;
                    result.rgb = _ShadowColor.rgb;
                    result.a = finalAlpha * _ShadowColor.a * IN.color.a;
                    return result;
                }
                else
                {
                    // Main vertex: normal rendering with tear mask
                    half4 color = texColor * IN.color;
                    color.a = finalAlpha * IN.color.a;
                    return color;
                }
            }
            ENDHLSL
        }
    }
    
    Fallback "UI/Default"
}
