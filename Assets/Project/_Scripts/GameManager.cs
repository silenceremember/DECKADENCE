using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic; // Нужно для списков (List)

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Настройки Игры")]
    public CardDisplay cardDisplay; // Ссылка на скрипт карты
    public List<CardData> allCards; // Все возможные карты (закинешь их сюда в Инспекторе)
    
    public CardLoader cardLoader; 
    
    // Внутренняя колода (копия), из которой мы будем тянуть
    private List<CardData> _activeDeck; 

    [Header("Ресурсы")]
    public int crown = 50;
    public int church = 50;
    public int mob = 50;
    public int plague = 50;

    [Header("UI")]
    public TextMeshProUGUI crownText;
    public TextMeshProUGUI churchText;
    public TextMeshProUGUI mobText;
    public TextMeshProUGUI plagueText;
    public TextMeshProUGUI dayText; // Текст дня (создай его в Canvas!)
    
    [Header("UI Иконки")]
    public Image crownIcon;
    public Image churchIcon;
    public Image mobIcon;
    public Image plagueIcon;
    
    // Цвета для подсказок
    public Color neutralColor = Color.white;
    public Color activeColor = Color.yellow; 

    private int _currentDay = 1;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Если есть загрузчик, грузим карты из JSON
        if (cardLoader != null)
        {
            var jsonCards = cardLoader.LoadCardsFromJson();
            // Добавляем загруженные карты к тем, что уже были (если были)
            allCards.AddRange(jsonCards);
        }

        _activeDeck = new List<CardData>(allCards);
        
        UpdateUI();
        LoadNewCard();
    }

    public void ApplyCardEffect(int dCrown, int dChurch, int dMob, int dPlague)
    {
        // 1. Меняем ресурсы
        crown = Mathf.Clamp(crown + dCrown, 0, 100);
        church = Mathf.Clamp(church + dChurch, 0, 100);
        mob = Mathf.Clamp(mob + dMob, 0, 100);
        plague = Mathf.Clamp(plague + dPlague, 0, 100);

        // 2. Проверяем смерть (пока просто лог)
        if (CheckGameOver()) return;

        // 3. Следующий день
        _currentDay++;
        UpdateUI();

        // 4. Грузим новую карту
        LoadNewCard();
    }

    void LoadNewCard()
    {
        if (_activeDeck.Count == 0)
        {
            Debug.Log("Колода кончилась! Победа или Решаффл.");
            // Тут можно сделать экран победы
            return;
        }

        // Берем случайную карту
        int randomIndex = Random.Range(0, _activeDeck.Count);
        CardData newCard = _activeDeck[randomIndex];
        
        // Удаляем её из активной колоды, чтобы не повторялась сразу
        _activeDeck.RemoveAt(randomIndex);

        // Передаем данные в дисплей
        cardDisplay.LoadCard(newCard);
    }

    bool CheckGameOver()
    {
        if (crown <= 0 || crown >= 100) { Debug.Log("Смерть от Короля"); return true; }
        if (church <= 0 || church >= 100) { Debug.Log("Смерть от Церкви"); return true; }
        if (mob <= 0 || mob >= 100) { Debug.Log("Смерть от Толпы"); return true; }
        if (plague >= 100) { Debug.Log("Смерть от Чумы"); return true; } // От чумы умирают только при максимуме
        return false;
    }

    void UpdateUI()
    {
        crownText.text = crown.ToString();
        churchText.text = church.ToString();
        mobText.text = mob.ToString();
        plagueText.text = plague.ToString();
        
        if(dayText != null) dayText.text = "День " + _currentDay;
    }
    
    public void HighlightResources(int dCrown, int dChurch, int dMob, int dPlague)
    {
        crownIcon.color = (dCrown != 0) ? activeColor : neutralColor;
        churchIcon.color = (dChurch != 0) ? activeColor : neutralColor;
        mobIcon.color = (dMob != 0) ? activeColor : neutralColor;
        plagueIcon.color = (dPlague != 0) ? activeColor : neutralColor;
    }

    public void ResetHighlights()
    {
        crownIcon.color = neutralColor;
        churchIcon.color = neutralColor;
        mobIcon.color = neutralColor;
        plagueIcon.color = neutralColor;
    }
}