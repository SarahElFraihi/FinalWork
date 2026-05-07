using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI timerText; 
    public Image healthLiquidImage;
    public GameObject resolutionPanel; 
    public TextMeshProUGUI resultsText; 

    [Header("Player Stats")]
    public int maxHealth = 100;
    public int currentHealth = 100;

    [Header("Timer Settings")]
    public float timeLeft = 15f; 
    public bool timerRunning = false;
    public bool isResolutionPhase = false;
    
    [Header("State Settings")]
    public float baseTimerDuration = 15f; 
    public float nextRoundTimerDuration = 15f; 
    public bool rule_GravityFlip = false; 
    public bool rule_HealingStrikes = false; 
    public bool isShielded = false;
    public bool isMirrorShielded = false;

    [Header("History")]
    public CardData lastPlayedCard;

    [Header("Selections")]
    public CardData selectedCard; 
    // On remplace la liste de décisions par une liste d'actions globale
    public List<TurnAction> allActionsThisTurn = new List<TurnAction>();

    [Header("Entities")]
    public PlayerEntity playerEntity; 
    public List<PlayerEntity> botEntities;
    
    [System.Serializable]
    public class TurnAction
    {
        public PlayerEntity performer; // Celui qui joue
        public CardData card;          // La carte jouée
        public PlayerEntity target;    // La cible
    }

    void Start()
    {
        if (healthLiquidImage != null)
    {
        healthLiquidImage.fillAmount = (float)currentHealth / maxHealth;
    }
        if (resolutionPanel != null) resolutionPanel.SetActive(false);
        StartTimer();
    }

    void Update()
    {
        if (timerRunning)
        {
            if (timeLeft > 0)
            {
                timeLeft -= Time.deltaTime;
                UpdateTimerUI();
            }
            else
            {
                timeLeft = 0;
                timerRunning = false;
                ResolveTurn(); 
            }
        }
    }

    public void StartTimer()
    {
        // --- ÉTAPE 1 : RÉINITIALISER LA FLÈCHE (SÉCURITÉ) ---
        TargetingManager tm = Object.FindFirstObjectByType<TargetingManager>();
        if (tm != null) tm.ResetArrow();

        // SAUVEGARDE DE L'HISTORIQUE
        if (selectedCard != null) 
        {
            lastPlayedCard = selectedCard; 
        }

        timeLeft = nextRoundTimerDuration;
        nextRoundTimerDuration = baseTimerDuration; 
        
        isShielded = false;
        isMirrorShielded = false;
        rule_HealingStrikes = false; 

        timerRunning = true;
        isResolutionPhase = false; 
        selectedCard = null; 

        if (resolutionPanel != null) resolutionPanel.SetActive(false);

        // RÉINITIALISATION VISUELLE DES CARTES
        HandManager hm = Object.FindFirstObjectByType<HandManager>();
        if (hm != null) hm.ResetAllCardsVisuals(); 
    }

    void UpdateTimerUI()
    {
        timerText.text = Mathf.Ceil(timeLeft).ToString();
    }

    void ResolveTurn()
    {

        TargetingManager tm = Object.FindFirstObjectByType<TargetingManager>();
        if (tm != null) tm.ResetArrow();

    // --- SÉCURITÉ : CHOIX ALÉATOIRE SI LE JOUEUR N'A RIEN CHOISI ---
        if (selectedCard == null)
        {
            HandManager hm = Object.FindFirstObjectByType<HandManager>();
            if (hm != null)
            {
                // On récupère une carte au hasard dans la main et on l'assigne
                selectedCard = hm.GetRandomCardFromHand().cardData;
                Debug.Log("Temps écoulé ! Sélection auto : " + selectedCard.cardName);
            }
        }
        // --------------------------------------------------------------

        // ... reset flèche etc ...
        allActionsThisTurn.Clear();

        // 1. ACTION DU JOUEUR
        TurnAction playerAct = new TurnAction();
        playerAct.performer = playerEntity;
        playerAct.card = selectedCard;
        
        // On ne cherche une cible que si c'est nécessaire
        if (selectedCard != null && selectedCard.requiresTarget && tm != null)
            playerAct.target = botEntities[tm.currentTargetIndex];
        else
            playerAct.target = playerEntity; // Cible soi-même par défaut (ex: Shield)
        
        allActionsThisTurn.Add(playerAct);

        // 2. ACTIONS DES BOTS
        List<PlayerEntity> allParticipants = new List<PlayerEntity> { playerEntity };
        allParticipants.AddRange(botEntities);

        foreach (PlayerEntity bot in botEntities)
        {
            BotBrain brain = bot.GetComponent<BotBrain>();
            if (brain != null)
            {
                TurnAction botAct = new TurnAction();
                botAct.performer = bot;
                botAct.card = brain.ChooseCard();
                
                // CORRECTION : On ne calcule une cible que si la carte le demande
                if (botAct.card != null && botAct.card.requiresTarget)
                {
                    int myIdx = allParticipants.IndexOf(bot);
                    botAct.target = allParticipants[brain.ChooseTargetIndex(myIdx, allParticipants.Count)];
                }
                else
                {
                    botAct.target = bot; // Pour les cartes de règles ou bonus personnels
                }

                allActionsThisTurn.Add(botAct);
                brain.DiscardAndReplace(botAct.card);
            }
        }
        ProcessAllCards();
    }

    void ProcessAllCards()
    {
        if (resolutionPanel != null) resolutionPanel.SetActive(true);
        string finalResults = "<b>ROUND RESULTS</b>\n\n";

        // 1. On crée la liste de toutes les cartes jouées ce tour-ci
        List<CardData> allPlayedCards = new List<CardData>();
        
        // On parcourt allActionsThisTurn qui contient maintenant les actions du joueur ET des bots
        foreach (TurnAction action in allActionsThisTurn)
        {
            if (action.card != null) 
                allPlayedCards.Add(action.card);
        }

        // 2. Tri des cartes par type pour la résolution (ne change pas)
        List<CardData> ruleCards = allPlayedCards.FindAll(c => c.type == CardData.CardType.Rule);
        List<CardData> actionCards = allPlayedCards.FindAll(c => c.type == CardData.CardType.Action);
        List<CardData> specialCards = allPlayedCards.FindAll(c => c.type == CardData.CardType.Special);

        // 3. Ordre de résolution selon les règles actives
        if (rule_GravityFlip)
        {
            finalResults += ExecuteSpecialCards(specialCards);
            finalResults += ExecuteActionCards(allActionsThisTurn);
            finalResults += ExecuteRuleCards(ruleCards);
        }
        else
        {
            finalResults += ExecuteRuleCards(ruleCards);
            finalResults += ExecuteActionCards(allActionsThisTurn);
            finalResults += ExecuteSpecialCards(specialCards);
        }

        if (resultsText != null) resultsText.text = finalResults;
        
        HandManager hm = Object.FindFirstObjectByType<HandManager>();
        if (hm != null) hm.RefillHand();
    }

    string ExecuteRuleCards(List<CardData> cards)
    {
        string log = "";
        foreach (CardData card in cards)
        {
            string name = card.cardName.Trim();
            if (name == "Speed Dating") 
            {
                baseTimerDuration = 5f; 
                nextRoundTimerDuration = 5f;
            }
            else if (name == "Turbo Mode") nextRoundTimerDuration = 5f;
            else if (name == "Gravity Flip") rule_GravityFlip = !rule_GravityFlip; 
            else if (name == "Healing Strikes") rule_HealingStrikes = true;
            
            log += $"<color=#FFD700>RULE:</color> {name}\n";
        }
        return log;
    }

    string ExecuteActionCards(List<TurnAction> actions)
    {
        string log = "";
        foreach (TurnAction act in actions)
        {
            // --- FIX : On ignore les cartes qui ne sont pas de type ACTION ---
            if (act.card == null || act.card.type != CardData.CardType.Action) continue;

            string name = act.card.cardName.Trim();
            
            if (name == "Shield") act.target.isShielded = true;
            else 
            {
                ApplyCardValue(act.card.effectValue, act.target);
            }
            
            // Affichage intelligent du log
            if (act.card.requiresTarget)
                log += $"<color=#E61A1A>ACTION:</color> {act.performer.playerName} plays {name} on {act.target.playerName}\n";
            else
                log += $"<color=#E61A1A>ACTION:</color> {act.performer.playerName} plays {name}\n";
        }
        return log;
    }

    string ExecuteSpecialCards(List<CardData> cards)
    {
        string log = "";
        foreach (CardData card in cards)
        {
            string name = card.cardName.Trim();
            if (name == "Glitch")
            {
                HandManager hm = Object.FindFirstObjectByType<HandManager>();
                if (hm != null) hm.GenerateRandomHand(); 
            }
            
            log += $"<color=#991AE6>SPECIAL:</color> {name}\n";
        }
        return log;
    }

    void ApplyCardValue(int value, PlayerEntity target)
    {
        if (value == 0 || target == null) return; 
        
        // On appelle TakeDamage sur la cible (Bot ou Joueur)
        target.TakeDamage(value); 

        // SI LA CIBLE EST LE JOUEUR : on met aussi à jour les variables de l'UI
        if (!target.isBot)
        {
            currentHealth = target.currentHealth;
            if (healthLiquidImage != null) 
                healthLiquidImage.fillAmount = (float)currentHealth / maxHealth;
        }
    }

    public void SelectCard(CardData data)
    {
        if (!isResolutionPhase) selectedCard = data;
    }
}