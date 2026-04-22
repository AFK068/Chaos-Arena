using System.Collections;
using UnityEngine;

public class HazardZone : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [SerializeField] private float armDelay = 0f;
    [SerializeField] private float damageCooldown = 0.8f;
    [SerializeField] private bool autoDestroyAfterAnimation = true;
    [SerializeField] private float fallbackLifetime = 2f;
    [SerializeField] private float postArmLifetime = 0.5f;

    private Coroutine _lifetimeCoroutine;
    private float _armedAtTime;
    private float _nextDamageTime;

    private void Awake()
    {
        _armedAtTime = Time.time + Mathf.Max(armDelay, 0f);
        _nextDamageTime = _armedAtTime;
    }

    private void OnEnable()
    {
        RestartLifetimeTimer();
    }

    public void ArmAfterDelay(float delay)
    {
        _armedAtTime = Time.time + Mathf.Max(delay, 0f);
        _nextDamageTime = Mathf.Max(_nextDamageTime, _armedAtTime);
        RestartLifetimeTimer();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent<PlayerHealth>(out var playerHealth))
            return;

        TryDealDamageNow(playerHealth);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.TryGetComponent<PlayerHealth>(out var playerHealth))
            return;

        TryDealDamageNow(playerHealth);
    }

    private void TryDealDamageNow(PlayerHealth playerHealth)
    {
        if (playerHealth == null)
            return;

        if (Time.time < _armedAtTime || Time.time < _nextDamageTime)
            return;

        playerHealth.TakeDamage(damage);
        _nextDamageTime = Time.time + Mathf.Max(0.01f, damageCooldown);
    }

    private void RestartLifetimeTimer()
    {
        if (!autoDestroyAfterAnimation)
            return;

        if (_lifetimeCoroutine != null)
        {
            StopCoroutine(_lifetimeCoroutine);
            _lifetimeCoroutine = null;
        }

        _lifetimeCoroutine = StartCoroutine(DestroyAfterLifetimeRoutine());
    }

    private IEnumerator DestroyAfterLifetimeRoutine()
    {
        float animationLifetime = fallbackLifetime;
        if (TryGetComponent<SpriteSheetAnimator>(out var animator) && animator.AnimationDuration > 0f)
            animationLifetime = animator.AnimationDuration;

        float minLifetimeAfterArm = Mathf.Max(postArmLifetime, damageCooldown + 0.05f);
        float minLifetimeFromArm = (_armedAtTime - Time.time) + minLifetimeAfterArm;
        float lifetime = Mathf.Max(animationLifetime, minLifetimeFromArm);

        if (lifetime > 0f)
            yield return new WaitForSeconds(lifetime);

        Destroy(gameObject);
    }
}
