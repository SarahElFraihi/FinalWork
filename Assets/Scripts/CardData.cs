using UnityEngine;

[CreateAssetMenu(fileName = "NewCard", menuName = "Game/CardData")]
public class CardData : ScriptableObject
{
    public string cardName;
    [TextArea] public string description;

    public enum CardType { Action, Rule, Special }
    public CardType type;

    [Header("Visual Settings")]
    public Color cardColor; 
    public int duration; 

    [Header("Effect Settings")]
    public int effectValue; 

    [Header("Targeting")]
    public bool needsTarget;

    private void OnValidate()
    {
        switch (type)
        {
            case CardType.Action:
                cardColor = new Color(0.9f, 0.1f, 0.1f); 
                break;
            case CardType.Rule:
                cardColor = new Color(1f, 0.85f, 0f); 
                break;
            case CardType.Special:
                cardColor = new Color(0.6f, 0.1f, 0.9f); 
                break;
        }
    }
}