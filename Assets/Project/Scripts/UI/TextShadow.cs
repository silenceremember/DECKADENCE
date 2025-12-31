using UnityEngine;
using TMPro;

/// <summary>
/// Shader-based shadow for TMP text using mesh modification.
/// Duplicates text geometry for shadow - same approach as ShaderShadow for Image.
/// Shadow vertices are marked with UV1.x=1 and rendered behind main text.
/// Backdrop vertices are marked with UV1.x=2 for per-letter random quads.
/// 
/// Uses Canvas.willRenderCanvases to apply shadow at the very last moment.
/// </summary>
[RequireComponent(typeof(TMP_Text))]
[ExecuteAlways]
public class TextShadow : MonoBehaviour
{
    [Header("Shadow Settings")]
    public Color shadowColor = new Color(0, 0, 0, 0.5f);
    
    [Tooltip("Shadow offset distance in world units")]
    public float intensity = 5f;
    
    [Header("Scale Influence")]
    [Tooltip("Enable dynamic shadow distance based on scale")]
    public bool useScaleInfluence = false;
    
    [Tooltip("How much scale affects shadow distance")]
    public float scaleInfluence = 1f;
    
    [Tooltip("Transform to track for scale (optional)")]
    public Transform scaleReference;
    
    [Header("Backdrop Settings")]
    [Tooltip("Enable per-letter backdrop quads")]
    public bool enableBackdrop = false;
    
    [Tooltip("Shadow from backdrop quads instead of letters")]
    public bool shadowFromBackdrop = true;
    
    [Tooltip("Base backdrop color")]
    public Color backdropColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    
    [Tooltip("Random hue variation per letter")]
    [Range(0f, 1f)]
    public float backdropHueVariation = 0.1f;
    
    [Tooltip("Random saturation variation")]
    [Range(0f, 1f)]
    public float backdropSatVariation = 0.2f;
    
    [Tooltip("Random brightness variation")]
    [Range(0f, 1f)]
    public float backdropBrightVariation = 0.2f;
    
    [Tooltip("Extra padding around each letter for backdrop")]
    public float backdropPadding = 5f;
    
    [Tooltip("How much each corner is randomly offset (Persona style)")]
    public float backdropRandomness = 8f;
    
    [Header("Debug")]
    [SerializeField] private Vector2 currentShadowOffset;
    [SerializeField] private float currentEffectiveIntensity;
    
    private TMP_Text _tmpText;
    private Canvas _canvas;
    private Material _materialInstance;
    private float _lastScale;
    
    // Our own mesh instance (separate from TMP's internal mesh)
    private Mesh _shadowMesh;
    private bool _isSubscribed;
    
    private static readonly int ShadowColorID = Shader.PropertyToID("_ShadowColor");
    
    void Awake()
    {
        _tmpText = GetComponent<TMP_Text>();
        _shadowMesh = new Mesh();
        _shadowMesh.name = "TextShadowMesh";
    }
    
    void Start()
    {
        CreateMaterialInstance();
        _canvas = GetComponentInParent<Canvas>();
        _lastScale = GetCurrentScale();
    }
    
    void OnEnable()
    {
        // Subscribe to canvas render event - this fires AFTER all LateUpdates
        Canvas.willRenderCanvases += OnWillRenderCanvases;
        _isSubscribed = true;
    }
    
    void OnDisable()
    {
        if (_isSubscribed)
        {
            Canvas.willRenderCanvases -= OnWillRenderCanvases;
            _isSubscribed = false;
        }
    }
    
    void OnDestroy()
    {
        if (_isSubscribed)
        {
            Canvas.willRenderCanvases -= OnWillRenderCanvases;
            _isSubscribed = false;
        }
        
        if (_shadowMesh != null)
        {
            if (Application.isPlaying)
                Destroy(_shadowMesh);
            else
                DestroyImmediate(_shadowMesh);
        }
        
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
        if (_tmpText == null) return;
        
        if (_tmpText.fontMaterial != null && _tmpText.fontMaterial.shader.name == "DECKADENCE/UI/BoilingText")
        {
            _materialInstance = new Material(_tmpText.fontMaterial);
            _tmpText.fontMaterial = _materialInstance;
        }
    }
    
    private static readonly int BackdropColorID = Shader.PropertyToID("_BackdropColor");
    
    void LateUpdate()
    {
        if (_tmpText == null) return;
        
        // Create material if needed
        if (_materialInstance == null)
        {
            if (_tmpText.fontMaterial != null && _tmpText.fontMaterial.shader.name == "DECKADENCE/UI/BoilingText")
            {
                CreateMaterialInstance();
            }
        }
        
        // Update shader colors
        if (_materialInstance != null)
        {
            _materialInstance.SetColor(ShadowColorID, shadowColor);
            _materialInstance.SetColor(BackdropColorID, backdropColor);
        }
        
        // Calculate effective intensity
        currentEffectiveIntensity = intensity;
        if (useScaleInfluence)
        {
            float currentScale = GetCurrentScale();
            float scaleDelta = currentScale - 1f;
            currentEffectiveIntensity = intensity * (1f + scaleDelta * scaleInfluence);
            _lastScale = currentScale;
        }
        
        // Calculate shadow direction
        currentShadowOffset = CalculateShadowDirection() * currentEffectiveIntensity;
    }
    
    // This fires AFTER all LateUpdate calls, right before Canvas renders
    void OnWillRenderCanvases()
    {
        if (_tmpText == null || !enabled || !gameObject.activeInHierarchy) return;
        
        ApplyShadowMesh();
    }
    
    float GetCurrentScale()
    {
        Transform refTransform = scaleReference != null ? scaleReference : transform;
        return (refTransform.localScale.x + refTransform.localScale.y) * 0.5f;
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
    
    // Seeded random for consistent per-letter offsets
    float SeededRandom(int seed, int component)
    {
        int hash = seed * 1103515245 + 12345 + component * 37;
        return (float)(hash & 0x7FFFFFFF) / (float)0x7FFFFFFF;
    }
    
    Vector2 GetRandomCornerOffset(int charIndex, int cornerIndex)
    {
        float x = (SeededRandom(charIndex, cornerIndex * 2) - 0.5f) * 2f * backdropRandomness;
        float y = (SeededRandom(charIndex, cornerIndex * 2 + 1) - 0.5f) * 2f * backdropRandomness;
        return new Vector2(x, y);
    }
    
    void ApplyShadowMesh()
    {
        if (_tmpText == null || _shadowMesh == null) return;
        
        // Read CURRENT mesh state (after all TextAnimator modifications)
        Mesh tmpMesh = _tmpText.mesh;
        if (tmpMesh == null || tmpMesh.vertexCount == 0) return;
        
        Vector3[] srcVerts = tmpMesh.vertices;
        Vector2[] srcUV0 = tmpMesh.uv;
        Color32[] srcColors = tmpMesh.colors32;
        int[] srcTris = tmpMesh.triangles;
        
        int srcVertCount = srcVerts.Length;
        if (srcVertCount == 0 || srcUV0 == null || srcColors == null) return;
        
        // Calculate offset in local space
        Vector3 offset = new Vector3(currentShadowOffset.x, currentShadowOffset.y, 0);
        if (_canvas != null)
        {
            offset /= _canvas.scaleFactor;
        }
        offset = transform.InverseTransformVector(offset);
        
        // Count visible characters for backdrop
        TMP_TextInfo textInfo = _tmpText.textInfo;
        int visibleCharCount = 0;
        if (enableBackdrop && textInfo != null)
        {
            for (int i = 0; i < textInfo.characterCount; i++)
            {
                if (textInfo.characterInfo[i].isVisible)
                    visibleCharCount++;
            }
        }
        
        // Calculate array sizes
        // Backdrop: 4 verts per char
        // Backdrop shadow (if shadowFromBackdrop): 4 verts per char
        // Letter shadow (if !shadowFromBackdrop): srcVertCount
        // Main text: srcVertCount
        int backdropVertCount = enableBackdrop ? visibleCharCount * 4 : 0;
        int backdropTriCount = enableBackdrop ? visibleCharCount * 6 : 0;
        int backdropShadowVertCount = (enableBackdrop && shadowFromBackdrop) ? visibleCharCount * 4 : 0;
        int backdropShadowTriCount = (enableBackdrop && shadowFromBackdrop) ? visibleCharCount * 6 : 0;
        int letterShadowVertCount = (!shadowFromBackdrop || !enableBackdrop) ? srcVertCount : 0;
        int letterShadowTriCount = (!shadowFromBackdrop || !enableBackdrop) ? srcTris.Length : 0;
        
        int newVertCount = backdropShadowVertCount + backdropVertCount + letterShadowVertCount + srcVertCount;
        int newTriCount = backdropShadowTriCount + backdropTriCount + letterShadowTriCount + srcTris.Length;
        
        Vector3[] newVerts = new Vector3[newVertCount];
        Vector2[] newUV0 = new Vector2[newVertCount];
        Vector2[] newUV1 = new Vector2[newVertCount];
        Color32[] newColors = new Color32[newVertCount];
        int[] newTris = new int[newTriCount];
        
        int vertOffset = 0;
        int triOffset = 0;
        
        // Store backdrop vertices for shadow duplication
        Vector3[] backdropVerts = enableBackdrop ? new Vector3[visibleCharCount * 4] : null;
        Color32[] backdropColors = enableBackdrop ? new Color32[visibleCharCount * 4] : null;
        
        // === 1. BACKDROP SHADOW layer (UV1.x = 1) - rendered first (behind everything) ===
        if (enableBackdrop && shadowFromBackdrop && textInfo != null)
        {
            int charVisibleIndex = 0;
            for (int i = 0; i < textInfo.characterCount; i++)
            {
                TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
                if (!charInfo.isVisible) continue;
                
                int vertIdx = charInfo.vertexIndex;
                int matIdx = charInfo.materialReferenceIndex;
                
                if (matIdx >= textInfo.meshInfo.Length) continue;
                Vector3[] meshVerts = textInfo.meshInfo[matIdx].vertices;
                Color32[] meshColors = textInfo.meshInfo[matIdx].colors32;
                
                if (vertIdx + 3 >= meshVerts.Length) continue;
                
                Vector3 bl = meshVerts[vertIdx];
                Vector3 tl = meshVerts[vertIdx + 1];
                Vector3 tr = meshVerts[vertIdx + 2];
                Vector3 br = meshVerts[vertIdx + 3];
                
                // Random corner offsets for irregular shape - each corner moves independently
                Vector2 rBL = GetRandomCornerOffset(i, 0);
                Vector2 rTL = GetRandomCornerOffset(i, 1);
                Vector2 rTR = GetRandomCornerOffset(i, 2);
                Vector2 rBR = GetRandomCornerOffset(i, 3);
                
                // Base padding expansion + individual corner randomness
                // This creates trapezoids, parallelograms, etc.
                Vector3 backdropBL = bl + new Vector3(-backdropPadding, -backdropPadding, 0) + new Vector3(rBL.x, rBL.y, 0);
                Vector3 backdropTL = tl + new Vector3(-backdropPadding, backdropPadding, 0) + new Vector3(rTL.x, rTL.y, 0);
                Vector3 backdropTR = tr + new Vector3(backdropPadding, backdropPadding, 0) + new Vector3(rTR.x, rTR.y, 0);
                Vector3 backdropBR = br + new Vector3(backdropPadding, -backdropPadding, 0) + new Vector3(rBR.x, rBR.y, 0);
                
                byte alpha = meshColors[vertIdx].a;
                
                // Store for later backdrop layer
                int backdropIdx = charVisibleIndex * 4;
                backdropVerts[backdropIdx] = backdropBL;
                backdropVerts[backdropIdx + 1] = backdropTL;
                backdropVerts[backdropIdx + 2] = backdropTR;
                backdropVerts[backdropIdx + 3] = backdropBR;
                
                // Generate per-letter color variation
                Color variedColor = GetVariedColor(i, alpha);
                Color32 variedCol32 = variedColor;
                backdropColors[backdropIdx] = variedCol32;
                backdropColors[backdropIdx + 1] = variedCol32;
                backdropColors[backdropIdx + 2] = variedCol32;
                backdropColors[backdropIdx + 3] = variedCol32;
                
                // Add shadow vertices (offset backdrop)
                // UV1 = (1, 1) means shadow AND backdrop type (solid fill)
                int baseVert = vertOffset;
                newVerts[vertOffset] = backdropBL + offset;
                newUV0[vertOffset] = new Vector2(0, 0);
                newUV1[vertOffset] = new Vector2(1, 1); // Shadow + backdrop indicator
                newColors[vertOffset] = new Color32(255, 255, 255, alpha);
                vertOffset++;
                
                newVerts[vertOffset] = backdropTL + offset;
                newUV0[vertOffset] = new Vector2(0, 1);
                newUV1[vertOffset] = new Vector2(1, 1);
                newColors[vertOffset] = new Color32(255, 255, 255, alpha);
                vertOffset++;
                
                newVerts[vertOffset] = backdropTR + offset;
                newUV0[vertOffset] = new Vector2(1, 1);
                newUV1[vertOffset] = new Vector2(1, 1);
                newColors[vertOffset] = new Color32(255, 255, 255, alpha);
                vertOffset++;
                
                newVerts[vertOffset] = backdropBR + offset;
                newUV0[vertOffset] = new Vector2(1, 0);
                newUV1[vertOffset] = new Vector2(1, 1);
                newColors[vertOffset] = new Color32(255, 255, 255, alpha);
                vertOffset++;
                
                newTris[triOffset++] = baseVert;
                newTris[triOffset++] = baseVert + 1;
                newTris[triOffset++] = baseVert + 2;
                newTris[triOffset++] = baseVert + 2;
                newTris[triOffset++] = baseVert + 3;
                newTris[triOffset++] = baseVert;
                
                charVisibleIndex++;
            }
        }
        
        // === 2. BACKDROP layer (UV1.x = 2) ===
        if (enableBackdrop && textInfo != null)
        {
            int charVisibleIndex = 0;
            for (int i = 0; i < textInfo.characterCount; i++)
            {
                TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
                if (!charInfo.isVisible) continue;
                
                int backdropIdx = charVisibleIndex * 4;
                
                // If we already computed backdrop verts in shadow pass, reuse them
                Vector3 backdropBL, backdropTL, backdropTR, backdropBR;
                Color32 variedCol32;
                
                if (shadowFromBackdrop && backdropVerts != null)
                {
                    backdropBL = backdropVerts[backdropIdx];
                    backdropTL = backdropVerts[backdropIdx + 1];
                    backdropTR = backdropVerts[backdropIdx + 2];
                    backdropBR = backdropVerts[backdropIdx + 3];
                    variedCol32 = backdropColors[backdropIdx];
                }
                else
                {
                    // Compute fresh
                    int vertIdx = charInfo.vertexIndex;
                    int matIdx = charInfo.materialReferenceIndex;
                    
                    if (matIdx >= textInfo.meshInfo.Length) { charVisibleIndex++; continue; }
                    Vector3[] meshVerts = textInfo.meshInfo[matIdx].vertices;
                    Color32[] meshColors = textInfo.meshInfo[matIdx].colors32;
                    
                    if (vertIdx + 3 >= meshVerts.Length) { charVisibleIndex++; continue; }
                    
                    Vector3 bl = meshVerts[vertIdx];
                    Vector3 tl = meshVerts[vertIdx + 1];
                    Vector3 tr = meshVerts[vertIdx + 2];
                    Vector3 br = meshVerts[vertIdx + 3];
                    
                    Vector2 rBL = GetRandomCornerOffset(i, 0);
                    Vector2 rTL = GetRandomCornerOffset(i, 1);
                    Vector2 rTR = GetRandomCornerOffset(i, 2);
                    Vector2 rBR = GetRandomCornerOffset(i, 3);
                    
                    backdropBL = bl + new Vector3(-backdropPadding, -backdropPadding, 0) + new Vector3(rBL.x, rBL.y, 0);
                    backdropTL = tl + new Vector3(-backdropPadding, backdropPadding, 0) + new Vector3(rTL.x, rTL.y, 0);
                    backdropTR = tr + new Vector3(backdropPadding, backdropPadding, 0) + new Vector3(rTR.x, rTR.y, 0);
                    backdropBR = br + new Vector3(backdropPadding, -backdropPadding, 0) + new Vector3(rBR.x, rBR.y, 0);
                    
                    byte alpha = meshColors[vertIdx].a;
                    variedCol32 = GetVariedColor(i, alpha);
                }
                
                int baseVert = vertOffset;
                newVerts[vertOffset] = backdropBL;
                newUV0[vertOffset] = new Vector2(0, 0);
                newUV1[vertOffset] = new Vector2(2, 0); // Backdrop flag
                newColors[vertOffset] = variedCol32;
                vertOffset++;
                
                newVerts[vertOffset] = backdropTL;
                newUV0[vertOffset] = new Vector2(0, 1);
                newUV1[vertOffset] = new Vector2(2, 0);
                newColors[vertOffset] = variedCol32;
                vertOffset++;
                
                newVerts[vertOffset] = backdropTR;
                newUV0[vertOffset] = new Vector2(1, 1);
                newUV1[vertOffset] = new Vector2(2, 0);
                newColors[vertOffset] = variedCol32;
                vertOffset++;
                
                newVerts[vertOffset] = backdropBR;
                newUV0[vertOffset] = new Vector2(1, 0);
                newUV1[vertOffset] = new Vector2(2, 0);
                newColors[vertOffset] = variedCol32;
                vertOffset++;
                
                newTris[triOffset++] = baseVert;
                newTris[triOffset++] = baseVert + 1;
                newTris[triOffset++] = baseVert + 2;
                newTris[triOffset++] = baseVert + 2;
                newTris[triOffset++] = baseVert + 3;
                newTris[triOffset++] = baseVert;
                
                charVisibleIndex++;
            }
        }
        
        // === 3. LETTER SHADOW layer (UV1.x = 1) - only if not using shadow from backdrop ===
        if (!shadowFromBackdrop || !enableBackdrop)
        {
            int shadowBaseVert = vertOffset;
            for (int i = 0; i < srcVertCount; i++)
            {
                newVerts[vertOffset] = srcVerts[i] + offset;
                newUV0[vertOffset] = srcUV0[i];
                newUV1[vertOffset] = new Vector2(1, 0); // Shadow flag
                newColors[vertOffset] = srcColors[i];
                vertOffset++;
            }
            
            for (int i = 0; i < srcTris.Length; i++)
            {
                newTris[triOffset++] = srcTris[i] + shadowBaseVert;
            }
        }
        
        // === 4. MAIN TEXT layer (UV1.x = 0) ===
        int mainBaseVert = vertOffset;
        for (int i = 0; i < srcVertCount; i++)
        {
            newVerts[vertOffset] = srcVerts[i];
            newUV0[vertOffset] = srcUV0[i];
            newUV1[vertOffset] = new Vector2(0, 0); // Main flag
            newColors[vertOffset] = srcColors[i];
            vertOffset++;
        }
        
        for (int i = 0; i < srcTris.Length; i++)
        {
            newTris[triOffset++] = srcTris[i] + mainBaseVert;
        }
        
        // Apply to OUR mesh
        _shadowMesh.Clear();
        _shadowMesh.vertices = newVerts;
        _shadowMesh.uv = newUV0;
        _shadowMesh.uv2 = newUV1;
        _shadowMesh.colors32 = newColors;
        _shadowMesh.triangles = newTris;
        
        // Set our mesh for rendering - this happens RIGHT BEFORE canvas renders
        _tmpText.canvasRenderer.SetMesh(_shadowMesh);
    }
    
    // Generate per-letter color with HSV variation
    Color GetVariedColor(int charIndex, byte alpha)
    {
        Color.RGBToHSV(backdropColor, out float h, out float s, out float v);
        
        // Apply random variation
        float hueOffset = (SeededRandom(charIndex, 100) - 0.5f) * 2f * backdropHueVariation;
        float satOffset = (SeededRandom(charIndex, 101) - 0.5f) * 2f * backdropSatVariation;
        float valOffset = (SeededRandom(charIndex, 102) - 0.5f) * 2f * backdropBrightVariation;
        
        h = Mathf.Repeat(h + hueOffset, 1f);
        s = Mathf.Clamp01(s + satOffset);
        v = Mathf.Clamp01(v + valOffset);
        
        Color result = Color.HSVToRGB(h, s, v);
        result.a = backdropColor.a * (alpha / 255f);
        return result;
    }
    
    void OnValidate()
    {
        if (_materialInstance != null)
        {
            _materialInstance.SetColor(ShadowColorID, shadowColor);
            _materialInstance.SetColor(BackdropColorID, backdropColor);
        }
    }
}
