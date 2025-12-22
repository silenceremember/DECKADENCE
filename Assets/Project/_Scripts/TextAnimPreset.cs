using UnityEngine;
using System.Collections.Generic;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "NewTextAnimPreset", menuName = "Text Animation/Preset")]
public class TextAnimPreset : ScriptableObject
{
    [System.Serializable]
    public class EffectSettings
    {
        public EffectType type;
        
        [Header("Settings")]
        public float speed = 5f;
        public float amplitude = 5f;
        public float frequency = 1f;
        
        [Tooltip("Random seed/offset for noise based effects")]
        public float noiseScale = 1f; 
        
        public bool useUnscaledTime = false;
    }

    public enum EffectType
    {
        Fade,       // Alpha wave
        Bounce,     // Y bounce (abs sin)
        Wave,       // Y sin
        Shake,      // Random XY jitter
        Wiggle,     // Smooth random XY
        Pendulum,   // Rotate around pivot (top)
        Dangle,     // Damped pendulum? Or different pivot
        Rainbow,    // Hue cycle
        Rotate,     // Continuous rotation
        Slide,      // X sin
        Swing,      // Rotate sine (standard center pivot?)
        IncreaseSize // Pulsing scale
    }

    public List<EffectSettings> effects = new List<EffectSettings>();
    
    [Header("Preview Settings")]
    [Tooltip("Кастомный шрифт для превью (опционально)")]
    public TMP_FontAsset previewFont;
    
    [Tooltip("Размер шрифта для превью")]
    public float previewFontSize = 20f;
    
    [Tooltip("Текст для превью")]
    public string previewText = "Sample Text";

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Применяем рекомендуемые настройки для эффектов с нулевыми параметрами
        foreach (var effect in effects)
        {
            // Если все параметры нулевые - применяем рекомендуемые
            if (effect.speed == 0 && effect.amplitude == 0 && effect.frequency == 0)
            {
                var recommended = GetRecommendedSettings(effect.type);
                effect.speed = recommended.speed;
                effect.amplitude = recommended.amplitude;
                effect.frequency = recommended.frequency;
                effect.noiseScale = recommended.noiseScale;
            }
        }
    }
#endif

    public struct AnimationResult
    {
        public Vector3 posOffset;
        public Quaternion rotOffset;
        public float scaleMultiplier;
        public Color32? colorOverride; // Null if no change
    }

    public static AnimationResult GetEffectForChar(int charIndex, float time, EffectSettings settings, Vector3 charCenter, Vector3 charTop)
    {
        AnimationResult res = new AnimationResult
        {
            posOffset = Vector3.zero,
            rotOffset = Quaternion.identity,
            scaleMultiplier = 1f,
            colorOverride = null
        };

        // We rely on the caller to pass the correct time (scaled or unscaled).
        
        float animVal = time * settings.speed + charIndex * settings.frequency;

        switch (settings.type)
        {
            case EffectType.Fade:
                // Simple alpha fade 0..1
                float alpha = (Mathf.Sin(animVal) + 1f) * 0.5f;
                // Возвращаем белый цвет с переменной прозрачностью
                res.colorOverride = new Color(1f, 1f, 1f, alpha);
                break;
            case EffectType.Bounce:
                res.posOffset.y += Mathf.Abs(Mathf.Sin(animVal)) * settings.amplitude;
                break;
            case EffectType.Wave:
                res.posOffset.y += Mathf.Sin(animVal) * settings.amplitude;
                break;
            case EffectType.Shake:
                float noiseX = Mathf.PerlinNoise(time * settings.speed, charIndex * settings.noiseScale) - 0.5f;
                float noiseY = Mathf.PerlinNoise(time * settings.speed + 100f, charIndex * settings.noiseScale) - 0.5f;
                res.posOffset += new Vector3(noiseX, noiseY, 0) * settings.amplitude;
                break;
            case EffectType.Wiggle:
                res.rotOffset = Quaternion.Euler(0, 0, Mathf.Sin(animVal) * settings.amplitude);
                break;
            case EffectType.Pendulum:
                float angle = Mathf.Sin(animVal) * settings.amplitude;
                // Pivot correction logic is complex here without full vertex data.
                // We return rotation. Caller handles pivot if they can, or we approximate.
                res.rotOffset = Quaternion.Euler(0, 0, angle);
                break;
            case EffectType.Dangle:
                 // Like pendulum but maybe damped or different phase?
                 float dAngle = Mathf.Sin(animVal) * settings.amplitude;
                 res.rotOffset = Quaternion.Euler(0, 0, dAngle);
                 break;
            case EffectType.Rainbow:
                 // HSL 
                 float hue = Mathf.Repeat(time * settings.speed * 0.1f + charIndex * 0.1f, 1f);
                 res.colorOverride = Color.HSVToRGB(hue, 1f, 1f);
                 break;
            case EffectType.Rotate:
                 res.rotOffset = Quaternion.Euler(0, 0, -animVal * 10f);
                 break;
            case EffectType.Slide:
                 res.posOffset.x += Mathf.Sin(animVal) * settings.amplitude;
                 break;
            case EffectType.Swing:
                 res.rotOffset = Quaternion.Euler(0, 0, Mathf.Cos(animVal) * settings.amplitude);
                 break;
            case EffectType.IncreaseSize:
                 res.scaleMultiplier += Mathf.Sin(animVal) * 0.2f * settings.amplitude;
                 break;
        }

        return res;
    }

    // Рекомендуемые параметры для каждого типа эффекта
    public static EffectSettings GetRecommendedSettings(EffectType type)
    {
        var settings = new EffectSettings { type = type };
        
        switch (type)
        {
            case EffectType.Fade:
                settings.speed = 3f;
                settings.amplitude = 1f;
                settings.frequency = 0.5f;
                break;
                
            case EffectType.Bounce:
                settings.speed = 4f;
                settings.amplitude = 10f;
                settings.frequency = 0.3f;
                break;
                
            case EffectType.Wave:
                settings.speed = 3f;
                settings.amplitude = 8f;
                settings.frequency = 0.5f;
                break;
                
            case EffectType.Shake:
                settings.speed = 20f;
                settings.amplitude = 3f;
                settings.frequency = 1f;
                settings.noiseScale = 1f;
                break;
                
            case EffectType.Wiggle:
                settings.speed = 5f;
                settings.amplitude = 15f;
                settings.frequency = 0.3f;
                break;
                
            case EffectType.Pendulum:
                settings.speed = 2f;
                settings.amplitude = 25f;
                settings.frequency = 0.4f;
                break;
                
            case EffectType.Dangle:
                settings.speed = 3f;
                settings.amplitude = 20f;
                settings.frequency = 0.3f;
                break;
                
            case EffectType.Rainbow:
                settings.speed = 2f;
                settings.amplitude = 1f;
                settings.frequency = 0.2f;
                break;
                
            case EffectType.Rotate:
                settings.speed = 3f;
                settings.amplitude = 1f;
                settings.frequency = 0f;
                break;
                
            case EffectType.Slide:
                settings.speed = 3f;
                settings.amplitude = 10f;
                settings.frequency = 0.4f;
                break;
                
            case EffectType.Swing:
                settings.speed = 4f;
                settings.amplitude = 20f;
                settings.frequency = 0.3f;
                break;
                
            case EffectType.IncreaseSize:
                settings.speed = 4f;
                settings.amplitude = 0.3f;
                settings.frequency = 0.2f;
                break;
                
            default:
                settings.speed = 5f;
                settings.amplitude = 5f;
                settings.frequency = 1f;
                break;
        }
        
        settings.useUnscaledTime = false;
        return settings;
    }
}
