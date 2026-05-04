using UnityEngine;
using UnityEngine.InputSystem;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;

    private bool _isPaused;

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
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
        Time.timeScale = 0f;
        if (pausePanel != null) pausePanel.SetActive(true);
    }

    public void Resume()
    {
        _isPaused = false;
        Time.timeScale = 1f;
        if (pausePanel != null) pausePanel.SetActive(false);
    }

    public void OnContinue() => Resume();

    public void OnQuit()
    {
        Time.timeScale = 1f;
        GameManager.Instance.GoToMainMenu();
    }

    void OnDestroy() => Time.timeScale = 1f;
}
