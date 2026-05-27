using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class LobbyManager : MonoBehaviour
{
    [Header("UI Components")]
    public GameObject lobbyPanel;
    public TextMeshProUGUI statusText;
    public List<GameObject> gameplayCanvases; 
    public List<GameObject> presenceIndicators;
    public List<TextMeshProUGUI> buttonTexts;

    [Header("Game Setup")]
    public GameManager gameManager;
    public List<PlayerEntity> allPlayers;

    private bool[] isSlotHuman = new bool[4];

    void Start()
    {
        lobbyPanel.SetActive(true);
        gameManager.enabled = false;
        
        foreach (GameObject canvas in gameplayCanvases)
        {
            if (canvas != null) canvas.SetActive(false);
        }

        isSlotHuman[0] = true;
        for (int i = 1; i < 4; i++) isSlotHuman[i] = false;

        UpdateLobbySetup();
    }

    public void TogglePlayerSlot(int index)
    {
        if (index < 0 || index >= 4) return;
        isSlotHuman[index] = !isSlotHuman[index];
        UpdateLobbySetup();
    }

    void UpdateLobbySetup()
    {
        int humanCount = 0;

        for (int i = 0; i < allPlayers.Count; i++)
        {
            if (isSlotHuman[i])
            {
                allPlayers[i].isBot = false;
                allPlayers[i].playerName = "Player " + (i + 1);
                humanCount++;

                if (presenceIndicators.Count > i && presenceIndicators[i] != null) presenceIndicators[i].SetActive(true);
                if (buttonTexts.Count > i && buttonTexts[i] != null) buttonTexts[i].text = "Leave Player " + (i + 1);
            }
            else
            {
                allPlayers[i].isBot = true;
                allPlayers[i].playerName = "Bot Ghost " + (i + 1);

                if (presenceIndicators.Count > i && presenceIndicators[i] != null) presenceIndicators[i].SetActive(false);
                if (buttonTexts.Count > i && buttonTexts[i] != null) buttonTexts[i].text = "Join Player " + (i + 1);
            }
        }

        if (statusText != null)
        {
            string playerWord = (humanCount > 1) ? "Players" : "Player";
            string botWord = ((4 - humanCount) > 1) ? "Bots" : "Bot";
            statusText.text = humanCount + " " + playerWord + " & " + (4 - humanCount) + " " + botWord;
        }
    }

    public void PressStartGame()
    {
        lobbyPanel.SetActive(false);
        
        foreach (GameObject canvas in gameplayCanvases)
        {
            if (canvas != null) canvas.SetActive(true);
        }
        
        gameManager.enabled = true;
        gameManager.StartTimer();
    }
}