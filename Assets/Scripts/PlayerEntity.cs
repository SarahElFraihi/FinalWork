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

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;

        if (amount < 0) 
        {
            if (isShielded) { isShielded = false; return; }
            amount = Mathf.RoundToInt(amount * defenseMultiplier);

            // THORN : Si le joueur a des épines, il renvoie un peu de dégâts (Logiquement géré ici)
            if (thorns > 0) Debug.Log(playerName + " reflects damage via Thorns!");
        }

        currentHealth += amount; 
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        // LINK : Si lié, le voisin prend aussi cher
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