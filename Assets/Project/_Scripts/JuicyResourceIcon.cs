using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controller for JuicyResourceIcon shader effects.
/// Provides C# API for triggering and animating all juicy effects.
/// Supports mesh-based shadow like ShaderShadow for Image.
/// </summary>
[RequireComponent(typeof(Graphic))]
[ExecuteAlways]
public class JuicyResourceIcon : MonoBehaviour, IMeshModifier
{
    [Header("Fill Effect")]
    [Range(0, 1)] public float fillAmount = 1f;
    public Color fillColor = Color.white;
    public Color backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    [Range(0, 1)] public float backgroundAlpha = 0.5f;
    public float fillWaveStrength = 0.02f;
    public float fillWaveSpeed = 3f;
    
    [Header("Glow and Pulse")]
    public Color glowColor = new Color(1f, 0.8f, 0.2f, 1f);
    [Range(0, 2)] public float glowIntensity = 0f;
    [Range(0, 0.1f)] public float glowSize = 0.02f;
    public float pulseSpeed = 2f;
    [Range(0, 1)] public float pulseIntensity = 0f;
    
    [Header("Shake Effect")]
    [Range(0, 20)] public float shakeIntensity = 0f;
    public float shakeSpeed = 30f;
    
    [Header("Highlight Flash")]
    public Color highlightColor = Color.white;
    [Range(0, 1)] public float highlightIntensity = 0f;
    
    [Header("Color Tint")]
    public Color tintOverlay = new Color(1, 1, 1, 0);
    
    [Header("Shadow")]
    public Color shadowColor = new Color(0, 0, 0, 0.5f);
    public float shadowIntensity = 5f;
    
    [Header("Debug")]
    [SerializeField] private Vector2 currentShadowOffset;
    
    private Graphic _graphic;
    private Canvas _canvas;
    private Material _materialInstance;
    private float _lastScale;
    
    // Shader property IDs
    private static readonly int FillAmountID = Shader.PropertyToID("_FillAmount");
    private static readonly int FillColorID = Shader.PropertyToID("_FillColor");
    private static readonly int BackgroundColorID = Shader.PropertyToID("_BackgroundColor");
    private static readonly int BackgroundAlphaID = Shader.PropertyToID("_BackgroundAlpha");
    private static readonly int FillWaveStrengthID = Shader.PropertyToID("_FillWaveStrength");
    private static readonly int FillWaveSpeedID = Shader.PropertyToID("_FillWaveSpeed");
    private static readonly int GlowColorID = Shader.PropertyToID("_GlowColor");
    private static readonly int GlowIntensityID = Shader.PropertyToID("_GlowIntensity");
    private static readonly int GlowSizeID = Shader.PropertyToID("_GlowSize");
    private static readonly int PulseSpeedID = Shader.PropertyToID("_PulseSpeed");
    private static readonly int PulseIntensityID = Shader.PropertyToID("_PulseIntensity");
    private static readonly int ShakeIntensityID = Shader.PropertyToID("_ShakeIntensity");
    private static readonly int ShakeSpeedID = Shader.PropertyToID("_ShakeSpeed");
    private static readonly int HighlightColorID = Shader.PropertyToID("_HighlightColor");
    private static readonly int HighlightIntensityID = Shader.PropertyToID("_HighlightIntensity");
    private static readonly int TintOverlayID = Shader.PropertyToID("_TintOverlay");
    private static readonly int ShadowColorID = Shader.PropertyToID("_ShadowColor");
    
    void Awake()
    {
        _graphic = GetComponent<Graphic>();
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
        
        if (_graphic.material != null && _graphic.material.shader.name == "Custom/JuicyResourceIcon")
        {
            _materialInstance = new Material(_graphic.material);
            _graphic.material = _materialInstance;
        }
    }
    
    void LateUpdate()
    {
        if (_materialInstance == null)
        {
            if (_graphic != null && _graphic.material != null && 
                _graphic.material.shader.name == "Custom/JuicyResourceIcon")
            {
                CreateMaterialInstance();
            }
            else
            {
                return;
            }
        }
        
        // Update all shader properties
        UpdateShaderProperties();
        
        // Calculate shadow offset
        Vector2 newOffset = CalculateShadowDirection() * shadowIntensity;
        if (Vector2.Distance(newOffset, currentShadowOffset) > 0.1f)
        {
            currentShadowOffset = newOffset;
            if (_graphic != null)
                _graphic.SetVerticesDirty();
        }
    }
    
    void UpdateShaderProperties()
    {
        // Fill
        _materialInstance.SetFloat(FillAmountID, fillAmount);
        _materialInstance.SetColor(FillColorID, fillColor);
        _materialInstance.SetColor(BackgroundColorID, backgroundColor);
        _materialInstance.SetFloat(BackgroundAlphaID, backgroundAlpha);
        _materialInstance.SetFloat(FillWaveStrengthID, fillWaveStrength);
        _materialInstance.SetFloat(FillWaveSpeedID, fillWaveSpeed);
        
        // Glow/Pulse
        _materialInstance.SetColor(GlowColorID, glowColor);
        _materialInstance.SetFloat(GlowIntensityID, glowIntensity);
        _materialInstance.SetFloat(GlowSizeID, glowSize);
        _materialInstance.SetFloat(PulseSpeedID, pulseSpeed);
        _materialInstance.SetFloat(PulseIntensityID, pulseIntensity);
        
        // Shake
        _materialInstance.SetFloat(ShakeIntensityID, shakeIntensity);
        _materialInstance.SetFloat(ShakeSpeedID, shakeSpeed);
        
        // Highlight
        _materialInstance.SetColor(HighlightColorID, highlightColor);
        _materialInstance.SetFloat(HighlightIntensityID, highlightIntensity);
        
        // Tint
        _materialInstance.SetColor(TintOverlayID, tintOverlay);
        
        // Shadow
        _materialInstance.SetColor(ShadowColorID, shadowColor);
    }
    
    Vector2 CalculateShadowDirection()
    {
        if (ShadowLightSource.Instance == null)
            return new Vector2(1, -1).normalized;
        
        if (_canvas == null) _canvas = GetComponentInParent<Canvas>();
        
        Camera cam = null;
        if (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            cam = _canvas.worldCamera;
        if (cam == null) cam = Camera.main;
        
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cam, transform.position);
        screenPos -= new Vector2(Screen.width / 2f, Screen.height / 2f);
        
        return ShadowLightSource.Instance.GetShadowDirection(screenPos);
    }
    
    // === IMeshModifier for Shadow ===
    
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
        
        int originalVertCount = vh.currentVertCount;
        if (originalVertCount == 0)
            return;
        
        // Get original vertices
        System.Collections.Generic.List<UIVertex> originalVerts = 
            new System.Collections.Generic.List<UIVertex>();
        for (int i = 0; i < originalVertCount; i++)
        {
            UIVertex v = new UIVertex();
            vh.PopulateUIVertex(ref v, i);
            originalVerts.Add(v);
        }
        
        // Calculate offset
        Vector3 offset = new Vector3(currentShadowOffset.x, currentShadowOffset.y, 0);
        if (_canvas != null)
        {
            offset /= _canvas.scaleFactor;
        }
        offset = transform.InverseTransformVector(offset);
        
        // Build triangles (quads: 4 verts = 6 indices)
        int quadCount = originalVertCount / 4;
        System.Collections.Generic.List<int> triangles = 
            new System.Collections.Generic.List<int>();
        for (int q = 0; q < quadCount; q++)
        {
            int b = q * 4;
            triangles.Add(b); triangles.Add(b + 1); triangles.Add(b + 2);
            triangles.Add(b + 2); triangles.Add(b + 3); triangles.Add(b);
        }
        
        // Clear and rebuild
        vh.Clear();
        
        // Shadow vertices first
        for (int i = 0; i < originalVerts.Count; i++)
        {
            UIVertex v = originalVerts[i];
            v.position += offset;
            v.uv1 = new Vector4(1, 0, 0, 0); // Shadow flag
            vh.AddVert(v);
        }
        
        // Shadow triangles
        for (int i = 0; i < triangles.Count; i += 3)
        {
            vh.AddTriangle(triangles[i], triangles[i + 1], triangles[i + 2]);
        }
        
        // Main vertices
        int mainBase = vh.currentVertCount;
        for (int i = 0; i < originalVerts.Count; i++)
        {
            UIVertex v = originalVerts[i];
            v.uv1 = new Vector4(0, 0, 0, 0); // Main flag
            vh.AddVert(v);
        }
        
        // Main triangles
        for (int i = 0; i < triangles.Count; i += 3)
        {
            vh.AddTriangle(
                mainBase + triangles[i],
                mainBase + triangles[i + 1],
                mainBase + triangles[i + 2]
            );
        }
    }
    
    void OnValidate()
    {
        if (_graphic != null)
            _graphic.SetVerticesDirty();
        
        if (_materialInstance != null)
            UpdateShaderProperties();
    }
    
    // === PUBLIC API FOR ANIMATIONS ===
    
    /// <summary>Set fill with optional animation target for DOTween.</summary>
    public void SetFill(float amount) => fillAmount = amount;
    
    /// <summary>Flash highlight effect.</summary>
    public void TriggerHighlight(float intensity = 1f) => highlightIntensity = intensity;
    
    /// <summary>Start pulse effect.</summary>
    public void StartPulse(float intensity = 0.5f) => pulseIntensity = intensity;
    public void StopPulse() => pulseIntensity = 0f;
    
    /// <summary>Start glow effect.</summary>
    public void SetGlow(float intensity) => glowIntensity = intensity;
    
    /// <summary>Start shake effect.</summary>
    public void StartShake(float intensity = 10f) => shakeIntensity = intensity;
    public void StopShake() => shakeIntensity = 0f;
    
    /// <summary>Set tint overlay (alpha controls blend).</summary>
    public void SetTint(Color color) => tintOverlay = color;
    public void ClearTint() => tintOverlay = new Color(1, 1, 1, 0);
}
