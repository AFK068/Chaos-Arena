using UnityEngine;
using System.Collections;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float hitFlashDuration = 0.08f;
    [SerializeField] private Color defaultHitColor = new Color(1f, 0.45f, 0.45f, 1f);
    [SerializeField] private Color poisonHitColor = new Color(0.45f, 1f, 0.45f, 1f);
    [SerializeField] private Color frozenHitColor = new Color(0.45f, 0.75f, 1f, 1f);
    [SerializeField] private Color burnHitColor = new Color(1f, 0.15f, 0.15f, 1f);
    [SerializeField] private float freezeSlowMultiplier = 0.3f;
    [SerializeField] private float poisonSlowMultiplier = 0.7f;
    [SerializeField] [Range(0f, 1f)] private float debuffTintStrength = 0.35f;

    private int _currentHealth;
    private Color _baseColor;
    private Color _originalBaseColor;
    private Coroutine _flashCoroutine;
    private Coroutine _poisonCoroutine;
    private Coroutine _freezeCoroutine;
    private Coroutine _burnCoroutine;
    private IDamageable[] _damageReceivers;
    private EnemyAI _enemyAI;
    private CacodaemonBoss _cacodaemonBoss;
    private HulkZBoss _hulkZBoss;
    private ExperimentZ10Boss _experimentZ10Boss;

    private void Awake()
    {
        int floor = FloorManager.Instance != null ? FloorManager.Instance.CurrentFloor : 1;
        bool isBoss = DifficultyScaler.IsBoss(gameObject);
        maxHealth = DifficultyScaler.ScaleHp(maxHealth, floor, isBoss);

        _currentHealth = maxHealth;
        _damageReceivers = GetComponents<IDamageable>();
        _enemyAI = GetComponent<EnemyAI>();
        _cacodaemonBoss = GetComponent<CacodaemonBoss>();
        _hulkZBoss = GetComponent<HulkZBoss>();
        _experimentZ10Boss = GetComponent<ExperimentZ10Boss>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            _baseColor = spriteRenderer.color;
            _originalBaseColor = _baseColor;
        }
    }

    public void ApplyHit(HitData hitData)
    {
        if (_currentHealth <= 0) return;

        if (spriteRenderer != null)
        {
            if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);

            var flashColor = hitData.DebuffType switch
            {
                DebuffType.Poison => poisonHitColor,
                DebuffType.Frozen => frozenHitColor,
                DebuffType.Burn => burnHitColor,
                _ => defaultHitColor,
            };
            _flashCoroutine = StartCoroutine(FlashColor(flashColor, hitFlashDuration));
        }

        if (hitData.DebuffType == DebuffType.Poison)
            StartPoison(hitData.DebuffDuration, hitData.DebuffPower, hitData.DebuffTickInterval);

        if (hitData.DebuffType == DebuffType.Frozen)
            StartFreeze(hitData.DebuffDuration);

        if (hitData.DebuffType == DebuffType.Burn)
            StartBurn(hitData.DebuffDuration, hitData.DebuffPower, hitData.DebuffTickInterval);

        NotifyAdditionalDamageReceivers(hitData);
        TakeDamage(hitData.Damage);
    }

    public void TakeDamage(int amount)
    {
        _currentHealth -= Mathf.Max(amount, 0);
        _currentHealth = Mathf.Max(_currentHealth, 0);

        if (_currentHealth == 0) Die();
    }

    private void Die()
    {
        GetComponent<EnemyDeathNotifier>()?.Notify();

        if (_poisonCoroutine != null) { StopCoroutine(_poisonCoroutine); _poisonCoroutine = null; }
        if (_freezeCoroutine != null) { StopCoroutine(_freezeCoroutine); _freezeCoroutine = null; }
        if (_burnCoroutine != null) { StopCoroutine(_burnCoroutine); _burnCoroutine = null; }

        _baseColor = _originalBaseColor;
        if (spriteRenderer != null) spriteRenderer.color = _baseColor;

        if (GetComponent<BossDeathAnimator>()?.TryPlayDeathAnimation() == true) { enabled = false; return; }
        if (_cacodaemonBoss != null && _cacodaemonBoss.TryPlayDeathAnimation()) { enabled = false; return; }
        if (_hulkZBoss != null && _hulkZBoss.TryPlayDeathAnimation()) { enabled = false; return; }
        if (_experimentZ10Boss != null && _experimentZ10Boss.TryPlayDeathAnimation()) { enabled = false; return; }

        GetComponent<CoinDrop>()?.DropImmediate(transform.position);
        GetComponent<EnemyDeathEffect>()?.Play(transform.position);
        Destroy(gameObject);
    }

    private void StartFreeze(float duration)
    {
        if (_freezeCoroutine != null) StopCoroutine(_freezeCoroutine);
        _freezeCoroutine = StartCoroutine(FreezeRoutine(duration));
    }

    private IEnumerator FreezeRoutine(float duration)
    {
        _baseColor = Color.Lerp(_originalBaseColor, frozenHitColor, debuffTintStrength);
        if (spriteRenderer != null) spriteRenderer.color = _baseColor;

        _enemyAI?.SetSpeedMultiplier(freezeSlowMultiplier);
        yield return new WaitForSeconds(Mathf.Max(duration, 0f));
        _enemyAI?.SetSpeedMultiplier(1f);

        _baseColor = _originalBaseColor;
        if (spriteRenderer != null) spriteRenderer.color = _baseColor;
        _freezeCoroutine = null;
    }

    private void StartPoison(float duration, float damagePerTick, float tickInterval)
    {
        if (_poisonCoroutine != null) StopCoroutine(_poisonCoroutine);
        _poisonCoroutine = StartCoroutine(PoisonRoutine(duration, damagePerTick, tickInterval));
    }

    private IEnumerator PoisonRoutine(float duration, float damagePerTick, float tickInterval)
    {
        var totalDuration = Mathf.Max(duration, 0f);
        var interval = Mathf.Max(tickInterval, 0.05f);
        var tickDamage = Mathf.Max(Mathf.RoundToInt(damagePerTick), 0);
        var elapsed = 0f;

        _baseColor = Color.Lerp(_originalBaseColor, poisonHitColor, debuffTintStrength);
        if (spriteRenderer != null) spriteRenderer.color = _baseColor;

        _enemyAI?.SetSpeedMultiplier(poisonSlowMultiplier);

        while (elapsed < totalDuration && _currentHealth > 0)
        {
            yield return new WaitForSeconds(interval);
            elapsed += interval;
            if (_currentHealth <= 0) break;
            if (tickDamage > 0) TakeDamage(tickDamage);
        }

        _enemyAI?.SetSpeedMultiplier(1f);
        _baseColor = _originalBaseColor;
        if (spriteRenderer != null) spriteRenderer.color = _baseColor;
        _poisonCoroutine = null;
    }

    private void StartBurn(float duration, float damagePerTick, float tickInterval)
    {
        if (_burnCoroutine != null) StopCoroutine(_burnCoroutine);
        _burnCoroutine = StartCoroutine(BurnRoutine(duration, damagePerTick, tickInterval));
    }

    private IEnumerator BurnRoutine(float duration, float damagePerTick, float tickInterval)
    {
        var totalDuration = Mathf.Max(duration, 0f);
        var interval = Mathf.Max(tickInterval, 0.05f);
        var tickDamage = Mathf.Max(Mathf.RoundToInt(damagePerTick), 0);
        var elapsed = 0f;

        _baseColor = Color.Lerp(_originalBaseColor, burnHitColor, debuffTintStrength);
        if (spriteRenderer != null) spriteRenderer.color = _baseColor;

        while (elapsed < totalDuration && _currentHealth > 0)
        {
            yield return new WaitForSeconds(interval);
            elapsed += interval;
            if (_currentHealth <= 0) break;
            if (tickDamage > 0) TakeDamage(tickDamage);
        }

        _baseColor = _originalBaseColor;
        if (spriteRenderer != null) spriteRenderer.color = _baseColor;
        _burnCoroutine = null;
    }

    private IEnumerator FlashColor(Color targetColor, float duration)
    {
        spriteRenderer.color = targetColor;
        yield return new WaitForSeconds(Mathf.Max(duration, 0.01f));
        spriteRenderer.color = _baseColor;
    }

    private void NotifyAdditionalDamageReceivers(HitData hitData)
    {
        if (_damageReceivers == null || _damageReceivers.Length == 0) return;

        for (int i = 0; i < _damageReceivers.Length; i++)
        {
            var receiver = _damageReceivers[i];
            if (receiver == null || ReferenceEquals(receiver, this)) continue;
            receiver.ApplyHit(hitData);
        }
    }
}
