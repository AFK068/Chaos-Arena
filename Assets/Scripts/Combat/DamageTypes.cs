public enum DebuffType
{
    None = 0,
    Poison = 1,
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
    public float DebuffDuration { get; } // total debuff time
    public float DebuffPower { get; } // meaning depends on debuff type
    public float DebuffTickInterval { get; }

    public HitData(int damage, DebuffType debuffType, float debuffDuration, float debuffPower, float debuffTickInterval)
    {
        Damage = damage;
        DebuffType = debuffType;
        DebuffDuration = debuffDuration;
        DebuffPower = debuffPower;
        DebuffTickInterval = debuffTickInterval;
    }
}
