using UnityEngine;

/// <summary>
/// PlayerBubble preset - Persona-style split bubble.
/// Balanced, juicy, memorable, selling.
/// 
/// Design principles:
/// - Normal: subtle asymmetry, both halves visible
/// - Active: one side dominates, other disappears smoothly
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
    public float splitAngleNormal = 15f;
    public float splitPositionNormal = 0.5f;
    
    [Header("Split - Left Active")]
    public float splitAngleLeftActive = 22f;
    public float splitPositionLeftActive = 1.1f;
    
    [Header("Split - Right Active")]
    public float splitAngleRightActive = 8f;
    public float splitPositionRightActive = -0.1f;
    
    // ========================================
    // LEFT SIDE - DARK
    // ========================================
    [Header("Left - Normal")]
    public Color leftFillNormal = new Color(0.07f, 0.07f, 0.10f, 1f);
    public Vector2 leftOffsetNormal = new Vector2(-2f, 0f);
    public Vector2 leftExpandNormal = Vector2.zero;
    
    [Header("Left Corners - Normal")]
    public Vector2 leftCornerBL_Normal = new Vector2(-6f, 4f);
    public Vector2 leftCornerTL_Normal = new Vector2(-8f, -3f);
    public Vector2 leftCornerBR_Normal = new Vector2(5f, 0f);
    public Vector2 leftCornerTR_Normal = new Vector2(-4f, 0f);
    
    [Header("Left - Active")]
    public Color leftFillActive = new Color(0.10f, 0.10f, 0.14f, 1f);
    public Vector2 leftOffsetActive = new Vector2(-12f, 8f);
    public Vector2 leftExpandActive = new Vector2(8f, 6f);
    
    [Header("Left Corners - Active")]
    public Vector2 leftCornerBL_Active = new Vector2(-18f, 10f);
    public Vector2 leftCornerTL_Active = new Vector2(-22f, -8f);
    public Vector2 leftCornerBR_Active = new Vector2(20f, 0f);
    public Vector2 leftCornerTR_Active = new Vector2(-14f, 0f);
    
    // ========================================
    // RIGHT SIDE - LIGHT
    // ========================================
    [Header("Right - Normal")]
    public Color rightFillNormal = new Color(0.94f, 0.94f, 0.97f, 1f);
    public Vector2 rightOffsetNormal = new Vector2(2f, 0f);
    public Vector2 rightExpandNormal = Vector2.zero;
    
    [Header("Right Corners - Normal")]
    public Vector2 rightCornerBL_Normal = new Vector2(-5f, 0f);
    public Vector2 rightCornerTL_Normal = new Vector2(4f, 0f);
    public Vector2 rightCornerBR_Normal = new Vector2(6f, 4f);
    public Vector2 rightCornerTR_Normal = new Vector2(8f, -3f);
    
    [Header("Right - Active")]
    public Color rightFillActive = new Color(1f, 1f, 1f, 1f);
    public Vector2 rightOffsetActive = new Vector2(12f, 8f);
    public Vector2 rightExpandActive = new Vector2(8f, 6f);
    
    [Header("Right Corners - Active")]
    public Vector2 rightCornerBL_Active = new Vector2(-20f, 0f);
    public Vector2 rightCornerTL_Active = new Vector2(14f, 0f);
    public Vector2 rightCornerBR_Active = new Vector2(18f, 10f);
    public Vector2 rightCornerTR_Active = new Vector2(22f, -8f);
    
    // ========================================
    // COMMON
    // ========================================
    [Header("Shadow")]
    public Color shadowColor = new Color(0f, 0f, 0f, 0.4f);
    public float shadowIntensity = 6f;
    
    [Header("Text")]
    public Color textColorLeft = new Color(0.96f, 0.96f, 0.96f, 1f);
    public Color textColorRight = new Color(0.06f, 0.06f, 0.09f, 1f);
    
    [Header("Animation")]
    public float transitionDuration = 0.15f;
    
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
    
    public void GetLeftState(float t, 
        out Color fill, out Vector2 offset, out Vector2 expand,
        out Vector2 cBL, out Vector2 cTL, out Vector2 cBR, out Vector2 cTR)
    {
        fill = Color.Lerp(leftFillNormal, leftFillActive, t);
        offset = Vector2.Lerp(leftOffsetNormal, leftOffsetActive, t);
        expand = Vector2.Lerp(leftExpandNormal, leftExpandActive, t);
        cBL = Vector2.Lerp(leftCornerBL_Normal, leftCornerBL_Active, t);
        cTL = Vector2.Lerp(leftCornerTL_Normal, leftCornerTL_Active, t);
        cBR = Vector2.Lerp(leftCornerBR_Normal, leftCornerBR_Active, t);
        cTR = Vector2.Lerp(leftCornerTR_Normal, leftCornerTR_Active, t);
    }
    
    public void GetRightState(float t,
        out Color fill, out Vector2 offset, out Vector2 expand,
        out Vector2 cBL, out Vector2 cTL, out Vector2 cBR, out Vector2 cTR)
    {
        fill = Color.Lerp(rightFillNormal, rightFillActive, t);
        offset = Vector2.Lerp(rightOffsetNormal, rightOffsetActive, t);
        expand = Vector2.Lerp(rightExpandNormal, rightExpandActive, t);
        cBL = Vector2.Lerp(rightCornerBL_Normal, rightCornerBL_Active, t);
        cTL = Vector2.Lerp(rightCornerTL_Normal, rightCornerTL_Active, t);
        cBR = Vector2.Lerp(rightCornerBR_Normal, rightCornerBR_Active, t);
        cTR = Vector2.Lerp(rightCornerTR_Normal, rightCornerTR_Active, t);
    }
}
