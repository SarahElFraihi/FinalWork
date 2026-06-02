using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewCard", menuName = "Game/CardData")]
public class CardData : ScriptableObject
{
    public string cardName;
    [TextArea] public string description;

    public enum CardType { Action, Rule, Special }
    public CardType type;

    [Header("Visual Settings")]
    public Color cardColor; 

    // --- LA BIBLIOTHÈQUE D'EFFETS COMPLÈTE ---
    public enum EffectType { 
        Damage, Heal, Gold, Karma, Luck, 
        Frozen, Burn, Poison, // États
        Shield, Invisible, Silenced, Wanted, Linked, // Bools
        Thorns, HandSize, TimerMod, // Modificateurs
        StealCard, Duplicate, // Actions complexes
        GravityFlip, // Règle spéciale
        Revive,
        Mimic,
        MirrorShield
    }
    
    [System.Serializable]
    public class CardEffect
    {
        public EffectType effectType;
        public float value; 
    }

    public List<CardEffect> effects = new List<CardEffect>();

    [Header("Targeting")]
    public bool requiresTarget;
    public enum TargetMode { None, Chosen, Left, Right, Opposite, Self, Everyone }    
    public TargetMode targetMode;

    private void OnValidate()
    {
        requiresTarget = (targetMode == TargetMode.Chosen);
        
        switch (type)
        {
            case CardType.Action: cardColor = new Color(0.9f, 0.1f, 0.1f); break;
            case CardType.Rule: cardColor = new Color(1f, 0.85f, 0f); break;
            case CardType.Special: cardColor = new Color(0.6f, 0.1f, 0.9f); break;
        }
    }
}