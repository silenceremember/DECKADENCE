using UnityEngine;

/// <summary>
/// Offset mode for bubble layers.
/// </summary>
public enum BubbleOffsetMode
{
    Uniform,   // Single value for all edges
    XY,        // Separate X and Y offsets
    PerEdge    // Individual offset per edge (left, right, top, bottom)
}

/// <summary>
/// Configuration for a single bubble layer.
/// Each layer is a complete bubble with its own fill, border, shadows, tears, and arrow.
/// </summary>
[System.Serializable]
public class BubbleLayerConfig
{
    [Header("Layer Settings")]
    [Tooltip("Enable this layer")]
    public bool enabled = true;
    
    [Tooltip("How to specify offset")]
    public BubbleOffsetMode offsetMode = BubbleOffsetMode.Uniform;
    
    [Tooltip("Uniform offset from base rect in pixels (positive = larger bubble)")]
    public float offset = 0f;
    
    [Tooltip("X offset (left/right simultaneously)")]
    public float offsetX = 0f;
    
    [Tooltip("Y offset (top/bottom simultaneously)")]
    public float offsetY = 0f;
    
    [Tooltip("Left edge offset in pixels")]
    public float offsetLeft = 0f;
    
    [Tooltip("Right edge offset in pixels")]
    public float offsetRight = 0f;
    
    [Tooltip("Top edge offset in pixels")]
    public float offsetTop = 0f;
    
    [Tooltip("Bottom edge offset in pixels")]
    public float offsetBottom = 0f;
    
    /// <summary>
    /// Get the effective offset for each edge based on the offset mode.
    /// Returns Vector4(left, right, top, bottom)
    /// </summary>
    public Vector4 GetEdgeOffsets()
    {
        switch (offsetMode)
        {
            case BubbleOffsetMode.Uniform:
                return new Vector4(offset, offset, offset, offset);
            case BubbleOffsetMode.XY:
                return new Vector4(offsetX, offsetX, offsetY, offsetY);
            case BubbleOffsetMode.PerEdge:
                return new Vector4(offsetLeft, offsetRight, offsetTop, offsetBottom);
            default:
                return Vector4.zero;
        }
    }
    
    [Tooltip("Cut out the area where the next layer will be (creates a frame effect)")]
    public bool cutoutNextLayer = false;
    
    [Tooltip("Extra padding for the cutout in pixels")]
    public float cutoutPadding = 0f;
    
    [Header("Fill")]
    [Tooltip("Fill color of this layer")]
    public Color fillColor = new Color(0.15f, 0.12f, 0.1f, 0.95f);
    
    [Tooltip("Active/selected state color")]
    public Color activeColor = new Color(1f, 0.97f, 0.88f, 0.95f);
    
    [Header("Arrow")]
    [Tooltip("Show arrow on this layer")]
    public bool showArrow = false;
    
    [Tooltip("Arrow size in pixels")]
    public float arrowSize = 30f;
    
    [Tooltip("Arrow width in pixels")]
    public float arrowWidth = 40f;
    
    [Header("Border")]
    [Tooltip("Show decorative border")]
    public bool showBorder = false;
    
    [Tooltip("Border color")]
    public Color borderColor = new Color(0.95f, 0.90f, 0.82f, 0.9f);
    
    [Tooltip("Border thickness in pixels")]
    public float borderThickness = 2f;
    
    [Tooltip("Border offset from edge in pixels")]
    public float borderOffset = 6f;
    
    [Tooltip("Border style: 0=solid, 1=dashed, 2=double, 3=corners only")]
    [Range(0, 3)]
    public int borderStyle = 0;
    
    [Tooltip("Dash length (for dashed style)")]
    public float dashLength = 8f;
    
    [Tooltip("Dash gap (for dashed style)")]
    public float dashGap = 4f;
    
    [Header("Outer Shadow")]
    [Tooltip("Primary shadow color")]
    public Color shadowColor = new Color(0f, 0f, 0f, 0.5f);
    
    [Tooltip("Primary shadow intensity (distance in pixels)")]
    public float shadowIntensity = 15f;
    
    [Tooltip("Show second shadow for additional depth")]
    public bool showSecondShadow = false;
    
    [Tooltip("Secondary shadow color")]
    public Color secondShadowColor = new Color(0f, 0f, 0f, 0.3f);
    
    [Tooltip("Secondary shadow intensity (distance in pixels)")]
    public float secondShadowIntensity = 6f;
    
    [Header("Inner Shadow")]
    [Tooltip("Show inner shadow (emboss/deboss effect)")]
    public bool showInnerShadow = false;
    
    [Tooltip("Inner shadow intensity")]
    public float innerShadowIntensity = 10f;
    
    [Tooltip("Inner shadow color")]
    public Color innerShadowColor = new Color(0f, 0f, 0f, 0.4f);
    
    [Header("Tear Effects")]
    [Tooltip("Top edge tear intensity")]
    [Range(0, 1)]
    public float topTear = 0f;
    
    [Tooltip("Bottom edge tear intensity")]
    [Range(0, 1)]
    public float bottomTear = 0f;
    
    [Tooltip("Left edge tear intensity")]
    [Range(0, 1)]
    public float leftTear = 0f;
    
    [Tooltip("Right edge tear intensity")]
    [Range(0, 1)]
    public float rightTear = 0f;
    
    /// <summary>
    /// Create a default main bubble layer configuration.
    /// </summary>
    public static BubbleLayerConfig CreateDefault()
    {
        return new BubbleLayerConfig
        {
            enabled = true,
            offset = 0f,
            cutoutNextLayer = false,
            fillColor = new Color(0.15f, 0.12f, 0.1f, 0.95f),
            showArrow = true,
            arrowSize = 30f,
            arrowWidth = 40f,
            showBorder = true,
            borderColor = new Color(0.95f, 0.90f, 0.82f, 0.9f),
            borderThickness = 2f,
            borderOffset = 6f,
            shadowColor = new Color(0f, 0f, 0f, 0.5f),
            shadowIntensity = 15f,
            showSecondShadow = true,
            secondShadowColor = new Color(0f, 0f, 0f, 0.3f),
            secondShadowIntensity = 6f
        };
    }
    
    /// <summary>
    /// Create a frame layer configuration (outer layer with cutout).
    /// </summary>
    public static BubbleLayerConfig CreateFrame(float offset = 20f)
    {
        return new BubbleLayerConfig
        {
            enabled = true,
            offset = offset,
            cutoutNextLayer = true,
            cutoutPadding = 0f,
            fillColor = new Color(0.2f, 0.18f, 0.15f, 0.95f),
            showArrow = false,
            showBorder = false,
            shadowColor = new Color(0f, 0f, 0f, 0.4f),
            shadowIntensity = 20f,
            showSecondShadow = false
        };
    }
}
