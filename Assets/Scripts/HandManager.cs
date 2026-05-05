using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI; 

public class HandManager : MonoBehaviour
{
    [Header("Setup")]
    public List<CardData> allCardsInGame; 
    public List<CardDisplay> cardSlots; 

    void Start()
    {
        GenerateRandomHand();
    }

    public void GenerateRandomHand()
    {
        foreach (CardDisplay slot in cardSlots)
        {
            if (allCardsInGame.Count > 0)
            {
                int randomIndex = Random.Range(0, allCardsInGame.Count);
                slot.LoadCard(allCardsInGame[randomIndex]);
            }
        }
    }

    public CardDisplay GetRandomCardFromHand()
    {
        int randomIndex = Random.Range(0, cardSlots.Count);
        return cardSlots[randomIndex];
    }

    public void ResetAllCardsVisuals()
    {
        foreach (CardDisplay slot in cardSlots)
        {
            // On ne passe plus qu'un seul argument : false (car on ne veut pas que ce soit grisé)
            slot.SetVisualState(false); 
            slot.SetYOffset(0); 
        }
    }

    public void HighlightSelectedCard(CardDisplay selectedSlot)
    {
        foreach (CardDisplay slot in cardSlots)
        {
            if (slot == selectedSlot)
            {
                // La carte choisie n'est PAS grisée
                slot.SetVisualState(false); 
                slot.SetYOffset(0);
            }
            else
            {
                // Les autres SONT grisées
                slot.SetVisualState(true); 
                slot.SetYOffset(-40f); 
            }
        }
    }

    public void RefillHand()
    {
        GameManager gm = Object.FindFirstObjectByType<GameManager>();

        foreach (CardDisplay slot in cardSlots)
        {
            // On ne remplace que la carte qui a été jouée
            if (gm != null && slot.cardData == gm.selectedCard)
            {
                int randomIndex = Random.Range(0, allCardsInGame.Count);
                slot.LoadCard(allCardsInGame[randomIndex]);
            }
        }
    }
}