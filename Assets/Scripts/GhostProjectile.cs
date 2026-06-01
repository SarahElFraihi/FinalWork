using UnityEngine;

public class GhostProjectile : MonoBehaviour
{
    [Header("Configuration")]
    public float speed = 7f; 

    [Tooltip("Modifie cet angle (ex: 90, -90, 180) si ta flèche vole de côté !")]
    public float rotationOffsetZ = 0f; 

    private Vector3 targetWorldPosition;
    private bool isInitialized = false;
    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Setup(Vector3 targetPos, Color projectileColor)
    {
        targetWorldPosition = targetPos;
        isInitialized = true;
        
        if (spriteRenderer != null)
        {
            spriteRenderer.color = projectileColor;
        }
    }

    void Update()
    {
        if (!isInitialized) return;

        Vector3 direction = targetWorldPosition - transform.position;
        float distanceThisFrame = speed * Time.deltaTime;

        if (direction.magnitude <= distanceThisFrame)
        {
            transform.position = targetWorldPosition;
            Destroy(gameObject);
            return;
        }

        transform.position = Vector3.MoveTowards(transform.position, targetWorldPosition, distanceThisFrame);

        if (Camera.main != null && direction != Vector3.zero)
        {
            // 1. On applique la rotation face à la caméra et vers le bot
            Quaternion baseRotation = Quaternion.LookRotation(-Camera.main.transform.forward, direction.normalized);
            
            // 2. CORRECTION : On injecte ton angle personnalisé pour redresser le dessin !
            transform.rotation = baseRotation * Quaternion.Euler(0, 0, rotationOffsetZ);
        }
    }
}