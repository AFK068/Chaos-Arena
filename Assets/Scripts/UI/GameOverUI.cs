using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] private string gameplayScene = "Gameplay";
    [SerializeField] private string mainMenuScene = "MainMenu";

    public void OnNewRun()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.RestartRun();
        else
            SceneManager.LoadScene(gameplayScene);
    }

    public void OnMainMenu()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.GoToMainMenu();
        else
            SceneManager.LoadScene(mainMenuScene);
    }
}
