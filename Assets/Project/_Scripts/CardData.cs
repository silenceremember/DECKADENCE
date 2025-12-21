// CardData.cs

using UnityEngine;

[CreateAssetMenu(fileName = "NewCard", menuName = "Game/Card Data")]
public class CardData : ScriptableObject
{
    [Header("Визуал")]
    public Sprite characterSprite; // Картинка персонажа
    public string characterName;   // Имя (например "Король")
    [TextArea(3, 5)] 
    public string dialogueText;    // Текст проблемы

    [Header("Левый выбор (Нет/Казнить)")]
    public string leftChoiceText;
    // Влияние на ресурсы (Корона, Церковь, Народ, Мор)
    public int leftCrown, leftChurch, leftMob, leftPlague;

    [Header("Правый выбор (Да/Помиловать)")]
    public string rightChoiceText;
    // Влияние на ресурсы
    public int rightCrown, rightChurch, rightMob, rightPlague;
}