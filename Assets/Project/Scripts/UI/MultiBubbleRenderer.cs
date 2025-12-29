using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Multi-layer bubble renderer. Renders multiple bubble layers with independent effects.
/// Each layer can have its own fill, border, shadows, tears, and arrow.
/// </summary>
[RequireComponent(typeof(Graphic))]
[ExecuteAlways]
public class MultiBubbleRenderer : MonoBehaviour, IMeshModifier
{
    [Header("Preset")]
    [Tooltip("Multi-layer bubble preset")]
    public MultiBubblePreset preset;
    
    [Header("Arrow Settings")]
    [Range(0f, 1f)]
    [Tooltip("Position on perimeter: 0=center bottom, 0.25=center right, 0.5=center top, 0.75=center left")]
    public float arrowPerimeter = 0f;
    
    [Tooltip("If set, arrow will point towards this transform")]
    public Transform arrowTarget;
    
    [Header("Runtime")]
    [Tooltip("Raised = emboss effect, Recessed = deboss effect")]
    public bool borderShadowRaised = true;
    
    private Graphic _graphic;
    private Canvas _canvas;
    private Material _materialInstance;
    private RectTransform _rectTransform;
    private Camera _canvasCamera;
    
    // Cached shadow offsets per layer
    private List<Vector2> _primaryShadowOffsets = new List<Vector2>();
    private List<Vector2> _secondaryShadowOffsets = new List<Vector2>();
    
    // Shader property IDs - base
    private static readonly int RectSizeID = Shader.PropertyToID("_RectSize");
    private static readonly int CanvasScaleID = Shader.PropertyToID("_CanvasScale");
    private static readonly int CornerCutMinID = Shader.PropertyToID("_CornerCutMinPixels");
    private static readonly int CornerCutMaxID = Shader.PropertyToID("_CornerCutMaxPixels");
    private static readonly int TearDepthMinID = Shader.PropertyToID("_TearDepthMinPixels");
    private static readonly int TearDepthMaxID = Shader.PropertyToID("_TearDepthMaxPixels");
    private static readonly int TearWidthMinID = Shader.PropertyToID("_TearWidthMinPixels");
    private static readonly int TearWidthMaxID = Shader.PropertyToID("_TearWidthMaxPixels");
    private static readonly int TearSpacingMinID = Shader.PropertyToID("_TearSpacingMinPixels");
    private static readonly int TearSpacingMaxID = Shader.PropertyToID("_TearSpacingMaxPixels");
    private static readonly int TearSeedID = Shader.PropertyToID("_TearSeed");
    private static readonly int AnimSpeedID = Shader.PropertyToID("_AnimSpeed");
    private static readonly int ArrowPerimeterID = Shader.PropertyToID("_ArrowPerimeter");
    private static readonly int LightDirectionID = Shader.PropertyToID("_LightDirection");
    private static readonly int LayerCountID = Shader.PropertyToID("_LayerCount");
    
    // Layer property ID arrays (generated once)
    private static readonly int[][] LayerPropertyIDs = GenerateLayerPropertyIDs();
    
    // Property indices
    private const int PROP_ENABLED = 0;
    private const int PROP_OFFSET = 1;
    private const int PROP_CUTOUT = 2;
    private const int PROP_CUTOUT_PADDING = 3;
    private const int PROP_FILL_COLOR = 4;
    private const int PROP_SHOW_ARROW = 5;
    private const int PROP_ARROW_SIZE = 6;
    private const int PROP_ARROW_WIDTH = 7;
    private const int PROP_SHOW_BORDER = 8;
    private const int PROP_BORDER_COLOR = 9;
    private const int PROP_BORDER_THICKNESS = 10;
    private const int PROP_BORDER_OFFSET = 11;
    private const int PROP_BORDER_STYLE = 12;
    private const int PROP_DASH_LENGTH = 13;
    private const int PROP_DASH_GAP = 14;
    private const int PROP_SHADOW_COLOR = 15;
    private const int PROP_SHADOW_INTENSITY = 16;
    private const int PROP_SHOW_SECOND_SHADOW = 17;
    private const int PROP_SECOND_SHADOW_COLOR = 18;
    private const int PROP_SECOND_SHADOW_INTENSITY = 19;
    private const int PROP_SHOW_INNER_SHADOW = 20;
    private const int PROP_INNER_SHADOW_INTENSITY = 21;
    private const int PROP_INNER_SHADOW_COLOR = 22;
    private const int PROP_TEARS = 23;
    
    private static int[][] GenerateLayerPropertyIDs()
    {
        int[][] ids = new int[4][];
        string[] prefixes = { "_L0_", "_L1_", "_L2_", "_L3_" };
        
        for (int i = 0; i < 4; i++)
        {
            ids[i] = new int[24];
            string p = prefixes[i];
            ids[i][PROP_ENABLED] = Shader.PropertyToID(p + "Enabled");
            ids[i][PROP_OFFSET] = Shader.PropertyToID(p + "Offset");
            ids[i][PROP_CUTOUT] = Shader.PropertyToID(p + "Cutout");
            ids[i][PROP_CUTOUT_PADDING] = Shader.PropertyToID(p + "CutoutPadding");
            ids[i][PROP_FILL_COLOR] = Shader.PropertyToID(p + "FillColor");
            ids[i][PROP_SHOW_ARROW] = Shader.PropertyToID(p + "ShowArrow");
            ids[i][PROP_ARROW_SIZE] = Shader.PropertyToID(p + "ArrowSize");
            ids[i][PROP_ARROW_WIDTH] = Shader.PropertyToID(p + "ArrowWidth");
            ids[i][PROP_SHOW_BORDER] = Shader.PropertyToID(p + "ShowBorder");
            ids[i][PROP_BORDER_COLOR] = Shader.PropertyToID(p + "BorderColor");
            ids[i][PROP_BORDER_THICKNESS] = Shader.PropertyToID(p + "BorderThickness");
            ids[i][PROP_BORDER_OFFSET] = Shader.PropertyToID(p + "BorderOffset");
            ids[i][PROP_BORDER_STYLE] = Shader.PropertyToID(p + "BorderStyle");
            ids[i][PROP_DASH_LENGTH] = Shader.PropertyToID(p + "DashLength");
            ids[i][PROP_DASH_GAP] = Shader.PropertyToID(p + "DashGap");
            ids[i][PROP_SHADOW_COLOR] = Shader.PropertyToID(p + "ShadowColor");
            ids[i][PROP_SHADOW_INTENSITY] = Shader.PropertyToID(p + "ShadowIntensity");
            ids[i][PROP_SHOW_SECOND_SHADOW] = Shader.PropertyToID(p + "ShowSecondShadow");
            ids[i][PROP_SECOND_SHADOW_COLOR] = Shader.PropertyToID(p + "SecondShadowColor");
            ids[i][PROP_SECOND_SHADOW_INTENSITY] = Shader.PropertyToID(p + "SecondShadowIntensity");
            ids[i][PROP_SHOW_INNER_SHADOW] = Shader.PropertyToID(p + "ShowInnerShadow");
            ids[i][PROP_INNER_SHADOW_INTENSITY] = Shader.PropertyToID(p + "InnerShadowIntensity");
            ids[i][PROP_INNER_SHADOW_COLOR] = Shader.PropertyToID(p + "InnerShadowColor");
            ids[i][PROP_TEARS] = Shader.PropertyToID(p + "Tears");
        }
        
        return ids;
    }
    
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
        
        if (_graphic.material != null && _graphic.material.shader.name == "DECKADENCE/UI/MultiBubble")
        {
            _materialInstance = new Material(_graphic.material);
            _graphic.material = _materialInstance;
        }
    }
    
    void LateUpdate()
    {
        if (_materialInstance == null || preset == null) return;
        
        if (_rectTransform == null)
            _rectTransform = GetComponent<RectTransform>();
        
        if (_canvas == null)
            _canvas = GetComponentInParent<Canvas>();
        
        float scaleFactor = _canvas != null ? _canvas.scaleFactor : 1f;
        Rect rect = _rectTransform.rect;
        
        // Shared properties
        _materialInstance.SetVector(RectSizeID, new Vector4(rect.width, rect.height, 0, 0));
        _materialInstance.SetFloat(CanvasScaleID, scaleFactor);
        _materialInstance.SetFloat(CornerCutMinID, preset.cornerCutMin);
        _materialInstance.SetFloat(CornerCutMaxID, preset.cornerCutMax);
        _materialInstance.SetFloat(TearDepthMinID, preset.tearDepthMin);
        _materialInstance.SetFloat(TearDepthMaxID, preset.tearDepthMax);
        _materialInstance.SetFloat(TearWidthMinID, preset.tearWidthMin);
        _materialInstance.SetFloat(TearWidthMaxID, preset.tearWidthMax);
        _materialInstance.SetFloat(TearSpacingMinID, preset.tearSpacingMin);
        _materialInstance.SetFloat(TearSpacingMaxID, preset.tearSpacingMax);
        _materialInstance.SetFloat(TearSeedID, preset.tearSeed);
        _materialInstance.SetFloat(AnimSpeedID, preset.animationFPS);
        
        // Arrow target
        if (arrowTarget != null)
            UpdateArrowTowardsTarget();
        
        _materialInstance.SetFloat(ArrowPerimeterID, arrowPerimeter);
        
        // Light direction
        Vector2 shadowDir = CalculateShadowDirection();
        float invert = borderShadowRaised ? -1f : 1f;
        _materialInstance.SetVector(LightDirectionID, new Vector4(shadowDir.x * invert, shadowDir.y * invert, 0, 0));
        
        // Layer count
        int layerCount = Mathf.Min(preset.layers.Count, 4);
        _materialInstance.SetFloat(LayerCountID, layerCount);
        
        // Update shadow offsets
        UpdateShadowOffsets(shadowDir);
        
        // Per-layer properties
        for (int i = 0; i < 4; i++)
        {
            if (i < layerCount && preset.layers[i] != null)
            {
                SetLayerProperties(i, preset.layers[i]);
            }
            else
            {
                // Disable unused layers
                _materialInstance.SetFloat(LayerPropertyIDs[i][PROP_ENABLED], 0f);
            }
        }
    }
    
    void SetLayerProperties(int index, BubbleLayerConfig layer)
    {
        int[] ids = LayerPropertyIDs[index];
        
        _materialInstance.SetFloat(ids[PROP_ENABLED], layer.enabled ? 1f : 0f);
        _materialInstance.SetFloat(ids[PROP_OFFSET], layer.offset);
        _materialInstance.SetFloat(ids[PROP_CUTOUT], layer.cutoutNextLayer ? 1f : 0f);
        _materialInstance.SetFloat(ids[PROP_CUTOUT_PADDING], layer.cutoutPadding);
        _materialInstance.SetColor(ids[PROP_FILL_COLOR], layer.fillColor);
        _materialInstance.SetFloat(ids[PROP_SHOW_ARROW], layer.showArrow ? 1f : 0f);
        _materialInstance.SetFloat(ids[PROP_ARROW_SIZE], layer.arrowSize);
        _materialInstance.SetFloat(ids[PROP_ARROW_WIDTH], layer.arrowWidth);
        _materialInstance.SetFloat(ids[PROP_SHOW_BORDER], layer.showBorder ? 1f : 0f);
        _materialInstance.SetColor(ids[PROP_BORDER_COLOR], layer.borderColor);
        _materialInstance.SetFloat(ids[PROP_BORDER_THICKNESS], layer.borderThickness);
        _materialInstance.SetFloat(ids[PROP_BORDER_OFFSET], layer.borderOffset);
        _materialInstance.SetFloat(ids[PROP_BORDER_STYLE], layer.borderStyle);
        _materialInstance.SetFloat(ids[PROP_DASH_LENGTH], layer.dashLength);
        _materialInstance.SetFloat(ids[PROP_DASH_GAP], layer.dashGap);
        _materialInstance.SetColor(ids[PROP_SHADOW_COLOR], layer.shadowColor);
        _materialInstance.SetFloat(ids[PROP_SHADOW_INTENSITY], layer.shadowIntensity);
        _materialInstance.SetFloat(ids[PROP_SHOW_SECOND_SHADOW], layer.showSecondShadow ? 1f : 0f);
        _materialInstance.SetColor(ids[PROP_SECOND_SHADOW_COLOR], layer.secondShadowColor);
        _materialInstance.SetFloat(ids[PROP_SECOND_SHADOW_INTENSITY], layer.secondShadowIntensity);
        _materialInstance.SetFloat(ids[PROP_SHOW_INNER_SHADOW], layer.showInnerShadow ? 1f : 0f);
        _materialInstance.SetFloat(ids[PROP_INNER_SHADOW_INTENSITY], layer.innerShadowIntensity);
        _materialInstance.SetColor(ids[PROP_INNER_SHADOW_COLOR], layer.innerShadowColor);
        _materialInstance.SetVector(ids[PROP_TEARS], new Vector4(layer.topTear, layer.bottomTear, layer.leftTear, layer.rightTear));
    }
    
    void UpdateShadowOffsets(Vector2 shadowDir)
    {
        if (preset == null || preset.layers == null) return;
        
        int count = preset.layers.Count;
        
        while (_primaryShadowOffsets.Count < count)
        {
            _primaryShadowOffsets.Add(Vector2.zero);
            _secondaryShadowOffsets.Add(Vector2.zero);
        }
        
        bool needsRebuild = false;
        
        for (int i = 0; i < count; i++)
        {
            var layer = preset.layers[i];
            Vector2 newPrimary = shadowDir * layer.shadowIntensity;
            Vector2 newSecondary = shadowDir * layer.secondShadowIntensity;
            
            if (Vector2.Distance(newPrimary, _primaryShadowOffsets[i]) > 0.1f ||
                Vector2.Distance(newSecondary, _secondaryShadowOffsets[i]) > 0.1f)
            {
                _primaryShadowOffsets[i] = newPrimary;
                _secondaryShadowOffsets[i] = newSecondary;
                needsRebuild = true;
            }
        }
        
        if (needsRebuild && _graphic != null)
            _graphic.SetVerticesDirty();
    }
    
    void UpdateArrowTowardsTarget()
    {
        if (_rectTransform == null || arrowTarget == null) return;
        
        Vector3 targetWorld = arrowTarget.position;
        Vector3 localTarget = _rectTransform.InverseTransformPoint(targetWorld);
        
        Rect rect = _rectTransform.rect;
        Vector2 center = rect.center;
        Vector2 toTarget = new Vector2(localTarget.x - center.x, localTarget.y - center.y);
        
        float angle = Mathf.Atan2(toTarget.y, toTarget.x);
        float shiftedAngle = angle + Mathf.PI * 0.5f;
        if (shiftedAngle < 0) shiftedAngle += Mathf.PI * 2f;
        
        arrowPerimeter = shiftedAngle / (Mathf.PI * 2f);
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
        if (!enabled || !gameObject.activeInHierarchy || preset == null)
            return;
        
        if (_rectTransform == null)
            _rectTransform = GetComponent<RectTransform>();
        
        Rect rect = _rectTransform.rect;
        
        // Calculate max expansion needed
        float maxOffset = 0f;
        float maxShadow = 0f;
        float maxArrow = 0f;
        
        foreach (var layer in preset.layers)
        {
            if (!layer.enabled) continue;
            maxOffset = Mathf.Max(maxOffset, layer.offset);
            maxShadow = Mathf.Max(maxShadow, layer.shadowIntensity);
            if (layer.showSecondShadow)
                maxShadow = Mathf.Max(maxShadow, layer.secondShadowIntensity);
            if (layer.showArrow)
                maxArrow = Mathf.Max(maxArrow, layer.arrowSize);
        }
        
        float totalExpand = maxOffset + maxShadow + maxArrow;
        
        float left = rect.xMin - totalExpand;
        float right = rect.xMax + totalExpand;
        float bottom = rect.yMin - totalExpand;
        float top = rect.yMax + totalExpand;
        
        float uvLeft = -totalExpand / rect.width;
        float uvRight = 1f + totalExpand / rect.width;
        float uvBottom = -totalExpand / rect.height;
        float uvTop = 1f + totalExpand / rect.height;
        
        vh.Clear();
        
        Color32 color32 = _graphic != null ? _graphic.color : Color.white;
        UIVertex vert = UIVertex.simpleVert;
        vert.color = color32;
        
        int vertexIndex = 0;
        int layerCount = Mathf.Min(preset.layers.Count, 4);
        
        // Calculate offsets in local space
        float scaleFactor = _canvas != null ? _canvas.scaleFactor : 1f;
        
        // Rendering order:
        // 1. All shadows (back to front layers, secondary then primary per layer)
        // 2. All fills (back to front layers)
        
        // === PHASE 1: SHADOWS ===
        for (int i = 0; i < layerCount; i++)
        {
            var layer = preset.layers[i];
            if (!layer.enabled) continue;
            
            Vector3 primaryOffset = Vector3.zero;
            Vector3 secondaryOffset = Vector3.zero;
            
            if (i < _primaryShadowOffsets.Count)
            {
                primaryOffset = new Vector3(_primaryShadowOffsets[i].x, _primaryShadowOffsets[i].y, 0) / scaleFactor;
                primaryOffset = transform.InverseTransformVector(primaryOffset);
            }
            if (i < _secondaryShadowOffsets.Count)
            {
                secondaryOffset = new Vector3(_secondaryShadowOffsets[i].x, _secondaryShadowOffsets[i].y, 0) / scaleFactor;
                secondaryOffset = transform.InverseTransformVector(secondaryOffset);
            }
            
            // Secondary shadow (furthest back)
            if (layer.showSecondShadow)
            {
                AddQuad(vh, ref vert, left, right, bottom, top,
                        uvLeft, uvRight, uvBottom, uvTop,
                        secondaryOffset, i, 2); // quadType 2 = secondary shadow
                AddQuadTriangles(vh, vertexIndex);
                vertexIndex += 4;
            }
            
            // Primary shadow
            AddQuad(vh, ref vert, left, right, bottom, top,
                    uvLeft, uvRight, uvBottom, uvTop,
                    primaryOffset, i, 1); // quadType 1 = primary shadow
            AddQuadTriangles(vh, vertexIndex);
            vertexIndex += 4;
        }
        
        // === PHASE 2: FILLS ===
        for (int i = 0; i < layerCount; i++)
        {
            var layer = preset.layers[i];
            if (!layer.enabled) continue;
            
            AddQuad(vh, ref vert, left, right, bottom, top,
                    uvLeft, uvRight, uvBottom, uvTop,
                    Vector3.zero, i, 0); // quadType 0 = fill
            AddQuadTriangles(vh, vertexIndex);
            vertexIndex += 4;
        }
    }
    
    void AddQuad(VertexHelper vh, ref UIVertex vert,
                 float left, float right, float bottom, float top,
                 float uvLeft, float uvRight, float uvBottom, float uvTop,
                 Vector3 offset, int layerIndex, int quadType)
    {
        // uv1.x = layerIndex, uv1.y = quadType
        Vector2 uv1 = new Vector2(layerIndex, quadType);
        
        vert.position = new Vector3(left, bottom, 0) + offset;
        vert.uv0 = new Vector2(uvLeft, uvBottom);
        vert.uv1 = uv1;
        vh.AddVert(vert);
        
        vert.position = new Vector3(left, top, 0) + offset;
        vert.uv0 = new Vector2(uvLeft, uvTop);
        vert.uv1 = uv1;
        vh.AddVert(vert);
        
        vert.position = new Vector3(right, top, 0) + offset;
        vert.uv0 = new Vector2(uvRight, uvTop);
        vert.uv1 = uv1;
        vh.AddVert(vert);
        
        vert.position = new Vector3(right, bottom, 0) + offset;
        vert.uv0 = new Vector2(uvRight, uvBottom);
        vert.uv1 = uv1;
        vh.AddVert(vert);
    }
    
    void AddQuadTriangles(VertexHelper vh, int startIndex)
    {
        vh.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
        vh.AddTriangle(startIndex + 2, startIndex + 3, startIndex);
    }
    
    void OnValidate()
    {
        if (_graphic != null)
            _graphic.SetVerticesDirty();
    }
    
    public void SetPreset(MultiBubblePreset newPreset)
    {
        preset = newPreset;
        if (_graphic != null)
            _graphic.SetVerticesDirty();
    }
}
