using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Dialog box shadow with dynamic arrow positioning.
/// Controls arrow position via shader properties.
/// </summary>
[RequireComponent(typeof(Graphic))]
[ExecuteAlways]
public class DialogShadow : MonoBehaviour, IMeshModifier
{
    [Header("Shadow Settings")]
    public Color shadowColor = new Color(0, 0, 0, 0.5f);
    public float intensity = 15f;
    
    [Header("Arrow Settings")]
    [Range(0f, 1f)]
    [Tooltip("Position on perimeter: 0=center bottom, 0.25=center right, 0.5=center top, 0.75=center left")]
    public float arrowPerimeter = 0f;  // Default: center of bottom edge
    [Tooltip("Arrow size in pixels (scale-independent)")]
    public float arrowSizePixels = 30f;
    [Tooltip("Arrow width in pixels (scale-independent)")]
    public float arrowWidthPixels = 40f;
    
    [Header("Target (Optional)")]
    [Tooltip("If set, arrow will point towards this transform")]
    public Transform arrowTarget;
    
    [Header("Debug")]
    [SerializeField] private Vector2 currentShadowOffset;
    
    private Graphic _graphic;
    private Canvas _canvas;
    private Material _materialInstance;
    private RectTransform _rectTransform;
    private Camera _canvasCamera;
    
    private static readonly int ShadowColorID = Shader.PropertyToID("_ShadowColor");
    private static readonly int ArrowPerimeterID = Shader.PropertyToID("_ArrowPerimeter");
    private static readonly int ArrowSizePixelsID = Shader.PropertyToID("_ArrowSizePixels");
    private static readonly int ArrowWidthPixelsID = Shader.PropertyToID("_ArrowWidthPixels");
    private static readonly int RectSizeID = Shader.PropertyToID("_RectSize");
    private static readonly int RectScreenPosID = Shader.PropertyToID("_RectScreenPos");
    private static readonly int CanvasScaleID = Shader.PropertyToID("_CanvasScale");
    
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
        
        if (_graphic.material != null && _graphic.material.shader.name == "RoyalLeech/UI/DialogShadow")
        {
            _materialInstance = new Material(_graphic.material);
            _graphic.material = _materialInstance;
        }
    }
    
    private float _lastArrowPerimeter;
    private float _lastArrowSize;
    private Vector2 _lastRectSize;
    
    void LateUpdate()
    {
        if (_materialInstance == null) return;
        
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
        
        // Update shadow color
        _materialInstance.SetColor(ShadowColorID, shadowColor);
        
        // Auto-position arrow towards target
        if (arrowTarget != null)
        {
            UpdateArrowTowardsTarget();
        }
        
        // Update arrow properties
        _materialInstance.SetFloat(ArrowPerimeterID, arrowPerimeter);
        _materialInstance.SetFloat(ArrowSizePixelsID, arrowSizePixels);
        _materialInstance.SetFloat(ArrowWidthPixelsID, arrowWidthPixels);
        
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
        
        // Update shadow offset
        Vector2 newOffset = CalculateShadowDirection() * intensity;
        if (Vector2.Distance(newOffset, currentShadowOffset) > 0.1f)
        {
            currentShadowOffset = newOffset;
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
        
        // Determine which edge to use based on target position
        float distToTop = localTarget.y - rect.yMax;
        float distToBottom = rect.yMin - localTarget.y;
        float distToLeft = rect.xMin - localTarget.x;
        float distToRight = localTarget.x - rect.xMax;
        
        float maxDist = Mathf.Max(distToTop, distToBottom, distToLeft, distToRight);
        
        // Calculate perimeter position based on edge and position
        // New scheme: 0=center bottom, 0.25=center right, 0.5=center top, 0.75=center left
        // Each edge spans ±0.125 around its center
        float posOnEdge;
        
        if (maxDist == distToBottom)
        {
            posOnEdge = Mathf.InverseLerp(rect.xMin, rect.xMax, localTarget.x);
            posOnEdge = Mathf.Clamp(posOnEdge, 0.1f, 0.9f);
            // Bottom: -0.125 to 0.125 (wrap around), center at 0
            arrowPerimeter = (posOnEdge - 0.5f) * 0.25f;  // -0.125 to 0.125
            if (arrowPerimeter < 0) arrowPerimeter += 1f;  // Wrap negative values
        }
        else if (maxDist == distToRight)
        {
            posOnEdge = Mathf.InverseLerp(rect.yMin, rect.yMax, localTarget.y);
            posOnEdge = Mathf.Clamp(posOnEdge, 0.1f, 0.9f);
            // Right: 0.125 to 0.375, center at 0.25
            arrowPerimeter = 0.25f + (posOnEdge - 0.5f) * 0.25f;
        }
        else if (maxDist == distToTop)
        {
            posOnEdge = Mathf.InverseLerp(rect.xMin, rect.xMax, localTarget.x);
            posOnEdge = Mathf.Clamp(posOnEdge, 0.1f, 0.9f);
            // Top: 0.375 to 0.625, center at 0.5 (reversed direction)
            arrowPerimeter = 0.5f + (0.5f - posOnEdge) * 0.25f;
        }
        else // distToLeft
        {
            posOnEdge = Mathf.InverseLerp(rect.yMin, rect.yMax, localTarget.y);
            posOnEdge = Mathf.Clamp(posOnEdge, 0.1f, 0.9f);
            // Left: 0.625 to 0.875, center at 0.75 (reversed direction)
            arrowPerimeter = 0.75f + (0.5f - posOnEdge) * 0.25f;
        }
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
        // Shadow vertices first
        Vector3 offset = new Vector3(currentShadowOffset.x, currentShadowOffset.y, 0);
        if (_canvas != null)
        {
            offset /= _canvas.scaleFactor;
        }
        offset = transform.InverseTransformVector(offset);
        
        Color32 color32 = _graphic != null ? _graphic.color : Color.white;
        
        // Shadow quad (4 vertices)
        UIVertex vert = UIVertex.simpleVert;
        vert.color = color32;
        
        // Bottom-left
        vert.position = new Vector3(left, bottom, 0) + offset;
        vert.uv0 = new Vector2(uvLeft, uvBottom);
        vert.uv1 = new Vector2(1, 0); // Shadow flag
        vh.AddVert(vert);
        
        // Top-left
        vert.position = new Vector3(left, top, 0) + offset;
        vert.uv0 = new Vector2(uvLeft, uvTop);
        vert.uv1 = new Vector2(1, 0);
        vh.AddVert(vert);
        
        // Top-right
        vert.position = new Vector3(right, top, 0) + offset;
        vert.uv0 = new Vector2(uvRight, uvTop);
        vert.uv1 = new Vector2(1, 0);
        vh.AddVert(vert);
        
        // Bottom-right
        vert.position = new Vector3(right, bottom, 0) + offset;
        vert.uv0 = new Vector2(uvRight, uvBottom);
        vert.uv1 = new Vector2(1, 0);
        vh.AddVert(vert);
        
        // Shadow triangles
        vh.AddTriangle(0, 1, 2);
        vh.AddTriangle(2, 3, 0);
        
        // Main quad (4 vertices)
        // Bottom-left
        vert.position = new Vector3(left, bottom, 0);
        vert.uv0 = new Vector2(uvLeft, uvBottom);
        vert.uv1 = new Vector2(0, 0); // Main flag
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
        vh.AddTriangle(4, 5, 6);
        vh.AddTriangle(6, 7, 4);
    }
    
    void OnValidate()
    {
        if (_graphic != null)
            _graphic.SetVerticesDirty();
            
        if (_materialInstance != null)
        {
            _materialInstance.SetColor(ShadowColorID, shadowColor);
            _materialInstance.SetFloat(ArrowPerimeterID, arrowPerimeter);
        }
    }
}
