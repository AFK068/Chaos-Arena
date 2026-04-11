public enum DebuffType
{
    None = 0,
    Slow = 1,
}

public enum ProjectileKind
{
    Basic = 0,
    Slow = 1,
}


public readonly struct HitData
{
    public int Damage { get; }
    public DebuffType DebuffType { get; }
    public float DebuffDuration { get; }
    public float DebuffPower { get; }

    public HitData(int damage, DebuffType debuffType, float debuffDuration, float debuffPower)
    {
        Damage = damage;
        DebuffType = debuffType;
        DebuffDuration = debuffDuration;
        DebuffPower = debuffPower;
    }
}
