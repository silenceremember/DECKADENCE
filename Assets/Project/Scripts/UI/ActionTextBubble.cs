// ActionTextBubble.cs
// Component to manage action text bubble with dynamic size
// Size based on actual visible text bounds from TMP
// Does NOT modify anchors - only uses sizeDelta

using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages action text bubble that dynamically sizes based on visible text.
/// Anchors should be set to stretch (0,0)-(1,1) in Inspector.
/// Script only modifies sizeDelta during Play mode.
/// </summary>
public class ActionTextBubble : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The bubble background RectTransform (with DialogShadow)")]
    public RectTransform bubbleRect;
    
    [Tooltip("The text component inside the bubble")]
    public TextMeshProUGUI textComponent;
    
    [Tooltip("Optional: DialogShadow for disabling arrow")]
    public DialogShadow dialogShadow;
    
    [Tooltip("Image component on the bubble for color control")]
    public Image bubbleImage;
    
    [Header("Size Settings")]
    [Tooltip("Padding around text")]
    public Vector2 padding = new Vector2(40f, 30f);
    
    [Header("Animation")]
    [Tooltip("Speed of size interpolation")]
    public float sizeSpeed = 20f;
    
    [Header("Debug")]
    [SerializeField] private int _debugVisibleChars;
    [SerializeField] private Vector2 _debugTargetSize;
    [SerializeField] private Vector2 _debugCurrentSize;
    
    // Cached components
    private TextAnimator _textAnimator;
    
    // The parent size (what bubble should be when fully expanded)
    private Vector2 _parentSize;
    
    // Animation state - this is the TARGET sizeDelta
    // When collapsed: sizeDelta = -parentSize (shrinks to 0)
    // When expanded: sizeDelta approaches 0 (fills parent)
    private Vector2 _currentSizeDelta;
    private Vector2 _targetSizeDelta;
    
    void Awake()
    {
        CacheComponents();
    }
    
    void Start()
    {
        CacheParentSize();
        
        // Only collapse in Play mode
        if (Application.isPlaying)
        {
            ForceCollapse();
        }
    }
    
    void OnEnable()
    {
        CacheComponents();
        CacheParentSize();
        
        // Only collapse in Play mode
        if (Application.isPlaying)
        {
            ForceCollapse();
        }
    }
    
    void CacheComponents()
    {
        if (textComponent != null && _textAnimator == null)
        {
            _textAnimator = textComponent.GetComponent<TextAnimator>();
        }
        
        // Auto-get Image from bubbleRect if not assigned
        if (bubbleImage == null && bubbleRect != null)
        {
            bubbleImage = bubbleRect.GetComponent<Image>();
        }
        
        // Auto-disable arrow on DialogShadow
        if (dialogShadow != null)
        {
            dialogShadow.showArrow = false;
        }
    }
    
    void CacheParentSize()
    {
        if (bubbleRect != null && bubbleRect.parent is RectTransform parentRect)
        {
            _parentSize = parentRect.rect.size;
        }
        else if (bubbleRect != null)
        {
            // Fallback: use current size as parent size
            _parentSize = bubbleRect.rect.size;
        }
    }
    
    void ForceCollapse()
    {
        // With stretch anchors, sizeDelta = -parentSize means size = 0
        _currentSizeDelta = -_parentSize;
        _targetSizeDelta = -_parentSize;
        
        if (bubbleRect != null)
        {
            bubbleRect.sizeDelta = _currentSizeDelta;
        }
    }
    
    void Update()
    {
        if (textComponent == null || bubbleRect == null) return;
        
        UpdateTargetSize();
        AnimateSize();
        ApplySize();
    }
    
    void UpdateTargetSize()
    {
        int visibleChars = GetVisibleCharCount();
        _debugVisibleChars = visibleChars;
        
        if (visibleChars > 0)
        {
            // Calculate actual bounds of visible text
            Vector2 textBounds = GetVisibleTextBounds();
            
            // Desired size = text bounds + padding
            Vector2 desiredSize = textBounds + padding;
            
            // With stretch anchors: sizeDelta = desiredSize - parentSize
            _targetSizeDelta = desiredSize - _parentSize;
            
            _debugTargetSize = desiredSize;
        }
        else
        {
            // Collapsed: sizeDelta = -parentSize (size becomes 0)
            _targetSizeDelta = -_parentSize;
            _debugTargetSize = Vector2.zero;
        }
    }
    
    Vector2 GetVisibleTextBounds()
    {
        if (textComponent == null) return Vector2.zero;
        
        // Force mesh update to get accurate info
        textComponent.ForceMeshUpdate();
        
        TMP_TextInfo textInfo = textComponent.textInfo;
        if (textInfo == null || textInfo.characterCount == 0) return Vector2.zero;
        
        int visibleChars = GetVisibleCharCount();
        if (visibleChars == 0) return Vector2.zero;
        
        // Find bounds of visible characters
        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minY = float.MaxValue;
        float maxY = float.MinValue;
        
        bool hasVisibleChar = false;
        
        for (int i = 0; i < Mathf.Min(visibleChars, textInfo.characterCount); i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
            
            // В dynamic mode isVisible может быть false, но данные есть
            // Пропускаем только если нет реальных координат (пробелы)
            if (charInfo.bottomLeft == Vector3.zero && charInfo.topRight == Vector3.zero) continue;
            
            hasVisibleChar = true;
            
            // Get character corners
            Vector3 bl = charInfo.bottomLeft;
            Vector3 tr = charInfo.topRight;
            
            minX = Mathf.Min(minX, bl.x);
            maxX = Mathf.Max(maxX, tr.x);
            minY = Mathf.Min(minY, bl.y);
            maxY = Mathf.Max(maxY, tr.y);
        }
        
        if (!hasVisibleChar) return Vector2.zero;
        
        return new Vector2(maxX - minX, maxY - minY);
    }
    
    int GetVisibleCharCount()
    {
        if (_textAnimator != null)
        {
            return _textAnimator.VisibleCharacters;
        }
        else if (textComponent != null)
        {
            string text = textComponent.text ?? "";
            int maxVisible = textComponent.maxVisibleCharacters;
            int totalChars = text.Length;
            return (maxVisible >= 99999) ? totalChars : Mathf.Min(maxVisible, totalChars);
        }
        return 0;
    }
    
    void AnimateSize()
    {
        // Smooth interpolation
        _currentSizeDelta = Vector2.Lerp(_currentSizeDelta, _targetSizeDelta, Time.deltaTime * sizeSpeed);
        _debugCurrentSize = _currentSizeDelta + _parentSize; // Actual visible size
    }
    
    void ApplySize()
    {
        if (bubbleRect != null)
        {
            bubbleRect.sizeDelta = _currentSizeDelta;
        }
    }
    
    void LateUpdate()
    {
        // Force apply in LateUpdate
        if (bubbleRect != null)
        {
            bubbleRect.sizeDelta = _currentSizeDelta;
        }
    }
    
    /// <summary>
    /// Force immediate collapse.
    /// </summary>
    public void CollapseImmediate()
    {
        CacheParentSize();
        ForceCollapse();
    }
    
    /// <summary>
    /// Force immediate expand to current text size.
    /// </summary>
    public void ExpandImmediate()
    {
        CacheParentSize();
        UpdateTargetSize();
        _currentSizeDelta = _targetSizeDelta;
        ApplySize();
    }
    
    /// <summary>
    /// Set the bubble fill color via Image component.
    /// </summary>
    public void SetBubbleColor(Color color)
    {
        if (bubbleImage != null)
        {
            bubbleImage.color = color;
        }
    }
    
    /// <summary>
    /// Get current bubble color.
    /// </summary>
    public Color GetBubbleColor()
    {
        return bubbleImage != null ? bubbleImage.color : Color.white;
    }
}






