using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CardDisplay : MonoBehaviour
{
    public CardData cardData; 

    [Header("UI Images References")]
    public Image colorOutline; 
    public Image iconDisplay;  
    public RectTransform visualContent; 

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
            visualContent.anchoredPosition = new Vector2(0, yOffset);
        }
    }

    public void SetVisualState(bool isDimmed)
{
    // 1. On récupère la couleur de base définie dans ton CardData
    Color targetColor = cardData.cardColor;

    // 2. Si la carte doit être grisée/désélectionnée[cite: 4]
    if (isDimmed)
    {
        targetColor.a = 0.25f; // On passe l'alpha à 25% (très transparent)
    }
    else
    {
        targetColor.a = 1f;    // Opacité totale (100%) pour la carte active
    }

    // 3. On applique la couleur avec le bon niveau de transparence
    colorOutline.color = targetColor;
    iconDisplay.color = targetColor;
    nameText.color = targetColor;

    if (descriptionText != null) descriptionText.color = isDimmed ? new Color(1f, 1f, 1f, 0.25f) : Color.white;
}

    public void SelectThisCard()
    {
        HandManager hm = Object.FindFirstObjectByType<HandManager>();
        GameManager gm = Object.FindFirstObjectByType<GameManager>();
        TargetingManager tm = Object.FindFirstObjectByType<TargetingManager>();

        if (gm != null)
        {
            if (hm != null) hm.HighlightSelectedCard(this);
            gm.SelectCard(cardData);

            if (cardData.requiresTarget && tm != null)
            {
                tm.StartTargeting();
            }
            else if (tm != null)
            {
                tm.ResetArrow();
            }
        }
    }
}