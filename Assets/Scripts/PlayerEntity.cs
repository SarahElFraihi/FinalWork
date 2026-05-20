using UnityEngine;
using System.Collections.Generic;

public class PlayerEntity : MonoBehaviour
{
    [Header("Identité")]
    public string playerName;
    public bool isBot = true;

    [Header("Statistiques de Base")]
    public int maxHealth = 100;
    public int currentHealth = 100;
    public int gold = 0;
    public int karma = 50; // 0 = Démon, 100 = Ange
    public int luck = 50;  // Influence certains événements

    [Header("États Actifs (Bools)")]
    public bool isDead = false;
    public bool isShielded = false;
    public bool isMirrorShielded = false;
    public bool isFrozen = false;    // Saute son tour
    public bool isPoisoned = false;  // Perd des PV chaque tour
    public bool isOnFire = false;    // Dégâts + visuel flammes
    public bool isInvisible = false; // Ne peut pas être ciblé directement
    public bool isSilenced = false;  // Ne peut plus jouer de cartes Special
    public bool isConfused = false;  // Cible au hasard
    public bool isWanted = false;    // Donne de l'argent à celui qui l'attaque
    public bool isLinked = false;    // Partage ses dégâts avec un voisin

    [Header("Modificateurs")]
    public int thorns = 0;           // Renvoie X dégâts à l'attaquant
    public int handSize = 5;         // Nombre de cartes piochées
    public float defenseMultiplier = 1f; // 0.5 = double défense, 2.0 = vulnérable
    public float individualTimerMod = 0f; // Pour réduire le temps d'un seul joueur

    [Header("Position à Table")]
    public PlayerEntity leftNeighbor;
    public PlayerEntity rightNeighbor;
    public PlayerEntity oppositePlayer;

    [Header("Couleurs de la Fiole")]
    public Color fullHealthColor = Color.red; // Ta belle couleur de base
    public Color lowHealthColor = new Color(0.2f, 0.05f, 0.05f); // La version très sombre

    void Start()
    {
        currentHealth = maxHealth;

        if (myHealthLiquidImage != null)
        {
            myHealthLiquidImage.fillAmount = 1f;
            myHealthLiquidImage.color = fullHealthColor;
        }
    }

    void Update()
    {
        if (!isBot && myHealthLiquidImage != null)
        {
            float healthRatio = (float)currentHealth / maxHealth;
            myHealthLiquidImage.fillAmount = healthRatio;
            myHealthLiquidImage.color = Color.Lerp(lowHealthColor, fullHealthColor, healthRatio);
        }
    }

    [Header("UI Spécifique Entité")]
    public UnityEngine.UI.Image myHealthLiquidImage; 

    public void TakeDamage(int amount)
    {
        if (isDead) return;

        if (amount < 0) 
        {
            if (isShielded) { isShielded = false; return; }
            amount = Mathf.RoundToInt(amount * defenseMultiplier);
        }

        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (myHealthLiquidImage != null)
        {
            myHealthLiquidImage.fillAmount = (float)currentHealth / maxHealth;
        }

        if (isLinked && amount < 0)
        {
            isLinked = false;
            if (rightNeighbor != null) rightNeighbor.TakeDamage(amount / 2);
        }

        if (currentHealth <= 0) Die();
    }

    void Die()
    {
        isDead = true;
        Debug.Log(playerName + " est éliminé !");
        // On pourra ajouter ici un effet de transparence ou une animation
    }
}