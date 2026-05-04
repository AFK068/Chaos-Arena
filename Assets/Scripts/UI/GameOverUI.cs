using UnityEngine;

public class GameOverUI : MonoBehaviour
{
    public void OnNewRun() => GameManager.Instance.RestartRun();
    public void OnMainMenu() => GameManager.Instance.GoToMainMenu();
}
