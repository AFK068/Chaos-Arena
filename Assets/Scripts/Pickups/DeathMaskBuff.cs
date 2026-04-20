using System.Collections;
using UnityEngine;

public class DeathMaskBuff : MonoBehaviour
{
    [SerializeField] private float rageDuration = 3f;
    [SerializeField] private float speedMultiplier = 1.5f;
    [SerializeField] private float fireRateMultiplier = 2f;
    [SerializeField] private float dashCooldownMultiplier = 2f;
    [SerializeField] private float triggerChance = 0.2f;
    [SerializeField] private GameObject effectPrefab;

    private PlayerHealth _health;
    private PlayerMovement _movement;
    private PlayerShoot _shoot;
    private bool _raging;

    public void Init(float duration, float speed, float fireRate, float dash, float chance, GameObject effect)
    {
        rageDuration = duration;
        speedMultiplier = speed;
        fireRateMultiplier = fireRate;
        dashCooldownMultiplier = dash;
        triggerChance = chance;
        effectPrefab = effect;
    }

    private void Awake()
    {
        _health = GetComponent<PlayerHealth>();
        _movement = GetComponent<PlayerMovement>();
        _shoot = GetComponent<PlayerShoot>();
        _health.OnDamageTaken += TriggerRage;
    }

    private void OnDestroy()
    {
        if (_health != null)
            _health.OnDamageTaken -= TriggerRage;
    }

    private void TriggerRage()
    {
        if (_raging) return;
        if (Random.value > triggerChance) return;
        StartCoroutine(RageRoutine());
    }

    private IEnumerator RageRoutine()
    {
        _raging = true;

        _movement?.ApplyRageBuff(speedMultiplier, dashCooldownMultiplier, rageDuration);
        _shoot?.ApplyFireRateBuff(fireRateMultiplier, rageDuration);
        RageEffect.Spawn(effectPrefab, transform, rageDuration);

        yield return new WaitForSeconds(rageDuration);
        _raging = false;
    }
}
