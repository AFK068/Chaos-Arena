using UnityEngine;
using System.Collections;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float hitFlashDuration = 0.08f;
    [SerializeField] private Color hitFlashColor = new Color(1f, 0.45f, 0.45f, 1f);
    [SerializeField] private Color poisonTickColor = new Color(0.45f, 1f, 0.45f, 1f);
    [SerializeField] private float poisonTickFlashDuration = 0.1f;

    private int _currentHealth;
    private Color _baseColor;
    private Coroutine _flashCoroutine;
    private Coroutine _poisonCoroutine;

    private void Awake()
    {
        _currentHealth = maxHealth;

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (spriteRenderer != null)
        {
            _baseColor = spriteRenderer.color;
        }
    }

    public void ApplyHit(HitData hitData)
    {
        if (_currentHealth <= 0)
        {
            return;
        }

        if (spriteRenderer != null)
        {
            if (_flashCoroutine != null)
            {
                StopCoroutine(_flashCoroutine);
            }

            _flashCoroutine = StartCoroutine(FlashColor(hitFlashColor, hitFlashDuration));
        }

        if (hitData.DebuffType == DebuffType.Poison)
        {
            StartPoison(hitData.DebuffDuration, hitData.DebuffPower, hitData.DebuffTickInterval);
        }

        TakeDamage(hitData.Damage);
    }

    public void TakeDamage(int amount)
    {
        _currentHealth -= Mathf.Max(amount, 0);
        _currentHealth = Mathf.Max(_currentHealth, 0);

        if (_currentHealth == 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (_poisonCoroutine != null)
        {
            StopCoroutine(_poisonCoroutine);
            _poisonCoroutine = null;
        }

        Destroy(gameObject);
    }

    private void StartPoison(float duration, float damagePerTick, float tickInterval)
    {
        if (_poisonCoroutine != null)
        {
            StopCoroutine(_poisonCoroutine);
        }

        _poisonCoroutine = StartCoroutine(PoisonRoutine(duration, damagePerTick, tickInterval));
    }

    private IEnumerator PoisonRoutine(float duration, float damagePerTick, float tickInterval)
    {
        var totalDuration = Mathf.Max(duration, 0f);
        var interval = Mathf.Max(tickInterval, 0.05f);
        var tickDamage = Mathf.Max(Mathf.RoundToInt(damagePerTick), 0);
        var elapsed = 0f;

        while (elapsed < totalDuration && _currentHealth > 0)
        {
            yield return new WaitForSeconds(interval);
            elapsed += interval;

            if (_currentHealth <= 0)
            {
                break;
            }

            if (tickDamage > 0)
            {
                TakeDamage(tickDamage);
            }

            if (spriteRenderer != null)
            {
                if (_flashCoroutine != null)
                {
                    StopCoroutine(_flashCoroutine);
                }

                _flashCoroutine = StartCoroutine(FlashColor(poisonTickColor, poisonTickFlashDuration));
            }
        }

        _poisonCoroutine = null;
    }

    private IEnumerator FlashColor(Color targetColor, float duration)
    {
        spriteRenderer.color = targetColor;
        yield return new WaitForSeconds(Mathf.Max(duration, 0.01f));
        spriteRenderer.color = _baseColor;
    }
}
