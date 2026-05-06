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
    public CardData lastPlayedCard; // Stocke la carte du tour précédent

    [Header("Selections")]
    public CardData selectedCard; 
    public List<CardData> botSelections = new List<CardData>(); 

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

        if (selectedCard == null)
        {
            HandManager hm = Object.FindFirstObjectByType<HandManager>();
            if (hm != null) hm.GetRandomCardFromHand().SelectThisCard();
        }
        
        if (selectedCard == null)
        {
            HandManager hm = Object.FindFirstObjectByType<HandManager>();
            if (hm != null) hm.GetRandomCardFromHand().SelectThisCard();
        }

        isResolutionPhase = true; 

        botSelections.Clear();
        HandManager hmRef = Object.FindFirstObjectByType<HandManager>();
        if (hmRef != null)
        {
            for (int i = 0; i < 3; i++)
            {
                int randomIndex = Random.Range(0, hmRef.allCardsInGame.Count);
                botSelections.Add(hmRef.allCardsInGame[randomIndex]);
            }
        }

        ProcessAllCards();
    }

    void ProcessAllCards()
    {
        if (resolutionPanel != null) resolutionPanel.SetActive(true);
        string finalResults = "<b>ROUND RESULTS</b>\n\n";

        List<CardData> allPlayedCards = new List<CardData>();
        if (selectedCard != null) allPlayedCards.Add(selectedCard);
        allPlayedCards.AddRange(botSelections);

        List<CardData> ruleCards = allPlayedCards.FindAll(c => c.type == CardData.CardType.Rule);
        List<CardData> actionCards = allPlayedCards.FindAll(c => c.type == CardData.CardType.Action);
        List<CardData> specialCards = allPlayedCards.FindAll(c => c.type == CardData.CardType.Special);

        if (rule_GravityFlip)
        {
            finalResults += ExecuteSpecialCards(specialCards);
            finalResults += ExecuteActionCards(actionCards);
            finalResults += ExecuteRuleCards(ruleCards);
        }
        else
        {
            finalResults += ExecuteRuleCards(ruleCards);
            finalResults += ExecuteActionCards(actionCards);
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

    string ExecuteActionCards(List<CardData> cards)
    {
        string log = "";
        foreach (CardData card in cards)
        {
            string name = card.cardName.Trim();
            
            // LOGIQUE DUPLICATE[cite: 15]
            if (name == "Duplicate")
            {
                if (lastPlayedCard != null)
                {
                    log += $"<color=#FF00FF>DUPLICATE:</color> Copying {lastPlayedCard.cardName}!\n";
                    ApplyCardValue(lastPlayedCard.effectValue);
                }
                else
                {
                    log += "<i>(Duplicate failed: no history)</i>\n";
                }
            }
            else if (name == "Shield") isShielded = true;
            else if (name == "Mirror Shield") isMirrorShielded = true;
            else 
            {
                int hpChange = card.effectValue; 

                if (rule_HealingStrikes && hpChange < 0) hpChange = -hpChange;

                if (hpChange < 0) 
                {
                    if (isShielded) 
                    {
                        hpChange = 0; 
                        isShielded = false; 
                        log += "<i>(Attack Blocked by Shield!)</i> ";
                    }
                    else if (isMirrorShielded)
                    {
                        hpChange = 0; 
                        isMirrorShielded = false; 
                        log += "<i>(Attack Reflected!)</i> ";
                    }
                }

                ApplyCardValue(hpChange);
            }
            log += $"<color=#E61A1A>ACTION:</color> {name}\n";
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

    void ApplyCardValue(int value)
    {
        if (value == 0) return; 
        currentHealth += value;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        if (healthLiquidImage != null) healthLiquidImage.fillAmount = (float)currentHealth / maxHealth;
    }

    public void SelectCard(CardData data)
    {
        if (!isResolutionPhase) selectedCard = data;
    }
}