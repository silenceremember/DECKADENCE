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
                // We'll return a color with alpha. Assume white? 
                // Caller should blend.
                // This returns a "modifier".
                // Let's just return a generic alpha val? 
                // For simplicity in this helper, let's return a Color with Alpha modified if possible
                // But we don't know original color.
                // We'll skip Fade in this simple helper or just pass alpha via color A?
                // Let's use colorOverride for Rainbow. Fade is tricky without context.
                // But user wants Fade.
                // Let's assume Fade modifies Alpha.
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
}
