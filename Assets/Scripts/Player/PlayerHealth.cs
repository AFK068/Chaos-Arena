using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private GameObject heartPrefab;
    [SerializeField] private Transform heartsContainer;
    [SerializeField] private Sprite fullHeartSprite;
    [SerializeField] private Sprite halfHeartSprite;
    [SerializeField] private int maxHealth = 8;
    [SerializeField] private SpriteRenderer playerRenderer;
    [SerializeField] private float hitFlashDuration = 0.1f;
    [SerializeField] private Color hitFlashColor = new Color(1f, 0.45f, 0.45f, 1f);

    public event System.Action OnDamageTaken;
    public event System.Action OnShieldBroken;

    public bool ShieldActive => _shieldActive;
    private bool _shieldActive;

    private int _currentHealth;
    private float _dodgeChance;
    private Image[] hearts;
    private Color _baseColor;
    private Coroutine _flashCoroutine;
    private Coroutine _shakeCoroutine;

    void Start()
    {
        _currentHealth = maxHealth;

        if (playerRenderer == null)
        {
            playerRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (playerRenderer != null)
        {
            _baseColor = playerRenderer.color;
        }

        GenerateHearts();
        UpdateHearts();
    }

    private void GenerateHearts()
    {
        foreach (Transform child in heartsContainer)
        {
            Destroy(child.gameObject);
        }

        int heartCount = Mathf.CeilToInt(maxHealth / 2f);
        hearts = new Image[heartCount];

        for (int i = 0; i < heartCount; i++)
        {
            GameObject heart = Instantiate(heartPrefab, heartsContainer);
            hearts[i] = heart.GetComponent<Image>();
        }
    }

    public void AddDodgeChance(float amount)
    {
        _dodgeChance = Mathf.Max(_dodgeChance, Mathf.Clamp01(amount));
    }

    public void GrantShield() => _shieldActive = true;
    public void RemoveShield() { _shieldActive = false; }

    public void TakeDamage(int amount)
    {
        if (_shieldActive)
        {
            _shieldActive = false;
            OnShieldBroken?.Invoke();
            return;
        }
        if (_dodgeChance > 0f && Random.value < _dodgeChance) return;

        _currentHealth -= Mathf.Max(amount, 0);
        _currentHealth = Mathf.Max(_currentHealth, 0);
        OnDamageTaken?.Invoke();
        UpdateHearts();

        if (playerRenderer != null)
        {
            if (_flashCoroutine != null)
                StopCoroutine(_flashCoroutine);
            _flashCoroutine = StartCoroutine(HitFlash());
        }

        if (_shakeCoroutine != null)
            StopCoroutine(_shakeCoroutine);
        _shakeCoroutine = StartCoroutine(ShakeHearts());

        if (_currentHealth == 0)
        {
            Debug.LogWarning("TODO: Add player death logic when HP reaches 0.");
        }
    }

    public void Heal(int amount)
    {
        _currentHealth += amount;
        _currentHealth = Mathf.Min(_currentHealth, maxHealth);
        UpdateHearts();
    }

    [SerializeField] private int absoluteMaxHealth = 10;

    public void AddMaxHealth(int amount)
    {
        if (maxHealth >= absoluteMaxHealth)
        {
            Heal(amount);
            return;
        }
        maxHealth = Mathf.Min(maxHealth + amount, absoluteMaxHealth);
        _currentHealth = Mathf.Min(_currentHealth + amount, maxHealth);
        GenerateHearts();
        UpdateHearts();
    }

    public void ApplyMaxHealthCap(int cap)
    {
        int clampedCap = Mathf.Max(1, cap);
        absoluteMaxHealth = clampedCap;
        maxHealth = Mathf.Min(maxHealth, absoluteMaxHealth);
        _currentHealth = Mathf.Min(_currentHealth, maxHealth);
        GenerateHearts();
        UpdateHearts();
    }

    private void UpdateHearts()
    {
        for (int i = 0; i < hearts.Length; i++)
        {
            int heartValue = Mathf.Clamp(_currentHealth - (i * 2), 0, 2);

            if (heartValue == 2)
            {
                hearts[i].sprite = fullHeartSprite;
                hearts[i].enabled = true;
            }
            else if (heartValue == 1)
            {
                hearts[i].sprite = halfHeartSprite;
                hearts[i].enabled = true;
            }
            else
            {
                hearts[i].enabled = false;
            }
        }
    }

    private IEnumerator HitFlash()
    {
        playerRenderer.color = hitFlashColor;
        yield return new WaitForSeconds(hitFlashDuration);
        playerRenderer.color = _baseColor;
    }

    private IEnumerator ShakeHearts()
    {
        float duration = 0.3f;
        float magnitude = 2f;
        float elapsed = 0f;

        var originalPositions = new Vector3[hearts.Length];
        for (int i = 0; i < hearts.Length; i++)
            originalPositions[i] = hearts[i].rectTransform.anchoredPosition3D;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            for (int i = 0; i < hearts.Length; i++)
            {
                var pos = originalPositions[i];
                pos.x += x;
                pos.y += y;
                hearts[i].rectTransform.anchoredPosition3D = pos;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        for (int i = 0; i < hearts.Length; i++)
            hearts[i].rectTransform.anchoredPosition3D = originalPositions[i];
    }
}
