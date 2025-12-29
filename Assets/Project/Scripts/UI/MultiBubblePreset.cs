using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ScriptableObject preset for multi-layer bubble appearance.
/// Contains a list of bubble layers that are rendered back to front.
/// Layer 0 is the furthest back (e.g., outer frame), last layer is frontmost (e.g., main bubble).
/// </summary>
[CreateAssetMenu(fileName = "NewMultiBubblePreset", menuName = "Game/Multi Bubble Preset")]
public class MultiBubblePreset : ScriptableObject
{
    [Header("Layers")]
    [Tooltip("Bubble layers rendered back to front. Layer 0 = furthest back, last = front.")]
    public List<BubbleLayerConfig> layers = new List<BubbleLayerConfig>();
    
    [Header("Text Colors")]
    [Tooltip("Text color in normal state")]
    public Color textNormalColor = Color.black;
    
    [Tooltip("Text color in active/selected state")]
    public Color textActiveColor = new Color(0.36f, 0.25f, 0.22f);
    
    [Header("Shared Settings")]
    [Tooltip("Corner cut minimum size in pixels")]
    public float cornerCutMin = 15f;
    
    [Tooltip("Corner cut maximum size in pixels")]
    public float cornerCutMax = 25f;
    
    [Tooltip("Tear depth minimum in pixels")]
    public float tearDepthMin = 5f;
    
    [Tooltip("Tear depth maximum in pixels")]
    public float tearDepthMax = 15f;
    
    [Tooltip("Tear width minimum in pixels")]
    public float tearWidthMin = 10f;
    
    [Tooltip("Tear width maximum in pixels")]
    public float tearWidthMax = 30f;
    
    [Tooltip("Tear spacing minimum in pixels")]
    public float tearSpacingMin = 40f;
    
    [Tooltip("Tear spacing maximum in pixels")]
    public float tearSpacingMax = 80f;
    
    [Tooltip("Animation frames per second")]
    public float animationFPS = 1f;
    
    [Tooltip("Random seed for tear patterns")]
    public float tearSeed = 0f;
    
    /// <summary>
    /// Get the frontmost layer (main bubble).
    /// </summary>
    public BubbleLayerConfig GetFrontLayer()
    {
        if (layers == null || layers.Count == 0) return null;
        return layers[layers.Count - 1];
    }
    
    /// <summary>
    /// Get the layer that has arrow enabled, or null if none.
    /// </summary>
    public BubbleLayerConfig GetArrowLayer()
    {
        if (layers == null) return null;
        foreach (var layer in layers)
        {
            if (layer.enabled && layer.showArrow) return layer;
        }
        return null;
    }
    
    /// <summary>
    /// Create a simple single-layer preset.
    /// </summary>
    public static MultiBubblePreset CreateSimple()
    {
        var preset = CreateInstance<MultiBubblePreset>();
        preset.layers = new List<BubbleLayerConfig> { BubbleLayerConfig.CreateDefault() };
        return preset;
    }
    
    /// <summary>
    /// Create a two-layer preset with frame and main bubble.
    /// </summary>
    public static MultiBubblePreset CreateWithFrame(float frameOffset = 20f)
    {
        var preset = CreateInstance<MultiBubblePreset>();
        preset.layers = new List<BubbleLayerConfig>
        {
            BubbleLayerConfig.CreateFrame(frameOffset),
            BubbleLayerConfig.CreateDefault()
        };
        return preset;
    }
    
    private void OnValidate()
    {
        // Ensure at least one layer exists
        if (layers == null)
            layers = new List<BubbleLayerConfig>();
        
        if (layers.Count == 0)
            layers.Add(BubbleLayerConfig.CreateDefault());
    }
}
