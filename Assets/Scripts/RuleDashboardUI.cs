using UnityEngine;
using UnityEngine.UI;

public class RulesDashboardUI : MonoBehaviour
{
    [Header("Panneau Glissant")]
    public RectTransform panelRect;    
    public Vector2 closedPosition;      
    public Vector2 openPosition;        
    public float slideSpeed = 10f;
    private bool isOpen = false;

    [Header("Flèche de Notification")]
    public RectTransform alertArrowRect; 
    public float wiggleSpeed = 15f; 
    public float wiggleAmount = 15f;  
    private bool hasNewRuleAlert = false;
    private Vector2 arrowOriginalPosition;

    void Start()
    {
        if (panelRect != null) panelRect.anchoredPosition = closedPosition;
        if (alertArrowRect != null) arrowOriginalPosition = alertArrowRect.anchoredPosition;
    }

    void Update()
    {
        if (panelRect != null)
        {
            Vector2 targetPos = isOpen ? openPosition : closedPosition;
            panelRect.anchoredPosition = Vector2.Lerp(panelRect.anchoredPosition, targetPos, Time.deltaTime * slideSpeed);
        }

        if (hasNewRuleAlert && alertArrowRect != null && !isOpen)
        {
            float offsetX = Mathf.Sin(Time.time * wiggleSpeed) * wiggleAmount;
            alertArrowRect.anchoredPosition = new Vector2(arrowOriginalPosition.x + offsetX, arrowOriginalPosition.y);
        }
        else if (alertArrowRect != null)
        {
            alertArrowRect.anchoredPosition = arrowOriginalPosition;
        }

        if (alertArrowRect != null)
        {
            float targetAngle = isOpen ? 0f : 180f;
            float currentAngle = alertArrowRect.localEulerAngles.z;
            
            float smoothAngle = Mathf.LerpAngle(currentAngle, targetAngle, Time.deltaTime * slideSpeed);
            
            alertArrowRect.localEulerAngles = new Vector3(0f, 0f, smoothAngle);
        }
    }

    public void TogglePanel()
    {
        isOpen = !isOpen;
        
        if (isOpen)
        {
            hasNewRuleAlert = false;
        }
    }

    public void TriggerNewRuleAlert()
    {
        if (!isOpen)
        {
            hasNewRuleAlert = true;
        }
    }
}