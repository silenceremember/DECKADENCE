// CardDisplay.cs

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
    public float movementLimit = 450f;
    public float lockedLimit = 80f;      
    public float unlockDistance = 50f;   
    public float choiceThreshold = 300f;
    public float sensitivity = 1.0f;
    public float lockedDampening = 0.6f;
    
    [Header("Настройки Анимации")]
    public float hiddenY = 2500f; 
    public float fallDuration = 0.6f;     // Чуть ускорил падение для динамики
    public float interactionDelay = 0.5f; 

    [Header("Typewriter Effect")]
    public float typingSpeed = 0.02f; // Скорость появления одной буквы (сек)

    // Насколько сильно карта реагирует при локе (настройка)
    [Header("Настройки Лока")]
    public float lockedInputScale = 0.15f;

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
    private bool _isUnlockAnimating = false; // Блокирует Update во время "встряски"


    private float _currentVerticalOffset = 0f; 
    private float _currentAngularOffset = 0f;

    private float _shakeOffset = 0f;

    // Коэффициент передачи движения (0.15 = вяло, 1.0 = 1 к 1)
    private float _inputScale = 1.0f;

    // Твин для текста, чтобы можно было остановить
    private Tween _typewriterTween;

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
        
        // 1. Устанавливаем текст, но скрываем все буквы
        questionText.text = data.dialogueText;
        questionText.maxVisibleCharacters = 0; 

        if (actionText) actionText.gameObject.SetActive(false);
        
        _isLocked = false;
        _isFront = isFront;
        _isInteractable = false;
        _safetyLock = false;
        _isUnlockAnimating = false; // Важно сбросить анимацию
        _inputScale = 1.0f; // По умолчанию полный контроль
        
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

        _safetyLock = true;
        
        // ВКЛЮЧАЕМ "ВЯЛОСТЬ"
        // Карта будет двигаться, но с маленькой амплитудой
        _inputScale = lockedInputScale;
        
        DOTween.Kill(this); 
        // Убиваем старый твин текста, если он вдруг еще идет
        if (_typewriterTween != null) _typewriterTween.Kill();

        // Анимация падения
        DOTween.To(() => _currentVerticalOffset, x => _currentVerticalOffset = x, 0f, fallDuration)
            .SetEase(Ease.OutBack).SetTarget(this);

        DOTween.To(() => _currentAngularOffset, x => _currentAngularOffset = x, 0f, fallDuration)
            .SetEase(Ease.OutBack).SetTarget(this);
            
        // ЗАПУСК ТЕКСТА
        // Мы запускаем текст чуть раньше, чем закончится падение (на 80%), чтобы было динамичнее
        DOVirtual.DelayedCall(fallDuration * 0.8f, () => 
        {
            StartTypewriter();
        }).SetTarget(this);

        // Разблокировка управления
        DOVirtual.DelayedCall(interactionDelay, () => 
        {
            _isInteractable = true;
        }).SetTarget(this);
    }

    void StartTypewriter()
    {
        int totalChars = questionText.text.Length;
        questionText.maxVisibleCharacters = 0;

        // Рассчитываем длительность: длина текста * скорость одной буквы
        float duration = totalChars * typingSpeed;

        // Анимируем число видимых символов от 0 до totalChars
        _typewriterTween = DOTween.To(x => questionText.maxVisibleCharacters = (int)x, 0, totalChars, duration)
            .SetEase(Ease.Linear)
            .SetTarget(this);
            // .OnUpdate(() => { PlayTypeSound(); }) // Сюда можно добавить звук "тук-тук"
    }

    void Update()
    {
        if (!_isFront || _isLocked) return;
        HandleMotion();
    }

    void HandleMotion()
    {
        // 1. СЫРОЙ ВВОД
        float rawDiff = 0f;
        if (Mouse.current != null)
        {
            float screenCenter = Screen.width / 2f;
            rawDiff = (Mouse.current.position.ReadValue().x - screenCenter) * sensitivity;
        }

        bool isClickFrame = Mouse.current.leftButton.wasPressedThisFrame;

        // 2. ОБРАБОТКА ЛОКА (Клик по карте)
        if (_safetyLock && _isInteractable)
        {
            if (isClickFrame)
            {
                TriggerWobbleUnlock();
                isClickFrame = false; 
            }
        }

        // 3. МАТЕМАТИКА (Масштабирование)
        // Умножаем ввод на наш коэффициент.
        // Если лок: rawDiff (500) * 0.15 = 75. Карта сместится только на 75.
        // Если анлок: rawDiff (500) * 1.0 = 500. Карта там же где и мышь.
        float scaledDiff = rawDiff * _inputScale;

        // Ограничиваем только ГЛОБАЛЬНЫМ лимитом экрана, никаких "стенок" посередине
        float targetX = Mathf.Clamp(scaledDiff, -movementLimit, movementLimit);

        // 4. ФИЗИКА
        float smoothX = Mathf.Lerp(_rectTransform.anchoredPosition.x, targetX, Time.deltaTime * 20f);
        
        _rectTransform.anchoredPosition = new Vector2(smoothX + _shakeOffset, _currentVerticalOffset);

        float mouseRotation = -_rectTransform.anchoredPosition.x * 0.05f;
        _rectTransform.rotation = Quaternion.Euler(0, 0, mouseRotation + _currentAngularOffset);

        // 5. ТЕКСТ И ВЫБОР
        UpdateVisuals(targetX);
        
        if (_isInteractable && !_safetyLock && isClickFrame)
        {
            HandleInput(targetX);
        }
    }

void TriggerWobbleUnlock()
    {
        _safetyLock = false; 

        // 1. ОПРЕДЕЛЯЕМ НАПРАВЛЕНИЕ
        float mouseX = 0f;
        if (Mouse.current != null)
        {
            mouseX = Mouse.current.position.ReadValue().x - Screen.width / 2f;
        }

        // Если мышь справа (> 0), то первый рывок влево (-1).
        // Если мышь слева или в центре, то первый рывок вправо (+1).
        // Это делает анимацию логичной физически.
        float dir = (mouseX > 0) ? -1f : 1f;
        
        float power = 10f; // Амплитуда, как ты просил

        DOTween.Kill(this, "shake"); 
        DOTween.Kill(this, "inputScale");

        Sequence seq = DOTween.Sequence();
        seq.SetId("shake");
        
        // --- АНИМАЦИЯ ПРОБУЖДЕНИЯ ---
        
        // 1. Рывок "ПРОТИВ" (Замах) -> 30px
        seq.Append(DOTween.To(() => _shakeOffset, x => _shakeOffset = x, dir * power, 0.08f)
            .SetEase(Ease.OutSine));
        
        // 2. Рывок "ЗА" (Перехлест) -> -30px (в другую сторону)
        seq.Append(DOTween.To(() => _shakeOffset, x => _shakeOffset = x, -dir * power, 0.1f)
            .SetEase(Ease.InOutSine));
        
        // 3. Возврат в ноль (Успокоение)
        seq.Append(DOTween.To(() => _shakeOffset, x => _shakeOffset = x, 0f, 0.3f)
            .SetEase(Ease.OutBack)); // Легкая пружинка в конце

        // --- ФИЗИКА (РАЗГОН) ---
        // Включаем следование за мышью параллельно с тряской
        DOTween.To(() => _inputScale, x => _inputScale = x, 1.0f, 0.3f)
            .SetEase(Ease.OutCubic) 
            .SetId("inputScale")
            .SetTarget(this);
    }

    void UpdateVisuals(float diff)
    {
        // Скрываем если падает ИЛИ если ЗАБЛОКИРОВАНО (но не во время анимации разблокировки!)
        if (_currentVerticalOffset > 150f || (_safetyLock && !_isUnlockAnimating)) 
        {
            if (actionText && actionText.gameObject.activeSelf) actionText.gameObject.SetActive(false);
            GameManager.Instance.ResetHighlights();
            return;
        }

        float absDiff = Mathf.Abs(diff);
        
        // Deadzone
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
