using UnityEngine;

public static class PlayerStats
{
    private const string KeyBestFloor = "stats.best_floor";
    private const string KeyTotalRuns = "stats.total_runs";
    private const string KeyTotalCoins = "stats.total_coins";
    private const string KeyTotalKills = "stats.total_kills";

    public static int BestFloor => PlayerPrefs.GetInt(KeyBestFloor, 0);
    public static int TotalRuns => PlayerPrefs.GetInt(KeyTotalRuns, 0);
    public static int TotalCoins => PlayerPrefs.GetInt(KeyTotalCoins, 0);
    public static int TotalKills => PlayerPrefs.GetInt(KeyTotalKills, 0);

    public static void RecordRun(RunStats run)
    {
        if (run.Floor > BestFloor) PlayerPrefs.SetInt(KeyBestFloor, run.Floor);
        PlayerPrefs.SetInt(KeyTotalRuns, TotalRuns + 1);
        PlayerPrefs.SetInt(KeyTotalCoins, TotalCoins + run.Coins);
        PlayerPrefs.SetInt(KeyTotalKills, TotalKills + run.Kills);
        PlayerPrefs.Save();
    }

    public static void Reset()
    {
        PlayerPrefs.DeleteKey(KeyBestFloor);
        PlayerPrefs.DeleteKey(KeyTotalRuns);
        PlayerPrefs.DeleteKey(KeyTotalCoins);
        PlayerPrefs.DeleteKey(KeyTotalKills);
        PlayerPrefs.Save();
    }
}
