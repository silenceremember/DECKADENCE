Shader "RoyalLeech/UI/CardShadow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        [Header(Shadow Settings)]
        [HideInInspector] _ShadowColor ("Shadow Color", Color) = (0, 0, 0, 0.5)
        
        [Header(Corner Cut)]
        _CornerCutMinPixels ("Corner Cut Min (Pixels)", Float) = 15
        _CornerCutMaxPixels ("Corner Cut Max (Pixels)", Float) = 25
        
        [Header(Tear Settings)]
        _TearDepthMinPixels ("Tear Depth Min (Pixels)", Float) = 5
        _TearDepthMaxPixels ("Tear Depth Max (Pixels)", Float) = 15
        _TearWidthMinPixels ("Tear Width Min (Pixels)", Float) = 10
        _TearWidthMaxPixels ("Tear Width Max (Pixels)", Float) = 30
        _TearSpacingMinPixels ("Tear Spacing Min (Pixels)", Float) = 40
        _TearSpacingMaxPixels ("Tear Spacing Max (Pixels)", Float) = 80
        _TearSeed ("Random Seed", Float) = 0
        
        [Header(Animation)]
        _AnimSpeed ("Frames Per Second", Float) = 1.0
        
        [Header(Per Edge Tear Intensity)]
        _TopTear ("Top Edge", Range(0, 1)) = 0.5
        _BottomTear ("Bottom Edge", Range(0, 1)) = 0.5
        _LeftTear ("Left Edge", Range(0, 1)) = 0.5
        _RightTear ("Right Edge", Range(0, 1)) = 0.5
        
        [Header(Canvas Info)]
        [HideInInspector] _RectSize ("Rect Size", Vector) = (100, 100, 0, 0)
        [HideInInspector] _CanvasScale ("Canvas Scale", Float) = 1.0
        
        [Header(Pixelation)]
        _PixelSizePixels ("Pixel Size (Screen Pixels)", Float) = 0
        
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
            
            // Explicit point sampler - NO interpolation for alpha or color
            SAMPLER(my_point_clamp_sampler);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float4 _ShadowColor;
                float4 _RectSize;
                float _CanvasScale;
                float _CornerCutMinPixels;
                float _CornerCutMaxPixels;
                float _TearDepthMinPixels;
                float _TearDepthMaxPixels;
                float _TearWidthMinPixels;
                float _TearWidthMaxPixels;
                float _TearSpacingMinPixels;
                float _TearSpacingMaxPixels;
                float _TearSeed;
                float _AnimSpeed;
                float _TopTear;
                float _BottomTear;
                float _LeftTear;
                float _RightTear;
                float _PixelSizePixels;
            CBUFFER_END
            
            // Hash function for randomness
            float Hash(float n)
            {
                return frac(sin(n * 127.1) * 43758.5453);
            }
            
            // Check if point is inside a triangular tooth notch at a specific position
            // toothCenter is the center position of this tooth along the edge (in canvas units)
            float IsInsideToothNotch(float edgePos, float distFromEdge, float toothCenter, float seed,
                                      float minWidth, float maxWidth, float minDepth, float maxDepth)
            {
                // Random parameters for this tooth based on its center position
                float r1 = Hash(toothCenter + seed);
                float r3 = Hash(toothCenter + seed + 200.0);
                float r4 = Hash(toothCenter + seed + 300.0);
                float r5 = Hash(toothCenter + seed + 400.0);
                
                // Should this tooth exist? (~60% chance)
                if (r1 < 0.4) return 0.0;
                
                // Random width and depth (already in canvas units)
                float toothWidth = lerp(minWidth, maxWidth, r3);
                float toothDepth = lerp(minDepth, maxDepth, r4);
                
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
            
            // Calculate total tear for an edge using spacing-based tooth placement
            // Teeth appear every spacingMin to spacingMax canvas units
            float CalculateEdgeTear(float edgePos, float distFromEdge, float seed, float intensity,
                                    float minWidth, float maxWidth, float minDepth, float maxDepth, 
                                    float edgeLength, float spacingMin, float spacingMax)
            {
                if (intensity < 0.01) return 0.0;
                
                float result = 0.0;
                float currentPos = 0.0;
                
                // Maximum 16 teeth to prevent infinite loops
                for (int i = 0; i < 16; i++)
                {
                    // Random spacing for this tooth
                    float spacingRandom = Hash(float(i) + seed + 5000.0);
                    float spacing = lerp(spacingMin, spacingMax, spacingRandom);
                    
                    // Position this tooth
                    currentPos += spacing;
                    
                    // Stop if we've gone past the edge
                    if (currentPos > edgeLength) break;
                    
                    // Check if point is inside this tooth
                    result = max(result, IsInsideToothNotch(
                        edgePos, 
                        distFromEdge, 
                        currentPos,
                        seed,
                        minWidth,
                        maxWidth,
                        minDepth * intensity,
                        maxDepth * intensity
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
                
                float2 rectSize = _RectSize.xy;
                float canvasScale = max(_CanvasScale, 0.001);
                
                // Convert UV to local position in canvas units FIRST
                float2 localPos = IN.uv * rectSize;
                
                // Apply pixelation in canvas space (uniform square pixels)
                // _PixelSizePixels is the size of each pixel in SCREEN PIXELS
                // Convert to canvas units for uniform pixelation
                float2 pixelUV = IN.uv;
                float pixelSizeCanvas = _PixelSizePixels / canvasScale;
                if (pixelSizeCanvas > 0.001)
                {
                    // Pixelate in canvas space
                    localPos = (floor(localPos / pixelSizeCanvas) + 0.5) * pixelSizeCanvas;
                    
                    // Convert back to UV for compatibility
                    pixelUV = localPos / rectSize;
                }
                
                // Sample texture
                half4 texColor;
                if (_PixelSizePixels > 0)
                {
                    texColor = SAMPLE_TEXTURE2D(_MainTex, my_point_clamp_sampler, pixelUV);
                }
                else
                {
                    texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, pixelUV);
                }
                
                // Convert pixel values to canvas units using scale factor
                float cornerCutMinCanvas = _CornerCutMinPixels / canvasScale;
                float cornerCutMaxCanvas = _CornerCutMaxPixels / canvasScale;
                float tearDepthMinCanvas = _TearDepthMinPixels / canvasScale;
                float tearDepthMaxCanvas = _TearDepthMaxPixels / canvasScale;
                float tearWidthMinCanvas = _TearWidthMinPixels / canvasScale;
                float tearWidthMaxCanvas = _TearWidthMaxPixels / canvasScale;
                float tearSpacingMinCanvas = _TearSpacingMinPixels / canvasScale;
                float tearSpacingMaxCanvas = _TearSpacingMaxPixels / canvasScale;
                
                // Calculate distances from edges (in canvas units)
                float distBottom = localPos.y;
                float distTop = rectSize.y - localPos.y;
                float distLeft = localPos.x;
                float distRight = rectSize.x - localPos.x;
                
                // Random corner cut sizes
                float blCut = lerp(cornerCutMinCanvas, cornerCutMaxCanvas, Hash(seed + 500.0));
                float brCut = lerp(cornerCutMinCanvas, cornerCutMaxCanvas, Hash(seed + 600.0));
                float tlCut = lerp(cornerCutMinCanvas, cornerCutMaxCanvas, Hash(seed + 700.0));
                float trCut = lerp(cornerCutMinCanvas, cornerCutMaxCanvas, Hash(seed + 800.0));
                
                // Apply corner cuts
                float cornerMask = 1.0;
                cornerMask *= step(blCut, distLeft + distBottom);
                cornerMask *= step(brCut, distRight + distBottom);
                cornerMask *= step(tlCut, distLeft + distTop);
                cornerMask *= step(trCut, distRight + distTop);
                
                // Calculate tears from each edge (using canvas units)
                float tearTop = CalculateEdgeTear(localPos.x, distTop, seed, _TopTear,
                                                  tearWidthMinCanvas, tearWidthMaxCanvas,
                                                  tearDepthMinCanvas, tearDepthMaxCanvas, 
                                                  rectSize.x, tearSpacingMinCanvas, tearSpacingMaxCanvas);
                
                float tearBottom = CalculateEdgeTear(localPos.x, distBottom, seed + 1000.0, _BottomTear,
                                                     tearWidthMinCanvas, tearWidthMaxCanvas,
                                                     tearDepthMinCanvas, tearDepthMaxCanvas, 
                                                     rectSize.x, tearSpacingMinCanvas, tearSpacingMaxCanvas);
                
                float tearLeft = CalculateEdgeTear(localPos.y, distLeft, seed + 2000.0, _LeftTear,
                                                   tearWidthMinCanvas, tearWidthMaxCanvas,
                                                   tearDepthMinCanvas, tearDepthMaxCanvas, 
                                                   rectSize.y, tearSpacingMinCanvas, tearSpacingMaxCanvas);
                
                float tearRight = CalculateEdgeTear(localPos.y, distRight, seed + 3000.0, _RightTear,
                                                    tearWidthMinCanvas, tearWidthMaxCanvas,
                                                    tearDepthMinCanvas, tearDepthMaxCanvas, 
                                                    rectSize.y, tearSpacingMinCanvas, tearSpacingMaxCanvas);
                
                // Combine tears - if any tear applies, cut the pixel
                float shouldCut = max(max(tearTop, tearBottom), max(tearLeft, tearRight));
                
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
