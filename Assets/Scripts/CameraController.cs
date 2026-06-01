using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Réglages de fluidité")]
    public float moveSpeed = 3f;      // Vitesse de déplacement
    public float rotationSpeed = 3f;  // Vitesse de pivot

    private Transform targetPoint;

    void Update()
    {
        if (targetPoint != null)
        {
            // Déplacement fluide de la caméra vers la position cible
            transform.position = Vector3.Lerp(transform.position, targetPoint.position, Time.deltaTime * moveSpeed);
            
            // Rotation fluide de la caméra vers l'orientation cible
            transform.rotation = Quaternion.Slerp(transform.rotation, targetPoint.rotation, Time.deltaTime * rotationSpeed);
        }
    }

    public void SetTarget(Transform newTarget)
    {
        targetPoint = newTarget;
    }
}