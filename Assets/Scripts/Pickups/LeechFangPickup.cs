using UnityEngine;

public class LeechFangPickup : MonoBehaviour, IPickupEffect
{
    public void OnPickup(GameObject player)
    {
        if (player.GetComponent<LeechFangBuff>() != null) return;
        player.AddComponent<LeechFangBuff>();
    }
}

public class LeechFangBuff : MonoBehaviour
{
    private const float HealChance = 0.01f;

    private PlayerHealth _health;

    private void Awake()
    {
        _health = GetComponent<PlayerHealth>();
        EnemyDeathNotifier.OnAnyEnemyKilled += OnKill;
    }

    private void OnDestroy() => EnemyDeathNotifier.OnAnyEnemyKilled -= OnKill;

    private void OnKill()
    {
        if (Random.value < HealChance) _health?.Heal(1);
    }
}
