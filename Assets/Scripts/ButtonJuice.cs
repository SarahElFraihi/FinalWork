using UnityEngine;
using UnityEngine.EventSystems; // Très important pour détecter la souris !

public class ButtonJuice : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private RectTransform rectTransform;
    private Vector3 originalScale;
    private Vector3 targetScale;
    private Quaternion targetRotation;

    [Header("Réglages du Hover")]
    public float scaleFactor = 1.15f; // Grossit de 15% au survol
    public float wiggleAngle = 5f;    // Se penche de 5 degrés
    public float lerpSpeed = 12f;     // Vitesse de l'animation fluide

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        originalScale = rectTransform.localScale;
        
        // Au début, les cibles sont les valeurs normales
        targetScale = originalScale;
        targetRotation = Quaternion.identity;
    }

    void Update()
    {
        // On anime en continu vers la cible de manière ultra fluide (Lerp)
        rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, targetScale, Time.deltaTime * lerpSpeed);
        rectTransform.localRotation = Quaternion.Lerp(rectTransform.localRotation, targetRotation, Time.deltaTime * lerpSpeed);
    }

    // Déclenché AUTOMATIQUEMENT quand la souris entre sur le bouton
    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = originalScale * scaleFactor;
        targetRotation = Quaternion.Euler(0, 0, wiggleAngle); // Petit effet penché stylé
    }

    // Déclenché AUTOMATIQUEMENT quand la souris sort du bouton
    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = originalScale;
        targetRotation = Quaternion.identity; // Retour à la normale
    }
}