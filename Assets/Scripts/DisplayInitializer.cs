using UnityEngine;

public class DisplayInitializer : MonoBehaviour
{
    void Start()
    {
        for (int i = 1; i < Display.displays.Length; i++)
        {
            if (i < 4)
            {
                Display.displays[i].Activate();
            }
        }
    }
}