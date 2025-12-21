using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using UnityEngine.InputSystem;

public class CardDisplay : MonoBehaviour
{
    [Header("UI Компоненты")]
    public Image characterImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI questionText;
    public TextMeshProUGUI actionText; 
    public CanvasGroup canvasGroup;

    [Header("Настройки Управления")]
    public float movementLimit = 450f;   // Полный ход (разблокировано)
    public float lockedLimit = 80f;      // Короткий ход (заблокировано) - карта упрется сюда
    public float unlockDistance = 50f;   // Насколько близко к центру надо вернуть мышь, чтобы снять блок
    public float choiceThreshold = 300f;
    public float sensitivity = 1.0f;
    
    [Header("Настройки Анимации")]
    public float hiddenY = 2500f; 
    public float fallDuration = 0.7f;
    public float interactionDelay = 0.5f; 

    [Header("Сочность Текста")]
    public float minScale = 0.6f;
    public float maxScale = 1.3f;
    public float shakeSpeed = 30f;
    public float shakeAngle = 10f;
    public Color normalColor = Color.white;
    public Color snapColor = Color.yellow;

    public CardData CurrentData { get; private set; }
    
    private RectTransform _rectTransform;
    private RectTransform _textRectTransform;
    private bool _isLocked;       
    private bool _isFront;        
    private bool _isInteractable; 
    
    // ПРЕДОХРАНИТЕЛЬ
    private bool _safetyLock = false; 

    private float _currentVerticalOffset = 0f; 
    private float _currentAngularOffset = 0f;

    void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        if (actionText != null) _textRectTransform = actionText.GetComponent<RectTransform>();
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
    }

    public void Setup(CardData data, bool isFront)
    {
        CurrentData = data;
        characterImage.sprite = data.characterSprite;
        nameText.text = data.characterName;
        questionText.text = data.dialogueText;

        if (actionText) actionText.gameObject.SetActive(false);
        
        _isLocked = false;
        _isFront = isFront;
        _isInteractable = false;
        _safetyLock = false;
        
        if (isFront)
        {
            _currentVerticalOffset = 0f;
            _currentAngularOffset = 0f;
            _rectTransform.anchoredPosition = Vector2.zero;
            _rectTransform.rotation = Quaternion.identity;
            canvasGroup.alpha = 1f;
        }
        else
        {
            _currentVerticalOffset = hiddenY;
            _currentAngularOffset = 5f;
            _rectTransform.anchoredPosition = new Vector2(0, hiddenY);
            _rectTransform.rotation = Quaternion.Euler(0, 0, _currentAngularOffset);
            canvasGroup.alpha = 1f;
        }
    }

    public void AnimateToFront()
    {
        _isFront = true;
        _isLocked = false;
        _isInteractable = false;

        // 1. ВКЛЮЧАЕМ ПРЕДОХРАНИТЕЛЬ ПРИ ВЫЛЕТЕ
        _safetyLock = true;
        
        DOTween.Kill(this); 

        DOTween.To(() => _currentVerticalOffset, x => _currentVerticalOffset = x, 0f, fallDuration)
            .SetEase(Ease.OutBack).SetTarget(this);

        DOTween.To(() => _currentAngularOffset, x => _currentAngularOffset = x, 0f, fallDuration)
            .SetEase(Ease.OutBack).SetTarget(this);
            
        DOVirtual.DelayedCall(interactionDelay, () => 
        {
            _isInteractable = true;
        }).SetTarget(this);
    }

    void Update()
    {
        if (!_isFront || _isLocked) return;
        HandleMotion();
    }

    void HandleMotion()
    {
        float rawDiff = 0f;
        
        if (Mouse.current != null)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            float screenCenter = Screen.width / 2f;
            rawDiff = (mousePos.x - screenCenter) * sensitivity;
        }

        // --- ЛОГИКА ОГРАНИЧЕНИЯ (CLAMP) ---
        
        float currentLimit = movementLimit; // По умолчанию полный ход

        if (_safetyLock)
        {
            // Если мышь вернулась в центр - снимаем замок
            if (Mathf.Abs(rawDiff) < unlockDistance)
            {
                _safetyLock = false;
            }
            else
            {
                // Пока замок висит - ограничиваем движение коротким поводком
                currentLimit = lockedLimit;
            }
        }

        // Ключевой момент: Мы используем Clamp с динамическим лимитом
        // Если rawDiff = 300, а limit = 80 -> applied = 80.
        // Если rawDiff = 50, а limit = 80 -> applied = 50.
        // Это и есть "1-2-3-4 совпадают, а 5-6 обрезаются".
        float appliedDiff = Mathf.Clamp(rawDiff, -currentLimit, currentLimit);

        // --- ФИЗИКА КАРТЫ ---
        // Lerp для плавности
        float smoothX = Mathf.Lerp(_rectTransform.anchoredPosition.x, appliedDiff, Time.deltaTime * 20f);
        _rectTransform.anchoredPosition = new Vector2(smoothX, _currentVerticalOffset);

        float mouseRotation = -smoothX * 0.05f;
        _rectTransform.rotation = Quaternion.Euler(0, 0, mouseRotation + _currentAngularOffset);

        UpdateVisuals(appliedDiff);
        
        // Клик разрешен только если нет блокировки (карта на длинном поводке) и анимация прошла
        if (_isInteractable && !_safetyLock)
        {
            HandleInput(appliedDiff);
        }
    }

    void UpdateVisuals(float diff)
    {
        // Скрываем если карта падает или ВКЛЮЧЕН предохранитель
        if (_currentVerticalOffset > 150f || _safetyLock) 
        {
            if (actionText && actionText.gameObject.activeSelf) actionText.gameObject.SetActive(false);
            GameManager.Instance.ResetHighlights();
            return;
        }

        float absDiff = Mathf.Abs(diff);
        
        // Deadzone (чуть больше lockedLimit, чтобы текст не мелькал, когда карта упирается в стену)
        if (absDiff < lockedLimit + 10f)
        {
             if (actionText && actionText.gameObject.activeSelf) actionText.gameObject.SetActive(false);
             GameManager.Instance.ResetHighlights();
             return;
        }

        if (actionText && !actionText.gameObject.activeSelf) actionText.gameObject.SetActive(true);

        bool isRight = diff > 0;
        actionText.text = isRight ? CurrentData.rightChoiceText : CurrentData.leftChoiceText;

        float fadeRange = 150f;
        float alpha = Mathf.Clamp01((absDiff - (lockedLimit + 10f)) / fadeRange);
        
        Color targetColor = normalColor;
        
        float progress = absDiff / choiceThreshold;
        float clampedProgress = Mathf.Clamp01(progress);

        float targetScale = Mathf.Lerp(minScale, maxScale, clampedProgress);
        if (progress > 1.0f) targetScale += Mathf.Sin(Time.time * 20f) * 0.1f;
        _textRectTransform.localScale = Vector3.one * targetScale;

        float baseAngle = isRight ? -5f : 5f;
        float currentShake = Mathf.Sin(Time.time * shakeSpeed) * (shakeAngle * clampedProgress);
        _textRectTransform.localRotation = Quaternion.Euler(0, 0, baseAngle + currentShake);

        if (progress >= 1.0f)
        {
            targetColor = snapColor;
             if (isRight) GameManager.Instance.HighlightResources(CurrentData.rightCrown, CurrentData.rightChurch, CurrentData.rightMob, CurrentData.rightPlague);
            else GameManager.Instance.HighlightResources(CurrentData.leftCrown, CurrentData.leftChurch, CurrentData.leftMob, CurrentData.leftPlague);
        }
        else
        {
            GameManager.Instance.ResetHighlights();
        }
        
        targetColor.a = alpha;
        actionText.color = targetColor;
    }

    void HandleInput(float diff)
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (diff > choiceThreshold) MakeChoice(true);
            else if (diff < -choiceThreshold) MakeChoice(false);
        }
    }

    void MakeChoice(bool isRight)
    {
        _isLocked = true;
        _isInteractable = false;
        
        if (actionText) actionText.gameObject.SetActive(false);

        if (isRight) GameManager.Instance.ApplyCardEffect(CurrentData.rightCrown, CurrentData.rightChurch, CurrentData.rightMob, CurrentData.rightPlague);
        else GameManager.Instance.ApplyCardEffect(CurrentData.leftCrown, CurrentData.leftChurch, CurrentData.leftMob, CurrentData.leftPlague);

        float endX = isRight ? 1500f : -1500f;
        float endRotation = isRight ? -45f : 45f;

        Sequence seq = DOTween.Sequence();
        seq.Append(_rectTransform.DOAnchorPosX(endX, 0.4f).SetEase(Ease.InBack));
        seq.Join(_rectTransform.DORotate(new Vector3(0, 0, endRotation), 0.4f));

        seq.OnComplete(() => 
        {
            GameManager.Instance.OnCardAnimationComplete();
        });
    }
}
