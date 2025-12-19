using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class CardJsonData
{
    public string id;
    public string characterName;
    public string dialogueText;
    public string leftChoice;
    public string rightChoice;
    public int[] leftStats; // [Crown, Church, Mob, Plague]
    public int[] rightStats;
}

[System.Serializable]
public class CardCollection
{
    public CardJsonData[] cards;
}

public class CardLoader : MonoBehaviour
{
    public string jsonFileName = "Data/cards"; // Имя файла без расширения в папке Resources
    
    // Этот метод будет вызываться из GameManager
    public List<CardData> LoadCardsFromJson()
    {
        List<CardData> loadedCards = new List<CardData>();

        // 1. Загружаем текст из Resources
        TextAsset jsonText = Resources.Load<TextAsset>(jsonFileName);
        
        if (jsonText == null)
        {
            Debug.LogError("Не найден файл JSON: " + jsonFileName);
            return loadedCards;
        }

        // 2. Парсим текст в объекты
        CardCollection collection = JsonUtility.FromJson<CardCollection>(jsonText.text);

        // 3. Конвертируем JSON-объекты в наши ScriptableObject (CardData)
        foreach (CardJsonData jsonData in collection.cards)
        {
            // Создаем экземпляр ScriptableObject в памяти (он не сохраняется как файл)
            CardData newCard = ScriptableObject.CreateInstance<CardData>();
            
            newCard.name = jsonData.id; // Имя объекта для удобства
            newCard.characterName = jsonData.characterName;
            newCard.dialogueText = jsonData.dialogueText;
            
            newCard.leftChoiceText = jsonData.leftChoice;
            newCard.rightChoiceText = jsonData.rightChoice;

            // Раскидываем статы (безопасно, если массив неполный)
            if (jsonData.leftStats.Length >= 4)
            {
                newCard.leftCrown = jsonData.leftStats[0];
                newCard.leftChurch = jsonData.leftStats[1];
                newCard.leftMob = jsonData.leftStats[2];
                newCard.leftPlague = jsonData.leftStats[3];
            }

            if (jsonData.rightStats.Length >= 4)
            {
                newCard.rightCrown = jsonData.rightStats[0];
                newCard.rightChurch = jsonData.rightStats[1];
                newCard.rightMob = jsonData.rightStats[2];
                newCard.rightPlague = jsonData.rightStats[3];
            }
            
            // ВАЖНО: Спрайты придется грузить отдельно по имени
            // Например: Resources.Load<Sprite>("Sprites/" + jsonData.characterName);
            // Пока оставим пустыми или дефолтными
            
            loadedCards.Add(newCard);
        }

        Debug.Log($"Загружено карт из JSON: {loadedCards.Count}");
        return loadedCards;
    }
}