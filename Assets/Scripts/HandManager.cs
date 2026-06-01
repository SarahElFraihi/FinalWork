using UnityEngine;
using System.Collections.Generic;

public class HandManager : MonoBehaviour
{
    [Header("Setup")]
    public List<CardData> allCardsInGame; 
    public List<CardDisplay> cardSlots; 

    void Start()
    {
        // Plus d'attente, on remplit la main immédiatement au lancement
        GenerateRandomHand();
    }

    public void GenerateRandomHand()
    {
        AICardGenerator aiGen = Object.FindFirstObjectByType<AICardGenerator>();

        foreach (CardDisplay slot in cardSlots)
        {
            if (aiGen != null && aiGen.aiCardPool.Count > 0)
            {
                slot.LoadCard(aiGen.aiCardPool.Dequeue());
                aiGen.FillPool(); 
            }
           else 
            {
                CardData baseCard = GetWeightedRandomBaseCard();
                if (baseCard != null) slot.LoadCard(baseCard);
            }
        }
    }

    public void RefillHand()
    {
        GameManager gm = Object.FindFirstObjectByType<GameManager>();
        AICardGenerator aiGen = Object.FindFirstObjectByType<AICardGenerator>();

        if (gm == null || aiGen == null) return;

        foreach (CardDisplay slot in cardSlots)
        {
            // On vérifie si ce slot est celui qui a été vidé (comparaison de la donnée de carte)
            if (slot.cardData == gm.selectedCard)
            {
                Debug.Log("<color=yellow>[HandManager]</color> Slot vide trouvé. Tentative de pioche IA...");

                if (aiGen.aiCardPool.Count > 0)
                {
                    CardData generatedCard = aiGen.aiCardPool.Dequeue(); 
                    slot.LoadCard(generatedCard);
                    Debug.Log("<color=green>[HandManager]</color> Succès : Carte IA insérée !");
                    aiGen.FillPool(); 
                }
                else 
                {
                    Debug.LogWarning("<color=red>[HandManager]</color> Stock IA vide ! Utilisation d'une carte manuelle pondérée.");
                    CardData baseCard = GetWeightedRandomBaseCard();
                    if (baseCard != null) slot.LoadCard(baseCard);
                }
            }
        }
    }

    public void ResetAllCardsVisuals()
    {
        foreach (CardDisplay slot in cardSlots) { slot.SetVisualState(false); slot.SetYOffset(0); }
    }

    public void HighlightSelectedCard(CardDisplay selectedSlot)
    {
        foreach (CardDisplay slot in cardSlots)
        {
            if (slot == selectedSlot) { slot.SetVisualState(false); slot.SetYOffset(0); }
            else { slot.SetVisualState(true); slot.SetYOffset(-40f); }
        }
    }

    public void AnimateSideSlide()
    {
        StartCoroutine(SideSlideRoutine());
    }

    System.Collections.IEnumerator SideSlideRoutine()
    {
        // 1. On décale d'abord toutes les cartes sur le côté gauche et invisibles
        foreach (CardDisplay slot in cardSlots)
        {
            if (slot != null && slot.visualContent != null)
            {
                slot.visualContent.anchoredPosition = new Vector2(-400f, 0f); 
                slot.visualContent.localScale = Vector3.zero; 
            }
        }

        // 2. On les fait défiler l'une après l'autre de gauche à droite
        foreach (CardDisplay slot in cardSlots)
        {
            if (slot != null && slot.visualContent != null)
            {
                slot.SetVisualState(false);
                StartCoroutine(SmoothSideSlideCard(slot));
                
                // Petit délai pour l'effet cascade/défilement
                yield return new WaitForSeconds(0.1f); 
            }
        }
    }

    System.Collections.IEnumerator SmoothSideSlideCard(CardDisplay slot)
    {
        float time = 0f;
        float duration = 0.4f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            t = Mathf.Sin(t * Mathf.PI * 0.5f); 

            if (slot != null && slot.visualContent != null)
            {
                // Glisse horizontale de -400 à 0
                slot.visualContent.anchoredPosition = new Vector2(Mathf.Lerp(-400f, 0f, t), 0f);
                slot.visualContent.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, t);
            }
            yield return null;
        }

        if (slot != null && slot.visualContent != null)
        {
            slot.visualContent.anchoredPosition = Vector2.zero;
            slot.visualContent.localScale = Vector3.one;
        }
    }

    private CardData GetWeightedRandomBaseCard()
    {
        if (allCardsInGame == null || allCardsInGame.Count == 0) return null;

        // 1. On trie tes cartes de base par catégorie
        List<CardData> actionCards = allCardsInGame.FindAll(c => c.type == CardData.CardType.Action);
        List<CardData> ruleCards = allCardsInGame.FindAll(c => c.type == CardData.CardType.Rule);
        List<CardData> specialCards = allCardsInGame.FindAll(c => c.type == CardData.CardType.Special);

        float roll = Random.Range(0f, 100f);

        if (roll < 60f && actionCards.Count > 0)
            return actionCards[Random.Range(0, actionCards.Count)];
        
        else if (roll < 80f && ruleCards.Count > 0)
            return ruleCards[Random.Range(0, ruleCards.Count)];
        
        else if (specialCards.Count > 0)
            return specialCards[Random.Range(0, specialCards.Count)];

        return allCardsInGame[Random.Range(0, allCardsInGame.Count)];
    }
}