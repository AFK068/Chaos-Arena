using UnityEngine;
using ChaosArena.Platform;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject settingsPanel;

    private void Start()
    {
        AudioManager.Instance?.PlayMenuMusic();
        YandexPlatformService.NotifyMainMenuInteractive();
    }

    public void OnNewRun() => GameManager.Instance.StartRun();

    public void OnOpenSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(true);
        if (mainPanel != null) mainPanel.SetActive(false);
    }

    public void OnCloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(true);
    }

    public void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
