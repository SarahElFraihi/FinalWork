using UnityEngine;

public class TargetableEntity : MonoBehaviour
{
    // Référence vers les données de ce joueur/bot (vie, or, etc.)
    public PlayerEntity associatedEntity; 

    void Start()
    {
        if (associatedEntity == null)
        {
            associatedEntity = GetComponent<PlayerEntity>();
        }
    }

    // Cette fonction s'active automatiquement quand on clique sur le Collider du fantôme
    void OnMouseDown()
    {
        GameManager gm = Object.FindFirstObjectByType<GameManager>();
        
        // On ne peut choisir une cible que si le jeu attend une action
        if (gm != null && gm.selectedCard != null && !gm.isResolutionPhase)
        {
            if (gm.selectedCard.targetMode == CardData.TargetMode.Chosen)
            {
                // On transmet la cible choisie au GameManager !
                gm.SetSelectedTarget(associatedEntity);
            }
        }
    }
}