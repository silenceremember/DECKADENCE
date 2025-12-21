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
    
    // ЕДИНЫЙ ТЕКСТ ВЫБОРА
    public TextMeshProUGUI actionText; 
    
    public CanvasGroup canvasGroup;

    [Header("Настройки Управления")]
    public float movementLimit = 400f;
    public float choiceThreshold = 300f;
    public float sensitivity = 1.0f;
    
    [Header("Настройки Анимации")]
    public float hiddenY = 2500f; 
    public float fallDuration = 0.7f;
    public float interactionDelay = 0.5f; 

    [Header("Сочность Текста (Juiciness)")]
    public float minScale = 0.5f;       // Размер в начале
    public float maxScale = 1.4f;       // Размер при готовности
    public float shakeSpeed = 30f;      // Как быстро трясется
    public float shakeAngle = 3f;      // Угол наклона при тряске
    public Color normalColor = Color.white;
    public Color snapColor = Color.yellow; // Цвет, когда выбор готов

    // Данные
    public CardData CurrentData { get; private set; }
    
    // Внутренние переменные
    private RectTransform _rectTransform;
    private RectTransform _textRectTransform; // Кэшируем трансформ текста
    private bool _isLocked;       
    private bool _isFront;        
    private bool _isInteractable; 

    private float _currentVerticalOffset = 0f; 
    private float _currentAngularOffset = 0f;

    void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        // Кэшируем RectTransform текста для оптимизации
        if (actionText != null) _textRectTransform = actionText.GetComponent<RectTransform>();
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
    }

    public void Setup(CardData data, bool isFront)
    {
        CurrentData = data;
        characterImage.sprite = data.characterSprite;
        nameText.text = data.characterName;
        questionText.text = data.dialogueText;

        // Скрываем текст выбора при старте
        if (actionText)
        {
            actionText.gameObject.SetActive(false);
            actionText.alpha = 0;
            actionText.transform.localScale = Vector3.one * minScale;
            actionText.transform.localRotation = Quaternion.identity;
        }
        
        _isLocked = false;
        _isFront = isFront;
        _isInteractable = false;
        
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

        DOTween.Kill(this); 

        DOTween.To(() => _currentVerticalOffset, x => _currentVerticalOffset = x, 0f, fallDuration)
            .SetEase(Ease.OutBack).SetTarget(this);

        DOTween.To(() => _currentAngularOffset, x => _currentAngularOffset = x, 0f, fallDuration)
            .SetEase(Ease.OutBack).SetTarget(this);
            
        DOVirtual.DelayedCall(interactionDelay, () => { _isInteractable = true; }).SetTarget(this);
    }

    void Update()
    {
        if (!_isFront || _isLocked) return;
        HandleMotion();
    }

    void HandleMotion()
    {
        float targetX = 0f;
        
        if (Mouse.current != null)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            float screenCenter = Screen.width / 2f;
            float diff = (mousePos.x - screenCenter) * sensitivity;
            targetX = Mathf.Clamp(diff, -movementLimit, movementLimit);
        }

        float smoothX = Mathf.Lerp(_rectTransform.anchoredPosition.x, targetX, Time.deltaTime * 15f);
        _rectTransform.anchoredPosition = new Vector2(smoothX, _currentVerticalOffset);

        float mouseRotation = -smoothX * 0.05f;
        _rectTransform.rotation = Quaternion.Euler(0, 0, mouseRotation + _currentAngularOffset);

        UpdateVisuals(targetX);
        
        if (_isInteractable) HandleInput(targetX);
    }

    void UpdateVisuals(float diff)
    {
        // 1. Скрываем, если карта высоко
        if (_currentVerticalOffset > 150f) 
        {
            if (actionText.gameObject.activeSelf) actionText.gameObject.SetActive(false);
            GameManager.Instance.ResetHighlights();
            return;
        }

        float absDiff = Mathf.Abs(diff);
        float fadeStart = 20f;
        float fadeDuration = 150f;
        
        // Рассчитываем желаемую прозрачность (0.0 - 1.0)
        float targetAlpha = Mathf.Clamp01((absDiff - fadeStart) / fadeDuration);

        // Оптимизация: выключаем объект если он совсем прозрачный
        if (targetAlpha <= 0.01f)
        {
            if (actionText.gameObject.activeSelf) actionText.gameObject.SetActive(false);
            GameManager.Instance.ResetHighlights();
            return;
        }

        if (!actionText.gameObject.activeSelf) actionText.gameObject.SetActive(true);

        // 2. Устанавливаем текст
        bool isRight = diff > 0;
        actionText.text = isRight ? CurrentData.rightChoiceText : CurrentData.leftChoiceText;

        // 3. Рассчитываем прогресс выбора
        float progress = absDiff / choiceThreshold;
        float clampedProgress = Mathf.Clamp01(progress);

        // Масштаб
        float targetScale = Mathf.Lerp(minScale, maxScale, clampedProgress);
        if (progress > 1.0f) targetScale += Mathf.Sin(Time.time * 20f) * 0.1f;
        _textRectTransform.localScale = Vector3.one * targetScale;

        // Вращение
        float baseAngle = isRight ? -5f : 5f;
        float currentShake = Mathf.Sin(Time.time * shakeSpeed) * (shakeAngle * clampedProgress);
        _textRectTransform.localRotation = Quaternion.Euler(0, 0, baseAngle + currentShake);

        // 4. ЦВЕТ И АЛЬФА (Исправлено)
        // Сначала выбираем базовый цвет (Белый или Желтый)
        Color finalColor;
        
        if (progress >= 1.0f)
        {
            finalColor = snapColor;
            // Подсветка ресурсов
            if (isRight) GameManager.Instance.HighlightResources(CurrentData.rightCrown, CurrentData.rightChurch, CurrentData.rightMob, CurrentData.rightPlague);
            else GameManager.Instance.HighlightResources(CurrentData.leftCrown, CurrentData.leftChurch, CurrentData.leftMob, CurrentData.leftPlague);
        }
        else
        {
            finalColor = normalColor;
            GameManager.Instance.ResetHighlights();
        }

        // ВАЖНО: Применяем рассчитанную альфу к выбранному цвету
        finalColor.a = targetAlpha;
        
        // Присваиваем цвет тексту ОДИН РАЗ
        actionText.color = finalColor;
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

        // Скрываем текст при выборе
        actionText.gameObject.SetActive(false);

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