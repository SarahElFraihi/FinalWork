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
            // PRIORITÉ : On pioche dans le stock IA s'il y a quelque chose
            if (aiGen != null && aiGen.aiCardPool.Count > 0)
            {
                slot.LoadCard(aiGen.aiCardPool.Dequeue());
                aiGen.FillPool(); 
            }
            else // FALLBACK : Tes cartes créées si le stock IA est vide
            {
                if (allCardsInGame.Count > 0)
                {
                    int randomIndex = Random.Range(0, allCardsInGame.Count);
                    slot.LoadCard(allCardsInGame[randomIndex]);
                }
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
                    Debug.LogWarning("<color=red>[HandManager]</color> Stock IA vide ! Utilisation d'une carte manuelle.");
                    if (allCardsInGame.Count > 0)
                    {
                        int randomIndex = Random.Range(0, allCardsInGame.Count);
                        slot.LoadCard(allCardsInGame[randomIndex]);
                    }
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
}