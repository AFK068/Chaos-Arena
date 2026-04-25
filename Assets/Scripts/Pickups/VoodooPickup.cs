using UnityEngine;

public class VoodooPickup : MonoBehaviour, IPickupEffect
{
    [SerializeField] private int maxHealthCap = 4;
    [SerializeField] private float damageMultiplier = 2f;

    public void OnPickup(GameObject player)
    {
        if (player.GetComponent<VoodooBuff>() != null) return;

        var buff = player.AddComponent<VoodooBuff>();
        buff.Init(maxHealthCap, damageMultiplier);
    }
}

public class VoodooBuff : MonoBehaviour
{
    private int _maxHealthCap = 4;
    private float _damageMultiplier = 2f;
    private bool _applied;

    private PlayerHealth _health;
    private PlayerShoot _shoot;

    public void Init(int maxHealthCap, float damageMultiplier)
    {
        _maxHealthCap = Mathf.Max(1, maxHealthCap);
        _damageMultiplier = Mathf.Max(0f, damageMultiplier);
        ApplyPermanentEffects();
    }

    private void Awake()
    {
        _health = GetComponent<PlayerHealth>();
        _shoot = GetComponent<PlayerShoot>();
    }

    private void ApplyPermanentEffects()
    {
        if (_applied) return;

        if (_health == null)
            _health = GetComponent<PlayerHealth>();
        if (_shoot == null)
            _shoot = GetComponent<PlayerShoot>();

        _health?.ApplyMaxHealthCap(_maxHealthCap);
        _shoot?.ModifyProjectileDamage(_damageMultiplier);
        _applied = true;
    }
}
