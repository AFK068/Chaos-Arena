using UnityEngine;
using TMPro;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] private TMP_Text floorValue;
    [SerializeField] private TMP_Text coinsValue;
    [SerializeField] private TMP_Text killsValue;
    [SerializeField] private TMP_Text durationValue;

    void Start()
    {
        var run = GameManager.Instance.CurrentRun;
        if (floorValue != null) floorValue.text = run.Floor.ToString();
        if (coinsValue != null) coinsValue.text = run.Coins.ToString();
        if (killsValue != null) killsValue.text = run.Kills.ToString();
        if (durationValue != null) durationValue.text = FormatDuration(run.Duration);
    }

    public void OnNewRun() => GameManager.Instance.RestartRun();
    public void OnMainMenu() => GameManager.Instance.GoToMainMenu();

    private static string FormatDuration(float seconds)
    {
        int total = Mathf.FloorToInt(seconds);
        return $"{total / 60:D2}:{total % 60:D2}";
    }
}
