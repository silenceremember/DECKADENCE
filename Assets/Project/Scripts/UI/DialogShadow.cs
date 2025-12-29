using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Dialog box shadow with dynamic arrow positioning.
/// Styling controlled via DialogBubblePreset ScriptableObject.
/// </summary>
[RequireComponent(typeof(Graphic))]
[ExecuteAlways]
public class DialogShadow : MonoBehaviour, IMeshModifier
{
    [Header("Preset")]
    [Tooltip("Visual style preset (controls colors, border, shadows)")]
    public DialogBubblePreset bubblePreset;
    
    [Header("Arrow Settings")]
    [Tooltip("Enable or disable the arrow")]
    public bool showArrow = true;
    [Range(0f, 1f)]
    [Tooltip("Position on perimeter: 0=center bottom, 0.25=center right, 0.5=center top, 0.75=center left")]
    public float arrowPerimeter = 0f;
    [Tooltip("Arrow size in pixels (scale-independent)")]
    public float arrowSizePixels = 30f;
    [Tooltip("Arrow width in pixels (scale-independent)")]
    public float arrowWidthPixels = 40f;
    
    [Header("Target (Optional)")]
    [Tooltip("If set, arrow will point towards this transform")]
    public Transform arrowTarget;
    
    [Header("Runtime Overrides")]
    [Tooltip("Raised = border pushed forward, Recessed = frame above content")]
    public bool borderShadowRaised = true;
    
    [Header("Debug")]
    [SerializeField] private Vector2 currentShadowOffset;
    [SerializeField] private Vector2 currentSecondShadowOffset;
    
    private Graphic _graphic;
    private Canvas _canvas;
    private Material _materialInstance;
    private RectTransform _rectTransform;
    private Camera _canvasCamera;
    
    private static readonly int ShadowColorID = Shader.PropertyToID("_ShadowColor");
    private static readonly int ShowSecondShadowID = Shader.PropertyToID("_ShowSecondShadow");
    private static readonly int SecondShadowColorID = Shader.PropertyToID("_SecondShadowColor");
    private static readonly int ArrowPerimeterID = Shader.PropertyToID("_ArrowPerimeter");
    private static readonly int ArrowSizePixelsID = Shader.PropertyToID("_ArrowSizePixels");
    private static readonly int ArrowWidthPixelsID = Shader.PropertyToID("_ArrowWidthPixels");
    private static readonly int ShowArrowID = Shader.PropertyToID("_ShowArrow");
    private static readonly int RectSizeID = Shader.PropertyToID("_RectSize");
    private static readonly int RectScreenPosID = Shader.PropertyToID("_RectScreenPos");
    private static readonly int CanvasScaleID = Shader.PropertyToID("_CanvasScale");
    private static readonly int ShowBorderID = Shader.PropertyToID("_ShowBorder");
    private static readonly int BorderThicknessPixelsID = Shader.PropertyToID("_BorderThicknessPixels");
    private static readonly int BorderOffsetPixelsID = Shader.PropertyToID("_BorderOffsetPixels");
    private static readonly int BorderColorID = Shader.PropertyToID("_BorderColor");
    private static readonly int BorderStyleID = Shader.PropertyToID("_BorderStyle");
    private static readonly int BorderDashLengthID = Shader.PropertyToID("_BorderDashLength");
    private static readonly int BorderDashGapID = Shader.PropertyToID("_BorderDashGap");
    private static readonly int BorderSecondOffsetID = Shader.PropertyToID("_BorderSecondOffset");
    private static readonly int ShowInnerShadowID = Shader.PropertyToID("_ShowInnerShadow");
    private static readonly int BorderShadowIntensityID = Shader.PropertyToID("_BorderShadowIntensity");
    private static readonly int InnerShadowColorID = Shader.PropertyToID("_InnerShadowColor");
    private static readonly int LightDirectionID = Shader.PropertyToID("_LightDirection");
    
    void Awake()
    {
        _graphic = GetComponent<Graphic>();
        _rectTransform = GetComponent<RectTransform>();
    }
    
    void Start()
    {
        CreateMaterialInstance();
        _canvas = GetComponentInParent<Canvas>();
    }
    
    void OnEnable()
    {
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
        if (_graphic == null) return;
        
        if (_graphic.material != null && _graphic.material.shader.name == "DECKADENCE/UI/DialogShadow")
        {
            _materialInstance = new Material(_graphic.material);
            _graphic.material = _materialInstance;
        }
    }
    
    private float _lastArrowPerimeter;
    private float _lastArrowSize;
    private Vector2 _lastRectSize;
    private bool _lastShowSecondShadow;
    
    void LateUpdate()
    {
        if (_materialInstance == null) return;
        if (bubblePreset == null) return;  // Need preset for styling
        
        if (_rectTransform == null)
            _rectTransform = GetComponent<RectTransform>();
        
        if (_canvas == null)
            _canvas = GetComponentInParent<Canvas>();
        
        // Get Canvas scale factor
        float scaleFactor = _canvas != null ? _canvas.scaleFactor : 1f;
        
        Rect rect = _rectTransform.rect;
        
        // Pass rect size in CANVAS UNITS (not screen pixels!)
        _materialInstance.SetVector(RectSizeID, new Vector4(rect.width, rect.height, 0, 0));
        // Pass scale factor so shader can convert pixel values to canvas units
        _materialInstance.SetFloat(CanvasScaleID, scaleFactor);
        
        // Calculate screen position of rect corner
        if (_canvasCamera == null && _canvas != null)
            _canvasCamera = _canvas.worldCamera ?? Camera.main;
        
        // Get bottom-left corner in screen pixels
        Vector3[] corners = new Vector3[4];
        _rectTransform.GetWorldCorners(corners);
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(_canvasCamera, corners[0]);
        _materialInstance.SetVector(RectScreenPosID, new Vector4(screenPos.x, screenPos.y, 0, 0));
        
        // Rebuild mesh if rect size changed
        Vector2 currentRectSize = new Vector2(rect.width, rect.height);
        if (Vector2.Distance(currentRectSize, _lastRectSize) > 0.1f)
        {
            _lastRectSize = currentRectSize;
            if (_graphic != null)
                _graphic.SetVerticesDirty();
        }
        
        // === Read ALL styling from bubblePreset ===
        // NOTE: fillColor/activeColor are handled by CardDisplay for animation
        
        // Shadow colors
        _materialInstance.SetColor(ShadowColorID, bubblePreset.shadowColor);
        _materialInstance.SetFloat(ShowSecondShadowID, bubblePreset.showSecondShadow ? 1f : 0f);
        _materialInstance.SetColor(SecondShadowColorID, bubblePreset.secondShadowColor);
        
        // Rebuild mesh if showSecondShadow changed (mesh controls second shadow rendering)
        if (bubblePreset.showSecondShadow != _lastShowSecondShadow)
        {
            _lastShowSecondShadow = bubblePreset.showSecondShadow;
            if (_graphic != null)
                _graphic.SetVerticesDirty();
        }
        
        // Auto-position arrow towards target
        if (arrowTarget != null)
        {
            UpdateArrowTowardsTarget();
        }
        
        // Arrow properties (local to instance)
        _materialInstance.SetFloat(ShowArrowID, showArrow ? 1f : 0f);
        _materialInstance.SetFloat(ArrowPerimeterID, arrowPerimeter);
        _materialInstance.SetFloat(ArrowSizePixelsID, arrowSizePixels);
        _materialInstance.SetFloat(ArrowWidthPixelsID, arrowWidthPixels);
        
        // Border properties from preset
        _materialInstance.SetFloat(ShowBorderID, bubblePreset.showBorder ? 1f : 0f);
        _materialInstance.SetFloat(BorderThicknessPixelsID, bubblePreset.borderThickness);
        _materialInstance.SetFloat(BorderOffsetPixelsID, bubblePreset.borderOffset);
        _materialInstance.SetColor(BorderColorID, bubblePreset.borderColor);
        
        // Border style from preset
        _materialInstance.SetFloat(BorderStyleID, bubblePreset.borderStyle);
        _materialInstance.SetFloat(BorderDashLengthID, bubblePreset.dashLength);
        _materialInstance.SetFloat(BorderDashGapID, bubblePreset.dashGap);
        _materialInstance.SetFloat(BorderSecondOffsetID, bubblePreset.secondBorderOffset);
        
        // Inner shadow from preset
        _materialInstance.SetFloat(ShowInnerShadowID, bubblePreset.showInnerShadow ? 1f : 0f);
        _materialInstance.SetFloat(BorderShadowIntensityID, bubblePreset.innerShadowIntensity);
        _materialInstance.SetColor(InnerShadowColorID, bubblePreset.innerShadowColor);
        
        // Light direction for inner shadow
        Vector2 lightDir = CalculateShadowDirection();
        float invert = borderShadowRaised ? -1f : 1f;
        float innerShadowIntensity = bubblePreset.innerShadowIntensity;
        _materialInstance.SetVector(LightDirectionID, new Vector4(lightDir.x * invert * innerShadowIntensity, lightDir.y * invert * innerShadowIntensity, 0, 0));
        
        // Check if arrow changed (need to rebuild mesh for arrow extension)
        bool arrowChanged = Mathf.Abs(arrowPerimeter - _lastArrowPerimeter) > 0.01f ||
                           Mathf.Abs(arrowSizePixels - _lastArrowSize) > 0.1f;
        
        if (arrowChanged)
        {
            _lastArrowPerimeter = arrowPerimeter;
            _lastArrowSize = arrowSizePixels;
            if (_graphic != null)
                _graphic.SetVerticesDirty();
        }
        
        // Update shadow offsets using preset intensities
        Vector2 shadowDir = CalculateShadowDirection();
        Vector2 newOffset = shadowDir * bubblePreset.shadowIntensity;
        Vector2 newSecondOffset = shadowDir * bubblePreset.secondShadowIntensity;
        
        if (Vector2.Distance(newOffset, currentShadowOffset) > 0.1f ||
            Vector2.Distance(newSecondOffset, currentSecondShadowOffset) > 0.1f)
        {
            currentShadowOffset = newOffset;
            currentSecondShadowOffset = newSecondOffset;
            if (_graphic != null)
                _graphic.SetVerticesDirty();
        }
    }
    
    void UpdateArrowTowardsTarget()
    {
        if (_rectTransform == null || arrowTarget == null) return;
        
        // Get target position in local space
        Vector3 targetWorld = arrowTarget.position;
        Vector3 localTarget = _rectTransform.InverseTransformPoint(targetWorld);
        
        Rect rect = _rectTransform.rect;
        
        // Calculate center of rect
        Vector2 center = rect.center;
        
        // Direction from center to target
        Vector2 toTarget = new Vector2(localTarget.x - center.x, localTarget.y - center.y);
        
        // Calculate angle using atan2 (gives smooth continuous angle)
        // atan2 returns: 0 at right, PI/2 at top, PI/-PI at left, -PI/2 at bottom
        float angle = Mathf.Atan2(toTarget.y, toTarget.x);
        
        // Convert angle to our perimeter scheme:
        // 0 = center bottom (down), 0.25 = center right, 0.5 = center top, 0.75 = center left
        // atan2: -PI/2 = bottom, 0 = right, PI/2 = top, ±PI = left
        
        // Shift and normalize: we want bottom(-PI/2) to map to 0
        // angle + PI/2 gives: 0 = bottom, PI/2 = right, PI = top, 3PI/2 = left
        float shiftedAngle = angle + Mathf.PI * 0.5f;
        if (shiftedAngle < 0) shiftedAngle += Mathf.PI * 2f;
        
        // Normalize to 0-1: divide by 2*PI
        arrowPerimeter = shiftedAngle / (Mathf.PI * 2f);
        
        // Clamp away from exact corners to avoid edge cases
        // Corners are at 0.125, 0.375, 0.625, 0.875
        // This is optional - the shader handles corners smoothly now
    }
    
    Vector2 CalculateShadowDirection()
    {
        if (ShadowLightSource.Instance == null)
            return new Vector2(1, -1).normalized;
        
        Vector2 screenPos = GetScreenPosition();
        return ShadowLightSource.Instance.GetShadowDirection(screenPos);
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
    
    // IMeshModifier implementation
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
        
        Rect rect = _rectTransform.rect;
        
        // Arrow extension in local units
        float arrowExtension = arrowSizePixels;
        
        // Expand mesh in ALL directions so arrow can be on any edge
        // This ensures the quad is large enough for the arrow wherever it is
        float expandTop = arrowExtension;
        float expandBottom = arrowExtension;
        float expandLeft = arrowExtension;
        float expandRight = arrowExtension;
        
        // Calculate expanded rect
        float left = rect.xMin - expandLeft;
        float right = rect.xMax + expandRight;
        float bottom = rect.yMin - expandBottom;
        float top = rect.yMax + expandTop;
        
        // Calculate UV mapping: original rect maps to 0-1, arrow area extends beyond
        float uvLeft = -expandLeft / rect.width;
        float uvRight = 1f + expandRight / rect.width;
        float uvBottom = -expandBottom / rect.height;
        float uvTop = 1f + expandTop / rect.height;
        
        vh.Clear();
        
        // Build expanded quad with proper UVs
        
        // Calculate shadow offsets in local space
        Vector3 primaryOffset = new Vector3(currentShadowOffset.x, currentShadowOffset.y, 0);
        Vector3 secondOffset = new Vector3(currentSecondShadowOffset.x, currentSecondShadowOffset.y, 0);
        if (_canvas != null)
        {
            primaryOffset /= _canvas.scaleFactor;
            secondOffset /= _canvas.scaleFactor;
        }
        primaryOffset = transform.InverseTransformVector(primaryOffset);
        secondOffset = transform.InverseTransformVector(secondOffset);
        
        Color32 color32 = _graphic != null ? _graphic.color : Color.white;
        
        UIVertex vert = UIVertex.simpleVert;
        vert.color = color32;
        
        // Primary shadow quad (4 vertices) - rendered first (behind)
        // Bottom-left
        vert.position = new Vector3(left, bottom, 0) + primaryOffset;
        vert.uv0 = new Vector2(uvLeft, uvBottom);
        vert.uv1 = new Vector2(1, 0); // Shadow flag = 1 (primary shadow)
        vh.AddVert(vert);
        
        // Top-left
        vert.position = new Vector3(left, top, 0) + primaryOffset;
        vert.uv0 = new Vector2(uvLeft, uvTop);
        vert.uv1 = new Vector2(1, 0);
        vh.AddVert(vert);
        
        // Top-right
        vert.position = new Vector3(right, top, 0) + primaryOffset;
        vert.uv0 = new Vector2(uvRight, uvTop);
        vert.uv1 = new Vector2(1, 0);
        vh.AddVert(vert);
        
        // Bottom-right
        vert.position = new Vector3(right, bottom, 0) + primaryOffset;
        vert.uv0 = new Vector2(uvRight, uvBottom);
        vert.uv1 = new Vector2(1, 0);
        vh.AddVert(vert);
        
        // Primary shadow triangles
        vh.AddTriangle(0, 1, 2);
        vh.AddTriangle(2, 3, 0);
        
        // Secondary shadow quad (4 vertices) - rendered between primary shadow and main
        bool useSecondShadow = bubblePreset != null && bubblePreset.showSecondShadow;
        if (useSecondShadow)
        {
            // Bottom-left
            vert.position = new Vector3(left, bottom, 0) + secondOffset;
            vert.uv0 = new Vector2(uvLeft, uvBottom);
            vert.uv1 = new Vector2(2, 0); // Shadow flag = 2 (secondary shadow)
            vh.AddVert(vert);
            
            // Top-left
            vert.position = new Vector3(left, top, 0) + secondOffset;
            vert.uv0 = new Vector2(uvLeft, uvTop);
            vert.uv1 = new Vector2(2, 0);
            vh.AddVert(vert);
            
            // Top-right
            vert.position = new Vector3(right, top, 0) + secondOffset;
            vert.uv0 = new Vector2(uvRight, uvTop);
            vert.uv1 = new Vector2(2, 0);
            vh.AddVert(vert);
            
            // Bottom-right
            vert.position = new Vector3(right, bottom, 0) + secondOffset;
            vert.uv0 = new Vector2(uvRight, uvBottom);
            vert.uv1 = new Vector2(2, 0);
            vh.AddVert(vert);
            
            // Secondary shadow triangles
            vh.AddTriangle(4, 5, 6);
            vh.AddTriangle(6, 7, 4);
        }
        
        // Main quad (4 vertices)
        int mainStartIdx = useSecondShadow ? 8 : 4;
        
        // Bottom-left
        vert.position = new Vector3(left, bottom, 0);
        vert.uv0 = new Vector2(uvLeft, uvBottom);
        vert.uv1 = new Vector2(0, 0); // Main flag = 0
        vh.AddVert(vert);
        
        // Top-left
        vert.position = new Vector3(left, top, 0);
        vert.uv0 = new Vector2(uvLeft, uvTop);
        vert.uv1 = new Vector2(0, 0);
        vh.AddVert(vert);
        
        // Top-right
        vert.position = new Vector3(right, top, 0);
        vert.uv0 = new Vector2(uvRight, uvTop);
        vert.uv1 = new Vector2(0, 0);
        vh.AddVert(vert);
        
        // Bottom-right
        vert.position = new Vector3(right, bottom, 0);
        vert.uv0 = new Vector2(uvRight, uvBottom);
        vert.uv1 = new Vector2(0, 0);
        vh.AddVert(vert);
        
        // Main triangles
        vh.AddTriangle(mainStartIdx, mainStartIdx + 1, mainStartIdx + 2);
        vh.AddTriangle(mainStartIdx + 2, mainStartIdx + 3, mainStartIdx);
    }
    
    private DialogBubblePreset _lastPreset;
    
    void OnValidate()
    {
        if (_graphic != null)
            _graphic.SetVerticesDirty();
    }
    
    /// <summary>
    /// Set the bubble preset at runtime.
    /// </summary>
    public void SetPreset(DialogBubblePreset preset)
    {
        bubblePreset = preset;
        if (_graphic != null)
            _graphic.SetVerticesDirty();
    }
    
    /// <summary>
    /// Get the current preset.
    /// </summary>
    public DialogBubblePreset GetCurrentPreset()
    {
        return bubblePreset;
    }
}
