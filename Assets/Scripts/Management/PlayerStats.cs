using ChaosArena.Platform;

public static class PlayerStats
{
    public static int BestFloor => ProgressService.Shared.BestFloor;
    public static int TotalRuns => ProgressService.Shared.TotalRuns;
    public static int TotalCoins => ProgressService.Shared.TotalCoins;
    public static int TotalKills => ProgressService.Shared.TotalKills;

    public static void RecordRun(RunStats run)
    {
        if (run == null) throw new System.ArgumentNullException(nameof(run));
        ProgressService.Shared.RecordRun(run.Floor, run.Coins, run.Kills);
    }

    public static void Reset()
    {
        ProgressService.Shared.Reset();
    }
}
