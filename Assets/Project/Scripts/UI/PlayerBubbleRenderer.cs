using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Renderer for player's split bubble - two-tone diagonal split effect.
/// Designed for the 54th Card (Player) action choices.
/// </summary>
[RequireComponent(typeof(Graphic))]
[ExecuteAlways]
public class PlayerBubbleRenderer : MonoBehaviour, IMeshModifier
{
    [Header("Split Settings")]
    [Range(-90f, 90f)]
    [Tooltip("Angle of the split line in degrees. 0 = vertical, positive = tilted right")]
    public float splitAngle = 15f;
    
    [Range(0f, 1f)]
    [Tooltip("Position of split. 0 = all left, 0.5 = center, 1 = all right")]
    public float splitPosition = 0.5f;
    
    [Header("Left Side Colors")]
    public Color leftFillColor = new Color(0.15f, 0.15f, 0.2f, 1f);
    public Color leftActiveColor = new Color(0.1f, 0.1f, 0.15f, 1f);
    public Color leftBorderColor = new Color(0.3f, 0.3f, 0.4f, 1f);
    
    [Header("Right Side Colors")]
    public Color rightFillColor = new Color(0.85f, 0.85f, 0.9f, 1f);
    public Color rightActiveColor = new Color(0.95f, 0.95f, 1f, 1f);
    public Color rightBorderColor = new Color(0.6f, 0.6f, 0.7f, 1f);
    
    [Header("Border")]
    public float borderThickness = 2f;
    public float splitBorderThickness = 2f;
    
    [Header("Corner")]
    public float cornerCut = 15f;
    
    [Header("Shadow")]
    public Color shadowColor = new Color(0f, 0f, 0f, 0.5f);
    public float shadowIntensity = 5f;
    
    [Header("Runtime")]
    [Range(0f, 1f)]
    [Tooltip("Progress towards active state (0 = normal, 1 = active)")]
    [SerializeField] private float _activeProgress = 0f;
    
    /// <summary>
    /// Progress towards active state. Interpolates fill colors.
    /// </summary>
    public float ActiveProgress
    {
        get => _activeProgress;
        set => _activeProgress = Mathf.Clamp01(value);
    }
    
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
    private static readonly int RightFillColorID = Shader.PropertyToID("_RightFillColor");
    private static readonly int RightBorderColorID = Shader.PropertyToID("_RightBorderColor");
    private static readonly int BorderThicknessID = Shader.PropertyToID("_BorderThickness");
    private static readonly int SplitBorderThicknessID = Shader.PropertyToID("_SplitBorderThickness");
    private static readonly int CornerCutID = Shader.PropertyToID("_CornerCut");
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
        
        // Update shader properties
        _materialInstance.SetVector(RectSizeID, new Vector4(rect.width, rect.height, 0, 0));
        _materialInstance.SetFloat(CanvasScaleID, scaleFactor);
        _materialInstance.SetFloat(SplitAngleID, splitAngle);
        _materialInstance.SetFloat(SplitPositionID, splitPosition);
        
        // Interpolate colors based on activeProgress
        Color currentLeftFill = Color.Lerp(leftFillColor, leftActiveColor, _activeProgress);
        Color currentRightFill = Color.Lerp(rightFillColor, rightActiveColor, _activeProgress);
        
        _materialInstance.SetColor(LeftFillColorID, currentLeftFill);
        _materialInstance.SetColor(LeftBorderColorID, leftBorderColor);
        _materialInstance.SetColor(RightFillColorID, currentRightFill);
        _materialInstance.SetColor(RightBorderColorID, rightBorderColor);
        
        _materialInstance.SetFloat(BorderThicknessID, borderThickness);
        _materialInstance.SetFloat(SplitBorderThicknessID, splitBorderThickness);
        _materialInstance.SetFloat(CornerCutID, cornerCut);
        
        _materialInstance.SetColor(ShadowColorID, shadowColor);
        _materialInstance.SetFloat(ShadowIntensityID, shadowIntensity);
        
        // Shadow direction from light source - update mesh if changed
        Vector2 shadowDir = CalculateShadowDirection();
        Vector2 newShadowOffset = shadowDir * shadowIntensity;
        
        if (Vector2.Distance(newShadowOffset, _cachedShadowOffset) > 0.1f)
        {
            _cachedShadowOffset = newShadowOffset;
            if (_graphic != null)
                _graphic.SetVerticesDirty();
        }
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
        
        // Expand for shadow (like MultiBubble)
        float expand = shadowIntensity + cornerCut;
        
        float left = rect.xMin - expand;
        float right = rect.xMax + expand;
        float bottom = rect.yMin - expand;
        float top = rect.yMax + expand;
        
        // UV mapping: 0-1 maps to rect.width/height, negative/positive expansion 
        float uvLeft = -expand / rect.width;
        float uvRight = 1f + expand / rect.width;
        float uvBottom = -expand / rect.height;
        float uvTop = 1f + expand / rect.height;
        
        vh.Clear();
        
        Color32 color32 = _graphic != null ? _graphic.color : Color.white;
        UIVertex vert = UIVertex.simpleVert;
        vert.color = color32;
        
        // Calculate shadow offset in local space
        Vector3 shadowOffset = new Vector3(_cachedShadowOffset.x, _cachedShadowOffset.y, 0) / scaleFactor;
        shadowOffset = transform.InverseTransformVector(shadowOffset);
        
        // Quad 0: Shadow (offset position, quadType = 1 in uv1.y)
        vert.uv1 = new Vector2(0, 1); // quadType 1 = shadow
        
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
        
        // Quad 1: Main bubble (no offset, quadType = 0 in uv1.y)
        vert.uv1 = new Vector2(0, 0); // quadType 0 = fill
        
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
    
    /// <summary>
    /// Set split position with animation support.
    /// Use with DOTween: renderer.DOSplitPosition(target, duration)
    /// </summary>
    public void SetSplitPosition(float position)
    {
        splitPosition = Mathf.Clamp01(position);
    }
    
    /// <summary>
    /// Set split angle with animation support.
    /// </summary>
    public void SetSplitAngle(float angle)
    {
        splitAngle = Mathf.Clamp(angle, -90f, 90f);
    }
}
