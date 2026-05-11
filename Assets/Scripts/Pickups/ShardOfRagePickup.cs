using UnityEngine;

public class ShardOfRagePickup : MonoBehaviour, IPickupEffect
{
    public void OnPickup(GameObject player)
    {
        if (player.GetComponent<ShardOfRageBuff>() != null) return;
        player.AddComponent<ShardOfRageBuff>();
    }
}

public class ShardOfRageBuff : MonoBehaviour
{
    private const int MaxStacks = 5;
    private const float StackBonusFactor = 1.05f;

    private int _stacks;
    private PlayerShoot _shoot;
    private PlayerHealth _health;

    private void Awake()
    {
        _shoot = GetComponent<PlayerShoot>();
        _health = GetComponent<PlayerHealth>();
        EnemyDeathNotifier.OnAnyEnemyKilled += OnKill;
        _health.OnDamageTaken += OnDamage;
    }

    private void OnDestroy()
    {
        EnemyDeathNotifier.OnAnyEnemyKilled -= OnKill;
        if (_health != null) _health.OnDamageTaken -= OnDamage;
    }

    private void OnKill()
    {
        if (_stacks >= MaxStacks) return;
        _stacks++;
        _shoot?.ModifyFireRate(1f / StackBonusFactor);
    }

    private void OnDamage()
    {
        if (_stacks == 0) return;
        _shoot?.ModifyFireRate(Mathf.Pow(StackBonusFactor, _stacks));
        _stacks = 0;
    }
}
