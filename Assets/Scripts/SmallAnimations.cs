using UnityEngine;

public class SmallAnimations : MonoBehaviour
{
    [Header("Référence du Modèle")]
    public Transform modelTransform; 

    private float nextAnimationTime;
    private Quaternion originalRotation;
    private bool isPlayingAnimation = false;
    private PlayerEntity playerEntity;

    [Header("Configuration Festival")]
    public float minWaitTime = 6f;  
    public float maxWaitTime = 12f; 

    void Start()
    {
        if (modelTransform == null) modelTransform = transform;
        originalRotation = modelTransform.localRotation;
        playerEntity = GetComponent<PlayerEntity>();
        
        ScheduleNextAnimation();
    }

    void Update()
    {
        if (playerEntity != null && playerEntity.isDead) return;

        GameManager gm = Object.FindFirstObjectByType<GameManager>();
        if (gm != null && gm.isResolutionPhase) return;

        if (!isPlayingAnimation && Time.time >= nextAnimationTime)
        {
            TriggerRandomAnimation();
        }
    }

    void ScheduleNextAnimation()
    {
        nextAnimationTime = Time.time + Random.Range(minWaitTime, maxWaitTime);
    }

    void TriggerRandomAnimation()
    {
        int choice = Random.Range(0, 2); 
        if (choice == 0) StartCoroutine(PossessedTwitchRoutine());
        else StartCoroutine(ParanoidLookRoutine());
    }

    // --- ANIMATION 1 : Le Twitch Possédé (Saccadé et creepy) ---
    System.Collections.IEnumerator PossessedTwitchRoutine()
    {
        isPlayingAnimation = true;
        float duration = 0.4f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            
            // On crée des sauts brusques et aléatoires sur les axes X et Z
            // Ça donne un effet "poupée hantée" ou bug de réalité
            float twitchX = Random.Range(-15f, 15f);
            float twitchZ = Random.Range(-15f, 15f);
            
            modelTransform.localRotation = originalRotation * Quaternion.Euler(twitchX, 0, twitchZ);
            
            // On attend une fraction de seconde avant le prochain glitch visuel
            yield return new WaitForSeconds(0.05f); 
        }

        modelTransform.localRotation = originalRotation; 
        isPlayingAnimation = false;
        ScheduleNextAnimation();
    }

    // --- ANIMATION 2 : Le Regard Paranoïaque (Inquiet face à l'IA) ---
    System.Collections.IEnumerator ParanoidLookRoutine()
    {
        isPlayingAnimation = true;
        
        // Étape A : Regard brusque à GAUCHE (-45 degrés)
        modelTransform.localRotation = originalRotation * Quaternion.Euler(0, -45f, 0);
        yield return new WaitForSeconds(0.25f); // Bloque le regard un instant

        // Étape B : Regard brusque à DROITE (+45 degrés)
        modelTransform.localRotation = originalRotation * Quaternion.Euler(0, 45f, 0);
        yield return new WaitForSeconds(0.25f);

        // Étape C : Retour à la normale
        modelTransform.localRotation = originalRotation; 
        
        isPlayingAnimation = false;
        ScheduleNextAnimation();
    }
}