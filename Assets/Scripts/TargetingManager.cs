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

    void Start() { ResetArrow(); }

    public void StartTargeting()
    {
        isTargeting = true;
        isLocked = false;
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

        // Adaptation intelligente de la flèche selon le type de carte
        RedirectBasedOnCardContext();

        UpdateArrowPosition();

        if (Input.GetMouseButtonDown(0)) LockTarget();
    }

    private void RedirectBasedOnCardContext()
    {
        if (playerTargets == null || playerTargets.Count == 0) return;

        GameManager gm = Object.FindFirstObjectByType<GameManager>();
        if (gm == null || gm.selectedCard == null) return;

        bool isReviveCard = gm.selectedCard.cardName.ToLower().Contains("revive") || 
                            gm.selectedCard.description.ToLower().Contains("revive");

        // Détermine si une cible à un index donné est invalide
        System.Func<int, bool> IsInvalid = (idx) =>
        {
            if (idx < 0 || idx >= playerTargets.Count || playerTargets[idx] == null) return true;
            PlayerEntity pe = playerTargets[idx].GetComponent<PlayerEntity>();
            if (pe == null) pe = playerTargets[idx].GetComponentInParent<PlayerEntity>();
            if (pe == null) return true;

            return isReviveCard ? !pe.isDead : pe.isDead; 
        };

        // Si la bande survolée est invalide, on glisse vers une bande valide
        if (IsInvalid(currentTargetIndex))
        {
            for (int i = 0; i < playerTargets.Count; i++)
            {
                int testIndex = (i == 0) ? 1 : (i == 1 ? 0 : 2); // Test Milieu, puis Gauche, puis Droite
                if (!IsInvalid(testIndex))
                {
                    currentTargetIndex = testIndex;
                    break;
                }
            }
        }
    }

    void LockTarget()
    {
        isLocked = true;
        if (arrowImage != null) arrowImage.color = Color.green; 

        // SYNCHRONISATION CRUCIALE : On force la cible de la flèche dans le GameManager !
        GameManager gm = Object.FindFirstObjectByType<GameManager>();
        if (gm != null && playerTargets[currentTargetIndex] != null)
        {
            PlayerEntity targetPE = playerTargets[currentTargetIndex].GetComponent<PlayerEntity>();
            if (targetPE == null) targetPE = playerTargets[currentTargetIndex].GetComponentInParent<PlayerEntity>();
            
            if (targetPE != null) gm.SetSelectedTarget(targetPE);
        }
    }

    [Header("Ajustements Visuels")]
    public float heightOffset3D = 0.5f; 
    public float sideOffsetX = 350f;    
    public float topOffsetY = 300f;     

    void UpdateArrowPosition()
    {
        if (playerTargets.Count <= currentTargetIndex || playerTargets[currentTargetIndex] == null) return;

        Vector3 worldPos = playerTargets[currentTargetIndex].position + Vector3.up * heightOffset3D;
        Vector3 screenPoint = Camera.main.WorldToScreenPoint(worldPos);

        float finalX = screenPoint.x;
        float finalY = screenPoint.y;
        float rotationZ = 0f;

        if (currentTargetIndex == 1) { finalY = screenPoint.y + topOffsetY; rotationZ = 180f; }
        else if (currentTargetIndex == 0) { finalX = screenPoint.x - sideOffsetX; rotationZ = -90f; }
        else if (currentTargetIndex == 2) { finalX = screenPoint.x + sideOffsetX; rotationZ = 90f; }

        if (arrowImage != null)
        {
            arrowImage.rectTransform.position = new Vector2(finalX, finalY);
            arrowImage.rectTransform.rotation = Quaternion.Euler(0, 0, rotationZ);
        }
    }

    public void ResetArrow()
    {
        isTargeting = false;
        isLocked = false; 
        if (arrowImage != null) arrowImage.gameObject.SetActive(false);
    }
}