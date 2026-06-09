using UnityEngine;

public class TitleJuice : MonoBehaviour
{
    [Header("Réglages du Battement (Taille)")]
    public float pulseSpeed = 2f;
    public float pulseAmount = 0.05f;

    [Header("Réglages du Flottement (Position)")]
    public float floatSpeed = 1.5f;
    public float floatAmount = 15f;

    private Vector3 startScale;
    private Vector2 startPos;
    private RectTransform rectTransform;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        startScale = rectTransform.localScale;
        startPos = rectTransform.anchoredPosition;
    }

    void Update()
    {
        // 1. Le titre grossit et rétrécit doucement (Sinus)
        float scaleSin = Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        rectTransform.localScale = startScale + new Vector3(scaleSin, scaleSin, 0);

        // 2. Le titre flotte de haut en bas doucement
        float floatSin = Mathf.Sin(Time.time * floatSpeed) * floatAmount;
        rectTransform.anchoredPosition = startPos + new Vector2(0, floatSin);
    }
}