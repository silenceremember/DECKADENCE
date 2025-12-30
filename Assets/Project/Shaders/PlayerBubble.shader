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
        _LeftBorderColor ("Left Border Color", Color) = (0.4, 0.4, 0.4, 1)
        
        [Header(Right Side)]
        _RightFillColor ("Right Fill Color", Color) = (0.8, 0.8, 0.8, 1)
        _RightBorderColor ("Right Border Color", Color) = (0.6, 0.6, 0.6, 1)
        
        [Header(Border)]
        _BorderThickness ("Border Thickness", Float) = 2
        _SplitBorderThickness ("Split Line Thickness", Float) = 2
        
        [Header(Corner)]
        _CornerCut ("Corner Cut Size", Float) = 15
        
        [Header(Shadow)]
        _ShadowColor ("Shadow Color", Color) = (0, 0, 0, 0.5)
        _ShadowIntensity ("Shadow Intensity", Float) = 5
        
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
                float4 uv1 : TEXCOORD1; // x: unused, y: quadType (0=fill, 1=shadow)
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
                float4 _LeftBorderColor;
                float4 _RightFillColor;
                float4 _RightBorderColor;
                
                float _BorderThickness;
                float _SplitBorderThickness;
                float _CornerCut;
                
                float4 _ShadowColor;
                float _ShadowIntensity;
            CBUFFER_END
            
            // ==========================================
            // UTILITY FUNCTIONS
            // ==========================================
            
            // Signed distance to the split line
            // Positive = right side, Negative = left side
            float GetSplitDistance(float2 localPos, float2 rectSize, float angle, float position)
            {
                float2 center = rectSize * 0.5;
                float2 pos = localPos - center;
                
                // Offset based on position (0 = all left, 1 = all right)
                float offset = (position - 0.5) * rectSize.x;
                
                // Rotate by angle
                float rad = radians(angle);
                float cosA = cos(rad);
                float sinA = sin(rad);
                
                // Normal of split line (perpendicular to line direction)
                float2 normal = float2(cosA, sinA);
                
                return dot(pos, normal) - offset * cosA;
            }
            
            // Check if inside bubble shape (with corner cuts)
            float IsInsideBubble(float2 localPos, float2 rectSize, float cornerCut)
            {
                // Basic rect check
                float insideRect = step(0.0, localPos.x) * step(localPos.x, rectSize.x) * 
                                   step(0.0, localPos.y) * step(localPos.y, rectSize.y);
                
                if (insideRect < 0.5) return 0.0;
                
                // Corner cuts
                float distBottom = localPos.y;
                float distTop = rectSize.y - localPos.y;
                float distLeft = localPos.x;
                float distRight = rectSize.x - localPos.x;
                
                // Check all 4 corners
                float cornerMask = 1.0;
                cornerMask *= step(cornerCut, distLeft + distBottom);   // BL
                cornerMask *= step(cornerCut, distRight + distBottom);  // BR
                cornerMask *= step(cornerCut, distLeft + distTop);      // TL
                cornerMask *= step(cornerCut, distRight + distTop);     // TR
                
                return cornerMask;
            }
            
            // Distance to edge (for border calculation)
            float GetEdgeDistance(float2 localPos, float2 rectSize, float cornerCut)
            {
                float distBottom = localPos.y;
                float distTop = rectSize.y - localPos.y;
                float distLeft = localPos.x;
                float distRight = rectSize.x - localPos.x;
                
                float minDist = min(min(distBottom, distTop), min(distLeft, distRight));
                
                // Corner distances (diagonal)
                float blCorner = (distLeft + distBottom - cornerCut) * 0.707;
                float brCorner = (distRight + distBottom - cornerCut) * 0.707;
                float tlCorner = (distLeft + distTop - cornerCut) * 0.707;
                float trCorner = (distRight + distTop - cornerCut) * 0.707;
                
                // Use corner distance when in corner zone
                if (distLeft + distBottom < cornerCut * 2.0) minDist = min(minDist, blCorner);
                if (distRight + distBottom < cornerCut * 2.0) minDist = min(minDist, brCorner);
                if (distLeft + distTop < cornerCut * 2.0) minDist = min(minDist, tlCorner);
                if (distRight + distTop < cornerCut * 2.0) minDist = min(minDist, trCorner);
                
                return minDist;
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
                float2 rectSize = _RectSize.xy;
                float canvasScale = max(_CanvasScale, 0.001);
                
                // Calculate local position from UV (same as MultiBubble)
                float2 localPos = input.uv * rectSize;
                
                float cornerCut = _CornerCut / canvasScale;
                float borderThick = _BorderThickness / canvasScale;
                float splitBorderThick = _SplitBorderThickness / canvasScale;
                
                int quadType = int(input.quadType + 0.5); // 0=fill, 1=shadow
                
                // Check if inside bubble
                float visible = IsInsideBubble(localPos, rectSize, cornerCut);
                
                if (visible < 0.5) discard;
                
                // ========== SHADOW QUAD ==========
                if (quadType == 1)
                {
                    float4 result = _ShadowColor;
                    result.a *= input.color.a;
                    return result;
                }
                
                // ========== MAIN BUBBLE (quadType == 0) ==========
                
                // Get split distance
                float splitDist = GetSplitDistance(localPos, rectSize, _SplitAngle, _SplitPosition);
                float onRightSide = step(0.0, splitDist);
                
                // Edge distance for border
                float edgeDist = GetEdgeDistance(localPos, rectSize, cornerCut);
                float inBorder = step(edgeDist, borderThick);
                
                // Split line border
                float inSplitBorder = step(abs(splitDist), splitBorderThick * 0.5);
                
                // Choose colors based on side
                float4 fillColor = lerp(_LeftFillColor, _RightFillColor, onRightSide);
                float4 borderColor = lerp(_LeftBorderColor, _RightBorderColor, onRightSide);
                
                // Final color
                float4 result = fillColor;
                
                // Apply outer border
                if (inBorder > 0.5)
                {
                    result = borderColor;
                }
                
                // Apply split line (blend of both border colors)
                if (inSplitBorder > 0.5)
                {
                    float4 splitColor = lerp(_LeftBorderColor, _RightBorderColor, 0.5);
                    result = splitColor;
                }
                
                result.a *= input.color.a;
                
                return result;
            }
            
            ENDHLSL
        }
    }
}
