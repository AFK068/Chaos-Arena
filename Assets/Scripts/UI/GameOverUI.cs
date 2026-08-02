using UnityEngine;
using TMPro;
using ChaosArena.Platform;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] private TMP_Text floorValue;
    [SerializeField] private TMP_Text coinsValue;
    [SerializeField] private TMP_Text killsValue;
    [SerializeField] private TMP_Text durationValue;
    private bool _newRunRequested;

    void Start()
    {
        var run = GameManager.Instance.CurrentRun;
        if (floorValue != null) floorValue.text = run.Floor.ToString();
        if (coinsValue != null) coinsValue.text = run.Coins.ToString();
        if (killsValue != null) killsValue.text = run.Kills.ToString();
        if (durationValue != null) durationValue.text = FormatDuration(run.Duration);
    }

    public void OnNewRun()
    {
        var gameManager = GameManager.Instance;
        if (_newRunRequested || gameManager == null || !gameManager.TryBeginGameOverRestart(out var requestToken))
            return;

        _newRunRequested = true;
        if (!YandexAdsService.RequestFullscreenBefore(() => gameManager.CompleteGameOverRestart(requestToken)))
        {
            _newRunRequested = false;
            gameManager.CancelGameOverRestart();
        }
    }

    public void OnMainMenu()
    {
        var gameManager = GameManager.Instance;
        if (gameManager == null)
            return;

        _newRunRequested = false;
        gameManager.CancelGameOverRestart();
        YandexAdsService.CancelPendingFullscreenRequest();
        gameManager.GoToMainMenu();
    }

    private static string FormatDuration(float seconds)
    {
        int total = Mathf.FloorToInt(seconds);
        return $"{total / 60:D2}:{total % 60:D2}";
    }
}
