using UnityEngine;

public class TargetableEntity : MonoBehaviour
{
    public PlayerEntity associatedEntity; 

    void Start()
    {
        if (associatedEntity == null) associatedEntity = GetComponent<PlayerEntity>();
    }

    void OnMouseDown()
    {
        GameManager gm = Object.FindFirstObjectByType<GameManager>();
        if (gm == null || gm.selectedCard == null) return;

        // Détection intelligente : la carte est-elle une résurrection ?
        bool isReviveCard = gm.selectedCard.cardName.ToLower().Contains("revive") || 
                            gm.selectedCard.description.ToLower().Contains("revive");

        if (isReviveCard)
        {
            // Sécurité carte Revive : Interdit de cliquer sur un joueur vivant !
            if (associatedEntity == null || !associatedEntity.isDead) return;
        }
        else
        {
            // Sécurité carte Normale : Interdit de cliquer sur un fantôme mort !
            if (associatedEntity != null && associatedEntity.isDead) return;
        }

        if (gm.selectedCard.targetMode == CardData.TargetMode.Chosen)
        {
            gm.SetSelectedTarget(associatedEntity);
        }
    }
}