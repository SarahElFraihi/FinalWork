using UnityEngine;
using UnityEngine.UI; // Obligatoire pour gérer la barre de vie

public class PlayerEntity : MonoBehaviour
{
    [Header("Identité")]
    public string playerName;
    public bool isBot = true;

    [Header("Statistiques")]
    public int maxHealth = 100;
    public int currentHealth = 100;

    [Header("États")]
    public bool isShielded = false;
    public bool isMirrorShielded = false;

    [Header("Interface Visuelle")]
    public Image healthBar; // Glisse l'image de la barre de vie ici

    void Start()
    {
        currentHealth = maxHealth;
        UpdateUI();
    }

    public void TakeDamage(int amount)
    {
        // Logique de bouclier
        if (amount > 0) // Si c'est une attaque
        {
            if (isShielded)
            {
                isShielded = false;
                Debug.Log(playerName + " a bloqué l'attaque avec son bouclier !");
                return;
            }
        }

        // Application des dégâts ou soins
        currentHealth -= amount; 
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        UpdateUI();
        
        if (currentHealth <= 0)
        {
            Debug.Log(playerName + " est KO !");
        }
    }

    // Voici la méthode qui manquait !
    public void UpdateUI()
    {
        if (healthBar != null)
        {
            // Calcul du ratio pour le fillAmount (entre 0 et 1)
            healthBar.fillAmount = (float)currentHealth / maxHealth;
        }
    }
}