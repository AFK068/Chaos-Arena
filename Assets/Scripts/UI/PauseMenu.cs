using UnityEngine;
using UnityEngine.InputSystem;
using ChaosArena.Platform;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject settingsPanel;

    private bool _isPaused;

    void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;

        if (settingsPanel != null && settingsPanel.activeSelf)
            OnCloseSettings();
        else
            Toggle();
    }

    public void Toggle()
    {
        if (_isPaused) Resume();
        else Pause();
    }

    public void Pause()
    {
        _isPaused = true;
        YandexPlatformService.SetLocalPause(true);
        if (pausePanel != null) pausePanel.SetActive(true);
    }

    public void Resume()
    {
        _isPaused = false;
        YandexPlatformService.SetLocalPause(false);
        if (pausePanel != null) pausePanel.SetActive(false);
    }

    public void OnContinue() => Resume();

    public void OnOpenSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(true);
        if (pausePanel != null) pausePanel.SetActive(false);
    }

    public void OnCloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(true);
    }

    public void OnQuit()
    {
        Resume();
        GameManager.Instance.GoToMainMenu();
    }

    void OnDestroy()
    {
        if (_isPaused)
            YandexPlatformService.SetLocalPause(false);
    }
}
