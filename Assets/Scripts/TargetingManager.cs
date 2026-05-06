using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class TargetingManager : MonoBehaviour
{
    public Image arrowImage;
    public List<Transform> playerTargets; 
    public int currentTargetIndex = 1; 
    
    public bool isTargeting = false;
    public bool isLocked = false;

    void Start()
    {
        // On s'assure que la flèche est cachée dès le début de la partie
        ResetArrow(); 
    }

    public void StartTargeting()
    {
        Debug.Log("Ciblage activé !");
        isTargeting = true;
        isLocked = false;
        
        // FORCE LA COULEUR ROUGE au départ
        if (arrowImage != null) arrowImage.color = Color.red; 
        
        arrowImage.gameObject.SetActive(true);
        UpdateArrowPosition();
    }

    void Update()
    {
        if (!isTargeting || isLocked) return;

        if (Input.mousePosition.x < Screen.width * 0.33f) currentTargetIndex = 0; 
        else if (Input.mousePosition.x > Screen.width * 0.66f) currentTargetIndex = 2; 
        else currentTargetIndex = 1; 

        UpdateArrowPosition();

        if (Input.GetMouseButtonDown(0))
        {
            LockTarget();
        }
    }

    [Header("Ajustements Visuels")]
    public float heightOffset3D = 0.5f; 
    public float sideOffsetX = 350f;    // Augmenté pour l'éloigner sur les côtés
    public float topOffsetY = 300f;     // Augmenté pour l'éloigner vers le haut

    void UpdateArrowPosition()
    {
        if (playerTargets.Count <= currentTargetIndex || playerTargets[currentTargetIndex] == null) return;

        // 1. Calcul de la position 2D à partir du monde 3D
        Vector3 worldPos = playerTargets[currentTargetIndex].position + Vector3.up * heightOffset3D;
        Vector3 screenPoint = Camera.main.WorldToScreenPoint(worldPos);

        float finalX = screenPoint.x;
        float finalY = screenPoint.y;
        float rotationZ = 0f;

        // 2. Logique spécifique par personnage
        if (currentTargetIndex == 1) // FANTÔME DU MILIEU
        {
            finalY = screenPoint.y + topOffsetY; 
            // Si ta flèche pointe vers le haut par défaut, 180 la fait pointer vers le BAS
            rotationZ = 180f; 
        }
        else if (currentTargetIndex == 0) // FANTÔME DE GAUCHE
        {
            finalX = screenPoint.x - sideOffsetX; 
            rotationZ = -90f; // Pointe vers la DROITE
        }
        else if (currentTargetIndex == 2) // FANTÔME DE DROITE
        {
            finalX = screenPoint.x + sideOffsetX; 
            rotationZ = 90f; // Pointe vers la GAUCHE
        }

        // 3. Application
        arrowImage.rectTransform.position = new Vector2(finalX, finalY);
        arrowImage.rectTransform.rotation = Quaternion.Euler(0, 0, rotationZ);
    }

    void LockTarget()
    {
        isLocked = true;
        arrowImage.color = Color.green; 
        Debug.Log("Cible verrouillée : " + playerTargets[currentTargetIndex].name);
    }

    public void ResetArrow()
    {
        isTargeting = false;
        isLocked = false; 
        if (arrowImage != null) arrowImage.gameObject.SetActive(false);
    }
}