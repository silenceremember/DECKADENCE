Shader "RoyalLeech/UI/DialogShadow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Fill Color", Color) = (1,1,1,1)
        
        [Header(Shadow Settings)]
        [HideInInspector] _ShadowColor ("Shadow Color", Color) = (0, 0, 0, 0.5)
        
        [Header(Arrow Settings)]
        _ArrowEdge ("Arrow Edge (0=Bottom, 1=Top, 2=Left, 3=Right)", Range(0, 3)) = 0
        _ArrowPosition ("Arrow Position Along Edge", Range(0, 1)) = 0.5
        _ArrowSize ("Arrow Size", Range(0, 0.3)) = 0.1
        _ArrowWidth ("Arrow Width", Range(0.02, 0.3)) = 0.1
        
        [Header(Corner Cut)]
        _CornerCutPixels ("Corner Cut (Screen Pixels)", Float) = 20
        
        [Header(Canvas Info)]
        [HideInInspector] _RectSize ("Rect Size", Vector) = (100, 100, 0, 0)
        [HideInInspector] _CanvasScale ("Canvas Scale", Float) = 1.0
        
        [Header(Pixelation)]
        _PixelSize ("Pixel Size", Range(0, 512)) = 0
        
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
                float _ArrowEdge;
                float _ArrowPosition;
                float _ArrowSize;
                float _ArrowWidth;
                float _CornerCutPixels;
                float _PixelSize;
            CBUFFER_END
            
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
            float IsInsideArrow(float2 uv, float edge, float pos, float size, float width)
            {
                float2 arrowTip;
                float2 baseLeft;
                float2 baseRight;
                float halfWidth = width * 0.5;
                
                int edgeInt = (int)edge;
                
                if (edgeInt == 0) // Bottom
                {
                    arrowTip = float2(pos, -size);
                    baseLeft = float2(pos - halfWidth, 0.0);
                    baseRight = float2(pos + halfWidth, 0.0);
                }
                else if (edgeInt == 1) // Top
                {
                    arrowTip = float2(pos, 1.0 + size);
                    baseLeft = float2(pos - halfWidth, 1.0);
                    baseRight = float2(pos + halfWidth, 1.0);
                }
                else if (edgeInt == 2) // Left
                {
                    arrowTip = float2(-size, pos);
                    baseLeft = float2(0.0, pos - halfWidth);
                    baseRight = float2(0.0, pos + halfWidth);
                }
                else // Right
                {
                    arrowTip = float2(1.0 + size, pos);
                    baseLeft = float2(1.0, pos - halfWidth);
                    baseRight = float2(1.0, pos + halfWidth);
                }
                
                float2 v0 = baseRight - arrowTip;
                float2 v1 = baseLeft - arrowTip;
                float2 v2 = uv - arrowTip;
                
                float dot00 = dot(v0, v0);
                float dot01 = dot(v0, v1);
                float dot02 = dot(v0, v2);
                float dot11 = dot(v1, v1);
                float dot12 = dot(v1, v2);
                
                float invDenom = 1.0 / (dot00 * dot11 - dot01 * dot01);
                float u = (dot11 * dot02 - dot01 * dot12) * invDenom;
                float v = (dot00 * dot12 - dot01 * dot02) * invDenom;
                
                return (u >= 0.0) && (v >= 0.0) && (u + v <= 1.0) ? 1.0 : 0.0;
            }
            
            half4 frag(Varyings IN) : SV_Target
            {
                float2 rectSize = _RectSize.xy;
                
                // Apply pixelation
                float2 pixelUV = IN.uv;
                if (_PixelSize > 0)
                {
                    pixelUV = (floor(IN.uv * _PixelSize) + 0.5) / _PixelSize;
                }
                
                // Convert UV to local position in canvas units
                float2 localPos = pixelUV * rectSize;
                
                // Convert pixel values to canvas units using scale factor
                float cornerCutCanvas = _CornerCutPixels / max(_CanvasScale, 0.001);
                
                // Check if inside main rectangle (UV 0-1 range)
                float insideRect = step(0.0, pixelUV.x) * step(pixelUV.x, 1.0) * 
                                   step(0.0, pixelUV.y) * step(pixelUV.y, 1.0);
                
                // Check if inside arrow
                float insideArrow = IsInsideArrow(pixelUV, _ArrowEdge, _ArrowPosition, _ArrowSize, _ArrowWidth);
                
                // Combine: visible if in rect OR in arrow
                float visible = max(insideRect, insideArrow);
                
                // Apply corner cuts (only to rect, not arrow)
                if (insideRect > 0.5)
                {
                    float distBottom = localPos.y;
                    float distTop = rectSize.y - localPos.y;
                    float distLeft = localPos.x;
                    float distRight = rectSize.x - localPos.x;
                    
                    float cornerMask = 1.0;
                    // Diagonal corner cuts using canvas-converted pixel value
                    cornerMask *= step(cornerCutCanvas, distLeft + distBottom);
                    cornerMask *= step(cornerCutCanvas, distRight + distBottom);
                    cornerMask *= step(cornerCutCanvas, distLeft + distTop);
                    cornerMask *= step(cornerCutCanvas, distRight + distTop);
                    
                    visible *= cornerMask;
                }
                
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
