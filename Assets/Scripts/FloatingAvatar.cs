using UnityEngine;

public class UltraSmoothAvatar : MonoBehaviour
{
    private float floatSpeed;
    private float squashSpeed;
    private float height;
    private float offset;
    private Vector3 startPos;
    private Vector3 startScale;

    [Header("Réglages Invisibles")]
    // On descend à 0.005 pour que l'étirement soit à peine perceptible
    public float squashAmount = 0.005f; 

    void Start()
    {
        startPos = transform.position;
        startScale = transform.localScale;

        // On sépare les vitesses pour que le mouvement soit moins prévisible
        floatSpeed = Random.Range(0.5f, 1.0f);   // Flottement très lent
        squashSpeed = Random.Range(1.2f, 1.8f);  // Respiration un peu plus rapide
        height = Random.Range(0.005f, 0.015f);  // Hauteur divisée par 2
        offset = Random.Range(0f, 500f);        // Décalage massif
    }

    void Update()
    {
        // 1. Flottement vertical ultra-calme
        float floatSin = Mathf.Sin((Time.time + offset) * floatSpeed);
        transform.position = startPos + new Vector3(0, floatSin * height, 0);

        // 2. Respiration (Squash) désynchronisée
        // En utilisant un multiplicateur différent, le perso ne s'étire pas pile quand il est en haut
        float squashSin = Mathf.Sin((Time.time + offset) * squashSpeed);
        
        float stretchY = 1f + (squashSin * squashAmount);
        float squashXZ = 1f - (squashSin * squashAmount * 0.5f);

        transform.localScale = new Vector3(
            startScale.x * squashXZ,
            startScale.y * stretchY,
            startScale.z * squashXZ
        );
    }
}