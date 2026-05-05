using UnityEngine;
using TMPro;

public class MainMenuStats : MonoBehaviour
{
    [SerializeField] private TMP_Text bestFloorValue;
    [SerializeField] private TMP_Text totalRunsValue;
    [SerializeField] private TMP_Text totalCoinsValue;
    [SerializeField] private TMP_Text totalKillsValue;

    void Start() => Refresh();

    public void Refresh()
    {
        if (bestFloorValue != null) bestFloorValue.text = PlayerStats.BestFloor.ToString();
        if (totalRunsValue != null) totalRunsValue.text = PlayerStats.TotalRuns.ToString();
        if (totalCoinsValue != null) totalCoinsValue.text = PlayerStats.TotalCoins.ToString();
        if (totalKillsValue != null) totalKillsValue.text = PlayerStats.TotalKills.ToString();
    }
}
