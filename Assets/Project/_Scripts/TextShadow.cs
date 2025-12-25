using UnityEngine;
using TMPro;

/// <summary>
/// Shader-based shadow for TMP text using mesh modification.
/// Duplicates text geometry for shadow - same approach as ShaderShadow for Image.
/// Shadow vertices are marked with UV1.x=1 and rendered behind main text.
/// 
/// IMPORTANT: Runs AFTER TextAnimator (execution order 100) to pick up animated vertices.
/// </summary>
[RequireComponent(typeof(TMP_Text))]
[ExecuteAlways]
[DefaultExecutionOrder(100)] // Run after TextAnimator
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
    
    [Header("Debug")]
    [SerializeField] private Vector2 currentShadowOffset;
    [SerializeField] private float currentEffectiveIntensity;
    
    private TMP_Text _tmpText;
    private Canvas _canvas;
    private Material _materialInstance;
    private float _lastScale;
    
    // Our own mesh instance (separate from TMP's internal mesh)
    private Mesh _shadowMesh;
    
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
    
    void OnDestroy()
    {
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
        
        if (_tmpText.fontMaterial != null && _tmpText.fontMaterial.shader.name == "UI/VoidText")
        {
            _materialInstance = new Material(_tmpText.fontMaterial);
            _tmpText.fontMaterial = _materialInstance;
        }
    }
    
    void LateUpdate()
    {
        if (_tmpText == null) return;
        
        // Create material if needed
        if (_materialInstance == null)
        {
            if (_tmpText.fontMaterial != null && _tmpText.fontMaterial.shader.name == "UI/VoidText")
            {
                CreateMaterialInstance();
            }
            else
            {
                return;
            }
        }
        
        // Update shadow color
        _materialInstance.SetColor(ShadowColorID, shadowColor);
        
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
        
        // Always apply mesh - we need to pick up TextAnimator's changes
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
    
    void ApplyShadowMesh()
    {
        if (_tmpText == null || _shadowMesh == null) return;
        
        // Read from the MESH directly (after TextAnimator has modified it via UpdateGeometry)
        // This mesh contains the animated vertex positions
        Mesh tmpMesh = _tmpText.mesh;
        if (tmpMesh == null || tmpMesh.vertexCount == 0) return;
        
        Vector3[] srcVerts = tmpMesh.vertices;
        Vector2[] srcUV0 = tmpMesh.uv;
        Color32[] srcColors = tmpMesh.colors32;
        int[] srcTris = tmpMesh.triangles;
        
        int srcVertCount = srcVerts.Length;
        if (srcVertCount == 0) return;
        
        // Calculate offset in local space
        Vector3 offset = new Vector3(currentShadowOffset.x, currentShadowOffset.y, 0);
        if (_canvas != null)
        {
            offset /= _canvas.scaleFactor;
        }
        offset = transform.InverseTransformVector(offset);
        
        // Create doubled arrays for shadow + main
        int newVertCount = srcVertCount * 2;
        Vector3[] newVerts = new Vector3[newVertCount];
        Vector2[] newUV0 = new Vector2[newVertCount];
        Vector2[] newUV1 = new Vector2[newVertCount]; // Shadow flag
        Color32[] newColors = new Color32[newVertCount];
        
        // Shadow vertices first (rendered behind)
        for (int i = 0; i < srcVertCount; i++)
        {
            newVerts[i] = srcVerts[i] + offset;
            newUV0[i] = srcUV0[i];
            newUV1[i] = new Vector2(1, 0); // Shadow flag
            newColors[i] = srcColors[i];
        }
        
        // Main vertices second (rendered on top)
        for (int i = 0; i < srcVertCount; i++)
        {
            int idx = srcVertCount + i;
            newVerts[idx] = srcVerts[i];
            newUV0[idx] = srcUV0[i];
            newUV1[idx] = new Vector2(0, 0); // Main flag
            newColors[idx] = srcColors[i];
        }
        
        // Duplicate triangles
        int srcTriCount = srcTris.Length;
        int[] newTris = new int[srcTriCount * 2];
        
        // Shadow triangles
        System.Array.Copy(srcTris, 0, newTris, 0, srcTriCount);
        
        // Main triangles (offset by srcVertCount)
        for (int i = 0; i < srcTriCount; i++)
        {
            newTris[srcTriCount + i] = srcTris[i] + srcVertCount;
        }
        
        // Apply to OUR mesh
        _shadowMesh.Clear();
        _shadowMesh.vertices = newVerts;
        _shadowMesh.uv = newUV0;
        _shadowMesh.uv2 = newUV1;
        _shadowMesh.colors32 = newColors;
        _shadowMesh.triangles = newTris;
        
        // Replace what canvasRenderer displays with our shadow mesh
        _tmpText.canvasRenderer.SetMesh(_shadowMesh);
    }
    
    void OnValidate()
    {
        if (_materialInstance != null)
        {
            _materialInstance.SetColor(ShadowColorID, shadowColor);
        }
    }
}
