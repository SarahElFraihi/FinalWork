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
    public TextMeshProUGUI goldText;

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

    [Header("Rule Engine")]
    // Liste de tous les effets de règles actuellement en vigueur
    public List<CardData.CardEffect> activeRules = new List<CardData.CardEffect>();

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
        UpdateGoldUI();
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
        TargetingManager tm = Object.FindFirstObjectByType<TargetingManager>();
        if (tm != null) tm.ResetArrow();

        if (selectedCard != null) lastPlayedCard = selectedCard; 

        timeLeft = nextRoundTimerDuration;

        // --- LOGIQUE DE PERSISTENCE ---

        timerRunning = true;
        isResolutionPhase = false; 
        selectedCard = null; 

        if (resolutionPanel != null) resolutionPanel.SetActive(false);

        HandManager hm = Object.FindFirstObjectByType<HandManager>();
        if (hm != null) hm.ResetAllCardsVisuals(); 

        ApplyPassiveRules();
    }
    void UpdateTimerUI()
    {
        timerText.text = Mathf.Ceil(timeLeft).ToString();
    }

    void ResolveTurn()
    {
        TargetingManager tm = Object.FindFirstObjectByType<TargetingManager>();
        if (tm != null) tm.ResetArrow();

        allActionsThisTurn.Clear();
        List<PlayerEntity> allParticipants = new List<PlayerEntity> { playerEntity };
        allParticipants.AddRange(botEntities);

        foreach (PlayerEntity performer in allParticipants)
        {
            // --- 1. LOI : FROZEN (GELÉ) ---
            if (performer.isFrozen)
            {
                Debug.Log(performer.playerName + " is frozen and skips their turn!");
                performer.isFrozen = false; // On le dégèle pour le tour suivant
                continue; // On passe au joueur suivant sans ajouter d'action
            }

            TurnAction action = new TurnAction();
            action.performer = performer;

            if (performer.isBot)
                action.card = performer.GetComponent<BotBrain>().ChooseCard();
            else
                action.card = selectedCard;

            if (action.card == null) continue;

            // --- 2. LOI : SILENCED (SILENCE) ---
            // Si le joueur est silencieux, il ne peut pas jouer de cartes Special ou Rule
            if (performer.isSilenced && action.card.type != CardData.CardType.Action)
            {
                Debug.Log(performer.playerName + " is silenced and can't use Special/Rule cards!");
                performer.isSilenced = false; 
                continue;
            }

            // --- 3. LOI : CONFUSED (CONFUS) ---
            // On change la cible pour une cible aléatoire
            CardData.TargetMode effectiveTargetMode = action.card.targetMode;
            if (performer.isConfused)
            {
                effectiveTargetMode = CardData.TargetMode.Chosen; // On force un choix aléatoire
                performer.isConfused = false;
            }

            // --- CALCUL DE LA CIBLE ---
            switch (effectiveTargetMode)
            {
                case CardData.TargetMode.Self: action.target = performer; break;
                case CardData.TargetMode.Chosen:
                    // --- 4. LOI : INVISIBLE ---
                    // On s'assure que la cible n'est pas invisible (sauf si c'est un bot qui choisit mal volontairement)
                    if (!performer.isBot && tm != null)
                        action.target = botEntities[tm.currentTargetIndex];
                    else
                    {
                        // Pour les bots, on cherche une cible qui n'est pas invisible
                        List<PlayerEntity> potentialTargets = allParticipants.FindAll(p => p != performer && !p.isInvisible);
                        if (potentialTargets.Count > 0)
                            action.target = potentialTargets[Random.Range(0, potentialTargets.Count)];
                        else
                            action.target = allParticipants[Random.Range(0, allParticipants.Count)];
                    }
                    break;
                // ... garde les autres cases (Left, Right, etc.) ...
            }

            allActionsThisTurn.Add(action);
            if (performer.isBot) performer.GetComponent<BotBrain>().DiscardAndReplace(action.card);
        }

        ProcessAllCards();
    }

    void ResolveEffect(CardData.CardEffect effect, PlayerEntity target, PlayerEntity performer)
    {
        if (target == null || performer == null) return;

        switch (effect.effectType)
        {
            // --- Stats Numériques ---
            case CardData.EffectType.Damage:
                int dmg = (int)effect.value;
                // LOGIQUE "HEALING STRIKES" : Si une règle de soin active existe, on soigne au lieu de blesser
                if (activeRules.Exists(r => r.effectType == CardData.EffectType.Heal)) target.TakeDamage(dmg);
                else target.TakeDamage(-dmg);
                break;
            case CardData.EffectType.Heal: target.TakeDamage((int)effect.value); break;
            case CardData.EffectType.Gold: target.gold += (int)effect.value; break;
            case CardData.EffectType.Karma: target.karma += (int)effect.value; break;
            case CardData.EffectType.Luck: target.luck += (int)effect.value; break;
            
            // --- États & Bools (On les active simplement)
            case CardData.EffectType.Frozen: target.isFrozen = true; break;
            case CardData.EffectType.Burn: target.isOnFire = true; break;
            case CardData.EffectType.Poison: target.isPoisoned = true; break;
            case CardData.EffectType.Shield: target.isShielded = true; break;
            case CardData.EffectType.Invisible: target.isInvisible = true; break;
            case CardData.EffectType.Wanted: target.isWanted = true; break;
            case CardData.EffectType.Silenced: target.isSilenced = true; break;
            case CardData.EffectType.Linked: target.isLinked = true; break;

            // --- Modificateurs de jeu ---
            case CardData.EffectType.Thorns: target.thorns += (int)effect.value; break;
            case CardData.EffectType.HandSize: target.handSize = (int)effect.value; break;
            case CardData.EffectType.TimerMod: // Changement immédiat du timer
                baseTimerDuration = effect.value;
                nextRoundTimerDuration = effect.value;
                break;
            case CardData.EffectType.GravityFlip: rule_GravityFlip = !rule_GravityFlip; break;

            // --- Actions Complexes ---
            case CardData.EffectType.StealCard:
                if (target.isBot) {
                    BotBrain brain = target.GetComponent<BotBrain>();
                    if (brain != null && brain.hand.Count > 0) brain.hand.RemoveAt(Random.Range(0, brain.hand.Count));
                }
                break;
        }

        if (performer == playerEntity) UpdateGoldUI();
    }

    public void UpdateGoldUI()
    {
        // On vérifie si tu as glissé un objet texte dans l'inspecteur
        if (goldText != null && playerEntity != null)
        {
            goldText.text = playerEntity.gold.ToString();
        }
    }

    void ProcessAllCards()
    {
        if (resolutionPanel != null) resolutionPanel.SetActive(true);
        string finalResults = "<b>ROUND RESULTS</b>\n\n";

        // Tri des actions par type de carte
        List<TurnAction> ruleActions = allActionsThisTurn.FindAll(a => a.card.type == CardData.CardType.Rule);
        List<TurnAction> actionActions = allActionsThisTurn.FindAll(a => a.card.type == CardData.CardType.Action);
        List<TurnAction> specialActions = allActionsThisTurn.FindAll(a => a.card.type == CardData.CardType.Special);

        // Ordre d'exécution respectant la stratégie de chaos
        if (rule_GravityFlip) {
            finalResults += ExecuteSpecialCards(specialActions);
            finalResults += ExecuteActionCards(actionActions);
            finalResults += ExecuteRuleCards(ruleActions);
        } else {
            finalResults += ExecuteRuleCards(ruleActions);
            finalResults += ExecuteActionCards(actionActions);
            finalResults += ExecuteSpecialCards(specialActions);
        }

        if (resultsText != null) resultsText.text = finalResults;
        if (Object.FindFirstObjectByType<HandManager>() != null) Object.FindFirstObjectByType<HandManager>().RefillHand();
    }

    string ExecuteRuleCards(List<TurnAction> actions)
    {
        string log = "";
        foreach (TurnAction act in actions)
        {
            foreach (var effect in act.card.effects)
            {
                // On applique l'effet immédiatement (ex: Timer) ET on l'enregistre en règle passive
                ResolveEffect(effect, playerEntity, act.performer); 
                activeRules.RemoveAll(r => r.effectType == effect.effectType);
                activeRules.Add(effect);
            }
            log += $"<color=#FFD700>RULE:</color> {act.card.cardName} is active!\n";
        }
        return log;
    }

    string ExecuteActionCards(List<TurnAction> actions)
    {
        string log = "";
        foreach (TurnAction act in actions)
        {
            ApplyEffectsWithTargeting(act);
            log += $"<color=#E61A1A>ACTION:</color> {act.performer.playerName} played {act.card.cardName}\n";
        }
        return log;
    }

    string ExecuteSpecialCards(List<TurnAction> actions)
    {
        string log = "";
        foreach (TurnAction act in actions)
        {
            // Glitch spécifique
            if (act.card.cardName == "Glitch" && Object.FindFirstObjectByType<HandManager>() != null) 
                Object.FindFirstObjectByType<HandManager>().GenerateRandomHand();

            ApplyEffectsWithTargeting(act);
            log += $"<color=#991AE6>SPECIAL:</color> {act.card.cardName}\n";
        }
        return log;
    }

    // Fonction utilitaire pour gérer le ciblage (Everyone vs Single)
    void ApplyEffectsWithTargeting(TurnAction act)
    {
        List<PlayerEntity> everyone = new List<PlayerEntity> { playerEntity };
        everyone.AddRange(botEntities);

        foreach (var effect in act.card.effects)
        {
            if (act.card.targetMode == CardData.TargetMode.Everyone)
                foreach (PlayerEntity p in everyone) ResolveEffect(effect, p, act.performer);
            else
                ResolveEffect(effect, act.target, act.performer);
        }
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

    void ApplyPassiveRules()
    {
        List<PlayerEntity> everyone = new List<PlayerEntity> { playerEntity };
        everyone.AddRange(botEntities);

        foreach (PlayerEntity p in everyone)
        {
            if (p.isPoisoned) p.TakeDamage(-5);
            if (p.isOnFire) p.TakeDamage(-10);

            // MISE À JOUR VISUELLE : Si c'est TOI qui prends des dégâts passifs
            if (p == playerEntity && healthLiquidImage != null)
            {
                currentHealth = p.currentHealth;
                healthLiquidImage.fillAmount = (float)currentHealth / maxHealth;
            }
        }
        UpdateGoldUI();
    }

}