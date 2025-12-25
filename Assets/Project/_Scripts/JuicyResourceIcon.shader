Shader "Custom/JuicyResourceIcon"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [HideInInspector] _Color ("Tint", Color) = (1,1,1,1)
        
        [Header(Fill Effect)]
        _FillAmount ("Fill Amount", Range(0, 1)) = 1.0
        _FillColor ("Fill Tint", Color) = (1,1,1,1)
        _BackgroundColor ("Background Color", Color) = (0.2, 0.2, 0.2, 0.8)
        _BackgroundAlpha ("Background Alpha", Range(0, 1)) = 0.5
        _FillWaveStrength ("Fill Wave Strength", Range(0, 0.1)) = 0.02
        _FillWaveSpeed ("Fill Wave Speed", Float) = 3.0
        
        [Header(Glow and Pulse)]
        _GlowColor ("Glow Color", Color) = (1, 0.8, 0.2, 1)
        _GlowIntensity ("Glow Intensity", Range(0, 2)) = 0.0
        _GlowSize ("Glow Size", Range(0, 0.1)) = 0.02
        _PulseSpeed ("Pulse Speed", Float) = 2.0
        _PulseIntensity ("Pulse Intensity", Range(0, 1)) = 0.0
        
        [Header(Shake Effect)]
        _ShakeIntensity ("Shake Intensity", Range(0, 20)) = 0.0
        _ShakeSpeed ("Shake Speed", Float) = 30.0
        
        [Header(Highlight Flash)]
        _HighlightColor ("Highlight Color", Color) = (1, 1, 1, 1)
        _HighlightIntensity ("Highlight Intensity", Range(0, 1)) = 0.0
        
        [Header(Color Tint)]
        _TintOverlay ("Tint Overlay Color", Color) = (1, 1, 1, 0)
        
        [Header(Shadow)]
        [HideInInspector] _ShadowColor ("Shadow Color", Color) = (0, 0, 0, 0.5)
        
        // Stencil
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
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
            Name "JuicyResourceIcon"
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1; // Shadow flag
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float isShadow : TEXCOORD1;
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                
                // Fill
                float _FillAmount;
                float4 _FillColor;
                float4 _BackgroundColor;
                float _BackgroundAlpha;
                float _FillWaveStrength;
                float _FillWaveSpeed;
                
                // Glow/Pulse
                float4 _GlowColor;
                float _GlowIntensity;
                float _GlowSize;
                float _PulseSpeed;
                float _PulseIntensity;
                
                // Shake
                float _ShakeIntensity;
                float _ShakeSpeed;
                
                // Highlight
                float4 _HighlightColor;
                float _HighlightIntensity;
                
                // Tint
                float4 _TintOverlay;
                
                // Shadow
                float4 _ShadowColor;
            CBUFFER_END
            
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                
                float3 posOS = IN.positionOS.xyz;
                
                // Apply shake in vertex shader (UI needs larger values)
                if (_ShakeIntensity > 0.001)
                {
                    float time = _Time.y * _ShakeSpeed;
                    float2 shake;
                    shake.x = sin(time) * cos(time * 1.3) * _ShakeIntensity;
                    shake.y = cos(time * 0.7) * sin(time * 1.1) * _ShakeIntensity;
                    posOS.xy += shake;
                }
                
                OUT.positionCS = TransformObjectToHClip(posOS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color * _Color;
                OUT.isShadow = IN.uv1.x;
                
                return OUT;
            }
            
            half4 frag(Varyings IN) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                
                // === SHADOW ===
                if (IN.isShadow > 0.5)
                {
                    return half4(_ShadowColor.rgb, texColor.a * _ShadowColor.a * IN.color.a);
                }
                
                // === FILL CALCULATION ===
                float wave = sin(IN.uv.x * 20.0 + _Time.y * _FillWaveSpeed) * _FillWaveStrength;
                float fillLine = _FillAmount + wave;
                float isFilled = step(IN.uv.y, fillLine);
                
                // === LAYER 1: BACKGROUND (empty region) ===
                // Show background color where NOT filled, with sprite shape
                half4 background = half4(_BackgroundColor.rgb, texColor.a * _BackgroundAlpha);
                
                // === LAYER 2: FILLED PORTION ===
                // Show actual sprite with fill tint where filled
                half4 filled = texColor * _FillColor;
                
                // Composite: background behind, filled on top
                half4 result;
                result.rgb = lerp(background.rgb, filled.rgb, isFilled);
                result.a = lerp(background.a, filled.a, isFilled);
                
                // === GLOW EFFECT ===
                if (_GlowIntensity > 0.001)
                {
                    half edgeAlpha = 0;
                    for (int i = 0; i < 8; i++)
                    {
                        float angle = i * 0.785398;
                        float2 offset = float2(cos(angle), sin(angle)) * _GlowSize;
                        edgeAlpha += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + offset).a;
                    }
                    edgeAlpha /= 8.0;
                    
                    half glowMask = saturate(edgeAlpha - texColor.a * 0.5);
                    float pulse = 1.0 + sin(_Time.y * _PulseSpeed) * _PulseIntensity * 0.5;
                    glowMask *= pulse;
                    
                    result.rgb += _GlowColor.rgb * glowMask * _GlowIntensity;
                    result.a = max(result.a, glowMask * _GlowIntensity * _GlowColor.a);
                }
                
                // === HIGHLIGHT FLASH ===
                if (_HighlightIntensity > 0.001)
                {
                    result.rgb = lerp(result.rgb, _HighlightColor.rgb, _HighlightIntensity);
                }
                
                // === TINT OVERLAY ===
                if (_TintOverlay.a > 0.001)
                {
                    result.rgb = lerp(result.rgb, _TintOverlay.rgb, _TintOverlay.a);
                }
                
                // === PULSE (brightness) ===
                if (_PulseIntensity > 0.001)
                {
                    float pulse = 1.0 + sin(_Time.y * _PulseSpeed) * _PulseIntensity * 0.3;
                    result.rgb *= pulse;
                }
                
                return result * IN.color;
            }
            ENDHLSL
        }
    }
    
    FallBack "UI/Default"
}
