public enum DebuffType
{
    None = 0,
    Poison = 1,
    Frozen = 2,
    Burn = 3,
}

public enum ProjectileKind
{
    Basic = 0,
    Poison = 1,
}


public readonly struct HitData
{
    public int Damage { get; }
    public DebuffType DebuffType { get; }
    public float DebuffDuration { get; }
    public float DebuffPower { get; }
    public float DebuffTickInterval { get; }
    public UnityEngine.GameObject DebuffEffectPrefab { get; }

    public HitData(int damage, DebuffType debuffType, float debuffDuration, float debuffPower, float debuffTickInterval, UnityEngine.GameObject debuffEffectPrefab = null)
    {
        Damage = damage;
        DebuffType = debuffType;
        DebuffDuration = debuffDuration;
        DebuffPower = debuffPower;
        DebuffTickInterval = debuffTickInterval;
        DebuffEffectPrefab = debuffEffectPrefab;
    }
}
