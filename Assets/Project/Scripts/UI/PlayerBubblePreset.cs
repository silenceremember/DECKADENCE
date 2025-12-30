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
    
    [Header("Left - Active")]
    public Color leftFillActive = new Color(0.10f, 0.10f, 0.16f, 1f);
    public Vector2 leftOffsetActive = new Vector2(-8f, 4f);
    public Vector2 leftExpandActive = new Vector2(12f, 8f);
    
    [Header("Left Corners - Active")]
    public Vector2 leftCornerBL_Active = new Vector2(-16f, -6f);
    public Vector2 leftCornerTL_Active = new Vector2(-18f, 8f);
    public Vector2 leftCornerBR_Active = new Vector2(-8f, 0f);
    public Vector2 leftCornerTR_Active = new Vector2(10f, 0f);
    
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
    
    [Header("Right - Active")]
    public Color rightFillActive = new Color(1f, 1f, 1f, 1f);
    public Vector2 rightOffsetActive = new Vector2(8f, 4f);
    public Vector2 rightExpandActive = new Vector2(12f, 8f);
    
    [Header("Right Corners - Active")]
    public Vector2 rightCornerBL_Active = new Vector2(8f, 0f);
    public Vector2 rightCornerTL_Active = new Vector2(-10f, 0f);
    public Vector2 rightCornerBR_Active = new Vector2(16f, -6f);
    public Vector2 rightCornerTR_Active = new Vector2(18f, 8f);
    
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
