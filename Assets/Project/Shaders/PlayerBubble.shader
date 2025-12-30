Shader "DECKADENCE/UI/PlayerBubble"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        [Header(Rect Info)]
        _RectSize ("Rect Size", Vector) = (100, 100, 0, 0)
        _CanvasScale ("Canvas Scale", Float) = 1
        
        [Header(Split Settings)]
        _SplitAngle ("Split Angle (degrees)", Range(-90, 90)) = 15
        _SplitPosition ("Split Position", Range(0, 1)) = 0.5
        
        [Header(Left Side)]
        _LeftFillColor ("Left Fill Color", Color) = (0.2, 0.2, 0.2, 1)
        _LeftOffset ("Left Offset (X, Y)", Vector) = (0, 0, 0, 0)
        _LeftExpand ("Left Expand (width, height)", Vector) = (0, 0, 0, 0)
        _LeftCornersBLTL ("Left Corners BL/TL", Vector) = (0, 0, 0, 0)
        _LeftCornersBRTR ("Left Corners BR/TR (slide along split)", Vector) = (0, 0, 0, 0)
        
        [Header(Right Side)]
        _RightFillColor ("Right Fill Color", Color) = (0.8, 0.8, 0.8, 1)
        _RightOffset ("Right Offset (X, Y)", Vector) = (0, 0, 0, 0)
        _RightExpand ("Right Expand (width, height)", Vector) = (0, 0, 0, 0)
        _RightCornersBLTL ("Right Corners BL/TL (slide along split)", Vector) = (0, 0, 0, 0)
        _RightCornersBRTR ("Right Corners BR/TR", Vector) = (0, 0, 0, 0)
        
        [Header(Shadow)]
        _ShadowColor ("Shadow Color", Color) = (0, 0, 0, 0.5)
        _ShadowIntensity ("Shadow Intensity", Float) = 5
        
        // Stencil
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
                float4 uv1 : TEXCOORD1;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float quadType : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float4 _RectSize;
                float _CanvasScale;
                
                float _SplitAngle;
                float _SplitPosition;
                
                float4 _LeftFillColor;
                float4 _LeftOffset;
                float4 _LeftExpand;
                float4 _LeftCornersBLTL;
                float4 _LeftCornersBRTR;
                
                float4 _RightFillColor;
                float4 _RightOffset;
                float4 _RightExpand;
                float4 _RightCornersBLTL;
                float4 _RightCornersBRTR;
                
                float4 _ShadowColor;
                float _ShadowIntensity;
            CBUFFER_END
            
            // ==========================================
            // UTILITY FUNCTIONS
            // ==========================================
            
            float DistToSegment(float2 p, float2 a, float2 b)
            {
                float2 ab = b - a;
                float len = length(ab);
                if (len < 0.001) return length(p - a);
                float t = saturate(dot(p - a, ab) / (len * len));
                return length(p - (a + ab * t));
            }
            
            float IsInsideQuad(float2 p, float2 v0, float2 v1, float2 v2, float2 v3)
            {
                float2 e0 = v3 - v0; float c0 = e0.x * (p.y - v0.y) - e0.y * (p.x - v0.x);
                float2 e1 = v2 - v3; float c1 = e1.x * (p.y - v3.y) - e1.y * (p.x - v3.x);
                float2 e2 = v1 - v2; float c2 = e2.x * (p.y - v2.y) - e2.y * (p.x - v2.x);
                float2 e3 = v0 - v1; float c3 = e3.x * (p.y - v1.y) - e3.y * (p.x - v1.x);
                
                return step(0, c0) * step(0, c1) * step(0, c2) * step(0, c3);
            }
            
            float GetQuadEdgeDistance(float2 p, float2 v0, float2 v1, float2 v2, float2 v3)
            {
                float d0 = DistToSegment(p, v0, v3);
                float d1 = DistToSegment(p, v3, v2);
                float d2 = DistToSegment(p, v2, v1);
                float d3 = DistToSegment(p, v1, v0);
                return min(min(d0, d1), min(d2, d3));
            }
            
            // Get split line direction (unit vector along the diagonal)
            float2 GetSplitDirection(float angle)
            {
                float rad = radians(angle);
                // Split line goes "up" with angle tilt
                // At angle=0, direction is (0,1) - vertical
                // At angle=45, direction tilts right
                return normalize(float2(sin(rad), cos(rad)));
            }
            
            // Get base split point at bottom (y=0)
            float2 GetSplitBasePoint(float2 baseRect, float position)
            {
                return float2(baseRect.x * position, 0);
            }
            
            // Get point on split line at given distance from base
            float2 GetSplitPointAtDistance(float2 basePoint, float2 splitDir, float distance)
            {
                return basePoint + splitDir * distance;
            }
            
            // Slide a corner along the split line
            // slideAmount.x = distance along split direction
            // slideAmount.y = perpendicular offset (away from split)
            float2 ApplySplitSlide(float2 basePoint, float2 splitDir, float2 slideAmount)
            {
                float2 perpDir = float2(-splitDir.y, splitDir.x); // perpendicular to split
                return basePoint + splitDir * slideAmount.x + perpDir * slideAmount.y;
            }
            
            // ==========================================
            // VERTEX SHADER
            // ==========================================
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color * _Color;
                output.quadType = input.uv1.y;
                
                return output;
            }
            
            // ==========================================
            // FRAGMENT SHADER
            // ==========================================
            
            float4 frag(Varyings input) : SV_Target
            {
                float2 baseRect = _RectSize.xy;
                float canvasScale = max(_CanvasScale, 0.001);
                float2 localPos = input.uv * baseRect;
                // Split geometry
                float2 splitDir = GetSplitDirection(_SplitAngle);
                float2 splitBase = GetSplitBasePoint(baseRect, _SplitPosition);
                float splitLength = baseRect.y / max(cos(radians(_SplitAngle)), 0.001);
                
                // Left side parameters
                float2 leftOffset = _LeftOffset.xy / canvasScale;
                float2 leftExpand = _LeftExpand.xy / canvasScale;
                float2 lCBL = float2(_LeftCornersBLTL.x, _LeftCornersBLTL.y) / canvasScale;
                float2 lCTL = float2(_LeftCornersBLTL.z, _LeftCornersBLTL.w) / canvasScale;
                float2 lCBR = float2(_LeftCornersBRTR.x, _LeftCornersBRTR.y) / canvasScale; // slides along split
                float2 lCTR = float2(_LeftCornersBRTR.z, _LeftCornersBRTR.w) / canvasScale; // slides along split
                
                // Right side parameters
                float2 rightOffset = _RightOffset.xy / canvasScale;
                float2 rightExpand = _RightExpand.xy / canvasScale;
                float2 rCBL = float2(_RightCornersBLTL.x, _RightCornersBLTL.y) / canvasScale; // slides along split
                float2 rCTL = float2(_RightCornersBLTL.z, _RightCornersBLTL.w) / canvasScale; // slides along split
                float2 rCBR = float2(_RightCornersBRTR.x, _RightCornersBRTR.y) / canvasScale;
                float2 rCTR = float2(_RightCornersBRTR.z, _RightCornersBRTR.w) / canvasScale;
                
                int quadType = int(input.quadType + 0.5);
                
                // Heights
                float leftHeight = baseRect.y + leftExpand.y;
                float rightHeight = baseRect.y + rightExpand.y;
                
                // LEFT quad corners
                // BL, TL: outer edge (normal XY offset)
                float2 lBL = float2(-leftExpand.x, 0) + leftOffset + lCBL;
                float2 lTL = float2(-leftExpand.x, leftHeight) + leftOffset + lCTL;
                // BR, TR: at split line (slide along split direction)
                float2 lBR_base = splitBase;
                float2 lTR_base = GetSplitPointAtDistance(splitBase, splitDir, splitLength + leftExpand.y);
                float2 lBR = ApplySplitSlide(lBR_base, splitDir, lCBR) + leftOffset;
                float2 lTR = ApplySplitSlide(lTR_base, splitDir, lCTR) + leftOffset;
                
                // RIGHT quad corners
                // BL, TL: at split line (slide along split direction)
                float2 rBL_base = splitBase;
                float2 rTL_base = GetSplitPointAtDistance(splitBase, splitDir, splitLength + rightExpand.y);
                float2 rBL = ApplySplitSlide(rBL_base, splitDir, rCBL) + rightOffset;
                float2 rTL = ApplySplitSlide(rTL_base, splitDir, rCTL) + rightOffset;
                // BR, TR: outer edge (normal XY offset)
                float2 rBR = float2(baseRect.x + rightExpand.x, 0) + rightOffset + rCBR;
                float2 rTR = float2(baseRect.x + rightExpand.x, rightHeight) + rightOffset + rCTR;
                
                // Check if inside each quad
                float inLeft = IsInsideQuad(localPos, lBL, lTL, lTR, lBR);
                float inRight = IsInsideQuad(localPos, rBL, rTL, rTR, rBR);
                
                float inEither = max(inLeft, inRight);
                
                if (inEither < 0.5) discard;
                
                // ========== SHADOW ==========
                if (quadType == 1)
                {
                    float4 result = _ShadowColor;
                    result.a *= input.color.a;
                    return result;
                }
                
                // ========== MAIN BUBBLE ==========
                float onRight = inRight;
                
                float4 result = onRight > 0.5 ? _RightFillColor : _LeftFillColor;
                result.a *= input.color.a;
                return result;
            }
            
            ENDHLSL
        }
    }
}
