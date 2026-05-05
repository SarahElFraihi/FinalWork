using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CardDisplay : MonoBehaviour
{
    public CardData cardData; 

    [Header("UI Images References")]
    public Image colorOutline; 
    public Image iconDisplay;  
    public RectTransform visualContent; // L'objet qui va bouger

    [Header("Icons Sprites")]
    public Sprite actionSprite;
    public Sprite ruleSprite;
    public Sprite eventSprite;

    [Header("Texts")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;

    public void LoadCard(CardData data)
    {
        if (data == null) return;
        cardData = data;
        
        nameText.text = cardData.cardName;
        descriptionText.text = cardData.description;
        nameText.color = cardData.cardColor; 
        colorOutline.color = cardData.cardColor;
        iconDisplay.color = cardData.cardColor;

        // LOGIQUE ESSENTIELLE : Choix de l'icône
        switch (cardData.type)
        {
            case CardData.CardType.Action: iconDisplay.sprite = actionSprite; break;
            case CardData.CardType.Rule: iconDisplay.sprite = ruleSprite; break;
            case CardData.CardType.Special: iconDisplay.sprite = eventSprite; break;
        }
    }

    public void SetYOffset(float yOffset)
    {
        if (visualContent != null)
        {
            // On déplace le contenu localement pour ne pas casser le Layout
            visualContent.anchoredPosition = new Vector2(0, yOffset);
        }
    }

    public void SetVisualState(bool isDimmed)
    {
        Color targetColor = isDimmed ? new Color(0.2f, 0.2f, 0.2f, 1f) : cardData.cardColor;
        colorOutline.color = targetColor;
        iconDisplay.color = targetColor;
        nameText.color = targetColor;
    }

    public void SelectThisCard()
    {
        HandManager hm = Object.FindFirstObjectByType<HandManager>();
        GameManager gm = Object.FindFirstObjectByType<GameManager>();
        if (gm != null && !gm.isResolutionPhase)
        {
            if (hm != null) hm.HighlightSelectedCard(this);
            gm.SelectCard(cardData);
        }
    }
}