using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;

public class CardDisplay : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    [Header("Данные")]
    public CardData currentCard;

    [Header("UI Компоненты")]
    public Image characterImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI questionText;
    public TextMeshProUGUI leftActionText;
    public TextMeshProUGUI rightActionText;

    private RectTransform rectTransform;
    private Canvas canvas;
    private Vector2 startPos; // Начальная позиция (чтобы вернуть карту)
    private Vector3 offset;   // Смещение мыши относительно центра карты

    private float moveLimit = 400f; // Увеличил лимит, чтобы случайно не свайпнуть

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    void Start()
    {
        startPos = rectTransform.anchoredPosition;
        if (currentCard != null) LoadCard(currentCard);
    }

    public void LoadCard(CardData data)
    {
        currentCard = data;
        characterImage.sprite = data.characterSprite;
        nameText.text = data.characterName;
        questionText.text = data.dialogueText;
        
        leftActionText.gameObject.SetActive(false);
        rightActionText.gameObject.SetActive(false);
        
        // Сброс позиции при загрузке новой карты
        rectTransform.anchoredPosition = startPos;
        rectTransform.rotation = Quaternion.identity;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // ---> ДОБАВИТЬ ЭТУ СТРОКУ <---
        // Это мгновенно останавливает все текущие движения (возврат в центр)
        rectTransform.DOKill(); 
        // -----------------------------

        RectTransformUtility.ScreenPointToWorldPointInRectangle(
            rectTransform, 
            eventData.position, 
            eventData.pressEventCamera, 
            out Vector3 worldPoint
        );

        offset = rectTransform.position - worldPoint;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // 1. Получаем текущую позицию мыши в мире
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
            canvas.transform as RectTransform, 
            eventData.position, 
            eventData.pressEventCamera, 
            out Vector3 worldPoint))
        {
            // 2. Применяем позицию с учетом смещения (чтобы не прыгало)
            rectTransform.position = worldPoint + offset;
        }

        // --- Визуал (Поворот и Текст) ---
        
        // Вычисляем, насколько мы сдвинулись от центра (по X)
        float diffX = rectTransform.anchoredPosition.x - startPos.x;

        // Поворот: чем дальше тянем, тем сильнее наклон
        float rotation = -diffX * 0.03f; 
        rectTransform.rotation = Quaternion.Euler(0, 0, rotation);

        // Прозрачность/Показ текста
        if (diffX > 50) 
        {
            rightActionText.gameObject.SetActive(true);
            leftActionText.gameObject.SetActive(false);
            rightActionText.text = currentCard.rightChoiceText;
            
            // Меняем прозрачность текста в зависимости от дальности
            var color = rightActionText.color;
            color.a = Mathf.Clamp01(Mathf.Abs(diffX) / moveLimit);
            rightActionText.color = color;
            GameManager.Instance.HighlightResources(
                currentCard.rightCrown, currentCard.rightChurch, currentCard.rightMob, currentCard.rightPlague
            );
        }
        else if (diffX < -50)
        {
            leftActionText.gameObject.SetActive(true);
            rightActionText.gameObject.SetActive(false);
            leftActionText.text = currentCard.leftChoiceText;
            
            var color = leftActionText.color;
            color.a = Mathf.Clamp01(Mathf.Abs(diffX) / moveLimit);
            leftActionText.color = color;
            GameManager.Instance.HighlightResources(
                currentCard.leftCrown, currentCard.leftChurch, currentCard.leftMob, currentCard.leftPlague
            );
        }
        else
        {
            leftActionText.gameObject.SetActive(false);
            rightActionText.gameObject.SetActive(false);
            GameManager.Instance.ResetHighlights();
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        float diffX = rectTransform.anchoredPosition.x - startPos.x;

        // Если утащили достаточно далеко
        if (Mathf.Abs(diffX) >= moveLimit * 0.5f) // 50% от лимита достаточно для срабатывания
        {
            if (diffX > 0) MakeChoice(true);
            else MakeChoice(false);
        }
        else
        {
            // Возврат назад (анимацию можно добавить позже, пока телепорт)
            ResetCard();
        }
    }

    void ResetCard()
    {
        // Вместо мгновенного телепорта:
        // Двигаем в startPos за 0.5 секунды с эффектом "ElasticOut" (пружина)
        rectTransform.DOAnchorPos(startPos, 0.5f).SetEase(Ease.OutElastic);
        
        // Возвращаем поворот в 0
        rectTransform.DORotate(Vector3.zero, 0.5f).SetEase(Ease.OutBack);
        
        // Скрываем текст
        leftActionText.gameObject.SetActive(false);
        rightActionText.gameObject.SetActive(false);
    }

    void MakeChoice(bool isRight)
    {
        // Отключаем взаимодействие, чтобы игрок не мог дергать карту пока она летит
        GetComponent<CanvasGroup>().blocksRaycasts = false; 

        // Куда полетит карта (далеко вбок и вниз)
        float endX = isRight ? 1500f : -1500f;
        Vector2 flyTarget = new Vector2(endX, -200f);
        
        // Анимация полета
        rectTransform.DOAnchorPos(flyTarget, 0.4f).SetEase(Ease.InBack).OnComplete(() => 
        {
            // Этот код выполнится ТОЛЬКО когда анимация закончится
            
            // 1. Применяем эффекты
             if (isRight) GameManager.Instance.ApplyCardEffect(currentCard.rightCrown, currentCard.rightChurch, currentCard.rightMob, currentCard.rightPlague);
             else GameManager.Instance.ApplyCardEffect(currentCard.leftCrown, currentCard.leftChurch, currentCard.leftMob, currentCard.leftPlague);
            
            // 2. Возвращаем взаимодействие
            GetComponent<CanvasGroup>().blocksRaycasts = true;
            
            // 3. Менеджер сам загрузит новую карту, которая сбросит позицию
        });
    }
}