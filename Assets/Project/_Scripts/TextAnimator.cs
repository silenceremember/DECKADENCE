// TextAnimator.cs
// Royal Leech Text Animation System
// Компактная система для анимации текста с typewriter эффектами

using UnityEngine;
using UnityEngine.Events;
using TMPro;
using System.Collections;

/// <summary>
/// Компонент для анимации текста с несколькими режимами.
/// Поддерживает стандартный typewriter и distance-based анимацию.
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class TextAnimator : MonoBehaviour
{
    public enum AnimationMode
    {
        TimeBasedTypewriter,  // Классический typewriter (время)
        DistanceBased         // Управление извне через SetProgress (расстояние/значение)
    }

    [Header("Animation Mode")]
    public AnimationMode mode = AnimationMode.TimeBasedTypewriter;
    
    [Header("Time-Based Settings")]
    [Tooltip("Количество символов в секунду")]
    public float charactersPerSecond = 30f;
    
    [Tooltip("Задержка перед началом анимации (секунды)")]
    public float delay = 0f;
    
    [Tooltip("Автоматически запустить анимацию при активации")]
    public bool startOnEnable = false;
    
    [Header("Distance-Based Settings")]
    [Tooltip("Скорость интерполяции для плавного появления/исчезновения символов")]
    public float interpolationSpeed = 15f;
    
    [Header("Events")]
    public UnityEvent<char> OnCharacterShown;
    public UnityEvent OnTextComplete;
    
    // Приватные поля
    private TMP_Text _textComponent;
    private string _textToAnimate;
    private Coroutine _typewriterCoroutine;
    private bool _isAnimating;
    
    // Distance-based режим
    private float _currentVisibleCharsFloat = 0f;
    private int _targetCharCount = 0;
    private bool _wasMovingBack = false;
    private int _lastVisibleChars = 0;
    
    /// <summary>
    /// Проверка, идет ли сейчас анимация
    /// </summary>
    public bool IsAnimating => _isAnimating;
    
    /// <summary>
    /// Текущий текст, который анимируется
    /// </summary>
    public string CurrentText => _textToAnimate;
    
    /// <summary>
    /// Текущее количество видимых символов (для distance-based режима)
    /// </summary>
    public int VisibleCharacters => Mathf.FloorToInt(_currentVisibleCharsFloat);

    void Awake()
    {
        _textComponent = GetComponent<TMP_Text>();
    }

    void OnEnable()
    {
        if (mode == AnimationMode.TimeBasedTypewriter && startOnEnable && !string.IsNullOrEmpty(_textToAnimate))
        {
            StartWriter();
        }
    }

    void OnDisable()
    {
        if (mode == AnimationMode.TimeBasedTypewriter)
        {
            StopWriter();
        }
    }

    void Update()
    {
        // Distance-based режим обновляется здесь
        if (mode == AnimationMode.DistanceBased)
        {
            UpdateDistanceBasedAnimation();
        }
    }

    /// <summary>
    /// Установить текст для анимации (не запускает автоматически для time-based)
    /// </summary>
    public void SetText(string text)
    {
        _textToAnimate = text;
        
        if (mode == AnimationMode.TimeBasedTypewriter)
        {
            // Останавливаем текущую анимацию если она идет
            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
                _typewriterCoroutine = null;
                _isAnimating = false;
            }
            
            // Устанавливаем текст в компонент, но скрываем все символы
            _textComponent.text = text;
            _textComponent.maxVisibleCharacters = 0;
        }
        else if (mode == AnimationMode.DistanceBased)
        {
            // Для distance-based сразу показываем текст, но скрываем символы
            _textComponent.text = text;
            _textComponent.maxVisibleCharacters = 0;
            _currentVisibleCharsFloat = 0f;
            _targetCharCount = 0;
            _wasMovingBack = false;
        }
    }

    #region Time-Based Typewriter Mode

    /// <summary>
    /// Запустить анимацию печати текста (time-based режим)
    /// </summary>
    public void StartWriter()
    {
        if (mode != AnimationMode.TimeBasedTypewriter)
        {
            Debug.LogWarning("StartWriter() работает только в режиме TimeBasedTypewriter", this);
            return;
        }

        if (string.IsNullOrEmpty(_textToAnimate))
        {
            Debug.LogWarning("TextAnimator: Нет текста для анимации. Используйте SetText() сначала.", this);
            return;
        }

        if (_typewriterCoroutine != null)
        {
            StopCoroutine(_typewriterCoroutine);
        }

        _typewriterCoroutine = StartCoroutine(TypewriterCoroutine());
    }

    /// <summary>
    /// Остановить анимацию печати
    /// </summary>
    public void StopWriter()
    {
        if (_typewriterCoroutine != null)
        {
            StopCoroutine(_typewriterCoroutine);
            _typewriterCoroutine = null;
            _isAnimating = false;
        }
    }

    /// <summary>
    /// Мгновенно показать весь текст
    /// </summary>
    public void SkipToEnd()
    {
        StopWriter();
        if (_textComponent != null && !string.IsNullOrEmpty(_textToAnimate))
        {
            _textComponent.maxVisibleCharacters = _textToAnimate.Length;
            OnTextComplete?.Invoke();
        }
    }

    /// <summary>
    /// Перезапустить анимацию с начала
    /// </summary>
    public void RestartWriter()
    {
        StopWriter();
        _textComponent.maxVisibleCharacters = 0;
        StartWriter();
    }

    private IEnumerator TypewriterCoroutine()
    {
        _isAnimating = true;
        
        if (delay > 0)
        {
            yield return new WaitForSeconds(delay);
        }

        _textComponent.maxVisibleCharacters = 0;
        _textComponent.ForceMeshUpdate();
        TMP_TextInfo textInfo = _textComponent.textInfo;
        int totalCharacters = textInfo.characterCount;
        
        float timePerCharacter = 1f / charactersPerSecond;
        float timer = 0f;
        int currentVisibleChars = 0;

        while (currentVisibleChars < totalCharacters)
        {
            timer += Time.deltaTime;
            
            while (timer >= timePerCharacter && currentVisibleChars < totalCharacters)
            {
                currentVisibleChars++;
                _textComponent.maxVisibleCharacters = currentVisibleChars;
                timer -= timePerCharacter;
                
                if (currentVisibleChars > 0 && currentVisibleChars <= textInfo.characterCount)
                {
                    TMP_CharacterInfo charInfo = textInfo.characterInfo[currentVisibleChars - 1];
                    char displayedChar = charInfo.character;
                    OnCharacterShown?.Invoke(displayedChar);
                }
            }
            
            yield return null;
        }
        
        _isAnimating = false;
        _typewriterCoroutine = null;
        OnTextComplete?.Invoke();
    }

    #endregion

    #region Distance-Based Mode

    /// <summary>
    /// Установить целевое количество символов (distance-based режим)
    /// Вызывайте каждый кадр с нужным значением
    /// </summary>
    public void SetTargetCharacterCount(int targetCount)
    {
        if (mode != AnimationMode.DistanceBased)
        {
            Debug.LogWarning("SetTargetCharacterCount() работает только в режиме DistanceBased", this);
            return;
        }

        _targetCharCount = Mathf.Clamp(targetCount, 0, _textToAnimate != null ? _textToAnimate.Length : 0);
    }

    /// <summary>
    /// Сбросить прогресс анимации (для distance-based режима)
    /// </summary>
    public void ResetProgress()
    {
        _currentVisibleCharsFloat = 0f;
        _targetCharCount = 0;
        _wasMovingBack = false;
        _lastVisibleChars = 0;
        if (_textComponent != null)
        {
            _textComponent.maxVisibleCharacters = 0;
        }
    }

    private void UpdateDistanceBasedAnimation()
    {
        if (string.IsNullOrEmpty(_textToAnimate)) return;

        int totalChars = _textToAnimate.Length;
        
        // ДИНАМИЧЕСКАЯ ИНТЕРПОЛЯЦИЯ (ВПЕРЁД/НАЗАД)
        if (_targetCharCount > _currentVisibleCharsFloat)
        {
            // ВПЕРЁД
            _wasMovingBack = false;
            
            _currentVisibleCharsFloat = Mathf.MoveTowards(
                _currentVisibleCharsFloat,
                _targetCharCount,
                Time.deltaTime * interpolationSpeed
            );
        }
        else if (_targetCharCount < _currentVisibleCharsFloat)
        {
            // НАЗАД
            // "Грамотная отмена" - срабатывает только в момент СМЕНЫ направления на "назад"
            if (!_wasMovingBack)
            {
                if (_currentVisibleCharsFloat % 1f > 0.001f)
                {
                    _currentVisibleCharsFloat = Mathf.Floor(_currentVisibleCharsFloat);
                }
            }
            
            _wasMovingBack = true;

            _currentVisibleCharsFloat = Mathf.MoveTowards(
                _currentVisibleCharsFloat,
                _targetCharCount,
                Time.deltaTime * interpolationSpeed
            );
        }
        else
        {
            // Стоим на месте
            _wasMovingBack = false;
        }
        
        int visibleChars = Mathf.FloorToInt(_currentVisibleCharsFloat);
        visibleChars = Mathf.Clamp(visibleChars, 0, totalChars);
        
        // Обновляем только если изменилось
        if (visibleChars != _lastVisibleChars)
        {
            _textComponent.maxVisibleCharacters = visibleChars;
            
            // Обновляем отображаемый текст (substring для корректного отображения)
            string displayText = visibleChars > 0 ? _textToAnimate.Substring(0, visibleChars) : "";
            if (_textComponent.text != displayText)
            {
                _textComponent.text = displayText;
            }
            
            // Вызываем событие при появлении нового символа
            if (visibleChars > _lastVisibleChars && visibleChars > 0)
            {
                char newChar = _textToAnimate[visibleChars - 1];
                OnCharacterShown?.Invoke(newChar);
            }
            
            _lastVisibleChars = visibleChars;
            
            // Проверяем завершение
            if (visibleChars >= totalChars && _targetCharCount >= totalChars)
            {
                OnTextComplete?.Invoke();
            }
        }
    }

    #endregion

    /// <summary>
    /// Установить текст и сразу запустить анимацию (time-based)
    /// </summary>
    public void SetTextAndStart(string text)
    {
        SetText(text);
        if (mode == AnimationMode.TimeBasedTypewriter)
        {
            StartWriter();
        }
    }

    #if UNITY_EDITOR
    [Header("Debug Info (Read-only)")]
    [SerializeField] private bool _debugIsAnimating;
    [SerializeField] private int _debugVisibleChars;
    [SerializeField] private int _debugTargetChars;
    
    void LateUpdate()
    {
        if (Application.isPlaying)
        {
            _debugIsAnimating = _isAnimating;
            _debugVisibleChars = _textComponent != null ? _textComponent.maxVisibleCharacters : 0;
            _debugTargetChars = _targetCharCount;
        }
    }
    #endif
}
