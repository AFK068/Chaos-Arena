using UnityEngine;

public class MainMenuUI : MonoBehaviour
{
    public void OnNewRun() => GameManager.Instance.StartRun();

    public void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
