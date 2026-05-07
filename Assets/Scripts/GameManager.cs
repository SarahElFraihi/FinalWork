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

        // 1. SÉCURITÉ : CHOIX ALÉATOIRE JOUEUR
        if (selectedCard == null)
        {
            HandManager hm = Object.FindFirstObjectByType<HandManager>();
            if (hm != null) selectedCard = hm.GetRandomCardFromHand().cardData;
        }

        allActionsThisTurn.Clear();

        // 2. ON PRÉPARE LA LISTE DES PARTICIPANTS
        List<PlayerEntity> allParticipants = new List<PlayerEntity> { playerEntity };
        allParticipants.AddRange(botEntities);

        // 3. UNE SEULE BOUCLE POUR TOUT LE MONDE
        foreach (PlayerEntity performer in allParticipants)
        {
            TurnAction action = new TurnAction();
            action.performer = performer;

            // On récupère la carte (celle choisie pour toi, celle du cerveau pour le bot)
            if (performer.isBot)
                action.card = performer.GetComponent<BotBrain>().ChooseCard();
            else
                action.card = selectedCard;

            if (action.card == null) continue;

            // 4. CALCUL DE LA CIBLE SELON LE MODE
            switch (action.card.targetMode)
            {
                case CardData.TargetMode.None:
                case CardData.TargetMode.Everyone:
                    action.target = null; // Pas de cible unique nécessaire
                    break;

                case CardData.TargetMode.Self:
                    action.target = performer;
                    break;

                case CardData.TargetMode.Chosen:
                    if (!performer.isBot && tm != null)
                        action.target = botEntities[tm.currentTargetIndex];
                    else if (performer.isBot)
                    {
                        int myIdx = allParticipants.IndexOf(performer);
                        int targetIdx = performer.GetComponent<BotBrain>().ChooseTargetIndex(myIdx, allParticipants.Count);
                        action.target = allParticipants[targetIdx];
                    }
                    break;

                case CardData.TargetMode.Left:
                    action.target = performer.leftNeighbor;
                    break;

                case CardData.TargetMode.Right:
                    action.target = performer.rightNeighbor;
                    break;

                case CardData.TargetMode.Opposite:
                    action.target = performer.oppositePlayer;
                    break;
            }

            allActionsThisTurn.Add(action);

            // Si c'est un bot, il jette sa carte pour en piocher une nouvelle
            if (performer.isBot) performer.GetComponent<BotBrain>().DiscardAndReplace(action.card);
        }

        ProcessAllCards();
    }

    void ProcessAllCards()
    {
        if (resolutionPanel != null) resolutionPanel.SetActive(true);
        string finalResults = "<b>ROUND RESULTS</b>\n\n";

        // 1. On crée la liste de toutes les cartes jouées
        List<CardData> allPlayedCards = new List<CardData>();
        
        // CORRECTIF : On utilise UNIQUEMENT allActionsThisTurn qui contient déjà 
        // le joueur ET les bots sans exception.
        foreach (TurnAction action in allActionsThisTurn)
        {
            if (action.card != null) 
                allPlayedCards.Add(action.card);
        }

        // 2. Tri des cartes par type
        List<CardData> ruleCards = allPlayedCards.FindAll(c => c.type == CardData.CardType.Rule);
        List<CardData> actionCards = allPlayedCards.FindAll(c => c.type == CardData.CardType.Action);
        List<CardData> specialCards = allPlayedCards.FindAll(c => c.type == CardData.CardType.Special);

        // 3. Exécution
        if (rule_GravityFlip)
        {
            finalResults += ExecuteSpecialCards(specialCards);
            finalResults += ExecuteActionCards(allActionsThisTurn); // Utilise les actions complètes
            finalResults += ExecuteRuleCards(ruleCards);
        }
        else
        {
            finalResults += ExecuteRuleCards(ruleCards);
            finalResults += ExecuteActionCards(allActionsThisTurn); // Utilise les actions complètes
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
        
        // On prépare la liste de tous les participants pour les effets de zone
        List<PlayerEntity> everyone = new List<PlayerEntity> { playerEntity };
        everyone.AddRange(botEntities);

        foreach (TurnAction act in actions)
        {
            // On ignore ce qui n'est pas une action
            if (act.card == null || act.card.type != CardData.CardType.Action) continue;

            string name = act.card.cardName.Trim();

            // 1. CAS : TOUT LE MONDE (AOE)
            if (act.card.targetMode == CardData.TargetMode.Everyone)
            {
                foreach (PlayerEntity p in everyone)
                {
                    ApplyCardValue(act.card.effectValue, p);
                }
                log += $"<color=#E61A1A>ACTION:</color> {act.performer.playerName} plays {name} on EVERYONE\n";
            }
            // 2. CAS : CIBLE UNIQUE (OU SOI-MÊME)
            else if (act.target != null)
            {
                if (name == "Shield") act.target.isShielded = true;
                else 
                {
                    ApplyCardValue(act.card.effectValue, act.target);
                }
                
                // Log intelligent selon si la flèche était utilisée ou non
                if (act.card.requiresTarget)
                    log += $"<color=#E61A1A>ACTION:</color> {act.performer.playerName} plays {name} on {act.target.playerName}\n";
                else
                    log += $"<color=#E61A1A>ACTION:</color> {act.performer.playerName} plays {name}\n";
            }
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