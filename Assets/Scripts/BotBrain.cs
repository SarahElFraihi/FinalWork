using UnityEngine;
using System.Collections.Generic;

public class BotBrain : MonoBehaviour
{
    public enum Personality { Aggressive, Defensive, Chaotic }
    
    [Header("Configuration")]
    public Personality personality;
    public PlayerEntity selfEntity;
    public List<CardData> hand = new List<CardData>();
    
    private HandManager hm;

    void Start()
    {
        hm = Object.FindFirstObjectByType<HandManager>();
        selfEntity = GetComponent<PlayerEntity>();
        FillHand();
    }

    public void FillHand()
    {
        if (hm == null) return;
        while (hand.Count < 5)
        {
            hand.Add(hm.allCardsInGame[Random.Range(0, hm.allCardsInGame.Count)]);
        }
    }

    public CardData ChooseCard()
    {
        CardData chosenCard = null;

        // --- LOGIQUE SELON LA PERSONNALITÉ ---
        if (personality == Personality.Aggressive)
        {
            // Cherche si le premier effet est de type Damage
            chosenCard = hand.Find(c => c.type == CardData.CardType.Action && c.effects.Count > 0 && c.effects[0].effectType == CardData.EffectType.Damage);
        }
        else if (personality == Personality.Defensive)
        {
            // Cherche un bouclier ou du soin
            chosenCard = hand.Find(c => c.cardName == "Shield" || (c.effects.Count > 0 && c.effects[0].effectType == CardData.EffectType.Heal));
        }

        // Si pas de carte spécifique trouvée ou si Chaotic, on prend au hasard dans la main
        if (chosenCard == null)
        {
            chosenCard = hand[Random.Range(0, hand.Count)];
        }

        return chosenCard;
    }

    public int ChooseTargetIndex(int myIndex, int totalPlayers)
    {
        int target;
        // Boucle tant que la cible choisie est le bot lui-même
        do {
            target = Random.Range(0, totalPlayers);
        } while (target == myIndex); 
        
        return target;
    }
    
    public void DiscardAndReplace(CardData playedCard)
    {
        hand.Remove(playedCard);
        FillHand();
    }
}