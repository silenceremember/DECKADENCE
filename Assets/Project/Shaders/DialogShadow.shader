Shader "DECKADENCE/UI/DialogShadow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Fill Color", Color) = (1,1,1,1)
        
        [Header(Shadow Settings)]
        [HideInInspector] _ShadowColor ("Shadow Color", Color) = (0, 0, 0, 0.5)
        
        [Header(Arrow Settings)]
        _ArrowPerimeter ("Arrow Position on Perimeter (0-1)", Range(0, 1)) = 0
        _ArrowSizePixels ("Arrow Size (Screen Pixels)", Float) = 30
        _ArrowWidthPixels ("Arrow Width (Screen Pixels)", Float) = 40
        
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
            Name "DialogShadow"
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
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
                float4 _RectSize;
                float _CanvasScale;
                float _ArrowPerimeter;
                float _ArrowSizePixels;
                float _ArrowWidthPixels;
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
                
                // Use abs(distFromEdge) to cut on BOTH sides of the edge:
                // - Positive distFromEdge: cuts INTO the rectangle
                // - Negative distFromEdge: cuts INTO the arrow (mirrored triangle)
                // This creates a symmetric notch that affects both rect and arrow
                return step(abs(distFromEdge), maxDepthAtPos);
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
            
            // Check if point is inside the arrow triangle
            // Works in CANVAS UNITS for fixed pixel size (like tears and corners)
            // localPos: position in canvas units
            // rectSize: rectangle size in canvas units
            // size, width: arrow dimensions in canvas units
            // Arrow rotates ONLY at corners, stays perpendicular on edges
            float IsInsideArrowCanvas(float2 localPos, float perimeter, float2 rectSize, float size, float width)
            {
                // Shift perimeter for position calculation
                // So that 0 = center of bottom edge, 0.25 = center of right edge, etc.
                float p = frac(perimeter + 0.125);
                
                // Define corner transition zone (how much of the edge is "corner")
                float cornerSize = 0.03;  // Small zone at each corner
                
                // Calculate base position IN CANVAS UNITS and edge-perpendicular direction
                float2 basePos;
                float2 edgeDir;  // Direction perpendicular to edge (outward)
                
                // Edge directions: down, right, up, left
                float2 dirDown = float2(0.0, -1.0);
                float2 dirRight = float2(1.0, 0.0);
                float2 dirUp = float2(0.0, 1.0);
                float2 dirLeft = float2(-1.0, 0.0);
                
                // Diagonal directions for corners (45 degrees)
                float2 diagDownLeft = normalize(dirDown + dirLeft);   // Corner at p=0
                float2 diagDownRight = normalize(dirDown + dirRight); // Corner at p=0.25
                float2 diagUpRight = normalize(dirUp + dirRight);     // Corner at p=0.5
                float2 diagUpLeft = normalize(dirUp + dirLeft);       // Corner at p=0.75
                
                // Corners are at p = 0, 0.25, 0.5, 0.75 (after the +0.125 shift)
                
                if (p < 0.25) // Bottom edge
                {
                    float t = p / 0.25;
                    basePos = float2(t * rectSize.x, 0.0);
                    
                    if (p < cornerSize) // Near left corner (p=0)
                    {
                        float blend = p / cornerSize;
                        blend = smoothstep(0.0, 1.0, blend);
                        edgeDir = lerp(diagDownLeft, dirDown, blend);
                    }
                    else if (p > 0.25 - cornerSize) // Near right corner (p=0.25)
                    {
                        float blend = (0.25 - p) / cornerSize;
                        blend = smoothstep(0.0, 1.0, blend);
                        edgeDir = lerp(diagDownRight, dirDown, blend);
                    }
                    else
                    {
                        edgeDir = dirDown;
                    }
                }
                else if (p < 0.5) // Right edge
                {
                    float t = (p - 0.25) / 0.25;
                    basePos = float2(rectSize.x, t * rectSize.y);
                    
                    if (p < 0.25 + cornerSize) // Near bottom-right corner (p=0.25)
                    {
                        float blend = (p - 0.25) / cornerSize;
                        blend = smoothstep(0.0, 1.0, blend);
                        edgeDir = lerp(diagDownRight, dirRight, blend);
                    }
                    else if (p > 0.5 - cornerSize) // Near top-right corner (p=0.5)
                    {
                        float blend = (0.5 - p) / cornerSize;
                        blend = smoothstep(0.0, 1.0, blend);
                        edgeDir = lerp(diagUpRight, dirRight, blend);
                    }
                    else
                    {
                        edgeDir = dirRight;
                    }
                }
                else if (p < 0.75) // Top edge
                {
                    float t = 1.0 - (p - 0.5) / 0.25;
                    basePos = float2(t * rectSize.x, rectSize.y);
                    
                    if (p < 0.5 + cornerSize) // Near top-right corner (p=0.5)
                    {
                        float blend = (p - 0.5) / cornerSize;
                        blend = smoothstep(0.0, 1.0, blend);
                        edgeDir = lerp(diagUpRight, dirUp, blend);
                    }
                    else if (p > 0.75 - cornerSize) // Near top-left corner (p=0.75)
                    {
                        float blend = (0.75 - p) / cornerSize;
                        blend = smoothstep(0.0, 1.0, blend);
                        edgeDir = lerp(diagUpLeft, dirUp, blend);
                    }
                    else
                    {
                        edgeDir = dirUp;
                    }
                }
                else // Left edge
                {
                    float t = 1.0 - (p - 0.75) / 0.25;
                    basePos = float2(0.0, t * rectSize.y);
                    
                    if (p < 0.75 + cornerSize) // Near top-left corner (p=0.75)
                    {
                        float blend = (p - 0.75) / cornerSize;
                        blend = smoothstep(0.0, 1.0, blend);
                        edgeDir = lerp(diagUpLeft, dirLeft, blend);
                    }
                    else if (p > 1.0 - cornerSize) // Near bottom-left corner (p=1.0/0)
                    {
                        float blend = (1.0 - p) / cornerSize;
                        blend = smoothstep(0.0, 1.0, blend);
                        edgeDir = lerp(diagDownLeft, dirLeft, blend);
                    }
                    else
                    {
                        edgeDir = dirLeft;
                    }
                }
                
                // Normalize direction (in case lerp denormalized it)
                float2 dir = normalize(edgeDir);
                
                // Arrow tip extends outward from base position (in canvas units)
                float2 arrowTip = basePos + dir * size;
                
                // Calculate perpendicular direction for arrow width
                float2 perp = float2(-dir.y, dir.x);
                
                // Compensate width for diagonal arrows (at corners)
                // When rotated 45°, the perpendicular extends diagonally and gets clipped
                // Increase width by sqrt(2) ≈ 1.414 to maintain visual width
                float diagonalFactor = abs(dir.x * dir.y) * 2.0;  // 0 on edges, 1 at 45°
                float widthCompensation = 1.0 + diagonalFactor * 0.414;  // 1.0 to 1.414
                
                float halfWidth = width * 0.5 * widthCompensation;
                float2 baseLeft = basePos - perp * halfWidth;
                float2 baseRight = basePos + perp * halfWidth;
                
                // Clamp base vertices to rectangle bounds
                // This ensures the arrow connects to the rect even at corners
                float2 baseLeftClamped = clamp(baseLeft, float2(0, 0), rectSize);
                float2 baseRightClamped = clamp(baseRight, float2(0, 0), rectSize);
                
                // Triangle point-in-triangle test (all in canvas units)
                float2 v0 = baseRightClamped - arrowTip;
                float2 v1 = baseLeftClamped - arrowTip;
                float2 v2 = localPos - arrowTip;
                
                float dot00 = dot(v0, v0);
                float dot01 = dot(v0, v1);
                float dot02 = dot(v0, v2);
                float dot11 = dot(v1, v1);
                float dot12 = dot(v1, v2);
                
                float invDenom = 1.0 / (dot00 * dot11 - dot01 * dot01 + 0.0001);
                float u = (dot11 * dot02 - dot01 * dot12) * invDenom;
                float v = (dot00 * dot12 - dot01 * dot02) * invDenom;
                
                float inTriangle = (u >= 0.0) && (v >= 0.0) && (u + v <= 1.0) ? 1.0 : 0.0;
                
                // Additional check: fill the wedge between clamped and unclamped bases
                // This fills the corner area when arrow is rotated
                float inCornerFill = 0.0;
                
                // Check if we need corner fill (base vertices were clamped)
                float2 clampDiff = abs(baseLeft - baseLeftClamped) + abs(baseRight - baseRightClamped);
                if (clampDiff.x + clampDiff.y > 0.01)
                {
                    // IMPORTANT: Corner fill only inside the rectangle bounds!
                    bool insideRectBounds = localPos.x >= 0.0 && localPos.x <= rectSize.x &&
                                            localPos.y >= 0.0 && localPos.y <= rectSize.y;
                    
                    if (insideRectBounds)
                    {
                        // Check distance from corner point
                        float distFromCorner = length(localPos - basePos);
                        if (distFromCorner < halfWidth * 1.5)
                        {
                            // Check if within the angular spread
                            float2 toPoint = localPos - basePos;
                            float perpProj = abs(dot(toPoint, perp));
                            if (perpProj < halfWidth)
                            {
                                inCornerFill = 1.0;
                            }
                        }
                    }
                }
                
                return max(inTriangle, inCornerFill);
            }
            
            half4 frag(Varyings IN) : SV_Target
            {
                // Stepped time for animation (like CardShadow)
                float steppedTime = floor(_Time.y * _AnimSpeed);
                float seed = _TearSeed + steppedTime * 17.31;
                
                float2 rectSize = _RectSize.xy;
                float canvasScale = max(_CanvasScale, 0.001);
                
                // Convert UV to local position in canvas units FIRST
                float2 localPos = IN.uv * rectSize;
                
                // Apply pixelation in canvas space (uniform square pixels)
                // _PixelSizePixels is the size of each pixel in SCREEN PIXELS
                // Convert to canvas units for uniform pixelation across rect+arrow
                float2 pixelUV = IN.uv;
                float pixelSizeCanvas = _PixelSizePixels / canvasScale;
                if (pixelSizeCanvas > 0.001)
                {
                    // Pixelate in canvas space - works correctly for both rect and arrow
                    // Pixel size is fixed in screen pixels, converted to canvas units
                    localPos = (floor(localPos / pixelSizeCanvas) + 0.5) * pixelSizeCanvas;
                    
                    // Convert back to UV for compatibility
                    pixelUV = localPos / rectSize;
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
                
                // Convert arrow pixel values to canvas units
                float arrowSizeCanvas = _ArrowSizePixels / canvasScale;
                float arrowWidthCanvas = _ArrowWidthPixels / canvasScale;
                
                // Determine which edge based on perimeter position
                // After +0.125 shift: 0.0 = center bottom, 0.25 = center right, etc.
                float p = frac(_ArrowPerimeter + 0.125);
                int edgeInt;
                float arrowPosOnEdge;  // 0-1 position along that edge
                
                if (p < 0.25) 
                {
                    edgeInt = 0;  // Bottom
                    arrowPosOnEdge = p / 0.25;
                }
                else if (p < 0.5)
                {
                    edgeInt = 3;  // Right
                    arrowPosOnEdge = (p - 0.25) / 0.25;
                }
                else if (p < 0.75)
                {
                    edgeInt = 1;  // Top
                    arrowPosOnEdge = 1.0 - (p - 0.5) / 0.25;  // Reversed
                }
                else
                {
                    edgeInt = 2;  // Left
                    arrowPosOnEdge = 1.0 - (p - 0.75) / 0.25;  // Reversed
                }
                
                // Arrow dimensions are already in canvas units (arrowSizeCanvas, arrowWidthCanvas)
                // No normalization needed - arrow size is fixed in pixels like tears and corners
                
                // Check if inside main rectangle (UV 0-1 range)
                float insideRect = step(0.0, pixelUV.x) * step(pixelUV.x, 1.0) * 
                                   step(0.0, pixelUV.y) * step(pixelUV.y, 1.0);
                
                // Check if inside arrow (using canvas units for fixed pixel size)
                float insideArrow = IsInsideArrowCanvas(localPos, _ArrowPerimeter, rectSize, arrowSizeCanvas, arrowWidthCanvas);
                
                // Calculate distances from edges (in canvas units)
                float distBottom = localPos.y;
                float distTop = rectSize.y - localPos.y;
                float distLeft = localPos.x;
                float distRight = rectSize.x - localPos.x;
                
                // Normalized position for arrow zone checks
                float normX = pixelUV.x;  // 0-1 along width
                float normY = pixelUV.y;  // 0-1 along height
                
                // Arrow connection zone (using calculated position on edge)
                // Normalize arrow width for zone calculations (need normalized for UV-based checks)
                float arrowWidthNorm = arrowWidthCanvas / max((edgeInt == 0 || edgeInt == 1) ? rectSize.x : rectSize.y, 1.0);
                float arrowHalfWidthNorm = arrowWidthNorm * 0.5;
                float arrowZoneLeft = arrowPosOnEdge - arrowHalfWidthNorm;
                float arrowZoneRight = arrowPosOnEdge + arrowHalfWidthNorm;
                
                // Check if current pixel is in arrow zone for each relevant edge
                float inArrowZoneX = step(arrowZoneLeft, normX) * step(normX, arrowZoneRight);
                float inArrowZoneY = step(arrowZoneLeft, normY) * step(normY, arrowZoneRight);
                
                // Random corner cut sizes
                float blCut = lerp(cornerCutMinCanvas, cornerCutMaxCanvas, Hash(seed + 500.0));
                float brCut = lerp(cornerCutMinCanvas, cornerCutMaxCanvas, Hash(seed + 600.0));
                float tlCut = lerp(cornerCutMinCanvas, cornerCutMaxCanvas, Hash(seed + 700.0));
                float trCut = lerp(cornerCutMinCanvas, cornerCutMaxCanvas, Hash(seed + 800.0));
                
                // Apply corner cuts, but skip the corner for pixels in arrow connection zone
                // Same logic as tears - per-pixel skip based on arrow zone
                float cornerMask = 1.0;
                
                // BL corner: skip if arrow on bottom edge AND pixel in arrow zone (near left)
                // OR if arrow on left edge AND pixel in arrow zone (near bottom)
                float skipBLPixel = (edgeInt == 0 && inArrowZoneX > 0.5 && normX < 0.5) ||
                                    (edgeInt == 2 && inArrowZoneY > 0.5 && normY < 0.5) ? 1.0 : 0.0;
                if (skipBLPixel < 0.5) cornerMask *= step(blCut, distLeft + distBottom);
                
                // BR corner: skip if arrow on bottom edge AND pixel in arrow zone (near right)
                // OR if arrow on right edge AND pixel in arrow zone (near bottom)
                float skipBRPixel = (edgeInt == 0 && inArrowZoneX > 0.5 && normX > 0.5) ||
                                    (edgeInt == 3 && inArrowZoneY > 0.5 && normY < 0.5) ? 1.0 : 0.0;
                if (skipBRPixel < 0.5) cornerMask *= step(brCut, distRight + distBottom);
                
                // TL corner: skip if arrow on top edge AND pixel in arrow zone (near left)
                // OR if arrow on left edge AND pixel in arrow zone (near top)
                float skipTLPixel = (edgeInt == 1 && inArrowZoneX > 0.5 && normX < 0.5) ||
                                    (edgeInt == 2 && inArrowZoneY > 0.5 && normY > 0.5) ? 1.0 : 0.0;
                if (skipTLPixel < 0.5) cornerMask *= step(tlCut, distLeft + distTop);
                
                // TR corner: skip if arrow on top edge AND pixel in arrow zone (near right)
                // OR if arrow on right edge AND pixel in arrow zone (near top)
                float skipTRPixel = (edgeInt == 1 && inArrowZoneX > 0.5 && normX > 0.5) ||
                                    (edgeInt == 3 && inArrowZoneY > 0.5 && normY > 0.5) ? 1.0 : 0.0;
                if (skipTRPixel < 0.5) cornerMask *= step(trCut, distRight + distTop);
                
                // Combine: rect with corners + arrow on top
                float visible = max(insideRect * cornerMask, insideArrow);
                
                // Apply tear effects treating rect+arrow as ONE mesh
                // Don't apply tears on the edge where arrow connects (within arrow width zone)
                float2 clampedPos = clamp(localPos, float2(0, 0), rectSize);
                
                // Calculate tears from each edge
                // Skip tears on the edge where arrow is attached (mask out arrow zone)
                float tearTop = 0.0;
                float tearBottom = 0.0;
                float tearLeft = 0.0;
                float tearRight = 0.0;
                
                // Bottom edge (arrow edge 0)
                if (edgeInt != 0 || inArrowZoneX < 0.5)
                {
                    tearBottom = CalculateEdgeTear(clampedPos.x, distBottom, seed + 1000.0, _BottomTear,
                                                    tearWidthMinCanvas, tearWidthMaxCanvas,
                                                    tearDepthMinCanvas, tearDepthMaxCanvas, 
                                                    rectSize.x, tearSpacingMinCanvas, tearSpacingMaxCanvas);
                }
                
                // Top edge (arrow edge 1)
                if (edgeInt != 1 || inArrowZoneX < 0.5)
                {
                    tearTop = CalculateEdgeTear(clampedPos.x, distTop, seed, _TopTear,
                                                 tearWidthMinCanvas, tearWidthMaxCanvas,
                                                 tearDepthMinCanvas, tearDepthMaxCanvas, 
                                                 rectSize.x, tearSpacingMinCanvas, tearSpacingMaxCanvas);
                }
                
                // Left edge (arrow edge 2)
                if (edgeInt != 2 || inArrowZoneY < 0.5)
                {
                    tearLeft = CalculateEdgeTear(clampedPos.y, distLeft, seed + 2000.0, _LeftTear,
                                                  tearWidthMinCanvas, tearWidthMaxCanvas,
                                                  tearDepthMinCanvas, tearDepthMaxCanvas, 
                                                  rectSize.y, tearSpacingMinCanvas, tearSpacingMaxCanvas);
                }
                
                // Right edge (arrow edge 3)
                if (edgeInt != 3 || inArrowZoneY < 0.5)
                {
                    tearRight = CalculateEdgeTear(clampedPos.y, distRight, seed + 3000.0, _RightTear,
                                                   tearWidthMinCanvas, tearWidthMaxCanvas,
                                                   tearDepthMinCanvas, tearDepthMaxCanvas, 
                                                   rectSize.y, tearSpacingMinCanvas, tearSpacingMaxCanvas);
                }
                
                // Combine tears - if any tear applies, cut the pixel
                // BUT never cut the arrow itself!
                float shouldCut = max(max(tearTop, tearBottom), max(tearLeft, tearRight));
                if (insideArrow > 0.5)
                {
                    shouldCut = 0.0;  // Protect arrow from ALL tears
                }
                visible *= (1.0 - shouldCut);
                
                // Final alpha
                float finalAlpha = visible * IN.color.a;
                
                // Shadow or main rendering
                if (IN.isShadow > 0.5)
                {
                    half4 result;
                    result.rgb = _ShadowColor.rgb;
                    result.a = finalAlpha * _ShadowColor.a;
                    return result;
                }
                else
                {
                    half4 color;
                    color.rgb = IN.color.rgb;
                    color.a = finalAlpha;
                    return color;
                }
            }
            ENDHLSL
        }
    }
    
    FallBack "UI/Default"
}
