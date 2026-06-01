using UnityEngine;

public class TargetableEntity : MonoBehaviour
{
    public PlayerEntity associatedEntity; 

    void Start()
    {
        if (associatedEntity == null)
        {
            associatedEntity = GetComponent<PlayerEntity>();
        }
    }

    void OnMouseDown()
    {
        GameManager gm = Object.FindFirstObjectByType<GameManager>();
        
        if (gm != null && gm.selectedCard != null)
        {
            if (gm.selectedCard.targetMode == CardData.TargetMode.Chosen)
            {
                gm.SetSelectedTarget(associatedEntity);
            }
        }
    }
}