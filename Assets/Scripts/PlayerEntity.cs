using UnityEngine;

public class PlayerEntity : MonoBehaviour
{
    [Header("Identité")]
    public string playerName;
    public bool isBot = true;

    [Header("Statistiques")]
    public int maxHealth = 100;
    public int currentHealth = 100;

    [Header("Position à Table")]
    public PlayerEntity leftNeighbor;
    public PlayerEntity rightNeighbor;
    public PlayerEntity oppositePlayer;

    [Header("États")]
    public bool isShielded = false;
    public bool isMirrorShielded = false;

    // Suppression de l'UpdateUI et de la healthBar pour garder la vie secrète
    
    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        // Logique de bouclier (Shield)
        if (amount < 0 && isShielded) // Si c'est une attaque (valeur négative)
        {
            isShielded = false;
            Debug.Log(playerName + " a bloqué avec son bouclier !");
            return;
        }

        // Application des dégâts ou soins
        currentHealth += amount; 
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (currentHealth <= 0)
        {
            Debug.Log(playerName + " est KO !");
            // Ici tu pourrais désactiver le fantôme ou lancer une animation de disparition
        }
    }
}