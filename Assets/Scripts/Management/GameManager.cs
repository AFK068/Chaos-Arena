using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using ChaosArena.Platform;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private string gameplayScene = "Gameplay";
    [SerializeField] private string mainMenuScene = "MainMenu";
    [SerializeField] private string gameOverScene  = "GameOver";

    public RunStats CurrentRun { get; } = new RunStats();
    public HashSet<string> PlayerInventory { get; } = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;
        var go = new GameObject(nameof(GameManager));
        go.AddComponent<GameManager>();
    }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnemyDeathNotifier.OnAnyEnemyKilled += HandleEnemyKilled;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            EnemyDeathNotifier.OnAnyEnemyKilled -= HandleEnemyKilled;
    }

    public void StartRun()
    {
        CurrentRun.Reset();
        PlayerInventory.Clear();
        YandexPlatformService.SetGameplayIntent(true);
        SceneManager.LoadScene(gameplayScene);
    }

    public void RestartRun()
    {
        CurrentRun.Reset();
        PlayerInventory.Clear();
        YandexPlatformService.SetGameplayIntent(true);
        SceneManager.LoadScene(gameplayScene);
    }

    public void GoToGameOver()
    {
        CurrentRun.StopTimer();
        PlayerStats.RecordRun(CurrentRun);
        YandexPlatformService.SetGameplayIntent(false);
        YandexPlatformService.SetLocalPause(false);
        SceneManager.LoadScene(gameOverScene);
    }

    public void GoToMainMenu()
    {
        YandexPlatformService.SetGameplayIntent(false);
        YandexPlatformService.SetLocalPause(false);
        SceneManager.LoadScene(mainMenuScene);
    }

    private void HandleEnemyKilled() => CurrentRun.Kills++;

    [ContextMenu("Reset Player Stats")]
    private void ResetPlayerStats()
    {
        PlayerStats.Reset();
        Debug.Log("Player stats reset");
    }
}
