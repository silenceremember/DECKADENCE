Shader "DECKADENCE/UI/MultiBubble"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        [Header(Rect Info)]
        _RectSize ("Rect Size", Vector) = (100, 100, 0, 0)
        _CanvasScale ("Canvas Scale", Float) = 1
        
        [Header(Corner and Tear Settings)]
        _CornerCutMinPixels ("Corner Cut Min (Pixels)", Float) = 15
        _CornerCutMaxPixels ("Corner Cut Max (Pixels)", Float) = 25
        _TearDepthMinPixels ("Tear Depth Min (Pixels)", Float) = 5
        _TearDepthMaxPixels ("Tear Depth Max (Pixels)", Float) = 15
        _TearWidthMinPixels ("Tear Width Min (Pixels)", Float) = 10
        _TearWidthMaxPixels ("Tear Width Max (Pixels)", Float) = 30
        _TearSpacingMinPixels ("Tear Spacing Min (Pixels)", Float) = 40
        _TearSpacingMaxPixels ("Tear Spacing Max (Pixels)", Float) = 80
        _TearSeed ("Random Seed", Float) = 0
        _AnimSpeed ("Animation Speed", Float) = 1
        
        [Header(Arrow)]
        _ArrowPerimeter ("Arrow Perimeter Position", Range(0, 1)) = 0
        
        [Header(Light Direction)]
        _LightDirection ("Light Direction", Vector) = (1, -1, 0, 0)
        
        // Stencil for UI masking
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
        
        Pass
        {
            Name "Default"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest [unity_GUIZTestMode]
            Cull Off
            ColorMask [_ColorMask]
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 uv1 : TEXCOORD1;  // x: layerIndex, y: quadType (0=fill, 1=shadow1, 2=shadow2)
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float layerIndex : TEXCOORD1;
                float quadType : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
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
                float _ArrowPerimeter;
                float4 _LightDirection;
                
                // Per-layer data passed via vertex color + additional channels
                // Layer data is encoded in the mesh and passed through uniforms
                
                // Layer 0
                float _L0_Enabled;
                float _L0_Offset;
                float _L0_Cutout;
                float _L0_CutoutPadding;
                float4 _L0_FillColor;
                float _L0_ShowArrow;
                float _L0_ArrowSize;
                float _L0_ArrowWidth;
                float _L0_ShowBorder;
                float4 _L0_BorderColor;
                float _L0_BorderThickness;
                float _L0_BorderOffset;
                float _L0_BorderStyle;
                float _L0_DashLength;
                float _L0_DashGap;
                float4 _L0_ShadowColor;
                float _L0_ShadowIntensity;
                float _L0_ShowSecondShadow;
                float4 _L0_SecondShadowColor;
                float _L0_SecondShadowIntensity;
                float _L0_ShowInnerShadow;
                float _L0_InnerShadowIntensity;
                float4 _L0_InnerShadowColor;
                float4 _L0_Tears; // top, bottom, left, right
                
                // Layer 1
                float _L1_Enabled;
                float _L1_Offset;
                float _L1_Cutout;
                float _L1_CutoutPadding;
                float4 _L1_FillColor;
                float _L1_ShowArrow;
                float _L1_ArrowSize;
                float _L1_ArrowWidth;
                float _L1_ShowBorder;
                float4 _L1_BorderColor;
                float _L1_BorderThickness;
                float _L1_BorderOffset;
                float _L1_BorderStyle;
                float _L1_DashLength;
                float _L1_DashGap;
                float4 _L1_ShadowColor;
                float _L1_ShadowIntensity;
                float _L1_ShowSecondShadow;
                float4 _L1_SecondShadowColor;
                float _L1_SecondShadowIntensity;
                float _L1_ShowInnerShadow;
                float _L1_InnerShadowIntensity;
                float4 _L1_InnerShadowColor;
                float4 _L1_Tears;
                
                // Layer 2
                float _L2_Enabled;
                float _L2_Offset;
                float _L2_Cutout;
                float _L2_CutoutPadding;
                float4 _L2_FillColor;
                float _L2_ShowArrow;
                float _L2_ArrowSize;
                float _L2_ArrowWidth;
                float _L2_ShowBorder;
                float4 _L2_BorderColor;
                float _L2_BorderThickness;
                float _L2_BorderOffset;
                float _L2_BorderStyle;
                float _L2_DashLength;
                float _L2_DashGap;
                float4 _L2_ShadowColor;
                float _L2_ShadowIntensity;
                float _L2_ShowSecondShadow;
                float4 _L2_SecondShadowColor;
                float _L2_SecondShadowIntensity;
                float _L2_ShowInnerShadow;
                float _L2_InnerShadowIntensity;
                float4 _L2_InnerShadowColor;
                float4 _L2_Tears;
                
                // Layer 3
                float _L3_Enabled;
                float _L3_Offset;
                float _L3_Cutout;
                float _L3_CutoutPadding;
                float4 _L3_FillColor;
                float _L3_ShowArrow;
                float _L3_ArrowSize;
                float _L3_ArrowWidth;
                float _L3_ShowBorder;
                float4 _L3_BorderColor;
                float _L3_BorderThickness;
                float _L3_BorderOffset;
                float _L3_BorderStyle;
                float _L3_DashLength;
                float _L3_DashGap;
                float4 _L3_ShadowColor;
                float _L3_ShadowIntensity;
                float _L3_ShowSecondShadow;
                float4 _L3_SecondShadowColor;
                float _L3_SecondShadowIntensity;
                float _L3_ShowInnerShadow;
                float _L3_InnerShadowIntensity;
                float4 _L3_InnerShadowColor;
                float4 _L3_Tears;
                
                float _LayerCount;
            CBUFFER_END
            
            // ==========================================
            // UTILITY FUNCTIONS
            // ==========================================
            
            float Hash(float n)
            {
                return frac(sin(n * 127.1) * 43758.5453);
            }
            
            // Check if inside a triangular tooth notch
            float IsInsideToothNotch(float edgePos, float distFromEdge, float toothCenter, float seed,
                                      float minWidth, float maxWidth, float minDepth, float maxDepth)
            {
                float r1 = Hash(toothCenter + seed);
                float r3 = Hash(toothCenter + seed + 200.0);
                float r4 = Hash(toothCenter + seed + 300.0);
                float r5 = Hash(toothCenter + seed + 400.0);
                
                float width = lerp(minWidth, maxWidth, r3);
                float depth = lerp(minDepth, maxDepth, r4);
                float skew = lerp(-0.3, 0.3, r5);
                
                float halfWidth = width * 0.5;
                float distFromCenter = edgePos - toothCenter;
                
                float skewedDist = distFromCenter + skew * distFromEdge;
                
                if (abs(skewedDist) > halfWidth) return 0.0;
                if (distFromEdge > depth) return 0.0;
                
                float normalizedDist = abs(skewedDist) / halfWidth;
                float cutoffDepth = depth * (1.0 - normalizedDist);
                return step(distFromEdge, cutoffDepth);
            }
            
            // Calculate edge tear
            float CalculateEdgeTear(float edgePos, float distFromEdge, float seed, float intensity,
                                     float minWidth, float maxWidth, float minDepth, float maxDepth,
                                     float edgeLength, float spacingMin, float spacingMax)
            {
                if (intensity < 0.01) return 0.0;
                
                float result = 0.0;
                float currentPos = 0.0;
                
                for (int i = 0; i < 16; i++)
                {
                    float spacingRandom = Hash(float(i) + seed + 5000.0);
                    float spacing = lerp(spacingMin, spacingMax, spacingRandom);
                    currentPos += spacing;
                    
                    if (currentPos > edgeLength) break;
                    
                    result = max(result, IsInsideToothNotch(
                        edgePos, distFromEdge, currentPos, seed,
                        minWidth, maxWidth, minDepth * intensity, maxDepth * intensity
                    ));
                }
                
                return result;
            }
            
            // Check if inside arrow triangle - FULL VERSION with corner handling
            // Works in CANVAS UNITS for fixed pixel size
            // Arrow rotates smoothly at corners, stays perpendicular on edges
            // Base vertices are clamped to rect bounds to prevent levitation
            float IsInsideArrowCanvas(float2 localPos, float perimeter, float2 rectSize, float size, float width)
            {
                // Shift perimeter for position calculation
                float p = frac(perimeter + 0.125);
                
                // Define corner transition zone
                float cornerSize = 0.03;
                
                // Calculate base position and edge-perpendicular direction
                float2 basePos;
                float2 edgeDir;
                
                // Edge directions
                float2 dirDown = float2(0.0, -1.0);
                float2 dirRight = float2(1.0, 0.0);
                float2 dirUp = float2(0.0, 1.0);
                float2 dirLeft = float2(-1.0, 0.0);
                
                // Diagonal directions for corners (45 degrees)
                float2 diagDownLeft = normalize(dirDown + dirLeft);
                float2 diagDownRight = normalize(dirDown + dirRight);
                float2 diagUpRight = normalize(dirUp + dirRight);
                float2 diagUpLeft = normalize(dirUp + dirLeft);
                
                if (p < 0.25) // Bottom edge
                {
                    float t = p / 0.25;
                    basePos = float2(t * rectSize.x, 0.0);
                    
                    if (p < cornerSize)
                    {
                        float blend = p / cornerSize;
                        blend = smoothstep(0.0, 1.0, blend);
                        edgeDir = lerp(diagDownLeft, dirDown, blend);
                    }
                    else if (p > 0.25 - cornerSize)
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
                    
                    if (p < 0.25 + cornerSize)
                    {
                        float blend = (p - 0.25) / cornerSize;
                        blend = smoothstep(0.0, 1.0, blend);
                        edgeDir = lerp(diagDownRight, dirRight, blend);
                    }
                    else if (p > 0.5 - cornerSize)
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
                    
                    if (p < 0.5 + cornerSize)
                    {
                        float blend = (p - 0.5) / cornerSize;
                        blend = smoothstep(0.0, 1.0, blend);
                        edgeDir = lerp(diagUpRight, dirUp, blend);
                    }
                    else if (p > 0.75 - cornerSize)
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
                    
                    if (p < 0.75 + cornerSize)
                    {
                        float blend = (p - 0.75) / cornerSize;
                        blend = smoothstep(0.0, 1.0, blend);
                        edgeDir = lerp(diagUpLeft, dirLeft, blend);
                    }
                    else if (p > 1.0 - cornerSize)
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
                
                // Normalize direction
                float2 dir = normalize(edgeDir);
                
                // Arrow tip extends outward from base position
                float2 arrowTip = basePos + dir * size;
                
                // Calculate perpendicular direction for arrow width
                float2 perp = float2(-dir.y, dir.x);
                
                // Compensate width for diagonal arrows
                float diagonalFactor = abs(dir.x * dir.y) * 2.0;
                float widthCompensation = 1.0 + diagonalFactor * 0.414;
                
                float halfWidth = width * 0.5 * widthCompensation;
                float2 baseLeft = basePos - perp * halfWidth;
                float2 baseRight = basePos + perp * halfWidth;
                
                // Clamp base vertices to rectangle bounds
                float2 baseLeftClamped = clamp(baseLeft, float2(0, 0), rectSize);
                float2 baseRightClamped = clamp(baseRight, float2(0, 0), rectSize);
                
                // Triangle point-in-triangle test
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
                
                // Corner fill: fill the wedge between clamped and unclamped bases
                // ONLY fill pixels that are inside the rect AND in front of the arrow base
                float inCornerFill = 0.0;
                float2 clampDiff = abs(baseLeft - baseLeftClamped) + abs(baseRight - baseRightClamped);
                if (clampDiff.x + clampDiff.y > 0.01)
                {
                    bool insideRectBounds = localPos.x >= 0.0 && localPos.x <= rectSize.x &&
                                            localPos.y >= 0.0 && localPos.y <= rectSize.y;
                    
                    if (insideRectBounds)
                    {
                        float2 toPoint = localPos - basePos;
                        
                        // Check if point is in front of the base (not behind, which would be outside rect)
                        // For corner arrows, we need to ensure we're filling INSIDE the rect
                        float alongDir = dot(toPoint, dir);
                        
                        // Only fill if:
                        // 1. Point is BEHIND the arrow direction (alongDir <= 0, meaning inside rect)
                        // 2. Point is within the arrow width zone
                        // 3. Point is close enough to the corner
                        if (alongDir <= 0.0)
                        {
                            float perpProj = abs(dot(toPoint, perp));
                            float distFromBase = length(toPoint);
                            
                            if (perpProj < halfWidth && distFromBase < halfWidth * 1.5)
                            {
                                inCornerFill = 1.0;
                            }
                        }
                    }
                }
                
                return max(inTriangle, inCornerFill);
            }
            
            // ==========================================
            // LAYER DATA ACCESSOR
            // ==========================================
            
            struct LayerData
            {
                float enabled;
                float offset;
                float cutout;
                float cutoutPadding;
                float4 fillColor;
                float showArrow;
                float arrowSize;
                float arrowWidth;
                float showBorder;
                float4 borderColor;
                float borderThickness;
                float borderOffset;
                float borderStyle;
                float dashLength;
                float dashGap;
                float4 shadowColor;
                float shadowIntensity;
                float showSecondShadow;
                float4 secondShadowColor;
                float secondShadowIntensity;
                float showInnerShadow;
                float innerShadowIntensity;
                float4 innerShadowColor;
                float4 tears;
            };
            
            LayerData GetLayerData(int index)
            {
                LayerData data;
                
                if (index == 0)
                {
                    data.enabled = _L0_Enabled;
                    data.offset = _L0_Offset;
                    data.cutout = _L0_Cutout;
                    data.cutoutPadding = _L0_CutoutPadding;
                    data.fillColor = _L0_FillColor;
                    data.showArrow = _L0_ShowArrow;
                    data.arrowSize = _L0_ArrowSize;
                    data.arrowWidth = _L0_ArrowWidth;
                    data.showBorder = _L0_ShowBorder;
                    data.borderColor = _L0_BorderColor;
                    data.borderThickness = _L0_BorderThickness;
                    data.borderOffset = _L0_BorderOffset;
                    data.borderStyle = _L0_BorderStyle;
                    data.dashLength = _L0_DashLength;
                    data.dashGap = _L0_DashGap;
                    data.shadowColor = _L0_ShadowColor;
                    data.shadowIntensity = _L0_ShadowIntensity;
                    data.showSecondShadow = _L0_ShowSecondShadow;
                    data.secondShadowColor = _L0_SecondShadowColor;
                    data.secondShadowIntensity = _L0_SecondShadowIntensity;
                    data.showInnerShadow = _L0_ShowInnerShadow;
                    data.innerShadowIntensity = _L0_InnerShadowIntensity;
                    data.innerShadowColor = _L0_InnerShadowColor;
                    data.tears = _L0_Tears;
                }
                else if (index == 1)
                {
                    data.enabled = _L1_Enabled;
                    data.offset = _L1_Offset;
                    data.cutout = _L1_Cutout;
                    data.cutoutPadding = _L1_CutoutPadding;
                    data.fillColor = _L1_FillColor;
                    data.showArrow = _L1_ShowArrow;
                    data.arrowSize = _L1_ArrowSize;
                    data.arrowWidth = _L1_ArrowWidth;
                    data.showBorder = _L1_ShowBorder;
                    data.borderColor = _L1_BorderColor;
                    data.borderThickness = _L1_BorderThickness;
                    data.borderOffset = _L1_BorderOffset;
                    data.borderStyle = _L1_BorderStyle;
                    data.dashLength = _L1_DashLength;
                    data.dashGap = _L1_DashGap;
                    data.shadowColor = _L1_ShadowColor;
                    data.shadowIntensity = _L1_ShadowIntensity;
                    data.showSecondShadow = _L1_ShowSecondShadow;
                    data.secondShadowColor = _L1_SecondShadowColor;
                    data.secondShadowIntensity = _L1_SecondShadowIntensity;
                    data.showInnerShadow = _L1_ShowInnerShadow;
                    data.innerShadowIntensity = _L1_InnerShadowIntensity;
                    data.innerShadowColor = _L1_InnerShadowColor;
                    data.tears = _L1_Tears;
                }
                else if (index == 2)
                {
                    data.enabled = _L2_Enabled;
                    data.offset = _L2_Offset;
                    data.cutout = _L2_Cutout;
                    data.cutoutPadding = _L2_CutoutPadding;
                    data.fillColor = _L2_FillColor;
                    data.showArrow = _L2_ShowArrow;
                    data.arrowSize = _L2_ArrowSize;
                    data.arrowWidth = _L2_ArrowWidth;
                    data.showBorder = _L2_ShowBorder;
                    data.borderColor = _L2_BorderColor;
                    data.borderThickness = _L2_BorderThickness;
                    data.borderOffset = _L2_BorderOffset;
                    data.borderStyle = _L2_BorderStyle;
                    data.dashLength = _L2_DashLength;
                    data.dashGap = _L2_DashGap;
                    data.shadowColor = _L2_ShadowColor;
                    data.shadowIntensity = _L2_ShadowIntensity;
                    data.showSecondShadow = _L2_ShowSecondShadow;
                    data.secondShadowColor = _L2_SecondShadowColor;
                    data.secondShadowIntensity = _L2_SecondShadowIntensity;
                    data.showInnerShadow = _L2_ShowInnerShadow;
                    data.innerShadowIntensity = _L2_InnerShadowIntensity;
                    data.innerShadowColor = _L2_InnerShadowColor;
                    data.tears = _L2_Tears;
                }
                else
                {
                    data.enabled = _L3_Enabled;
                    data.offset = _L3_Offset;
                    data.cutout = _L3_Cutout;
                    data.cutoutPadding = _L3_CutoutPadding;
                    data.fillColor = _L3_FillColor;
                    data.showArrow = _L3_ShowArrow;
                    data.arrowSize = _L3_ArrowSize;
                    data.arrowWidth = _L3_ArrowWidth;
                    data.showBorder = _L3_ShowBorder;
                    data.borderColor = _L3_BorderColor;
                    data.borderThickness = _L3_BorderThickness;
                    data.borderOffset = _L3_BorderOffset;
                    data.borderStyle = _L3_BorderStyle;
                    data.dashLength = _L3_DashLength;
                    data.dashGap = _L3_DashGap;
                    data.shadowColor = _L3_ShadowColor;
                    data.shadowIntensity = _L3_ShadowIntensity;
                    data.showSecondShadow = _L3_ShowSecondShadow;
                    data.secondShadowColor = _L3_SecondShadowColor;
                    data.secondShadowIntensity = _L3_SecondShadowIntensity;
                    data.showInnerShadow = _L3_ShowInnerShadow;
                    data.innerShadowIntensity = _L3_InnerShadowIntensity;
                    data.innerShadowColor = _L3_InnerShadowColor;
                    data.tears = _L3_Tears;
                }
                
                return data;
            }
            
            // ==========================================
            // COMPUTE LAYER VISIBILITY
            // ==========================================
            
            float ComputeLayerVisibility(float2 localPos, float2 layerSize, float seed, float canvasScale,
                                          float4 tearIntensity, float showArrow, float arrowSize, float arrowWidth)
            {
                float cornerCutMin = _CornerCutMinPixels / canvasScale;
                float cornerCutMax = _CornerCutMaxPixels / canvasScale;
                float tearDepthMin = _TearDepthMinPixels / canvasScale;
                float tearDepthMax = _TearDepthMaxPixels / canvasScale;
                float tearWidthMin = _TearWidthMinPixels / canvasScale;
                float tearWidthMax = _TearWidthMaxPixels / canvasScale;
                float tearSpacingMin = _TearSpacingMinPixels / canvasScale;
                float tearSpacingMax = _TearSpacingMaxPixels / canvasScale;
                float arrowSizeCanvas = arrowSize / canvasScale;
                float arrowWidthCanvas = arrowWidth / canvasScale;
                
                // Normalized UV
                float2 uv = localPos / layerSize;
                float normX = uv.x;
                float normY = uv.y;
                
                // Inside rect check
                float insideRect = step(0.0, uv.x) * step(uv.x, 1.0) * 
                                   step(0.0, uv.y) * step(uv.y, 1.0);
                
                // Distances from edges
                float distBottom = localPos.y;
                float distTop = layerSize.y - localPos.y;
                float distLeft = localPos.x;
                float distRight = layerSize.x - localPos.x;
                
                // Arrow setup
                float insideArrow = 0.0;
                int edgeInt = 0;
                float arrowPosOnEdge = 0.0;
                float inArrowZoneX = 0.0;
                float inArrowZoneY = 0.0;
                
                if (showArrow > 0.5)
                {
                    insideArrow = IsInsideArrowCanvas(localPos, _ArrowPerimeter, layerSize, arrowSizeCanvas, arrowWidthCanvas);
                    
                    // Determine arrow edge and position
                    float p = frac(_ArrowPerimeter + 0.125);
                    
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
                        arrowPosOnEdge = 1.0 - (p - 0.5) / 0.25;
                    }
                    else
                    {
                        edgeInt = 2;  // Left
                        arrowPosOnEdge = 1.0 - (p - 0.75) / 0.25;
                    }
                    
                    // Calculate arrow zone
                    float arrowWidthNorm = arrowWidthCanvas / max((edgeInt == 0 || edgeInt == 1) ? layerSize.x : layerSize.y, 1.0);
                    float arrowHalfWidthNorm = arrowWidthNorm * 0.5;
                    float arrowZoneLeft = arrowPosOnEdge - arrowHalfWidthNorm;
                    float arrowZoneRight = arrowPosOnEdge + arrowHalfWidthNorm;
                    
                    inArrowZoneX = step(arrowZoneLeft, normX) * step(normX, arrowZoneRight);
                    inArrowZoneY = step(arrowZoneLeft, normY) * step(normY, arrowZoneRight);
                }
                
                // Corner cuts
                float blCut = lerp(cornerCutMin, cornerCutMax, Hash(seed + 500.0));
                float brCut = lerp(cornerCutMin, cornerCutMax, Hash(seed + 600.0));
                float tlCut = lerp(cornerCutMin, cornerCutMax, Hash(seed + 700.0));
                float trCut = lerp(cornerCutMin, cornerCutMax, Hash(seed + 800.0));
                
                // Apply corner cuts, but skip where arrow connects
                float cornerMask = 1.0;
                
                // BL corner
                float skipBLPixel = (edgeInt == 0 && inArrowZoneX > 0.5 && normX < 0.5) ||
                                    (edgeInt == 2 && inArrowZoneY > 0.5 && normY < 0.5) ? 1.0 : 0.0;
                if (skipBLPixel < 0.5) cornerMask *= step(blCut, distLeft + distBottom);
                
                // BR corner
                float skipBRPixel = (edgeInt == 0 && inArrowZoneX > 0.5 && normX > 0.5) ||
                                    (edgeInt == 3 && inArrowZoneY > 0.5 && normY < 0.5) ? 1.0 : 0.0;
                if (skipBRPixel < 0.5) cornerMask *= step(brCut, distRight + distBottom);
                
                // TL corner
                float skipTLPixel = (edgeInt == 1 && inArrowZoneX > 0.5 && normX < 0.5) ||
                                    (edgeInt == 2 && inArrowZoneY > 0.5 && normY > 0.5) ? 1.0 : 0.0;
                if (skipTLPixel < 0.5) cornerMask *= step(tlCut, distLeft + distTop);
                
                // TR corner
                float skipTRPixel = (edgeInt == 1 && inArrowZoneX > 0.5 && normX > 0.5) ||
                                    (edgeInt == 3 && inArrowZoneY > 0.5 && normY > 0.5) ? 1.0 : 0.0;
                if (skipTRPixel < 0.5) cornerMask *= step(trCut, distRight + distTop);
                
                float visible = max(insideRect * cornerMask, insideArrow);
                
                // Tears - skip on edge where arrow is attached
                float2 clampedPos = clamp(localPos, float2(0, 0), layerSize);
                
                float tearTop = 0.0;
                float tearBottom = 0.0;
                float tearLeft = 0.0;
                float tearRight = 0.0;
                
                // Bottom edge (arrow edge 0)
                if (edgeInt != 0 || inArrowZoneX < 0.5)
                {
                    tearBottom = CalculateEdgeTear(clampedPos.x, distBottom, seed + 1000.0, tearIntensity.y,
                                                    tearWidthMin, tearWidthMax, tearDepthMin, tearDepthMax,
                                                    layerSize.x, tearSpacingMin, tearSpacingMax);
                }
                
                // Top edge (arrow edge 1)
                if (edgeInt != 1 || inArrowZoneX < 0.5)
                {
                    tearTop = CalculateEdgeTear(clampedPos.x, distTop, seed, tearIntensity.x,
                                                 tearWidthMin, tearWidthMax, tearDepthMin, tearDepthMax,
                                                 layerSize.x, tearSpacingMin, tearSpacingMax);
                }
                
                // Left edge (arrow edge 2)
                if (edgeInt != 2 || inArrowZoneY < 0.5)
                {
                    tearLeft = CalculateEdgeTear(clampedPos.y, distLeft, seed + 2000.0, tearIntensity.z,
                                                  tearWidthMin, tearWidthMax, tearDepthMin, tearDepthMax,
                                                  layerSize.y, tearSpacingMin, tearSpacingMax);
                }
                
                // Right edge (arrow edge 3)
                if (edgeInt != 3 || inArrowZoneY < 0.5)
                {
                    tearRight = CalculateEdgeTear(clampedPos.y, distRight, seed + 3000.0, tearIntensity.w,
                                                   tearWidthMin, tearWidthMax, tearDepthMin, tearDepthMax,
                                                   layerSize.y, tearSpacingMin, tearSpacingMax);
                }
                
                float shouldCut = max(max(tearTop, tearBottom), max(tearLeft, tearRight));
                if (insideArrow > 0.5) shouldCut = 0.0; // Protect arrow
                
                visible *= (1.0 - shouldCut);
                
                return visible;
            }
            
            // ==========================================
            // VERTEX SHADER
            // ==========================================
            
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color * _Color;
                OUT.layerIndex = IN.uv1.x;
                OUT.quadType = IN.uv1.y;
                
                return OUT;
            }
            
            // ==========================================
            // FRAGMENT SHADER
            // ==========================================
            
            half4 frag(Varyings IN) : SV_Target
            {
                float2 rectSize = _RectSize.xy;
                float canvasScale = max(_CanvasScale, 0.001);
                float2 localPos = IN.uv * rectSize;
                
                int layerIdx = int(IN.layerIndex + 0.5);
                int quadType = int(IN.quadType + 0.5); // 0=fill, 1=primary shadow, 2=secondary shadow
                
                LayerData layer = GetLayerData(layerIdx);
                
                if (layer.enabled < 0.5) discard;
                
                // Calculate layer size with offset
                float offsetCanvas = layer.offset / canvasScale;
                float2 layerSize = rectSize + float2(offsetCanvas * 2.0, offsetCanvas * 2.0);
                float2 layerLocalPos = localPos + float2(offsetCanvas, offsetCanvas);
                
                // Animation seed
                float steppedTime = floor(_Time.y * _AnimSpeed);
                float seed = _TearSeed + steppedTime * 17.31 + float(layerIdx) * 50000.0;
                
                // Compute visibility for this layer
                float visible = ComputeLayerVisibility(layerLocalPos, layerSize, seed, canvasScale,
                                                        layer.tears, layer.showArrow, layer.arrowSize, layer.arrowWidth);
                
                // Apply cutout if enabled (cut out the next layer's area)
                if (layer.cutout > 0.5 && layerIdx < int(_LayerCount) - 1)
                {
                    LayerData nextLayer = GetLayerData(layerIdx + 1);
                    float nextOffsetCanvas = nextLayer.offset / canvasScale;
                    float cutoutPaddingCanvas = layer.cutoutPadding / canvasScale;
                    float2 cutoutSize = rectSize + float2((nextOffsetCanvas - cutoutPaddingCanvas) * 2.0, (nextOffsetCanvas - cutoutPaddingCanvas) * 2.0);
                    float2 cutoutLocalPos = localPos + float2(nextOffsetCanvas - cutoutPaddingCanvas, nextOffsetCanvas - cutoutPaddingCanvas);
                    
                    float nextSeed = _TearSeed + steppedTime * 17.31 + float(layerIdx + 1) * 50000.0;
                    float cutoutVisible = ComputeLayerVisibility(cutoutLocalPos, cutoutSize, nextSeed, canvasScale,
                                                                  nextLayer.tears, nextLayer.showArrow, nextLayer.arrowSize, nextLayer.arrowWidth);
                    visible *= (1.0 - cutoutVisible);
                }
                
                if (visible < 0.01) discard;
                
                // Render based on quad type
                if (quadType == 2) // Secondary shadow
                {
                    half4 result;
                    result.rgb = layer.secondShadowColor.rgb;
                    result.a = visible * layer.secondShadowColor.a * IN.color.a;
                    return result;
                }
                else if (quadType == 1) // Primary shadow
                {
                    half4 result;
                    result.rgb = layer.shadowColor.rgb;
                    result.a = visible * layer.shadowColor.a * IN.color.a;
                    return result;
                }
                else // Fill (quadType == 0)
                {
                    // Multiply layer fill color with vertex color (from Image.color)
                    float3 color = layer.fillColor.rgb * IN.color.rgb;
                    
                    // Border
                    if (layer.showBorder > 0.5)
                    {
                        float borderThickness = layer.borderThickness / canvasScale;
                        float borderOffset = layer.borderOffset / canvasScale;
                        
                        float distBottom = layerLocalPos.y;
                        float distTop = layerSize.y - layerLocalPos.y;
                        float distLeft = layerLocalPos.x;
                        float distRight = layerSize.x - layerLocalPos.x;
                        float minDist = min(min(distBottom, distTop), min(distLeft, distRight));
                        
                        float inBorder = step(borderOffset, minDist) * step(minDist, borderOffset + borderThickness);
                        color = lerp(color, layer.borderColor.rgb, inBorder * layer.borderColor.a);
                    }
                    
                    // Inner shadow
                    if (layer.showInnerShadow > 0.5)
                    {
                        float2 shadowOffset = _LightDirection.xy / canvasScale * layer.innerShadowIntensity;
                        float2 shadowPos = layerLocalPos - shadowOffset;
                        
                        float distBottom = shadowPos.y;
                        float distTop = layerSize.y - shadowPos.y;
                        float distLeft = shadowPos.x;
                        float distRight = layerSize.x - shadowPos.x;
                        float minDist = min(min(distBottom, distTop), min(distLeft, distRight));
                        
                        float borderOffset = layer.borderOffset / canvasScale;
                        float inShadow = step(0.0, minDist) * step(minDist, borderOffset);
                        color = lerp(color, layer.innerShadowColor.rgb, inShadow * layer.innerShadowColor.a);
                    }
                    
                    half4 result;
                    result.rgb = color;
                    result.a = visible * layer.fillColor.a * IN.color.a;
                    return result;
                }
            }
            ENDHLSL
        }
    }
    
    FallBack "UI/Default"
}
