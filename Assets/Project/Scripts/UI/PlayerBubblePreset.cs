using UnityEngine;

/// <summary>
/// PlayerBubble preset - Persona-style split bubble.
/// Balanced, juicy, memorable, selling.
/// 
/// Design principles:
/// - Normal: subtle asymmetry, both halves visible
/// - Active: one side dominates, other shrinks/retreats
/// - Inactive: the non-active side when other is active
/// - Corners: angular but controlled (not chaotic)
/// - Animation: snappy and responsive
/// </summary>
[CreateAssetMenu(fileName = "PlayerBubblePreset", menuName = "DECKADENCE/UI/Player Bubble Preset")]
public class PlayerBubblePreset : ScriptableObject
{
    // ========================================
    // SPLIT SETTINGS
    // ========================================
    [Header("Split - Normal")]
    public float splitAngleNormal = 0f;
    public float splitPositionNormal = 0.5f;
    
    [Header("Split - Left Active")]
    public float splitAngleLeftActive = 22f;
    public float splitPositionLeftActive = 0.85f;
    
    [Header("Split - Right Active")]
    public float splitAngleRightActive = -22f;
    public float splitPositionRightActive = 0.15f;
    
    // ========================================
    // LEFT SIDE - DARK
    // ========================================
    [Header("Left - Normal")]
    public Color leftFillNormal = new Color(0.08f, 0.08f, 0.12f, 1f);
    public Vector2 leftOffsetNormal = Vector2.zero;
    public Vector2 leftExpandNormal = Vector2.zero;
    
    [Header("Left Corners - Normal")]
    public Vector2 leftCornerBL_Normal = Vector2.zero;
    public Vector2 leftCornerTL_Normal = Vector2.zero;
    public Vector2 leftCornerBR_Normal = Vector2.zero;
    public Vector2 leftCornerTR_Normal = Vector2.zero;
    
    [Header("Left - Active (when left is selected)")]
    public Color leftFillActive = new Color(0.10f, 0.10f, 0.16f, 1f);
    public Vector2 leftOffsetActive = new Vector2(-8f, 4f);
    public Vector2 leftExpandActive = new Vector2(12f, 8f);
    
    [Header("Left Corners - Active")]
    public Vector2 leftCornerBL_Active = new Vector2(-16f, -6f);
    public Vector2 leftCornerTL_Active = new Vector2(-18f, 8f);
    public Vector2 leftCornerBR_Active = new Vector2(8f, 0f);
    public Vector2 leftCornerTR_Active = new Vector2(-10f, 0f);
    
    [Header("Left - Inactive (when RIGHT is active)")]
    public Color leftFillInactive = new Color(0.05f, 0.05f, 0.08f, 0.6f);
    public Vector2 leftOffsetInactive = new Vector2(-20f, 0f);
    public Vector2 leftExpandInactive = new Vector2(-10f, -4f);
    
    [Header("Left Corners - Inactive")]
    public Vector2 leftCornerBL_Inactive = new Vector2(-8f, 4f);
    public Vector2 leftCornerTL_Inactive = new Vector2(-10f, -4f);
    public Vector2 leftCornerBR_Inactive = new Vector2(4f, 0f);
    public Vector2 leftCornerTR_Inactive = new Vector2(-6f, 0f);
    
    // ========================================
    // RIGHT SIDE - LIGHT
    // ========================================
    [Header("Right - Normal")]
    public Color rightFillNormal = new Color(0.92f, 0.92f, 0.96f, 1f);
    public Vector2 rightOffsetNormal = Vector2.zero;
    public Vector2 rightExpandNormal = Vector2.zero;
    
    [Header("Right Corners - Normal")]
    public Vector2 rightCornerBL_Normal = Vector2.zero;
    public Vector2 rightCornerTL_Normal = Vector2.zero;
    public Vector2 rightCornerBR_Normal = Vector2.zero;
    public Vector2 rightCornerTR_Normal = Vector2.zero;
    
    [Header("Right - Active (when right is selected)")]
    public Color rightFillActive = new Color(1f, 1f, 1f, 1f);
    public Vector2 rightOffsetActive = new Vector2(8f, 4f);
    public Vector2 rightExpandActive = new Vector2(12f, 8f);
    
    [Header("Right Corners - Active")]
    public Vector2 rightCornerBL_Active = new Vector2(8f, 0f);
    public Vector2 rightCornerTL_Active = new Vector2(-10f, 0f);
    public Vector2 rightCornerBR_Active = new Vector2(16f, -6f);
    public Vector2 rightCornerTR_Active = new Vector2(18f, 8f);
    
    [Header("Right - Inactive (when LEFT is active)")]
    public Color rightFillInactive = new Color(0.7f, 0.7f, 0.75f, 0.6f);
    public Vector2 rightOffsetInactive = new Vector2(20f, 0f);
    public Vector2 rightExpandInactive = new Vector2(-10f, -4f);
    
    [Header("Right Corners - Inactive")]
    public Vector2 rightCornerBL_Inactive = new Vector2(-4f, 0f);
    public Vector2 rightCornerTL_Inactive = new Vector2(6f, 0f);
    public Vector2 rightCornerBR_Inactive = new Vector2(8f, 4f);
    public Vector2 rightCornerTR_Inactive = new Vector2(10f, -4f);
    
    // ========================================
    // COMMON
    // ========================================
    [Header("Shadow")]
    public Color shadowColor = new Color(0f, 0f, 0f, 0.5f);
    public float shadowIntensity = 8f;
    
    [Header("Text")]
    public Color textColorLeft = Color.white;
    public Color textColorRight = new Color(0.06f, 0.06f, 0.09f, 1f);
    
    [Header("Animation")]
    public float transitionDuration = 0.12f;
    
    // ========================================
    // METHODS
    // ========================================
    
    public void GetSplitState(float leftProgress, float rightProgress, out float angle, out float position)
    {
        if (leftProgress > rightProgress)
        {
            angle = Mathf.Lerp(splitAngleNormal, splitAngleLeftActive, leftProgress);
            position = Mathf.Lerp(splitPositionNormal, splitPositionLeftActive, leftProgress);
        }
        else if (rightProgress > leftProgress)
        {
            angle = Mathf.Lerp(splitAngleNormal, splitAngleRightActive, rightProgress);
            position = Mathf.Lerp(splitPositionNormal, splitPositionRightActive, rightProgress);
        }
        else
        {
            angle = splitAngleNormal;
            position = splitPositionNormal;
        }
    }
    
    /// <summary>
    /// Get left state considering both self progress AND opposite side progress.
    /// When right is active, left goes toward Inactive state.
    /// </summary>
    public void GetLeftState(float selfProgress, float oppositeProgress,
        out Color fill, out Vector2 offset, out Vector2 expand,
        out Vector2 cBL, out Vector2 cTL, out Vector2 cBR, out Vector2 cTR)
    {
        // Start from normal
        Color baseFill = leftFillNormal;
        Vector2 baseOffset = leftOffsetNormal;
        Vector2 baseExpand = leftExpandNormal;
        Vector2 baseBL = leftCornerBL_Normal;
        Vector2 baseTL = leftCornerTL_Normal;
        Vector2 baseBR = leftCornerBR_Normal;
        Vector2 baseTR = leftCornerTR_Normal;
        
        // If self is MORE active (or equally active), interpolate toward Active
        if (selfProgress >= oppositeProgress && selfProgress > 0)
        {
            fill = Color.Lerp(baseFill, leftFillActive, selfProgress);
            offset = Vector2.Lerp(baseOffset, leftOffsetActive, selfProgress);
            expand = Vector2.Lerp(baseExpand, leftExpandActive, selfProgress);
            cBL = Vector2.Lerp(baseBL, leftCornerBL_Active, selfProgress);
            cTL = Vector2.Lerp(baseTL, leftCornerTL_Active, selfProgress);
            cBR = Vector2.Lerp(baseBR, leftCornerBR_Active, selfProgress);
            cTR = Vector2.Lerp(baseTR, leftCornerTR_Active, selfProgress);
        }
        // If opposite (right) is MORE active, interpolate toward Inactive
        else if (oppositeProgress > selfProgress)
        {
            fill = Color.Lerp(baseFill, leftFillInactive, oppositeProgress);
            offset = Vector2.Lerp(baseOffset, leftOffsetInactive, oppositeProgress);
            expand = Vector2.Lerp(baseExpand, leftExpandInactive, oppositeProgress);
            cBL = Vector2.Lerp(baseBL, leftCornerBL_Inactive, oppositeProgress);
            cTL = Vector2.Lerp(baseTL, leftCornerTL_Inactive, oppositeProgress);
            cBR = Vector2.Lerp(baseBR, leftCornerBR_Inactive, oppositeProgress);
            cTR = Vector2.Lerp(baseTR, leftCornerTR_Inactive, oppositeProgress);
        }
        // Normal state
        else
        {
            fill = baseFill;
            offset = baseOffset;
            expand = baseExpand;
            cBL = baseBL;
            cTL = baseTL;
            cBR = baseBR;
            cTR = baseTR;
        }
    }
    
    /// <summary>
    /// Get right state considering both self progress AND opposite side progress.
    /// When left is active, right goes toward Inactive state.
    /// </summary>
    public void GetRightState(float selfProgress, float oppositeProgress,
        out Color fill, out Vector2 offset, out Vector2 expand,
        out Vector2 cBL, out Vector2 cTL, out Vector2 cBR, out Vector2 cTR)
    {
        // Start from normal
        Color baseFill = rightFillNormal;
        Vector2 baseOffset = rightOffsetNormal;
        Vector2 baseExpand = rightExpandNormal;
        Vector2 baseBL = rightCornerBL_Normal;
        Vector2 baseTL = rightCornerTL_Normal;
        Vector2 baseBR = rightCornerBR_Normal;
        Vector2 baseTR = rightCornerTR_Normal;
        
        // If self is MORE active (or equally active), interpolate toward Active
        if (selfProgress >= oppositeProgress && selfProgress > 0)
        {
            fill = Color.Lerp(baseFill, rightFillActive, selfProgress);
            offset = Vector2.Lerp(baseOffset, rightOffsetActive, selfProgress);
            expand = Vector2.Lerp(baseExpand, rightExpandActive, selfProgress);
            cBL = Vector2.Lerp(baseBL, rightCornerBL_Active, selfProgress);
            cTL = Vector2.Lerp(baseTL, rightCornerTL_Active, selfProgress);
            cBR = Vector2.Lerp(baseBR, rightCornerBR_Active, selfProgress);
            cTR = Vector2.Lerp(baseTR, rightCornerTR_Active, selfProgress);
        }
        // If opposite (left) is MORE active, interpolate toward Inactive
        else if (oppositeProgress > selfProgress)
        {
            fill = Color.Lerp(baseFill, rightFillInactive, oppositeProgress);
            offset = Vector2.Lerp(baseOffset, rightOffsetInactive, oppositeProgress);
            expand = Vector2.Lerp(baseExpand, rightExpandInactive, oppositeProgress);
            cBL = Vector2.Lerp(baseBL, rightCornerBL_Inactive, oppositeProgress);
            cTL = Vector2.Lerp(baseTL, rightCornerTL_Inactive, oppositeProgress);
            cBR = Vector2.Lerp(baseBR, rightCornerBR_Inactive, oppositeProgress);
            cTR = Vector2.Lerp(baseTR, rightCornerTR_Inactive, oppositeProgress);
        }
        // Normal state
        else
        {
            fill = baseFill;
            offset = baseOffset;
            expand = baseExpand;
            cBL = baseBL;
            cTL = baseTL;
            cBR = baseBR;
            cTR = baseTR;
        }
    }
}
