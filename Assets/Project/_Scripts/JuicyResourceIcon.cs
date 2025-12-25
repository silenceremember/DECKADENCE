using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Controller for JuicyResourceIcon shader effects.
/// Provides C# API for triggering and animating all juicy effects.
/// Supports mesh-based shadow like ShaderShadow for Image.
/// 
/// MAXIMUM JUICE: Includes DOTween-based animations for all effects.
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
    
    [Header("Animation Presets")]
    [Tooltip("Duration for fill animations")]
    public float fillDuration = 0.5f;
    public Ease fillEase = Ease.OutBack;
    
    [Tooltip("Duration for highlight/flash")]
    public float flashDuration = 0.15f;
    
    [Tooltip("Duration for shake")]
    public float shakeDuration = 0.3f;
    
    [Tooltip("Scale punch intensity")]
    public float punchScale = 0.2f;
    
    [Header("Debug")]
    [SerializeField] private Vector2 currentShadowOffset;
    
    private Graphic _graphic;
    private Canvas _canvas;
    private Material _materialInstance;
    private RectTransform _rectTransform;
    private Sequence _currentSequence;
    private Tween _fillTween;
    private Tween _shakeTween;
    private Tween _pulseTween;
    private Tween _glowTween;
    private Tween _previewGlowTween;
    private Tween _previewPulseTween;
    private bool _isPreviewActive;
    
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
        KillAllTweens();
        if (_graphic != null)
            _graphic.SetVerticesDirty();
    }
    
    void OnDestroy()
    {
        KillAllTweens();
        
        if (_materialInstance != null)
        {
            if (Application.isPlaying)
                Destroy(_materialInstance);
            else
                DestroyImmediate(_materialInstance);
        }
    }
    
    void KillAllTweens()
    {
        _currentSequence?.Kill();
        _fillTween?.Kill();
        _shakeTween?.Kill();
        _pulseTween?.Kill();
        _glowTween?.Kill();
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
    
    // =====================================================
    // === MAXIMUM JUICE API ===
    // =====================================================
    
    /// <summary>
    /// Animate fill to target value with all the juice!
    /// Includes scale punch and glow.
    /// </summary>
    public Tween AnimateFillTo(float targetFill, float duration = -1)
    {
        if (duration < 0) duration = fillDuration;
        
        _fillTween?.Kill();
        _fillTween = DOTween.To(() => fillAmount, x => fillAmount = x, targetFill, duration)
            .SetEase(fillEase);
        
        return _fillTween;
    }
    
    /// <summary>
    /// JUICY resource gain effect!
    /// Flash + scale punch + glow burst + fill increase.
    /// </summary>
    public Sequence PlayGainEffect(float newFillAmount, Color? flashColor = null)
    {
        _currentSequence?.Kill();
        _currentSequence = DOTween.Sequence();
        
        Color flash = flashColor ?? new Color(0.5f, 1f, 0.5f, 1f); // Green flash
        
        // Scale punch
        _currentSequence.Append(
            _rectTransform.DOPunchScale(Vector3.one * punchScale, flashDuration * 2, 1, 0.5f)
        );
        
        // Flash
        _currentSequence.Join(
            DOTween.To(() => highlightIntensity, x => highlightIntensity = x, 0.8f, flashDuration * 0.5f)
                .SetEase(Ease.OutQuad)
        );
        highlightColor = flash;
        
        // Glow burst
        _currentSequence.Join(
            DOTween.To(() => glowIntensity, x => glowIntensity = x, 1.5f, flashDuration)
                .SetEase(Ease.OutQuad)
        );
        glowColor = flash;
        
        // Fill animation
        _currentSequence.Join(AnimateFillTo(newFillAmount));
        
        // Fade out effects
        _currentSequence.Append(
            DOTween.To(() => highlightIntensity, x => highlightIntensity = x, 0f, flashDuration)
        );
        _currentSequence.Join(
            DOTween.To(() => glowIntensity, x => glowIntensity = x, 0f, flashDuration * 2)
        );
        
        return _currentSequence;
    }
    
    /// <summary>
    /// JUICY resource loss effect!
    /// Red flash + shake + fill decrease.
    /// </summary>
    public Sequence PlayLossEffect(float newFillAmount, Color? flashColor = null)
    {
        _currentSequence?.Kill();
        _currentSequence = DOTween.Sequence();
        
        Color flash = flashColor ?? new Color(1f, 0.3f, 0.3f, 1f); // Red flash
        
        // Shake
        _currentSequence.Append(
            DOTween.To(() => shakeIntensity, x => shakeIntensity = x, 15f, shakeDuration * 0.3f)
                .SetEase(Ease.OutQuad)
        );
        
        // Flash
        _currentSequence.Join(
            DOTween.To(() => highlightIntensity, x => highlightIntensity = x, 0.6f, flashDuration * 0.5f)
                .SetEase(Ease.OutQuad)
        );
        highlightColor = flash;
        
        // Tint overlay
        tintOverlay = new Color(flash.r, flash.g, flash.b, 0.3f);
        
        // Fill animation
        _currentSequence.Join(AnimateFillTo(newFillAmount, fillDuration * 0.7f));
        
        // Fade out effects
        _currentSequence.Append(
            DOTween.To(() => shakeIntensity, x => shakeIntensity = x, 0f, shakeDuration)
        );
        _currentSequence.Join(
            DOTween.To(() => highlightIntensity, x => highlightIntensity = x, 0f, flashDuration)
        );
        _currentSequence.Join(
            DOTween.To(() => tintOverlay, x => tintOverlay = x, new Color(1, 1, 1, 0), flashDuration * 2)
        );
        
        return _currentSequence;
    }
    
    /// <summary>
    /// Critical/low resource warning effect!
    /// Continuous pulse + red glow.
    /// </summary>
    public void StartCriticalPulse(Color? pulseColor = null)
    {
        Color glow = pulseColor ?? new Color(1f, 0.2f, 0.2f, 1f);
        glowColor = glow;
        
        _pulseTween?.Kill();
        pulseIntensity = 0.5f;
        glowIntensity = 0.8f;
    }
    
    public void StopCriticalPulse()
    {
        _pulseTween?.Kill();
        
        DOTween.To(() => pulseIntensity, x => pulseIntensity = x, 0f, 0.3f);
        DOTween.To(() => glowIntensity, x => glowIntensity = x, 0f, 0.3f);
    }
    
    /// <summary>
    /// Quick highlight flash.
    /// </summary>
    public Tween Flash(Color? color = null, float intensity = 0.7f)
    {
        highlightColor = color ?? Color.white;
        highlightIntensity = intensity;
        
        return DOTween.To(() => highlightIntensity, x => highlightIntensity = x, 0f, flashDuration)
            .SetEase(Ease.OutQuad);
    }
    
    /// <summary>
    /// Punch scale effect.
    /// </summary>
    public Tween PunchScale(float scale = -1)
    {
        if (scale < 0) scale = punchScale;
        return _rectTransform.DOPunchScale(Vector3.one * scale, flashDuration * 2, 1, 0.5f);
    }
    
    /// <summary>
    /// Shake effect.
    /// </summary>
    public Tween Shake(float intensity = 10f, float duration = -1)
    {
        if (duration < 0) duration = shakeDuration;
        
        _shakeTween?.Kill();
        shakeIntensity = intensity;
        _shakeTween = DOTween.To(() => shakeIntensity, x => shakeIntensity = x, 0f, duration)
            .SetEase(Ease.OutQuad);
        
        return _shakeTween;
    }
    
    /// <summary>
    /// Glow effect with auto-fade.
    /// </summary>
    public Tween Glow(Color? color = null, float intensity = 1f, float duration = 0.5f)
    {
        glowColor = color ?? glowColor;
        glowIntensity = intensity;
        
        _glowTween?.Kill();
        _glowTween = DOTween.To(() => glowIntensity, x => glowIntensity = x, 0f, duration)
            .SetEase(Ease.OutQuad);
        
        return _glowTween;
    }
    
    // === SIMPLE API (no animation) ===
    
    public void SetFill(float amount) => fillAmount = amount;
    public void SetHighlight(float intensity) => highlightIntensity = intensity;
    public void SetPulse(float intensity) => pulseIntensity = intensity;
    public void SetGlowIntensity(float intensity) => glowIntensity = intensity;
    public void SetShake(float intensity) => shakeIntensity = intensity;
    public void SetTint(Color color) => tintOverlay = color;
    public void ClearTint() => tintOverlay = new Color(1, 1, 1, 0);
    
    // =====================================================
    // === MAGNITUDE-BASED JUICE (no gain/loss reveal) ===
    // =====================================================
    
    /// <summary>
    /// Play magnitude-based effect - intensity scales with |delta|.
    /// Doesn't reveal whether it's gain or loss, just how BIG the change is.
    /// Uses neutral golden color.
    /// </summary>
    /// <param name="newFillAmount">Target fill 0-1</param>
    /// <param name="magnitude">Absolute magnitude 0-100 (or whatever your max delta is)</param>
    /// <param name="maxMagnitude">Max expected magnitude for scaling (default 30)</param>
    public Sequence PlayMagnitudeEffect(float newFillAmount, float magnitude, float maxMagnitude = 30f)
    {
        _currentSequence?.Kill();
        _currentSequence = DOTween.Sequence();
        
        // Normalize magnitude to 0-1 scale
        float t = Mathf.Clamp01(magnitude / maxMagnitude);
        
        // Neutral golden color - doesn't reveal gain/loss
        Color effectColor = new Color(1f, 0.85f, 0.4f, 1f);
        
        // === SCALE PUNCH (stronger with magnitude) ===
        float punchAmount = Mathf.Lerp(0.05f, punchScale * 1.5f, t);
        _currentSequence.Append(
            _rectTransform.DOPunchScale(Vector3.one * punchAmount, flashDuration * 2f, 1, 0.5f)
        );
        
        // === SHAKE (only for significant changes) ===
        if (t > 0.2f)
        {
            float shakeAmount = Mathf.Lerp(0.5f, 3f, t);
            _currentSequence.Join(
                DOTween.To(() => shakeIntensity, x => shakeIntensity = x, shakeAmount, shakeDuration * 0.3f)
                    .SetEase(Ease.OutQuad)
            );
        }
        
        // === GLOW (scales with magnitude) ===
        float glowAmount = Mathf.Lerp(0.3f, 1.5f, t);
        glowColor = effectColor;
        _currentSequence.Join(
            DOTween.To(() => glowIntensity, x => glowIntensity = x, glowAmount, flashDuration)
                .SetEase(Ease.OutQuad)
        );
        
        // === HIGHLIGHT FLASH (subtle for small, strong for big) ===
        float flashAmount = Mathf.Lerp(0.2f, 0.7f, t);
        highlightColor = effectColor;
        _currentSequence.Join(
            DOTween.To(() => highlightIntensity, x => highlightIntensity = x, flashAmount, flashDuration * 0.5f)
                .SetEase(Ease.OutQuad)
        );
        
        // === FILL ANIMATION ===
        // Duration also scales - big changes animate slightly faster (more dramatic)
        float fillDur = Mathf.Lerp(fillDuration, fillDuration * 0.7f, t);
        _currentSequence.Join(AnimateFillTo(newFillAmount, fillDur));
        
        // === FADE OUT ALL EFFECTS ===
        float fadeTime = Mathf.Lerp(0.2f, 0.4f, t);
        
        _currentSequence.Append(
            DOTween.To(() => highlightIntensity, x => highlightIntensity = x, 0f, fadeTime)
        );
        _currentSequence.Join(
            DOTween.To(() => glowIntensity, x => glowIntensity = x, 0f, fadeTime * 1.5f)
        );
        _currentSequence.Join(
            DOTween.To(() => shakeIntensity, x => shakeIntensity = x, 0f, fadeTime * 2f)
        );
        
        return _currentSequence;
    }
    
    /// <summary>
    /// Preview/highlight effect for when player is hovering/dragging.
    /// Shows that this resource WILL change, intensity based on magnitude.
    /// Includes SHAKE for juicy feedback!
    /// </summary>
    public void PlayHighlightPreview(float magnitude, float maxMagnitude = 30f)
    {
        // Kill any fade-out tweens
        _previewGlowTween?.Kill();
        _previewPulseTween?.Kill();
        
        _isPreviewActive = true;
        
        float t = Mathf.Clamp01(magnitude / maxMagnitude);
        
        // Subtle neutral glow
        glowColor = new Color(1f, 0.9f, 0.6f, 1f);
        glowIntensity = Mathf.Lerp(0.3f, 0.8f, t);
        
        // Pulse for larger changes
        pulseIntensity = Mathf.Lerp(0f, 0.4f, t);
        
        // SHAKE - scales with magnitude (the juice!)
        // Values are in local space units (UI pixels)
        shakeIntensity = Mathf.Lerp(0.5f, 3f, t);
    }
    
    /// <summary>
    /// Stop preview highlight.
    /// </summary>
    public void StopHighlightPreview()
    {
        if (!_isPreviewActive) return; // Not showing preview
        _isPreviewActive = false;
        
        // Kill existing tweens before creating new ones
        _previewGlowTween?.Kill();
        _previewPulseTween?.Kill();
        
        _previewGlowTween = DOTween.To(() => glowIntensity, x => glowIntensity = x, 0f, 0.2f);
        _previewPulseTween = DOTween.To(() => pulseIntensity, x => pulseIntensity = x, 0f, 0.2f);
        
        // Fade out shake
        DOTween.To(() => shakeIntensity, x => shakeIntensity = x, 0f, 0.15f);
    }
    
    public void ClearAllEffects()
    {
        highlightIntensity = 0;
        pulseIntensity = 0;
        glowIntensity = 0;
        shakeIntensity = 0;
        tintOverlay = new Color(1, 1, 1, 0);
    }
}
