using UnityEngine;
using System.Collections.Generic;

public class MansionEffects : MonoBehaviour
{
    public static MansionEffects Instance;

    [Header("Haunted Mansion References")]
    [Tooltip("Glisse ici tes 4 cadres de tableaux depuis la hiérarchie.")]
    public List<Transform> paintings = new List<Transform>();
    
    [Tooltip("Glisse l'objet parent 'Candlestick' ici.")]
    public Transform centerCandle;
    
    [Tooltip("Glisse le GameObject parent 'Lights' qui contient tes Monster_Lamps.")]
    public GameObject backgroundLightsParent;

    [Header("Stage 1: Bougeoir (Candle) Settings")]
    public float candleFloatHeight = 0.2f;       // Hauteur de l'envol (en mètres, ex: 0.2 = 20cm)
    public float candleRiseDuration = 1.2f;      // Temps pour monter au départ (en secondes)
    public float candleSwaySpeed = 2.5f;         // Vitesse du balancement gauche/droite
    public float candleSwayIntensity = 0.1f;     // Amplitude du balancement gauche/droite
    public float candleBobbingSpeed = 1.2f;      // Vitesse du flottement haut/bas
    public float candleBobbingIntensity = 0.03f; // Amplitude du flottement haut/bas
    public float candleRotationAngle = 18f;      // Angle max d'inclinaison lors du balancement

    [Header("Stage 1: Clignotement (Flicker) Settings")]
    public float flickerMinPercent = 0.2f;       // Intensité minimale (0.2 = 20% de la puissance d'origine)
    public float flickerMaxPercent = 1.1f;       // Intensité maximale (1.1 = 110% de la puissance d'origine)
    public float flickerMinWait = 0.06f;         // Temps minimum entre deux clignotements (en secondes)
    public float flickerMaxWait = 0.2f;          // Temps maximum entre deux clignotements (en secondes)

    [Header("Stage 2: Tableaux (Paintings) Settings")]
    public float paintingsFloatHeight = 0.6f;    // Hauteur de l'envol des tableaux
    public float paintingsHoverSpeed = 1.8f;     // Vitesse du flottement vertical des tableaux
    public float paintingsHoverIntensity = 0.45f;// Amplitude du flottement vertical des tableaux
    public float paintingsDriftSpeed = 1.1f;     // Vitesse de la dérive horizontale des tableaux
    public float paintingsDriftIntensity = 0.2f; // Amplitude de la dérive horizontale des tableaux
    public float paintingsTiltSpeed = 1.4f;      // Vitesse de l'inclinaison creepy des tableaux
    public float paintingsTiltIntensity = 12f;   // Angle max de torsion des tableaux

    [Header("Stage 2: Obscurité (Darkness) Settings")]
    [Range(0f, 1f)]
    public float stage2DarknessMultiplier = 0.25f; // La pièce devient sombre (0.25 = 25% de la lumière d'origine)

    // Variables internes techniques
    private List<Vector3> paintingStartPositions = new List<Vector3>();
    private List<Quaternion> paintingStartRotations = new List<Quaternion>();
    private List<float> paintingOffsets = new List<float>();
    private List<Light> bgLights = new List<Light>();
    private List<float> baseIntensities = new List<float>();
    
    private int playerHitCount = 0;
    private bool isCandleFloating = false;
    private bool isPaintingsFloating = false;
    private float roomDarknessMultiplier = 1.0f; 

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        foreach (Transform t in paintings)
        {
            if (t != null)
            {
                paintingStartPositions.Add(t.localPosition);
                paintingStartRotations.Add(t.localRotation);
                paintingOffsets.Add(Random.Range(0f, 100f));
            }
        }

        if (backgroundLightsParent != null)
        {
            Light[] componentLights = backgroundLightsParent.GetComponentsInChildren<Light>();
            foreach (Light l in componentLights)
            {
                bgLights.Add(l);
                baseIntensities.Add(l.intensity);
            }
        }
    }

    public void TriggerNextMansionStage()
    {
        playerHitCount++;

        if (playerHitCount == 1)
        {
            if (centerCandle != null && !isCandleFloating) 
                StartCoroutine(CandleFloatAndSwayRoutine());
            
            if (bgLights.Count > 0) 
                StartCoroutine(LightsFlickerRoutine());
        }
        else if (playerHitCount == 2)
        {
            roomDarknessMultiplier = stage2DarknessMultiplier; // Utilise la variable de l'inspecteur
            
            if (paintings.Count > 0 && !isPaintingsFloating) 
                StartCoroutine(PaintingsFloatRoutine());
        }
    }

    System.Collections.IEnumerator CandleFloatAndSwayRoutine()
    {
        isCandleFloating = true;
        Vector3 startPos = centerCandle.localPosition;
        Vector3 targetPos = startPos + Vector3.up * candleFloatHeight; // Utilise la hauteur de l'inspecteur
        
        float elapsed = 0f;
        while (elapsed < candleRiseDuration)
        {
            elapsed += Time.deltaTime;
            centerCandle.localPosition = Vector3.Lerp(startPos, targetPos, Mathf.SmoothStep(0f, 1f, elapsed / candleRiseDuration));
            yield return null;
        }

        float runningTime = 0f;
        while (isCandleFloating)
        {
            runningTime += Time.deltaTime;
            
            float swayX = Mathf.Sin(runningTime * candleSwaySpeed) * candleSwayIntensity; 
            float bobbingY = Mathf.Sin(runningTime * candleBobbingSpeed) * candleBobbingIntensity; 
            
            centerCandle.localPosition = targetPos + new Vector3(swayX, bobbingY, 0f);
            centerCandle.localRotation = Quaternion.Euler(0f, 0f, swayX * candleRotationAngle); 
            yield return null;
        }
    }

    System.Collections.IEnumerator LightsFlickerRoutine()
    {
        while (true) 
        {
            for (int i = 0; i < bgLights.Count; i++)
            {
                if (bgLights[i] != null)
                {
                    float currentMaxIntensity = baseIntensities[i] * roomDarknessMultiplier;
                    bgLights[i].intensity = Random.Range(currentMaxIntensity * flickerMinPercent, currentMaxIntensity * flickerMaxPercent);
                }
            }
            yield return new WaitForSeconds(Random.Range(flickerMinWait, flickerMaxWait)); 
        }
    }

    System.Collections.IEnumerator PaintingsFloatRoutine()
    {
        isPaintingsFloating = true;
        float floatTime = 0f;

        while (isPaintingsFloating)
        {
            floatTime += Time.deltaTime;

            for (int i = 0; i < paintings.Count; i++)
            {
                if (paintings[i] == null) continue;

                float uniqueTime = floatTime + paintingOffsets[i];
                
                float hoverY = Mathf.Sin(uniqueTime * paintingsHoverSpeed) * paintingsHoverIntensity; 
                float driftX = Mathf.Cos(uniqueTime * paintingsDriftSpeed) * paintingsDriftIntensity;  
                float tiltZ = Mathf.Sin(uniqueTime * paintingsTiltSpeed) * paintingsTiltIntensity;    

                paintings[i].localPosition = paintingStartPositions[i] + new Vector3(driftX, hoverY + paintingsFloatHeight, 0f);
                paintings[i].localRotation = paintingStartRotations[i] * Quaternion.Euler(0f, 0f, tiltZ);
            }
            yield return null;
        }
    }
}