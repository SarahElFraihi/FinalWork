using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public void PlayGame()
    {
        SceneManager.LoadScene("Demo");
    }

    public void QuitGame()
    {
        Debug.Log("Le jeu se ferme !");
        Application.Quit(); 
    }
}