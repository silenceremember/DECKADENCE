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
    public enum ArrowEdge
    {
        Bottom = 0,
        Top = 1,
        Left = 2,
        Right = 3
    }
    
    [Header("Shadow Settings")]
    public Color shadowColor = new Color(0, 0, 0, 0.5f);
    public float intensity = 15f;
    
    [Header("Arrow Settings")]
    public ArrowEdge arrowEdge = ArrowEdge.Bottom;
    [Range(0f, 1f)]
    public float arrowPosition = 0.5f;
    [Range(0f, 0.3f)]
    public float arrowSize = 0.1f;
    [Range(0.02f, 0.3f)]
    public float arrowWidth = 0.1f;
    
    [Header("Target (Optional)")]
    [Tooltip("If set, arrow will point towards this transform")]
    public Transform arrowTarget;
    
    [Header("Debug")]
    [SerializeField] private Vector2 currentShadowOffset;
    
    private Graphic _graphic;
    private Canvas _canvas;
    private Material _materialInstance;
    private RectTransform _rectTransform;
    
    private static readonly int ShadowColorID = Shader.PropertyToID("_ShadowColor");
    private static readonly int ArrowEdgeID = Shader.PropertyToID("_ArrowEdge");
    private static readonly int ArrowPositionID = Shader.PropertyToID("_ArrowPosition");
    private static readonly int ArrowSizeID = Shader.PropertyToID("_ArrowSize");
    private static readonly int ArrowWidthID = Shader.PropertyToID("_ArrowWidth");
    
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
    
    private ArrowEdge _lastArrowEdge;
    private float _lastArrowPosition;
    private float _lastArrowSize;
    
    void LateUpdate()
    {
        if (_materialInstance == null) return;
        
        // Update shadow color
        _materialInstance.SetColor(ShadowColorID, shadowColor);
        
        // Auto-position arrow towards target
        if (arrowTarget != null)
        {
            UpdateArrowTowardsTarget();
        }
        
        // Update arrow properties
        _materialInstance.SetFloat(ArrowEdgeID, (float)arrowEdge);
        _materialInstance.SetFloat(ArrowPositionID, arrowPosition);
        _materialInstance.SetFloat(ArrowSizeID, arrowSize);
        _materialInstance.SetFloat(ArrowWidthID, arrowWidth);
        
        // Check if arrow changed (need to rebuild mesh)
        bool arrowChanged = arrowEdge != _lastArrowEdge || 
                           Mathf.Abs(arrowPosition - _lastArrowPosition) > 0.01f ||
                           Mathf.Abs(arrowSize - _lastArrowSize) > 0.001f;
        
        if (arrowChanged)
        {
            _lastArrowEdge = arrowEdge;
            _lastArrowPosition = arrowPosition;
            _lastArrowSize = arrowSize;
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
        
        if (maxDist == distToBottom)
        {
            arrowEdge = ArrowEdge.Bottom;
            arrowPosition = Mathf.InverseLerp(rect.xMin, rect.xMax, localTarget.x);
        }
        else if (maxDist == distToTop)
        {
            arrowEdge = ArrowEdge.Top;
            arrowPosition = Mathf.InverseLerp(rect.xMin, rect.xMax, localTarget.x);
        }
        else if (maxDist == distToLeft)
        {
            arrowEdge = ArrowEdge.Left;
            arrowPosition = Mathf.InverseLerp(rect.yMin, rect.yMax, localTarget.y);
        }
        else
        {
            arrowEdge = ArrowEdge.Right;
            arrowPosition = Mathf.InverseLerp(rect.yMin, rect.yMax, localTarget.y);
        }
        
        arrowPosition = Mathf.Clamp(arrowPosition, 0.1f, 0.9f);
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
        
        // Calculate arrow extension in local units
        float arrowExtension = arrowSize * Mathf.Max(rect.width, rect.height);
        
        // Calculate expanded bounds based on arrow edge
        float expandTop = (arrowEdge == ArrowEdge.Top) ? arrowExtension : 0;
        float expandBottom = (arrowEdge == ArrowEdge.Bottom) ? arrowExtension : 0;
        float expandLeft = (arrowEdge == ArrowEdge.Left) ? arrowExtension : 0;
        float expandRight = (arrowEdge == ArrowEdge.Right) ? arrowExtension : 0;
        
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
            _materialInstance.SetFloat(ArrowEdgeID, (float)arrowEdge);
            _materialInstance.SetFloat(ArrowPositionID, arrowPosition);
            _materialInstance.SetFloat(ArrowSizeID, arrowSize);
            _materialInstance.SetFloat(ArrowWidthID, arrowWidth);
        }
    }
}
