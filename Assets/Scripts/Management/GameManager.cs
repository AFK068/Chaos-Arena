using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private string gameplayScene = "Gameplay";
    [SerializeField] private string mainMenuScene = "MainMenu";
    [SerializeField] private string gameOverScene  = "GameOver";

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void StartRun()   => SceneManager.LoadScene(gameplayScene);
    public void RestartRun() => SceneManager.LoadScene(gameplayScene);
    public void GoToGameOver() => SceneManager.LoadScene(gameOverScene);
    public void GoToMainMenu() => SceneManager.LoadScene(mainMenuScene);
}
