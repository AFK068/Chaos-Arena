using UnityEngine;
using System.Collections;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float hitFlashDuration = 0.08f;
    [SerializeField] private Color hitFlashColor = new Color(1f, 0.45f, 0.45f, 1f);
    [SerializeField] private EnemyAI enemyAI;

    private int _currentHealth;
    private Color _baseColor;
    private Coroutine _flashCoroutine;

    private void Awake()
    {
        _currentHealth = maxHealth;

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (enemyAI == null)
        {
            enemyAI = GetComponent<EnemyAI>();
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

            _flashCoroutine = StartCoroutine(HitFlash());
        }

        if (hitData.DebuffType == DebuffType.Slow && enemyAI != null)
        {
            enemyAI.ApplySlow(hitData.DebuffDuration, hitData.DebuffPower);
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
        Destroy(gameObject);
    }

    private IEnumerator HitFlash()
    {
        spriteRenderer.color = hitFlashColor;
        yield return new WaitForSeconds(hitFlashDuration);
        spriteRenderer.color = _baseColor;
    }
}
