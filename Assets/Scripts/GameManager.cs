using UnityEngine;
using TMPro;
using System.Collections.Generic;

[System.Serializable]
public class ActiveRuleInstance
{
    public string ruleName;       
    public string ruleDescription; 
    public CardData.EffectType effectType;
    public float value;
}

public class GameManager : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI timerText; 
    public GameObject resolutionPanel; 
    public TextMeshProUGUI resultsText; 
    public TextMeshProUGUI goldText;
    public TMPro.TextMeshProUGUI activeRulesText;
    public RulesDashboardUI dashboardController;
    public GameObject validateTurnButton;
    public GameObject playerHandUI;
    public CardDisplay centerCardDisplay;
    public Transform centerTableViewPoint;

    [Header("Timer Settings")]
    public float timeLeft = 15f; 
    public bool timerRunning = false;
    public float baseTimerDuration = 15f; 
    public float nextRoundTimerDuration = 15f; 

    [Header("Rule Engine")]
    public List<ActiveRuleInstance> activeRules = new List<ActiveRuleInstance>();
    public bool rule_GravityFlip = false; 

    [Header("Selections")]
    public CardData selectedCard; 
    public PlayerEntity selectedTarget;

    [Header("Entities & Turn System")]
    public PlayerEntity playerEntity; 
    public List<PlayerEntity> botEntities;
    public List<PlayerEntity> turnOrder = new List<PlayerEntity>();
    public int currentTurnIndex = 0;
    public int currentRoundNumber = 1;

    [Header("Visual Effects")]
    public GameObject attackProjectilePrefab;
    public GameObject stealProjectilePrefab;  
    public GameObject deathParticlePrefab;

    [Header("Compatibility Fields")]
    public bool isResolutionPhase = false;
    public int currentHealth { get { return playerEntity != null ? playerEntity.currentHealth : 100; } }
    public int maxHealth { get { return playerEntity != null ? playerEntity.maxHealth : 100; } }

    void Start()
    {
        if (playerEntity != null) 
        { 
            playerEntity.gold = Random.Range(0, 10);
            playerEntity.isInvisible = false; 
            playerEntity.isWanted = false; 
        }

        foreach (PlayerEntity bot in botEntities) 
        { 
            bot.gold = Random.Range(0, 10);
            bot.isInvisible = false; 
            bot.isWanted = false; 
        }

        UpdateActiveRulesUI();
        if (resolutionPanel != null) resolutionPanel.SetActive(false);
        if (validateTurnButton != null) validateTurnButton.SetActive(false);
        
        if (timerText != null) 
        {
            timerText.gameObject.SetActive(true);
            timerText.text = "";
        }

        isResolutionPhase = false;
        UpdateGoldUI();
        
        BuildTurnOrder();
        StartNewRound();
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
                UpdateTimerUI();
                HandleTimeOut();
            }
        }
    }

    void BuildTurnOrder()
    {
        turnOrder.Clear();
        if (playerEntity != null && !playerEntity.isDead) turnOrder.Add(playerEntity);
        
        foreach (PlayerEntity bot in botEntities)
        {
            if (bot != null && !bot.isDead) turnOrder.Add(bot);
        }
    }

    void StartNewRound()
    {
        ApplyPassiveRules();
        BuildTurnOrder();
        
        if (CheckWinCondition()) return;

        currentTurnIndex = 0;
        currentRoundNumber++;
        
        StartNextPlayerTurn();
    }

    void StartNextPlayerTurn()
    {
        if (CheckWinCondition()) return;

        if (validateTurnButton != null) validateTurnButton.SetActive(false);
        
        if (centerCardDisplay != null) centerCardDisplay.gameObject.SetActive(false);

        timerRunning = false;
        UpdateTimerUI();

        if (currentTurnIndex >= turnOrder.Count)
        {
            StartNewRound();
            return;
        }

        PlayerEntity activePlayer = turnOrder[currentTurnIndex];

        CameraController camCtrl = Object.FindFirstObjectByType<CameraController>();
        if (camCtrl != null && activePlayer.cameraViewPoint != null)
        {
            camCtrl.SetTarget(activePlayer.cameraViewPoint);
        }

        if (activePlayer.isDead)
        {
            AdvanceTurn();
            return;
        }

        if (activePlayer.isFrozen)
        {
            if (resolutionPanel != null) resolutionPanel.SetActive(true);
            isResolutionPhase = true;
            if (resultsText != null) resultsText.text = "<size=60><b>" + activePlayer.playerName + " IS FROZEN!</b></size>";
            activePlayer.isFrozen = false;
            Invoke("AdvanceTurn", 2.0f);
            return;
        }

        selectedCard = null;
        selectedTarget = null;

       if (!activePlayer.isBot)
        {
            if (resolutionPanel != null) resolutionPanel.SetActive(true);
            isResolutionPhase = true;
            if (resultsText != null) resultsText.text = "<size=100><b>YOUR TURN</b></size>";
            
            if (playerHandUI != null) playerHandUI.SetActive(false); 
            
            Invoke("InitializeHumanTurnVisuals", 2.0f);
        }
        else
        {
            if (playerHandUI != null) playerHandUI.SetActive(false);
            StartCoroutine(BotTurnRoutine(activePlayer));
        }
    }

    void InitializeHumanTurnVisuals()
    {
        if (resolutionPanel != null) resolutionPanel.SetActive(false);
        isResolutionPhase = false;

        // On affiche enfin le deck au moment où le texte s'en va !
        if (playerHandUI != null) playerHandUI.SetActive(true); 
        
        HandManager hm = Object.FindFirstObjectByType<HandManager>();
        if (hm != null)
        {
            hm.AnimateSideSlide(); 
        }

        timeLeft = nextRoundTimerDuration;
        timerRunning = true;
        UpdateTimerUI();

        StartCoroutine(RefreshLayoutGroupRoutine());
    }

    System.Collections.IEnumerator RefreshLayoutGroupRoutine()
    {
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();

        HandManager hm = Object.FindFirstObjectByType<HandManager>();
        if (hm != null)
        {
            UnityEngine.UI.HorizontalLayoutGroup layout = hm.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            if (layout != null)
            {
                layout.enabled = false;
                layout.enabled = true;
            }
            RectTransform rt = hm.GetComponent<RectTransform>();
            if (rt != null) UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        }
    }

    void HandleTimeOut()
    {
        PlayerEntity activePlayer = turnOrder[currentTurnIndex];
        if (activePlayer.isBot) return; 

        if (validateTurnButton != null) validateTurnButton.SetActive(false);
        timerRunning = false;
        UpdateTimerUI();

        if (resultsText != null) resultsText.text = "<size=80><b>TIME OUT!</b></size>";
        if (resolutionPanel != null) resolutionPanel.SetActive(true);
        isResolutionPhase = true;
        Invoke("AdvanceTurn", 2.0f);
    }

    public void SelectCard(CardData data)
    {
        PlayerEntity activePlayer = turnOrder[currentTurnIndex];
        if (activePlayer.isBot || timerRunning == false) return;

        selectedCard = data;

        if (activePlayer.isSilenced && selectedCard.type != CardData.CardType.Action)
        {
            selectedCard = null;
            return;
        }

        TargetingManager tm = Object.FindFirstObjectByType<TargetingManager>();
        if (tm != null)
        {
            if (selectedCard != null && selectedCard.targetMode == CardData.TargetMode.Chosen)
            {
                tm.StartTargeting();
            }
            else if (selectedCard != null)
            {
                tm.ResetArrow();
                ExecuteHumanCardNoTarget();
            }
        }
    }

    public void SetSelectedTarget(PlayerEntity target)
    {
        if (timerRunning == false || selectedCard == null) return;

        selectedTarget = target;

        if (validateTurnButton != null) 
        {
            validateTurnButton.SetActive(true);
            SetValidateButtonColor(new Color(0.4f, 0.4f, 0.4f, 1f));
        }
    }
    void ExecuteHumanCardNoTarget()
    {
        PlayerEntity performer = turnOrder[currentTurnIndex];
        selectedTarget = CalculateAutomaticTarget(performer, selectedCard.targetMode);
        
        if (validateTurnButton != null) 
        {
            validateTurnButton.SetActive(true);
            SetValidateButtonColor(new Color(0.4f, 0.4f, 0.4f, 1f));
        }
    }

    void SetValidateButtonColor(Color c)
    {
        if (validateTurnButton != null)
        {
            UnityEngine.UI.Image img = validateTurnButton.GetComponent<UnityEngine.UI.Image>();
            if (img != null) img.color = c;
        }
    }

    public void ConfirmHumanTurn()
    {
        StartCoroutine(ConfirmHumanTurnRoutine());
    }

    System.Collections.IEnumerator ConfirmHumanTurnRoutine()
    {
        SetValidateButtonColor(Color.white); 
        timerRunning = false;
        UpdateTimerUI();
        yield return new WaitForSeconds(0.3f);

        TargetingManager tm = Object.FindFirstObjectByType<TargetingManager>();
        if (tm != null) tm.ResetArrow();

        if (validateTurnButton != null) validateTurnButton.SetActive(false);
        ExecuteCardAction(turnOrder[currentTurnIndex], selectedCard, selectedTarget);
    }

    System.Collections.IEnumerator BotTurnRoutine(PlayerEntity bot)
    {
        if (resolutionPanel != null) resolutionPanel.SetActive(true);
        isResolutionPhase = true;
        if (resultsText != null) resultsText.text = "<size=80><b>" + bot.playerName.ToUpper() + "'S TURN</b></size>";
        yield return new WaitForSeconds(1.0f);

        if (resolutionPanel != null) resolutionPanel.SetActive(false);
        isResolutionPhase = false;

        if (centerCardDisplay != null) centerCardDisplay.gameObject.SetActive(false);

        timeLeft = Random.Range(2.0f, 3.5f);
        timerRunning = true;
        UpdateTimerUI();

        yield return new WaitUntil(() => timeLeft <= 0);

        timerRunning = false;
        UpdateTimerUI();

        BotBrain brain = bot.GetComponent<BotBrain>();
        CardData botCard = (brain != null) ? brain.ChooseCard() : null;

        if (botCard != null)
        {
            if (bot.isSilenced && botCard.type != CardData.CardType.Action)
            {
                if (resolutionPanel != null) resolutionPanel.SetActive(true);
                isResolutionPhase = true;
                if (resultsText != null) resultsText.text = "<size=60><b>" + bot.playerName + " IS SILENCED!</b></size>";
                bot.isSilenced = false;
                yield return new WaitForSeconds(2.0f);
                AdvanceTurn();
                yield break;
            }

            bot.isSilenced = false;

            PlayerEntity botTarget = CalculateAutomaticTarget(bot, botCard.targetMode);
            if (botCard.targetMode == CardData.TargetMode.Chosen)
            {
                List<PlayerEntity> potentialTargets = turnOrder.FindAll(p => p != bot && !p.isInvisible);
                if (potentialTargets.Count > 0) botTarget = potentialTargets[Random.Range(0, potentialTargets.Count)];
                else botTarget = turnOrder[Random.Range(0, turnOrder.Count)];
            }

            if (brain != null) brain.DiscardAndReplace(botCard);
            ExecuteCardAction(bot, botCard, botTarget);
        }
        else
        {
            AdvanceTurn();
        }
    }

    // Cette fonction sert maintenant de passerelle sécurisée pour lancer notre Coroutine de mise en scène
    void ExecuteCardAction(PlayerEntity performer, CardData card, PlayerEntity target)
    {
        StartCoroutine(ExecuteCardActionRoutine(performer, card, target));
    }

    System.Collections.IEnumerator ExecuteCardActionRoutine(PlayerEntity performer, CardData card, PlayerEntity target)
    {
        if (resolutionPanel != null) resolutionPanel.SetActive(true);
        isResolutionPhase = true;
        timerRunning = false;
        UpdateTimerUI();

        // --- 1. SÉQUENCE LECTURE : On affiche la carte en grand (la caméra ne bouge pas encore) ---
        if (centerCardDisplay != null)
        {
            centerCardDisplay.gameObject.SetActive(true);
            centerCardDisplay.LoadCard(card); 
            centerCardDisplay.SetVisualState(false);
            centerCardDisplay.SetYOffset(0);
        }
        
        if (!performer.isBot && playerHandUI != null)
        {
            playerHandUI.SetActive(false);
        }

        string cardColorHex = "#E61A1A"; 
        if (card.type == CardData.CardType.Rule) cardColorHex = "#FFD700";
        else if (card.type == CardData.CardType.Special) cardColorHex = "#991AE6";

        if (resultsText != null)
        {
            resultsText.text = "<b><color=" + cardColorHex + "><size=2>" + card.cardName.ToUpper() + "</size></color></b>";
        }

        // [CHRONO LECTURE] On attend sagement 3 secondes que le joueur lise la carte au calme
        yield return new WaitForSeconds(3.0f);

        // --- 2. SÉQUENCE ACTION : On masque la carte et on lance l'attaque visuelle ---
        if (target != null && target != performer)
        {
            // === NOUVEAU : REDIRECTION AUTOMATIQUE SI LA CIBLE EST MORTE ===
            if (target.isDead)
            {
                target = GetRedirectedTarget(target);
            }

            // On fait disparaître la carte géante pour libérer totalement le centre de l'écran !
            if (centerCardDisplay != null) centerCardDisplay.gameObject.SetActive(false);

            // La caméra recule pour filmer toute la table
            CameraController camCtrl = Object.FindFirstObjectByType<CameraController>();
            if (camCtrl != null && centerTableViewPoint != null)
            {
                camCtrl.SetTarget(centerTableViewPoint);
            }

            yield return new WaitForSeconds(0.4f);
            
            GameObject prefabToSpawn = null;
            Color projColor = Color.white;

            bool structureContientDeLor = card.effects.Exists(e => e.effectType == CardData.EffectType.Gold);

            if (structureContientDeLor || 
                card.cardName.ToLower().Contains("steal") || 
                card.cardName.ToLower().Contains("or") || 
                card.cardName.ToLower().Contains("money"))
            {
                prefabToSpawn = stealProjectilePrefab;
                projColor = new Color(1f, 0.85f, 0f);
            }
            else if (card.type == CardData.CardType.Action || card.type == CardData.CardType.Special)
            {
                prefabToSpawn = attackProjectilePrefab;

                if (card.effects.Count > 0)
                {
                    CardData.EffectType currentEffect = card.effects[0].effectType;
                    switch (currentEffect)
                    {
                        case CardData.EffectType.Burn: projColor = new Color(1f, 0.3f, 0f); break;   // Orange
                        case CardData.EffectType.Poison: projColor = new Color(0.2f, 1f, 0.2f); break; // Vert
                        case CardData.EffectType.Frozen: projColor = new Color(0f, 0.7f, 1f); break;  // Bleu
                        default: projColor = new Color(0.9f, 0.1f, 0.1f); break;                      // Rouge
                    }
                }
            }

            // === SYSTÈME DE TIR 3D VERROUILLÉ (MODIFIÉ ICI) ===
            if (prefabToSpawn != null)
            {
                Vector3 spawnPos = performer.transform.position;
                Vector3 targetPos = target.transform.position;

                // On détecte le centre réel du modèle 3D visible (fiole/corps)
                Renderer perfRenderer = performer.GetComponentInChildren<Renderer>();
                if (perfRenderer != null) spawnPos = perfRenderer.bounds.center;

                Renderer targetRenderer = target.GetComponentInChildren<Renderer>();
                if (targetRenderer != null) targetPos = targetRenderer.bounds.center;

                // On instancie notre magnifique projectile 3D
                GameObject projGO = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
                
                // AJOUT : On va chercher le script déjà présent sur le prefab pour garder ton Rotation Offset !
                GhostProjectile projectileScript = projGO.GetComponent<GhostProjectile>();
                if (projectileScript == null)
                {
                    projectileScript = projGO.AddComponent<GhostProjectile>();
                }
                
                // On l'initialise avec la vraie position du corps adverse
                projectileScript.Setup(targetPos, projColor);
            }

            // On attend que le projectile termine sa course
            yield return new WaitForSeconds(0.6f);

            yield return StartCoroutine(PlayImpactJuice(target));

            // === NOUVEAU : DÉCLENCHEMENT DE L'ANIMATION D'IMPACT ===
            StartCoroutine(PlayImpactJuice(target));

            if (impactParticlePrefab != null)
            {
                Renderer targetRenderer = target.GetComponentInChildren<Renderer>();
                Vector3 impactPos = targetRenderer != null ? targetRenderer.bounds.center : target.transform.position;
                
                GameObject effectInstance = Instantiate(impactParticlePrefab, impactPos, Quaternion.identity);
                
                Destroy(effectInstance, 2.0f);
            }
        } 

        // --- 3. SÉQUENCE IMPACT : On applique les effets REELS dans le jeu ---
        if (card.cardName == "Glitch" && Object.FindFirstObjectByType<HandManager>() != null)
        {
            Object.FindFirstObjectByType<HandManager>().GenerateRandomHand();
        }

        foreach (var effect in card.effects)
        {
            if (card.targetMode == CardData.TargetMode.Everyone)
            {
                foreach (PlayerEntity p in turnOrder) ResolveEffect(effect, p, performer);
            }
            else
            {
                ResolveEffect(effect, target, performer);
            }
        }

        if (card.type == CardData.CardType.Rule)
        {
            foreach (var effect in card.effects)
            {
                activeRules.RemoveAll(r => r.effectType == effect.effectType);
                ActiveRuleInstance newRule = new ActiveRuleInstance
                {
                    ruleName = card.cardName,
                    ruleDescription = card.description,
                    effectType = effect.effectType,
                    value = effect.value
                };
                activeRules.Add(newRule);
            }
            UpdateActiveRulesUI();
        }

        performer.isSilenced = false;

        if (!performer.isBot && Object.FindFirstObjectByType<HandManager>() != null)
        {
            Object.FindFirstObjectByType<HandManager>().RefillHand();
        }

        // On laisse 1 seconde de pause pour que le joueur voie bien le résultat des dégâts sur les fioles
        yield return new WaitForSeconds(1.0f);

        // On passe enfin au tour suivant
        AdvanceTurn();
    }

    void AdvanceTurn()
    {
        currentTurnIndex++;
        StartNextPlayerTurn();
    }

    PlayerEntity CalculateAutomaticTarget(PlayerEntity performer, CardData.TargetMode mode)
    {
        switch (mode)
        {
            case CardData.TargetMode.Self: return performer;
            case CardData.TargetMode.Left: return performer.leftNeighbor;
            case CardData.TargetMode.Right: return performer.rightNeighbor;
            case CardData.TargetMode.Opposite: return performer.oppositePlayer;
            case CardData.TargetMode.Everyone: return performer;
            default: return performer;
        }
    }

    void ResolveEffect(CardData.CardEffect effect, PlayerEntity target, PlayerEntity performer)
    {
        if (target == null || performer == null) return;

        switch (effect.effectType)
        {
            case CardData.EffectType.Damage:
                int dmg = Mathf.Abs((int)effect.value);
                if (activeRules.Exists(r => r.effectType == CardData.EffectType.Heal)) target.TakeDamage(dmg);
                else target.TakeDamage(-dmg);
                
                // 1. On applique l'assombrissement progressif
                UpdatePlayerVisualDarkness(target);

                // 2. === EXPLOSION ===
                if (target.isDead)
                {
                    if (deathParticlePrefab != null)
                    {
                        Renderer targetRenderer = target.GetComponentInChildren<Renderer>();
                        Vector3 deathPos = targetRenderer != null ? targetRenderer.bounds.center : target.transform.position;
                        
                        GameObject deathFX = Instantiate(deathParticlePrefab, deathPos, Quaternion.identity);
                        Destroy(deathFX, 3.0f); 
                    }

                    if (target.GetComponentInChildren<Renderer>() != null)
                    {
                        target.GetComponentInChildren<Renderer>().gameObject.SetActive(false);
                    }
                }
                break;
            case CardData.EffectType.Heal: 
                target.TakeDamage(Mathf.Abs((int)effect.value)); 
                UpdatePlayerVisualDarkness(target);
                break;
            case CardData.EffectType.Gold: 
                int goldAmount = Mathf.Abs((int)effect.value);
                if (target == performer)
                {
                    target.gold += goldAmount;
                }
                else
                {
                    if (target.gold < goldAmount) goldAmount = target.gold;
                    target.gold -= goldAmount;
                    performer.gold += goldAmount;
                }
                break;
            case CardData.EffectType.Karma: target.karma += (int)effect.value; break;
            case CardData.EffectType.Luck: target.luck += (int)effect.value; break;
            case CardData.EffectType.Frozen: target.isFrozen = true; break;
            case CardData.EffectType.Burn: target.isOnFire = true; break;
            case CardData.EffectType.Poison: target.isPoisoned = true; break;
            case CardData.EffectType.Shield: target.isShielded = true; break;
            case CardData.EffectType.Invisible: target.isInvisible = true; break;
            case CardData.EffectType.Wanted: target.isWanted = true; break;
            case CardData.EffectType.Silenced: target.isSilenced = true; break;
            case CardData.EffectType.Linked: target.isLinked = true; break;
            case CardData.EffectType.Thorns: target.thorns += (int)effect.value; break;
            case CardData.EffectType.HandSize: target.handSize = (int)effect.value; break;
            case CardData.EffectType.TimerMod: 
                nextRoundTimerDuration = effect.value;
                break;
            case CardData.EffectType.GravityFlip: rule_GravityFlip = !rule_GravityFlip; break;
        }

        if (performer == playerEntity || target == playerEntity) UpdateGoldUI();
    }

    public bool CheckWinCondition()
    {
        List<PlayerEntity> survivors = new List<PlayerEntity>();
        if (playerEntity != null && !playerEntity.isDead) survivors.Add(playerEntity);
        foreach (PlayerEntity bot in botEntities)
        {
            if (bot != null && !bot.isDead) survivors.Add(bot);
        }

        if (survivors.Count <= 1)
        {
            timerRunning = false;
            UpdateTimerUI();
            if (resolutionPanel != null) resolutionPanel.SetActive(true);
            isResolutionPhase = true;
            
            if (survivors.Count == 1)
            {
                if (resultsText != null) resultsText.text = "<size=100><b>VICTORY!</b></size>\n\n<size=50>" + survivors[0].playerName + " survit!</size>";
            }
            else
            {
                if (resultsText != null) resultsText.text = "<size=100><b>DRAW!</b></size>\n\n<size=50>Plus personne n'est en vie!</size>";
            }
            return true;
        }
        return false;
    }

    public void UpdateGoldUI()
    {
        if (goldText != null && playerEntity != null) goldText.text = playerEntity.gold.ToString();
    }

    void UpdateTimerUI()
    {
        if (timerText != null) 
        {
            if (timerRunning) timerText.text = Mathf.Ceil(timeLeft).ToString();
            else timerText.text = ""; 
        }
    }

    void ApplyPassiveRules()
    {
        List<PlayerEntity> everyone = new List<PlayerEntity> { playerEntity };
        everyone.AddRange(botEntities);

        bool globalPoison = activeRules.Exists(r => r.effectType == CardData.EffectType.Poison);
        bool globalBurn = activeRules.Exists(r => r.effectType == CardData.EffectType.Burn);

        foreach (PlayerEntity p in everyone)
        {
            if (p != null && !p.isDead)
            {
                if (p.isPoisoned || globalPoison) p.TakeDamage(-5);
                if (p.isOnFire || globalBurn) p.TakeDamage(-10);

                // On actualise son visuel après les dégâts passifs
                UpdatePlayerVisualDarkness(p);

                // Si le poison ou le feu l'a achevé, il explose !
                if (p.isDead)
                {
                    if (deathParticlePrefab != null)
                    {
                        Renderer targetRenderer = p.GetComponentInChildren<Renderer>();
                        Vector3 deathPos = targetRenderer != null ? targetRenderer.bounds.center : p.transform.position;
                        GameObject deathFX = Instantiate(deathParticlePrefab, deathPos, Quaternion.identity);
                        Destroy(deathFX, 3.0f);
                    }
                    if (p.GetComponentInChildren<Renderer>() != null)
                    {
                        p.GetComponentInChildren<Renderer>().gameObject.SetActive(false);
                    }
                }
            }
        }
        UpdateGoldUI();
    }

    public void UpdateActiveRulesUI()
    {
        if (activeRulesText == null) return; 

        if (activeRules.Count == 0) 
        {
            activeRulesText.text = "<size=22><b>MANOR LAWS:</b></size>\n\n<size=14><i>No laws active.</i></size>";
            return;
        }

        string textBuffer = "<size=22><b>MANOR LAWS:</b></size>\n\n";
        foreach (ActiveRuleInstance rule in activeRules) 
        {
            textBuffer += $"<b><color=#FFD700>• {rule.ruleName}</color></b>\n";
            textBuffer += $"<size=-3><i>{rule.ruleDescription}</i></size>\n\n";
        }
        activeRulesText.text = textBuffer; 

        if (dashboardController != null)
        {
            dashboardController.TriggerNewRuleAlert();
        }
    }

    [Header("Future Assets Pack")]
    [Tooltip("Tu glisseras ton effet de particule du Legacy Pack ici plus tard !")]
    public GameObject impactParticlePrefab;

   // === JUICE EFFECT CORRIGÉ (CORPS + YEUX FLASHENT EN ENTIER) ===
    System.Collections.IEnumerator PlayImpactJuice(PlayerEntity target)
    {
        if (target == null) yield break;

        Transform rootTransform = target.transform;
        Vector3 originalScale = rootTransform.localScale;
        Vector3 originalPosition = rootTransform.position;

        Renderer[] allRenderers = target.GetComponentsInChildren<Renderer>();

        // 1. FLASH ROUGE SUR TOUS LES MORCEAUX (Corps + Yeux !)
        if (allRenderers != null)
        {
            foreach (Renderer ren in allRenderers)
            {
                if (ren == null || ren.material == null) continue;
                
                string colorPropName = "";
                if (ren.material.HasProperty("_Color")) colorPropName = "_Color";
                else if (ren.material.HasProperty("_BaseColor")) colorPropName = "_BaseColor";

                if (!string.IsNullOrEmpty(colorPropName))
                {
                    ren.material.SetColor(colorPropName, new Color(1f, 0.25f, 0.25f)); 
                }
            }
        }

        // 2. BOUCLE DE REBOND PHYSICS
        float duration = 0.25f;
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            float wave = Mathf.Sin(t * Mathf.PI);
            
            float scaleModifier = wave * 0.4f; 
            rootTransform.localScale = new Vector3(originalScale.x + scaleModifier, originalScale.y - scaleModifier, originalScale.z + scaleModifier);
            rootTransform.position = originalPosition + Vector3.up * (wave * 0.5f);

            yield return null;
        }

        // 3. REMISE À ZÉRO PHYSIQUE
        rootTransform.localScale = originalScale;
        rootTransform.position = originalPosition;

        // Recalcule la couleur sombre finale sur TOUT le monde d'un coup
        UpdatePlayerVisualDarkness(target);
    }

   // === SYSTÈME DE DAMAGE VISUEL CORRIGÉ (CORPS + YEUX) ===
    public void UpdatePlayerVisualDarkness(PlayerEntity target)
    {
        if (target == null) return;

        // NOUVEAU : On récupère TOUS les Renderers (Corps, Yeux, Accessoires...)
        Renderer[] allRenderers = target.GetComponentsInChildren<Renderer>();
        if (allRenderers == null || allRenderers.Length == 0) return;

        float healthRatio = (float)target.currentHealth / target.maxHealth;

        // Calibrage sur ton gris #6C6C6C (0.42f)
        float maxBrightness = 108f / 255f; 
        float minBrightness = 0.05f; 

        float brightness = Mathf.Lerp(minBrightness, maxBrightness, healthRatio);
        Color healthColor = new Color(brightness, brightness, brightness, 1f);

        // On applique la couleur sur CHAQUE morceau trouvé
        foreach (Renderer ren in allRenderers)
        {
            if (ren == null || ren.material == null) continue;

            string colorPropName = "";
            if (ren.material.HasProperty("_Color")) colorPropName = "_Color";
            else if (ren.material.HasProperty("_BaseColor")) colorPropName = "_BaseColor";

            if (!string.IsNullOrEmpty(colorPropName))
            {
                ren.material.SetColor(colorPropName, healthColor);
            }
        }
    }

    // === CALCUL DES POSITIONS ET REDIRECTION DES CIBLES ===
    private PlayerEntity GetRedirectedTarget(PlayerEntity deadTarget)
    {
        if (botEntities == null || botEntities.Count == 0) return deadTarget;

        // On crée une copie de la liste des bots et on les trie par leur position X dans le monde 3D
        // (Le plus petit X = Gauche, le X du milieu = Milieu, le plus grand X = Droite)
        List<PlayerEntity> sortedBots = new List<PlayerEntity>(botEntities);
        sortedBots.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));

        PlayerEntity leftBot = sortedBots.Count > 0 ? sortedBots[0] : null;
        PlayerEntity middleBot = sortedBots.Count > 1 ? sortedBots[1] : null;
        PlayerEntity rightBot = sortedBots.Count > 2 ? sortedBots[2] : null;

        // RÈGLE : Si la cible morte est à GAUCHE ou à DROITE, on redirige vers le MILIEU (s'il est en vie)
        if ((deadTarget == leftBot || deadTarget == rightBot) && middleBot != null && !middleBot.isDead)
        {
            Debug.Log($"[REDIRECTION] {deadTarget.playerName} est mort ! L'attaque est redirigée sur le milieu : {middleBot.playerName}");
            return middleBot;
        }

        // SÉCURITÉ ULTIME : Si le milieu est mort aussi, on cherche le premier bot vivant disponible
        foreach (PlayerEntity bot in sortedBots)
        {
            if (bot != null && !bot.isDead) return bot;
        }

        // Si vraiment TOUS les bots sont morts, on renvoie la cible d'origine par sécurité
        return deadTarget;
    }
}