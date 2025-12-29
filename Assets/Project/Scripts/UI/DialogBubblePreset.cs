using UnityEngine;

/// <summary>
/// ScriptableObject preset for dialog bubble appearance.
/// Controls the full visual style of dialog bubbles including fill, border, shadow, and tear effects.
/// </summary>
[CreateAssetMenu(fileName = "NewDialogBubblePreset", menuName = "Game/Dialog Bubble Preset")]
public class DialogBubblePreset : ScriptableObject
{
    [Header("Character Info")]
    [Tooltip("Name of the character or context this preset is for")]
    public string presetName = "Default";
    
    [Header("Fill")]
    [Tooltip("Main fill color of the bubble (normal state)")]
    public Color fillColor = new Color(0.15f, 0.12f, 0.1f, 0.95f);
    [Tooltip("Fill color when bubble is in active/selected state")]
    public Color activeColor = new Color(1f, 0.97f, 0.88f, 0.95f);  // Cream
    
    [Header("Border")]
    [Tooltip("Show decorative inner border")]
    public bool showBorder = true;
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
    [Tooltip("Second border offset (for double style)")]
    public float secondBorderOffset = 4f;
    
    [Header("Inner Shadow")]
    [Tooltip("Show inner shadow")]
    public bool showInnerShadow = false;
    [Tooltip("Shadow intensity")]
    public float innerShadowIntensity = 15f;
    [Tooltip("Shadow color")]
    public Color innerShadowColor = new Color(0f, 0f, 0f, 0.5f);
    
    [Header("Outer Shadow")]
    [Tooltip("Primary shadow color")]
    public Color shadowColor = new Color(0f, 0f, 0f, 0.5f);
    [Tooltip("Primary shadow intensity (distance in pixels)")]
    public float shadowIntensity = 15f;
    [Tooltip("Show second shadow for depth")]
    public bool showSecondShadow = true;
    [Tooltip("Secondary shadow color")]
    public Color secondShadowColor = new Color(0f, 0f, 0f, 0.3f);
    [Tooltip("Secondary shadow intensity (distance in pixels)")]
    public float secondShadowIntensity = 6f;
    
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
}
