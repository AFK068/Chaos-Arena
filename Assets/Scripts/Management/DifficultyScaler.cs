using UnityEngine;

public static class DifficultyScaler
{
    private const float NormalHpStep = 0.15f;
    private const float NormalDmgStep = 0.10f;
    private const float BossHpStep = 0.08f;
    private const float BossDmgStep = 0.06f;

    public static int ScaleHp(int baseHp, int floor, bool isBoss)
    {
        float step = isBoss ? BossHpStep : NormalHpStep;
        float mul = 1f + Mathf.Max(0, floor - 1) * step;
        return Mathf.Max(1, Mathf.RoundToInt(baseHp * mul));
    }

    public static int ScaleDamage(int baseDamage, int floor, bool isBoss)
    {
        float step = isBoss ? BossDmgStep : NormalDmgStep;
        float mul = 1f + Mathf.Max(0, floor - 1) * step;
        return Mathf.Max(1, Mathf.RoundToInt(baseDamage * mul));
    }

    public static bool IsBoss(GameObject go)
    {
        return go.GetComponent<CacodaemonBoss>() != null
            || go.GetComponent<HulkZBoss>() != null
            || go.GetComponent<ExperimentZ10Boss>() != null;
    }
}
