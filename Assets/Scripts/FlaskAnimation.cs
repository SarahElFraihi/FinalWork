using UnityEngine;
using UnityEngine.UI;

public class HealthBarJuice : MonoBehaviour
{
    private Image liquidImage;
    private GameManager gameManager;

    [Header("Couleurs Cartoon Flashy")]
    // Un mauve néon très saturé qui va "popper" avec ton éclairage
    public Color healthyMauve = new Color(0.85f, 0.2f, 1.0f); 
    // Un violet très sombre, presque "encre", pour l'effet de vide
    public Color dangerDark = new Color(0.15f, 0.0f, 0.2f); 

    [Header("Animation de Respiration")]
    public float swaySpeed = 2.0f;
    public float swayAngle = 1.5f; 

    void Start()
    {
        liquidImage = GetComponent<Image>();
        gameManager = Object.FindFirstObjectByType<GameManager>();
        
        // Petit tips pour le cartoon : on force la saturation au max
        liquidImage.type = Image.Type.Filled;
    }

    void Update()
    {
        if (gameManager == null) return;

        // On récupère le pourcentage de vie (0.0 à 1.0)
        float healthPercent = (float)gameManager.currentHealth / (float)gameManager.maxHealth;

        // Remplissage fluide (Lerp pour éviter les sauts brusques)
        liquidImage.fillAmount = Mathf.Lerp(liquidImage.fillAmount, healthPercent, Time.deltaTime * 4f);

        // Transition de couleur "Punchy"
        // On utilise une courbe pour que la couleur reste "belle" longtemps avant de noircir
        liquidImage.color = Color.Lerp(dangerDark, healthyMauve, Mathf.Pow(healthPercent, 0.7f));

        // Animation de flottement (assure-toi que le Pivot Y de l'image est à 0)
        float angle = Mathf.Sin(Time.time * swaySpeed) * swayAngle;
        transform.localRotation = Quaternion.Euler(0, 0, angle);
    }
}