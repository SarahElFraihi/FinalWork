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
    public int turnsRemaining = 2;
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

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip attackSound;
    public AudioClip stealSound;
    public AudioClip healSound;
    public AudioClip deathSound;  

    [Header("Timer Juice Settings")]
    private int lastSecondTicked = -1;
    private float timerPulseScale = 0f;
    private Vector2 timerOriginalPos;
    private Vector3 timerOriginalScale;
    private bool timerVisualsInitialized = false;

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
    private CardData lastPlayedCard;
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
    public GameObject healProjectilePrefab;
    public GameObject deathParticlePrefab;
    public GameObject damageImpactPrefab;
    public GameObject stealImpactPrefab;   
    public GameObject healImpactPrefab;

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
            // Initialisation unique des positions d'origine de ton texte
            if (!timerVisualsInitialized && timerText != null)
            {
                timerOriginalPos = timerText.rectTransform.anchoredPosition;
                timerOriginalScale = timerText.rectTransform.localScale;
                timerVisualsInitialized = true;
            }

            if (timeLeft > 0)
            {
                timeLeft -= Time.deltaTime;
                UpdateTimerUI(); // Met à jour le chiffre affiché (ex: "5")

                // --- SYSTEME DE JUICE DU TIMER ---
                if (timerText != null)
                {
                    // 1. Détection du changement de seconde pour le battement (Pop)
                    int currentSecond = Mathf.CeilToInt(timeLeft);
                    if (currentSecond != lastSecondTicked)
                    {
                        lastSecondTicked = currentSecond;
                        timerPulseScale = 1f; // On recharge l'impulsion à fond
                    }

                    // Dissipation progressive de l'impulsion (effet ressort)
                    timerPulseScale = Mathf.MoveTowards(timerPulseScale, 0f, Time.deltaTime * 5f);

                    // 2. Calcul du taux de stress (0 = calme, 1 = panique totale à 0 seconde)
                    float stressProgress = 0f;
                    if (timeLeft <= 5f)
                    {
                        stressProgress = 1f - (timeLeft / 5f); // Évolue de 0 à 1 en 5 secondes
                    }

                    // 3. Application de la COULEUR (Blanc -> Orange -> Rouge de plus en plus sombre)
                    if (timeLeft > 5f)
                    {
                        timerText.color = Color.white;
                    }
                    else
                    {
                        timerText.color = Color.Lerp(new Color(1f, 0.6f, 0f), Color.red, stressProgress);
                    }

                    // 4. Application de la TAILLE (Pulse de la seconde + Grossissement continu du stress)
                    float bonusScale = (timerPulseScale * 0.35f) + (stressProgress * 0.5f);
                    timerText.rectTransform.localScale = timerOriginalScale * (1f + bonusScale);

                    // 5. Application du TREMBLEMENT (Shake qui s'intensifie sous les 5 secondes)
                    if (timeLeft <= 5f)
                    {
                        float shakeIntensity = stressProgress * 12f; // Tremble jusqu'à 12 pixels de décalage max
                        float randomX = Random.Range(-1f, 1f) * shakeIntensity;
                        float randomY = Random.Range(-1f, 1f) * shakeIntensity;
                        timerText.rectTransform.anchoredPosition = timerOriginalPos + new Vector2(randomX, randomY);
                    }
                    else
                    {
                        timerText.rectTransform.anchoredPosition = timerOriginalPos;
                    }
                }
            }
            else
            {
                timeLeft = 0;
                timerRunning = false;

                // Remise à zéro propre des transformations mécaniques avant le Time Out
                if (timerText != null)
                {
                    timerText.rectTransform.localScale = timerOriginalScale;
                    timerText.rectTransform.anchoredPosition = timerOriginalPos;
                    timerText.color = Color.white;
                }

                UpdateTimerUI();
                HandleTimeOut(); // Lance l'affichage de fin de tour
            }
        }
        else
        {
            // Reset du tick dès que le chrono s'arrête pour être prêt au prochain tour
            lastSecondTicked = -1;
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
            // === NOUVEAU : On lance notre routine d'animation à la place du vieux Invoke ===
            StartCoroutine(PlayYourTurnVisualRoutine());
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

    // === 1. DÉCLENCHEMENT DE L'EXPLOSION DU TIME OUT ===
    // === 1. DÉCLENCHEMENT DU TIME OUT SANS EXPLOSION ===
    void HandleTimeOut()
    {
        PlayerEntity activePlayer = turnOrder[currentTurnIndex];
        if (activePlayer.isBot) return; 

        // On lance la routine visuelle épurée
        StartCoroutine(TimeOutVisualRoutine());
    }

    // === 2. LA MISE EN SCÈNE DU TEXTE (SLAM + SHAKE EN DIRECT) ===
    System.Collections.IEnumerator TimeOutVisualRoutine()
    {
        if (validateTurnButton != null) validateTurnButton.SetActive(false);
        timerRunning = false;
        UpdateTimerUI();

        // --- ÉTAPE A : LE TEXTE APPARAÎT GÉANT EN DIRECT (Slam Effect) ---
        if (resultsText != null) resultsText.text = "<size=80><b>TIME OUT!</b></size>";
        if (resolutionPanel != null) resolutionPanel.SetActive(true);
        isResolutionPhase = true;

        RectTransform textRect = resultsText.rectTransform;
        Vector3 finalScale = Vector3.one;
        
        // Le texte commence 4 fois plus gros pour simuler un impact visuel projeté
        textRect.localScale = finalScale * 4f; 

        float slamDuration = 0.15f; // Très rapide et percutant
        float time = 0f;
        while (time < slamDuration)
        {
            time += Time.deltaTime;
            float t = time / slamDuration;
            
            // Le texte se rétracte à toute vitesse vers sa taille normale
            textRect.localScale = Vector3.Lerp(finalScale * 4f, finalScale, t);
            yield return null;
        }
        textRect.localScale = finalScale;

        // --- ÉTAPE B : L'ONDE DE CHOC (Le texte vibre) ---
        Vector2 originalAnchoredPos = textRect.anchoredPosition;
        float shakeDuration = 0.35f; 
        time = 0f;
        
        while (time < shakeDuration)
        {
            time += Time.deltaTime;
            float t = time / shakeDuration;
            
            // L'intensité du tremblement diminue au fil des frames
            float intensity = (1f - t) * 35f; 
            float randomX = Random.Range(-1f, 1f) * intensity;
            float randomY = Random.Range(-1f, 1f) * intensity;
            
            textRect.anchoredPosition = originalAnchoredPos + new Vector2(randomX, randomY);
            yield return null;
        }
        // Remise en place parfaite
        textRect.anchoredPosition = originalAnchoredPos;

        // --- ÉTAPE C : PAUSE ET PASSAGE AU TOUR SUIVANT ---
        // On laisse le panneau affiché pendant 1.5 seconde pour que le joueur digère son retard
        yield return new WaitForSeconds(1.5f);

        AdvanceTurn();
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

    // === ANIMATION IDENTIQUE ET ACCÉLÉRÉE POUR LE TOUR DES BOTS ===
    System.Collections.IEnumerator BotTurnRoutine(PlayerEntity bot)
    {
        if (resolutionPanel != null) resolutionPanel.SetActive(true);
        isResolutionPhase = true;

        if (resultsText != null)
        {
            resultsText.text = bot.playerName.ToUpper() + "'S TURN";
            RectTransform textRect = resultsText.rectTransform;

            // Strictement le même effet soft de 1 seconde pour une cohérence parfaite
            textRect.localScale = new Vector3(0.7f, 0.7f, 1f);
            
            float duration = 1.0f; 
            float time = 0f;
            while (time < duration)
            {
                time += Time.deltaTime;
                float t = time / duration;

                if (t < 0.2f)
                {
                    float scaleT = t / 0.2f;
                    textRect.localScale = Vector3.Lerp(new Vector3(0.7f, 0.7f, 1f), Vector3.one, Mathf.Sin(scaleT * Mathf.PI * 0.5f));
                }
                else
                {
                    textRect.localScale = Vector3.one;
                }
                yield return null;
            }
            textRect.localScale = Vector3.one;
        }
        else
        {
            yield return new WaitForSeconds(1.0f);
        }

        if (resolutionPanel != null) resolutionPanel.SetActive(false);
        isResolutionPhase = false;

        if (centerCardDisplay != null) centerCardDisplay.gameObject.SetActive(false);

        // === ⏱️ UNIFICATION DU TIMER (MÊME QUE LE JOUEUR) ===
        // On donne exactement la même durée initiale au bot
        timeLeft = nextRoundTimerDuration; 
        timerRunning = true;
        UpdateTimerUI();

        // === 🤔 SIMULATION DU TEMPS DE RÉFLEXION (3 À 5 SECONDES) ===
        float botThinkingTime = Random.Range(3.0f, 5.0f);
        float elapsed = 0f;

        // Le bot réfléchit tant que son temps n'est pas écoulé ET que le chrono global n'est pas à 0
        while (elapsed < botThinkingTime && timeLeft > 0)
        {
            elapsed += Time.deltaTime;
            yield return null; // On attend frame par frame pour laisser l'Update() faire descendre le temps
        }

        // Le bot a fait son choix, on coupe le chrono (comme un joueur qui valide son tour)
        timerRunning = false;
        UpdateTimerUI();

        // === EXÉCUTION DE L'ACTION DU BOT ===
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
                yield return new WaitForSeconds(1.5f);
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

       // --- 2. SÉQUENCE ACTION : On masse la carte et on lance l'attaque visuelle ---
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

            // === 1. ANTICIPATION DE LA MORT (DÉPLACÉE ICI) ===
            bool isFatalBlow = false;
            var dmgEffect = card.effects.Find(e => e.effectType == CardData.EffectType.Damage);
            
            if (dmgEffect != null && !activeRules.Exists(r => r.effectType == CardData.EffectType.Heal))
            {
                if (!target.isShielded)
                {
                    int dmgAmount = Mathf.Abs((int)dmgEffect.value);
                    int finalDmg = Mathf.RoundToInt(dmgAmount * target.defenseMultiplier);
                    
                    if (target.currentHealth - finalDmg <= 0)
                    {
                        isFatalBlow = true; // C'est le coup de grâce !
                    }
                }
            }
            
            GameObject prefabToSpawn = null;
            GameObject chosenImpactPrefab = null;
            Color projColor = Color.white;

            bool structureContientDeLor = card.effects.Exists(e => e.effectType == CardData.EffectType.Gold);
            bool structureContientDuSoin = card.effects.Exists(e => e.effectType == CardData.EffectType.Heal);

            // A. CAS DU VOL D'OR
            if (structureContientDeLor || 
                card.cardName.ToLower().Contains("steal") || 
                card.cardName.ToLower().Contains("or") || 
                card.cardName.ToLower().Contains("gold") || 
                card.cardName.ToLower().Contains("money"))
            {
                prefabToSpawn = stealProjectilePrefab;
                chosenImpactPrefab = stealImpactPrefab;
                audioSource.PlayOneShot(stealSound);
                projColor = new Color(1f, 0.85f, 0f);
            }
            // B. CAS DU SOIN
            else if (structureContientDuSoin || 
                     card.cardName.ToLower().Contains("heal") || 
                     card.cardName.ToLower().Contains("health") || 
                     card.cardName.ToLower().Contains("hp") ||
                     card.cardName.ToLower().Contains("revive"))
            {
                prefabToSpawn = healProjectilePrefab;
                chosenImpactPrefab = healImpactPrefab;
                audioSource.PlayOneShot(healSound);
                projColor = new Color(0.2f, 1f, 0.2f);
            }
            // C. CAS DES DEGATS CLASSIQUES
            else if (card.type == CardData.CardType.Action || card.type == CardData.CardType.Special)
            {
                prefabToSpawn = attackProjectilePrefab;
                chosenImpactPrefab = damageImpactPrefab;

                // === CORRECTION AUDIO : On ne joue l'attaque QUE si ce n'est pas fatal ===
                if (audioSource != null && !isFatalBlow)
                {
                    audioSource.PlayOneShot(attackSound);
                }

                if (card.effects.Count > 0)
                {
                    CardData.EffectType currentEffect = card.effects[0].effectType;
                    switch (currentEffect)
                    {
                        case CardData.EffectType.Burn: projColor = new Color(1f, 0.3f, 0f); break;   
                        case CardData.EffectType.Poison: projColor = new Color(0.2f, 1f, 0.2f); break; 
                        case CardData.EffectType.Frozen: projColor = new Color(0f, 0.7f, 1f); break;  
                        default: projColor = new Color(0.9f, 0.1f, 0.1f); break;                      
                    }
                }
            }

            // === SYSTÈME DE TIR 3D VERROUILLÉ ===
            if (prefabToSpawn != null)
            {
                Vector3 spawnPos = performer.transform.position;
                Vector3 targetPos = target.transform.position;

                Renderer perfRenderer = performer.GetComponentInChildren<Renderer>();
                if (perfRenderer != null) spawnPos = perfRenderer.bounds.center;

                Renderer targetRenderer = target.GetComponentInChildren<Renderer>();
                if (targetRenderer != null) targetPos = targetRenderer.bounds.center;

                GameObject projGO = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
                GhostProjectile projectileScript = projGO.GetComponent<GhostProjectile>();
                if (projectileScript == null) projectileScript = projGO.AddComponent<GhostProjectile>();
                projectileScript.Setup(targetPos, projColor);
            }

            // On attend que le projectile termine sa course
            yield return new WaitForSeconds(0.6f);

            // (L'ancien bloc d'anticipation de la mort qui était ici a été supprimé)

            // 2. === DÉCLENCHEMENT SIMULTANÉ DE L'ANIMATION ET DES PARTICULES ===
            StartCoroutine(PlayImpactJuice(target, projColor));

            if (chosenImpactPrefab != null && !isFatalBlow)
            {
                Renderer targetRenderer = target.GetComponentInChildren<Renderer>();
                Vector3 impactPos = targetRenderer != null ? targetRenderer.bounds.center : target.transform.position;
                
                GameObject effectInstance = Instantiate(chosenImpactPrefab, impactPos, Quaternion.identity);
                Destroy(effectInstance, 2.0f); 
            }

            yield return new WaitForSeconds(0.25f);
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
                foreach (PlayerEntity p in turnOrder) ResolveEffect(effect, p, performer, card);
            }
            else
            {
                ResolveEffect(effect, target, performer, card);
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
                    value = effect.value,

                    turnsRemaining = 2
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

        if (!card.effects.Exists(e => e.effectType == CardData.EffectType.Mimic))
        {
            lastPlayedCard = card;
        }

    if (CheckWinCondition()) yield break; 
        AdvanceTurn();
    }

    void AdvanceTurn()
    {
        // === GESTION DE LA DURÉE DES RÈGLES SÉCURISÉE ===
        for (int i = activeRules.Count - 1; i >= 0; i--)
        {
            activeRules[i].turnsRemaining--;

            if (activeRules[i].turnsRemaining <= 0)
            {
                Debug.Log($"<color=orange>[Loi expirée]</color> {activeRules[i].ruleName} s'arrête !");
                activeRules.RemoveAt(i);
            }
        }
        // On actualise ton interface pour faire disparaître l'icône de la règle
        UpdateActiveRulesUI();

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

    void ResolveEffect(CardData.CardEffect effect, PlayerEntity target, PlayerEntity performer, CardData card)
    {
        if (target == null || performer == null) return;

        switch (effect.effectType)
        {
            case CardData.EffectType.Damage:
                int dmg = Mathf.Abs((int)effect.value);

                // A. CAS DU BOUCLIER MIROIR
                if (target.isMirrorShielded && !activeRules.Exists(r => r.effectType == CardData.EffectType.Heal))
                {
                    target.isMirrorShielded = false; 
                    performer.TakeDamage(-dmg);
                    UpdatePlayerVisualDarkness(performer);

                    if (performer.isDead)
                    {
                        // === JOUE LE SON DE MORT (RETOUR MIROIR) ===
                        if (audioSource != null) audioSource.PlayOneShot(deathSound);

                        if (deathParticlePrefab != null)
                        {
                            Renderer perfRenderer = performer.GetComponentInChildren<Renderer>();
                            Vector3 deathPos = perfRenderer != null ? perfRenderer.bounds.center : performer.transform.position;
                            GameObject deathFX = Instantiate(deathParticlePrefab, deathPos, Quaternion.identity);
                            Destroy(deathFX, 3.0f);
                        }
                        if (performer.GetComponentInChildren<Renderer>() != null)
                        {
                            performer.GetComponentInChildren<Renderer>().gameObject.SetActive(false);
                        }
                    }
                    break; 
                }

                // B. CAS NORMAL
                if (activeRules.Exists(r => r.effectType == CardData.EffectType.Heal)) target.TakeDamage(dmg);
                else target.TakeDamage(-dmg);
                
                UpdatePlayerVisualDarkness(target);

                if (target.isDead)
                {
                    // === JOUE LE SON DE MORT (ATTAQUE NORMALE OU BOT) ===
                    if (audioSource != null) audioSource.PlayOneShot(deathSound);

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
                // Un soin classique ne touche plus aux morts !
                if (!target.isDead)
                {
                    target.TakeDamage(Mathf.Abs((int)effect.value)); 
                    UpdatePlayerVisualDarkness(target);
                }
                break;

            case CardData.EffectType.Revive:
                // === EFFET REVIVE OFFICIEL ===
                if (target.isDead)
                {
                    target.isDead = false;
                    
                    // On réactive son modèle 3D sur la table
                    Renderer targetRenderer = target.GetComponentInChildren<Renderer>(true);
                    if (targetRenderer != null) targetRenderer.gameObject.SetActive(true);

                    // On lui redonne de la vie par rapport à la "Value" entrée sur ta carte
                    target.currentHealth = Mathf.Abs((int)effect.value);
                    if (target.myHealthLiquidImage != null) target.myHealthLiquidImage.fillAmount = (float)target.currentHealth / target.maxHealth;

                    UpdatePlayerVisualDarkness(target);
                    Debug.Log($"<color=green>[REVIVE]</color> {target.playerName} ressuscite avec {target.currentHealth} PV !");
                }
                break;

            case CardData.EffectType.Mimic:
                // === EFFET MIMIC OFFICIEL ===
                if (lastPlayedCard != null)
                {
                    Debug.Log($"<color=purple>[MIMIC]</color> Copie de la carte : {lastPlayedCard.cardName}");
                    // Le Mimic exécute immédiatement tous les effets de la dernière carte sur la cible actuelle !
                    foreach (var subEffect in lastPlayedCard.effects)
                    {
                        ResolveEffect(subEffect, target, performer, lastPlayedCard);
                    }
                }
                else
                {
                    Debug.Log("[MIMIC] Aucune carte n'a été jouée ce tour-ci, le sort échoue.");
                }
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
        // 1. CAS OÙ LE JOUEUR EST MORT : Écran de défaite immédiat !
        if (playerEntity != null && playerEntity.isDead)
        {
            // === NIEUW: We verbergen de grote kaart zodat de tekst zichtbaar is! ===
            if (centerCardDisplay != null) centerCardDisplay.gameObject.SetActive(false);

            timerRunning = false;
            UpdateTimerUI();
            if (resolutionPanel != null) resolutionPanel.SetActive(true);
            isResolutionPhase = true;
            
            if (resultsText != null) 
                resultsText.text = "<size=100><b>YOU ARE DEAD!</b></size>\n\n<size=50>De bots hebben gewonnen...</size>";
            
            StartCoroutine(ReturnToMainMenuRoutine());
            return true;
        }

        // 2. CAS CLASSIQUE : On compte les survivants sur la table
        List<PlayerEntity> survivors = new List<PlayerEntity>();
        if (playerEntity != null && !playerEntity.isDead) survivors.Add(playerEntity);
        foreach (PlayerEntity bot in botEntities)
        {
            if (bot != null && !bot.isDead) survivors.Add(bot);
        }

        if (survivors.Count <= 1)
        {
            // === NIEUW: We verbergen de grote kaart ook bij een overwinning! ===
            if (centerCardDisplay != null) centerCardDisplay.gameObject.SetActive(false);

            timerRunning = false;
            UpdateTimerUI();
            if (resolutionPanel != null) resolutionPanel.SetActive(true);
            isResolutionPhase = true;
            
            if (survivors.Count == 1)
            {
                if (resultsText != null) 
                    resultsText.text = "<size=100><b>VICTORY!</b></size>\n\n<size=50>" + survivors[0].playerName + " survit!</size>";
            }
            else
            {
                if (resultsText != null) 
                    resultsText.text = "<size=100><b>DRAW!</b></size>\n\n<size=50>Plus personne n'est en vie!</size>";
            }

            StartCoroutine(ReturnToMainMenuRoutine());
            return true;
        }
        return false;
    }

    System.Collections.IEnumerator ReturnToMainMenuRoutine()
    {
        yield return new WaitForSeconds(4.0f);
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu"); 
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
                    if (audioSource != null) audioSource.PlayOneShot(deathSound);

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
    // === JUICE EFFECT MIS À JOUR (COULEUR DYNAMIQUE SELON L'ACTION) ===
    System.Collections.IEnumerator PlayImpactJuice(PlayerEntity target, Color flashColor)
    {
        if (target == null) yield break;

        Transform rootTransform = target.transform;
        Vector3 originalScale = rootTransform.localScale;
        Vector3 originalPosition = rootTransform.position;

        Renderer[] allRenderers = target.GetComponentsInChildren<Renderer>();

        // 1. FLASH DE COULEUR PERSONNALISÉ (Rouge, Jaune, Vert...) sur tous les morceaux
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
                    // On applique la couleur reçue en paramètre à la place du rouge fixe !
                    ren.material.SetColor(colorPropName, flashColor); 
                }
            }
        }

        // 2. BOUCLE DE REBOND PHYSICS (0.25 seconde)
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

    // === ANIMATION RAPIDE ET SOFT "YOUR TURN" (1 SECONDE PILE) ===
    System.Collections.IEnumerator PlayYourTurnVisualRoutine()
    {
        if (resolutionPanel != null) resolutionPanel.SetActive(true);
        isResolutionPhase = true;
        if (playerHandUI != null) playerHandUI.SetActive(false); 

        if (resultsText != null)
        {
            resultsText.text = "YOUR TURN";
            RectTransform textRect = resultsText.rectTransform;

            // EFFET SOFT : Le texte commence légèrement plus petit (70%) et va glisser doucement vers 100%
            textRect.localScale = new Vector3(0.7f, 0.7f, 1f);
            
            float duration = 1.0f; // Durée totale divisée par deux !
            float time = 0f;
            while (time < duration)
            {
                time += Time.deltaTime;
                float t = time / duration;

                // Durant les premiers 20% du temps (0.2s), on applique une transition fluide (Smooth)
                if (t < 0.2f)
                {
                    float scaleT = t / 0.2f;
                    textRect.localScale = Vector3.Lerp(new Vector3(0.7f, 0.7f, 1f), Vector3.one, Mathf.Sin(scaleT * Mathf.PI * 0.5f));
                }
                else
                {
                    textRect.localScale = Vector3.one;
                }
                yield return null;
            }
            textRect.localScale = Vector3.one;
        }
        else
        {
            yield return new WaitForSeconds(1.0f);
        }

        // On lance la main du joueur
        InitializeHumanTurnVisuals();
    }
}