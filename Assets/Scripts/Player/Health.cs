using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Health : MonoBehaviour
{
    [SerializeField] private Image[] hearts;
    [SerializeField] private int maxHealth = 4;

    private int _currentHealth;
    private Vector3[] _baseScales;

    void Start()
    {
        _currentHealth = maxHealth;
        _baseScales = new Vector3[hearts.Length];
        for (int i = 0; i < hearts.Length; i++)
            _baseScales[i] = hearts[i].rectTransform.localScale;
        UpdateHearts();
    }

    public void TakeDamage(int amount)
    {
        _currentHealth -= amount;
        _currentHealth = Mathf.Max(_currentHealth, 0);
        UpdateHearts();
    }

    public void Heal(int amount)
    {
        _currentHealth += amount;
        _currentHealth = Mathf.Min(_currentHealth, maxHealth);
        UpdateHearts();
    }

    private void UpdateHearts()
    {
        for (int i = 0; i < hearts.Length; i++)
        {
            bool wasEnabled = hearts[i].enabled;
            bool shouldBeEnabled = i < _currentHealth;
            if (wasEnabled && !shouldBeEnabled)
            {
                StopCoroutine("AppearHeart");
                StartCoroutine(ShakeAndFadeOutHeart(hearts[i]));
            }
            else if (!wasEnabled && shouldBeEnabled)
            {
                StopCoroutine("ShakeAndFadeOutHeart");
                StartCoroutine(AppearHeart(hearts[i]));
            }
            else
            {
                hearts[i].enabled = shouldBeEnabled;
                if (shouldBeEnabled)
                {
                    var c = hearts[i].color;
                    c.a = 1f;
                    hearts[i].color = c;
                }
            }
        }
    }

    private IEnumerator ShakeAndFadeOutHeart(Image heart)
    {
        int idx = System.Array.IndexOf(hearts, heart);
        var rect = heart.GetComponent<RectTransform>();
        Vector3 originalScale = _baseScales[idx];

        // Вздрагивание
        rect.localScale = originalScale * 0.7f;
        yield return new WaitForSeconds(0.08f);
        rect.localScale = originalScale;

        // Моментально выключаем сердечко
        heart.enabled = false;
        var c = heart.color;
        c.a = 1f;
        heart.color = c;
        rect.localScale = originalScale;
    }

    private IEnumerator AppearHeart(Image heart)
    {
        int idx = System.Array.IndexOf(hearts, heart);
        var rect = heart.GetComponent<RectTransform>();
        Vector3 originalScale = _baseScales[idx];
        Color c = heart.color;

        rect.localScale = originalScale * 0.7f;
        c.a = 0f;
        heart.color = c;
        heart.enabled = true;

        // Анимация появления
        float appearTime = 0.25f;
        float t = 0f;
        while (t < appearTime)
        {
            t += Time.deltaTime;
            float progress = t / appearTime;
            rect.localScale = Vector3.Lerp(originalScale * 0.7f, originalScale, progress);
            c.a = Mathf.Lerp(0f, 1f, progress);
            heart.color = c;
            yield return null;
        }

        rect.localScale = originalScale;
        c.a = 1f;
        heart.color = c;
    }
}