using UnityEngine;

public class UltraSmoothAvatar : MonoBehaviour
{
    [Header("Référence du Modèle")]
    public Transform modelTransform; // Glisse ton fantôme ici dans l'Inspector

    private float floatSpeed;
    private float squashSpeed;
    private float height;
    private float offset;
    private Vector3 startPos;
    private Vector3 startScale;

    [Header("Réglages Invisibles")]
    public float squashAmount = 0.005f; 

    void Start()
    {
        if (modelTransform == null)
        {
            Debug.LogError("Oulà ! Tu as oublié de glisser le modèle 3D dans le script sur l'objet Effects.");
            return;
        }

        // On enregistre les données de départ du MODÈLE, pas de l'objet Effects
        startPos = modelTransform.localPosition;
        startScale = modelTransform.localScale;

        floatSpeed = Random.Range(0.5f, 1.0f);
        squashSpeed = Random.Range(1.2f, 1.8f);
        height = Random.Range(0.005f, 0.015f);
        offset = Random.Range(0f, 500f);
    }

    void Update()
    {
        if (modelTransform == null) return;

        // 1. Flottement vertical appliqué au modèle
        float floatSin = Mathf.Sin((Time.time + offset) * floatSpeed);
        modelTransform.localPosition = startPos + new Vector3(0, floatSin * height, 0);

        // 2. Respiration (Squash) appliquée au modèle
        float squashSin = Mathf.Sin((Time.time + offset) * squashSpeed);
        
        float stretchY = 1f + (squashSin * squashAmount);
        float squashXZ = 1f - (squashSin * squashAmount * 0.5f);

        modelTransform.localScale = new Vector3(
            startScale.x * squashXZ,
            startScale.y * stretchY,
            startScale.z * squashXZ
        );
    }
}