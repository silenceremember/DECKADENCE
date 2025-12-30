using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Renderer for player's split bubble - Persona-style two-tone diagonal split.
/// Uses PlayerBubblePreset for configuration and state interpolation.
/// </summary>
[RequireComponent(typeof(Graphic))]
[ExecuteAlways]
public class PlayerBubbleRenderer : MonoBehaviour, IMeshModifier
{
    [Header("Preset")]
    [Tooltip("Configuration preset for colors, offsets, etc.")]
    public PlayerBubblePreset preset;
    
    [Header("State")]
    [Range(0f, 1f)]
    [Tooltip("Left side progress: 0 = normal, 1 = active")]
    public float leftProgress = 0f;
    
    [Range(0f, 1f)]
    [Tooltip("Right side progress: 0 = normal, 1 = active")]
    public float rightProgress = 0f;
    
    [Header("Runtime Overrides (optional)")]
    [Tooltip("Override split position (if preset is null)")]
    public float splitPositionOverride = 0.5f;
    [Tooltip("Override split angle (if preset is null)")]
    public float splitAngleOverride = 15f;
    
    private Graphic _graphic;
    private Canvas _canvas;
    private Material _materialInstance;
    private RectTransform _rectTransform;
    private Vector2 _cachedShadowOffset;
    
    // Shader property IDs
    private static readonly int RectSizeID = Shader.PropertyToID("_RectSize");
    private static readonly int CanvasScaleID = Shader.PropertyToID("_CanvasScale");
    private static readonly int SplitAngleID = Shader.PropertyToID("_SplitAngle");
    private static readonly int SplitPositionID = Shader.PropertyToID("_SplitPosition");
    private static readonly int LeftFillColorID = Shader.PropertyToID("_LeftFillColor");
    private static readonly int LeftBorderColorID = Shader.PropertyToID("_LeftBorderColor");
    private static readonly int LeftOffsetID = Shader.PropertyToID("_LeftOffset");
    private static readonly int LeftExpandID = Shader.PropertyToID("_LeftExpand");
    private static readonly int LeftCornersBLTLID = Shader.PropertyToID("_LeftCornersBLTL");
    private static readonly int LeftCornersBRTRID = Shader.PropertyToID("_LeftCornersBRTR");
    private static readonly int RightFillColorID = Shader.PropertyToID("_RightFillColor");
    private static readonly int RightBorderColorID = Shader.PropertyToID("_RightBorderColor");
    private static readonly int RightOffsetID = Shader.PropertyToID("_RightOffset");
    private static readonly int RightExpandID = Shader.PropertyToID("_RightExpand");
    private static readonly int RightCornersBLTLID = Shader.PropertyToID("_RightCornersBLTL");
    private static readonly int RightCornersBRTRID = Shader.PropertyToID("_RightCornersBRTR");
    private static readonly int BorderThicknessID = Shader.PropertyToID("_BorderThickness");
    private static readonly int ShadowColorID = Shader.PropertyToID("_ShadowColor");
    private static readonly int ShadowIntensityID = Shader.PropertyToID("_ShadowIntensity");
    
    void Awake()
    {
        _graphic = GetComponent<Graphic>();
        _rectTransform = GetComponent<RectTransform>();
    }
    
    void Start()
    {
        _canvas = GetComponentInParent<Canvas>();
        CreateMaterialInstance();
    }
    
    void OnEnable()
    {
        if (_graphic == null)
            _graphic = GetComponent<Graphic>();
        
        CreateMaterialInstance();
        
        if (_graphic != null)
            _graphic.SetVerticesDirty();
    }
    
    void OnDisable()
    {
        if (_graphic != null)
            _graphic.SetVerticesDirty();
    }
    
    void OnDestroy()
    {
        if (_materialInstance != null)
        {
            if (Application.isPlaying)
                Destroy(_materialInstance);
            else
                DestroyImmediate(_materialInstance);
        }
    }
    
    void CreateMaterialInstance()
    {
        if (_graphic == null) 
        {
            _graphic = GetComponent<Graphic>();
            if (_graphic == null) return;
        }
        
        if (_materialInstance != null) return;
        
        if (_graphic.material != null && _graphic.material.shader != null)
        {
            if (_graphic.material.shader.name == "DECKADENCE/UI/PlayerBubble")
            {
                _materialInstance = new Material(_graphic.material);
                _graphic.material = _materialInstance;
            }
        }
    }
    
    void LateUpdate()
    {
        if (_materialInstance == null) return;
        
        if (_rectTransform == null)
            _rectTransform = GetComponent<RectTransform>();
        
        if (_canvas == null)
            _canvas = GetComponentInParent<Canvas>();
        
        float scaleFactor = _canvas != null ? _canvas.scaleFactor : 1f;
        Rect rect = _rectTransform.rect;
        
        _materialInstance.SetVector(RectSizeID, new Vector4(rect.width, rect.height, 0, 0));
        _materialInstance.SetFloat(CanvasScaleID, scaleFactor);
        
        if (preset != null)
        {
            ApplyPreset();
        }
        else
        {
            _materialInstance.SetFloat(SplitAngleID, splitAngleOverride);
            _materialInstance.SetFloat(SplitPositionID, splitPositionOverride);
        }
        
        // Shadow
        Vector2 shadowDir = CalculateShadowDirection();
        Vector2 newShadowOffset = shadowDir * (preset != null ? preset.shadowIntensity : 5f);
        
        if (Vector2.Distance(newShadowOffset, _cachedShadowOffset) > 0.1f)
        {
            _cachedShadowOffset = newShadowOffset;
            if (_graphic != null)
                _graphic.SetVerticesDirty();
        }
    }
    
    void ApplyPreset()
    {
        // Split state based on which side is active
        preset.GetSplitState(leftProgress, rightProgress, out float splitAngle, out float splitPosition);
        _materialInstance.SetFloat(SplitAngleID, splitAngle);
        _materialInstance.SetFloat(SplitPositionID, splitPosition);
        _materialInstance.SetColor(ShadowColorID, preset.shadowColor);
        _materialInstance.SetFloat(ShadowIntensityID, preset.shadowIntensity);
        
        // Left side interpolation
        preset.GetLeftState(leftProgress,
            out Color leftFill,
            out Vector2 leftOffset, out Vector2 leftExpand,
            out Vector2 lBL, out Vector2 lTL, out Vector2 lBR, out Vector2 lTR);
        
        _materialInstance.SetColor(LeftFillColorID, leftFill);
        _materialInstance.SetVector(LeftOffsetID, new Vector4(leftOffset.x, leftOffset.y, 0, 0));
        _materialInstance.SetVector(LeftExpandID, new Vector4(leftExpand.x, leftExpand.y, 0, 0));
        _materialInstance.SetVector(LeftCornersBLTLID, new Vector4(lBL.x, lBL.y, lTL.x, lTL.y));
        _materialInstance.SetVector(LeftCornersBRTRID, new Vector4(lBR.x, lBR.y, lTR.x, lTR.y));
        
        // Right side interpolation
        preset.GetRightState(rightProgress,
            out Color rightFill,
            out Vector2 rightOffset, out Vector2 rightExpand,
            out Vector2 rBL, out Vector2 rTL, out Vector2 rBR, out Vector2 rTR);
        
        _materialInstance.SetColor(RightFillColorID, rightFill);
        _materialInstance.SetVector(RightOffsetID, new Vector4(rightOffset.x, rightOffset.y, 0, 0));
        _materialInstance.SetVector(RightExpandID, new Vector4(rightExpand.x, rightExpand.y, 0, 0));
        _materialInstance.SetVector(RightCornersBLTLID, new Vector4(rBL.x, rBL.y, rTL.x, rTL.y));
        _materialInstance.SetVector(RightCornersBRTRID, new Vector4(rBR.x, rBR.y, rTR.x, rTR.y));
    }
    
    Vector2 CalculateShadowDirection()
    {
        if (ShadowLightSource.Instance == null)
            return new Vector2(1, -1).normalized;
        
        return ShadowLightSource.Instance.GetShadowDirection(GetScreenPosition());
    }
    
    Vector2 GetScreenPosition()
    {
        if (_canvas == null) _canvas = GetComponentInParent<Canvas>();
        
        Camera cam = null;
        if (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            cam = _canvas.worldCamera;
        if (cam == null) cam = Camera.main;
        
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cam, transform.position);
        screenPos -= new Vector2(Screen.width / 2f, Screen.height / 2f);
        
        return screenPos;
    }
    
    // ========================================
    // MESH MODIFICATION
    // ========================================
    
    public void ModifyMesh(Mesh mesh)
    {
        using (var vh = new VertexHelper(mesh))
        {
            ModifyMesh(vh);
            vh.FillMesh(mesh);
        }
    }
    
    public void ModifyMesh(VertexHelper vh)
    {
        if (!enabled || !gameObject.activeInHierarchy)
            return;
        
        if (_rectTransform == null)
            _rectTransform = GetComponent<RectTransform>();
        
        if (_canvas == null)
            _canvas = GetComponentInParent<Canvas>();
        
        Rect rect = _rectTransform.rect;
        float scaleFactor = _canvas != null ? _canvas.scaleFactor : 1f;
        
        float shadowIntensity = preset != null ? preset.shadowIntensity : 5f;
        float expand = shadowIntensity + 100f; // Buffer for corners and offsets
        
        float left = rect.xMin - expand;
        float right = rect.xMax + expand;
        float bottom = rect.yMin - expand;
        float top = rect.yMax + expand;
        
        float uvLeft = -expand / rect.width;
        float uvRight = 1f + expand / rect.width;
        float uvBottom = -expand / rect.height;
        float uvTop = 1f + expand / rect.height;
        
        vh.Clear();
        
        Color32 color32 = _graphic != null ? _graphic.color : Color.white;
        UIVertex vert = UIVertex.simpleVert;
        vert.color = color32;
        
        Vector3 shadowOffset = new Vector3(_cachedShadowOffset.x, _cachedShadowOffset.y, 0) / scaleFactor;
        shadowOffset = transform.InverseTransformVector(shadowOffset);
        
        // Quad 0: Shadow
        vert.uv1 = new Vector2(0, 1);
        
        vert.position = new Vector3(left, bottom, 0) + shadowOffset;
        vert.uv0 = new Vector2(uvLeft, uvBottom);
        vh.AddVert(vert);
        
        vert.position = new Vector3(left, top, 0) + shadowOffset;
        vert.uv0 = new Vector2(uvLeft, uvTop);
        vh.AddVert(vert);
        
        vert.position = new Vector3(right, top, 0) + shadowOffset;
        vert.uv0 = new Vector2(uvRight, uvTop);
        vh.AddVert(vert);
        
        vert.position = new Vector3(right, bottom, 0) + shadowOffset;
        vert.uv0 = new Vector2(uvRight, uvBottom);
        vh.AddVert(vert);
        
        vh.AddTriangle(0, 1, 2);
        vh.AddTriangle(2, 3, 0);
        
        // Quad 1: Main bubble
        vert.uv1 = new Vector2(0, 0);
        
        vert.position = new Vector3(left, bottom, 0);
        vert.uv0 = new Vector2(uvLeft, uvBottom);
        vh.AddVert(vert);
        
        vert.position = new Vector3(left, top, 0);
        vert.uv0 = new Vector2(uvLeft, uvTop);
        vh.AddVert(vert);
        
        vert.position = new Vector3(right, top, 0);
        vert.uv0 = new Vector2(uvRight, uvTop);
        vh.AddVert(vert);
        
        vert.position = new Vector3(right, bottom, 0);
        vert.uv0 = new Vector2(uvRight, uvBottom);
        vh.AddVert(vert);
        
        vh.AddTriangle(4, 5, 6);
        vh.AddTriangle(6, 7, 4);
    }
    
    void OnValidate()
    {
        if (_graphic != null)
            _graphic.SetVerticesDirty();
    }
    
    // ========================================
    // PUBLIC API
    // ========================================
    
    /// <summary>
    /// Set left side active progress (0-1).
    /// </summary>
    public void SetLeftProgress(float t) => leftProgress = Mathf.Clamp01(t);
    
    /// <summary>
    /// Set right side active progress (0-1).
    /// </summary>
    public void SetRightProgress(float t) => rightProgress = Mathf.Clamp01(t);
    
    /// <summary>
    /// Get text color for left side at current progress.
    /// </summary>
    public Color GetLeftTextColor()
    {
        return preset != null ? preset.textColorLeft : Color.white;
    }
    
    /// <summary>
    /// Get text color for right side at current progress.
    /// </summary>
    public Color GetRightTextColor()
    {
        return preset != null ? preset.textColorRight : Color.black;
    }
}
