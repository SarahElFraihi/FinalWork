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
    private AICardGenerator aiGenerator; // NOUVEAU : Connexion au générateur d'IA

    void Start()
    {
        hm = Object.FindFirstObjectByType<HandManager>();
        aiGenerator = Object.FindFirstObjectByType<AICardGenerator>(); // On trouve le script Ollama dans la scène
        selfEntity = GetComponent<PlayerEntity>();
        FillHand();
    }

    public void FillHand()
    {
        if (hm == null) return;

        while (hand.Count < 5)
        {
            // === INTÉGRATION POOL IA ===
            // Si le générateur d'IA existe et qu'il a des cartes en stock dans sa Queue
            if (aiGenerator != null && aiGenerator.aiCardPool.Count > 0)
            {
                CardData aiCard = aiGenerator.aiCardPool.Dequeue(); // On pioche la carte de l'IA
                hand.Add(aiCard);
                
                // On demande immédiatement à l'IA de relancer une génération en arrière-plan pour restocker
                aiGenerator.FillPool();
            }
            else
            {
                // === SYSTEME DE SÉCURITÉ (FALLBACK) ===
                // Si Ollama met 1 ou 2 secondes à générer, on ne bloque pas le jeu !
                // Le bot pioche temporairement une carte classique en attendant.
                if (hm.allCardsInGame != null && hm.allCardsInGame.Count > 0)
                {
                    hand.Add(hm.allCardsInGame[Random.Range(0, hm.allCardsInGame.Count)]);
                }
                else
                {
                    break; // Sécurité anti-boucle infinie si le deck est vide
                }
            }
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
            // CORRECTION JURY : Avant, le bot cherchera une carte nommée exactement "Shield".
            // Mais l'IA génère des noms uniques (ex: "Ghostly Protection").
            // On cherche donc par TYPE d'effet (Soin ou Bouclier) plutôt que par le nom écrit !
            chosenCard = hand.Find(c => c.effects.Count > 0 && 
                (c.effects[0].effectType == CardData.EffectType.Heal || 
                 c.effects[0].effectType == (CardData.EffectType)8)); // 8 = l'index du Bouclier dans ton AICardGenerator
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